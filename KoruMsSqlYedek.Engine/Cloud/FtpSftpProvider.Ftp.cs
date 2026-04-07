using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Serilog;
using KoruMsSqlYedek.Core.Constants;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    // ── FTP/FTPS Operations (FluentFTP) ──────────────────────────────
    public partial class FtpSftpProvider
    {
        private async Task<long> UploadViaFtpAsync(
            string localFilePath, string remotePath, CloudTargetConfig config,
            IProgress<int> progress, CancellationToken cancellationToken)
        {
            using (var client = CreateFtpClient(config))
            {
                client.Config.ConnectTimeout = TimeoutConstants.FtpConnectTimeoutMs;
                client.Config.DataConnectionConnectTimeout = TimeoutConstants.FtpDataConnectionTimeoutMs;
                client.Config.ReadTimeout = TimeoutConstants.FtpReadTimeoutMs;

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
                client.Config.ConnectTimeout = TimeoutConstants.FtpTestConnectionTimeoutMs;
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
    }
}
