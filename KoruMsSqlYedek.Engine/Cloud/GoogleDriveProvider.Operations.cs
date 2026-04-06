using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using Serilog;
using KoruMsSqlYedek.Core.Models;

using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace KoruMsSqlYedek.Engine.Cloud
{
    // ── Public API Operations + Upload Helper ─────────────────────────
    public partial class GoogleDriveProvider
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
                ProviderType = _type,
                DisplayName = DisplayName
            };

            try
            {
                ValidateConfig(config);

                if (!File.Exists(localFilePath))
                    throw new FileNotFoundException("Kaynak dosya bulunamadı.", localFilePath);

                using (var driveService = await GoogleDriveAuthHelper.CreateDriveServiceAsync(config, cancellationToken)
                    .ConfigureAwait(false))
                {
                    string folderId = await EnsureFolderExistsAsync(
                        driveService, config.RemoteFolderPath, cancellationToken).ConfigureAwait(false);

                    var (fileId, remoteSize) = await UploadFileAsync(
                        driveService, localFilePath, remoteFileName, folderId,
                        progress, cancellationToken,
                        resumeSessionUri, sessionUriObtained)
                        .ConfigureAwait(false);

                    result.IsSuccess = true;
                    result.RemoteFilePath = fileId;
                    result.RemoteFileSizeBytes = remoteSize;
                    result.UploadedAt = DateTime.UtcNow;

                    var fileInfo = new FileInfo(localFilePath);
                    Log.Information(
                        "Google Drive upload başarılı: {FileName} → {FileId} ({Size:N0} bytes, uzak={RemoteSize:N0} bytes)",
                        remoteFileName, fileId, fileInfo.Length, remoteSize);
                }
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "İşlem kullanıcı tarafından iptal edildi.";
                Log.Warning("Google Drive upload iptal edildi: {FileName}", remoteFileName);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Log.Error(ex, "Google Drive upload başarısız: {FileName}", remoteFileName);
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
                using (var driveService = await GoogleDriveAuthHelper.CreateDriveServiceAsync(config, cancellationToken)
                    .ConfigureAwait(false))
                {
                    if (config.PermanentDeleteFromTrash)
                    {
                        // Kalıcı silme (çöp kutusuna göndermeden doğrudan sil)
                        await driveService.Files.Delete(remoteFileIdentifier)
                            .ExecuteAsync(cancellationToken).ConfigureAwait(false);

                        Log.Information("Google Drive dosyası kalıcı olarak silindi: {FileId}", remoteFileIdentifier);
                    }
                    else
                    {
                        // Çöp kutusuna gönder
                        var updateRequest = driveService.Files.Update(
                            new GoogleFile { Trashed = true }, remoteFileIdentifier);
                        await updateRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                        Log.Information("Google Drive dosyası çöp kutusuna taşındı: {FileId}", remoteFileIdentifier);
                    }

                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Google Drive silme iptal edildi: {FileId}", remoteFileIdentifier);
                return false;
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Log.Debug("Google Drive dosyası zaten mevcut değil: {FileId}", remoteFileIdentifier);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Google Drive silme başarısız: {FileId}", remoteFileIdentifier);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            try
            {
                ValidateConfig(config);

                using (var driveService = await GoogleDriveAuthHelper.CreateDriveServiceAsync(config, cancellationToken)
                    .ConfigureAwait(false))
                {
                    // About.Get ile bağlantıyı test et — kullanıcı bilgilerini al
                    var aboutRequest = driveService.About.Get();
                    aboutRequest.Fields = "user,storageQuota";
                    var about = await aboutRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                    var quota = about.StorageQuota;
                    string userEmail = about.User?.EmailAddress ?? "bilinmiyor";

                    if (quota != null && quota.Limit.HasValue)
                    {
                        long usedMb = (quota.Usage ?? 0) / (1024 * 1024);
                        long limitMb = quota.Limit.Value / (1024 * 1024);
                        Log.Information(
                            "Google Drive bağlantı testi başarılı: {Email} — Kullanım: {Used:N0} MB / {Limit:N0} MB",
                            userEmail, usedMb, limitMb);
                    }
                    else
                    {
                        Log.Information(
                            "Google Drive bağlantı testi başarılı: {Email} — Sınırsız depolama",
                            userEmail);
                    }

                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Google Drive bağlantı testi başarısız.");
                return false;
            }
        }

        /// <summary>
        /// Dosyayı resumable upload ile Google Drive'a yükler.
        /// Yeni upload: session başlatılır, URI callback aracılığıyla kaydedilir, ardından aktarılır.
        /// Devam: kaydedilmiş URI ile Google sunucusuna kalan byte sayısı sorgulanır, stream konumlandırılır.
        /// </summary>
        private static async Task<(string FileId, long RemoteSize)> UploadFileAsync(
            DriveService driveService,
            string localFilePath,
            string remoteFileName,
            string parentFolderId,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            string resumeSessionUri,
            Action<string> sessionUriObtained)
        {
            var fileMetadata = new GoogleFile
            {
                Name = remoteFileName,
                Parents = parentFolderId != null
                    ? new[] { parentFolderId }.ToList()
                    : null
            };

            long fileSize = new FileInfo(localFilePath).Length;
            const string mimeType = "application/octet-stream";

            using (var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var uploadRequest = driveService.Files.Create(fileMetadata, stream, mimeType);
                uploadRequest.ChunkSize = ChunkSize;
                uploadRequest.Fields = "id,name,size";

                uploadRequest.ProgressChanged += (uploadProgress) =>
                {
                    if (fileSize > 0 && progress != null)
                    {
                        int percent = (int)(uploadProgress.BytesSent * 100 / fileSize);
                        progress.Report(percent);
                    }
                };

                IUploadProgress uploadResult;

                if (!string.IsNullOrEmpty(resumeSessionUri))
                {
                    // Kaldığı yerden devam et — Google sunucusu kalan byte aralıklarını belirler
                    Log.Information(
                        "Google Drive upload kaldığı yerden devam ediyor: {FileName}",
                        remoteFileName);
                    uploadResult = await uploadRequest
                        .ResumeAsync(new Uri(resumeSessionUri), cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // Yeni session: InitiateSessionAsync Task<Uri> döndürür — UploadUri property yok
                    var resumableBase = (Google.Apis.Upload.ResumableUpload)uploadRequest;
                    Uri sessionUri = await resumableBase
                        .InitiateSessionAsync(cancellationToken)
                        .ConfigureAwait(false);
                    sessionUriObtained?.Invoke(sessionUri?.ToString());

                    uploadResult = await resumableBase
                        .ResumeAsync(sessionUri, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (uploadResult.Status == UploadStatus.Failed)
                {
                    throw new IOException(
                        $"Google Drive upload başarısız: {uploadResult.Exception?.Message}",
                        uploadResult.Exception);
                }

                progress?.Report(100);

                string fileId = uploadRequest.ResponseBody?.Id;
                if (string.IsNullOrEmpty(fileId))
                    throw new IOException("Google Drive upload sonucunda dosya ID'si alınamadı.");

                // Bütünlük: Google'dan alınan boyutu doğrula
                long remoteSize = uploadRequest.ResponseBody?.Size ?? 0;
                if (remoteSize == 0 && fileSize > 0)
                {
                    // ResponseBody.Size zaman zaman gelmiyor — ayrı API çağrısıyla al
                    var getReq = driveService.Files.Get(fileId);
                    getReq.Fields = "id,size";
                    var meta = await getReq.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    remoteSize = meta.Size ?? 0;
                }

                return (fileId, remoteSize);
            }
        }

        /// <summary>
        /// Google Drive çöp kutusundaki YALNIZCA bizim klasörümüze ait dosyaları kalıcı olarak siler.
        /// Kullanıcının diğer çöp öğelerine dokunmaz.
        /// </summary>
        public async Task<int> EmptyTrashAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            try
            {
                ValidateConfig(config);

                using (var driveService = await GoogleDriveAuthHelper.CreateDriveServiceAsync(config, cancellationToken)
                    .ConfigureAwait(false))
                {
                    // Bizim klasörümüzün ID'sini bul
                    string folderId = null;
                    if (!string.IsNullOrEmpty(config.RemoteFolderPath))
                    {
                        try
                        {
                            folderId = await FindFolderIdAsync(driveService, config.RemoteFolderPath, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            // Klasör bulunamazsa root'tan arayacağız
                        }
                    }

                    // Sadece bizim klasörümüzdeki çöp dosyalarını listele
                    string query = folderId is not null
                        ? $"trashed = true and '{folderId}' in parents"
                        : "trashed = true";

                    var allTrashedFiles = new System.Collections.Generic.List<string>();
                    string pageToken = null;

                    do
                    {
                        var listRequest = driveService.Files.List();
                        listRequest.Q = query;
                        listRequest.Fields = "nextPageToken, files(id, name)";
                        listRequest.PageSize = 100;
                        if (pageToken is not null)
                            listRequest.PageToken = pageToken;

                        var result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                        if (result.Files is not null)
                        {
                            foreach (var file in result.Files)
                                allTrashedFiles.Add(file.Id);
                        }

                        pageToken = result.NextPageToken;
                    }
                    while (pageToken is not null);

                    if (allTrashedFiles.Count == 0)
                    {
                        Log.Information("Google Drive çöp kutusunda bizim dosyamız yok.");
                        return 0;
                    }

                    Log.Information(
                        "Google Drive çöp kutusundan {Count} dosya kalıcı siliniyor (klasör: {Folder})",
                        allTrashedFiles.Count,
                        config.RemoteFolderPath ?? "root");

                    int deletedCount = 0;

                    foreach (string fileId in allTrashedFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            await driveService.Files.Delete(fileId)
                                .ExecuteAsync(cancellationToken).ConfigureAwait(false);
                            deletedCount++;
                        }
                        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            // Zaten silinmiş — sorun değil
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Google Drive çöp öğesi silinemedi: {FileId}", fileId);
                        }
                    }

                    Log.Information(
                        "Google Drive çöp kutusu temizlendi: {Deleted}/{Total} dosya silindi (klasör: {Folder})",
                        deletedCount, allTrashedFiles.Count,
                        config.RemoteFolderPath ?? "root");

                    return deletedCount;
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Google Drive çöp boşaltma iptal edildi.");
                return 0;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Google Drive çöp kutusu boşaltılamadı.");
                return 0;
            }
        }
    }
}
