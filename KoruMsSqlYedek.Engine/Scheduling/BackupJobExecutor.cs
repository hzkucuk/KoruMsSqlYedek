using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public partial class BackupJobExecutor : IJob
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<BackupJobExecutor>();

        /// <summary>
        /// Plan başına eşzamanlı çalışmayı engelleyen kilit.
        /// Farklı planlar paralel çalışabilir, aynı plan ise bekler.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _planLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

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
            bool lockAcquired = false;
            var cleanupPaths = new List<string>();
            var logLines = new List<string>();
            DateTime jobStartedAt = DateTime.Now;
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

                var planLock = _planLocks.GetOrAdd(planId, _ => new SemaphoreSlim(1, 1));
                if (!await planLock.WaitAsync(0, context.CancellationToken))
                {
                    Log.Warning(
                        "Bu plan zaten çalışıyor, atlanıyor: Plan={PlanId}, Tür={BackupType}",
                        planId, backupType);
                    return;
                }

                lockAcquired = true;

                // Dosya yedekleme koşulunu Started event'inden ÖNCE hesapla (progress ağırlıkları için)
                // Dosya yedekleme SQL ile aynı zamanlamayı kullanır; ayrı schedule yok
                bool willRunFileBackup = backupType == "FileBackup"
                    || (plan.FileBackup != null && plan.FileBackup.IsEnabled);

                int sqlDbCount = backupType == "FileBackup" ? 0 : (plan.Databases?.Count ?? 0);

                bool hasCloudTargets = CloudOrchestrator != null
                    && plan.CloudTargets != null
                    && plan.CloudTargets.Any(t => t.IsEnabled);

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    ActivityType = BackupActivityType.Started,
                    TotalCount = sqlDbCount,
                    HasFileBackup = willRunFileBackup,
                    HasCloudTargets = hasCloudTargets
                });

                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    context.CancellationToken);

                jobStartedAt = DateTime.Now;

                CancellationRegistry?.Register(planId, cts);
                try
                {

                        void OnActivity(object sender, BackupActivityEventArgs ae)
                        {
                            if (ae.PlanId != planId) return;
                            // İlerleme satırlarını atlayalım, sadece anlamlı adımları toplayalım
                            if (ae.ActivityType == BackupActivityType.CloudUploadProgress) return;
                            string line = FormatActivityLogLine(ae);
                            if (!string.IsNullOrEmpty(line))
                                logLines.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
                        }

                        BackupActivityHub.ActivityChanged += OnActivity;

                        try
                        {

                        // Dosya yedekleme tipi
                        if (backupType == "FileBackup")
                        {
                            var (fileResults, fileArchivePath) =
                                await ExecuteFileBackupAsync(plan, correlationId, cts.Token, cleanupPaths);

                            // Konsolide bulut yükleme (tüm dosyalar tek seferde)
                            var (cloudOk, fileCloudResults) = await UploadAllPendingAsync(
                                plan, null, fileArchivePath, cts.Token);

                            await EmptyTrashIfNeededAsync(plan, cts.Token);
                            cleanupPaths.Clear();

                            bool anyFileSourceFailed = fileResults != null && fileResults.Any(r => r.Status != BackupResultStatus.Success);
                            bool fileOverallSuccess = cloudOk && !anyFileSourceFailed;

                            logLines.Add($"[{DateTime.Now:HH:mm:ss}] [{plan.PlanName}] Yedekleme tamamlandı. {(fileOverallSuccess ? "✓" : "⚠")}");

                            // Konsolide bildirim
                            await SendConsolidatedNotificationAsync(plan, new JobNotificationData
                            {
                                PlanName = plan.PlanName,
                                PlanId = plan.PlanId,
                                BackupType = "Dosya Yedekleme",
                                CorrelationId = correlationId,
                                StartedAt = jobStartedAt,
                                CompletedAt = DateTime.Now,
                                IsSuccess = fileOverallSuccess,
                                FileResults = fileResults ?? new List<FileBackupResult>(),
                                FileArchiveFileName = !string.IsNullOrEmpty(fileArchivePath) ? Path.GetFileName(fileArchivePath) : null,
                                FileArchiveSizeBytes = GetFileSize(fileArchivePath),
                                FileCloudUploadResults = fileCloudResults ?? new List<CloudUploadResult>(),
                                LogLines = logLines
                            }, cts.Token);

                            BackupActivityHub.Raise(new BackupActivityEventArgs
                            {
                                PlanId = plan.PlanId,
                                PlanName = plan.PlanName,
                                ActivityType = BackupActivityType.Completed,
                                IsSuccess = fileOverallSuccess,
                                Message = !fileOverallSuccess
                                    ? (anyFileSourceFailed ? "Dosya yedekleme başarısız" : "Bulut yükleme başarısız")
                                    : (hasCloudTargets ? "Doğruluk kontrolü tamamlandı" : null)
                            });
                            return;
                        }

                        // SQL yedekleme tipi
                        var (sqlResults, sqlPendingUploads) = await ExecuteSqlBackupAsync(plan, backupType, correlationId, cts.Token, cleanupPaths);

                        // Dosya yedekleme — SQL yedek ile aynı zamanlamada çalıştır
                        List<FileBackupResult> fileResults2 = null;
                        string fileArchivePath2 = null;

                        if (willRunFileBackup && backupType != "FileBackup")
                        {
                            var fileResult = await ExecuteFileBackupAsync(plan, correlationId, cts.Token, cleanupPaths);
                            fileResults2 = fileResult.FileResults;
                            fileArchivePath2 = fileResult.ArchivePath;
                        }

                        // Konsolide bulut yükleme (SQL + dosya dosyaları tek seferde)
                        var (allCloudOk, fileCloudResults2) = await UploadAllPendingAsync(
                            plan, sqlPendingUploads, fileArchivePath2, cts.Token);

                        // Tüm SQL sonuçlarını history'e kaydet (cloud sonuçları UploadAllPendingAsync içinde atandı)
                        if (sqlResults != null)
                        {
                            foreach (var sqlResult in sqlResults)
                                SaveHistory(sqlResult);
                        }

                        bool anySqlFailed = sqlResults != null && sqlResults.Any(r => r.Status != BackupResultStatus.Success);
                        bool anyFileFailed = fileResults2 != null && fileResults2.Any(r => r.Status != BackupResultStatus.Success);
                        bool overallSuccess = allCloudOk && !anySqlFailed && !anyFileFailed;
                        await EmptyTrashIfNeededAsync(plan, cts.Token);
                        cleanupPaths.Clear();

                        logLines.Add($"[{DateTime.Now:HH:mm:ss}] [{plan.PlanName}] Yedekleme tamamlandı. {(overallSuccess ? "✓" : "⚠")}");

                        // Konsolide bildirim
                        await SendConsolidatedNotificationAsync(plan, new JobNotificationData
                        {
                            PlanName = plan.PlanName,
                            PlanId = plan.PlanId,
                            BackupType = backupType,
                            CorrelationId = correlationId,
                            StartedAt = jobStartedAt,
                            CompletedAt = DateTime.Now,
                            IsSuccess = overallSuccess,
                            SqlResults = sqlResults ?? new List<BackupResult>(),
                            FileResults = fileResults2 ?? new List<FileBackupResult>(),
                            FileArchiveFileName = !string.IsNullOrEmpty(fileArchivePath2) ? Path.GetFileName(fileArchivePath2) : null,
                            FileArchiveSizeBytes = GetFileSize(fileArchivePath2),
                            FileCloudUploadResults = fileCloudResults2 ?? new List<CloudUploadResult>(),
                            LogLines = logLines
                        }, cts.Token);

                        BackupActivityHub.Raise(new BackupActivityEventArgs
                        {
                            PlanId = plan.PlanId,
                            PlanName = plan.PlanName,
                            ActivityType = BackupActivityType.Completed,
                            IsSuccess = overallSuccess,
                            Message = !overallSuccess
                                ? (anySqlFailed ? "SQL yedekleme başarısız" : "Bulut yükleme başarısız")
                                : (hasCloudTargets ? "Doğruluk kontrolü tamamlandı" : null)
                        });

                        }
                        finally
                        {
                            BackupActivityHub.ActivityChanged -= OnActivity;
                        }
                    }
                    finally
                    {
                        CancellationRegistry?.Unregister(planId);
                    }
                }
                catch (OperationCanceledException)
            {
                Log.Warning("Job iptal edildi: Plan={PlanId}, CorrelationId={CorrelationId}", planId, correlationId);
                CleanupOnFailure(cleanupPaths, planId);
                if (plan != null)
                {
                    logLines.Add($"[{DateTime.Now:HH:mm:ss}] [{plan.PlanName}] Yedekleme iptal edildi.");

                    await SendConsolidatedNotificationAsync(plan, new JobNotificationData
                    {
                        PlanName = plan.PlanName,
                        PlanId = plan.PlanId,
                        BackupType = backupType,
                        CorrelationId = correlationId,
                        StartedAt = jobStartedAt,
                        CompletedAt = DateTime.Now,
                        IsSuccess = false,
                        LogLines = logLines
                    }, CancellationToken.None);

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.Cancelled,
                        Message = cleanupPaths.Count > 0
                            ? $"{cleanupPaths.Count} ara dosya/klasör temizlendi"
                            : null
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Job hatası: Plan={PlanId}, CorrelationId={CorrelationId}", planId, correlationId);
                CleanupOnFailure(cleanupPaths, planId);
                if (plan != null)
                {
                    logLines.Add($"[{DateTime.Now:HH:mm:ss}] [{plan.PlanName}] Yedekleme başarısız: {ex.Message}");

                    await SendConsolidatedNotificationAsync(plan, new JobNotificationData
                    {
                        PlanName = plan.PlanName,
                        PlanId = plan.PlanId,
                        BackupType = backupType,
                        CorrelationId = correlationId,
                        StartedAt = jobStartedAt,
                        CompletedAt = DateTime.Now,
                        IsSuccess = false,
                        LogLines = logLines
                    }, CancellationToken.None);

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.Failed,
                        Message = ex.Message
                    });
                }
            }
            finally
            {
                if (lockAcquired && _planLocks.TryGetValue(planId, out var releaseLock))
                    releaseLock.Release();
            }
        }
    }
}
