using System;
using System.IO;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// Uygulama dizin yolları yardımcı sınıfı.
    /// Tüm veriler (planlar, ayarlar, loglar, upload state, geçmiş, güncellemeler)
    /// kurulum dizininin altındaki tek bir <c>Data</c> klasöründe tutulur:
    /// <c>{Kurulum}\Data\</c>. Tray <c>{Kurulum}\</c>, servis <c>{Kurulum}\Service\</c>
    /// altından çalıştığı için kök, çalışan exe'nin konumundan türetilir.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>Veri klasörünün adı (kurulum dizininin altında).</summary>
        public const string DataFolderName = "Data";

        /// <summary>
        /// Kurulum kök dizini. Servis exe'si <c>{Kurulum}\Service\</c> altında
        /// olduğundan, dizin adı "Service" ise bir üst dizine çıkılır.
        /// </summary>
        public static string InstallRoot { get; } = ResolveInstallRoot();

        /// <summary>
        /// Uygulama verileri kök dizini: <c>{Kurulum}\Data\</c>
        /// Hem Tray hem Service tarafından erişilir.
        /// </summary>
        private static readonly string AppDataRoot = Path.Combine(InstallRoot, DataFolderName);

        /// <summary>
        /// Eski konum: %ProgramData%\KoruMsSqlYedek (v0.76.0 – v0.99.94).
        /// Installer kurulumda buradaki verileri <c>Data</c> altına kopyalar;
        /// <see cref="MigrateProgramDataToInstallDir"/> çalışma zamanı yedeğidir.
        /// </summary>
        public static string LegacyProgramDataRoot { get; } = Path.Combine(
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

        private static string ResolveInstallRoot()
        {
            string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(Path.GetFileName(baseDir), "Service", StringComparison.OrdinalIgnoreCase))
            {
                string parent = Path.GetDirectoryName(baseDir);
                if (!string.IsNullOrEmpty(parent))
                    return parent;
            }
            return baseDir;
        }

        /// <summary>Plan JSON dosyaları dizini: {Kurulum}\Data\Plans\</summary>
        public static string PlansDirectory => Path.Combine(AppDataRoot, "Plans");

        /// <summary>Log dosyaları dizini: {Kurulum}\Data\Logs\</summary>
        public static string LogsDirectory => Path.Combine(AppDataRoot, "Logs");

        /// <summary>Genel ayarlar dizini: {Kurulum}\Data\Config\</summary>
        public static string ConfigDirectory => Path.Combine(AppDataRoot, "Config");

        /// <summary>Yarıda kalan upload durumları: {Kurulum}\Data\UploadState\</summary>
        public static string UploadStateDirectory => Path.Combine(AppDataRoot, "UploadState");

        /// <summary>Yedek geçmişi dizini: {Kurulum}\Data\History\</summary>
        public static string HistoryDirectory => Path.Combine(AppDataRoot, "History");

        /// <summary>Self-update installer'ları ve restart bayrağı: {Kurulum}\Data\Updates\</summary>
        public static string UpdatesDirectory => Path.Combine(AppDataRoot, "Updates");

        /// <summary>Uygulama verileri kök dizini: {Kurulum}\Data\</summary>
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
            Directory.CreateDirectory(HistoryDirectory);
        }

        /// <summary>
        /// Eski %ProgramData%\KoruMsSqlYedek konumundaki verileri <c>{Kurulum}\Data</c>
        /// altına kopyalar. Asıl kopyalama installer tarafından yapılır; bu metot
        /// installer adımı atlanmışsa (elle kurulum, eski installer) çalışma zamanı yedeğidir.
        /// Yeni konumda zaten plan varsa hiçbir şey yapmaz; var olan dosyaların üzerine yazmaz.
        /// </summary>
        /// <returns>En az bir dosya kopyalandıysa true.</returns>
        public static bool MigrateProgramDataToInstallDir()
        {
            if (!Directory.Exists(LegacyProgramDataRoot))
                return false;

            if (string.Equals(
                    Path.GetFullPath(LegacyProgramDataRoot).TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(AppDataRoot).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
                return false;

            if (Directory.Exists(PlansDirectory) && Directory.GetFiles(PlansDirectory, "*.json").Length > 0)
                return false;

            EnsureDirectoriesExist();
            return CopyTree(LegacyProgramDataRoot, AppDataRoot);
        }

        /// <summary>
        /// Kaynak ağacı hedefe kopyalar; var olan dosyaların üzerine yazmaz.
        /// Tek tek dosya hataları (erişim, kilit) yutulur — kalan dosyalar kopyalanmaya devam eder.
        /// </summary>
        private static bool CopyTree(string sourceRoot, string targetRoot)
        {
            bool copiedAny = false;

            foreach (string sourceDir in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
            {
                try { Directory.CreateDirectory(Path.Combine(targetRoot, Path.GetRelativePath(sourceRoot, sourceDir))); }
                catch { /* alt dizin oluşturulamazsa dosyaları da atlanır */ }
            }

            foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string targetFile = Path.Combine(targetRoot, Path.GetRelativePath(sourceRoot, sourceFile));
                if (File.Exists(targetFile))
                    continue;

                try
                {
                    File.Copy(sourceFile, targetFile);
                    copiedAny = true;
                }
                catch { /* erişilemeyen/kilitli dosya — atla */ }
            }

            return copiedAny;
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
        /// Eski kullanıcı %APPDATA% konumundaki verileri ortak veri dizinine kopyalar.
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
