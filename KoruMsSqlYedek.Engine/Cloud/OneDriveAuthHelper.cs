using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// OneDrive MSAL kimlik doğrulama yardımcı sınıfı.
    /// Token yönetimi, yenileme ve GraphServiceClient oluşturma işlemlerini sağlar.
    /// Bireysel (MSA) ve kurumsal (Entra ID) hesapları destekler.
    /// </summary>
    public static class OneDriveAuthHelper
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(OneDriveAuthHelper));

        private static readonly string[] Scopes = { "Files.ReadWrite.All", "User.Read" };

        private const string PersonalAuthority = "https://login.microsoftonline.com/consumers";
        private const string CommonAuthority = "https://login.microsoftonline.com/common";

        /// <summary>
        /// CloudTargetConfig'den GraphServiceClient oluşturur.
        /// Token bilgisi OAuthTokenJson'dan (MSAL cache) yüklenir ve otomatik yenilenir.
        /// </summary>
        public static async Task<GraphServiceClient> CreateGraphClientAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(config.OAuthClientId))
                throw new InvalidOperationException("OAuth2 Client ID yapılandırılmamış.");

            if (string.IsNullOrEmpty(config.OAuthTokenJson))
                throw new InvalidOperationException("OneDrive token bulunamadı. Önce interaktif kimlik doğrulama yapılmalıdır.");

            var app = BuildMsalApp(config.OAuthClientId, config.Type, config.OAuthTokenJson);

            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            var account = accounts.FirstOrDefault();

            if (account == null)
                throw new InvalidOperationException(
                    "OneDrive hesap bilgisi bulunamadı. Önce interaktif kimlik doğrulama yapılmalıdır.");

            AuthenticationResult result;
            try
            {
                result = await app.AcquireTokenSilent(Scopes, account)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                throw new InvalidOperationException(
                    "OneDrive token süresi dolmuş ve yenilenemedi. Lütfen yeniden kimlik doğrulama yapın.");
            }

            Log.Debug("OneDrive token alındı. Kullanıcı: {User}, Süre: {Expiry}",
                result.Account?.Username, result.ExpiresOn);

            var tokenProvider = new StaticAccessTokenProvider(result.AccessToken);
            var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

            return new GraphServiceClient(authProvider);
        }

        /// <summary>
        /// OAuth2 interaktif kimlik doğrulama akışını başlatır (tarayıcı açılır).
        /// Sonuç MSAL cache (Base64) olarak döner ve CloudTargetConfig.OAuthTokenJson'a kaydedilmelidir.
        /// </summary>
        public static async Task<string> AuthorizeInteractiveAsync(
            string clientId,
            CloudProviderType type,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("OAuth2 Client ID boş olamaz.", nameof(clientId));

            string authority = type == CloudProviderType.OneDrivePersonal
                ? PersonalAuthority
                : CommonAuthority;

            var app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(authority)
                .WithDefaultRedirectUri()
                .Build();

            byte[] updatedCache = null;
            app.UserTokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    updatedCache = args.TokenCache.SerializeMsalV3();
                }
            });

            var result = await app.AcquireTokenInteractive(Scopes)
                .WithUseEmbeddedWebView(false)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            Log.Information("OneDrive OAuth2 kimlik doğrulama başarılı. Kullanıcı: {User}", result.Account?.Username);

            if (updatedCache == null || updatedCache.Length == 0)
                throw new InvalidOperationException("MSAL token cache serileştirilemedi.");

            return Convert.ToBase64String(updatedCache);
        }

        /// <summary>
        /// Saklanan token bilgisinin geçerli olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsTokenValid(string oauthTokenJson)
        {
            if (string.IsNullOrEmpty(oauthTokenJson))
                return false;

            try
            {
                byte[] cacheData = DecodeCacheData(oauthTokenJson);
                return cacheData != null && cacheData.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        #region Private Helpers

        private static IPublicClientApplication BuildMsalApp(
            string clientId,
            CloudProviderType type,
            string serializedCache)
        {
            string authority = type == CloudProviderType.OneDrivePersonal
                ? PersonalAuthority
                : CommonAuthority;

            var app = PublicClientApplicationBuilder
                .Create(clientId)
                .WithAuthority(authority)
                .WithDefaultRedirectUri()
                .Build();

            if (!string.IsNullOrEmpty(serializedCache))
            {
                byte[] cacheData = DecodeCacheData(serializedCache);
                if (cacheData != null && cacheData.Length > 0)
                {
                    app.UserTokenCache.SetBeforeAccess(args =>
                    {
                        args.TokenCache.DeserializeMsalV3(cacheData);
                    });
                }
            }

            return app;
        }

        private static byte[] DecodeCacheData(string serializedCache)
        {
            string data = DecryptIfNeeded(serializedCache);
            return Convert.FromBase64String(data);
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
            catch (Exception ex)
            {
                Log.Warning(ex, "OneDrive token çözme başarısız, ham değer kullanılıyor");
                return value;
            }
        }

        #endregion

        /// <summary>
        /// Sabit access token sağlayan IAccessTokenProvider implementasyonu.
        /// GraphServiceClient oluşturmak için kullanılır.
        /// </summary>
        private class StaticAccessTokenProvider : IAccessTokenProvider
        {
            private readonly string _accessToken;

            public StaticAccessTokenProvider(string accessToken)
            {
                _accessToken = accessToken;
            }

            public Task<string> GetAuthorizationTokenAsync(
                Uri uri,
                Dictionary<string, object> additionalAuthenticationContext = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_accessToken);
            }

            public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();
        }
    }
}
