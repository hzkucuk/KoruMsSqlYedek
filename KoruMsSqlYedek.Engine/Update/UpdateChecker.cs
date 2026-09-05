using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Interfaces;
using Newtonsoft.Json.Linq;
using Serilog;

namespace KoruMsSqlYedek.Engine.Update
{
    /// <summary>
    /// GitHub Releases API üzerinden güncelleme kontrolü.
    /// Public repo — kimlik doğrulama gerektirmez (60 req/saat rate limit).
    /// </summary>
    public class UpdateChecker : IUpdateService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<UpdateChecker>();

        private const string GitHubApiUrl =
            "https://api.github.com/repos/hzkucuk/KoruMsSqlYedek/releases/latest";

        private const string InstallerPrefix = "KoruMsSqlYedek_v";

        /// <summary>Checksum varlığı: "&lt;installer&gt;.sha256" (sha256sum formatı: "&lt;hex&gt;  &lt;dosya&gt;").</summary>
        private const string ChecksumSuffix = ".sha256";

        /// <summary>
        /// İndirme URL'lerinin kabul edildiği hostlar. GitHub Releases varlıkları
        /// bu hostlardan birine yönlenir; başka host = güncelleme yok say.
        /// </summary>
        private static readonly string[] AllowedDownloadHosts =
        {
            "github.com",
            "api.github.com",
            "objects.githubusercontent.com",
            "release-assets.githubusercontent.com"
        };

