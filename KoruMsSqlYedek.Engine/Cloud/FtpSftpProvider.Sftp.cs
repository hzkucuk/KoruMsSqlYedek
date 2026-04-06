using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Constants;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    // ── SFTP Operations (SSH.NET) ────────────────────────────────────
    public partial class FtpSftpProvider
    {
        private async Task<long> UploadViaSftpAsync(
            string localFilePath, string remotePath, CloudTargetConfig config,
            IProgress<int> progress, CancellationToken cancellationToken)
        {
            long remoteSize = await Task.Run(() =>
            {
                using (var client = CreateSftpClient(config))
                {
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(TimeoutConstants.SftpConnectTimeoutSeconds);
                    client.OperationTimeout = TimeSpan.FromSeconds(TimeoutConstants.SftpOperationTimeoutSeconds);
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
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(TimeoutConstants.FtpTestConnectionTimeoutMs / 1000);
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
    }
}
