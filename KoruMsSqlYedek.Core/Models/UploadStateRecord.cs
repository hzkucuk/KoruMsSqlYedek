using System;
using Newtonsoft.Json;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Yarıda kalan bir bulut upload işleminin durumunu kalıcı olarak saklar.
    /// %APPDATA%\KoruMsSqlYedek\UploadState\{correlationId}_{providerType}.json
    /// </summary>
    public class UploadStateRecord
    {
        /// <summary>Tekil işlem kimliği (yedek correlationId + provider).</summary>
        [JsonProperty("stateId")]
        public string StateId { get; set; } = Guid.NewGuid().ToString("N");

        [JsonProperty("planId")]
        public string PlanId { get; set; }

        [JsonProperty("planName")]
        public string PlanName { get; set; }

        /// <summary>Yerel dosya tam yolu (.7z veya .bak).</summary>
        [JsonProperty("localFilePath")]
        public string LocalFilePath { get; set; }

        /// <summary>Uzak dosya adı.</summary>
        [JsonProperty("remoteFileName")]
        public string RemoteFileName { get; set; }

        /// <summary>Dosyanın SHA-256 hex özeti — bütünlük kontrolü için.</summary>
        [JsonProperty("localSha256")]
        public string LocalSha256 { get; set; }

        /// <summary>Dosya boyutu (byte).</summary>
        [JsonProperty("fileSizeBytes")]
        public long FileSizeBytes { get; set; }

        /// <summary>Provider türü.</summary>
        [JsonProperty("providerType")]
        public CloudProviderType ProviderType { get; set; }

        /// <summary>
        /// Google Drive / OneDrive resumable upload session URI.
        /// Boşsa sıfırdan başlanır.
        /// </summary>
        [JsonProperty("resumeSessionUri")]
        public string ResumeSessionUri { get; set; }

        /// <summary>Şimdiye kadar yüklenen byte miktarı.</summary>
        [JsonProperty("bytesUploaded")]
        public long BytesUploaded { get; set; }

        /// <summary>Bulut hedef yapılandırması (secrets hariç).</summary>
        [JsonProperty("cloudTarget")]
        public CloudTargetConfig CloudTarget { get; set; }

        /// <summary>Upload başlangıç zamanı.</summary>
        [JsonProperty("startedAt")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Son deneme zamanı.</summary>
        [JsonProperty("lastAttemptAt")]
        public DateTime? LastAttemptAt { get; set; }

        /// <summary>Deneme sayısı.</summary>
        [JsonProperty("attemptCount")]
        public int AttemptCount { get; set; }
    }
}
