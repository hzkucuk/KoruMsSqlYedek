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
        /// </summary>
        Task<List<CloudUploadResult>> UploadToAllAsync(
            string localFilePath,
            string remoteFileName,
            List<CloudTargetConfig> targets,
            IProgress<int> progress,
            CancellationToken cancellationToken);

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
