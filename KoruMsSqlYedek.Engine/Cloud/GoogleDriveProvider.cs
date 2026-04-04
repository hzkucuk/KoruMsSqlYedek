using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// Google Drive cloud provider.
    /// OAuth2 ile kimlik doğrulama, resumable upload, klasör yönetimi ve çöp kutusu temizleme.
    /// </summary>
    public class GoogleDriveProvider : ICloudProvider
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<GoogleDriveProvider>();
        private readonly CloudProviderType _type;

        /// <summary>
        /// Resumable upload chunk boyutu (256 KB'ın katları — Google API gerekliliği).
        /// </summary>
        private const int ChunkSize = 256 * 1024 * 4; // 1 MB

        public GoogleDriveProvider(CloudProviderType type)
        {
            if (type != CloudProviderType.GoogleDrivePersonal)
                throw new ArgumentException($"Geçersiz provider türü: {type}. GoogleDrivePersonal olmalıdır.");

            _type = type;
        }

        public CloudProviderType ProviderType => _type;

        public string DisplayName => "Google Drive";

        public bool SupportsTrash => true;

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

        #region Private Helpers

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
        /// Belirtilen klasörün var olduğunu doğrular, yoksa oluşturur.
        /// RemoteFolderPath boşsa root (My Drive) kullanılır.
        /// Klasör ID veya klasör adı/yolu kabul eder.
        /// </summary>
        private static async Task<string> EnsureFolderExistsAsync(
            DriveService driveService,
            string folderPath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(folderPath))
                return null; // Root (My Drive) kullanılır

            // Eğer bir klasör ID gibi görünüyorsa doğrudan kullan
            if (IsLikelyFolderId(folderPath))
            {
                try
                {
                    var getRequest = driveService.Files.Get(folderPath);
                    getRequest.Fields = "id,mimeType";
                    var existing = await getRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                    if (existing.MimeType == "application/vnd.google-apps.folder")
                        return existing.Id;
                }
                catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Log.Debug("Klasör ID bulunamadı, isim olarak aranacak: {FolderPath}", folderPath);
                }
            }

            // Klasör adı olarak ara veya oluştur
            return await FindOrCreateFolderByPathAsync(driveService, folderPath, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Klasör yolunu / ile ayrılmış parçalara böler ve iç içe klasörler oluşturur.
        /// Örnek: "KoruMsSqlYedek/Backups" → KoruMsSqlYedek klasörü altında Backups
        /// </summary>
        private static async Task<string> FindOrCreateFolderByPathAsync(
            DriveService driveService,
            string folderPath,
            CancellationToken cancellationToken)
        {
            string[] parts = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string parentId = "root";

            foreach (string folderName in parts)
            {
                string folderId = await FindFolderAsync(driveService, folderName, parentId, cancellationToken)
                    .ConfigureAwait(false);

                if (folderId == null)
                {
                    folderId = await CreateFolderAsync(driveService, folderName, parentId, cancellationToken)
                        .ConfigureAwait(false);

                    Log.Debug("Google Drive klasörü oluşturuldu: {FolderName} (parent: {ParentId})",
                        folderName, parentId);
                }

                parentId = folderId;
            }

            return parentId;
        }

        /// <summary>
        /// Belirli parent altında klasör arar.
        /// </summary>
        private static async Task<string> FindFolderAsync(
            DriveService driveService,
            string folderName,
            string parentId,
            CancellationToken cancellationToken)
        {
            var listRequest = driveService.Files.List();
            listRequest.Q = $"name = '{EscapeQuery(folderName)}' and mimeType = 'application/vnd.google-apps.folder' and '{parentId}' in parents and trashed = false";
            listRequest.Fields = "files(id)";
            listRequest.PageSize = 1;

            var result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return result.Files?.FirstOrDefault()?.Id;
        }

        /// <summary>
        /// Yeni klasör oluşturur.
        /// </summary>
        private static async Task<string> CreateFolderAsync(
            DriveService driveService,
            string folderName,
            string parentId,
            CancellationToken cancellationToken)
        {
            var folderMetadata = new GoogleFile
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new[] { parentId }.ToList()
            };

            var createRequest = driveService.Files.Create(folderMetadata);
            createRequest.Fields = "id";

            var folder = await createRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return folder.Id;
        }

        /// <summary>
        /// Google Drive dosya/klasör ID formatı kontrolü (basit heuristik).
        /// </summary>
        private static bool IsLikelyFolderId(string value)
        {
            // Google Drive ID'leri genellikle 20+ karakter, alfanumerik + _ + -
            return value.Length >= 15 &&
                   !value.Contains("/") &&
                   !value.Contains("\\") &&
                   !value.Contains(" ");
        }

        /// <summary>
        /// Google Drive API sorgu string'inde özel karakterleri escape eder.
        /// </summary>
        private static string EscapeQuery(string value)
        {
            return value?.Replace("'", "\\'");
        }

        /// <summary>
        /// Yapılandırmayı doğrular.
        /// ClientId/Secret gömülü credential'lardan da gelebilir, zorunlu değil.
        /// </summary>
        private static void ValidateConfig(CloudTargetConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // ClientId/Secret: config'de veya gömülü credential'larda olmalı
            bool hasConfigCredentials = !string.IsNullOrEmpty(config.OAuthClientId)
                                     && !string.IsNullOrEmpty(config.OAuthClientSecret);

            if (!hasConfigCredentials && !GoogleOAuthCredentials.IsConfigured)
                throw new ArgumentException(
                    "Google Drive OAuth2 kimlik bilgileri yapılandırılmamış. " +
                    "Gömülü credential bulunamadı ve config'de de yok.");

            if (string.IsNullOrEmpty(config.OAuthTokenJson))
                throw new ArgumentException("Google Drive OAuth2 token bulunamadı. Önce kimlik doğrulama yapılmalıdır.");
        }

        #endregion

        /// <summary>
        /// Google Drive çöp kutusundaki tüm dosyaları kalıcı olarak siler.
        /// Google Drive API'nin files.emptyTrash endpoint'ini kullanır.
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
                    // Önce çöpteki dosya sayısını öğren (bilgilendirme için)
                    int trashCount = 0;
                    try
                    {
                        var listRequest = driveService.Files.List();
                        listRequest.Q = "trashed = true";
                        listRequest.Fields = "files(id)";
                        listRequest.PageSize = 1000;
                        var trashFiles = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                        trashCount = trashFiles.Files?.Count ?? 0;
                    }
                    catch
                    {
                        // Sayım başarısız olursa devam et
                    }

                    if (trashCount == 0)
                    {
                        Log.Information("Google Drive çöp kutusu zaten boş.");
                        return 0;
                    }

                    Log.Information("Google Drive çöp kutusu boşaltılıyor: {Count} öğe", trashCount);

                    // Google Drive API: tek çağrıda tüm çöpü boşaltır
                    var emptyTrashRequest = driveService.Files.EmptyTrash();
                    await emptyTrashRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                    Log.Information("Google Drive çöp kutusu boşaltıldı: {Count} öğe silindi", trashCount);
                    return trashCount;
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
