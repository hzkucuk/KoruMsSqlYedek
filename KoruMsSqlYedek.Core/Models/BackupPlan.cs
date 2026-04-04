using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Yedekleme planını temsil eden ana model.
    /// JSON olarak %APPDATA%\KoruMsSqlYedek\Plans\{planId}.json dosyasına kaydedilir.
    /// </summary>
    public class BackupPlan
    {
        [JsonProperty("planId")]
        public string PlanId { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("planName")]
        public string PlanName { get; set; }

        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// [KALDIRILDI v0.73.0] Artık kullanılmıyor — JSON geriye uyumluluk için korunuyor.
        /// Bulut hedef olup olmadığını <see cref="HasCloudTargets"/> ile kontrol edin.
        /// </summary>
        [Obsolete("Mode artık kullanılmıyor. HasCloudTargets property'sini kullanın.")]
        [JsonProperty("backupMode")]
        public BackupMode Mode { get; set; } = BackupMode.Local;

        /// <summary>
        /// Plan en az bir aktif bulut hedefine sahip mi?
        /// Mode enum'u yerine bu property kullanılmalıdır.
        /// </summary>
        [JsonIgnore]
        public bool HasCloudTargets => CloudTargets != null && CloudTargets.Any(t => t.IsEnabled);

        [JsonProperty("sqlConnection")]
        public SqlConnectionInfo SqlConnection { get; set; } = new SqlConnectionInfo();

        /// <summary>
        /// Yedeklenecek veritabanı adları listesi.
        /// </summary>
        [JsonProperty("databases")]
        public List<string> Databases { get; set; } = new List<string>();

        [JsonProperty("strategy")]
        public BackupStrategyConfig Strategy { get; set; } = new BackupStrategyConfig();

        [JsonProperty("compression")]
        public CompressionConfig Compression { get; set; } = new CompressionConfig();

        [JsonProperty("retention")]
        public RetentionPolicy Retention { get; set; } = new RetentionPolicy();

        /// <summary>
        /// Yerel yedek dosyalarının saklanacağı dizin.
        /// </summary>
        [JsonProperty("localPath")]
        public string LocalPath { get; set; } = @"D:\Backups\KoruMsSqlYedek";

        [JsonProperty("cloudTargets")]
        public List<CloudTargetConfig> CloudTargets { get; set; } = new List<CloudTargetConfig>();

        [JsonProperty("notifications")]
        public NotificationConfig Notifications { get; set; } = new NotificationConfig();

        /// <summary>
        /// Dosya/klasör yedekleme yapılandırması.
        /// VSS desteği ile açık/kilitli dosyaları (Outlook PST/OST vb.) yedekler.
        /// null veya IsEnabled=false ise dosya yedekleme yapılmaz.
        /// </summary>
        [JsonProperty("fileBackup")]
        public FileBackupConfig FileBackup { get; set; }

        /// <summary>
        /// Yedekleme raporlama ayarları. Günlük/haftalık/aylık özet rapor gönderimi.
        /// </summary>
        [JsonProperty("reporting")]
        public ReportingConfig Reporting { get; set; } = new ReportingConfig();

        /// <summary>
        /// Yedek sonrası RESTORE VERIFYONLY ile doğrulama yapılsın mı?
        /// </summary>
        [JsonProperty("verifyAfterBackup")]
        public bool VerifyAfterBackup { get; set; } = true;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("lastModifiedAt")]
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Plan şeması versiyonu (geriye uyumluluk için).</summary>
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;
    }
}
