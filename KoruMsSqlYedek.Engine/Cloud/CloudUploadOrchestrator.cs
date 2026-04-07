using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// Bulut upload orkestratörü.
    /// Retry politikası (3 deneme, exponential backoff) uygular.
    /// Provider factory veya doğrudan provider listesi ile çalışır.
    /// </summary>
    public partial class CloudUploadOrchestrator : ICloudUploadOrchestrator
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<CloudUploadOrchestrator>();
        private const int MaxRetries = 3;
        private static readonly int[] RetryDelaysMs = { 2000, 4000, 8000 };

        /// <summary>Recovery'de aynı dosya için maksimum toplam deneme sayısı. Aşılırsa vazgeçilir.</summary>
        private const int MaxRecoveryAttempts = 10;

        private readonly Dictionary<CloudProviderType, ICloudProvider> _providers;
        private readonly ICloudProviderFactory _factory;
        private readonly UploadStateManager _stateManager;

        /// <summary>
        /// Doğrudan provider listesi ile oluşturur (geriye uyumluluk).
        /// </summary>
        public CloudUploadOrchestrator(IEnumerable<ICloudProvider> providers)
        {
            _providers = providers.ToDictionary(p => p.ProviderType);
            _factory = null;
            _stateManager = new UploadStateManager();
        }

        /// <summary>
        /// Factory pattern ile oluşturur. Provider'lar ihtiyaç anında yaratılır.
        /// </summary>
        public CloudUploadOrchestrator(ICloudProviderFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _providers = new Dictionary<CloudProviderType, ICloudProvider>();
            _stateManager = new UploadStateManager();
        }

        /// <summary>
        /// Dosyayı tüm aktif bulut hedeflerine yükler.
        /// Her hedef için BackupActivityHub üzerinden ilerleme eventi fırlatır.
        /// RemoteFolderPath boşsa otomatik olarak "KoruMsSqlYedek/{planName}" kullanılır.
        /// </summary>
        public async Task<List<CloudUploadResult>> UploadToAllAsync(
            string localFilePath,
            string remoteFileName,
            List<CloudTargetConfig> targets,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            string planName = null,
            string planId = null)
        {
            var results = new List<CloudUploadResult>();
            var enabledTargets = targets.Where(t => t.IsEnabled).ToList();
            if (enabledTargets.Count == 0) return results;

            // SHA-256 ve dosya boyutunu bir kez hesapla (tüm hedefler için ortak)
            string localSha256 = null;
            long fileSizeBytes = 0;
            try
            {
                fileSizeBytes = new FileInfo(localFilePath).Length;
                localSha256 = UploadStateManager.ComputeSha256(localFilePath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SHA-256 hesaplanamadı: {File}", localFilePath);
            }

            // Bekleyen state kayıtlarını bir kez yükle
            var pendingStates = _stateManager.GetAll();

            int completed = 0;

            foreach (var target in enabledTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var effectiveTarget = target;
                if (string.IsNullOrWhiteSpace(target.RemoteFolderPath))
                {
                    string safePlanName = SanitizeFolderName(planName);
                    effectiveTarget = ShallowCopyWithFolder(target,
                        string.IsNullOrEmpty(safePlanName)
                            ? "KoruMsSqlYedek"
                            : $"KoruMsSqlYedek/{safePlanName}");

                    Log.Information(
                        "RemoteFolderPath boş, varsayılan kullanılıyor: '{Folder}' — {Provider}",
                        effectiveTarget.RemoteFolderPath, target.DisplayName);
                }

                // Bu hedef için mevcut state yüklenir veya yeni oluşturulur
                var stateRecord = pendingStates.FirstOrDefault(s =>
                    s.LocalFilePath == localFilePath &&
                    s.RemoteFileName == remoteFileName &&
                    s.ProviderType == target.Type);

                if (stateRecord == null)
                {
                    stateRecord = new UploadStateRecord
                    {
                        PlanName = planName,
                        LocalFilePath = localFilePath,
                        RemoteFileName = remoteFileName,
                        LocalSha256 = localSha256,
                        FileSizeBytes = fileSizeBytes,
                        ProviderType = target.Type,
                        CloudTarget = effectiveTarget
                    };
                    _stateManager.Save(stateRecord);
                }

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = planId,
                    PlanName = planName,
                    ActivityType = BackupActivityType.CloudUploadStarted,
                    CloudTargetName = effectiveTarget.DisplayName,
                    CloudTargetIndex = completed + 1,
                    CloudTargetTotal = enabledTargets.Count
                });

                var uploadStartTime = DateTime.UtcNow;
                int lastReportedPct = -1;
                var hubProgress = new Progress<int>(pct =>
                {
                    // Progress<T> callback'leri ThreadPool'a post edilir —
                    // sıra garantisi yoktur. Geriye giden yüzde değerlerini atla.
                    if (pct <= lastReportedPct) return;
                    lastReportedPct = pct;

                    long bytesSent = stateRecord.FileSizeBytes > 0
                        ? (long)(stateRecord.FileSizeBytes * pct / 100.0)
                        : 0L;
                    double elapsedSec = (DateTime.UtcNow - uploadStartTime).TotalSeconds;
                    long speedBps = elapsedSec > 0.5 && bytesSent > 0
                        ? (long)(bytesSent / elapsedSec)
                        : 0L;

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = planId,
                        PlanName = planName,
                        ActivityType = BackupActivityType.CloudUploadProgress,
                        CloudTargetName = effectiveTarget.DisplayName,
                        CloudTargetIndex = completed + 1,
                        CloudTargetTotal = enabledTargets.Count,
                        ProgressPercent = pct,
                        BytesSent = bytesSent,
                        BytesTotal = stateRecord.FileSizeBytes,
                        SpeedBytesPerSecond = speedBps
                    });
                    progress?.Report(pct);
                });

                var result = await UploadWithRetryAsync(
                    localFilePath, remoteFileName, effectiveTarget, hubProgress, cancellationToken, stateRecord);
                results.Add(result);

                completed++;

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = planId,
                    PlanName = planName,
                    ActivityType = BackupActivityType.CloudUploadCompleted,
                    CloudTargetName = effectiveTarget.DisplayName,
                    CloudTargetIndex = completed,
                    CloudTargetTotal = enabledTargets.Count,
                    IsSuccess = result.IsSuccess,
                    Message = result.IsSuccess ? null : result.ErrorMessage
                });

                Log.Debug("Upload ilerleme: {Completed}/{Total} hedef tamamlandı — {Target}",
                    completed, enabledTargets.Count, effectiveTarget.DisplayName);
            }

            return results;
        }

        /// <summary>
        /// Klasör adında geçersiz karakter içeren karakterleri temizler.
        /// </summary>
        private static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Birden fazla dosyayı toplu olarak tüm aktif bulut hedeflerine yükler.
        /// Tüm dosyalar sırayla yüklenir, ilerleme toplam batch boyutu üzerinden hesaplanır.
        /// </summary>
        public async Task<List<List<CloudUploadResult>>> UploadBatchToAllAsync(
            List<(string LocalFilePath, string RemoteFileName)> files,
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken,
            string planName = null,
            string planId = null)
        {
            var allResults = new List<List<CloudUploadResult>>();
            if (files == null || files.Count == 0) return allResults;

            var enabledTargets = targets.Where(t => t.IsEnabled).ToList();
            if (enabledTargets.Count == 0)
            {
                // Her dosya için boş liste döndür
                foreach (var _ in files) allResults.Add(new List<CloudUploadResult>());
                return allResults;
            }

            // Toplam batch boyutunu hesapla
            var fileSizes = new long[files.Count];
            long totalBatchBytes = 0;
            for (int i = 0; i < files.Count; i++)
            {
                try { fileSizes[i] = new FileInfo(files[i].LocalFilePath).Length; }
                catch { fileSizes[i] = 0; }
                totalBatchBytes += fileSizes[i];
            }

            long completedBytes = 0;
            var uploadStartTime = DateTime.UtcNow;

            for (int fileIdx = 0; fileIdx < files.Count; fileIdx++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (localFilePath, remoteFileName) = files[fileIdx];
                long currentFileSize = fileSizes[fileIdx];
                string localSha256 = null;
                try { localSha256 = UploadStateManager.ComputeSha256(localFilePath); }
                catch (Exception ex) { Log.Warning(ex, "SHA-256 hesaplanamadı: {File}", localFilePath); }

                var pendingStates = _stateManager.GetAll();
                var fileResults = new List<CloudUploadResult>();
                int targetCompleted = 0;

                // Dosya başlangıcı eventi
                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = planId,
                    PlanName = planName,
                    ActivityType = BackupActivityType.CloudUploadStarted,
                    CloudTargetName = remoteFileName,
                    CloudFileIndex = fileIdx + 1,
                    CloudFileTotal = files.Count,
                    CloudFileName = remoteFileName,
                    CloudTargetIndex = 1,
                    CloudTargetTotal = enabledTargets.Count
                });

                foreach (var target in enabledTargets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var effectiveTarget = target;
                    if (string.IsNullOrWhiteSpace(target.RemoteFolderPath))
                    {
                        string safePlanName = SanitizeFolderName(planName);
                        effectiveTarget = ShallowCopyWithFolder(target,
                            string.IsNullOrEmpty(safePlanName)
                                ? "KoruMsSqlYedek"
                                : $"KoruMsSqlYedek/{safePlanName}");
                    }

                    var stateRecord = pendingStates.FirstOrDefault(s =>
                        s.LocalFilePath == localFilePath &&
                        s.RemoteFileName == remoteFileName &&
                        s.ProviderType == target.Type);

                    if (stateRecord == null)
                    {
                        stateRecord = new UploadStateRecord
                        {
                            PlanName = planName,
                            LocalFilePath = localFilePath,
                            RemoteFileName = remoteFileName,
                            LocalSha256 = localSha256,
                            FileSizeBytes = currentFileSize,
                            ProviderType = target.Type,
                            CloudTarget = effectiveTarget
                        };
                        _stateManager.Save(stateRecord);
                    }

                    int lastReportedPct = -1;
                    long capturedCompletedBytes = completedBytes;
                    int capturedFileIdx = fileIdx;
                    int capturedTargetCompleted = targetCompleted;

                    var hubProgress = new Progress<int>(pct =>
                    {
                        if (pct <= lastReportedPct) return;
                        lastReportedPct = pct;

                        // Hedef bazlı dosya ilerlemesi: (targetCompleted * 100 + pct) / targetTotal
                        double fileProgress = (capturedTargetCompleted * 100.0 + pct) / enabledTargets.Count;
                        long fileBytesSent = (long)(currentFileSize * fileProgress / 100.0);

                        // Toplam batch ilerlemesi
                        long totalSent = capturedCompletedBytes + fileBytesSent;
                        int batchPct = totalBatchBytes > 0
                            ? (int)(totalSent * 100.0 / totalBatchBytes)
                            : pct;
                        batchPct = Math.Clamp(batchPct, 0, 100);

                        double elapsedSec = (DateTime.UtcNow - uploadStartTime).TotalSeconds;
                        long speedBps = elapsedSec > 0.5 && totalSent > 0
                            ? (long)(totalSent / elapsedSec)
                            : 0L;

                        BackupActivityHub.Raise(new BackupActivityEventArgs
                        {
                            PlanId = planId,
                            PlanName = planName,
                            ActivityType = BackupActivityType.CloudUploadProgress,
                            CloudTargetName = effectiveTarget.DisplayName,
                            CloudFileName = remoteFileName,
                            CloudFileIndex = capturedFileIdx + 1,
                            CloudFileTotal = files.Count,
                            CloudTargetIndex = capturedTargetCompleted + 1,
                            CloudTargetTotal = enabledTargets.Count,
                            ProgressPercent = batchPct,
                            BytesSent = totalSent,
                            BytesTotal = totalBatchBytes,
                            SpeedBytesPerSecond = speedBps
                        });
                    });

                    var result = await UploadWithRetryAsync(
                        localFilePath, remoteFileName, effectiveTarget, hubProgress,
                        cancellationToken, stateRecord);
                    fileResults.Add(result);

                    targetCompleted++;

                    // Hedef tamamlandıktan sonra batch progress güncelle — sonraki hedefe kadar "duraklama" önlenir
                    {
                        double fileProgress = (targetCompleted * 100.0) / enabledTargets.Count;
                        long fileBytesSent = (long)(currentFileSize * fileProgress / 100.0);
                        long totalSent = capturedCompletedBytes + fileBytesSent;
                        int batchPct = totalBatchBytes > 0
                            ? (int)(totalSent * 100.0 / totalBatchBytes)
                            : 100;
                        batchPct = Math.Clamp(batchPct, 0, 100);

                        double elapsedSec = (DateTime.UtcNow - uploadStartTime).TotalSeconds;
                        long speedBps = elapsedSec > 0.5 && totalSent > 0
                            ? (long)(totalSent / elapsedSec)
                            : 0L;

                        BackupActivityHub.Raise(new BackupActivityEventArgs
                        {
                            PlanId = planId,
                            PlanName = planName,
                            ActivityType = BackupActivityType.CloudUploadProgress,
                            CloudTargetName = effectiveTarget.DisplayName,
                            CloudFileName = remoteFileName,
                            CloudFileIndex = fileIdx + 1,
                            CloudFileTotal = files.Count,
                            CloudTargetIndex = targetCompleted,
                            CloudTargetTotal = enabledTargets.Count,
                            ProgressPercent = batchPct,
                            BytesSent = totalSent,
                            BytesTotal = totalBatchBytes,
                            SpeedBytesPerSecond = speedBps
                        });
                    }

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = planId,
                        PlanName = planName,
                        ActivityType = BackupActivityType.CloudUploadCompleted,
                        CloudTargetName = effectiveTarget.DisplayName,
                        CloudFileName = remoteFileName,
                        CloudFileIndex = fileIdx + 1,
                        CloudFileTotal = files.Count,
                        CloudTargetIndex = targetCompleted,
                        CloudTargetTotal = enabledTargets.Count,
                        IsSuccess = result.IsSuccess,
                        Message = result.IsSuccess ? null : result.ErrorMessage
                    });
                }

                allResults.Add(fileResults);
                completedBytes += currentFileSize;
            }

            return allResults;
        }

        /// <summary>
        /// Sadece RemoteFolderPath değiştirilmiş sığ kopya döndürür (orijinal config'i değiştirmez).
        /// </summary>
        private static CloudTargetConfig ShallowCopyWithFolder(CloudTargetConfig src, string folderPath)
        {
            return new CloudTargetConfig
            {
                Type = src.Type,
                IsEnabled = src.IsEnabled,
                DisplayName = src.DisplayName,
                RemoteFolderPath = folderPath,
                Host = src.Host,
                Port = src.Port,
                Username = src.Username,
                Password = src.Password,
                OAuthClientId = src.OAuthClientId,
                OAuthClientSecret = src.OAuthClientSecret,
                OAuthTokenJson = src.OAuthTokenJson,
                LocalOrUncPath = src.LocalOrUncPath,
                BandwidthLimitMbps = src.BandwidthLimitMbps,
                PermanentDeleteFromTrash = src.PermanentDeleteFromTrash,
                FtpsSkipCertificateValidation = src.FtpsSkipCertificateValidation,
                SftpHostFingerprint = src.SftpHostFingerprint
            };
        }
    }
}
