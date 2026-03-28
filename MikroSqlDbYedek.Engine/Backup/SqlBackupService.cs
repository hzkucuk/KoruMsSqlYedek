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

                var serverConnection = CreateServerConnection(connectionInfo);
                var server = new Server(serverConnection);

                    var backup = new Microsoft.SqlServer.Management.Smo.Backup
                    {
                        Action = BackupActionType.Database,
                        Database = databaseName,
                        Incremental = backupType == SqlBackupType.Differential
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

                    await Task.Run(() => backup.SqlBackup(server), cancellationToken);

                    var fileInfo = new FileInfo(fullPath);
                    result.BackupFilePath = fullPath;
                    result.FileSizeBytes = fileInfo.Length;
                    result.Status = BackupResultStatus.Success;
                    result.CompletedAt = DateTime.UtcNow;

                    Log.Information(
                        "Yedekleme başarılı: {Database} ({BackupType}) → {FilePath} [{SizeMb:F1} MB]",
                        databaseName, backupType, fullPath, fileInfo.Length / 1048576.0);
            }
            catch (Exception ex)
            {
                result.Status = BackupResultStatus.Failed;
                result.ErrorMessage = ex.Message;
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
                var serverConnection = CreateServerConnection(connectionInfo);
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

                var serverConnection = CreateServerConnection(connectionInfo);
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
                var serverConnection = CreateServerConnection(connectionInfo);
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

        private ServerConnection CreateServerConnection(SqlConnInfo connectionInfo)
        {
            var serverConnection = new ServerConnection
            {
                ServerInstance = connectionInfo.Server,
                ConnectTimeout = connectionInfo.ConnectionTimeoutSeconds
            };

            if (connectionInfo.AuthMode == SqlAuthMode.Windows)
            {
                serverConnection.LoginSecure = true;
            }
            else
            {
                serverConnection.LoginSecure = false;
                serverConnection.Login = connectionInfo.Username;
                serverConnection.Password = PasswordProtector.Unprotect(connectionInfo.Password);
            }

            return serverConnection;
        }

        private string BuildConnectionString(SqlConnInfo connectionInfo)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = connectionInfo.Server,
                ConnectTimeout = connectionInfo.ConnectionTimeoutSeconds
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
