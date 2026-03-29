using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using DriveUpload = Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// OneDrive bulut provider'ı (bireysel MSA ve kurumsal Entra ID).
    /// Microsoft Graph API ile erişim. Resumable upload, klasör yönetimi,
    /// silme + çöp kutusu temizleme desteği.
    /// </summary>
    public class OneDriveProvider : ICloudProvider
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<OneDriveProvider>();
        private readonly CloudProviderType _type;

        /// <summary>
        /// Chunk boyutu: 10 × 320 KiB = 3.125 MiB (Graph API 320 KiB katları gerektirir).
        /// </summary>
        private const int MaxChunkSize = 10 * 320 * 1024;

        public OneDriveProvider(CloudProviderType type)
        {
            if (type != CloudProviderType.OneDrivePersonal && type != CloudProviderType.OneDriveBusiness)
                throw new ArgumentException($"Geçersiz OneDrive provider türü: {type}", nameof(type));

            _type = type;
        }

        public CloudProviderType ProviderType => _type;
        public string DisplayName => _type == CloudProviderType.OneDriveBusiness
            ? "OneDrive (Kurumsal)"
            : "OneDrive (Bireysel)";

        public async Task<CloudUploadResult> UploadAsync(
            string localFilePath,
            string remoteFileName,
            CloudTargetConfig config,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            var result = new CloudUploadResult { ProviderType = _type };

            try
            {
                var validationError = ValidateConfig(config);
                if (validationError != null)
                {
                    result.ErrorMessage = validationError;
                    return result;
                }

                if (!File.Exists(localFilePath))
                {
                    result.ErrorMessage = $"Kaynak dosya bulunamadı: {localFilePath}";
                    return result;
                }

                var client = await OneDriveAuthHelper.CreateGraphClientAsync(config, cancellationToken)
                    .ConfigureAwait(false);

                // Drive ID al
                var drive = await client.Me.Drive.GetAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (drive == null)
                {
                    result.ErrorMessage = "OneDrive erişimi sağlanamadı.";
                    return result;
                }

                // Hedef klasörün var olduğundan emin ol
                await EnsureFolderExistsAsync(client, drive.Id, config.RemoteFolderPath, cancellationToken)
                    .ConfigureAwait(false);

                // Uzak dosya yolu
                string remotePath = string.IsNullOrEmpty(config.RemoteFolderPath)
                    ? remoteFileName
                    : $"{config.RemoteFolderPath.TrimEnd('/')}/{remoteFileName}";

                // Upload session oluştur
                var uploadSessionBody = new DriveUpload.CreateUploadSessionPostRequestBody
                {
                    Item = new DriveItemUploadableProperties
                    {
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "@microsoft.graph.conflictBehavior", "replace" }
                        }
                    }
                };

                var uploadSession = await client.Drives[drive.Id]
                    .Items["root"]
                    .ItemWithPath(remotePath)
                    .CreateUploadSession
                    .PostAsync(uploadSessionBody, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (uploadSession == null)
                {
                    result.ErrorMessage = "Upload session oluşturulamadı.";
                    return result;
                }

                // Chunked upload
                using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long totalBytes = fileStream.Length;
                    var fileUploadTask = new LargeFileUploadTask<DriveItem>(
                        uploadSession, fileStream, MaxChunkSize, client.RequestAdapter);

                    IProgress<long> progressHandler = new Progress<long>(bytesUploaded =>
                    {
                        if (totalBytes > 0)
                        {
                            int pct = (int)(bytesUploaded * 100 / totalBytes);
                            progress?.Report(Math.Min(pct, 100));
                        }
                    });

                    var uploadResult = await fileUploadTask.UploadAsync(progressHandler, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    if (uploadResult.UploadSucceeded)
                    {
                        var uploadedItem = uploadResult.ItemResponse;
                        result.IsSuccess = true;
                        result.RemoteFilePath = uploadedItem?.Id ?? remotePath;
                        progress?.Report(100);

                        Log.Information(
                            "OneDrive upload başarılı: {FileName} → {RemotePath} ({Size:N0} bytes)",
                            remoteFileName, remotePath, totalBytes);
                    }
                    else
                    {
                        result.ErrorMessage = "Upload session tamamlanamadı.";
                        Log.Error("OneDrive upload başarısız: {FileName}", remoteFileName);
                    }
                }
            }
            catch (ODataError ex)
            {
                result.ErrorMessage = $"OneDrive API hatası: {ex.Error?.Message ?? ex.Message}";
                Log.Error(ex, "OneDrive upload hatası: {FileName}", remoteFileName);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"OneDrive upload hatası: {ex.Message}";
                Log.Error(ex, "OneDrive upload hatası: {FileName}", remoteFileName);
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
                var client = await OneDriveAuthHelper.CreateGraphClientAsync(config, cancellationToken)
                    .ConfigureAwait(false);

                var drive = await client.Me.Drive.GetAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (config.PermanentDeleteFromTrash)
                {
                    try
                    {
                        await client.Drives[drive.Id].Items[remoteFileIdentifier]
                            .PermanentDelete
                            .PostAsync(cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (ODataError)
                    {
                        // PermanentDelete desteklenmiyorsa normal silme
                        await client.Drives[drive.Id].Items[remoteFileIdentifier]
                            .DeleteAsync(cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                else
                {
                    await client.Drives[drive.Id].Items[remoteFileIdentifier]
                        .DeleteAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }

                Log.Information("OneDrive dosya silindi: {FileId}", remoteFileIdentifier);
                return true;
            }
            catch (ODataError ex) when (ex.ResponseStatusCode == 404)
            {
                Log.Warning("OneDrive silme: Dosya zaten mevcut değil: {FileId}", remoteFileIdentifier);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OneDrive silme hatası: {FileId}", remoteFileIdentifier);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            try
            {
                var validationError = ValidateConfig(config);
                if (validationError != null)
                {
                    Log.Warning("OneDrive bağlantı testi başarısız: {Error}", validationError);
                    return false;
                }

                var client = await OneDriveAuthHelper.CreateGraphClientAsync(config, cancellationToken)
                    .ConfigureAwait(false);

                var drive = await client.Me.Drive.GetAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (drive != null)
                {
                    Log.Information(
                        "OneDrive bağlantı testi başarılı. Sahip: {Owner}, Kullanılan: {Used:N0}/{Total:N0} bytes",
                        drive.Owner?.User?.DisplayName ?? "Bilinmiyor",
                        drive.Quota?.Used ?? 0,
                        drive.Quota?.Total ?? 0);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OneDrive bağlantı testi hatası.");
                return false;
            }
        }

        #region Private Helpers

        private static async Task EnsureFolderExistsAsync(
            GraphServiceClient client,
            string driveId,
            string remoteFolderPath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(remoteFolderPath) || remoteFolderPath == "/")
                return;

            try
            {
                await client.Drives[driveId].Root
                    .ItemWithPath(remoteFolderPath)
                    .GetAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ODataError ex) when (ex.ResponseStatusCode == 404)
            {
                await CreateFolderByPathAsync(client, driveId, remoteFolderPath, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private static async Task CreateFolderByPathAsync(
            GraphServiceClient client,
            string driveId,
            string folderPath,
            CancellationToken cancellationToken)
        {
            string[] segments = folderPath.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string currentPath = "";
            string lastFolderId = null;

            foreach (var segment in segments)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

                try
                {
                    var existing = await client.Drives[driveId].Root
                        .ItemWithPath(currentPath)
                        .GetAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    lastFolderId = existing?.Id;
                }
                catch (ODataError ex) when (ex.ResponseStatusCode == 404)
                {
                    var newFolder = new DriveItem
                    {
                        Name = segment,
                        Folder = new Folder(),
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "@microsoft.graph.conflictBehavior", "fail" }
                        }
                    };

                    DriveItem created;
                    if (lastFolderId != null)
                    {
                        created = await client.Drives[driveId].Items[lastFolderId].Children
                            .PostAsync(newFolder, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        created = await client.Drives[driveId].Items["root"].Children
                            .PostAsync(newFolder, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }

                    lastFolderId = created?.Id;
                    Log.Debug("OneDrive klasör oluşturuldu: {Path}", currentPath);
                }
            }
        }

        public static string ValidateConfig(CloudTargetConfig config)
        {
            if (config == null)
                return "CloudTargetConfig null olamaz.";

            if (string.IsNullOrEmpty(config.OAuthClientId))
                return "OAuth2 Client ID yapılandırılmamış.";

            if (string.IsNullOrEmpty(config.OAuthTokenJson))
                return "OneDrive token yapılandırılmamış. Lütfen kimlik doğrulama yapın.";

            if (!OneDriveAuthHelper.IsTokenValid(config.OAuthTokenJson))
                return "OneDrive token geçersiz. Lütfen yeniden kimlik doğrulama yapın.";

            return null;
        }

        #endregion
    }
}
