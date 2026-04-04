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

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// Mega.io cloud provider.
    /// Email/şifre ile kimlik doğrulama, dosya yükleme (progress), silme ve klasör yönetimi destekler.
    /// </summary>
    public class MegaProvider : ICloudProvider
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MegaProvider>();

        /// <summary>Login/Logout gibi CancellationToken almayan API çağrıları için zaman aşımı.</summary>
        private const int ApiTimeoutSeconds = 30;

        /// <summary>Logout temizliği için kısa zaman aşımı — takılırsa beklemeyi kes.</summary>
        private const int LogoutTimeoutSeconds = 10;

        /// <summary>Bağlantı ön kontrolü zaman aşımı (saniye).</summary>
        private const int ConnectivityCheckSeconds = 10;

        /// <summary>Mega API endpoint — bağlantı ön kontrolü için.</summary>
        private const string MegaApiUrl = "https://g.api.mega.co.nz/cs";

        /// <summary>Singleton HttpClient — bağlantı ön kontrolü için.</summary>
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(ConnectivityCheckSeconds)
        };

        public CloudProviderType ProviderType => CloudProviderType.Mega;

        public string DisplayName => "Mega.io";

        public bool SupportsTrash => true;

        public async Task<CloudUploadResult> UploadAsync(
            string localFilePath,
            string remoteFileName,
            CloudTargetConfig config,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            string resumeSessionUri = null,
            Action<string> sessionUriObtained = null)
        {
            var result = new CloudUploadResult
            {
                ProviderType = CloudProviderType.Mega,
                DisplayName = DisplayName
            };

            MegaApiClient client = null;

            try
            {
                ValidateConfig(config);

                if (!File.Exists(localFilePath))
                    throw new FileNotFoundException("Kaynak dosya bulunamadı.", localFilePath);

                client = new MegaApiClient();

                Log.Debug("Mega giriş yapılıyor: {Email}", config.Username);
                await LoginWithTimeoutAsync(client, config, cancellationToken).ConfigureAwait(false);
                Log.Debug("Mega giriş başarılı.");

                cancellationToken.ThrowIfCancellationRequested();

                Log.Debug("Mega hedef klasör kontrol ediliyor: {Path}", config.RemoteFolderPath ?? "(kök)");
                var targetFolder = await EnsureFolderExistsAsync(client, config.RemoteFolderPath, cancellationToken)
                    .ConfigureAwait(false);
                Log.Debug("Mega hedef klasör hazır: {FolderId}", targetFolder.Id);

                cancellationToken.ThrowIfCancellationRequested();

                long fileSize = new FileInfo(localFilePath).Length;

                // Progress adaptörü: MegaApiClient IProgress<double> (0.0-1.0) → IProgress<int> (0-100)
                var megaProgress = progress is not null
                    ? new Progress<double>(d => progress.Report((int)(d * 100)))
                    : null;

                Log.Debug("Mega upload başlıyor: {FileName} ({Size:N0} bytes)", remoteFileName, fileSize);

                INode uploadedNode;
                using (var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    uploadedNode = await UploadWithCancellationAsync(
                        client, stream, remoteFileName, targetFolder, megaProgress, cancellationToken)
                        .ConfigureAwait(false);
                }

                result.IsSuccess = true;
                result.RemoteFilePath = uploadedNode.Id;
                result.RemoteFileSizeBytes = uploadedNode.Size;
                result.UploadedAt = DateTime.UtcNow;

                Log.Information(
                    "Mega upload başarılı: {FileName} → {NodeId} ({Size:N0} bytes)",
                    remoteFileName, uploadedNode.Id, fileSize);
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "İşlem kullanıcı tarafından iptal edildi.";
                Log.Warning("Mega upload iptal edildi: {FileName}", remoteFileName);
            }
            catch (TimeoutException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Log.Error("Mega zaman aşımı: {FileName} — {Message}", remoteFileName, ex.Message);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Log.Error(ex, "Mega upload başarısız: {FileName}", remoteFileName);
            }
            finally
            {
                if (client is not null)
                    await LogoutSafeAsync(client).ConfigureAwait(false);
            }

            return result;
        }

        public async Task<bool> DeleteAsync(
            string remoteFileIdentifier,
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            MegaApiClient client = null;

            try
            {
                client = new MegaApiClient();
                await LoginWithTimeoutAsync(client, config, cancellationToken).ConfigureAwait(false);

                var nodes = await client.GetNodesAsync().ConfigureAwait(false);
                var targetNode = nodes.FirstOrDefault(n => n.Id == remoteFileIdentifier);

                if (targetNode is null)
                {
                    Log.Debug("Mega dosyası zaten mevcut değil: {NodeId}", remoteFileIdentifier);
                    return true;
                }

                bool moveToTrash = !config.PermanentDeleteFromTrash;
                await client.DeleteAsync(targetNode, moveToTrash).ConfigureAwait(false);

                Log.Information(
                    moveToTrash
                        ? "Mega dosyası çöp kutusuna taşındı: {NodeId}"
                        : "Mega dosyası kalıcı olarak silindi: {NodeId}",
                    remoteFileIdentifier);

                return true;
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Mega silme iptal edildi: {NodeId}", remoteFileIdentifier);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Mega silme başarısız: {NodeId}", remoteFileIdentifier);
                return false;
            }
            finally
            {
                if (client is not null)
                    await LogoutSafeAsync(client).ConfigureAwait(false);
            }
        }

        public async Task<bool> TestConnectionAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            MegaApiClient client = null;

            try
            {
                ValidateConfig(config);

                client = new MegaApiClient();
                await LoginWithTimeoutAsync(client, config, cancellationToken).ConfigureAwait(false);

                var accountInfo = await client.GetAccountInformationAsync().ConfigureAwait(false);

                long usedMb = accountInfo.UsedQuota / (1024 * 1024);
                long totalMb = accountInfo.TotalQuota / (1024 * 1024);

                Log.Information(
                    "Mega bağlantı testi başarılı — Kullanım: {Used:N0} MB / {Total:N0} MB",
                    usedMb, totalMb);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Mega bağlantı testi başarısız.");
                return false;
            }
            finally
            {
                if (client is not null)
                    await LogoutSafeAsync(client).ConfigureAwait(false);
            }
        }

        #region Private Helpers

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
        /// Mega hesabına email/şifre ile giriş yapar.
        /// Önce bağlantı ön kontrolü yapılır (10s), ardından login denenir (30s timeout).
        /// MegaApiClient.LoginAsync CancellationToken desteklemediği için timeout koruması uygulanır.
        /// </summary>
        private static async Task LoginWithTimeoutAsync(
            MegaApiClient client,
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string email = config.Username;
            string password = PasswordProtector.Unprotect(config.Password);

            Log.Debug("Mega kimlik bilgileri: email={Email}, şifre uzunluk={PasswordLength}",
                email, password?.Length ?? 0);

            // ── Bağlantı ön kontrolü ─────────────────────────────────
            // Mega API sunucusuna hızlı HTTP isteği göndererek erişilebilirliği doğrula.
            // Başarısızsa 30 saniye login timeout beklemek yerine anında hata verilir.
            await CheckMegaConnectivityAsync(cancellationToken).ConfigureAwait(false);

            // ── Login ────────────────────────────────────────────────
            var loginTask = client.LoginAsync(email, password);
            var completed = await Task.WhenAny(
                loginTask,
                Task.Delay(TimeSpan.FromSeconds(ApiTimeoutSeconds), cancellationToken))
                .ConfigureAwait(false);

            if (completed != loginTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException(
                    $"Mega giriş zaman aşımına uğradı ({ApiTimeoutSeconds} saniye). " +
                    "Sunucu erişilebilir ancak giriş yanıt vermiyor. " +
                    "Email/şifre bilgilerinizi kontrol edin.");
            }

            await loginTask.ConfigureAwait(false);
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
        /// Upload işlemini CancellationToken ile korur.
        /// MegaApiClient token'ı düzgün işlemezse WhenAny ile iptal garantilenir.
        /// </summary>
        private static async Task<INode> UploadWithCancellationAsync(
            MegaApiClient client,
            FileStream stream,
            string remoteFileName,
            INode targetFolder,
            IProgress<double> megaProgress,
            CancellationToken cancellationToken)
        {
            var uploadTask = client.UploadAsync(
                stream,
                remoteFileName,
                targetFolder,
                megaProgress,
                null,
                cancellationToken);

            var completed = await Task.WhenAny(
                uploadTask,
                Task.Delay(Timeout.Infinite, cancellationToken))
                .ConfigureAwait(false);

            if (completed != uploadTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return await uploadTask.ConfigureAwait(false);
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

        /// <summary>
        /// Mega'da hedef klasör yapısını oluşturur veya bulur.
        /// Alt klasör desteği: "Yedekler/Plan1" → Yedekler altında Plan1 oluşturulur.
        /// </summary>
        private static async Task<INode> EnsureFolderExistsAsync(
            MegaApiClient client,
            string folderPath,
            CancellationToken cancellationToken)
        {
            var nodes = await client.GetNodesAsync().ConfigureAwait(false);
            var root = nodes.First(n => n.Type == NodeType.Root);

            if (string.IsNullOrWhiteSpace(folderPath))
                return root;

            // Slash ile ayrılmış alt klasörleri sırayla oluştur/bul
            string[] parts = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            INode current = root;

            foreach (string part in parts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var existing = nodes.FirstOrDefault(n =>
                    n.Type == NodeType.Directory &&
                    n.ParentId == current.Id &&
                    string.Equals(n.Name, part, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    current = existing;
                }
                else
                {
                    current = await client.CreateFolderAsync(part, current).ConfigureAwait(false);
                    Log.Debug("Mega klasörü oluşturuldu: {FolderName}", part);

                    // Yeni node listesini güncelle (sonraki iterasyon için)
                    nodes = await client.GetNodesAsync().ConfigureAwait(false);
                }
            }

            return current;
        }

        #endregion

        /// <summary>
        /// Mega çöp kutusundaki tüm dosyaları kalıcı olarak siler.
        /// Trash node'un doğrudan çocuklarını bulup her birini permanent delete yapar.
        /// </summary>
        public async Task<int> EmptyTrashAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            MegaApiClient client = null;

            try
            {
                client = new MegaApiClient();
                await LoginWithTimeoutAsync(client, config, cancellationToken).ConfigureAwait(false);

                var nodes = await client.GetNodesAsync().ConfigureAwait(false);
                var trashNode = nodes.FirstOrDefault(n => n.Type == NodeType.Trash);

                if (trashNode is null)
                {
                    Log.Information("Mega çöp kutusu bulunamadı.");
                    return 0;
                }

                // Trash'ın doğrudan çocuklarını bul
                var trashChildren = nodes
                    .Where(n => n.ParentId == trashNode.Id)
                    .ToList();

                if (trashChildren.Count == 0)
                {
                    Log.Information("Mega çöp kutusu zaten boş.");
                    return 0;
                }

                Log.Information("Mega çöp kutusu boşaltılıyor: {Count} öğe", trashChildren.Count);

                int deletedCount = 0;

                foreach (var node in trashChildren)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await client.DeleteAsync(node, moveToTrash: false).ConfigureAwait(false);
                        deletedCount++;
                        Log.Debug("Mega çöp öğesi kalıcı silindi: {NodeName} ({NodeId})", node.Name, node.Id);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Mega çöp öğesi silinemedi: {NodeName} ({NodeId})", node.Name, node.Id);
                    }
                }

                Log.Information("Mega çöp kutusu boşaltıldı: {Deleted}/{Total} öğe silindi",
                    deletedCount, trashChildren.Count);

                return deletedCount;
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Mega çöp boşaltma iptal edildi.");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Mega çöp kutusu boşaltılamadı.");
                return 0;
            }
            finally
            {
                if (client is not null)
                    await LogoutSafeAsync(client).ConfigureAwait(false);
            }
        }
    }
}
