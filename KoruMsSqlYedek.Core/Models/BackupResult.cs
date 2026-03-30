using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Tek bir yedekleme işleminin sonucunu temsil eder.
    /// </summary>
    public class BackupResult
    {
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("planName")]
        public string PlanName { get; set; }

        [JsonProperty("databaseName")]
        public string DatabaseName { get; set; }

        [JsonProperty("backupType")]
        public SqlBackupType BackupType { get; set; }

        [JsonProperty("status")]
        public BackupResultStatus Status { get; set; }

        [JsonProperty("startedAt")]
        public DateTime StartedAt { get; set; }

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonProperty("duration")]
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : (TimeSpan?)null;

        /// <summary>Yerel .bak dosya yolu.</summary>
        [JsonProperty("backupFilePath")]
        public string BackupFilePath { get; set; }

        /// <summary>Sıkıştırılmış .7z dosya yolu.</summary>
        [JsonProperty("compressedFilePath")]
        public string CompressedFilePath { get; set; }

        /// <summary>
        /// Express Edition için VSS üzerinden alınan dosya kopyası arşivi (.7z).
        /// Yalnızca SQL Server Express'te doldurulur; diğer sürümlerde null.
        /// </summary>
        [JsonProperty("vssFileCopyPath")]
        public string VssFileCopyPath { get; set; }

        /// <summary>Express VSS dosya kopyası arşiv boyutu (byte).</summary>
        [JsonProperty("vssFileCopySizeBytes")]
        public long VssFileCopySizeBytes { get; set; }

        /// <summary>Dosya boyutu (byte).</summary>
        [JsonProperty("fileSizeBytes")]
        public long FileSizeBytes { get; set; }

        /// <summary>Sıkıştırılmış dosya boyutu (byte).</summary>
        [JsonProperty("compressedSizeBytes")]
        public long CompressedSizeBytes { get; set; }

        [JsonProperty("verifyResult")]
        public bool? VerifyResult { get; set; }

        /// <summary>Sıkıştırma sonrası 7z arşiv bütünlük doğrulaması.</summary>
        [JsonProperty("compressionVerified")]
        public bool? CompressionVerified { get; set; }

        [JsonProperty("cloudUploadResults")]
        public List<CloudUploadResult> CloudUploadResults { get; set; } = new List<CloudUploadResult>();

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Bulut upload sonucu.
    /// </summary>
    public class CloudUploadResult
    {
        [JsonProperty("providerType")]
        public CloudProviderType ProviderType { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonProperty("remoteFilePath")]
        public string RemoteFilePath { get; set; }

        [JsonProperty("uploadedAt")]
        public DateTime? UploadedAt { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("retryCount")]
        public int RetryCount { get; set; }

        /// <summary>
        /// Upload sonrası uzak sistemdeki dosya boyutu (byte). Bütünlük doğrulaması için kullanılır.
        /// 0 ise provider boyutu raporlamamıştır.
        /// </summary>
        [JsonProperty("remoteFileSizeBytes")]
        public long RemoteFileSizeBytes { get; set; }
    }
}
