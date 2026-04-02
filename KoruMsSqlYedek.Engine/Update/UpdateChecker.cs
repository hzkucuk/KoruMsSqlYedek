using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
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

        private const string InstallerPrefix = "KoruMsSqlYedek_Setup_";

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

                var updateInfo = new UpdateInfo
                {
                    Version = latestVersion.ToString(3),
                    Title = release["name"]?.ToString() ?? $"v{versionStr}",
                    ReleaseNotes = release["body"]?.ToString(),
                    DownloadUrl = installerAsset["browser_download_url"]?.ToString(),
                    FileSizeBytes = installerAsset["size"]?.Value<long>() ?? 0,
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

        /// <summary>Çalışan assembly'nin versiyonunu döndürür.</summary>
        private static Version GetCurrentVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
        }
    }
}
