using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Core.Constants;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// Mega.io cloud provider.
    /// Email/şifre ile kimlik doğrulama, dosya yükleme (progress), silme ve klasör yönetimi destekler.
    /// </summary>
    public partial class MegaProvider : ICloudProvider
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MegaProvider>();

        /// <summary>Login zaman aşımı (saniye). Hashcash/PBKDF2 hesaplaması nedeniyle yüksek tutulur.</summary>
        private const int LoginTimeoutSeconds = TimeoutConstants.MegaLoginTimeoutSeconds;

        /// <summary>Logout temizliği için kısa zaman aşımı — takılırsa beklemeyi kes.</summary>
        private const int LogoutTimeoutSeconds = TimeoutConstants.MegaLogoutTimeoutSeconds;

        /// <summary>Bağlantı ön kontrolü zaman aşımı (saniye).</summary>
        private const int ConnectivityCheckSeconds = TimeoutConstants.MegaConnectivityCheckSeconds;

        /// <summary>Oturum önbellek süresi (dakika). Bu süre boyunca aynı oturum yeniden kullanılır.</summary>
        private const int SessionExpiryMinutes = TimeoutConstants.MegaSessionExpiryMinutes;

        /// <summary>Mega API endpoint — bağlantı ön kontrolü için.</summary>
        private const string MegaApiUrl = "https://g.api.mega.co.nz/cs";

        /// <summary>Singleton HttpClient — bağlantı ön kontrolü için.</summary>
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(ConnectivityCheckSeconds)
        };

        // ── Session Caching ──────────────────────────────────────────
        // Aynı oturum 15 dakikaya kadar yeniden kullanılır.
        // SemaphoreSlim ile tüm Mega API çağrıları sıralı işlenir — rate limiting önlenir.
        private static MegaApiClient _cachedClient;
        private static string _cachedEmail;
        private static DateTime _sessionLastUsedUtc;
        private static readonly SemaphoreSlim _sessionSemaphore = new(1, 1);

        public CloudProviderType ProviderType => CloudProviderType.Mega;

        public string DisplayName => "Mega.io";

        public bool SupportsTrash => true;

        /// <summary>
        /// Config doğrulaması: Mega için email ve şifre zorunludur.
        /// </summary>
        private static void ValidateConfig(CloudTargetConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config), "Cloud hedef yapılandırması null olamaz.");

            if (string.IsNullOrWhiteSpace(config.Username))
                throw new InvalidOperationException("Mega email adresi belirtilmemiş.");

            if (string.IsNullOrWhiteSpace(config.Password))
                throw new InvalidOperationException("Mega şifresi belirtilmemiş.");
        }

        /// <summary>
        /// Önbelleğe alınmış Mega oturumunu döndürür veya yeni oturum oluşturur.
        /// Aynı email ile 15 dakika içinde yapılan çağrılar mevcut oturumu yeniden kullanır.
        /// Semaphore dışından ÇAĞRILMAMALI — caller semaphore'u tutmalı.
        /// </summary>
        private static async Task<MegaApiClient> GetOrCreateSessionAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            string email = config.Username;

            // Önbellek geçerli mi kontrol et
            if (_cachedClient is not null
                && string.Equals(_cachedEmail, email, StringComparison.OrdinalIgnoreCase)
                && (DateTime.UtcNow - _sessionLastUsedUtc).TotalMinutes < SessionExpiryMinutes)
            {
                Log.Information("Mega oturum yeniden kullanılıyor: {Email} (son kullanım: {Age:N0}s önce)",
                    email, (DateTime.UtcNow - _sessionLastUsedUtc).TotalSeconds);
                _sessionLastUsedUtc = DateTime.UtcNow;
                return _cachedClient;
            }

            // Eski oturum varsa kapat
            if (_cachedClient is not null)
            {
                string reason = !string.Equals(_cachedEmail, email, StringComparison.OrdinalIgnoreCase)
                    ? "email değişti"
                    : "süre doldu";
                Log.Information("Mega oturumu kapatılıyor ({Reason}), yeni oturum açılacak.", reason);
                await InvalidateSessionInternalAsync().ConfigureAwait(false);
            }

            // Yeni oturum aç — SynchronizeApiRequests=false: sıralama _sessionSemaphore ile sağlanıyor
            var client = new MegaApiClient(new Options(synchronizeApiRequests: false));
            await LoginWithTimeoutAsync(client, config, cancellationToken).ConfigureAwait(false);

            _cachedClient = client;
            _cachedEmail = email;
            _sessionLastUsedUtc = DateTime.UtcNow;

            Log.Information("Mega yeni oturum açıldı: {Email}", email);
            return client;
        }

        /// <summary>
        /// Önbelleğe alınmış oturumu kapatır ve temizler.
        /// Semaphore dışından ÇAĞRILMAMALI — caller semaphore'u tutmalı.
        /// </summary>
        private static async Task InvalidateSessionInternalAsync()
        {
            if (_cachedClient is not null)
            {
                await LogoutSafeAsync(_cachedClient).ConfigureAwait(false);
                _cachedClient = null;
                _cachedEmail = null;
                Log.Debug("Mega oturum önbelleği temizlendi.");
            }
        }

        /// <summary>
        /// Mega hesabına email/şifre ile giriş yapar.
        /// Önce bağlantı ön kontrolü yapılır (10s), ardından login denenir (90s timeout).
        /// MegaApiClient.LoginAsync CancellationToken desteklemediği için timeout koruması uygulanır.
        /// LoginAsync, Task.Run içinde çalıştırılır — kütüphanenin dahili sync-over-async çağrıları
        /// .NET 10'da thread pool'da güvenli şekilde çözülür.
        /// </summary>
        private static async Task LoginWithTimeoutAsync(
            MegaApiClient client,
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string email = config.Username;
            string password = PasswordProtector.Unprotect(config.Password);

            if (string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException(
                    "Mega şifresi çözülemedi — şifre DPAPI ile korunmuş olabilir ve farklı bir makinede çözülemez. " +
                    "Mega hesap ayarlarından şifreyi yeniden girin.");
            }

            Log.Debug("Mega login başlıyor: email={Email}, şifre uzunluk={PasswordLength}",
                email, password.Length);

            // ── Bağlantı ön kontrolü ─────────────────────────────────
            // Mega API sunucusuna hızlı HTTP isteği göndererek erişilebilirliği doğrula.
            // Başarısızsa login timeout beklemek yerine anında hata verilir.
            await CheckMegaConnectivityAsync(cancellationToken).ConfigureAwait(false);

            Log.Debug("Mega bağlantı kontrolü başarılı, login deneniyor ({Timeout}s timeout)...", LoginTimeoutSeconds);

            // ── Login ────────────────────────────────────────────────
            // Task.Run ile sarmalama: MegaApiClient dahili olarak sync PostRequestJson çağrıları
            // yapar (HttpClient.SendAsync().GetAwaiter().GetResult()). .NET 10'da bu çağrılar
            // thread pool'da güvenli çalışması için Task.Run gerekebilir.
            var loginTask = Task.Run(
                () => client.LoginAsync(email, password),
                cancellationToken);

            var completed = await Task.WhenAny(
                loginTask,
                Task.Delay(TimeSpan.FromSeconds(LoginTimeoutSeconds), cancellationToken))
                .ConfigureAwait(false);

            if (completed != loginTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException(
                    $"Mega giriş zaman aşımına uğradı ({LoginTimeoutSeconds} saniye). " +
                    "Sunucu erişilebilir ancak giriş yanıt vermiyor. " +
                    "Olası nedenler: (1) Email/şifre hatalı, (2) Hesap kilitli/2FA aktif, " +
                    "(3) Mega sunucu yoğunluğu. Email/şifre bilgilerinizi kontrol edin.");
            }

            // loginTask faulted olabilir — await ile exception'ı yakala
            await loginTask.ConfigureAwait(false);

            Log.Information("Mega login başarılı: {Email}", email);
        }

        /// <summary>
        /// Mega API sunucusuna hızlı HTTP isteği göndererek bağlantıyı doğrular.
        /// DNS çözümleme veya firewall sorunu varsa anında bildirir.
        /// </summary>
        private static async Task CheckMegaConnectivityAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.Debug("Mega bağlantı ön kontrolü: {Url}", MegaApiUrl);

                using var request = new HttpRequestMessage(HttpMethod.Post, MegaApiUrl);
                request.Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                Log.Debug("Mega bağlantı ön kontrolü başarılı: HTTP {StatusCode}", (int)response.StatusCode);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                // HttpClient.Timeout aşıldı (CancellationToken'dan değil)
                throw new TimeoutException(
                    $"Mega API sunucusuna erişilemiyor ({ConnectivityCheckSeconds} saniye zaman aşımı). " +
                    "İnternet bağlantınızı, DNS ayarlarınızı ve güvenlik duvarınızı kontrol edin. " +
                    $"Endpoint: {MegaApiUrl}");
            }
            catch (HttpRequestException ex)
            {
                throw new TimeoutException(
                    $"Mega API sunucusuna bağlantı başarısız: {ex.Message}. " +
                    "İnternet bağlantınızı ve güvenlik duvarınızı kontrol edin. " +
                    $"Endpoint: {MegaApiUrl}");
            }
        }
        /// <summary>
        /// Güvenli logout — takılırsa timeout ile keser, hata yutulur.
        /// Upload/delete sonrası finally bloğunda çağrılır.
        /// </summary>
        private static async Task LogoutSafeAsync(MegaApiClient client)
        {
            try
            {
                var logoutTask = client.LogoutAsync();
                var completed = await Task.WhenAny(
                    logoutTask,
                    Task.Delay(TimeSpan.FromSeconds(LogoutTimeoutSeconds)))
                    .ConfigureAwait(false);

                if (completed == logoutTask)
                    await logoutTask.ConfigureAwait(false);
                else
                    Log.Debug("Mega logout zaman aşımına uğradı, oturum sunucu tarafında kapanacak.");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Mega logout sırasında hata (önemsiz).");
            }
        }
    }
}
