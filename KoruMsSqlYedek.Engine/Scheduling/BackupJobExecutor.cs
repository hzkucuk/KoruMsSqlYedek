using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Serilog;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.IPC;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Backup;

namespace KoruMsSqlYedek.Engine.Scheduling
{
    /// <summary>
    /// Quartz.NET IJob implementasyonu.
    /// Her tetiklemede yedekleme pipeline'ını çalıştırır:
    /// SQL Backup → Verify → Compress → Cloud Upload → Retention → History → Notify
    /// </summary>
    [DisallowConcurrentExecution]
    public class BackupJobExecutor : IJob
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<BackupJobExecutor>();

        // Bu alanlar Autofac property injection veya JobFactory ile doldurulur
        public IPlanManager PlanManager { get; set; }
        public ISqlBackupService SqlBackupService { get; set; }
        public ICompressionService CompressionService { get; set; }
        public INotificationService NotificationService { get; set; }
        public IRetentionService RetentionService { get; set; }
        public IFileBackupService FileBackupService { get; set; }
        public ICloudUploadOrchestrator CloudOrchestrator { get; set; }
        public IBackupHistoryManager HistoryManager { get; set; }
        public BackupChainValidator ChainValidator { get; set; }
        public IBackupCancellationRegistry CancellationRegistry { get; set; }

