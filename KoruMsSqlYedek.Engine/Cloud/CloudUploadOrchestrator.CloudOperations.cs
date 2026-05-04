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
    public partial class CloudUploadOrchestrator
    {
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
        /// Çöp kutusu destekleyen tüm aktif bulut hedeflerinin çöp kutusunu temizler.
        /// TrashRetentionDays > 0 olan hedeflerde saklama süresi dolan dosyalar kalıcı silinir.
        /// </summary>
        public async Task<int> EmptyTrashForAllAsync(
            List<CloudTargetConfig> targets,
            CancellationToken cancellationToken)
        {
            int totalDeleted = 0;

            // Çöp kutusu kullanan hedefleri filtrele (TrashRetentionDays > 0)
            var trashTargets = targets
                .Where(t => t.IsEnabled && t.UsesTrash)
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

        /// <summary>
        /// Tek bir hedeften uzak dosyayı siler. Folder sweep için kullanılır.
        /// </summary>
        public async Task<bool> DeleteFromTargetAsync(
            string remoteFileIdentifier,
            CloudTargetConfig target,
            CancellationToken cancellationToken)
        {
            if (target is null || !target.IsEnabled)
                return false;

            try
            {
                var provider = GetProvider(target.Type);
                if (provider is null)
                {
                    Log.Warning("DeleteFromTarget: Provider bulunamadı: {Type}", target.Type);
                    return false;
                }

                bool ok = await provider.DeleteAsync(remoteFileIdentifier, target, cancellationToken)
                    .ConfigureAwait(false);

                if (ok)
                    Log.Information("Bulut dosya silindi: {Provider} — {FileId}",
                        target.DisplayName, remoteFileIdentifier);

                return ok;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulut silme hatası: {Provider} — {FileId}",
                    target.DisplayName, remoteFileIdentifier);
                return false;
            }
        }

        /// <summary>
        /// Belirtilen hedefin uzak klasörünü listeler.
        /// Provider <see cref="ICloudFolderListProvider"/> uygulamıyorsa null döner.
        /// </summary>
        public async Task<List<CloudFileEntry>> ListFolderAsync(
            CloudTargetConfig target,
            CancellationToken cancellationToken)
        {
            if (target is null || !target.IsEnabled)
                return null;

            try
            {
                var provider = GetProvider(target.Type);
                if (provider is null)
                    return null;

                if (provider is ICloudFolderListProvider folderProvider)
                {
                    return await folderProvider.ListFolderAsync(target, cancellationToken)
                        .ConfigureAwait(false);
                }

                // Provider folder list desteklemiyor
                return null;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Warning(ex, "Bulut klasör listeleme başarısız: {Provider}", target.DisplayName);
                return null;
            }
        }
    }
}
