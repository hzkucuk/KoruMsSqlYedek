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

namespace KoruMsSqlYedek.Engine.Cloud
{
    /// <summary>
    /// FTP/FTPS/SFTP bulut provider'ı.
    /// FTP/FTPS: FluentFTP (AsyncFtpClient), SFTP: SSH.NET (SftpClient).
    /// Upload sonrası MD5 checksum doğrulama destekler.
    /// </summary>
    public class FtpSftpProvider : ICloudProvider
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

        #region FTP/FTPS (FluentFTP)

        private async Task<long> UploadViaFtpAsync(
            string localFilePath, string remotePath, CloudTargetConfig config,
            IProgress<int> progress, CancellationToken cancellationToken)
        {
            using (var client = CreateFtpClient(config))
            {
                client.Config.ConnectTimeout = 30000;
                client.Config.DataConnectionConnectTimeout = 30000;
                client.Config.ReadTimeout = 60000;

                await client.AutoConnect(cancellationToken);

                Log.Debug("FTP bağlantısı kuruldu: {Host}:{Port}", config.Host, config.Port ?? 21);

                // Uzak klasör yoksa oluştur
                string remoteDir = GetRemoteDirectory(remotePath);
                if (!string.IsNullOrEmpty(remoteDir))
                {
                    await client.CreateDirectory(remoteDir, true, cancellationToken);
                }

                IProgress<FtpProgress> ftpProgress = null;
                if (progress != null)
                {
                    ftpProgress = new Progress<FtpProgress>(p =>
                    {
                        if (p.Progress >= 0)
                            progress.Report((int)p.Progress);
                    });
                }

                // Resume: yarıda kalan dosyayı kaldığı yerden devam ettirir
                var status = await client.UploadFile(
                    localFilePath,
                    remotePath,
                    FtpRemoteExists.Resume,
                    createRemoteDir: true,
                    progress: ftpProgress,
                    token: cancellationToken);

                if (status != FtpStatus.Success)
                {
                    throw new IOException($"FTP upload başarısız: status={status}");
                }

                // Bütünlük: MD5 checksum doğrulama (sunucu destekliyorsa)
                await VerifyChecksumFtpAsync(client, localFilePath, remotePath, cancellationToken);

                // Uzak dosya boyutunu al
                long remoteSize = await client.GetFileSize(remotePath, -1, cancellationToken);

                await client.Disconnect(cancellationToken);
                return remoteSize;
            }
        }

        private async Task DeleteViaFtpAsync(
            string remotePath, CloudTargetConfig config, CancellationToken cancellationToken)
        {
            using (var client = CreateFtpClient(config))
            {
                await client.AutoConnect(cancellationToken);
                await client.DeleteFile(remotePath, cancellationToken);
                await client.Disconnect(cancellationToken);
            }
        }

        private async Task<bool> TestFtpConnectionAsync(
            CloudTargetConfig config, CancellationToken cancellationToken)
        {
            using (var client = CreateFtpClient(config))
            {
                client.Config.ConnectTimeout = 10000;
                await client.AutoConnect(cancellationToken);
                bool connected = client.IsConnected;
                await client.Disconnect(cancellationToken);
                return connected;
            }
        }

        private AsyncFtpClient CreateFtpClient(CloudTargetConfig config)
        {
            int port = config.Port ?? (_type == CloudProviderType.Ftps ? 990 : 21);
            string password = DecryptPassword(config.Password);

            var client = new AsyncFtpClient(config.Host, config.Username, password, port);

            if (_type == CloudProviderType.Ftps)
            {
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;

                if (config.FtpsSkipCertificateValidation)
                {
                    Log.Warning(
                        "FTPS sertifika doğrulaması devre dışı — MITM riski mevcut: {Host}:{Port}",
                        config.Host, port);
                    client.ValidateCertificate += (control, e) => { e.Accept = true; };
                }
                // else: FluentFTP varsayılan olarak sistem sertifika deposunu kullanarak doğrular
            }

            return client;
        }

