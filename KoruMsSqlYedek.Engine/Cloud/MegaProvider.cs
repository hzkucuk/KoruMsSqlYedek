using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// Mega.io cloud provider.
    /// Email/şifre ile kimlik doğrulama, dosya yükleme (progress), silme ve klasör yönetimi destekler.
    /// </summary>
    public class MegaProvider : ICloudProvider
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MegaProvider>();

        public CloudProviderType ProviderType => CloudProviderType.Mega;

        public string DisplayName => "Mega.io";

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

            try
            {
                ValidateConfig(config);

                if (!File.Exists(localFilePath))
                    throw new FileNotFoundException("Kaynak dosya bulunamadı.", localFilePath);

                var client = new MegaApiClient();
                await LoginAsync(client, config).ConfigureAwait(false);

                try
                {
                    var targetFolder = await EnsureFolderExistsAsync(client, config.RemoteFolderPath, cancellationToken)
                        .ConfigureAwait(false);

                    long fileSize = new FileInfo(localFilePath).Length;

                    // Progress adaptörü: MegaApiClient IProgress<double> (0.0-1.0) → IProgress<int> (0-100)
                    var megaProgress = progress is not null
                        ? new Progress<double>(d => progress.Report((int)(d * 100)))
                        : null;

                    INode uploadedNode;
                    using (var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        uploadedNode = await client.UploadAsync(
                            stream,
                            remoteFileName,
                            targetFolder,
                            megaProgress,
                            null,
                            cancellationToken).ConfigureAwait(false);
                    }

                    result.IsSuccess = true;
                    result.RemoteFilePath = uploadedNode.Id;
                    result.RemoteFileSizeBytes = uploadedNode.Size;
                    result.UploadedAt = DateTime.UtcNow;

                    Log.Information(
                        "Mega upload başarılı: {FileName} → {NodeId} ({Size:N0} bytes)",
                        remoteFileName, uploadedNode.Id, fileSize);
                }
                finally
                {
                    await client.LogoutAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "İşlem kullanıcı tarafından iptal edildi.";
                Log.Warning("Mega upload iptal edildi: {FileName}", remoteFileName);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Log.Error(ex, "Mega upload başarısız: {FileName}", remoteFileName);
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
                var client = new MegaApiClient();
                await LoginAsync(client, config).ConfigureAwait(false);

                try
                {
                    var nodes = await client.GetNodesAsync().ConfigureAwait(false);
                    var targetNode = nodes.FirstOrDefault(n => n.Id == remoteFileIdentifier);

                    if (targetNode is null)
                    {
                        Log.Debug("Mega dosyası zaten mevcut değil: {NodeId}", remoteFileIdentifier);
                        return true;
                    }

                    bool moveToTrash = !config.PermanentDeleteFromTrash;
                    await client.DeleteAsync(targetNode, moveToTrash).ConfigureAwait(false);

                    Log.Information(
                        moveToTrash
                            ? "Mega dosyası çöp kutusuna taşındı: {NodeId}"
                            : "Mega dosyası kalıcı olarak silindi: {NodeId}",
                        remoteFileIdentifier);

                    return true;
                }
                finally
                {
                    await client.LogoutAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Mega silme iptal edildi: {NodeId}", remoteFileIdentifier);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Mega silme başarısız: {NodeId}", remoteFileIdentifier);
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

                var client = new MegaApiClient();
                await LoginAsync(client, config).ConfigureAwait(false);

                try
                {
                    var accountInfo = await client.GetAccountInformationAsync().ConfigureAwait(false);

                    long usedMb = accountInfo.UsedQuota / (1024 * 1024);
                    long totalMb = accountInfo.TotalQuota / (1024 * 1024);

                    Log.Information(
                        "Mega bağlantı testi başarılı — Kullanım: {Used:N0} MB / {Total:N0} MB",
                        usedMb, totalMb);

                    return true;
                }
                finally
                {
                    await client.LogoutAsync().ConfigureAwait(false);
                }
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
        }

        #region Private Helpers

        /// <summary>
        /// Config doğrulaması: Mega için email ve şifre zorunludur.
        /// </summary>
        private static void ValidateConfig(CloudTargetConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config), "Cloud hedef yapılandırması null olamaz.");

            if (string.IsNullOrWhiteSpace(config.Username))
                throw new InvalidOperationException("Mega email adresi belirtilmemiş.");

            if (string.IsNullOrWhiteSpace(config.Password))
                throw new InvalidOperationException("Mega şifresi belirtilmemiş.");
        }

        /// <summary>
        /// Mega hesabına email/şifre ile giriş yapar.
        /// Şifre DPAPI ile korunmuş olarak saklanır, giriş öncesi çözülür.
        /// </summary>
        private static async Task LoginAsync(MegaApiClient client, CloudTargetConfig config)
        {
            string email = config.Username;
            string password = PasswordProtector.Unprotect(config.Password);

            await client.LoginAsync(email, password).ConfigureAwait(false);
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

        #endregion
    }
}
