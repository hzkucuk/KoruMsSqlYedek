using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Serilog;
using KoruMsSqlYedek.Core.Models;
using SqlConnInfo = KoruMsSqlYedek.Core.Models.SqlConnectionInfo;

namespace KoruMsSqlYedek.Engine.Backup
{
    // ── Veritabanı dosyaları + AUTO_CLOSE ─────────────────────────────
    public partial class SqlBackupService
    {
        public async Task<List<SqlDatabaseFileInfo>> GetDatabaseFilesAsync(
            SqlConnInfo connectionInfo,
            CancellationToken cancellationToken)
        {
            var files = new List<SqlDatabaseFileInfo>();
            if (connectionInfo == null || string.IsNullOrWhiteSpace(connectionInfo.Server))
                return files;

            try
            {
                EnsureSystemLoginPermission(connectionInfo);
                using var conn = new SqlConnection(BuildConnectionString(connectionInfo));
                await conn.OpenAsync(cancellationToken);

                // sys.master_files OFFLINE/kapalı (AUTO_CLOSE) veritabanlarını da listeler
                using var cmd = new SqlCommand(
                    "SELECT d.name, mf.physical_name, d.is_auto_close_on " +
                    "FROM sys.master_files mf JOIN sys.databases d ON d.database_id = mf.database_id",
                    conn);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    files.Add(new SqlDatabaseFileInfo
                    {
                        DatabaseName = reader.GetString(0),
                        PhysicalPath = reader.GetString(1),
                        IsAutoCloseOn = reader.GetBoolean(2)
                    });
                }

                Log.Debug("SQL veritabanı dosyaları okundu: {Server} — {Count} dosya",
                    connectionInfo.Server, files.Count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SQL veritabanı dosya listesi alınamadı: {Server}", connectionInfo.Server);
            }

            return files;
        }

        public async Task<List<string>> DisableAutoCloseAsync(
            SqlConnInfo connectionInfo,
            IEnumerable<string> databaseNames,
            CancellationToken cancellationToken)
        {
            var changed = new List<string>();
            var names = databaseNames?
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (connectionInfo == null || string.IsNullOrWhiteSpace(connectionInfo.Server)
                || names == null || names.Count == 0)
                return changed;

            try
            {
                EnsureSystemLoginPermission(connectionInfo);
                using var conn = new SqlConnection(BuildConnectionString(connectionInfo));
                await conn.OpenAsync(cancellationToken);

                foreach (string name in names)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        using (var check = new SqlCommand(
                            "SELECT is_auto_close_on FROM sys.databases WHERE name = @name", conn))
                        {
                            check.Parameters.AddWithValue("@name", name);
                            object value = await check.ExecuteScalarAsync(cancellationToken);
                            if (value is not bool isOn || !isOn)
                                continue;
                        }

                        // WITH NO_WAIT: başka oturumlar seçeneği kilitliyorsa beklemeden hata verir,
                        // yedekleme bekletilmez (bir sonraki çalıştırmada yeniden denenir)
                        using var alter = new SqlCommand(
                            $"ALTER DATABASE [{EscapeBracketIdentifier(name)}] SET AUTO_CLOSE OFF WITH NO_WAIT",
                            conn)
                        {
                            CommandTimeout = 30
                        };
                        await alter.ExecuteNonQueryAsync(cancellationToken);

                        changed.Add(name);
                        Log.Information("AUTO_CLOSE kapatıldı: {Database}", name);
                    }
                    catch (SqlException ex)
                    {
                        Log.Warning(ex, "AUTO_CLOSE kapatılamadı (yedekleme devam ediyor): {Database}", name);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "AUTO_CLOSE kontrolü yapılamadı: {Server}", connectionInfo.Server);
            }

            return changed;
        }

        public async Task<string> GetLastFullBackupFileAsync(
            SqlConnInfo connectionInfo,
            string databaseName,
            CancellationToken cancellationToken)
        {
            if (connectionInfo == null || string.IsNullOrWhiteSpace(connectionInfo.Server)
                || string.IsNullOrWhiteSpace(databaseName))
                return null;

            try
            {
                EnsureSystemLoginPermission(connectionInfo);
                using var conn = new SqlConnection(BuildConnectionString(connectionInfo));
                await conn.OpenAsync(cancellationToken);

                using var cmd = new SqlCommand(
                    "SELECT TOP 1 mf.physical_device_name " +
                    "FROM msdb.dbo.backupset bs " +
                    "JOIN msdb.dbo.backupmediafamily mf ON mf.media_set_id = bs.media_set_id " +
                    "WHERE bs.database_name = @name AND bs.type = 'D' AND bs.is_copy_only = 0 " +
                    "ORDER BY bs.backup_finish_date DESC",
                    conn);
                cmd.Parameters.AddWithValue("@name", databaseName);

                return await cmd.ExecuteScalarAsync(cancellationToken) as string;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Son tam yedek bilgisi msdb'den okunamadı: {Database}", databaseName);
                return null;
            }
        }
    }
}
