using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Compression;

namespace KoruMsSqlYedek.Engine.Backup
{
    public partial class SqlBackupService
    {
        #region Express VSS Backup

        /// <summary>
        /// SQL Server Express Edition için ek güvenlik yedeği.
        /// Önce VSS üzerinden MDF/LDF dosya kopyası alınır.
        /// VSS başarısız olursa (admin yetkisi eksik vb.) COPY_ONLY SQL backup alınır.
        /// Her iki yol da .7z olarak sıkıştırılır. Ana yedek etkilenmez.
        /// </summary>
        private async Task TryExpressVssBackupAsync(
            Server server,
            Database dbObj,
            string databaseName,
            string destinationPath,
            BackupResult result,
            CancellationToken ct)
        {
            if (_vssService == null || _compressionService == null)
            {
                Log.Debug("VSS extra backup atlandı: bağımlılıklar enjekte edilmemiş.");
                return;
            }

            // Deneme 1: VSS ile ham dosya kopyası
            bool vssOk = await TryVssFileCopyAsync(dbObj, databaseName, destinationPath, result, ct);
            if (vssOk)
                return;

            // Deneme 2: COPY_ONLY SQL backup fallback
            Log.Information(
                "VSS başarısız; COPY_ONLY SQL backup fallback deneniyor: {Database}", databaseName);
            await TrySqlCopyOnlyFallbackAsync(server, databaseName, destinationPath, result, ct);
        }

        /// <summary>
        /// VSS snapshot üzerinden MDF/LDF/NDF kopyası alıp .7z arşivi oluşturur.
        /// </summary>
        /// <returns>true = arşiv oluşturuldu; false = başarısız/atlandı.</returns>
        private async Task<bool> TryVssFileCopyAsync(
            Database dbObj,
            string databaseName,
            string destinationPath,
            BackupResult result,
            CancellationToken ct)
        {
            if (!_vssService.IsAvailable())
            {
                Log.Warning("VSS: VSS servisi kullanılamıyor.");
                return false;
            }

            string stagingDir = Path.Combine(destinationPath, $"_vss_{Guid.NewGuid():N}");
            var volumeSnapshots = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 1. DB dosya yollarını topla — SMO Refresh ile lazy-load zorla
                var dbFiles = new List<string>();
                try
                {
                    await Task.Run(() =>
                    {
                        dbObj.FileGroups.Refresh();
                        foreach (FileGroup fg in dbObj.FileGroups)
                        {
                            fg.Files.Refresh();
                            foreach (DataFile df in fg.Files)
                                if (!string.IsNullOrEmpty(df.FileName))
                                    dbFiles.Add(df.FileName);
                        }

                        dbObj.LogFiles.Refresh();
                        foreach (LogFile lf in dbObj.LogFiles)
                            if (!string.IsNullOrEmpty(lf.FileName))
                                dbFiles.Add(lf.FileName);
                    }, ct);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex,
                        "VSS: DB dosya yolları alınamadı — {Database}", databaseName);
                    return false;
                }

                if (dbFiles.Count == 0)
                {
                    Log.Warning("VSS: SMO'dan dosya listesi boş — {Database}", databaseName);
                    return false;
                }

                Log.Information(
                    "VSS başlıyor: {Database} — {FileCount} dosya tespit edildi",
                    databaseName, dbFiles.Count);

                foreach (string f in dbFiles)
                {
                    long fSize = 0;
                    try { fSize = new FileInfo(f).Length; } catch { }
                    Log.Information(
                        "  DB dosyası: {FileName} [{SizeMb:F1} MB] ({FullPath})",
                        Path.GetFileName(f), fSize / BytesPerMb, f);
                }

