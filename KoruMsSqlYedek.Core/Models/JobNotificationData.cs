using System;
using System.Collections.Generic;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Bir görev çalıştırmasının tüm sonuçlarını konsolide eden veri nesnesi.
    /// Tek bir e-posta bildirimi için SQL, dosya ve bulut sonuçlarını birleştirir.
    /// </summary>
    public class JobNotificationData
    {
        /// <summary>Plan adı.</summary>
        public string PlanName { get; set; }

        /// <summary>Plan kimliği.</summary>
        public string PlanId { get; set; }

        /// <summary>Yedek türü (Full, Differential, Incremental, FileBackup).</summary>
        public string BackupType { get; set; }

        /// <summary>Correlation ID.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Görev başlama zamanı.</summary>
        public DateTime StartedAt { get; set; }

        /// <summary>Görev bitiş zamanı.</summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>Genel başarı durumu.</summary>
        public bool IsSuccess { get; set; }

        /// <summary>SQL yedekleme sonuçları (veritabanı başına).</summary>
        public List<BackupResult> SqlResults { get; set; } = new List<BackupResult>();

        /// <summary>Dosya yedekleme sonuçları (kaynak başına).</summary>
        public List<FileBackupResult> FileResults { get; set; } = new List<FileBackupResult>();

        /// <summary>Dosya yedekleme arşiv dosya adı.</summary>
        public string FileArchiveFileName { get; set; }

        /// <summary>Dosya yedekleme arşiv boyutu (byte).</summary>
        public long FileArchiveSizeBytes { get; set; }

        /// <summary>Dosya yedekleme bulut yükleme sonuçları.</summary>
        public List<CloudUploadResult> FileCloudUploadResults { get; set; } = new List<CloudUploadResult>();

        /// <summary>
        /// Görev sırasında log ekranında görüntülenen satırlar.
        /// E-postada aynen gösterilir.
        /// </summary>
        public List<string> LogLines { get; set; } = new List<string>();
    }
}
