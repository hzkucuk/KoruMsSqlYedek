using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace KoruMsSqlYedek.Core.Helpers
{
    /// <summary>
    /// v0.75.1 → v0.76.0 migrasyon yardımcısı.
    /// Eski %APPDATA% konumundaki verileri %ProgramData% altına taşır ve
    /// DPAPI şifrelerini CurrentUser → LocalMachine scope'a dönüştürür.
    /// Bu sınıf yalnızca Tray uygulaması (kullanıcı bağlamında) tarafından çalıştırılmalıdır;
    /// çünkü CurrentUser scope şifrelerini yalnızca orijinal kullanıcı çözebilir.
    /// </summary>
    public static class DataMigrationHelper
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof(DataMigrationHelper));

        /// <summary>
        /// Migrasyon gerekli mi kontrol eder ve gerekiyorsa uygular.
        /// Idempotent: birden fazla çalıştırılabilir, zaten taşınmış veriler tekrar taşınmaz.
        /// </summary>
        public static void MigrateIfNeeded()
        {
            string oldRoot = PathHelper.LegacyUserAppDataRoot;

            if (!Directory.Exists(oldRoot))
            {
                Log.Debug("Eski %APPDATA% konumu bulunamadı — migrasyon gerekli değil.");
                return;
            }

            string newPlansDir = PathHelper.PlansDirectory;
            if (Directory.Exists(newPlansDir) && Directory.GetFiles(newPlansDir, "*.json").Length > 0)
            {
                Log.Debug("Yeni konumda zaten plan dosyaları var — migrasyon atlanıyor.");
                return;
            }

            Log.Information(
                "Veri migrasyonu başlatılıyor: {OldRoot} → {NewRoot}",
                oldRoot, PathHelper.AppDataDirectory);

            try
            {
                // 1. Dosyaları kopyala
                CopyDirectoryContents(oldRoot, PathHelper.AppDataDirectory);

                // 2. Plan dosyalarındaki DPAPI şifrelerini LocalMachine scope'a dönüştür
                MigratePlanPasswords();

                // 3. AppSettings dosyasındaki DPAPI şifrelerini dönüştür
                MigrateAppSettingsPasswords();

                Log.Information("Veri migrasyonu başarıyla tamamlandı.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Veri migrasyonu sırasında hata oluştu.");
            }
        }

        /// <summary>
        /// Plan JSON dosyalarındaki tüm DPAPI-korumalı alanları LocalMachine scope'a dönüştürür.
        /// </summary>
        private static void MigratePlanPasswords()
        {
            string plansDir = PathHelper.PlansDirectory;
            if (!Directory.Exists(plansDir))
                return;

            foreach (string planFile in Directory.GetFiles(plansDir, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(planFile);
                    JObject plan = JObject.Parse(json);
                    bool modified = false;

                    // SqlConnectionInfo.Password
                    modified |= MigrateField(plan, "sqlConnection.password");

                    // CompressionConfig.ArchivePassword
                    modified |= MigrateField(plan, "compression.archivePassword");

                    // NotificationConfig.SmtpPassword (eski per-plan SMTP)
                    modified |= MigrateField(plan, "notifications.smtpPassword");

                    // BackupPlan.PasswordHash
                    modified |= MigrateField(plan, "passwordHash");

                    // CloudTargets — her bir bulut hedefi için
                    JArray cloudTargets = plan["cloudTargets"] as JArray;
                    if (cloudTargets is not null)
                    {
                        foreach (JObject target in cloudTargets)
                        {
                            modified |= MigrateFieldDirect(target, "password");
                            modified |= MigrateFieldDirect(target, "oauthClientSecret");
                        }
                    }

                    if (modified)
                    {
                        File.WriteAllText(planFile, plan.ToString(Formatting.Indented));
                        Log.Information("Plan şifreleri migrate edildi: {PlanFile}", Path.GetFileName(planFile));
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Plan dosyası migrate edilemedi: {PlanFile}", Path.GetFileName(planFile));
                }
            }
        }

        /// <summary>
        /// AppSettings JSON dosyasındaki DPAPI-korumalı alanları LocalMachine scope'a dönüştürür.
        /// </summary>
        private static void MigrateAppSettingsPasswords()
        {
            string settingsFile = Path.Combine(PathHelper.ConfigDirectory, "appsettings.json");
            if (!File.Exists(settingsFile))
                return;

            try
            {
                string json = File.ReadAllText(settingsFile);
                JObject settings = JObject.Parse(json);
                bool modified = false;

                // Global PasswordHash
                modified |= MigrateField(settings, "passwordHash");

                // SecurityAnswerHash
                modified |= MigrateField(settings, "securityAnswerHash");

                // GoogleOAuthClientSecret
                modified |= MigrateField(settings, "googleOAuthClientSecret");

                // SmtpProfiles — her profil için
                JArray profiles = settings["smtpProfiles"] as JArray;
                if (profiles is not null)
                {
                    foreach (JObject profile in profiles)
                    {
                        modified |= MigrateFieldDirect(profile, "password");
                    }
                }

                // Eski tekil SMTP ayarı
                modified |= MigrateField(settings, "smtp.password");

                if (modified)
                {
                    File.WriteAllText(settingsFile, settings.ToString(Formatting.Indented));
                    Log.Information("AppSettings şifreleri migrate edildi.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "AppSettings migrate edilemedi.");
            }
        }

        /// <summary>
        /// Nokta-ayrılmış yollu alanı DPAPI LocalMachine scope'a dönüştürür.
        /// </summary>
        private static bool MigrateField(JObject root, string dottedPath)
        {
            string[] parts = dottedPath.Split('.');
            JToken current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                current = current[parts[i]];
                if (current is null) return false;
            }

            string fieldName = parts[^1];
            return MigrateFieldDirect(current as JObject, fieldName);
        }

        /// <summary>
        /// Doğrudan JObject üzerindeki alanı DPAPI LocalMachine scope'a dönüştürür.
        /// </summary>
        private static bool MigrateFieldDirect(JObject obj, string fieldName)
        {
            if (obj is null) return false;

            string value = obj[fieldName]?.Value<string>();
            if (string.IsNullOrEmpty(value))
                return false;

            string migrated = PasswordProtector.MigrateToLocalMachine(value);
            if (migrated is null || migrated == value)
                return false;

            obj[fieldName] = migrated;
            return true;
        }

        /// <summary>
        /// Kaynak dizinin tüm içeriğini hedef dizine kopyalar (alt dizinler dahil).
        /// </summary>
        private static void CopyDirectoryContents(string sourceRoot, string targetRoot)
        {
            foreach (string sourceDir in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string targetDir = sourceDir.Replace(sourceRoot, targetRoot);
                Directory.CreateDirectory(targetDir);
            }

            foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string targetFile = sourceFile.Replace(sourceRoot, targetRoot);
                if (!File.Exists(targetFile))
                {
                    File.Copy(sourceFile, targetFile);
                }
            }
        }
    }
}
