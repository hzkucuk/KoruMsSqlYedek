using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MikroSqlDbYedek.Core.Models;

namespace MikroSqlDbYedek.Core.Interfaces
{
    /// <summary>
    /// SQL Server yedekleme ve restore işlemlerini yönetir.
    /// </summary>
    public interface ISqlBackupService
    {
        /// <summary>
        /// Belirtilen veritabanının yedeğini alır.
        /// </summary>
        Task<BackupResult> BackupDatabaseAsync(
            SqlConnectionInfo connection,
            string databaseName,
            SqlBackupType backupType,
            string destinationPath,
            IProgress<int> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// RESTORE VERIFYONLY ile yedek dosyasını doğrular.
        /// </summary>
        Task<bool> VerifyBackupAsync(
            SqlConnectionInfo connection,
            string backupFilePath,
            CancellationToken cancellationToken);

        /// <summary>
        /// Veritabanını yedek dosyasından geri yükler.
        /// Restore öncesi hedef DB'nin otomatik yedeği alınır.
        /// </summary>
        Task<bool> RestoreDatabaseAsync(
            SqlConnectionInfo connection,
            string databaseName,
            string backupFilePath,
            bool createPreRestoreBackup,
            IProgress<int> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// SQL Server instance'ındaki veritabanlarını listeler.
        /// </summary>
        Task<List<DatabaseInfo>> ListDatabasesAsync(
            SqlConnectionInfo connection,
            CancellationToken cancellationToken);

        /// <summary>
        /// SQL Server bağlantısını test eder.
        /// </summary>
        Task<bool> TestConnectionAsync(
            SqlConnectionInfo connection,
            CancellationToken cancellationToken);
    }
}