        private static readonly Regex Sha256HexRegex =
            new Regex("^[0-9a-fA-F]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HttpClient Http;

        static UpdateChecker()
        {
            Http = new HttpClient();
            Http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("KoruMsSqlYedek", GetCurrentVersion().ToString(3)));
            Http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            Http.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <inheritdoc/>
        public async Task<UpdateInfo> CheckForUpdateAsync(CancellationToken ct = default)
        {
            try
            {
                Version currentVersion = GetCurrentVersion();
                Log.Debug("Güncelleme kontrolü başlatılıyor. Mevcut sürüm: {Version}", currentVersion.ToString(3));

                string json = await Http.GetStringAsync(GitHubApiUrl, ct).ConfigureAwait(false);
                JObject release = JObject.Parse(json);

                string tagName = release["tag_name"]?.ToString();
                if (string.IsNullOrEmpty(tagName))
                {
                    Log.Debug("GitHub release'de tag_name bulunamadı.");
                    return null;
                }

                string versionStr = tagName.TrimStart('v', 'V');
                if (!Version.TryParse(versionStr, out Version latestVersion))
                {
                    Log.Warning("GitHub release tag parse edilemedi: {Tag}", tagName);
                    return null;
                }

                if (latestVersion <= currentVersion)
                {
                    Log.Debug("Güncel sürüm kullanılıyor. Mevcut: {Current}, Son: {Latest}",
                        currentVersion.ToString(3), latestVersion.ToString(3));
                    return null;
                }

                // Installer asset'ini bul
                JArray assets = release["assets"] as JArray;
                JToken installerAsset = assets?.FirstOrDefault(a =>
                    a["name"]?.ToString().StartsWith(InstallerPrefix, StringComparison.OrdinalIgnoreCase) == true &&
                    a["name"]?.ToString().EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true);

                if (installerAsset == null)
                {
                    Log.Warning("GitHub release'de installer asset bulunamadı: {Tag}", tagName);
                    return null;
                }

                string installerName = installerAsset["name"]?.ToString();
                string downloadUrl = installerAsset["browser_download_url"]?.ToString();

                // GÜVENLİK: Yalnızca https + GitHub hostlarından indirilir.
                if (!IsAllowedDownloadUrl(downloadUrl))
                {
                    Log.Warning("Installer indirme URL'i güvenilir değil, güncelleme yok sayılıyor: {Url}", downloadUrl);
                    return null;
                }

                // Checksum varlığını bul ve oku ("<installer>.sha256")
                string sha256 = await TryFetchChecksumAsync(assets, installerName, ct).ConfigureAwait(false);

                var updateInfo = new UpdateInfo
                {
                    Version = latestVersion.ToString(3),
                    Title = release["name"]?.ToString() ?? $"v{versionStr}",
                    ReleaseNotes = release["body"]?.ToString(),
                    DownloadUrl = downloadUrl,
                    FileSizeBytes = installerAsset["size"]?.Value<long>() ?? 0,
                    Sha256 = sha256,
                    PublishedAt = release["published_at"]?.Value<DateTime>() ?? DateTime.UtcNow,
                    HtmlUrl = release["html_url"]?.ToString()
                };

                Log.Information("Yeni sürüm bulundu: {NewVersion} (mevcut: {CurrentVersion})",
                    updateInfo.Version, currentVersion.ToString(3));

                return updateInfo;
            }
            catch (HttpRequestException ex)
            {
                Log.Warning(ex, "GitHub API erişim hatası (güncelleme kontrolü).");
                return null;
            }
            catch (TaskCanceledException)
            {
                Log.Debug("Güncelleme kontrolü zaman aşımı veya iptal.");
                return null;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Güncelleme kontrolü sırasında beklenmeyen hata.");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task DownloadInstallerAsync(
            string downloadUrl,
            string destinationPath,
            IProgress<int> progress = null,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(downloadUrl);
            ArgumentNullException.ThrowIfNull(destinationPath);

            if (!IsAllowedDownloadUrl(downloadUrl))
                throw new InvalidOperationException($"Installer indirme URL'i güvenilir değil: {downloadUrl}");

            Log.Information("Installer indiriliyor: {Url} → {Path}", downloadUrl, destinationPath);

            string directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using HttpResponseMessage response = await Http.GetAsync(
                downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long bytesRead = 0;
            int lastPercent = -1;

            using Stream contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using FileStream fileStream = new FileStream(
                destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            byte[] buffer = new byte[81920];
            int read;

            while ((read = await contentStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                bytesRead += read;

                if (totalBytes > 0 && progress is not null)
                {
                    int percent = (int)(bytesRead * 100 / totalBytes);
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        progress.Report(percent);
                    }
                }
            }

            Log.Information("Installer indirildi: {Size:F1} MB → {Path}",
                bytesRead / 1_048_576.0, destinationPath);
        }

        // ── Bütünlük doğrulama ──────────────────────────────────────────────

        /// <summary>
        /// Dosyanın SHA-256 özetini küçük harf hex olarak hesaplar.
        /// </summary>
        public static async Task<string> ComputeSha256HexAsync(string path, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(path);

            using FileStream stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            byte[] hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// İndirilen installer'ı beklenen SHA-256 (ve varsa boyut) ile karşılaştırır.
        /// Beklenen hash boş/geçersizse veya dosya yoksa false döner — asla "doğrulanmış" saymaz.
        /// </summary>
        /// <param name="path">Installer dosya yolu.</param>
        /// <param name="expectedSha256">Beklenen özet (64 hex karakter, büyük/küçük harf duyarsız).</param>
        /// <param name="expectedSize">Beklenen boyut (byte); 0 veya negatifse kontrol edilmez.</param>
        /// <param name="ct">İptal token'ı.</param>
        public static async Task<bool> VerifyInstallerAsync(
            string path, string expectedSha256, long expectedSize, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Log.Warning("Installer doğrulaması: dosya bulunamadı: {Path}", path);
                return false;
            }

            string expected = expectedSha256?.Trim();
            if (string.IsNullOrEmpty(expected) || !Sha256HexRegex.IsMatch(expected))
            {
                Log.Warning("Installer doğrulaması: beklenen SHA-256 eksik veya geçersiz.");
                return false;
            }

            if (expectedSize > 0)
            {
                long actualSize = new FileInfo(path).Length;
                if (actualSize != expectedSize)
                {
                    Log.Warning("Installer doğrulaması: boyut uyuşmuyor. Beklenen={Expected}, Gerçek={Actual}",
                        expectedSize, actualSize);
                    return false;
                }
            }

            string actual = await ComputeSha256HexAsync(path, ct).ConfigureAwait(false);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("Installer doğrulaması: SHA-256 uyuşmuyor. Beklenen={Expected}, Gerçek={Actual}",
                    expected, actual);
                return false;
            }

            Log.Information("Installer SHA-256 doğrulandı: {Path}", path);
            return true;
        }

        /// <summary>
        /// URL'nin https ve izin verilen GitHub hostlarından birine ait olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsAllowedDownloadUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return false;
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;

            foreach (string host in AllowedDownloadHosts)
            {
                if (string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// sha256sum formatındaki içerikten ("&lt;hex&gt;  &lt;dosya&gt;") ilk token'ı alıp
        /// 64 hex karakter ise küçük harf olarak döndürür; aksi halde null.
        /// </summary>
        public static string ParseSha256File(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            string[] tokens = content.Split(
                new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return null;

            // Bazı araçlar satırı "\" ile başlatır (escape modu) — ilk token yine hash'tir.
            string first = tokens[0].TrimStart('\\');
            return Sha256HexRegex.IsMatch(first) ? first.ToLowerInvariant() : null;
        }

        /// <summary>
        /// Release varlıkları içinde "&lt;installerName&gt;.sha256" dosyasını bulup indirir ve parse eder.
        /// Eksik, erişilemez veya bozuksa null döner (uyarı loglanır).
        /// </summary>
        private static async Task<string> TryFetchChecksumAsync(JArray assets, string installerName, CancellationToken ct)
        {
            if (assets == null || string.IsNullOrEmpty(installerName)) return null;

            string checksumName = installerName + ChecksumSuffix;
            JToken checksumAsset = assets.FirstOrDefault(a =>
                string.Equals(a["name"]?.ToString(), checksumName, StringComparison.OrdinalIgnoreCase));

            if (checksumAsset == null)
            {
                Log.Warning("Release'de checksum varlığı bulunamadı: {Name}. Kurulum doğrulanamayacak.", checksumName);
                return null;
            }

            string url = checksumAsset["browser_download_url"]?.ToString();
            if (!IsAllowedDownloadUrl(url))
            {
                Log.Warning("Checksum varlığı URL'i güvenilir değil: {Url}", url);
                return null;
            }

            try
            {
                using HttpResponseMessage response = await Http.GetAsync(url, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                // Checksum dosyası küçüktür; beklenmedik büyüklük = şüpheli içerik.
                long? length = response.Content.Headers.ContentLength;
                if (length.HasValue && length.Value > 4096)
                {
                    Log.Warning("Checksum varlığı beklenmedik büyüklükte ({Size} byte): {Name}", length.Value, checksumName);
                    return null;
                }

                string content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                string sha = ParseSha256File(content);
                if (sha == null)
                    Log.Warning("Checksum varlığı parse edilemedi: {Name}", checksumName);

                return sha;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is IOException)
            {
                Log.Warning(ex, "Checksum varlığı indirilemedi: {Name}", checksumName);
                return null;
            }
        }

        /// <summary>Çalışan assembly'nin versiyonunu döndürür.</summary>
        private static Version GetCurrentVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
        }
    }
}
