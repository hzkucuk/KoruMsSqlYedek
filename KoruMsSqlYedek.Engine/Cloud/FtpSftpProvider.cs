using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Renci.SshNet;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Core.Constants;

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// FTP/FTPS/SFTP bulut provider'ı.
    /// FTP/FTPS: FluentFTP (AsyncFtpClient), SFTP: SSH.NET (SftpClient).
    /// Upload sonrası MD5 checksum doğrulama destekler.
    /// </summary>
    public partial class FtpSftpProvider : ICloudProvider
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<FtpSftpProvider>();
        private readonly CloudProviderType _type;

        public FtpSftpProvider(CloudProviderType type)
        {
            if (type != CloudProviderType.Ftp &&
                type != CloudProviderType.Ftps &&
                type != CloudProviderType.Sftp)
                throw new ArgumentOutOfRangeException(nameof(type), $"FtpSftpProvider yalnızca Ftp/Ftps/Sftp türlerini destekler: {type}");

            _type = type;
        }

        public CloudProviderType ProviderType => _type;
        public string DisplayName
        {
            get
            {
                switch (_type)
                {
                    case CloudProviderType.Ftp: return "FTP";
                    case CloudProviderType.Ftps: return "FTPS";
                    case CloudProviderType.Sftp: return "SFTP";
                    default: return "FTP/SFTP";
                }
            }
        }

        public bool SupportsTrash => false;

        /// <summary>FTP/SFTP çöp kutusu desteklemez — no-op.</summary>
        public Task<int> EmptyTrashAsync(CloudTargetConfig config, CancellationToken cancellationToken)
            => Task.FromResult(0);

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
                DisplayName = config.DisplayName ?? DisplayName
            };

            try
            {
                ValidateConfig(config);

                if (!File.Exists(localFilePath))
                    throw new FileNotFoundException("Kaynak dosya bulunamadı.", localFilePath);

                string remotePath = BuildRemotePath(config.RemoteFolderPath, remoteFileName);
                long remoteSize;

                if (_type == CloudProviderType.Sftp)
                {
                    remoteSize = await UploadViaSftpAsync(localFilePath, remotePath, config, progress, cancellationToken);
                }
                else
                {
                    remoteSize = await UploadViaFtpAsync(localFilePath, remotePath, config, progress, cancellationToken);
                }

                result.IsSuccess = true;
                result.RemoteFilePath = remotePath;
                result.RemoteFileSizeBytes = remoteSize;
                result.UploadedAt = DateTime.UtcNow;

                Log.Information(
                    "Upload başarılı: {Provider} — {RemotePath} ({RemoteSize:N0} bytes)",
                    DisplayName, remotePath, remoteSize);
            }
            catch (OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Upload iptal edildi.";
                throw;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                Log.Error(ex, "Upload başarısız: {Provider} — {File}", DisplayName, localFilePath);
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
                if (_type == CloudProviderType.Sftp)
                {
                    await DeleteViaSftpAsync(remoteFileIdentifier, config, cancellationToken);
                }
                else
                {
                    await DeleteViaFtpAsync(remoteFileIdentifier, config, cancellationToken);
                }

                Log.Information("Uzak dosya silindi: {Provider} — {Path}", DisplayName, remoteFileIdentifier);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Uzak dosya silinemedi: {Provider} — {Path}", DisplayName, remoteFileIdentifier);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            try
            {
                if (_type == CloudProviderType.Sftp)
                {
                    return await TestSftpConnectionAsync(config, cancellationToken);
                }
                else
                {
                    return await TestFtpConnectionAsync(config, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Bağlantı testi başarısız: {Provider} — {Host}", DisplayName, config.Host);
                return false;
            }
        }
        #region Helpers

        private void ValidateConfig(CloudTargetConfig config)
        {
            if (string.IsNullOrEmpty(config.Host))
                throw new ArgumentException("Sunucu adresi (Host) boş olamaz.");
        }

        private string BuildRemotePath(string remoteFolderPath, string remoteFileName)
        {
            // Path traversal önlemi: yalnızca dosya adını al (../.. gibi girdileri temizler)
            string safeFileName = Path.GetFileName(remoteFileName);
            if (string.IsNullOrEmpty(safeFileName))
                throw new ArgumentException("Geçersiz uzak dosya adı.", nameof(remoteFileName));

            if (string.IsNullOrEmpty(remoteFolderPath))
                return "/" + safeFileName;

            string folder = remoteFolderPath.TrimEnd('/', '\\');
            return folder + "/" + safeFileName;
        }

        private string GetRemoteDirectory(string remotePath)
        {
            int lastSlash = remotePath.LastIndexOf('/');
            return lastSlash > 0 ? remotePath.Substring(0, lastSlash) : null;
        }

        private string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return null;

            try
            {
                if (Core.Helpers.PasswordProtector.IsProtected(encryptedPassword))
                    return Core.Helpers.PasswordProtector.Unprotect(encryptedPassword);

                Log.Warning(
                    "FTP/SFTP şifresi DPAPI koruması olmadan saklanmış — güvenlik riski! " +
                    "Şifreyi ayarlardan yeniden kaydedin.");
                return encryptedPassword;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "DPAPI şifre çözme başarısız. Şifre kullanılamıyor — " +
                    "şifreyi ayarlardan yeniden kaydedin.");
                return encryptedPassword;
            }
        }

        private string ComputeLocalMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        #endregion
    }
}
