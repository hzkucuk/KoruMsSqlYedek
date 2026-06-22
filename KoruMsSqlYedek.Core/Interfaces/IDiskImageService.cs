using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Disk imajı yedekleme servisi.
    /// wbadmin.exe veya benzeri bir araç aracılığıyla sürücü/bölüm düzeyinde
    /// WIM/VHD imajı oluşturur.
    /// </summary>
    public interface IDiskImageService
    {
        /// <summary>
        /// Belirtilen plan için yapılandırılmış tüm disk imajı kaynaklarını yedekler.
        /// </summary>
        /// <param name="plan">Yedekleme planı (DiskImageBackup config içermeli).</param>
        /// <param name="progress">İlerleme bildirimi (0-100).</param>
        /// <param name="cancellationToken">İptal jetonu.</param>
        /// <returns>Her kaynak için ayrı <see cref="DiskImageResult"/> listesi.</returns>
        Task<List<DiskImageResult>> BackupDiskImagesAsync(
            BackupPlan plan,
            IProgress<int> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tek bir sürücü/bölümün disk imajını oluşturur.
        /// </summary>
        /// <param name="source">Yedeklenecek kaynak sürücü.</param>
        /// <param name="destinationPath">İmaj dosyasının yazılacağı dizin.</param>
        /// <param name="format">İmaj formatı.</param>
        /// <param name="progress">İlerleme bildirimi (0-100).</param>
        /// <param name="cancellationToken">İptal jetonu.</param>
        Task<DiskImageResult> BackupVolumeAsync(
            DiskImageSource source,
            string destinationPath,
            DiskImageFormat format,
            IProgress<int> progress,
            CancellationToken cancellationToken);
    }
}
