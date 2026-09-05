using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// SQL Server yedekleme ve restore işlemlerini yönetir.
    /// </summary>
    public interface ISqlBackupService
    {
        /// <summary>
        /// Belirtilen veritabanının yedeğini alır.
        /// </summary>
        /// <param name="enableVssBackup">
        /// true (default): standart .bak yedeğinden sonra ek güvenlik olarak
        /// VSS üzerinden MDF/LDF ham dosya kopyası (.7z) alınır.
        /// false: VSS adımı tamamen atlanır; sadece standart SMO yedeği alınır.
        /// </param>
        Task<BackupResult> BackupDatabaseAsync(
            SqlConnectionInfo connection,
            string databaseName,
            SqlBackupType backupType,
            string destinationPath,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            bool enableVssBackup = true);

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
        /// <param name="safetyBackupDirectory">
        /// Restore öncesi güvenlik yedeğinin yazılacağı dizin. Kalıcı bir konum olmalıdır
        /// (ör. kullanıcının seçtiği ORİJİNAL yedek dosyasının dizini altındaki "PreRestore");
        /// geçici bir arşiv çıkarma dizini ASLA verilmemelidir, çünkü işlem sonunda silinir.
        /// null ise backupFilePath'in dizini altındaki "PreRestore" kullanılır.
        /// </param>
        Task<bool> RestoreDatabaseAsync(
            SqlConnectionInfo connection,
            string databaseName,
            string backupFilePath,
            bool createPreRestoreBackup,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            string safetyBackupDirectory = null);

        /// <summary>
        /// Yedek dosyasının başlığını (RESTORE HEADERONLY) okuyup içindeki veritabanı adını döndürür.
        /// Okunamazsa null döner (çağıran, dosya adından çıkarım gibi bir yedek yola başvurabilir).
        /// </summary>
        Task<string> ReadBackupDatabaseNameAsync(
            SqlConnectionInfo connection,
            string backupFilePath,
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

        /// <summary>
        /// SQL Server instance edition ve sürüm bilgisini döndürür.
        /// Plan doğrulaması ve bağlantı testinde kullanılır:
        /// Express ise log backup stratejisinin çalışmayacağını önceden uyarır.
        /// </summary>
        Task<SqlServerEditionInfo> GetServerEditionAsync(
            SqlConnectionInfo connection,
            CancellationToken cancellationToken);
    }
}
