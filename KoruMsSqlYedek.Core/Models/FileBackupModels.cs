using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Dosya yedekleme strateji türü.
    /// </summary>
    public enum FileBackupStrategy
    {
        /// <summary>Tüm dosyaları yedekler.</summary>
        Full = 0,

        /// <summary>Son tam yedekten bu yana değişen dosyaları yedekler.</summary>
        Differential = 1,

        /// <summary>Son yedekten (tür fark etmez) bu yana değişen dosyaları yedekler.</summary>
        Incremental = 2
    }

    /// <summary>
    /// Dosya yedekleme kaynak tanımı.
    /// Dizin yolu, include/exclude pattern, recursive flag ve VSS kullanımı belirler.
    /// </summary>
    public class FileBackupSource
    {
        /// <summary>Kaynak tanımlayıcı adı (ör. "Outlook PST", "Muhasebe Dosyaları").</summary>
        [JsonProperty("sourceName")]
        public string SourceName { get; set; }

        /// <summary>
        /// Kaynak kök dizin yolu. TreeView seçimlerinden otomatik türetilir (ortak kök).
        /// VSS volume tespiti ve eski davranış uyumluluğu için kullanılır.
        /// </summary>
        [JsonProperty("sourcePath")]
        public string SourcePath { get; set; }

        /// <summary>
        /// TreeView'da seçili klasör/dosya yolları.
        /// Bu liste yedeklenecek öğelerin kesin listesidir (kaynak gerçeği).
        /// Boş ise SourcePath altındaki tüm dosyalar yedeklenir (eski davranış uyumu).
        /// </summary>
        [JsonProperty("selectedPaths")]
        public List<string> SelectedPaths { get; set; } = new List<string>();

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

        /// <summary>Dosya yedekleme stratejisi (Tam / Fark / Artırımlı).</summary>
        [JsonProperty("strategy")]
        public FileBackupStrategy Strategy { get; set; } = FileBackupStrategy.Full;

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

        /// <summary>SHA-256 doğrulaması geçen dosya sayısı.</summary>
        [JsonProperty("filesVerified")]
        public int FilesVerified { get; set; }

        /// <summary>SHA-256 doğrulaması başarısız olan dosya sayısı.</summary>
        [JsonProperty("filesVerificationFailed")]
        public int FilesVerificationFailed { get; set; }

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

        /// <summary>
        /// Başarıyla yedeklenen dosyaların kaynak yolları.
        /// Manifest oluşturmak için kullanılır; JSON'a serileştirilmez.
        /// </summary>
        [JsonIgnore]
        public List<string> BackedUpFilePaths { get; set; } = new List<string>();
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
