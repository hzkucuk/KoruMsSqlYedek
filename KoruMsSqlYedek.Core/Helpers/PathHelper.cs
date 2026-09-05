using System;
using System.IO;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// Uygulama dizin yolları yardımcı sınıfı.
    /// Paylaşılan veriler (planlar, ayarlar, upload state) %ProgramData% altında tutulur
    /// böylece hem Tray (kullanıcı) hem Windows Service (LocalSystem) aynı verilere erişir.
    /// Log dosyaları da ortak dizinde saklanır.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Ortak uygulama verileri kök dizini: %ProgramData%\KoruMsSqlYedek\
        /// Hem Tray hem Service tarafından erişilir.
        /// </summary>
        private static readonly string AppDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "KoruMsSqlYedek");

        /// <summary>
        /// Eski konum: %APPDATA%\KoruMsSqlYedek (v0.75.1 ve öncesi).
        /// Migrasyon için kullanılır.
        /// </summary>
        internal static readonly string LegacyUserAppDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KoruMsSqlYedek");

        // Yeniden adlandırma öncesi kullanılan eski AppData klasörü (MikroSqlDbYedek)
        private static readonly string LegacyAppNameRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MikroSqlDbYedek");

        /// <summary>Plan JSON dosyaları dizini: %ProgramData%\KoruMsSqlYedek\Plans\</summary>
        public static string PlansDirectory => Path.Combine(AppDataRoot, "Plans");

        /// <summary>Log dosyaları dizini: %ProgramData%\KoruMsSqlYedek\Logs\</summary>
        public static string LogsDirectory => Path.Combine(AppDataRoot, "Logs");

        /// <summary>Genel ayarlar dizini: %ProgramData%\KoruMsSqlYedek\Config\</summary>
        public static string ConfigDirectory => Path.Combine(AppDataRoot, "Config");

        /// <summary>Yarıda kalan upload durumları: %ProgramData%\KoruMsSqlYedek\UploadState\</summary>
        public static string UploadStateDirectory => Path.Combine(AppDataRoot, "UploadState");

        /// <summary>Uygulama verileri kök dizini: %ProgramData%\KoruMsSqlYedek\</summary>
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
        /// yeni klasöre taşır. Yalnızca eski klasör mevcutsa çalışır.
        /// </summary>
        public static void MigrateLegacyAppName()
        {
            if (!Directory.Exists(LegacyAppNameRoot))
                return;

            foreach (string sourceDir in Directory.GetDirectories(LegacyAppNameRoot, "*", SearchOption.AllDirectories))
            {
                string targetDir = sourceDir.Replace(LegacyAppNameRoot, LegacyUserAppDataRoot);
                Directory.CreateDirectory(targetDir);
            }

            foreach (string sourceFile in Directory.GetFiles(LegacyAppNameRoot, "*", SearchOption.AllDirectories))
            {
                string targetFile = sourceFile.Replace(LegacyAppNameRoot, LegacyUserAppDataRoot);
                if (!File.Exists(targetFile))
                    File.Copy(sourceFile, targetFile);
            }

            try { Directory.Delete(LegacyAppNameRoot, recursive: true); }
            catch { /* silme başarısız olursa görmezden gel */ }
        }

        /// <summary>
        /// Eski kullanıcı %APPDATA% konumundaki verileri %ProgramData% altına kopyalar.
        /// DPAPI şifre migrasyonu için <see cref="DataMigrationHelper"/> kullanılır;
        /// bu metot sadece dosya kopyalama yapar.
        /// Yalnızca eski konum mevcutsa ve yeni konumda plan yoksa çalışır.
        /// </summary>
        /// <returns>Migrasyon yapıldıysa true.</returns>
        public static bool MigrateUserAppDataToProgramData()
        {
            if (!Directory.Exists(LegacyUserAppDataRoot))
                return false;

            // Yeni konumda zaten plan varsa migrasyon gereksiz
            string newPlansDir = PlansDirectory;
            if (Directory.Exists(newPlansDir) && Directory.GetFiles(newPlansDir, "*.json").Length > 0)
                return false;

            EnsureDirectoriesExist();

            // Tüm dosyaları kopyala (planlar, config, upload state)
            foreach (string sourceDir in Directory.GetDirectories(LegacyUserAppDataRoot, "*", SearchOption.AllDirectories))
            {
                string targetDir = sourceDir.Replace(LegacyUserAppDataRoot, AppDataRoot);
                Directory.CreateDirectory(targetDir);
            }

            foreach (string sourceFile in Directory.GetFiles(LegacyUserAppDataRoot, "*", SearchOption.AllDirectories))
            {
                string targetFile = sourceFile.Replace(LegacyUserAppDataRoot, AppDataRoot);
                if (!File.Exists(targetFile))
                    File.Copy(sourceFile, targetFile);
            }

            return true;
        }

        /// <summary>
        /// Plan ID'ye göre JSON dosya yolunu döndürür.
        /// </summary>
        public static string GetPlanFilePath(string planId)
        {
            return Path.Combine(PlansDirectory, $"{planId}.json");
        }

        /// <summary>
        /// Dosya adının bir bileşenini (örn. veritabanı adı) dosya sistemi için güvenli hale getirir.
        /// Geçersiz dosya adı karakterleri, joker karakterler ('*', '?') ve ".." dizileri '_' ile
        /// değiştirilir; böylece "..\" gibi dizin kaçışları ve bozuk arama desenleri engellenir.
        /// Yedek dosya adı üreten ve bu adları arayan (retention, zincir kontrolü) tüm kodlar
        /// AYNI dönüşümü kullanmalıdır; aksi halde üretilen dosya ile arama deseni eşleşmez.
        /// </summary>
        public static string SanitizeFileNameComponent(string component)
        {
            if (string.IsNullOrEmpty(component))
                return "_";

            var invalid = new System.Collections.Generic.HashSet<char>(Path.GetInvalidFileNameChars())
            {
                '*', '?', '/', '\\', ':'
            };

            var sb = new System.Text.StringBuilder(component.Length);
            foreach (char c in component)
                sb.Append(invalid.Contains(c) ? '_' : c);

            string result = sb.ToString();

            // ".." dizilerini (ve daha uzun nokta zincirlerini) tek '_' karakterine indir
            while (result.Contains(".."))
                result = result.Replace("..", "_");

            // Tamamen boşluk/nokta kalan adlar Windows'ta geçersizdir
            result = result.Trim();
            if (result.Length == 0 || result.Trim('.').Length == 0)
                return "_";

            return result;
        }

        /// <summary>
        /// Yedek dosyası için benzersiz ad üretir.
        /// Örnek: MIKRO_V16_DEMO_Full_20250115_020000.bak
        /// Veritabanı adı <see cref="SanitizeFileNameComponent"/> ile temizlenir.
        /// </summary>
        public static string GenerateBackupFileName(string databaseName, string backupType)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{SanitizeFileNameComponent(databaseName)}_{backupType}_{timestamp}.bak";
        }

        /// <summary>
        /// Sıkıştırılmış arşiv dosyası için ad üretir.
        /// Örnek: MIKRO_V16_DEMO_Full_20250115_020000.7z
        /// Veritabanı adı <see cref="SanitizeFileNameComponent"/> ile temizlenir.
        /// </summary>
        public static string GenerateArchiveFileName(string databaseName, string backupType)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{SanitizeFileNameComponent(databaseName)}_{backupType}_{timestamp}.7z";
        }
    }
}
