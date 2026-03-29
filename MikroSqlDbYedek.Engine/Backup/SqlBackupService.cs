using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Serilog;
using MikroSqlDbYedek.Core.Helpers;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using SqlConnInfo = MikroSqlDbYedek.Core.Models.SqlConnectionInfo;

namespace MikroSqlDbYedek.Engine.Backup
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
                string fileName = PathHelper.GenerateBackupFileName(databaseName, backupType.ToString());
                string fullPath = Path.Combine(destinationPath, fileName);

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

                // ── Backup with retry ────────────────────────────────────
                var backup = new Microsoft.SqlServer.Management.Smo.Backup
                {
                    Action = BackupActionType.Database,
                    Database = databaseName,
                    Incremental = backupType == SqlBackupType.Differential,
                    CopyOnly = backupType != SqlBackupType.Incremental
                };

                if (backupType == SqlBackupType.Incremental)
                {
                    backup.Action = BackupActionType.Log;
                    backup.Incremental = false;
                }

                backup.Devices.AddDevice(fullPath, DeviceType.File);
                backup.PercentComplete += (sender, e) =>
                {
                    progress?.Report(e.Percent);
                };

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
                    databaseName, backupType, fullPath, fileInfo.Length / BytesPerMb);
            }
            catch (OperationCanceledException)
            {
                result.Status = BackupResultStatus.Cancelled;
                result.ErrorMessage = "Yedekleme işlemi kullanıcı tarafından iptal edildi.";
                result.CompletedAt = DateTime.UtcNow;
                Log.Warning("Yedekleme iptal edildi: {Database} ({BackupType})", databaseName, backupType);
            }
            catch (Exception ex)
            {
                result.Status = BackupResultStatus.Failed;
                result.ErrorMessage = TranslateBackupError(ex);
                result.CompletedAt = DateTime.UtcNow;
                Log.Error(ex, "Yedekleme başarısız: {Database} ({BackupType})", databaseName, backupType);
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

                        databases.Add(new DatabaseInfo
                        {
                            Name = db.Name,
                            SizeInMb = db.Size,
                            Status = db.Status.ToString(),
                            RecoveryModel = db.RecoveryModel.ToString(),
                            LastFullBackupDate = db.LastBackupDate == DateTime.MinValue
                                ? "Hiç"
                                : db.LastBackupDate.ToString("yyyy-MM-dd HH:mm"),
                            IsSystemDb = db.IsSystemObject
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
                    await Task.Run(action, cancellationToken);
                    return;
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
