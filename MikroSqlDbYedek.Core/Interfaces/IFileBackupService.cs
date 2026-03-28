using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MikroSqlDbYedek.Core.Models;

namespace MikroSqlDbYedek.Core.Interfaces
{
    /// <summary>
    /// Dosya/klasör yedekleme servisi.
    /// VSS desteği ile açık/kilitli dosyaları (Outlook PST/OST vb.) yedekler.
    /// </summary>
    public interface IFileBackupService
    {
        /// <summary>
        /// Belirtilen plan için tüm dosya kaynaklarını yedekler.
        /// </summary>
        Task<List<FileBackupResult>> BackupFilesAsync(
            BackupPlan plan,
            IProgress<int> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tek bir dosya kaynağını yedekler.
        /// </summary>
        Task<FileBackupResult> BackupSourceAsync(
            FileBackupSource source,
            string destinationBasePath,
            IProgress<int> progress,
            CancellationToken cancellationToken);
    }
}
