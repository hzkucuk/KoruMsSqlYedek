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
    public partial class GoogleDriveProvider : ICloudProvider
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
        /// <summary>
        /// Klasör yolundan ID bulur (oluşturmaz). Bulamazsa null döner.
        /// </summary>
        private static async Task<string> FindFolderIdAsync(
            DriveService driveService,
            string folderPath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(folderPath))
                return null;

            // Klasör ID gibi görünüyorsa doğrudan doğrula
            if (IsLikelyFolderId(folderPath))
            {
                try
                {
                    var getRequest = driveService.Files.Get(folderPath);
                    getRequest.Fields = "id,mimeType,trashed";
                    var existing = await getRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                    if (existing.MimeType == "application/vnd.google-apps.folder" && existing.Trashed != true)
                        return existing.Id;
                }
                catch (Google.GoogleApiException) { }
            }

            // Yol olarak iç içe klasörleri ara
            string[] parts = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string parentId = "root";

            foreach (string folderName in parts)
            {
                string folderId = await FindFolderAsync(driveService, folderName, parentId, cancellationToken)
                    .ConfigureAwait(false);

                if (folderId is null)
                    return null; // Klasör bulunamadı

                parentId = folderId;
            }

            return parentId;
        }
    }
}
