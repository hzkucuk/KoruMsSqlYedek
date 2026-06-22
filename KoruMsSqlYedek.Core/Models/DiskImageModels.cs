using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Disk imajı yedekleme formatı.
    /// </summary>
    public enum DiskImageFormat
    {
        /// <summary>Windows Image Format (.wim) — wbadmin ile oluşturulur.</summary>
        Wim = 0,

        /// <summary>Virtual Hard Disk (.vhd) — ileride desteklenecek.</summary>
        Vhd = 1
    }

    /// <summary>
    /// Yedeklenecek sürücü/bölüm tanımı.
    /// Örnek: C:, D:\Data
    /// </summary>
    public class DiskImageSource
    {
        /// <summary>Sürücü harfi veya bölüm yolu (örn. "C:", "D:").</summary>
        [JsonProperty("volumePath")]
        public string VolumePath { get; set; }

        /// <summary>Görünen ad (örn. "Sistem Sürücüsü").</summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>Bu kaynak aktif mi?</summary>
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Disk imajı yedekleme yapılandırması.
    /// BackupPlan'ın bir parçası olarak JSON'da saklanır.
    /// null veya IsEnabled=false ise disk imajı yedekleme yapılmaz.
    /// </summary>
    public class DiskImageBackupConfig
    {
        /// <summary>Disk imajı yedekleme aktif mi?</summary>
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; }

        /// <summary>İmaj formatı (varsayılan: Wim).</summary>
        [JsonProperty("format")]
        public DiskImageFormat Format { get; set; } = DiskImageFormat.Wim;

        /// <summary>Yedeklenecek sürücü/bölüm listesi.</summary>
        [JsonProperty("sources")]
        public List<DiskImageSource> Sources { get; set; } = new List<DiskImageSource>();

        /// <summary>
        /// İmaj dosyalarının yazılacağı alt dizin adı.
        /// LocalPath altında oluşturulur (örn. D:\Backups\KoruMsSqlYedek\DiskImages\).
        /// </summary>
        [JsonProperty("subDirectory")]
        public string SubDirectory { get; set; } = "DiskImages";

        /// <summary>
        /// Cron zamanlama ifadesi (Quartz.NET formatı).
        /// null ise SQL yedek zamanlamasına bağlı olarak çalışır.
        /// </summary>
        [JsonProperty("schedule")]
        public string Schedule { get; set; }
    }

    /// <summary>
    /// Tek bir disk imajı yedekleme işleminin sonucu.
    /// </summary>
    public class DiskImageResult
    {
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("volumePath")]
        public string VolumePath { get; set; }

        [JsonProperty("status")]
        public BackupResultStatus Status { get; set; }

        [JsonProperty("format")]
        public DiskImageFormat Format { get; set; }

        [JsonProperty("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonProperty("duration")]
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : (TimeSpan?)null;

        /// <summary>Oluşturulan imaj dosyasının tam yolu.</summary>
        [JsonProperty("outputPath")]
        public string OutputPath { get; set; }

        /// <summary>İmaj dosyasının boyutu (byte). 0 ise henüz tamamlanmamış.</summary>
        [JsonProperty("imageSizeBytes")]
        public long ImageSizeBytes { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
