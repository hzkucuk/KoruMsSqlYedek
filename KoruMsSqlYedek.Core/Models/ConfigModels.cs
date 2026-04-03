using System;
using Newtonsoft.Json;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// SQL Server bağlantı bilgilerini temsil eder.
    /// </summary>
    public class SqlConnectionInfo
    {
        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("authMode")]
        public SqlAuthMode AuthMode { get; set; } = SqlAuthMode.Windows;

        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>
        /// DPAPI + Base64 ile encode edilmiş şifre. Düz metin ASLA saklanmaz.
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("connectionTimeoutSeconds")]
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Self-signed sertifikalı SQL Server bağlantıları için sertifika doğrulamasını atlar.
        /// Yerel ve güvenilir ağ sunucuları için varsayılan true.
        /// </summary>
        [JsonProperty("trustServerCertificate")]
        public bool TrustServerCertificate { get; set; } = true;
    }

    /// <summary>
    /// Yedekleme stratejisi ayarlarını tanımlar.
    /// </summary>
    public class BackupStrategyConfig
    {
        [JsonProperty("type")]
        public BackupStrategyType Type { get; set; } = BackupStrategyType.Full;

        /// <summary>Tam yedek cron zamanlaması (Quartz.NET formatı).</summary>
        [JsonProperty("fullSchedule")]
        public string FullSchedule { get; set; }

        /// <summary>Fark yedek cron zamanlaması.</summary>
        [JsonProperty("differentialSchedule")]
        public string DifferentialSchedule { get; set; }

        /// <summary>Artırımlı yedek cron zamanlaması.</summary>
        [JsonProperty("incrementalSchedule")]
        public string IncrementalSchedule { get; set; }

        /// <summary>
        /// Bu sayıda diff yedekten sonra otomatik Full tetiklenir.
        /// </summary>
        [JsonProperty("autoPromoteToFullAfter")]
        public int AutoPromoteToFullAfter { get; set; } = 7;
    }

    /// <summary>
    /// Sıkıştırma ayarları.
    /// </summary>
    public class CompressionConfig
    {
        [JsonProperty("algorithm")]
        public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.Lzma2;

        [JsonProperty("level")]
        public CompressionLevel Level { get; set; } = CompressionLevel.Ultra;

        /// <summary>
        /// DPAPI + Base64 ile encode edilmiş arşiv şifresi.
        /// </summary>
        [JsonProperty("archivePassword")]
        public string ArchivePassword { get; set; }
    }

    /// <summary>
    /// Saklama politikası.
    /// </summary>
    public class RetentionPolicy
    {
        [JsonProperty("type")]
        public RetentionPolicyType Type { get; set; } = RetentionPolicyType.KeepLastN;

        [JsonProperty("keepLastN")]
        public int KeepLastN { get; set; } = 30;

        [JsonProperty("deleteOlderThanDays")]
        public int DeleteOlderThanDays { get; set; } = 90;

        // ── GFS (Grandfather-Father-Son) alanları ──

        /// <summary>Son N günün en iyi yedeğini sakla.</summary>
        [JsonProperty("gfsKeepDaily")]
        public int GfsKeepDaily { get; set; } = 7;

        /// <summary>Son N haftanın en iyi yedeğini sakla.</summary>
        [JsonProperty("gfsKeepWeekly")]
        public int GfsKeepWeekly { get; set; } = 4;

        /// <summary>Son N ayın en iyi yedeğini sakla.</summary>
        [JsonProperty("gfsKeepMonthly")]
        public int GfsKeepMonthly { get; set; } = 12;

        /// <summary>Son N yılın en iyi yedeğini sakla.</summary>
        [JsonProperty("gfsKeepYearly")]
        public int GfsKeepYearly { get; set; } = 2;
    }

    /// <summary>
    /// Bulut hedef yapılandırması.
    /// </summary>
    public class CloudTargetConfig
    {
        [JsonProperty("type")]
        public CloudProviderType Type { get; set; }

        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>Google Drive veya Mega klasör yolu.</summary>
        [JsonProperty("remoteFolderPath")]
        public string RemoteFolderPath { get; set; }

        /// <summary>FTP/SFTP sunucu adresi.</summary>
        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("port")]
        public int? Port { get; set; }

        /// <summary>DPAPI + Base64 ile encode edilmiş kimlik bilgileri.</summary>
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>Yerel veya UNC yolu.</summary>
        [JsonProperty("localOrUncPath")]
        public string LocalOrUncPath { get; set; }

        /// <summary>OAuth2 token JSON (Google Drive).</summary>
        [JsonProperty("oauthTokenJson")]
        public string OAuthTokenJson { get; set; }

        /// <summary>OAuth2 Client ID (Google Cloud Console'dan alınır).</summary>
        [JsonProperty("oauthClientId")]
        public string OAuthClientId { get; set; }

        /// <summary>OAuth2 Client Secret — DPAPI + Base64 ile encode edilmiş.</summary>
        [JsonProperty("oauthClientSecret")]
        public string OAuthClientSecret { get; set; }

        /// <summary>
        /// FTPS bağlantısında sunucu sertifikası doğrulamasını atlar.
        /// Yalnızca self-signed sertifikalı güvenilir iç ağ sunucuları için true yapın.
        /// Varsayılan false — sertifika doğrulaması etkin.
        /// </summary>
        [JsonProperty("ftpsSkipCertificateValidation")]
        public bool FtpsSkipCertificateValidation { get; set; }

        /// <summary>
        /// SFTP sunucu parmak izi (SHA-256 hex). İlk bağlantıda otomatik kaydedilir (trust-on-first-use).
        /// Sonraki bağlantılarda bu parmak izi ile doğrulama yapılır.
        /// </summary>
        [JsonProperty("sftpHostFingerprint")]
        public string SftpHostFingerprint { get; set; }

        /// <summary>Upload hız limiti (MB/s). null = sınırsız.</summary>
        [JsonProperty("bandwidthLimitMbps")]
        public int? BandwidthLimitMbps { get; set; }

        /// <summary>Silinen dosyaların çöp kutusundan kalıcı temizlenmesi.</summary>
        [JsonProperty("permanentDeleteFromTrash")]
        public bool PermanentDeleteFromTrash { get; set; } = true;
    }

    /// <summary>
    /// Bildirim yapılandırması.
    /// </summary>
    public class NotificationConfig
    {
        [JsonProperty("onSuccess")]
        public bool OnSuccess { get; set; } = true;

        [JsonProperty("onFailure")]
        public bool OnFailure { get; set; } = true;

        [JsonProperty("emailEnabled")]
        public bool EmailEnabled { get; set; }

        /// <summary>
        /// Kullanılacak SMTP profilinin Id'si (AppSettings.SmtpProfiles içindeki SmtpProfile.Id).
        /// Null ise eski per-plan SMTP alanları kullanılır (geriye uyumluluk).
        /// </summary>
        [JsonProperty("smtpProfileId", NullValueHandling = NullValueHandling.Ignore)]
        public string SmtpProfileId { get; set; }

        // ── Eski per-plan SMTP alanları — geriye uyumluluk için korunmaktadır ──
        // Yeni planlarda SmtpProfileId kullanılır; bu alanlar boş bırakılır.

        [JsonProperty("emailTo", NullValueHandling = NullValueHandling.Ignore)]
        public string EmailTo { get; set; }

        [JsonProperty("smtpServer", NullValueHandling = NullValueHandling.Ignore)]
        public string SmtpServer { get; set; }

        [JsonProperty("smtpPort", NullValueHandling = NullValueHandling.Ignore)]
        public int? SmtpPort { get; set; }

        [JsonProperty("smtpUseSsl", NullValueHandling = NullValueHandling.Ignore)]
        public bool? SmtpUseSsl { get; set; }

        [JsonProperty("smtpUsername", NullValueHandling = NullValueHandling.Ignore)]
        public string SmtpUsername { get; set; }

        /// <summary>DPAPI + Base64 ile encode edilmiş SMTP şifresi.</summary>
        [JsonProperty("smtpPassword", NullValueHandling = NullValueHandling.Ignore)]
        public string SmtpPassword { get; set; }

        [JsonProperty("toastEnabled")]
        public bool ToastEnabled { get; set; } = true;
    }

    /// <summary>
    /// Yedekleme raporu yapılandırması.
    /// Belirli periyotlarda (günlük/haftalık/aylık) yedekleme özet raporunu yöneticiye gönderir.
    /// </summary>
    public class ReportingConfig
    {
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("frequency")]
        public ReportFrequency Frequency { get; set; } = ReportFrequency.Weekly;

        /// <summary>Rapor alıcı e-posta adresi. Boşsa bildirim e-postası kullanılır.</summary>
        [JsonProperty("emailTo")]
        public string EmailTo { get; set; }

        /// <summary>Rapor gönderilecek saat (0-23).</summary>
        [JsonProperty("sendHour")]
        public int SendHour { get; set; } = 8;
    }
}