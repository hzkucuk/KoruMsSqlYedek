using System;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Bulut depolama provider soyutlaması.
    /// Her provider bu arayüzü uygular.
    /// </summary>
    public interface ICloudProvider
    {
        /// <summary>Provider türü.</summary>
        CloudProviderType ProviderType { get; }

        /// <summary>Provider görünen adı.</summary>
        string DisplayName { get; }

        /// <summary>
        /// Dosyayı bulut hedefine yükler.
        /// Retry politikası (3 deneme, exponential backoff) implementasyon tarafından uygulanır.
        /// </summary>
        /// <param name="resumeSessionUri">Daha önce kesilmiş upload'ın session URI'si. Sağlanırsa sıfırdan başlamak yerine kaldığı yerden devam edilir.</param>
        /// <param name="sessionUriObtained">Session URI alındığında (transfer başlamadan önce) çağrılan callback. Kilitlenme güvenliği için URI bu noktada diske kaydedilmelidir.</param>
        Task<CloudUploadResult> UploadAsync(
            string localFilePath,
            string remoteFileName,
            CloudTargetConfig config,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            string resumeSessionUri = null,
            Action<string> sessionUriObtained = null);

        /// <summary>
        /// Uzak dosyayı siler. Google Drive ve OneDrive için çöp kutusundan da temizler.
        /// </summary>
        Task<bool> DeleteAsync(
            string remoteFileIdentifier,
            CloudTargetConfig config,
            CancellationToken cancellationToken);

        /// <summary>
        /// Bağlantıyı test eder.
        /// </summary>
        Task<bool> TestConnectionAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken);
    }
}
