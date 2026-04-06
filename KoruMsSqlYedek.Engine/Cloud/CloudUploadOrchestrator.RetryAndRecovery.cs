using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    public partial class CloudUploadOrchestrator
    {
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
