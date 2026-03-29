using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine
{
    /// <summary>
    /// JSON dosya tabanlı plan yöneticisi.
    /// Planlar %APPDATA%\KoruMsSqlYedek\Plans\{planId}.json olarak saklanır.
    /// Schema migration: Eski versiyon planlar yüklenirken otomatik yükseltilir.
    /// </summary>
    public class PlanManager : IPlanManager
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<PlanManager>();
        private const int CurrentSchemaVersion = 2;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include
        };

        public PlanManager()
        {
            PathHelper.EnsureDirectoriesExist();
        }

        public List<BackupPlan> GetAllPlans()
        {
            var plans = new List<BackupPlan>();
            string plansDir = PathHelper.PlansDirectory;

            if (!Directory.Exists(plansDir))
                return plans;

            foreach (string file in Directory.GetFiles(plansDir, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var plan = DeserializeAndMigrate(json, file);
                    if (plan != null)
                        plans.Add(plan);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plan dosyası okunamadı: {FilePath}", file);
                }
            }

            return plans;
        }

        public BackupPlan GetPlanById(string planId)
        {
            if (string.IsNullOrEmpty(planId))
                return null;

            string filePath = PathHelper.GetPlanFilePath(planId);
            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);
            return DeserializeAndMigrate(json, filePath);
        }

        public void SavePlan(BackupPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            ValidatePlan(plan);

            plan.SchemaVersion = CurrentSchemaVersion;
            plan.LastModifiedAt = DateTime.UtcNow;
            string filePath = PathHelper.GetPlanFilePath(plan.PlanId);
            string json = JsonConvert.SerializeObject(plan, JsonSettings);

            PathHelper.EnsureDirectoriesExist();
            File.WriteAllText(filePath, json);
            Log.Information("Plan kaydedildi: {PlanId} - {PlanName}", plan.PlanId, plan.PlanName);
        }

        public bool DeletePlan(string planId)
        {
            if (string.IsNullOrEmpty(planId))
                return false;

            string filePath = PathHelper.GetPlanFilePath(planId);
            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);
            Log.Information("Plan silindi: {PlanId}", planId);
            return true;
        }

        public void ExportPlan(string planId, string exportFilePath)
        {
            if (string.IsNullOrEmpty(planId))
                throw new ArgumentNullException(nameof(planId));
            if (string.IsNullOrEmpty(exportFilePath))
                throw new ArgumentNullException(nameof(exportFilePath));

            var plan = GetPlanById(planId);
            if (plan == null)
                throw new FileNotFoundException($"Plan bulunamadı: {planId}");

            string json = JsonConvert.SerializeObject(plan, JsonSettings);
            File.WriteAllText(exportFilePath, json);
            Log.Information("Plan dışa aktarıldı: {PlanId} → {Path}", planId, exportFilePath);
        }

        public BackupPlan ImportPlan(string importFilePath)
        {
            if (string.IsNullOrEmpty(importFilePath))
                throw new ArgumentNullException(nameof(importFilePath));
            if (!File.Exists(importFilePath))
                throw new FileNotFoundException($"Dosya bulunamadı: {importFilePath}");

            string json = File.ReadAllText(importFilePath);
            var plan = DeserializeAndMigrate(json, importFilePath);

            if (plan == null)
                throw new InvalidOperationException($"Geçersiz plan dosyası: {importFilePath}");

            // Yeni GUID ata (çakışma önleme)
            plan.PlanId = Guid.NewGuid().ToString();
            plan.CreatedAt = DateTime.UtcNow;
            plan.LastModifiedAt = DateTime.UtcNow;

            SavePlan(plan);
            Log.Information("Plan içe aktarıldı: {PlanId} - {PlanName}", plan.PlanId, plan.PlanName);
            return plan;
        }

        #region Schema Migration

        /// <summary>
        /// JSON'ı deserialize eder, eski schema versiyonlarını otomatik yükseltir.
        /// </summary>
        private BackupPlan DeserializeAndMigrate(string json, string sourceFile)
        {
            var jObject = JObject.Parse(json);
            int schemaVersion = jObject["schemaVersion"]?.Value<int>() ?? 1;

            if (schemaVersion < CurrentSchemaVersion)
            {
                jObject = MigrateSchema(jObject, schemaVersion);
                Log.Information(
                    "Plan şeması yükseltildi: v{OldVersion} → v{NewVersion} ({File})",
                    schemaVersion, CurrentSchemaVersion, Path.GetFileName(sourceFile));
            }

            var plan = jObject.ToObject<BackupPlan>(JsonSerializer.Create(JsonSettings));

            // Migration sonrası kaydet (dosyayı güncelle)
            if (schemaVersion < CurrentSchemaVersion && plan != null)
            {
                plan.SchemaVersion = CurrentSchemaVersion;
                string filePath = PathHelper.GetPlanFilePath(plan.PlanId);
                string updatedJson = JsonConvert.SerializeObject(plan, JsonSettings);
                File.WriteAllText(filePath, updatedJson);
            }

            return plan;
        }

        /// <summary>
        /// Schema versiyonlarını sırasıyla yükseltir.
        /// </summary>
        private JObject MigrateSchema(JObject plan, int fromVersion)
        {
            // v1 → v2: FileBackup alanı eklendi, VerifyAfterBackup varsayılan true
            if (fromVersion < 2)
            {
                if (plan["fileBackup"] == null)
                {
                    plan["fileBackup"] = null; // Opsiyonel alan, null bırakılır
                }

                if (plan["verifyAfterBackup"] == null)
                {
                    plan["verifyAfterBackup"] = true;
                }

                plan["schemaVersion"] = 2;
            }

            // Gelecek migration'lar buraya eklenir:
            // if (fromVersion < 3) { ... plan["schemaVersion"] = 3; }

            return plan;
        }

        #endregion

        #region Plan Validation

        /// <summary>
        /// Plan'ın temel geçerlilik kontrollerini yapar.
        /// </summary>
        private void ValidatePlan(BackupPlan plan)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(plan.PlanName))
                errors.Add("Plan adı boş olamaz.");

            if (plan.SqlConnection == null || string.IsNullOrWhiteSpace(plan.SqlConnection.Server))
            {
                // SQL bağlantı zorunlu değil (sadece dosya yedek planı olabilir)
                bool hasSqlDatabases = plan.Databases != null && plan.Databases.Count > 0;
                if (hasSqlDatabases)
                    errors.Add("SQL veritabanı seçili ama sunucu adresi boş.");
            }

            if (string.IsNullOrWhiteSpace(plan.LocalPath))
                errors.Add("Yerel yedek dizini boş olamaz.");

            // Strategy cron kontrolü
            if (plan.Strategy != null)
            {
                bool hasDatabases = plan.Databases != null && plan.Databases.Count > 0;
                if (hasDatabases && string.IsNullOrEmpty(plan.Strategy.FullSchedule))
                    errors.Add("SQL yedekleme için en az Full cron zamanlaması gerekli.");
            }

            if (errors.Count > 0)
            {
                string allErrors = string.Join("; ", errors);
                Log.Warning("Plan doğrulama uyarıları: {PlanName} — {Errors}", plan.PlanName, allErrors);
                // Uyarı olarak logla, kaydetmeyi engelleme (kullanıcı draft olarak kaydedebilir)
            }
        }

        #endregion
    }
}
