using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Bulut upload orkestratörü arayüzü.
    /// Dosyayı tüm aktif bulut hedeflerine retry politikası ile yükler.
    /// </summary>
    public interface ICloudUploadOrchestrator
    {
        /// <summary>
        /// Dosyayı tüm aktif bulut hedeflerine yükler.
        /// Retry: 3 deneme, exponential backoff (2s → 4s → 8s).
        /// RemoteFolderPath boşsa otomatik olarak "KoruMsSqlYedek/{planName}" klasörü kullanılır.
        /// </summary>
        /// <param name="planId">İlerleme olaylarına eklenen plan kimliği (null olabilir).</param>
        Task<List<CloudUploadResult>> UploadToAllAsync(
            string localFilePath,
            string remoteFileName,
            List<CloudTargetConfig> targets,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            string planName = null,
            string planId = null);

        /// <summary>
        /// Tüm aktif bulut hedeflerinden uzak dosyayı siler.
        /// Retention temizliği sırasında kullanılır.
        /// </summary>
        Task<List<CloudDeleteResult>> DeleteFromAllAsync(
            string remoteFileIdentifier,
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tüm aktif bulut hedeflerinin bağlantısını test eder.
        /// </summary>
        Task<List<CloudConnectionTestResult>> TestAllConnectionsAsync(
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken);

        /// <summary>
        /// Uygulama başlangıcında yarıda kalan upload işlemlerini kaldığı yerden sürdürür.
        /// %APPDATA%\KoruMsSqlYedek\UploadState\ altındaki state dosyalarını okur.
        /// </summary>
        /// <returns>Başarıyla tamamlanan recovery sayısı.</returns>
        Task<int> RecoverPendingUploadsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Çöp kutusu destekleyen tüm aktif bulut hedeflerinin çöp kutusunu boşaltır.
        /// Retention sonrası PermanentDeleteFromTrash=false olan hedefler için çağrılır.
        /// </summary>
        /// <returns>Toplam silinen öğe sayısı.</returns>
        Task<int> EmptyTrashForAllAsync(
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken);

        /// <summary>
        /// Birden fazla dosyayı toplu olarak tüm aktif bulut hedeflerine yükler.
        /// Her dosya sırayla tüm hedeflere gönderilir; ilerleme toplam batch üzerinden hesaplanır.
        /// </summary>
        /// <param name="files">Yüklenecek dosya listesi (yerel yol, uzak dosya adı).</param>
        /// <param name="targets">Aktif bulut hedefleri.</param>
        /// <param name="cancellationToken">İptal jetonu.</param>
        /// <param name="planName">Plan adı (klasör oluşturmak için).</param>
        /// <param name="planId">İlerleme olaylarına eklenen plan kimliği.</param>
        /// <returns>Her dosya için ayrı upload sonuçları listesi. Dosya sırası ile eşleşir.</returns>
        Task<List<List<CloudUploadResult>>> UploadBatchToAllAsync(
            List<(string LocalFilePath, string RemoteFileName)> files,
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken,
            string planName = null,
            string planId = null);
    }

    /// <summary>
    /// Bulut silme işlemi sonucu.
    /// </summary>
    public class CloudDeleteResult
    {
        public CloudProviderType ProviderType { get; set; }
        public string DisplayName { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Bulut bağlantı testi sonucu.
    /// </summary>
    public class CloudConnectionTestResult
    {
        public CloudProviderType ProviderType { get; set; }
        public string DisplayName { get; set; }
        public bool IsConnected { get; set; }
        public string ErrorMessage { get; set; }
    }
}
