using System;
using System.IO;
using Newtonsoft.Json;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine
{
    /// <summary>
    /// JSON dosya tabanlı uygulama ayarları yöneticisi.
    /// Dosya: %APPDATA%\KoruMsSqlYedek\Config\appsettings.json
    /// </summary>
    public class AppSettingsManager : IAppSettingsManager
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<AppSettingsManager>();

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include
        };

        private static readonly string SettingsFilePath = Path.Combine(
            PathHelper.ConfigDirectory, "appsettings.json");

        public AppSettingsManager()
        {
            PathHelper.EnsureDirectoriesExist();
        }

        /// <inheritdoc/>
        public AppSettings Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                Log.Information("Ayar dosyası bulunamadı, varsayılan ayarlar oluşturuluyor: {Path}", SettingsFilePath);
                var defaults = new AppSettings();
                Save(defaults);
                return defaults;
            }

            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json, JsonSettings);

                if (settings == null)
                {
                    Log.Warning("Ayar dosyası boş veya geçersiz, varsayılan ayarlar kullanılıyor.");
                    return new AppSettings();
                }

                MigrateSmtpLegacy(settings);
                return settings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayar dosyası okunamadı: {Path}", SettingsFilePath);
                return new AppSettings();
            }
        }

        /// <summary>
        /// Eski tekil <c>smtp</c> alanını SmtpProfiles listesine "Varsayılan" adlı profil olarak taşır.
        /// Bu işlem bir kez gerçekleşir; sonrasında <c>Smtp</c> alanı null bırakılır ve dosyaya yazılmaz.
        /// </summary>
        private static void MigrateSmtpLegacy(AppSettings settings)
        {
            if (settings.Smtp == null || string.IsNullOrWhiteSpace(settings.Smtp.Host))
                return;

            if (settings.SmtpProfiles == null)
                settings.SmtpProfiles = new System.Collections.Generic.List<SmtpProfile>();

            if (settings.SmtpProfiles.Count > 0)
            {
                // Zaten profil var — sadece eski alanı temizle
                settings.Smtp = null;
                return;
            }

            Log.Information("Eski SMTP ayarı 'Varsayılan' profili olarak taşınıyor.");

            settings.SmtpProfiles.Add(new SmtpProfile
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = "Varsayılan",
                Host = settings.Smtp.Host,
                Port = settings.Smtp.Port,
                UseSsl = settings.Smtp.UseSsl,
                Username = settings.Smtp.Username,
                Password = settings.Smtp.Password,
                SenderEmail = settings.Smtp.SenderEmail,
                SenderDisplayName = settings.Smtp.SenderDisplayName ?? "Koru MsSql Yedek",
                RecipientEmails = settings.Smtp.RecipientEmails
            });

            // Migrasyon tamamlandı — eski alan temizlendi, bir sonraki kayıtta dosyaya yazılmaz
            settings.Smtp = null;
        }

        /// <inheritdoc/>
        public void Save(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            try
            {
                string json = JsonConvert.SerializeObject(settings, JsonSettings);
                File.WriteAllText(SettingsFilePath, json);
                Log.Information("Ayarlar kaydedildi: {Path}", SettingsFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayarlar kaydedilemedi: {Path}", SettingsFilePath);
                throw;
            }
        }
    }
}
