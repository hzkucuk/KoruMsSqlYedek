using System;
using System.Threading;
using System.Threading.Tasks;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// GitHub Releases üzerinden otomatik güncelleme kontrolü.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// GitHub'dan en son sürümü kontrol eder.
        /// Yeni sürüm varsa bilgileri döner, yoksa null.
        /// </summary>
        Task<UpdateInfo> CheckForUpdateAsync(CancellationToken ct = default);

        /// <summary>
        /// Installer dosyasını belirtilen dizine indirir.
        /// </summary>
        /// <param name="downloadUrl">Installer asset URL'i.</param>
        /// <param name="destinationPath">Hedef dosya yolu.</param>
        /// <param name="progress">İndirme yüzdesi (0-100).</param>
        /// <param name="ct">İptal token'ı.</param>
        Task DownloadInstallerAsync(
            string downloadUrl,
            string destinationPath,
            IProgress<int> progress = null,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Güncelleme bilgisi — GitHub Release'den dönen veriler.
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>Yeni sürüm numarası (ör. "0.63.0").</summary>
        public string Version { get; set; }

        /// <summary>Release başlığı.</summary>
        public string Title { get; set; }

        /// <summary>Release açıklaması (markdown).</summary>
        public string ReleaseNotes { get; set; }

        /// <summary>Installer dosyası indirme URL'i.</summary>
        public string DownloadUrl { get; set; }

        /// <summary>Installer dosya boyutu (byte).</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>Release yayınlanma tarihi.</summary>
        public DateTime PublishedAt { get; set; }

        /// <summary>Release sayfası URL'i.</summary>
        public string HtmlUrl { get; set; }
    }
}
