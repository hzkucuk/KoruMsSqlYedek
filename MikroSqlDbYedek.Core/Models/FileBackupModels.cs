using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MikroSqlDbYedek.Core.Models
{
    /// <summary>
    /// Dosya yedekleme kaynak tanımı.
    /// Dizin yolu, include/exclude pattern, recursive flag ve VSS kullanımı belirler.
    /// </summary>
    public class FileBackupSource
    {
        /// <summary>Kaynak tanımlayıcı adı (ör. "Outlook PST", "Muhasebe Dosyaları").</summary>
        [JsonProperty("sourceName")]
        public string SourceName { get; set; }

        /// <summary>Kaynak dizin yolu (ör. "C:\Users\*\AppData\Local\Microsoft\Outlook").</summary>
        [JsonProperty("sourcePath")]
        public string SourcePath { get; set; }

        /// <summary>Alt dizinler dahil mi?</summary>
        [JsonProperty("recursive")]
        public bool Recursive { get; set; } = true;

        /// <summary>
        /// Dahil edilecek dosya kalıpları. Boş ise tümü dahil.
        /// Örnek: ["*.pst", "*.ost", "*.docx"]
        /// </summary>
        [JsonProperty("includePatterns")]
        public List<string> IncludePatterns { get; set; } = new List<string>();

        /// <summary>
        /// Hariç tutulacak dosya kalıpları.
        /// Örnek: ["*.tmp", "~*", "Thumbs.db"]
        /// </summary>
        [JsonProperty("excludePatterns")]
        public List<string> ExcludePatterns { get; set; } = new List<string>();

        /// <summary>
        /// Açık/kilitli dosyalar için VSS (Volume Shadow Copy) kullanılsın mı?
        /// Outlook PST/OST, açık Office dosyaları vb. için true olmalıdır.
        /// </summary>
        [JsonProperty("useVss")]
        public bool UseVss { get; set; } = true;

        /// <summary>Bu kaynak aktif mi?</summary>
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Dosya yedekleme yapılandırması.
    /// BackupPlan'ın bir parçası olarak JSON'da saklanır.
    /// </summary>
    public class FileBackupConfig
    {
        /// <summary>Dosya yedekleme aktif mi?</summary>
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; }

        /// <summary>Yedeklenecek dosya kaynakları listesi.</summary>
        [JsonProperty("sources")]
        public List<FileBackupSource> Sources { get; set; } = new List<FileBackupSource>();

        /// <summary>
        /// Cron zamanlama ifadesi (Quartz.NET formatı).
        /// null ise SQL yedek zamanlamasına bağlı olarak çalışır.
        /// </summary>
        [JsonProperty("schedule")]
        public string Schedule { get; set; }
    }

    /// <summary>
    /// Tek bir dosya yedekleme işleminin sonucu.
    /// </summary>
    public class FileBackupResult
    {
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("sourceName")]
        public string SourceName { get; set; }

        [JsonProperty("sourcePath")]
        public string SourcePath { get; set; }

        [JsonProperty("status")]
        public BackupResultStatus Status { get; set; }

        [JsonProperty("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonProperty("duration")]
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : (TimeSpan?)null;

        /// <summary>Kopyalanan dosya sayısı.</summary>
        [JsonProperty("filesCopied")]
        public int FilesCopied { get; set; }

        /// <summary>Atlanan dosya sayısı (hata veya exclude).</summary>
        [JsonProperty("filesSkipped")]
        public int FilesSkipped { get; set; }

        /// <summary>VSS snapshot kullanıldı mı?</summary>
        [JsonProperty("usedVss")]
        public bool UsedVss { get; set; }

        /// <summary>Toplam kopyalanan boyut (byte).</summary>
        [JsonProperty("totalSizeBytes")]
        public long TotalSizeBytes { get; set; }

        /// <summary>Yedeklenen dosyaların hedef dizini.</summary>
        [JsonProperty("destinationPath")]
        public string DestinationPath { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>Başarısız dosyaların listesi (yol + hata mesajı).</summary>
        [JsonProperty("failedFiles")]
        public List<FailedFileInfo> FailedFiles { get; set; } = new List<FailedFileInfo>();
    }

    /// <summary>
    /// Yedeklemesi başarısız olan dosya bilgisi.
    /// </summary>
    public class FailedFileInfo
    {
        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