        public async Task Execute(IJobExecutionContext context)
        {
            string planId = context.MergedJobDataMap.GetString("planId");
            string backupType = context.MergedJobDataMap.GetString("backupType");
            string correlationId = Guid.NewGuid().ToString("N");

            Log.Information(
                "Job başlatılıyor: Plan={PlanId}, Tür={BackupType}, CorrelationId={CorrelationId}",
                planId, backupType, correlationId);

            BackupPlan plan = null;
            try
            {
                plan = PlanManager.GetPlanById(planId);
                if (plan == null)
                {
                    Log.Error("Plan bulunamadı: {PlanId}", planId);
                    return;
                }

                if (!plan.IsEnabled)
                {
                    Log.Information("Plan devre dışı, atlanıyor: {PlanName}", plan.PlanName);
                    return;
                }

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    ActivityType = BackupActivityType.Started,
                    TotalCount = plan.Databases?.Count ?? 0
                });

                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    context.CancellationToken);

                CancellationRegistry?.Register(planId, cts);
                try
                {

                        // Dosya yedekleme tipi
                        if (backupType == "FileBackup")
                        {
                            await ExecuteFileBackupAsync(plan, correlationId, cts.Token);
                            BackupActivityHub.Raise(new BackupActivityEventArgs
                            {
                                PlanId = plan.PlanId,
                                PlanName = plan.PlanName,
                                ActivityType = BackupActivityType.Completed
                            });
                            return;
                        }

                        // SQL yedekleme tipi
                        await ExecuteSqlBackupAsync(plan, backupType, correlationId, cts.Token);

                        // Dosya yedekleme — ayrı zamanlama yoksa SQL yedek ile birlikte çalıştır
                        if (plan.FileBackup != null && plan.FileBackup.IsEnabled &&
                            string.IsNullOrEmpty(plan.FileBackup.Schedule))
                        {
                            await ExecuteFileBackupAsync(plan, correlationId, cts.Token);
                        }

                        BackupActivityHub.Raise(new BackupActivityEventArgs
                        {
                            PlanId = plan.PlanId,
                            PlanName = plan.PlanName,
                            ActivityType = BackupActivityType.Completed
                        });
                    }
                    finally
                    {
                        CancellationRegistry?.Unregister(planId);
                    }
                }
                catch (OperationCanceledException)
            {
                Log.Warning("Job iptal edildi: Plan={PlanId}, CorrelationId={CorrelationId}", planId, correlationId);
                if (plan != null)
                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.Cancelled
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Job hatası: Plan={PlanId}, CorrelationId={CorrelationId}", planId, correlationId);
                if (plan != null)
                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.Failed,
                        Message = ex.Message
                    });
            }
        }

        private async Task ExecuteSqlBackupAsync(
            BackupPlan plan, string backupType, string correlationId, CancellationToken ct)
        {
            SqlBackupType sqlType;
            switch (backupType)
            {
                case "Full":
                    sqlType = SqlBackupType.Full;
                    break;
                case "Differential":
                    sqlType = SqlBackupType.Differential;
                    break;
                case "Incremental":
                    sqlType = SqlBackupType.Incremental;
                    break;
                default:
                    Log.Error("Bilinmeyen yedek türü: {BackupType}", backupType);
                    return;
            }

            for (int i = 0; i < plan.Databases.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                string dbName = plan.Databases[i];

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    DatabaseName = dbName,
                    ActivityType = BackupActivityType.DatabaseProgress,
                    CurrentIndex = i + 1,
                    TotalCount = plan.Databases.Count
                });

                var effectiveType = sqlType;

                // Zincir bütünlük kontrolü
                if (effectiveType == SqlBackupType.Differential || effectiveType == SqlBackupType.Incremental)
                {
                    if (ChainValidator != null && !ChainValidator.HasValidFullBackup(plan.LocalPath, dbName))
                    {
                        Log.Warning(
                            "Geçerli Full yedek bulunamadı, Full'e yükseltiliyor: {Database}", dbName);
                        effectiveType = SqlBackupType.Full;
                    }
                }

                // Otomatik Full yükseltme kontrolü
                if (effectiveType == SqlBackupType.Differential && ChainValidator != null)
                {
                    if (ChainValidator.ShouldPromoteToFull(
                        plan.LocalPath, dbName, plan.Strategy.AutoPromoteToFullAfter))
                    {
                        effectiveType = SqlBackupType.Full;
                    }
                }

                // 1. SQL Backup
                var result = await SqlBackupService.BackupDatabaseAsync(
                    plan.SqlConnection, dbName, effectiveType, plan.LocalPath,
                    null, ct);

                result.PlanId = plan.PlanId;
                result.PlanName = plan.PlanName;
                result.CorrelationId = correlationId;

                if (result.Status != BackupResultStatus.Success)
                {
                    SaveHistory(result);
                    await NotifyIfConfigured(result, plan, ct);
                    continue;
                }

                // 2. Verify
                if (plan.VerifyAfterBackup)
                {
                    result.VerifyResult = await SqlBackupService.VerifyBackupAsync(
                        plan.SqlConnection, result.BackupFilePath, ct);

                    if (result.VerifyResult == false)
                    {
                        Log.Error("Yedek doğrulama başarısız: {Database}", dbName);
                    }
                }

                // 3. Compress
                if (plan.Compression != null)
                {
                    try
                    {
                        string archivePath = Path.ChangeExtension(result.BackupFilePath, ".7z");
                        string password = !string.IsNullOrEmpty(plan.Compression.ArchivePassword)
                            ? PasswordProtector.Unprotect(plan.Compression.ArchivePassword)
                            : null;

                        result.CompressedSizeBytes = await CompressionService.CompressAsync(
                            result.BackupFilePath, archivePath, password, null, ct);
                        result.CompressedFilePath = archivePath;

                        // 3b. Arşiv bütünlük doğrulaması
                        if (plan.VerifyAfterBackup)
                        {
                            result.CompressionVerified = await CompressionService.VerifyArchiveAsync(
                                archivePath, password, ct);

                            if (result.CompressionVerified == false)
                            {
                                Log.Error(
                                    "Arşiv bütünlük doğrulaması başarısız: {Database} — {Archive}",
                                    dbName, archivePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Sıkıştırma hatası: {Database}", dbName);
                    }
                }

                // 4. Cloud Upload
                if (CloudOrchestrator != null && plan.CloudTargets != null &&
                    plan.CloudTargets.Any(t => t.IsEnabled))
                {
                    try
                    {
                        string fileToUpload = !string.IsNullOrEmpty(result.CompressedFilePath)
                            ? result.CompressedFilePath
                            : result.BackupFilePath;

                        string remoteFileName = Path.GetFileName(fileToUpload);

                        result.CloudUploadResults = await CloudOrchestrator.UploadToAllAsync(
                            fileToUpload, remoteFileName, plan.CloudTargets, null, ct,
                            plan.PlanName);

                        int successCount = result.CloudUploadResults.Count(r => r.IsSuccess);
                        int totalCount = result.CloudUploadResults.Count;
                        Log.Information(
                            "Bulut upload tamamlandı: {Database} — {Success}/{Total} başarılı",
                            dbName, successCount, totalCount);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Bulut upload hatası: {Database}", dbName);
                    }
                }

                // 5. Retention
                if (RetentionService != null)
                {
                    try
                    {
                        await RetentionService.CleanupAsync(plan, ct);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Retention temizliği hatası: {Database}", dbName);
                    }
                }

                // 6. History
                SaveHistory(result);

                // 7. Notify
                await NotifyIfConfigured(result, plan, ct);
            }
        }

        private async Task ExecuteFileBackupAsync(BackupPlan plan, string correlationId, CancellationToken ct)
        {
            if (FileBackupService == null || plan.FileBackup == null || !plan.FileBackup.IsEnabled)
                return;

            Log.Information("Dosya yedekleme başlıyor: Plan={PlanName}, CorrelationId={CorrelationId}",
                plan.PlanName, correlationId);

            var results = await FileBackupService.BackupFilesAsync(plan, null, ct);

            foreach (var fileResult in results)
            {
                Log.Information(
                    "Dosya yedekleme tamamlandı: {SourceName} — {FilesCopied} dosya, {Status}",
                    fileResult.SourceName, fileResult.FilesCopied, fileResult.Status);
            }

            // Dosya yedekleri sıkıştırma
            if (plan.Compression != null && results.Any(r => r.Status == BackupResultStatus.Success))
            {
                try
                {
                    string filesDir = Path.Combine(plan.LocalPath, "Files");
                    if (Directory.Exists(filesDir))
                    {
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string archivePath = Path.Combine(plan.LocalPath, $"Files_{timestamp}.7z");
                        string password = !string.IsNullOrEmpty(plan.Compression.ArchivePassword)
                            ? PasswordProtector.Unprotect(plan.Compression.ArchivePassword)
                            : null;

                        var compressionService = CompressionService as Engine.Compression.SevenZipCompressionService;
                        if (compressionService != null)
                        {
                            await compressionService.CompressDirectoryAsync(
                                filesDir, archivePath, password,
                                plan.Compression.Level, null, ct);

                            // Sıkıştırılmış dosya yedeklerini buluta gönder
                            if (CloudOrchestrator != null && plan.CloudTargets != null &&
                                plan.CloudTargets.Any(t => t.IsEnabled))
                            {
                                await CloudOrchestrator.UploadToAllAsync(
                                    archivePath, Path.GetFileName(archivePath),
                                    plan.CloudTargets, null, ct);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Dosya yedek sıkıştırma/upload hatası");
                }
            }
        }

        private void SaveHistory(BackupResult result)
        {
            try
            {
                HistoryManager?.SaveResult(result);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Yedek geçmişi kaydedilemedi: {CorrelationId}", result.CorrelationId);
            }
        }

        private async Task NotifyIfConfigured(BackupResult result, BackupPlan plan, CancellationToken ct)
        {
            if (NotificationService != null && plan.Notifications != null)
            {
                try
                {
                    await NotificationService.NotifyAsync(result, plan.Notifications, ct);
                }
                catch (Exception ex)
                {
                    // Bildirim hatası yedek başarısını ETKİLEMEZ
                    Log.Error(ex, "Bildirim gönderilemedi: {CorrelationId}", result.CorrelationId);
                }
            }
        }
    }
}
