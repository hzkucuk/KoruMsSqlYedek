using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// Google Drive OAuth2 kimlik doğrulama yardımcı sınıfı.
    /// Token yönetimi, yenileme ve DriveService oluşturma işlemlerini sağlar.
    /// </summary>
    public static class GoogleDriveAuthHelper
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GoogleDriveAuthHelper));

        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };

        private const string ApplicationName = "KoruMsSqlYedek";

        // Uygulama genelinde özel credential override (AppSettings'ten yüklenir)
        private static string _customClientId;
        private static string _customClientSecret;

        /// <summary>
        /// AppSettings'ten okunan özel credential'ları ayarlar.
        /// Uygulama başlangıcında veya ayar değişikliğinde çağrılmalıdır.
        /// null geçilirse gömülü credential'lara dönülür.
        /// </summary>
        public static void SetCustomCredentials(string clientId, string clientSecret)
        {
            _customClientId = clientId;
            _customClientSecret = clientSecret;
            Log.Debug("Google OAuth özel credential {Status}.",
                !string.IsNullOrEmpty(clientId) ? "ayarlandı" : "temizlendi");
        }

        /// <summary>Aktif credential çiftini döner (özel > gömülü).</summary>
        internal static (string ClientId, string ClientSecret) ResolveCredentials()
        {
            string id = !string.IsNullOrEmpty(_customClientId)
                ? _customClientId
                : GoogleOAuthCredentials.ClientId;
            string secret = !string.IsNullOrEmpty(_customClientSecret)
                ? _customClientSecret
                : GoogleOAuthCredentials.ClientSecret;
            return (id, secret);
        }

        /// <summary>
        /// CloudTargetConfig'den DriveService oluşturur.
        /// Token bilgisi OAuthTokenJson'dan yüklenir.
        /// AppSettings'te özel credential varsa onu kullanır, yoksa gömülü.
        /// </summary>
        public static async Task<DriveService> CreateDriveServiceAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var credential = await GetCredentialAsync(config, cancellationToken).ConfigureAwait(false);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        /// <summary>
        /// Gömülü credential'ları kullanarak OAuth2 interaktif kimlik doğrulama başlatır.
        /// Kullanıcı tarayıcıda Google hesabını onaylar.
        /// </summary>
        public static Task<string> AuthorizeInteractiveAsync(
            CancellationToken cancellationToken)
        {
            var (clientId, clientSecret) = ResolveCredentials();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                throw new InvalidOperationException(
                    "Google OAuth kimlik bilgileri yapılandırılmamış. " +
                    "Lütfen geliştiriciye başvurun veya kendi Client ID/Secret değerlerinizi girin.");

            return AuthorizeInteractiveAsync(clientId, clientSecret, cancellationToken);
        }

        /// <summary>
        /// OAuth2 interaktif kimlik doğrulama akışını başlatır (tarayıcı açılır).
        /// Sonuç token JSON olarak döner ve CloudTargetConfig.OAuthTokenJson'a kaydedilmelidir.
        /// </summary>
        public static async Task<string> AuthorizeInteractiveAsync(
            string clientId,
            string clientSecret,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("OAuth2 Client ID boş olamaz.", nameof(clientId));
            if (string.IsNullOrEmpty(clientSecret))
                throw new ArgumentException("OAuth2 Client Secret boş olamaz.", nameof(clientSecret));

            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                Scopes,
                "user",
                cancellationToken,
                new NullDataStore()).ConfigureAwait(false);

            var tokenJson = SerializeToken(credential.Token);

            Log.Information("Google Drive OAuth2 kimlik doğrulama başarılı.");
            return tokenJson;
        }

        /// <summary>
        /// Mevcut token'ı yenileyerek güncel token JSON döner.
        /// </summary>
        public static async Task<string> RefreshTokenAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            var credential = await GetCredentialAsync(config, cancellationToken).ConfigureAwait(false);

            if (await credential.RefreshTokenAsync(cancellationToken).ConfigureAwait(false))
            {
                Log.Debug("Google Drive token yenilendi.");
                return SerializeToken(credential.Token);
            }

            Log.Warning("Google Drive token yenilenemedi.");
            return null;
        }

        /// <summary>
        /// Token'ın geçerli olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsTokenValid(string oauthTokenJson)
        {
            if (string.IsNullOrEmpty(oauthTokenJson))
                return false;

            try
            {
                string json = DecryptIfNeeded(oauthTokenJson);
                var token = JsonConvert.DeserializeObject<TokenResponse>(json);
                return token != null && !string.IsNullOrEmpty(token.RefreshToken);
            }
            catch
            {
                return false;
            }
        }

        #region Private Helpers

        private static async Task<UserCredential> GetCredentialAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            var (clientId, clientSecret) = ResolveCredentials();

            string tokenJson = DecryptIfNeeded(config.OAuthTokenJson);

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                throw new InvalidOperationException("OAuth2 Client ID veya Client Secret yapılandırılmamış.");

            if (string.IsNullOrEmpty(tokenJson))
                throw new InvalidOperationException("OAuth2 token bulunamadı. Önce interaktif kimlik doğrulama yapılmalıdır.");

            var token = JsonConvert.DeserializeObject<TokenResponse>(tokenJson);
            if (token == null)
                throw new InvalidOperationException("OAuth2 token JSON geçersiz.");

            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = Scopes
            });

            var credential = new UserCredential(flow, "user", token);

            // Token süresi dolmuşsa otomatik yenile
            if (credential.Token.IsStale)
            {
                Log.Debug("Google Drive token süresi dolmuş, yenileniyor...");
                await credential.RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
            }

            return credential;
        }

        private static string DecryptIfNeeded(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            try
            {
                return PasswordProtector.IsProtected(value)
                    ? PasswordProtector.Unprotect(value)
                    : value;
            }
            catch
            {
                return value;
            }
        }

        private static string SerializeToken(TokenResponse token)
        {
            return JsonConvert.SerializeObject(new
            {
                access_token = token.AccessToken,
                refresh_token = token.RefreshToken,
                token_type = token.TokenType,
                expires_in = token.ExpiresInSeconds,
                issued = token.IssuedUtc.ToString("O")
            });
        }

        #endregion

        /// <summary>
        /// Verilen token JSON ile bağlı Google hesabının e-posta adresini döner.
        /// Token geçersiz veya sorgu başarısız olursa null döner.
        /// </summary>
        public static async Task<string> GetAccountEmailAsync(
            string oauthTokenJson,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(oauthTokenJson))
                return null;

            try
            {
                string json = DecryptIfNeeded(oauthTokenJson);
                var token = JsonConvert.DeserializeObject<TokenResponse>(json);
                if (token is null)
                    return null;

                var (clientId, clientSecret) = ResolveCredentials();
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                    Scopes = Scopes
                });

                var credential = new UserCredential(flow, "user", token);

                using var service = new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                var request = service.About.Get();
                request.Fields = "user(emailAddress)";
                var about = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                return about?.User?.EmailAddress;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Google hesap e-postası alınamadı.");
                return null;
            }
        }

        /// <summary>
        /// Token saklamayan IDataStore implementasyonu.
        /// Etkileşimli auth sırasında kullanılır; token CloudTargetConfig'de DPAPI ile saklanır.
        /// </summary>
        private class NullDataStore : IDataStore
        {
            public Task StoreAsync<T>(string key, T value) => Task.CompletedTask;
            public Task DeleteAsync<T>(string key) => Task.CompletedTask;
            public Task<T> GetAsync<T>(string key) => Task.FromResult(default(T));
            public Task ClearAsync() => Task.CompletedTask;
        }
    }
}
