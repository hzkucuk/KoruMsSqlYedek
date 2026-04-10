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
    /// Dosya tipine göre ayrı retention politikaları tanımlayan şema.
    /// Null olduğunda BackupPlan.Retention (eski alan) fallback olarak kullanılır.
    /// </summary>
    public class RetentionScheme
    {
        /// <summary>Hangi hazır şablondan türetildiği (bilgi amaçlı).</summary>
        [JsonProperty("template")]
        public RetentionTemplateType Template { get; set; } = RetentionTemplateType.Custom;

        /// <summary>SQL Server tam yedekler için retention politikası.</summary>
        [JsonProperty("sqlFull")]
        public RetentionPolicy SqlFull { get; set; } = new RetentionPolicy { KeepLastN = 7 };

        /// <summary>SQL Server fark yedekler için retention politikası.</summary>
        [JsonProperty("sqlDifferential")]
        public RetentionPolicy SqlDifferential { get; set; } = new RetentionPolicy { KeepLastN = 14 };

        /// <summary>SQL Server log/artırımlı yedekler için retention politikası.</summary>
        [JsonProperty("sqlLog")]
        public RetentionPolicy SqlLog { get; set; } = new RetentionPolicy { KeepLastN = 30 };

        /// <summary>Dosya/klasör yedekleme arşivleri (Files_*.7z) için retention politikası.</summary>
        [JsonProperty("fileBackup")]
        public RetentionPolicy FileBackup { get; set; } = new RetentionPolicy { KeepLastN = 14 };
    }

    /// <summary>
    /// Hazır retention şablonlarını üreten factory.
    /// </summary>
    public static class RetentionTemplates
    {
        /// <summary>Az yer: Full×3, Diff×7, Log×14, Files×5.</summary>
        public static RetentionScheme Minimal => new RetentionScheme
        {
            Template = RetentionTemplateType.Minimal,
            SqlFull = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 3 },
            SqlDifferential = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 7 },
            SqlLog = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 14 },
            FileBackup = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 5 }
        };

        /// <summary>Dengeli (önerilen): Full×7, Diff×14, Log×30, Files×14.</summary>
        public static RetentionScheme Standard => new RetentionScheme
        {
            Template = RetentionTemplateType.Standard,
            SqlFull = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 7 },
            SqlDifferential = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 14 },
            SqlLog = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 30 },
            FileBackup = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 14 }
        };

        /// <summary>Kapsamlı: Full×14, Diff×30, Log×90, Files×30.</summary>
        public static RetentionScheme Extended => new RetentionScheme
        {
            Template = RetentionTemplateType.Extended,
            SqlFull = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 14 },
            SqlDifferential = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 30 },
            SqlLog = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 90 },
            FileBackup = new RetentionPolicy { Type = RetentionPolicyType.KeepLastN, KeepLastN = 30 }
        };

        /// <summary>GFS rotasyonu — tüm tipler için Grandfather-Father-Son.</summary>
        public static RetentionScheme GFS => new RetentionScheme
        {
            Template = RetentionTemplateType.GFS,
            SqlFull = new RetentionPolicy
            {
                Type = RetentionPolicyType.GFS,
                GfsKeepDaily = 7, GfsKeepWeekly = 4, GfsKeepMonthly = 12, GfsKeepYearly = 2
            },
            SqlDifferential = new RetentionPolicy
            {
                Type = RetentionPolicyType.GFS,
                GfsKeepDaily = 14, GfsKeepWeekly = 4, GfsKeepMonthly = 3, GfsKeepYearly = 0
            },
            SqlLog = new RetentionPolicy
            {
                Type = RetentionPolicyType.GFS,
                GfsKeepDaily = 30, GfsKeepWeekly = 4, GfsKeepMonthly = 2, GfsKeepYearly = 0
            },
            FileBackup = new RetentionPolicy
            {
                Type = RetentionPolicyType.GFS,
                GfsKeepDaily = 7, GfsKeepWeekly = 4, GfsKeepMonthly = 6, GfsKeepYearly = 1
            }
        };

        /// <summary>Şablon türüne göre hazır RetentionScheme döndürür.</summary>
        public static RetentionScheme FromType(RetentionTemplateType type) => type switch
        {
            RetentionTemplateType.Minimal => Minimal,
            RetentionTemplateType.Standard => Standard,
            RetentionTemplateType.Extended => Extended,
            RetentionTemplateType.GFS => GFS,
            _ => Standard
        };
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

        /// <summary>Google Drive / FTP uzak klasör yolu.</summary>
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

        /// <summary>
        /// Çöp kutusu saklama süresi (gün).
        /// 0 = çöp kutusuna göndermeden kalıcı sil (varsayılan).
        /// >0 = silinen dosyaları çöp kutusunda N gün sakla, sonra kalıcı sil.
        /// </summary>
        [JsonProperty("trashRetentionDays")]
        public int TrashRetentionDays { get; set; }

        /// <summary>
        /// [Geriye uyumluluk] Eski yapılandırmalardan okunur. Yeni kayıtlarda serileştirilmez.
        /// </summary>
        [JsonProperty("permanentDeleteFromTrash", NullValueHandling = NullValueHandling.Ignore)]
        private bool? _legacyPermanentDelete;

        [System.Runtime.Serialization.OnDeserialized]
        private void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
        {
            // Eski bool formatını yeni gün formatına dönüştür
            if (_legacyPermanentDelete.HasValue && TrashRetentionDays == 0)
            {
                if (!_legacyPermanentDelete.Value)
                {
                    // Eski false (çöp kutusuna gönder + hemen temizle) → 30 gün saklama
                    TrashRetentionDays = 30;
                }
                // Eski true (kalıcı sil) → 0 zaten varsayılan
            }

            _legacyPermanentDelete = null;
        }

        /// <summary>Çöp kutusu kullanılıp kullanılmayacağını belirler.</summary>
        [JsonIgnore]
        public bool UsesTrash => TrashRetentionDays > 0;
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