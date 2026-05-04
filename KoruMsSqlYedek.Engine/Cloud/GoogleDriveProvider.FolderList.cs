using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Cloud
{
    // ── ICloudFolderListProvider implementasyonu ────────────────────────
    // Retention "folder sweep" için Google Drive klasör içeriğini listeler.
    // Yalnızca config.RemoteFolderPath altındaki, .bak veya .7z uzantılı,
    // çöpte olmayan dosyalar döner.
    public partial class GoogleDriveProvider : ICloudFolderListProvider
    {
        public async Task<List<CloudFileEntry>> ListFolderAsync(
            CloudTargetConfig config,
            CancellationToken cancellationToken)
        {
            var entries = new List<CloudFileEntry>();

            try
            {
                ValidateConfig(config);

                using (var driveService = await GoogleDriveAuthHelper.CreateDriveServiceAsync(config, cancellationToken)
                    .ConfigureAwait(false))
                {
                    // Güvenlik: Klasör belirtilmemişse listeleme YAPMA — kullanıcının
                    // tüm Drive'ını taramayı engeller.
                    string folderId = null;
                    if (!string.IsNullOrEmpty(config.RemoteFolderPath))
                    {
                        try
                        {
                            folderId = await FindFolderIdAsync(
                                driveService, config.RemoteFolderPath, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            // Klasör bulunamazsa güvenli çık
                        }
                    }

                    if (folderId is null)
                    {
                        Log.Warning(
                            "Google Drive folder sweep atlandı: Klasör bulunamadı veya RemoteFolderPath boş. (Klasör: {Folder})",
                            config.RemoteFolderPath ?? "(boş)");
                        return entries;
                    }

                    string query =
                        $"'{folderId}' in parents and trashed = false " +
                        "and mimeType != 'application/vnd.google-apps.folder' " +
                        "and (name contains '.bak' or name contains '.7z')";

                    string pageToken = null;
                    do
                    {
                        var listRequest = driveService.Files.List();
                        listRequest.Q = query;
                        listRequest.Fields = "nextPageToken, files(id, name, createdTime, modifiedTime, size)";
                        listRequest.PageSize = 1000;
                        if (pageToken is not null)
                            listRequest.PageToken = pageToken;

                        var result = await listRequest.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                        if (result.Files is not null)
                        {
                            foreach (var file in result.Files)
                            {
                                DateTime created =
                                    file.CreatedTimeDateTimeOffset?.UtcDateTime
                                    ?? file.ModifiedTimeDateTimeOffset?.UtcDateTime
                                    ?? DateTime.UtcNow;

                                entries.Add(new CloudFileEntry
                                {
                                    FileId = file.Id,
                                    Name = file.Name,
                                    CreatedAtUtc = created,
                                    SizeBytes = file.Size ?? 0
                                });
                            }
                        }

                        pageToken = result.NextPageToken;
                    }
                    while (pageToken is not null);

                    Log.Information(
                        "Google Drive folder sweep listesi: {Count} dosya (klasör: {Folder})",
                        entries.Count, config.RemoteFolderPath);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Warning(ex, "Google Drive klasör listeleme başarısız: {Folder}",
                    config.RemoteFolderPath ?? "(boş)");
            }

            return entries;
        }
    }
}