                // 2. Her volume için snapshot oluştur
                foreach (string vol in dbFiles
                    .Select(f => Path.GetPathRoot(f))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(v => v is not null))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        // CreateSnapshot bloke edici VSS çağrıları içerir (GatherWriterMetadata vb.)
                        // Task.Run ile thread pool'a taşı; ct'yi ileterek adımlar arası erken çıkış sağla
                        volumeSnapshots[vol] = await Task.Run(() => _vssService.CreateSnapshot(vol, ct), CancellationToken.None);
                        Log.Information("VSS snapshot oluşturuldu: {Volume}", vol);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Log.Warning(ex,
                            "VSS snapshot başarısız: {Volume} " +
                            "(uygulamanın yönetici olarak çalışması gerekebilir)", vol);
                    }
                }

                // Snapshot oluşturulamadıysa fallback'e bırak
                if (volumeSnapshots.Count == 0)
                {
                    Log.Warning(
                        "VSS: Hiçbir snapshot alınamadı — " +
                        "SQL dosyaları kilitli olduğundan doğrudan kopyalama da başarısız olur. " +
                        "COPY_ONLY fallback'e geçiliyor.");
                    return false;
                }

                // 3. Dosyaları staging dizinine kopyala (yalnızca VSS yolu üzerinden)
                Directory.CreateDirectory(stagingDir);
                var copiedFiles = new List<string>();

                foreach (string srcFile in dbFiles)
                {
                    ct.ThrowIfCancellationRequested();

                    string vol = Path.GetPathRoot(srcFile);
                    if (vol is null || !volumeSnapshots.TryGetValue(vol, out var snapId))
                    {
                        Log.Warning("VSS snapshot yok: {File}", srcFile);
                        continue;
                    }

                    string destFile = Path.Combine(stagingDir, Path.GetFileName(srcFile));
                    try
                    {
                        string vssPath = _vssService.GetSnapshotFilePath(snapId, srcFile);
                        await CopyFileToStagingAsync(vssPath, destFile, ct);
                        copiedFiles.Add(destFile);

                        long copiedSize = 0;
                        try { copiedSize = new FileInfo(destFile).Length; } catch { }
                        Log.Information(
                            "  ✓ Kopyalandı: {File} [{SizeMb:F1} MB]",
                            Path.GetFileName(srcFile), copiedSize / BytesPerMb);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "  ✗ Kopyalama başarısız: {File}", Path.GetFileName(srcFile));
                    }
                }

                if (copiedFiles.Count == 0)
                {
                    Log.Warning(
                        "VSS: Snapshot oluşturuldu ama hiçbir dosya kopyalanamadı — {Database}",
                        databaseName);
                    return false;
                }

                // 4. Sıkıştır
                string archiveName = PathHelper.GenerateArchiveFileName(databaseName, "VSS");
                string archivePath = Path.Combine(destinationPath, archiveName);

                long rawTotal = copiedFiles.Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
                Log.Information(
                    "7z arşivi oluşturuluyor: {N} dosya, toplam {RawMb:F1} MB → {Archive}",
                    copiedFiles.Count, rawTotal / BytesPerMb, archiveName);

                long archiveSize = await _compressionService.CompressMultipleAsync(
                    copiedFiles.ToArray(),
                    archivePath,
                    password: null,
                    level: CompressionLevel.Normal,
                    progress: null,
                    cancellationToken: ct);

                result.VssFileCopyPath = archivePath;
                result.VssFileCopySizeBytes = archiveSize;

                double comprRatio = rawTotal > 0 ? (double)archiveSize / rawTotal * 100.0 : 100.0;
                Log.Information(
                    "VSS tamamlandı: {Database} → {Archive} [{SizeMb:F1} MB] " +
                    "(kaynak: {RawMb:F1} MB → oran: %{Ratio:F0}, {N}/{Total} dosya)",
                    databaseName, archiveName,
                    archiveSize / BytesPerMb, rawTotal / BytesPerMb, comprRatio,
                    copiedFiles.Count, dbFiles.Count);

                Log.Information(
                    "  Arşiv içeriği: {Files}",
                    string.Join(", ", copiedFiles.Select(Path.GetFileName)));

                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Warning(ex, "VSS başarısız: {Database}", databaseName);
                return false;
            }
            finally
            {
                foreach (var kv in volumeSnapshots)
                {
                    try
                    {
                        _vssService.DeleteSnapshot(kv.Value);
                        Log.Debug("VSS snapshot silindi: {Volume} (ID: {Id})", kv.Key, kv.Value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "VSS snapshot silinemedi: {Volume}", kv.Key);
                    }
                }

                try
                {
                    if (Directory.Exists(stagingDir))
                    {
                        int tmpCount = Directory.GetFiles(stagingDir).Length;
                        Directory.Delete(stagingDir, recursive: true);
                        Log.Debug(
                            "VSS staging temizlendi: {Dir} ({N} geçici dosya silindi)",
                            stagingDir, tmpCount);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "VSS staging dizini silinemedi: {Dir}", stagingDir);
                }
            }
        }

        /// <summary>
        /// VSS başarısız olduğunda COPY_ONLY SQL backup alır ve sıkıştırır.
        /// CopyOnly=true — differential zincirini (baseline) bozmaz.
        /// </summary>
        private async Task TrySqlCopyOnlyFallbackAsync(
            Server server,
            string databaseName,
            string destinationPath,
            BackupResult result,
            CancellationToken ct)
        {
            string extraDir = Path.Combine(destinationPath, "_express_extra");
            string bakFileName = PathHelper.GenerateBackupFileName(databaseName, "CopyOnly");
            string bakPath = Path.Combine(extraDir, bakFileName);

            try
            {
                Directory.CreateDirectory(extraDir);

                var backup = new Microsoft.SqlServer.Management.Smo.Backup
                {
                    Action = BackupActionType.Database,
                    Database = databaseName,
                    CopyOnly = true,
                    Incremental = false
                };
                backup.Devices.AddDevice(bakPath, DeviceType.File);

                using var abortReg = ct.Register(backup.Abort);
                try
                {
                    await Task.Run(() => backup.SqlBackup(server), CancellationToken.None);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(
                        "COPY_ONLY backup iptal edildi.", ex, ct);
                }
                ct.ThrowIfCancellationRequested();

                long bakSize = 0;
                try { bakSize = new FileInfo(bakPath).Length; } catch { }
                Log.Information(
                    "COPY_ONLY .bak alındı: {BakFile} [{SizeMb:F1} MB]",
                    bakFileName, bakSize / BytesPerMb);

                // Sıkıştır (tek dosya → ICompressionService.CompressAsync)
                string archiveName = PathHelper.GenerateArchiveFileName(databaseName, "VSS");
                string archivePath = Path.Combine(destinationPath, archiveName);

                Log.Information(
                    "7z sıkıştırılıyor: {BakFile} → {Archive}",
                    bakFileName, archiveName);

                long archiveSize = await _compressionService.CompressAsync(
                    bakPath, archivePath, password: null, progress: null, cancellationToken: ct);

                result.VssFileCopyPath = archivePath;
                result.VssFileCopySizeBytes = archiveSize;

                double ratio = bakSize > 0 ? (double)archiveSize / bakSize * 100.0 : 100.0;
                Log.Information(
                    "Express COPY_ONLY tamamlandı: {Database} → {Archive} [{SizeMb:F1} MB] " +
                    "(kaynak: {BakMb:F1} MB → oran: %{Ratio:F0})",
                    databaseName, archiveName,
                    archiveSize / BytesPerMb, bakSize / BytesPerMb, ratio);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Warning(ex,
                    "Express COPY_ONLY fallback başarısız (ana yedek etkilenmedi): {Database}",
                    databaseName);
            }
            finally
            {
                if (File.Exists(bakPath))
                {
                    TryDeleteFile(bakPath);
                    Log.Debug("Geçici .bak silindi: {BakFile}", bakFileName);
                }

                try
                {
                    if (Directory.Exists(extraDir) &&
                        !Directory.EnumerateFileSystemEntries(extraDir).Any())
                        Directory.Delete(extraDir);
                }
                catch { }
            }
        }

        /// <summary>Dosyayı asenkron olarak hedef yola kopyalar.</summary>
        private static async Task CopyFileToStagingAsync(
            string sourcePath, string destPath, CancellationToken ct)
        {
            const int bufferSize = 81920; // 80 KB
            using var src = new FileStream(
                sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync: true);
            using var dst = new FileStream(
                destPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
            await src.CopyToAsync(dst, bufferSize, ct).ConfigureAwait(false);
        }

        #endregion
    }
}
