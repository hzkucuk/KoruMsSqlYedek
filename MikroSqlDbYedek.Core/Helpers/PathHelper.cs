using System;
using System.IO;

namespace MikroSqlDbYedek.Core.Helpers
{
    /// <summary>
    /// Uygulama dizin yolları yardımcı sınıfı.
    /// </summary>
    public static class PathHelper
    {
        private static readonly string AppDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MikroSqlDbYedek");

        /// <summary>Plan JSON dosyaları dizini: %APPDATA%\MikroSqlDbYedek\Plans\</summary>
        public static string PlansDirectory => Path.Combine(AppDataRoot, "Plans");

        /// <summary>Log dosyaları dizini: %APPDATA%\MikroSqlDbYedek\Logs\</summary>
        public static string LogsDirectory => Path.Combine(AppDataRoot, "Logs");

        /// <summary>Genel ayarlar dizini: %APPDATA%\MikroSqlDbYedek\Config\</summary>
        public static string ConfigDirectory => Path.Combine(AppDataRoot, "Config");

        /// <summary>Uygulama verileri kök dizini: %APPDATA%\MikroSqlDbYedek\</summary>
        public static string AppDataDirectory => AppDataRoot;

        /// <summary>
        /// Tüm uygulama dizinlerini oluşturur (yoksa).
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(PlansDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(ConfigDirectory);
        }

        /// <summary>
        /// Plan ID'ye göre JSON dosya yolunu döndürür.
        /// </summary>
        public static string GetPlanFilePath(string planId)
        {
            return Path.Combine(PlansDirectory, $"{planId}.json");
        }

        /// <summary>
        /// Yedek dosyası için benzersiz ad üretir.
        /// Örnek: MIKRO_V16_DEMO_Full_20250115_020000.bak
        /// </summary>
        public static string GenerateBackupFileName(string databaseName, string backupType)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{databaseName}_{backupType}_{timestamp}.bak";
        }

        /// <summary>
        /// Sıkıştırılmış arşiv dosyası için ad üretir.
        /// Örnek: MIKRO_V16_DEMO_Full_20250115_020000.7z
        /// </summary>
        public static string GenerateArchiveFileName(string databaseName, string backupType)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{databaseName}_{backupType}_{timestamp}.7z";
        }
    }
}
