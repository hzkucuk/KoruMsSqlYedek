using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Compression;
using SqlConnInfo = KoruMsSqlYedek.Core.Models.SqlConnectionInfo;

namespace KoruMsSqlYedek.Engine.Backup
{
    /// <summary>
    /// SMO tabanlı SQL Server yedekleme ve restore servisi.
    /// </summary>
    public class SqlBackupService : ISqlBackupService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SqlBackupService>();
        private const double BytesPerMb = 1048576.0;

        /// <summary>Geçici hatalarda yeniden deneme sayısı.</summary>
        private const int MaxRetryCount = 3;

        /// <summary>Yeniden denemeler arası temel bekleme süresi (ms).</summary>
        private const int RetryBaseDelayMs = 2000;

        private readonly IVssService _vssService;
        private readonly SevenZipCompressionService _compressionService;

        /// <summary>
        /// Autofac constructor injection.
        /// VSS ve sıkıştırma servisleri opsiyoneldir; null ise Express VSS kopyası atlanır.
        /// </summary>
        public SqlBackupService(
            IVssService vssService = null,
            SevenZipCompressionService compressionService = null)
        {
            _vssService = vssService;
            _compressionService = compressionService;
        }

        public async Task<BackupResult> BackupDatabaseAsync(
            SqlConnInfo connectionInfo,
            string databaseName,
            SqlBackupType backupType,
            string destinationPath,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            var result = new BackupResult
            {
                DatabaseName = databaseName,
                BackupType = backupType,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                Directory.CreateDirectory(destinationPath);

                using var sqlConn1 = new SqlConnection(BuildConnectionString(connectionInfo));
                var serverConnection = new ServerConnection(sqlConn1);
                var server = new Server(serverConnection);

                // ── Pre-backup health check ──────────────────────────────
                Database dbObj = server.Databases[databaseName];
                if (dbObj == null)
                {
                    result.Status = BackupResultStatus.Failed;
                    result.ErrorMessage = $"'{databaseName}' veritabanı SQL Server üzerinde bulunamadı.";
                    result.CompletedAt = DateTime.UtcNow;
                    Log.Warning("Veritabanı bulunamadı: {Database}", databaseName);
                    return result;
                }

                if (dbObj.Status != DatabaseStatus.Normal)
                {
                    string statusText = dbObj.Status.ToString();
                    result.Status = BackupResultStatus.Failed;
                    result.ErrorMessage =
                        $"'{databaseName}' veritabanı yedeklenmeye uygun değil. " +
                        $"Mevcut durum: {statusText}. " +
                        "Veritabanının Online (Normal) durumda olduğundan emin olun.";
                    result.CompletedAt = DateTime.UtcNow;
                    Log.Warning("Veritabanı durumu uygun değil: {Database} — {Status}", databaseName, statusText);
                    return result;
                }

                // ── Edition tespiti & recovery model uyumluluk kontrolü ──
                string sqlEdition = "Bilinmiyor";
                try { sqlEdition = server.Information.Edition ?? sqlEdition; } catch { }
                bool isExpress = sqlEdition.IndexOf("Express", StringComparison.OrdinalIgnoreCase) >= 0;
                Log.Debug("SQL Server bağlantısı: {Edition}", sqlEdition);

                var effectiveBackupType = backupType;
                if (backupType == SqlBackupType.Incremental)
                {
                    RecoveryModel rm = RecoveryModel.Simple;
                    try { rm = dbObj.RecoveryModel; } catch { }
                    if (rm == RecoveryModel.Simple)
                    {
                        string editionNote = isExpress
                            ? " (Express — varsayılan Simple recovery model)"
                            : string.Empty;
                        Log.Warning(
                            "'{Database}' Simple recovery model kullanıyor{EditionNote}; " +
                            "transaction log yedeği desteklenmez. Full yedeke otomatik yükseltildi. " +
                            "Log yedek için: ALTER DATABASE [{Database}] SET RECOVERY FULL",
                            databaseName, editionNote, databaseName);
                        effectiveBackupType = SqlBackupType.Full;
                    }
                }

                // Etkin tür result'a ve dosya adına yansıtılır
                result.BackupType = effectiveBackupType;
                string fileName = PathHelper.GenerateBackupFileName(databaseName, effectiveBackupType.ToString());
                string fullPath = Path.Combine(destinationPath, fileName);

                // ── Backup with retry ────────────────────────────────────
                var backup = new Microsoft.SqlServer.Management.Smo.Backup
                {
                    Action = BackupActionType.Database,
                    Database = databaseName,
                    Incremental = effectiveBackupType == SqlBackupType.Differential,
                    CopyOnly = false   // Zamanlanmış yedekler differential baseline'ı güncellemelidir
                };

                if (effectiveBackupType == SqlBackupType.Incremental)
                {
                    backup.Action = BackupActionType.Log;
                    backup.Incremental = false;
                }

                backup.Devices.AddDevice(fullPath, DeviceType.File);
                backup.PercentComplete += (sender, e) =>
                {
                    progress?.Report(e.Percent);
                };

                // İptal sinyali gelince backup.Abort() çağrılır; SqlBackup() SmoException fırlatır
                // ExecuteWithRetryAsync bunu OperationCanceledException'a çevirir
                using var abortReg = cancellationToken.Register(backup.Abort);
                await ExecuteWithRetryAsync(
                    () => backup.SqlBackup(server),
                    databaseName, fullPath, cancellationToken);

                var fileInfo = new FileInfo(fullPath);
                result.BackupFilePath = fullPath;
                result.FileSizeBytes = fileInfo.Length;
                result.Status = BackupResultStatus.Success;
                result.CompletedAt = DateTime.UtcNow;

                Log.Information(
                    "Yedekleme başarılı: {Database} ({BackupType}) → {FilePath} [{SizeMb:F1} MB]",
                    databaseName, effectiveBackupType, fullPath, fileInfo.Length / BytesPerMb);

                // Tüm edition'larda ek güvenlik olarak VSS üzerinden MDF/LDF dosya kopyası al.
                // Başarısız olursa sessizce atlanır; .bak yedek zaten mevcut.
                await TryExpressVssBackupAsync(
                    server, dbObj, databaseName, destinationPath, result, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                result.Status = BackupResultStatus.Cancelled;
                result.ErrorMessage = "Yedekleme işlemi kullanıcı tarafından iptal edildi.";
                result.CompletedAt = DateTime.UtcNow;
                Log.Warning("Yedekleme iptal edildi: {Database} ({BackupType})", databaseName, result.BackupType);
            }
            catch (Exception ex)
            {
                result.Status = BackupResultStatus.Failed;
                result.ErrorMessage = TranslateBackupError(ex);
                result.CompletedAt = DateTime.UtcNow;
                Log.Error(ex, "Yedekleme başarısız: {Database} ({BackupType})", databaseName, result.BackupType);
            }

            return result;
        }

        public async Task<bool> VerifyBackupAsync(
            SqlConnInfo connectionInfo,
            string backupFilePath,
            CancellationToken cancellationToken)
        {
            try
            {
                using var sqlConn2 = new SqlConnection(BuildConnectionString(connectionInfo));
                var serverConnection = new ServerConnection(sqlConn2);
                var server = new Server(serverConnection);

                var restore = new Restore();
                restore.Devices.AddDevice(backupFilePath, DeviceType.File);

                bool isValid = await Task.Run(
                    () => restore.SqlVerify(server),
                    cancellationToken);

                Log.Information(
                    "Yedek doğrulama {Result}: {FilePath}",
                    isValid ? "başarılı" : "başarısız",
                    backupFilePath);

                return isValid;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Yedek doğrulama hatası: {FilePath}", backupFilePath);
                return false;
            }
        }

        public async Task<bool> RestoreDatabaseAsync(
            SqlConnInfo connectionInfo,
            string databaseName,
            string backupFilePath,
            bool createPreRestoreBackup,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                if (createPreRestoreBackup)
                {
                    Log.Information("Restore öncesi güvenlik yedeği alınıyor: {Database}", databaseName);
                    string safetyDir = Path.Combine(
                        Path.GetDirectoryName(backupFilePath),
                        "PreRestore");

                    await BackupDatabaseAsync(
                        connectionInfo,
                        databaseName,
                        SqlBackupType.Full,
                        safetyDir,
                        null,
                        cancellationToken);
                }

                using var sqlConn3 = new SqlConnection(BuildConnectionString(connectionInfo));
                var serverConnection = new ServerConnection(sqlConn3);
                var server = new Server(serverConnection);

                var restore = new Restore
                {
                    Database = databaseName,
                    ReplaceDatabase = true,
                    NoRecovery = false
                };

                restore.Devices.AddDevice(backupFilePath, DeviceType.File);
                restore.PercentComplete += (sender, e) =>
                {
                    progress?.Report(e.Percent);
                };

                await Task.Run(() => restore.SqlRestore(server), cancellationToken);

                Log.Information("Restore başarılı: {Database} ← {FilePath}", databaseName, backupFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Restore başarısız: {Database} ← {FilePath}", databaseName, backupFilePath);
                return false;
            }
        }

        public async Task<List<DatabaseInfo>> ListDatabasesAsync(
            SqlConnInfo connectionInfo,
            CancellationToken cancellationToken)
        {
            var databases = new List<DatabaseInfo>();

            try
            {
                using var sqlConn4 = new SqlConnection(BuildConnectionString(connectionInfo));
                var serverConnection = new ServerConnection(sqlConn4);
                var server = new Server(serverConnection);

                await Task.Run(() =>
                {
                    foreach (Database db in server.Databases)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        double sizeInMb = 0;
                        string status = "Unknown";
                        string recoveryModel = "Unknown";
                        string lastBackupDate = "Hiç";
                        bool isSystemDb = false;

                        try { sizeInMb = db.Size; } catch { }
                        try { status = db.Status.ToString(); } catch { }
                        try { recoveryModel = db.RecoveryModel.ToString(); } catch { }
                        try
                        {
                            lastBackupDate = db.LastBackupDate == DateTime.MinValue
                                ? "Hiç"
                                : db.LastBackupDate.ToString("yyyy-MM-dd HH:mm");
                        }
                        catch { }
                        try { isSystemDb = db.IsSystemObject; } catch { }

                        databases.Add(new DatabaseInfo
                        {
                            Name = db.Name,
                            SizeInMb = sizeInMb,
                            Status = status,
                            RecoveryModel = recoveryModel,
                            LastFullBackupDate = lastBackupDate,
                            IsSystemDb = isSystemDb
                        });
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Veritabanı listesi alınamadı: {Server}", connectionInfo.Server);
                throw;
            }

            return databases;
        }

        public async Task<bool> TestConnectionAsync(
            SqlConnInfo connectionInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(BuildConnectionString(connectionInfo)))
                {
                    await Task.Run(() => sqlConnection.Open(), cancellationToken);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SQL Server bağlantı testi başarısız: {Server}", connectionInfo.Server);
                return false;
            }
        }

        public async Task<SqlServerEditionInfo> GetServerEditionAsync(
            SqlConnInfo connectionInfo,
            CancellationToken cancellationToken)
        {
            var info = new SqlServerEditionInfo();
            try
            {
                using var sqlConn = new SqlConnection(BuildConnectionString(connectionInfo));
                var serverConn = new ServerConnection(sqlConn);
                var server = new Server(serverConn);

                await Task.Run(() =>
                {
                    try { info.Edition = server.Information.Edition ?? string.Empty; } catch { }
                    try { info.Version = server.Information.VersionString ?? string.Empty; } catch { }
                }, cancellationToken);

                info.IsExpress = info.Edition.IndexOf("Express", StringComparison.OrdinalIgnoreCase) >= 0;

                Log.Information(
                    "SQL Server edition tespit edildi: {Edition} v{Version} (Express={IsExpress})",
                    info.Edition, info.Version, info.IsExpress);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SQL Server edition bilgisi alınamadı: {Server}", connectionInfo.Server);
            }

            return info;
        }

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

        #region Retry & Error Helpers

        /// <summary>
        /// Geçici hatalarda otomatik yeniden deneme ile çalıştırır.
        /// </summary>
        private async Task ExecuteWithRetryAsync(
            Action action, string databaseName, string filePath,
            CancellationToken cancellationToken)
        {
            int attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    // ct yalnızca task başlamadan önce kontrol edilir; çalışırken Abort() gerekli
                    await Task.Run(action, cancellationToken);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (cancellationToken.IsCancellationRequested)
                {
                    // backup.Abort() çağrısı SmoException fırlatır — OperationCanceledException'a çevir
                    throw new OperationCanceledException(
                        "Yedekleme kullanıcı tarafından iptal edildi.", ex, cancellationToken);
                }
                catch (Exception ex) when (attempt < MaxRetryCount && IsTransientError(ex))
                {
                    Log.Warning(
                        "Geçici hata, yeniden deneniyor ({Attempt}/{MaxRetry}): {Database} — {Error}",
                        attempt, MaxRetryCount, databaseName, ExtractInnermostMessage(ex));

                    TryDeleteFile(filePath);
                    await Task.Delay(RetryBaseDelayMs * attempt, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Exception zincirinde geçici (transient) hata olup olmadığını kontrol eder.
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            string msg = ExtractInnermostMessage(ex).ToLowerInvariant();
            return msg.Contains("operating system error 32")
                || msg.Contains("sharing violation")
                || msg.Contains("timeout")
                || msg.Contains("the semaphore timeout period has expired");
        }

        /// <summary>
        /// SMO exception zincirinden en içteki (asıl) hata mesajını çıkarır.
        /// </summary>
        private static string ExtractInnermostMessage(Exception ex)
        {
            var inner = ex;
            while (inner.InnerException != null)
                inner = inner.InnerException;
            return inner.Message;
        }

        /// <summary>
        /// Bilinen SQL/SMO hata kalıpları için Türkçe açıklama üretir.
        /// </summary>
        private static string TranslateBackupError(Exception ex)
        {
            string innerMsg = ExtractInnermostMessage(ex);
            string lowerMsg = innerMsg.ToLowerInvariant();

            if (lowerMsg.Contains("operating system error 32") || lowerMsg.Contains("sharing violation"))
                return $"Veritabanı dosyası başka bir işlem tarafından kullanılıyor. " +
                       $"Mikro yazılımını kapatıp tekrar deneyin. (Detay: {innerMsg})";

            if (lowerMsg.Contains("cannot be opened") && lowerMsg.Contains("inaccessible"))
                return $"Veritabanı dosyalarına erişilemiyor. MDF/LDF dosyalarının SQL Server tarafından " +
                       $"erişilebilir olduğunu kontrol edin. (Detay: {innerMsg})";

            if (lowerMsg.Contains("insufficient disk space") || lowerMsg.Contains("not enough space on the disk"))
                return $"Yetersiz disk alanı. Yedek dizininde yeterli boş alan olduğundan emin olun. (Detay: {innerMsg})";

            if (lowerMsg.Contains("insufficient memory") || lowerMsg.Contains("not enough memory"))
                return $"Yetersiz bellek. SQL Server'ın yeterli RAM'e sahip olduğundan emin olun. (Detay: {innerMsg})";

            if (lowerMsg.Contains("access is denied") || lowerMsg.Contains("operating system error 5"))
                return $"Erişim reddedildi. SQL Server servis hesabının yedek dizinine yazma yetkisi " +
                       $"olduğundan emin olun. (Detay: {innerMsg})";

            if (lowerMsg.Contains("is not accessible") || lowerMsg.Contains("offline"))
                return $"Veritabanı çevrimdışı veya erişilemez durumda. " +
                       $"SQL Server Management Studio'dan veritabanı durumunu kontrol edin. (Detay: {innerMsg})";

            // Bilinmeyen hata — SMO sarmalayıcısı yerine asıl mesajı göster
            return innerMsg;
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Başarısız yedek dosyası silinemedi: {Path}", path);
            }
        }

        #endregion

        private string BuildConnectionString(SqlConnInfo connectionInfo)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = connectionInfo.Server,
                ConnectTimeout = connectionInfo.ConnectionTimeoutSeconds,
                TrustServerCertificate = connectionInfo.TrustServerCertificate,
                Encrypt = connectionInfo.TrustServerCertificate
                    ? SqlConnectionEncryptOption.Optional
                    : SqlConnectionEncryptOption.Mandatory
            };

            if (connectionInfo.AuthMode == SqlAuthMode.Windows)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = connectionInfo.Username;
                builder.Password = PasswordProtector.Unprotect(connectionInfo.Password);
            }

            return builder.ConnectionString;
        }
    }
}
