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
    public partial class SqlBackupService : ISqlBackupService
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

                // Çalışan hesabın SQL Server erişimini kontrol et (servis modunda)
                EnsureSystemLoginPermission(connectionInfo);

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

                // SQL Server Express sıkıştırmayı desteklemez — sadece non-Express'te etkinleştir
                if (!isExpress)
                {
                    backup.CompressionOption = BackupCompressionOptions.On;
                    Log.Debug("SQL native sıkıştırma etkin: {Database} ({Edition})", databaseName, sqlEdition);
                }
                else
                {
                    Log.Debug("SQL native sıkıştırma atlandı (Express): {Database}", databaseName);
                }

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
    }
}
