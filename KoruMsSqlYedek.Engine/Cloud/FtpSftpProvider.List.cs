using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using KoruMsSqlYedek.Core.Constants;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    // ── ICloudFolderListProvider implementasyonu ────────────────────────
    // Retention "folder sweep" için uzak klasördeki .bak/.7z dosyalarını listeler.
    // FileId, DeleteAsync'e verilen uzak yoldur (BuildRemotePath ile aynı biçim).
    // Klasör yoksa boş liste döner; bağlantı hatası fırlatılır (orkestratör yakalar).
    public partial class FtpSftpProvider : ICloudFolderListProvider
    {
        public async Task<List<CloudFileEntry>> ListFolderAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            ValidateConfig(config);

            string folder = string.IsNullOrEmpty(config.RemoteFolderPath)
                ? "/"
                : config.RemoteFolderPath.TrimEnd('/', '\\');
            if (folder.Length == 0)
                folder = "/";

            var entries = _type == CloudProviderType.Sftp
                ? await ListViaSftpAsync(folder, config, cancellationToken).ConfigureAwait(false)
                : await ListViaFtpAsync(folder, config, cancellationToken).ConfigureAwait(false);

            Log.Information("{Provider} folder sweep listesi: {Count} dosya (klasör: {Folder})",
                DisplayName, entries.Count, folder);
            return entries;
        }

        private async Task<List<CloudFileEntry>> ListViaFtpAsync(
            string folder, CloudTargetConfig config, CancellationToken cancellationToken)
        {
            using (var client = CreateFtpClient(config))
            {
                client.Config.ConnectTimeout = TimeoutConstants.FtpConnectTimeoutMs;
                await client.AutoConnect(cancellationToken);

                var entries = new List<CloudFileEntry>();
                if (await client.DirectoryExists(folder, cancellationToken))
                {
                    foreach (var item in await client.GetListing(folder, cancellationToken))
                    {
                        if (item.Type != FtpObjectType.File || !IsBackupExtension(item.Name))
                            continue;

                        entries.Add(new CloudFileEntry
                        {
                            FileId = BuildRemotePath(config.RemoteFolderPath, item.Name),
                            Name = item.Name,
                            CreatedAtUtc = item.Modified == DateTime.MinValue
                                ? DateTime.UtcNow
                                : item.Modified.ToUniversalTime(),
                            SizeBytes = item.Size
                        });
                    }
                }

                await client.Disconnect(cancellationToken);
                return entries;
            }
        }

        private Task<List<CloudFileEntry>> ListViaSftpAsync(
            string folder, CloudTargetConfig config, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using (var client = CreateSftpClient(config))
                {
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(TimeoutConstants.SftpConnectTimeoutSeconds);
                    client.OperationTimeout = TimeSpan.FromSeconds(TimeoutConstants.SftpOperationTimeoutSeconds);
                    client.Connect();

                    var entries = new List<CloudFileEntry>();
                    if (client.Exists(folder))
                    {
                        foreach (var file in client.ListDirectory(folder))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (!file.IsRegularFile || !IsBackupExtension(file.Name))
                                continue;

                            entries.Add(new CloudFileEntry
                            {
                                FileId = BuildRemotePath(config.RemoteFolderPath, file.Name),
                                Name = file.Name,
                                CreatedAtUtc = file.LastWriteTimeUtc,
                                SizeBytes = file.Length
                            });
                        }
                    }

                    client.Disconnect();
                    return entries;
                }
            }, cancellationToken);
        }

        private static bool IsBackupExtension(string name)
            => name != null &&
               (name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase));
    }
}
