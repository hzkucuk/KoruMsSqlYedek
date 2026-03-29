using System;
using System.Threading;
using System.Threading.Tasks;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Sıkıştırma ve açma işlemlerini yönetir.
    /// </summary>
    public interface ICompressionService
    {
        /// <summary>
        /// Dosyayı belirtilen algoritma ve şifre ile sıkıştırır.
        /// </summary>
        /// <param name="sourceFilePath">Sıkıştırılacak dosya yolu (.bak).</param>
        /// <param name="destinationArchivePath">Hedef arşiv yolu (.7z).</param>
        /// <param name="password">Arşiv şifresi (null ise şifresiz).</param>
        /// <param name="progress">Sıkıştırma ilerleme yüzdesi.</param>
        /// <param name="cancellationToken">İptal token'ı.</param>
        /// <returns>Sıkıştırılmış dosya boyutu (byte).</returns>
        Task<long> CompressAsync(
            string sourceFilePath,
            string destinationArchivePath,
            string password,
            IProgress<int> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Arşivden dosyayı çıkarır.
        /// </summary>
        Task ExtractAsync(
            string archivePath,
            string destinationDirectory,
            string password,
            IProgress<int> progress,
            CancellationToken cancellationToken);
    }
}
