using System;
using System.IO;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// Uygulama dizin yolları yardımcı sınıfı.
    /// </summary>
    public static class PathHelper
    {
        private static readonly string AppDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KoruMsSqlYedek");

        // Yeniden adlandırma öncesi kullanılan eski AppData klasörü
        private static readonly string LegacyAppDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MikroSqlDbYedek");

        /// <summary>Plan JSON dosyaları dizini: %APPDATA%\KoruMsSqlYedek\Plans\</summary>
        public static string PlansDirectory => Path.Combine(AppDataRoot, "Plans");

        /// <summary>Log dosyaları dizini: %APPDATA%\KoruMsSqlYedek\Logs\</summary>
        public static string LogsDirectory => Path.Combine(AppDataRoot, "Logs");

        /// <summary>Genel ayarlar dizini: %APPDATA%\KoruMsSqlYedek\Config\</summary>
        public static string ConfigDirectory => Path.Combine(AppDataRoot, "Config");

        /// <summary>Yarıda kalan upload durumları: %APPDATA%\KoruMsSqlYedek\UploadState\</summary>
        public static string UploadStateDirectory => Path.Combine(AppDataRoot, "UploadState");

        /// <summary>Uygulama verileri kök dizini: %APPDATA%\KoruMsSqlYedek\</summary>
        public static string AppDataDirectory => AppDataRoot;

        /// <summary>
        /// Tüm uygulama dizinlerini oluşturur (yoksa).
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(PlansDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(ConfigDirectory);
            Directory.CreateDirectory(UploadStateDirectory);
        }

        /// <summary>
        /// Eski uygulama adından (MikroSqlDbYedek) kalan AppData verilerini
        /// yeni klasöre (KoruMsSqlYedek) taşır. Yalnızca eski klasör mevcutsa çalışır.
        /// </summary>
        public static void MigrateLegacyAppData()
        {
            if (!Directory.Exists(LegacyAppDataRoot))
                return;

            foreach (string sourceDir in Directory.GetDirectories(LegacyAppDataRoot, "*", SearchOption.AllDirectories))
            {
                string targetDir = sourceDir.Replace(LegacyAppDataRoot, AppDataRoot);
                Directory.CreateDirectory(targetDir);
            }

            foreach (string sourceFile in Directory.GetFiles(LegacyAppDataRoot, "*", SearchOption.AllDirectories))
            {
                string targetFile = sourceFile.Replace(LegacyAppDataRoot, AppDataRoot);
                if (!File.Exists(targetFile))
                    File.Copy(sourceFile, targetFile);
            }

            Directory.Delete(LegacyAppDataRoot, recursive: true);
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
