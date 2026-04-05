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
    public class CloudUploadOrchestrator : ICloudUploadOrchestrator
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
        public async Task<List<CloudDeleteResult>> DeleteFromAllAsync(
            string remoteFileIdentifier,
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken)
        {
            var results = new List<CloudDeleteResult>();

            foreach (var target in targets.Where(t => t.IsEnabled))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var deleteResult = new CloudDeleteResult
                {
                    ProviderType = target.Type,
                    DisplayName = target.DisplayName
                };

                try
                {
                    var provider = GetProvider(target.Type);
                    if (provider == null)
                    {
                        deleteResult.ErrorMessage = $"Provider bulunamadı: {target.Type}";
                        results.Add(deleteResult);
                        continue;
                    }

                    deleteResult.IsSuccess = await provider.DeleteAsync(
                        remoteFileIdentifier, target, cancellationToken)
                        .ConfigureAwait(false);

                    if (deleteResult.IsSuccess)
                    {
                        Log.Information("Bulut dosya silindi: {Provider} — {FileId}",
                            target.DisplayName, remoteFileIdentifier);
                    }
                }
                catch (Exception ex)
                {
                    deleteResult.ErrorMessage = ex.Message;
                    Log.Error(ex, "Bulut silme hatası: {Provider} — {FileId}",
                        target.DisplayName, remoteFileIdentifier);
                }

                results.Add(deleteResult);
            }

            return results;
        }

        /// <summary>
        /// Çöp kutusu destekleyen tüm aktif bulut hedeflerinin çöp kutusunu boşaltır.
        /// PermanentDeleteFromTrash=false olan hedefler için retention sonrası çağrılır.
        /// </summary>
        public async Task<int> EmptyTrashForAllAsync(
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken)
        {
            int totalDeleted = 0;

            // Sadece çöp kutusu kullanan hedefleri filtrele (PermanentDeleteFromTrash=false)
            var trashTargets = targets
                .Where(t => t.IsEnabled && !t.PermanentDeleteFromTrash)
                .ToList();

            if (trashTargets.Count == 0)
                return 0;

            foreach (var target in trashTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var provider = GetProvider(target.Type);
                    if (provider is null || !provider.SupportsTrash)
                        continue;

                    int deleted = await provider.EmptyTrashAsync(target, cancellationToken)
                        .ConfigureAwait(false);

                    totalDeleted += deleted;

                    if (deleted > 0)
                    {
                        Log.Information("Çöp kutusu boşaltıldı: {Provider} — {Count} öğe silindi",
                            target.DisplayName, deleted);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Çöp kutusu boşaltma hatası: {Provider}", target.DisplayName);
                }
            }

            return totalDeleted;
        }

        /// <summary>
        /// Tüm aktif bulut hedeflerinin bağlantısını test eder.
        /// </summary>
        public async Task<List<CloudConnectionTestResult>> TestAllConnectionsAsync(
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken)
        {
            var results = new List<CloudConnectionTestResult>();

            foreach (var target in targets.Where(t => t.IsEnabled))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var testResult = new CloudConnectionTestResult
                {
                    ProviderType = target.Type,
                    DisplayName = target.DisplayName
                };

                try
                {
                    var provider = GetProvider(target.Type);
                    if (provider == null)
                    {
                        testResult.ErrorMessage = $"Provider bulunamadı: {target.Type}";
                        results.Add(testResult);
                        continue;
                    }

                    testResult.IsConnected = await provider.TestConnectionAsync(target, cancellationToken)
                        .ConfigureAwait(false);

                    Log.Information("Bağlantı testi: {Provider} — {Status}",
                        target.DisplayName, testResult.IsConnected ? "Başarılı" : "Başarısız");
                }
                catch (Exception ex)
                {
                    testResult.ErrorMessage = ex.Message;
                    Log.Error(ex, "Bağlantı testi hatası: {Provider}", target.DisplayName);
                }

                results.Add(testResult);
            }

            return results;
        }

        #region Private Helpers

        private ICloudProvider GetProvider(CloudProviderType type)
        {
            // Önce cache'e bak
            if (_providers.TryGetValue(type, out var cached))
                return cached;

            // Factory varsa oluştur ve cache'le
            if (_factory != null && _factory.IsSupported(type))
            {
                var provider = _factory.CreateProvider(type);
                _providers[type] = provider;
                return provider;
            }

            return null;
        }

        private async Task<CloudUploadResult> UploadWithRetryAsync(
            string localFilePath,
            string remoteFileName,
            CloudTargetConfig target,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            UploadStateRecord stateRecord = null)
        {
            var provider = GetProvider(target.Type);
            if (provider == null)
            {
                return new CloudUploadResult
                {
                    ProviderType = target.Type,
                    DisplayName = target.DisplayName,
                    IsSuccess = false,
                    ErrorMessage = $"Provider bulunamadı: {target.Type}"
                };
            }

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    // İlk denemede kayıtlı session URI'yi kullan, retry'larda sıfırdan başla
                    string resumeUri = attempt == 0 ? stateRecord?.ResumeSessionUri : null;

                    // Session URI alındığında state'i güncelle (transfer başlamadan önce)
                    Action<string> sessionUriObtained = null;
                    if (stateRecord != null)
                    {
                        sessionUriObtained = uri =>
                        {
                            if (!string.IsNullOrEmpty(uri))
                                _stateManager.UpdateProgress(stateRecord.StateId, uri, 0);
                        };
                    }

                    var result = await provider.UploadAsync(
                        localFilePath, remoteFileName, target, progress, cancellationToken,
                        resumeUri, sessionUriObtained);

                    result.RetryCount = attempt;

                    if (result.IsSuccess)
                    {
                        // Bütünlük kontrolü: uzak dosya boyutu yerel ile eşleşmeli
                        if (result.RemoteFileSizeBytes > 0 && stateRecord?.FileSizeBytes > 0
                            && result.RemoteFileSizeBytes != stateRecord.FileSizeBytes)
                        {
                            Log.Warning(
                                "Bütünlük hatası! Yerel={Local:N0} bytes ≠ Uzak={Remote:N0} bytes — {Provider} — {File}",
                                stateRecord.FileSizeBytes, result.RemoteFileSizeBytes,
                                target.DisplayName, remoteFileName);

                            result.IsSuccess = false;
                            result.ErrorMessage =
                                $"Bütünlük kontrolü başarısız: yerel {stateRecord.FileSizeBytes:N0} bytes, uzak {result.RemoteFileSizeBytes:N0} bytes.";

                            if (attempt < MaxRetries)
                            {
                                // Session URI'yi temizle, dosya yeniden gönderilecek
                                if (stateRecord != null)
                                    _stateManager.UpdateProgress(stateRecord.StateId, null, 0);
                                await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
                                continue;
                            }
                        }

                        if (result.IsSuccess)
                        {
                            // Upload tamamlandı — state dosyasını temizle
                            if (stateRecord != null)
                                _stateManager.Delete(stateRecord.StateId);

                            Log.Information(
                                "Bulut upload başarılı: {Provider} ({Attempt}. deneme)",
                                target.DisplayName, attempt + 1);
                            return result;
                        }
                    }
                    else
                    {
                        // Provider hata döndürdü (exception fırlatmadan) — retry uygula
                        Log.Warning(
                            "Bulut upload deneme {Attempt}/{Max} başarısız (provider): {Provider} — {Error}",
                            attempt + 1, MaxRetries + 1, target.DisplayName, result.ErrorMessage);

                        if (attempt >= MaxRetries)
                            return result;

                        await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // State'i koru — kullanıcı iptal etti, sonra resume edilebilir
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        ex,
                        "Bulut upload deneme {Attempt}/{Max} başarısız: {Provider}",
                        attempt + 1, MaxRetries + 1, target.DisplayName);

                    if (attempt < MaxRetries)
                        await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
                    else
                        return new CloudUploadResult
                        {
                            ProviderType = target.Type,
                            DisplayName = target.DisplayName,
                            IsSuccess = false,
                            ErrorMessage = ex.Message,
                            RetryCount = attempt
                        };
                }
            }

            return new CloudUploadResult
            {
                ProviderType = target.Type,
                DisplayName = target.DisplayName,
                IsSuccess = false,
                ErrorMessage = "Tüm denemeler başarısız oldu.",
                RetryCount = MaxRetries
            };
        }

        /// <summary>
        /// Uygulama başlatmada yarıda kalan upload işlemlerini kaldığı yerden sürdürür.
        /// %APPDATA%\KoruMsSqlYedek\UploadState\ altındaki state dosyalarını okur.
        /// </summary>
        public async Task<int> RecoverPendingUploadsAsync(CancellationToken cancellationToken)
        {
            var pendingStates = _stateManager.GetAll();
            if (pendingStates.Count == 0) return 0;

            Log.Information("Bekleyen {Count} upload işlemi bulundu, devam ettiriliyor...", pendingStates.Count);
            int recovered = 0;
            var abandonedFiles = new List<(string FileName, string Provider, int Attempts, string Error)>();

            foreach (var state in pendingStates)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Maks deneme sayısı aşıldıysa vazgeç ve state'i temizle
                if (state.AttemptCount >= MaxRecoveryAttempts)
                {
                    Log.Warning(
                        "Recovery: Maks deneme aşıldı ({Attempts}/{Max}), vazgeçiliyor: {Provider} — {File}",
                        state.AttemptCount, MaxRecoveryAttempts, state.ProviderType, state.RemoteFileName);

                    abandonedFiles.Add((state.RemoteFileName, state.ProviderType.ToString(),
                        state.AttemptCount, $"Maks deneme sayısı aşıldı ({state.AttemptCount})"));

                    _stateManager.Delete(state.StateId);
                    continue;
                }

                try
                {
                    var provider = GetProvider(state.ProviderType);
                    if (provider == null)
                    {
                        Log.Warning("Recovery: provider bulunamadı {Type}, atlanıyor.", state.ProviderType);
                        continue;
                    }

                    Log.Information(
                        "Recovery: {Provider} — {File} (session: {HasSession}, deneme: {Attempt}/{Max})",
                        state.ProviderType, state.RemoteFileName,
                        !string.IsNullOrEmpty(state.ResumeSessionUri) ? "mevcut" : "yok",
                        state.AttemptCount + 1, MaxRecoveryAttempts);

                    state.AttemptCount++;
                    state.LastAttemptAt = DateTime.UtcNow;
                    _stateManager.Save(state);

                    var result = await UploadWithRetryAsync(
                        state.LocalFilePath,
                        state.RemoteFileName,
                        state.CloudTarget,
                        progress: null,
                        cancellationToken,
                        stateRecord: state).ConfigureAwait(false);

                    if (result.IsSuccess)
                    {
                        recovered++;
                        Log.Information(
                            "Recovery başarılı: {Provider} — {File}",
                            state.ProviderType, state.RemoteFileName);
                    }
                    else
                    {
                        Log.Warning(
                            "Recovery başarısız: {Provider} — {File} — {Error} (deneme: {Attempt}/{Max})",
                            state.ProviderType, state.RemoteFileName, result.ErrorMessage,
                            state.AttemptCount, MaxRecoveryAttempts);

                        // Toplam deneme aşıldıysa vazgeç
                        if (state.AttemptCount >= MaxRecoveryAttempts)
                        {
                            abandonedFiles.Add((state.RemoteFileName, state.ProviderType.ToString(),
                                state.AttemptCount, result.ErrorMessage));
                            _stateManager.Delete(state.StateId);

                            Log.Error(
                                "Recovery: Kalıcı başarısızlık, dosya terk edildi: {Provider} — {File} ({Attempts} deneme)",
                                state.ProviderType, state.RemoteFileName, state.AttemptCount);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log.Error(ex, "Recovery hatası: {Provider} — {File}",
                        state.ProviderType, state.RemoteFileName);
                }
            }

            // Terk edilen dosyalar için bildirim fırlat
            if (abandonedFiles.Count > 0)
            {
                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    ActivityType = BackupActivityType.CloudUploadAbandoned,
                    Message = $"{abandonedFiles.Count} dosyanın bulut yüklemesi kalıcı olarak başarısız oldu",
                    AbandonedFiles = abandonedFiles
                        .Select(f => $"{f.FileName} ({f.Provider}, {f.Attempts} deneme): {f.Error}")
                        .ToList()
                });
            }

            return recovered;
        }

        #endregion
    }
}
