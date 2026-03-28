using Newtonsoft.Json;

namespace MikroSqlDbYedek.Core.Models
{
    /// <summary>
    /// Uygulama genelinde geçerli olan ayarları temsil eder.
    /// JSON olarak %APPDATA%\MikroSqlDbYedek\Config\appsettings.json dosyasına kaydedilir.
    /// </summary>
    public class AppSettings
    {
        /// <summary>Uygulama dili (tr-TR veya en-US).</summary>
        [JsonProperty("language")]
        public string Language { get; set; } = "tr-TR";

        /// <summary>Windows oturumu açıldığında otomatik başlat.</summary>
        [JsonProperty("startWithWindows")]
        public bool StartWithWindows { get; set; } = true;

        /// <summary>Minimize edildiğinde system tray'e küçül.</summary>
        [JsonProperty("minimizeToTray")]
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>Varsayılan yedek dosya dizini.</summary>
        [JsonProperty("defaultBackupPath")]
        public string DefaultBackupPath { get; set; } = @"D:\Backups\MikroSqlDbYedek";

        /// <summary>Log dosyalarının saklanacağı gün sayısı.</summary>
        [JsonProperty("logRetentionDays")]
        public int LogRetentionDays { get; set; } = 30;

        /// <summary>Yedekleme geçmişi saklanacak gün sayısı.</summary>
        [JsonProperty("historyRetentionDays")]
        public int HistoryRetentionDays { get; set; } = 90;

        /// <summary>SMTP bildirim ayarları.</summary>
        [JsonProperty("smtp")]
        public SmtpSettings Smtp { get; set; } = new SmtpSettings();

        /// <summary>Ayar şeması versiyonu (geriye uyumluluk).</summary>
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;
    }

    /// <summary>
    /// SMTP e-posta bildirimi ayarları.
    /// </summary>
    public class SmtpSettings
    {
        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; } = 587;

        [JsonProperty("useSsl")]
        public bool UseSsl { get; set; } = true;

        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>DPAPI + Base64 ile encode edilmiş şifre.</summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("senderEmail")]
        public string SenderEmail { get; set; }

        [JsonProperty("senderDisplayName")]
        public string SenderDisplayName { get; set; } = "MikroSqlDbYedek";

        [JsonProperty("recipientEmails")]
        public string RecipientEmails { get; set; }
    }
}
