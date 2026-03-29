using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
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

        private readonly Dictionary<CloudProviderType, ICloudProvider> _providers;
        private readonly ICloudProviderFactory _factory;

        /// <summary>
        /// Doğrudan provider listesi ile oluşturur (geriye uyumluluk).
        /// </summary>
        public CloudUploadOrchestrator(IEnumerable<ICloudProvider> providers)
        {
            _providers = providers.ToDictionary(p => p.ProviderType);
            _factory = null;
        }

        /// <summary>
        /// Factory pattern ile oluşturur. Provider'lar ihtiyaç anında yaratılır.
        /// </summary>
        public CloudUploadOrchestrator(ICloudProviderFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _providers = new Dictionary<CloudProviderType, ICloudProvider>();
        }

        /// <summary>
        /// Dosyayı tüm aktif bulut hedeflerine yükler.
        /// </summary>
        public async Task<List<CloudUploadResult>> UploadToAllAsync(
            string localFilePath,
            string remoteFileName,
            List<CloudTargetConfig> targets,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            var results = new List<CloudUploadResult>();
            var enabledTargets = targets.Where(t => t.IsEnabled).ToList();
            int completed = 0;

            foreach (var target in enabledTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await UploadWithRetryAsync(
                    localFilePath, remoteFileName, target, progress, cancellationToken);
                results.Add(result);

                completed++;
                if (enabledTargets.Count > 1)
                {
                    Log.Debug("Upload ilerleme: {Completed}/{Total} hedef tamamlandı",
                        completed, enabledTargets.Count);
                }
            }

            return results;
        }

        /// <summary>
        /// Tüm aktif bulut hedeflerinden dosyayı siler.
        /// </summary>
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
            CancellationToken cancellationToken)
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
                    var result = await provider.UploadAsync(
                        localFilePath, remoteFileName, target, progress, cancellationToken);

                    result.RetryCount = attempt;
                    if (result.IsSuccess)
                    {
                        Log.Information(
                            "Bulut upload başarılı: {Provider} ({Attempt}. deneme)",
                            target.DisplayName, attempt + 1);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        ex,
                        "Bulut upload deneme {Attempt}/{Max} başarısız: {Provider}",
                        attempt + 1, MaxRetries + 1, target.DisplayName);

                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(RetryDelaysMs[attempt], cancellationToken);
                    }
                    else
                    {
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

        #endregion
    }
}
