using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KoruMsSqlYedek.Core.Models
{
    /// <summary>
    /// Uygulama genelinde geçerli olan ayarları temsil eder.
    /// JSON olarak %ProgramData%\KoruMsSqlYedek\Config\appsettings.json dosyasına kaydedilir.
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
        public string DefaultBackupPath { get; set; } = @"D:\Backups\KoruMsSqlYedek";

        /// <summary>Log dosyalarının saklanacağı gün sayısı.</summary>
        [JsonProperty("logRetentionDays")]
        public int LogRetentionDays { get; set; } = 30;

        /// <summary>Yedekleme geçmişi saklanacak gün sayısı.</summary>
        [JsonProperty("historyRetentionDays")]
        public int HistoryRetentionDays { get; set; } = 90;

        /// <summary>
        /// Birden fazla SMTP profili. Görevler bu profilleri Id ile referanslar.
        /// </summary>
        [JsonProperty("smtpProfiles")]
        public List<SmtpProfile> SmtpProfiles { get; set; } = new List<SmtpProfile>();

        /// <summary>
        /// Eski tekil SMTP ayarı — yalnızca migrasyon için okunur, yeni kayıtlarda kullanılmaz.
        /// </summary>
        [JsonProperty("smtp", NullValueHandling = NullValueHandling.Ignore)]
        public SmtpSettings Smtp { get; set; }

        /// <summary>Güncelleme bulunduğunda sessiz (silent) kurulum yap.</summary>
        [JsonProperty("autoSilentUpdate")]
        public bool AutoSilentUpdate { get; set; }

        /// <summary>Uygulama teması ("ozgur-filistin", "dark" veya "light").</summary>
        [JsonProperty("theme")]
        public string Theme { get; set; } = "ozgur-filistin";

        /// <summary>Log konsolu terminal renk şablonu (ör. "ozgur-filistin", "koru", "dracula").</summary>
        [JsonProperty("logColorScheme")]
        public string LogColorScheme { get; set; } = "ozgur-filistin";

        // ═══════════════ ŞİFRE KORUMASI ═══════════════

        /// <summary>Görev şifresi (SHA256 hash, DPAPI ile korumalı). Boş ise koruma yok.</summary>
        [JsonProperty("passwordHash", NullValueHandling = NullValueHandling.Ignore)]
        public string PasswordHash { get; set; }

        /// <summary>Şifre kurtarma güvenlik sorusu.</summary>
        [JsonProperty("securityQuestion", NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityQuestion { get; set; }

        /// <summary>Güvenlik sorusu cevabı (SHA256 hash, DPAPI ile korumalı).</summary>
        [JsonProperty("securityAnswerHash", NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityAnswerHash { get; set; }

        /// <summary>Şifre koruması aktif/pasif durumu. true ise şifre sorulur.</summary>
        [JsonProperty("passwordEnabled")]
        public bool PasswordEnabled { get; set; } = true;

        /// <summary>Şifre tanımlı mı? (hash var mı)</summary>
        [JsonIgnore]
        public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

        /// <summary>Şifre koruması etkin mi? (tanımlı + aktif)</summary>
        [JsonIgnore]
        public bool IsPasswordProtected => HasPassword && PasswordEnabled;

        // ═══════════════ GOOGLE OAUTH ═══════════════

        /// <summary>
        /// Kullanıcının kendi Google Cloud Console Client ID değeri.
        /// Boş ise gömülü (embedded) credential kullanılır.
        /// </summary>
        [JsonProperty("googleOAuthClientId", NullValueHandling = NullValueHandling.Ignore)]
        public string GoogleOAuthClientId { get; set; }

        /// <summary>
        /// Kullanıcının kendi Google Cloud Console Client Secret değeri (DPAPI + Base64).
        /// Boş ise gömülü (embedded) credential kullanılır.
        /// </summary>
        [JsonProperty("googleOAuthClientSecret", NullValueHandling = NullValueHandling.Ignore)]
        public string GoogleOAuthClientSecret { get; set; }

        /// <summary>Kullanıcının özel Google OAuth credential'ları tanımlı mı?</summary>
        [JsonIgnore]
        public bool HasCustomGoogleOAuth =>
            !string.IsNullOrEmpty(GoogleOAuthClientId) &&
            !string.IsNullOrEmpty(GoogleOAuthClientSecret);

        /// <summary>Ayar şeması versiyonu (geriye uyumluluk migrasyonları için).</summary>
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 3;
    }

    /// <summary>
    /// Adlandırılmış SMTP e-posta profili.
    /// Birden fazla profil tanımlanabilir; görevler istediği profili SmtpProfileId ile seçer.
    /// </summary>
    public class SmtpProfile
    {
        /// <summary>Benzersiz tanımlayıcı (GUID).</summary>
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Kullanıcı dostu görünen ad (ör. "Gmail", "Office 365 Kurumsal").</summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

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
        public string SenderDisplayName { get; set; } = "Koru MsSql Yedek";

        /// <summary>Varsayılan alıcı adresleri (noktalı virgül veya virgülle ayrılmış).</summary>
        [JsonProperty("recipientEmails")]
        public string RecipientEmails { get; set; }
    }

    /// <summary>
    /// Eski tekil SMTP yapılandırması
    /// Yeni kodda SmtpProfile kullanılmalıdır.
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
        public string SenderDisplayName { get; set; } = "Koru MsSql Yedek";

        [JsonProperty("recipientEmails")]
        public string RecipientEmails { get; set; }
    }
}
