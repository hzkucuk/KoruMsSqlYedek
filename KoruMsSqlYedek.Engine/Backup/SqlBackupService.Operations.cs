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

        /// <summary>SQL 3201: "Cannot open backup device ... Operating system error N".</summary>
        private const int SqlErrorCannotOpenBackupDevice = 3201;

        public async Task<BackupPathAccessResult> CheckBackupPathWritableAsync(
            SqlConnInfo connectionInfo,
            string directoryPath,
            CancellationToken cancellationToken)
        {
            var result = new BackupPathAccessResult { Path = directoryPath };
            if (connectionInfo == null || string.IsNullOrWhiteSpace(connectionInfo.Server)
                || string.IsNullOrWhiteSpace(directoryPath))
                return result;

            string probePath = Path.Combine(directoryPath, $"_KoruYazmaTesti_{Guid.NewGuid():N}.bak");
            bool probeWritten = false;

            try
            {
                using var conn = new SqlConnection(BuildConnectionString(connectionInfo));
                await conn.OpenAsync(cancellationToken);

                result.InstanceName = await TryScalarAsync(conn, "SELECT @@SERVERNAME", cancellationToken);
                // VIEW SERVER STATE gerektirir; yoksa hesap adı mesajda yer almaz
                result.ServiceAccount = await TryScalarAsync(conn,
                    "SELECT TOP 1 service_account FROM sys.dm_server_services WHERE filename LIKE '%sqlservr.exe%'",
                    cancellationToken);

                // Yedeklemedeki gibi önce bu süreç oluşturur (BackupDatabaseAsync de öyle yapar), ardından
                // SQL Server hesabıyla denenir; olmazsa BACKUP hatası (OS error 3/5) karar verir
                try { Directory.CreateDirectory(directoryPath); }
                catch (Exception ex) { Log.Debug(ex, "Yedek dizini oluşturulamadı: {Path}", directoryPath); }

                try
                {
                    using var mkdir = new SqlCommand("EXEC master.sys.xp_create_subdir @dir", conn);
                    mkdir.Parameters.AddWithValue("@dir", directoryPath);
                    await mkdir.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    Log.Debug(ex, "xp_create_subdir başarısız: {Path}", directoryPath);
                }

                using (var backup = new SqlCommand(
                    "BACKUP DATABASE [model] TO DISK = @path WITH COPY_ONLY, INIT, FORMAT", conn))
                {
                    backup.CommandTimeout = 120;
                    backup.Parameters.AddWithValue("@path", probePath);
                    await backup.ExecuteNonQueryAsync(cancellationToken);
                }

                probeWritten = true;
                result.IsWritable = true;
                Log.Information("Yedek dizini yazma testi başarılı: {Instance} → {Path}",
                    result.InstanceName, directoryPath);

                try
                {
                    using var del = new SqlCommand("EXEC master.sys.xp_delete_file 0, @path", conn);
                    del.Parameters.AddWithValue("@path", probePath);
                    await del.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException ex)
                {
                    Log.Debug(ex, "Deneme yedeği SQL ile silinemedi: {Path}", probePath);
                }
            }
            catch (SqlException ex) when (ContainsError(ex, SqlErrorCannotOpenBackupDevice))
            {
                result.IsWritable = false;
                result.ErrorMessage = FirstErrorMessage(ex, SqlErrorCannotOpenBackupDevice);
                Log.Warning("SQL Server yedek dizinine yazamıyor: {Instance} ({Account}) → {Path} — {Error}",
                    result.InstanceName, result.ServiceAccount, directoryPath, result.ErrorMessage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Bağlantı/yetki vb. — test sonuçsuz; asıl yedekleme kendi hatasını raporlar
                Log.Warning(ex, "Yedek dizini yazma testi sonuçsuz: {Server} → {Path}",
                    connectionInfo.Server, directoryPath);
            }
            finally
            {
                // xp_delete_file başarısız olduysa (yetki) yerel yoldan son deneme
                if (probeWritten)
                {
                    try { if (File.Exists(probePath)) File.Delete(probePath); }
                    catch (Exception ex) { Log.Debug(ex, "Deneme yedeği silinemedi: {Path}", probePath); }
                }
            }

            return result;
        }

        private static async Task<string> TryScalarAsync(SqlConnection conn, string sql, CancellationToken ct)
        {
            try
            {
                using var cmd = new SqlCommand(sql, conn);
                object value = await cmd.ExecuteScalarAsync(ct);
                return value == null || value == DBNull.Value ? null : value.ToString();
            }
            catch (SqlException ex)
            {
                Log.Debug(ex, "Sorgu çalıştırılamadı: {Sql}", sql);
                return null;
            }
        }

        private static bool ContainsError(SqlException ex, int number)
        {
            foreach (SqlError error in ex.Errors)
                if (error.Number == number) return true;
            return false;
        }

        private static string FirstErrorMessage(SqlException ex, int number)
        {
            foreach (SqlError error in ex.Errors)
                if (error.Number == number) return error.Message;
            return ex.Message;
        }
    }
}
