using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// UNC ağ paylaşımı provider'ı.
    /// UNC kimlik bilgisi, buffered kopyalama, ilerleme raporlama ve boyut doğrulama destekler.
    /// </summary>
    public class LocalNetworkProvider : ICloudProvider
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<LocalNetworkProvider>();
        private readonly CloudProviderType _type;

        /// <summary>Buffered kopyalama için tampon boyutu (80 KB).</summary>
        private const int BufferSize = 81920;

        public LocalNetworkProvider(CloudProviderType type)
        {
            _type = type;
        }

        public CloudProviderType ProviderType => _type;
        public string DisplayName => "Ağ Paylaşımı (UNC)";

        public bool SupportsTrash => false;

        /// <summary>UNC ağ paylaşımı çöp kutusu desteklemez — no-op.</summary>
        public Task<int> EmptyTrashAsync(CloudTargetConfig config, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public async Task<CloudUploadResult> UploadAsync(
            string localFilePath,
            string remoteFileName,
            CloudTargetConfig config,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            string resumeSessionUri = null,
            Action<string> sessionUriObtained = null)
        {
            var result = new CloudUploadResult
            {
                ProviderType = _type,
                DisplayName = DisplayName
            };

            try
            {
                ValidateConfig(config);

                if (!File.Exists(localFilePath))
                    throw new FileNotFoundException("Kaynak dosya bulunamadı.", localFilePath);

                string destDir = config.LocalOrUncPath;
                string destPath = Path.Combine(destDir, remoteFileName);

                using (CreateUncConnectionIfNeeded(config))
                {
                    Directory.CreateDirectory(destDir);

                    await CopyWithProgressAsync(localFilePath, destPath, progress, cancellationToken)
                        .ConfigureAwait(false);

                    VerifyFileSizes(localFilePath, destPath);
                }

                result.IsSuccess = true;
                result.RemoteFilePath = destPath;
                result.UploadedAt = DateTime.UtcNow;

                Log.Information("Yerel kopyalama başarılı: {Source} → {Dest} ({Size:N0} bytes)",
                    localFilePath, destPath, new FileInfo(destPath).Length);
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "İşlem kullanıcı tarafından iptal edildi.";
                Log.Warning("Yerel kopyalama iptal edildi: {Source}", localFilePath);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Log.Error(ex, "Yerel kopyalama başarısız: {Source}", localFilePath);
            }

            return result;
        }

        public async Task<bool> DeleteAsync(
            string remoteFileIdentifier,
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            try
            {
                using (CreateUncConnectionIfNeeded(config))
                {
                    await Task.Run(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (File.Exists(remoteFileIdentifier))
                        {
                            File.Delete(remoteFileIdentifier);
                            Log.Information("Dosya silindi: {Path}", remoteFileIdentifier);
                        }
                        else
                        {
                            Log.Debug("Silinecek dosya bulunamadı: {Path}", remoteFileIdentifier);
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Silme işlemi iptal edildi: {Path}", remoteFileIdentifier);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dosya silinemedi: {Path}", remoteFileIdentifier);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string path = config?.LocalOrUncPath;
                    if (string.IsNullOrEmpty(path))
                    {
                        Log.Warning("Bağlantı testi başarısız: Hedef dizin belirtilmemiş.");
                        return false;
                    }

                    using (CreateUncConnectionIfNeeded(config))
                    {
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        // Yazma izni kontrolü: geçici dosya oluştur ve sil
                        string testFile = Path.Combine(path, $".write_test_{Guid.NewGuid():N}.tmp");
                        try
                        {
                            File.WriteAllText(testFile, "test");
                        }
                        finally
                        {
                            if (File.Exists(testFile))
                                File.Delete(testFile);
                        }

                        Log.Information("Bağlantı testi başarılı: {Path}", path);
                        return true;
                    }
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Bağlantı testi başarısız: {Path}", config?.LocalOrUncPath);
                    return false;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        #region Private Helpers

        /// <summary>
        /// UNC yolu ve kimlik bilgileri varsa bağlantı oluşturur, yoksa null döner.
        /// Dönen nesne IDisposable olduğu için using ile kullanılır.
        /// </summary>
        private UncNetworkConnection CreateUncConnectionIfNeeded(CloudTargetConfig config)
        {
            if (_type != CloudProviderType.UncPath)
                return null;

            if (string.IsNullOrEmpty(config.Username))
                return null;

            return new UncNetworkConnection(config.LocalOrUncPath, config.Username, config.Password);
        }

        /// <summary>
        /// Dosyayı buffered olarak kopyalar ve ilerleme yüzdesi raporlar.
        /// </summary>
        private static async Task CopyWithProgressAsync(
            string sourcePath,
            string destPath,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            var sourceInfo = new FileInfo(sourcePath);
            long totalBytes = sourceInfo.Length;
            long copiedBytes = 0;
            int lastReportedPercent = -1;

            using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
            using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                byte[] buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                    .ConfigureAwait(false)) > 0)
                {
                    await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken)
                        .ConfigureAwait(false);

                    copiedBytes += bytesRead;

                    if (totalBytes > 0 && progress != null)
                    {
                        int percent = (int)(copiedBytes * 100 / totalBytes);
                        if (percent != lastReportedPercent)
                        {
                            lastReportedPercent = percent;
                            progress.Report(percent);
                        }
                    }
                }
            }

            // Son olarak %100 garanti et
            progress?.Report(100);
        }

        /// <summary>
        /// Kaynak ve hedef dosya boyutlarını karşılaştırır.
        /// </summary>
        private static void VerifyFileSizes(string sourcePath, string destPath)
        {
            var sourceSize = new FileInfo(sourcePath).Length;
            var destSize = new FileInfo(destPath).Length;

            if (sourceSize != destSize)
            {
                throw new IOException(
                    $"Dosya boyutu doğrulama başarısız. Kaynak: {sourceSize:N0} bytes, Hedef: {destSize:N0} bytes.");
            }

            Log.Debug("Dosya boyutu doğrulandı: {Size:N0} bytes", sourceSize);
        }

        /// <summary>
        /// Yapılandırmayı doğrular.
        /// </summary>
        private static void ValidateConfig(CloudTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(config.LocalOrUncPath))
                throw new ArgumentException("Hedef dizin (LocalOrUncPath) belirtilmemiş.");
        }

        #endregion
    }
}
