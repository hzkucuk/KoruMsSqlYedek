using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Bildirim gönderme servisi.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Yedekleme sonucunu bildirir.
        /// Bildirim başarısızlığı yedek başarısını etkilemez; hata log'a yazılır.
        /// </summary>
        Task NotifyAsync(
            BackupResult result,
            NotificationConfig config,
            CancellationToken cancellationToken);

        /// <summary>
        /// Bulut yükleme kalıcı başarısızlığını bildirir.
        /// Maks deneme aşılıp dosya terk edildiğinde çağrılır.
        /// </summary>
        Task NotifyCloudUploadFailureAsync(
            string planName,
            List<CloudUploadResult> failedResults,
            string fileName,
            NotificationConfig config,
            CancellationToken cancellationToken);

        /// <summary>
        /// Dosya yedekleme sonuçlarını bildirir.
        /// Bulut yükleme sonuçları ve arşiv bilgisi dahil.
        /// </summary>
        Task NotifyFileBackupAsync(
            List<FileBackupResult> results,
            BackupPlan plan,
            List<CloudUploadResult> cloudUploadResults,
            string archiveFileName,
            long archiveSizeBytes,
            CancellationToken cancellationToken);
    }
}
