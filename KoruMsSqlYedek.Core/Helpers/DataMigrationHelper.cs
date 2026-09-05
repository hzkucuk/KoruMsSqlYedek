using System;
using System.IO;
using System.Linq;
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
        /// Plan ve AppSettings dosyalarında düz metin kalmış gizli alanları DPAPI ile korur.
        /// Her Tray açılışında çalıştırılır; idempotent — zaten korumalı değerlere dokunmaz.
        /// Kapsam: bulut hedefi <c>password</c>, <c>oauthClientSecret</c>, <c>oauthTokenJson</c>
        /// (düz JSON, '{' ile başlar), SMTP profil şifreleri ve eski tekil SMTP şifresi.
        /// </summary>
        public static void ProtectPlaintextSecrets()
        {
            ProtectPlaintextSecretsInPlans(PathHelper.PlansDirectory);
            ProtectPlaintextSecretsInAppSettings(Path.Combine(PathHelper.ConfigDirectory, "appsettings.json"));
        }

        /// <summary>
        /// Verilen dizindeki plan JSON dosyalarında düz metin gizli alanları korur.
        /// Test edilebilirlik için dizin parametre olarak alınır.
        /// </summary>
        public static void ProtectPlaintextSecretsInPlans(string plansDir)
        {
            if (string.IsNullOrEmpty(plansDir) || !Directory.Exists(plansDir))
                return;

            foreach (string planFile in Directory.GetFiles(plansDir, "*.json"))
            {
                try
                {
                    JObject plan = JObject.Parse(File.ReadAllText(planFile));
                    bool modified = false;

                    modified |= ProtectField(plan, "sqlConnection.password");
                    modified |= ProtectField(plan, "compression.archivePassword");
                    modified |= ProtectField(plan, "notifications.smtpPassword");

                    if (plan["cloudTargets"] is JArray cloudTargets)
                    {
                        foreach (JObject target in cloudTargets.OfType<JObject>())
                        {
                            modified |= ProtectFieldDirect(target, "password");
                            modified |= ProtectFieldDirect(target, "oauthClientSecret");
                            modified |= ProtectOAuthTokenJson(target);
                        }
                    }

                    if (modified)
                    {
                        File.WriteAllText(planFile, plan.ToString(Formatting.Indented));
                        Log.Information(
                            "Düz metin gizli alanlar DPAPI ile korundu: {PlanFile}",
                            Path.GetFileName(planFile));
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Plan dosyasında düz metin koruma yapılamadı: {PlanFile}", Path.GetFileName(planFile));
                }
            }
        }

        /// <summary>
        /// AppSettings JSON dosyasındaki düz metin SMTP şifrelerini ve OAuth client secret'ı korur.
        /// </summary>
        public static void ProtectPlaintextSecretsInAppSettings(string settingsFile)
        {
            if (string.IsNullOrEmpty(settingsFile) || !File.Exists(settingsFile))
                return;

            try
            {
                JObject settings = JObject.Parse(File.ReadAllText(settingsFile));
                bool modified = false;

                modified |= ProtectField(settings, "googleOAuthClientSecret");

                if (settings["smtpProfiles"] is JArray profiles)
                {
                    foreach (JObject profile in profiles.OfType<JObject>())
                        modified |= ProtectFieldDirect(profile, "password");
                }

                modified |= ProtectField(settings, "smtp.password");

                if (modified)
                {
                    File.WriteAllText(settingsFile, settings.ToString(Formatting.Indented));
                    Log.Information("AppSettings düz metin gizli alanları DPAPI ile korundu.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "AppSettings düz metin koruma yapılamadı.");
            }
        }

        /// <summary>
        /// Google OAuth token JSON'u düz metin ise (ör. '{' ile başlıyorsa) DPAPI ile korur.
        /// </summary>
        private static bool ProtectOAuthTokenJson(JObject target)
        {
            string value = target["oauthTokenJson"]?.Value<string>();
            if (string.IsNullOrEmpty(value))
                return false;

            if (!value.TrimStart().StartsWith("{", StringComparison.Ordinal))
                return false;

            target["oauthTokenJson"] = PasswordProtector.Protect(value);
            return true;
        }

        /// <summary>
        /// Nokta-ayrılmış yollu alan düz metin ise DPAPI ile korur.
        /// </summary>
        private static bool ProtectField(JObject root, string dottedPath)
        {
            string[] parts = dottedPath.Split('.');
            JToken current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                current = current[parts[i]];
                if (current is null) return false;
            }

            return ProtectFieldDirect(current as JObject, parts[^1]);
        }

        /// <summary>
        /// Doğrudan JObject üzerindeki alan düz metin ise DPAPI ile korur.
        /// Başka bir bağlamda korunmuş (başlığı DPAPI olan) değerler tekrar şifrelenmez.
        /// </summary>
        private static bool ProtectFieldDirect(JObject obj, string fieldName)
        {
            if (obj is null) return false;

            string value = obj[fieldName]?.Value<string>();
            if (string.IsNullOrEmpty(value) || PasswordProtector.LooksProtected(value))
                return false;

            obj[fieldName] = PasswordProtector.Protect(value);
            return true;
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
