using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Serilog;
using KoruMsSqlYedek.Core.Models;
using SqlConnInfo = KoruMsSqlYedek.Core.Models.SqlConnectionInfo;

namespace KoruMsSqlYedek.Engine.Backup
{
    public partial class SqlBackupService
    {
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
    }
}
