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

                if (isValid)
                {
                    Log.Information("Yedek doğrulama başarılı: {FilePath}", backupFilePath);
                }
                else
                {
                    // RESTORE VERIFYONLY çalıştı ve dosyayı GEÇERSİZ buldu (bozuk/eksik yedek)
                    Log.Error("Yedek doğrulama başarısız — RESTORE VERIFYONLY olumsuz sonuç verdi: {FilePath}",
                        backupFilePath);
                }

                return isValid;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                // Doğrulama hiç ÇALIŞTIRILAMADI (bağlantı, yetki, dosya erişimi vb.).
                // Bozuk yedek ile aynı şekilde "doğrulanmamış" kabul edilir (fail-closed);
                // istisna ayrıntısı burada loglanır ki iki durum log üzerinden ayırt edilebilsin.
                Log.Error(ex, "Yedek doğrulama çalıştırılamadı (doğrulama sonucu bilinmiyor): {FilePath}",
                    backupFilePath);
                return false;
            }
        }

        public async Task<string> ReadBackupDatabaseNameAsync(
            SqlConnInfo connectionInfo,
            string backupFilePath,
            CancellationToken cancellationToken)
        {
            try
            {
                using var sqlConn = new SqlConnection(BuildConnectionString(connectionInfo));
                var serverConnection = new ServerConnection(sqlConn);
                var server = new Server(serverConnection);

                var restore = new Restore();
                restore.Devices.AddDevice(backupFilePath, DeviceType.File);

                string dbName = await Task.Run(() =>
                {
                    var header = restore.ReadBackupHeader(server);
                    if (header == null || header.Rows.Count == 0 || !header.Columns.Contains("DatabaseName"))
                        return null;
                    return header.Rows[0]["DatabaseName"] as string;
                }, cancellationToken);

                Log.Information("Yedek başlığı okundu: {FilePath} → DatabaseName={Database}",
                    backupFilePath, dbName ?? "(boş)");
                return string.IsNullOrWhiteSpace(dbName) ? null : dbName;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Warning(ex, "Yedek başlığı okunamadı: {FilePath}", backupFilePath);
                return null;
            }
        }

        public async Task<bool> RestoreDatabaseAsync(
            SqlConnInfo connectionInfo,
            string databaseName,
            string backupFilePath,
            bool createPreRestoreBackup,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            string safetyBackupDirectory = null)
        {
            try
            {
                if (createPreRestoreBackup)
                {
                    // Güvenlik yedeği KALICI bir dizine yazılmalı. backupFilePath geçici bir
                    // arşiv çıkarma dizininde olabilir (RestoreDialog .7z'yi %TEMP%'e açar ve
                    // işlem sonunda siler); bu yüzden çağıran açıkça kalıcı bir dizin verir.
                    string safetyDir = !string.IsNullOrWhiteSpace(safetyBackupDirectory)
                        ? safetyBackupDirectory
                        : Path.Combine(Path.GetDirectoryName(backupFilePath) ?? string.Empty, "PreRestore");

                    Log.Information("Restore öncesi güvenlik yedeği alınıyor: {Database} → {Dir}",
                        databaseName, safetyDir);

                    var safetyResult = await BackupDatabaseAsync(
                        connectionInfo,
                        databaseName,
                        SqlBackupType.Full,
                        safetyDir,
                        null,
                        cancellationToken);

                    if (safetyResult == null || safetyResult.Status != BackupResultStatus.Success)
                    {
                        // Güvenlik yedeği alınamadıysa mevcut DB'nin üzerine yazmak geri dönüşsüz olur.
                        Log.Error(
                            "Restore iptal edildi: güvenlik yedeği alınamadı — {Database} ({Error})",
                            databaseName, safetyResult?.ErrorMessage ?? "bilinmeyen hata");
                        return false;
                    }

                    Log.Information("Güvenlik yedeği alındı: {File}", safetyResult.BackupFilePath);
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
                const string query = """
                    SELECT
                        d.name,
                        CAST(COALESCE(SUM(CAST(mf.size AS bigint)) * 8.0 / 1024.0, 0) AS float) AS size_mb,
                        d.state_desc,
                        d.recovery_model_desc,
                        CASE WHEN d.database_id <= 4 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS is_system_db
                    FROM sys.databases AS d
                    LEFT JOIN sys.master_files AS mf ON d.database_id = mf.database_id
                    GROUP BY d.database_id, d.name, d.state_desc, d.recovery_model_desc
                    ORDER BY CASE WHEN d.database_id <= 4 THEN 0 ELSE 1 END, d.name;
                    """;

                await using var sqlConn4 = new SqlConnection(BuildConnectionString(connectionInfo));
                await sqlConn4.OpenAsync(cancellationToken);

                await using var cmd = new SqlCommand(query, sqlConn4)
                {
                    CommandTimeout = connectionInfo.ConnectionTimeoutSeconds
                };

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    databases.Add(new DatabaseInfo
                    {
                        Name = reader.GetString(0),
                        SizeInMb = reader.GetDouble(1),
                        Status = reader.GetString(2),
                        RecoveryModel = reader.GetString(3),
                        LastFullBackupDate = "Hiç",
                        IsSystemDb = reader.GetBoolean(4)
                    });
                }
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
