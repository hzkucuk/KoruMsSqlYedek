using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using Serilog;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    // ── Public API Operations + Operation Helpers ─────────────────────
    public partial class MegaProvider
    {
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
                ProviderType = CloudProviderType.Mega,
                DisplayName = DisplayName
            };

            await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ValidateConfig(config);

                if (!File.Exists(localFilePath))
                    throw new FileNotFoundException("Kaynak dosya bulunamadı.", localFilePath);

                var client = await GetOrCreateSessionAsync(config, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                Log.Debug("Mega hedef klasör kontrol ediliyor: {Path}", config.RemoteFolderPath ?? "(kök)");
                var targetFolder = await EnsureFolderExistsAsync(client, config.RemoteFolderPath, cancellationToken)
                    .ConfigureAwait(false);
                Log.Debug("Mega hedef klasör hazır: {FolderId}", targetFolder.Id);

                cancellationToken.ThrowIfCancellationRequested();

                long fileSize = new FileInfo(localFilePath).Length;

                // Progress adaptörü: MegaApiClient IProgress<double> (0.0-1.0) → IProgress<int> (0-100)
                var megaProgress = progress is not null
                    ? new Progress<double>(d => progress.Report((int)(d * 100)))
                    : null;

                Log.Debug("Mega upload başlıyor: {FileName} ({Size:N0} bytes)", remoteFileName, fileSize);

                INode uploadedNode;
                using (var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    uploadedNode = await UploadWithCancellationAsync(
                        client, stream, remoteFileName, targetFolder, megaProgress, cancellationToken)
                        .ConfigureAwait(false);
                }

                _sessionLastUsedUtc = DateTime.UtcNow;

                result.IsSuccess = true;
                result.RemoteFilePath = uploadedNode.Id;
                result.RemoteFileSizeBytes = uploadedNode.Size;
                result.UploadedAt = DateTime.UtcNow;

                Log.Information(
                    "Mega upload başarılı: {FileName} → {NodeId} ({Size:N0} bytes)",
                    remoteFileName, uploadedNode.Id, fileSize);
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "İşlem kullanıcı tarafından iptal edildi.";
                Log.Warning("Mega upload iptal edildi: {FileName}", remoteFileName);
            }
            catch (TimeoutException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Log.Error("Mega zaman aşımı: {FileName} — {Message}", remoteFileName, ex.Message);
                await InvalidateSessionInternalAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Log.Error(ex, "Mega upload başarısız: {FileName}", remoteFileName);
                await InvalidateSessionInternalAsync().ConfigureAwait(false);
            }
            finally
            {
                _sessionSemaphore.Release();
            }

            return result;
        }

        public async Task<bool> DeleteAsync(
            string remoteFileIdentifier,
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var client = await GetOrCreateSessionAsync(config, cancellationToken).ConfigureAwait(false);

                var nodes = await client.GetNodesAsync().ConfigureAwait(false);
                var targetNode = nodes.FirstOrDefault(n => n.Id == remoteFileIdentifier);

                if (targetNode is null)
                {
                    Log.Debug("Mega dosyası zaten mevcut değil: {NodeId}", remoteFileIdentifier);
                    return true;
                }

                bool moveToTrash = !config.PermanentDeleteFromTrash;
                await client.DeleteAsync(targetNode, moveToTrash).ConfigureAwait(false);

                _sessionLastUsedUtc = DateTime.UtcNow;

                Log.Information(
                    moveToTrash
                        ? "Mega dosyası çöp kutusuna taşındı: {NodeId}"
                        : "Mega dosyası kalıcı olarak silindi: {NodeId}",
                    remoteFileIdentifier);

                return true;
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Mega silme iptal edildi: {NodeId}", remoteFileIdentifier);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Mega silme başarısız: {NodeId}", remoteFileIdentifier);
                await InvalidateSessionInternalAsync().ConfigureAwait(false);
                return false;
            }
            finally
            {
                _sessionSemaphore.Release();
            }
        }

        public async Task<bool> TestConnectionAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            MegaApiClient client = null;

            try
            {
                ValidateConfig(config);

                client = new MegaApiClient(new Options(synchronizeApiRequests: false));
                await LoginWithTimeoutAsync(client, config, cancellationToken).ConfigureAwait(false);

                var accountInfo = await client.GetAccountInformationAsync().ConfigureAwait(false);

                long usedMb = accountInfo.UsedQuota / (1024 * 1024);
                long totalMb = accountInfo.TotalQuota / (1024 * 1024);

                Log.Information(
                    "Mega bağlantı testi başarılı — Kullanım: {Used:N0} MB / {Total:N0} MB",
                    usedMb, totalMb);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Mega bağlantı testi başarısız.");
                return false;
            }
            finally
            {
                if (client is not null)
                    await LogoutSafeAsync(client).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Upload işlemini CancellationToken ile korur.
        /// MegaApiClient token'ı düzgün işlemezse WhenAny ile iptal garantilenir.
        /// </summary>
        private static async Task<INode> UploadWithCancellationAsync(
            MegaApiClient client,
            FileStream stream,
            string remoteFileName,
            INode targetFolder,
            IProgress<double> megaProgress,
            CancellationToken cancellationToken)
        {
            var uploadTask = client.UploadAsync(
                stream,
                remoteFileName,
                targetFolder,
                megaProgress,
                null,
                cancellationToken);

            var completed = await Task.WhenAny(
                uploadTask,
                Task.Delay(Timeout.Infinite, cancellationToken))
                .ConfigureAwait(false);

            if (completed != uploadTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return await uploadTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Mega'da hedef klasör yapısını oluşturur veya bulur.
        /// Alt klasör desteği: "Yedekler/Plan1" → Yedekler altında Plan1 oluşturulur.
        /// </summary>
        private static async Task<INode> EnsureFolderExistsAsync(
            MegaApiClient client,
            string folderPath,
            CancellationToken cancellationToken)
        {
            var nodes = await client.GetNodesAsync().ConfigureAwait(false);
            var root = nodes.First(n => n.Type == NodeType.Root);

            if (string.IsNullOrWhiteSpace(folderPath))
                return root;

            // Slash ile ayrılmış alt klasörleri sırayla oluştur/bul
            string[] parts = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            INode current = root;

            foreach (string part in parts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var existing = nodes.FirstOrDefault(n =>
                    n.Type == NodeType.Directory &&
                    n.ParentId == current.Id &&
                    string.Equals(n.Name, part, StringComparison.OrdinalIgnoreCase));

                if (existing is not null)
                {
                    current = existing;
                }
                else
                {
                    current = await client.CreateFolderAsync(part, current).ConfigureAwait(false);
                    Log.Debug("Mega klasörü oluşturuldu: {FolderName}", part);

                    // Yeni node listesini güncelle (sonraki iterasyon için)
                    nodes = await client.GetNodesAsync().ConfigureAwait(false);
                }
            }

            return current;
        }

        /// <summary>
        /// Mega çöp kutusundaki YALNIZCA bizim yedek dosyalarımızı kalıcı olarak siler.
        /// Dosya adı desenine göre filtreler — kullanıcının kişisel dosyalarına dokunmaz.
        /// Yedek desenleri: *_Full_*.bak/7z, *_Differential_*, *_Incremental_*, Files_*.7z
        /// </summary>
        public async Task<int> EmptyTrashAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            await _sessionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var client = await GetOrCreateSessionAsync(config, cancellationToken).ConfigureAwait(false);

                var nodes = await client.GetNodesAsync().ConfigureAwait(false);
                var trashNode = nodes.FirstOrDefault(n => n.Type == NodeType.Trash);

                if (trashNode is null)
                {
                    Log.Information("Mega çöp kutusu bulunamadı.");
                    return 0;
                }

                // Trash'ın doğrudan çocuklarından sadece bizim yedek dosyalarımızı filtrele
                var ourTrashFiles = nodes
                    .Where(n => n.ParentId == trashNode.Id && IsOurBackupFile(n.Name))
                    .ToList();

                int totalTrashCount = nodes.Count(n => n.ParentId == trashNode.Id);

                if (ourTrashFiles.Count == 0)
                {
                    Log.Information("Mega çöp kutusunda bizim yedek dosyamız yok (toplam çöp: {Total}).", totalTrashCount);
                    return 0;
                }

                Log.Information(
                    "Mega çöp kutusundan {OurCount}/{TotalCount} yedek dosyası kalıcı siliniyor",
                    ourTrashFiles.Count, totalTrashCount);

                int deletedCount = 0;

                foreach (var node in ourTrashFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await client.DeleteAsync(node, moveToTrash: false).ConfigureAwait(false);
                        deletedCount++;
                        Log.Debug("Mega çöp yedek dosyası kalıcı silindi: {NodeName} ({NodeId})", node.Name, node.Id);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Mega çöp öğesi silinemedi: {NodeName} ({NodeId})", node.Name, node.Id);
                    }
                }

                _sessionLastUsedUtc = DateTime.UtcNow;

                Log.Information("Mega çöp kutusu temizlendi: {Deleted}/{Total} yedek dosyası silindi",
                    deletedCount, ourTrashFiles.Count);

                return deletedCount;
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Mega çöp boşaltma iptal edildi.");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Mega çöp kutusu boşaltılamadı.");
                await InvalidateSessionInternalAsync().ConfigureAwait(false);
                return 0;
            }
            finally
            {
                _sessionSemaphore.Release();
            }
        }

        /// <summary>
        /// Dosya adının bizim yedek dosya desenimize uyup uymadığını kontrol eder.
        /// SQL yedek: DbName_Full_20260405_123456.bak/.7z (Full/Differential/Incremental)
        /// Dosya yedek: Files_20260405_123456.7z
        /// </summary>
        private static bool IsOurBackupFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string ext = Path.GetExtension(fileName);
            bool isBak = string.Equals(ext, ".bak", StringComparison.OrdinalIgnoreCase);
            bool is7z = string.Equals(ext, ".7z", StringComparison.OrdinalIgnoreCase);

            if (!isBak && !is7z)
                return false;

            // Yedek dosya adı desenleri — SQL backup veya dosya backup
            return fileName.Contains("_Full_", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("_Differential_", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("_Incremental_", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("Files_", StringComparison.OrdinalIgnoreCase);
        }
    }
}
