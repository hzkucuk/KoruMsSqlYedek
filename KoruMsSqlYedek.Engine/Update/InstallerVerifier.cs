using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace KoruMsSqlYedek.Engine.Update
{
    /// <summary>
    /// Self-update installer'ının güvenli indirilmesi ve doğrulanması için yardımcılar.
    /// Servis (LocalSystem) tarafından kullanılır: tray'den gelen URL yalnızca
    /// https + GitHub hostlarıysa kabul edilir; indirilen dosya SHA-256 ile doğrulanır.
    /// </summary>
    public static class InstallerVerifier
    {
        /// <summary>İndirme için kabul edilen hostlar (tam eşleşme, büyük/küçük harf duyarsız).</summary>
        private static readonly string[] AllowedHosts =
        {
            "github.com",
            "api.github.com",
            "objects.githubusercontent.com",
            "release-assets.githubusercontent.com"
        };

        private static readonly Lazy<HttpClient> Http = new Lazy<HttpClient>(() =>
        {
            // Yönlendirmeler kapalı: github.com → objects.githubusercontent.com gibi
            // her adım ayrı ayrı IsAllowedDownloadUrl'den geçirilir.
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("KoruMsSqlYedek-Service", "1.0"));
            return client;
        });

        /// <summary>
        /// URL yalnızca https şemasıyla ve izin verilen GitHub hostlarından biriyle
        /// mutlak bir URI ise true döner. Kullanıcı bilgisi (user:pass@) içeren URL'ler reddedilir.
        /// </summary>
        public static bool IsAllowedDownloadUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrEmpty(uri.UserInfo))
                return false;

            if (uri.HostNameType != UriHostNameType.Dns)
                return false;

            foreach (string host in AllowedHosts)
            {
                if (string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>Beklenen özet 64 hex karakter mi?</summary>
        public static bool IsValidSha256Hex(string sha256Hex)
        {
            if (string.IsNullOrWhiteSpace(sha256Hex) || sha256Hex.Length != 64)
                return false;

            foreach (char c in sha256Hex)
            {
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex) return false;
            }

            return true;
        }

        /// <summary>Dosyanın SHA-256 özetini küçük harf hex olarak döndürür.</summary>
        public static async Task<string> ComputeSha256HexAsync(string path, CancellationToken ct = default)
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 1 << 16, useAsync: true);

            byte[] hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Hesaplanan özeti beklenen değerle büyük/küçük harf duyarsız ve sabit zamanlı karşılaştırır.
        /// </summary>
        public static bool Sha256Matches(string actualHex, string expectedHex)
        {
            if (!IsValidSha256Hex(actualHex) || !IsValidSha256Hex(expectedHex))
                return false;

            byte[] a = Convert.FromHexString(actualHex);
            byte[] b = Convert.FromHexString(expectedHex);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>
        /// URL'deki dosyayı belirtilen yola indirir. Her yönlendirme adımı da
        /// <see cref="IsAllowedDownloadUrl"/> ile denetlenir (en fazla 5 yönlendirme).
        /// Hedef dosya varsa üzerine yazılır.
        /// </summary>
        public static async Task DownloadToFileAsync(string url, string path, CancellationToken ct = default)
        {
            if (!IsAllowedDownloadUrl(url))
                throw new InvalidOperationException($"İndirme URL'i izin verilen listede değil: {url}");

            string currentUrl = url;
            const int maxRedirects = 5;

            for (int hop = 0; hop <= maxRedirects; hop++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUrl);
                using var response = await Http.Value
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);

                int status = (int)response.StatusCode;
                if (status is 301 or 302 or 303 or 307 or 308)
                {
                    var location = response.Headers.Location;
                    if (location == null)
                        throw new HttpRequestException("Yönlendirme yanıtında Location başlığı yok.");

                    string next = location.IsAbsoluteUri
                        ? location.ToString()
                        : new Uri(new Uri(currentUrl), location).ToString();

                    if (!IsAllowedDownloadUrl(next))
                        throw new InvalidOperationException($"Yönlendirme hedefi izin verilen listede değil: {next}");

                    currentUrl = next;
                    continue;
                }

                response.EnsureSuccessStatusCode();

                await using (var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
                await using (var target = new FileStream(
                    path, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 1 << 16, useAsync: true))
                {
                    await source.CopyToAsync(target, ct).ConfigureAwait(false);
                }

                return;
            }

            throw new HttpRequestException("Çok fazla yönlendirme.");
        }
    }
}
