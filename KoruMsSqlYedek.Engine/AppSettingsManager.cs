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

                return settings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayar dosyası okunamadı: {Path}", SettingsFilePath);
                return new AppSettings();
            }
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