        private async Task VerifyChecksumFtpAsync(
            AsyncFtpClient client, string localFilePath, string remotePath,
            CancellationToken cancellationToken)
        {
            try
            {
                FtpHash remoteHash = await client.GetChecksum(remotePath, FtpHashAlgorithm.MD5, cancellationToken);
                if (remoteHash != null && remoteHash.IsValid)
                {
                    string localHash = ComputeLocalMd5(localFilePath);
                    if (!string.Equals(localHash, remoteHash.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Warning(
                            "Checksum uyuşmazlığı: local={LocalHash}, remote={RemoteHash}, file={File}",
                            localHash, remoteHash.Value, remotePath);
                    }
                    else
                    {
                        Log.Debug("Checksum doğrulama başarılı: {File}", remotePath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Bazı FTP sunucuları checksum desteklemez — uyarı olarak logla
                Log.Debug(ex, "FTP checksum doğrulama atlandı (sunucu desteklemiyor olabilir)");
            }
        }

        #endregion

        #region SFTP (SSH.NET)

        private async Task<long> UploadViaSftpAsync(
            string localFilePath, string remotePath, CloudTargetConfig config,
            IProgress<int> progress, CancellationToken cancellationToken)
        {
            long remoteSize = await Task.Run(() =>
            {
                using (var client = CreateSftpClient(config))
                {
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
                    client.OperationTimeout = TimeSpan.FromMinutes(30);
                    client.Connect();

                    Log.Debug("SFTP bağlantısı kuruldu: {Host}:{Port}", config.Host, config.Port ?? 22);

                    string remoteDir = GetRemoteDirectory(remotePath);
                    if (!string.IsNullOrEmpty(remoteDir))
                        EnsureSftpDirectoryExists(client, remoteDir);

                    long localFileSize = new FileInfo(localFilePath).Length;
                    long remoteOffset = 0;

                    // Yarıda kalan dosya var mı kontrol et
                    if (client.Exists(remotePath))
                    {
                        remoteOffset = client.GetAttributes(remotePath).Size;
                        if (remoteOffset >= localFileSize)
                        {
                            Log.Information(
                                "SFTP dosyası zaten tam mevcut, upload atlanıyor: {Path}",
                                remotePath);
                            client.Disconnect();
                            return remoteOffset;
                        }
                        Log.Information(
                            "SFTP upload kaldığı yerden devam ediyor: {Offset:N0}/{Total:N0} bytes — {Path}",
                            remoteOffset, localFileSize, remotePath);
                    }

                    using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (remoteOffset > 0)
                        {
                            // Kaldığı yerden devam: yerel stream konumlandır, uzak dosyayı aynı noktadan yaz
                            fileStream.Seek(remoteOffset, SeekOrigin.Begin);

                            using (var sftp = client.Open(remotePath, FileMode.Open, FileAccess.ReadWrite))
                            {
                                sftp.Seek(remoteOffset, SeekOrigin.Begin);
                                byte[] buffer = new byte[32 * 1024];
                                int read;
                                long uploaded = remoteOffset;

                                while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    sftp.Write(buffer, 0, read);
                                    uploaded += read;
                                    if (localFileSize > 0)
                                        progress?.Report((int)(uploaded * 100 / localFileSize));
                                }
                            }
                        }
                        else
                        {
                            Action<ulong> uploadCallback = null;
                            if (progress != null)
                            {
                                uploadCallback = uploaded =>
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    int percent = localFileSize > 0
                                        ? (int)((double)uploaded / localFileSize * 100)
                                        : 0;
                                    progress.Report(percent);
                                };
                            }

                            client.UploadFile(fileStream, remotePath, canOverride: true, uploadCallback);
                        }
                    }

                    // Bütünlük: checksum doğrulama
                    VerifyChecksumSftp(client, localFilePath, remotePath);

                    // Uzak dosya boyutunu al
                    long finalSize = client.GetAttributes(remotePath).Size;

                    client.Disconnect();

                    HostFingerprintUpdated?.Invoke(config);
                    return finalSize;
                }
            }, cancellationToken);

            return remoteSize;
        }

        private async Task DeleteViaSftpAsync(
            string remotePath, CloudTargetConfig config, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                using (var client = CreateSftpClient(config))
                {
                    client.Connect();
                    client.DeleteFile(remotePath);
                    client.Disconnect();
                }
            }, cancellationToken);
        }

        private async Task<bool> TestSftpConnectionAsync(
            CloudTargetConfig config, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                using (var client = CreateSftpClient(config))
                {
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
                    client.Connect();
                    bool connected = client.IsConnected;
                    client.Disconnect();

                    // TOFU: Yeni parmak izi kaydedildiyse persist et
                    if (connected)
                        HostFingerprintUpdated?.Invoke(config);

                    return connected;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// SFTP istemcisi oluşturur.
        /// Host key doğrulaması: trust-on-first-use (TOFU) — ilk bağlantıda parmak izi kaydedilir,
        /// sonraki bağlantılarda doğrulanır.
        /// </summary>
        private Renci.SshNet.SftpClient CreateSftpClient(CloudTargetConfig config)
        {
            int port = config.Port ?? 22;
            string password = DecryptPassword(config.Password);
            var client = new Renci.SshNet.SftpClient(config.Host, port, config.Username, password);

            client.HostKeyReceived += (sender, e) =>
            {
                string receivedFingerprint = BitConverter.ToString(e.FingerPrint).Replace("-", "").ToLowerInvariant();

                if (string.IsNullOrEmpty(config.SftpHostFingerprint))
                {
                    // TOFU: İlk bağlantı — parmak izini kaydet
                    config.SftpHostFingerprint = receivedFingerprint;
                    Log.Information(
                        "SFTP host key kaydedildi (TOFU): {Host}:{Port} — SHA256:{Fingerprint}",
                        config.Host, port, receivedFingerprint);
                    e.CanTrust = true;
                }
                else if (string.Equals(config.SftpHostFingerprint, receivedFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    // Parmak izi eşleşiyor — güvenilir
                    e.CanTrust = true;
                }
                else
                {
                    // Parmak izi değişmiş — olası MITM saldırısı!
                    Log.Error(
                        "SFTP host key UYUŞMAZLIĞI — olası MITM saldırısı! " +
                        "Host: {Host}:{Port}, Beklenen: {Expected}, Alınan: {Received}",
                        config.Host, port, config.SftpHostFingerprint, receivedFingerprint);
                    e.CanTrust = false;
                }
            };

            return client;
        }

        /// <summary>
        /// SFTP bağlantısı sonrası host key parmak izini kalıcı olarak kaydetmek için kullanılır.
        /// Çağıran kod, config değişikliğini plan dosyasına persist etmelidir.
        /// </summary>
        public event Action<CloudTargetConfig> HostFingerprintUpdated;

        private void EnsureSftpDirectoryExists(Renci.SshNet.SftpClient client, string remotePath)
        {
            string[] parts = remotePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string currentPath = "";

            foreach (string part in parts)
            {
                currentPath += "/" + part;
                try
                {
                    client.ChangeDirectory(currentPath);
                }
                catch
                {
                    client.CreateDirectory(currentPath);
                }
            }
        }

        private void VerifyChecksumSftp(
            Renci.SshNet.SftpClient client, string localFilePath, string remotePath)
        {
            try
            {
                // SFTP üzerinden dosya boyutu karşılaştırması (checksum desteği sınırlı)
                var remoteAttrs = client.GetAttributes(remotePath);
                long localSize = new FileInfo(localFilePath).Length;

                if (remoteAttrs.Size != localSize)
                {
                    Log.Warning(
                        "Boyut uyuşmazlığı: local={LocalSize}, remote={RemoteSize}, file={File}",
                        localSize, remoteAttrs.Size, remotePath);
                }
                else
                {
                    Log.Debug("Boyut doğrulama başarılı: {File} ({Size} bytes)", remotePath, localSize);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "SFTP dosya doğrulama atlandı");
            }
        }

        #endregion

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
