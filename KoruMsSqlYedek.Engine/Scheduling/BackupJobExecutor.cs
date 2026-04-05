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
    public class BackupJobExecutor : IJob
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
                bool isManualTrigger = context.MergedJobDataMap.GetString("manualTrigger") == "true";
                bool willRunFileBackup = backupType == "FileBackup"
                    || (plan.FileBackup != null && plan.FileBackup.IsEnabled
                        && (string.IsNullOrEmpty(plan.FileBackup.Schedule) || isManualTrigger));

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

                CancellationRegistry?.Register(planId, cts);
                try
                {

                        // Dosya yedekleme tipi
                        if (backupType == "FileBackup")
                        {
                            bool fileOk = await ExecuteFileBackupAsync(plan, correlationId, cts.Token, cleanupPaths);
                            await EmptyTrashIfNeededAsync(plan, cts.Token);
                            cleanupPaths.Clear();
                            BackupActivityHub.Raise(new BackupActivityEventArgs
                            {
                                PlanId = plan.PlanId,
                                PlanName = plan.PlanName,
                                ActivityType = BackupActivityType.Completed,
                                IsSuccess = fileOk,
                                Message = fileOk ? null : "Bulut yükleme başarısız"
                            });
                            return;
                        }

                        // SQL yedekleme tipi
                        bool sqlCloudOk = await ExecuteSqlBackupAsync(plan, backupType, correlationId, cts.Token, cleanupPaths);

                        // Dosya yedekleme — ayrı zamanlama yoksa veya manuel tetikleme ise SQL yedek ile birlikte çalıştır
                        bool fileCloudOk = true;
                        if (willRunFileBackup && backupType != "FileBackup")
                        {
                            fileCloudOk = await ExecuteFileBackupAsync(plan, correlationId, cts.Token, cleanupPaths);
                        }

                        bool allCloudOk = sqlCloudOk && fileCloudOk;
                        await EmptyTrashIfNeededAsync(plan, cts.Token);
                        cleanupPaths.Clear();
                        BackupActivityHub.Raise(new BackupActivityEventArgs
                        {
                            PlanId = plan.PlanId,
                            PlanName = plan.PlanName,
                            ActivityType = BackupActivityType.Completed,
                            IsSuccess = allCloudOk,
                            Message = allCloudOk ? null : "Bulut yükleme başarısız"
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
                CleanupOnFailure(cleanupPaths, planId);
                if (plan != null)
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
            catch (Exception ex)
            {
                Log.Error(ex, "Job hatası: Plan={PlanId}, CorrelationId={CorrelationId}", planId, correlationId);
                CleanupOnFailure(cleanupPaths, planId);
                if (plan != null)
                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.Failed,
                        Message = ex.Message
                    });
            }
            finally
            {
                if (lockAcquired && _planLocks.TryGetValue(planId, out var releaseLock))
                    releaseLock.Release();
            }
        }

        private async Task<bool> ExecuteSqlBackupAsync(
            BackupPlan plan, string backupType, string correlationId, CancellationToken ct,
            List<string> cleanupPaths)
        {
            bool cloudAllOk = true;
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
                    return true;
            }

            for (int i = 0; i < plan.Databases.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                int cleanupSnapshot = cleanupPaths.Count;
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

                // Ara dosya takibi: .bak
                if (!string.IsNullOrEmpty(result.BackupFilePath) && File.Exists(result.BackupFilePath))
                    cleanupPaths.Add(result.BackupFilePath);

                if (result.Status != BackupResultStatus.Success)
                {
                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        DatabaseName = dbName,
                        ActivityType = BackupActivityType.StepChanged,
                        StepName = "SQL Yedekleme",
                        Message = $"Yedekleme başarısız: {dbName} — {result.ErrorMessage}"
                    });
                    SaveHistory(result);
                    await NotifyIfConfigured(result, plan, ct);
                    continue;
                }

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    DatabaseName = dbName,
                    ActivityType = BackupActivityType.StepChanged,
                    StepName = "SQL Yedekleme",
                    Message = $"Yedekleme başarılı: {dbName} ({effectiveType}) ␦ {Path.GetFileName(result.BackupFilePath)} [{Fmt(result.FileSizeBytes)}]"
                });

                // 1b. VSS bilgisi
                if (result.VssFileCopySizeBytes > 0 && !string.IsNullOrEmpty(result.VssFileCopyPath))
                {
                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        DatabaseName = dbName,
                        ActivityType = BackupActivityType.StepChanged,
                        StepName = "VSS",
                        Message = $"VSS tamamlandı: {dbName} ␦ {Path.GetFileName(result.VssFileCopyPath)} [{Fmt(result.VssFileCopySizeBytes)}]"
                    });
                }

                // 2. Verify
                if (plan.VerifyAfterBackup)
                {
                    result.VerifyResult = await SqlBackupService.VerifyBackupAsync(
                        plan.SqlConnection, result.BackupFilePath, ct);

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        DatabaseName = dbName,
                        ActivityType = BackupActivityType.StepChanged,
                        StepName = "Doğrulama",
                        Message = result.VerifyResult == true
                            ? $"Yedek doğrulama başarılı ✓: {Path.GetFileName(result.BackupFilePath)}"
                            : $"Yedek doğrulama başarısız ✕: {Path.GetFileName(result.BackupFilePath)}"
                    });

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

                        // Ara dosya takibi: .7z
                        cleanupPaths.Add(archivePath);

                        BackupActivityHub.Raise(new BackupActivityEventArgs
                        {
                            PlanId = plan.PlanId,
                            PlanName = plan.PlanName,
                            DatabaseName = dbName,
                            ActivityType = BackupActivityType.StepChanged,
                            StepName = "Sıkıştırma",
                            Message = $"Sıkıştırma tamamlandı: {Path.GetFileName(archivePath)} [{Fmt(result.CompressedSizeBytes)}, oran: {FmtRatio(result.FileSizeBytes, result.CompressedSizeBytes)}]"
                        });

                        // 3b. Arşiv bütünlük doğrulaması
                        if (plan.VerifyAfterBackup)
                        {
                            result.CompressionVerified = await CompressionService.VerifyArchiveAsync(
                                archivePath, password, ct);

                            BackupActivityHub.Raise(new BackupActivityEventArgs
                            {
                                PlanId = plan.PlanId,
                                PlanName = plan.PlanName,
                                DatabaseName = dbName,
                                ActivityType = BackupActivityType.StepChanged,
                                StepName = "Arşiv Doğrulama",
                                Message = result.CompressionVerified == true
                                    ? $"Arşiv bütünlük doğrulaması başarılı ✓: {Path.GetFileName(archivePath)}"
                                    : $"Arşiv bütünlük doğrulaması başarısız ✕: {Path.GetFileName(archivePath)}"
                            });

                            if (result.CompressionVerified == false)
                            {
                                Log.Error(
                                    "Arşiv bütünlük doğrulaması başarısız: {Database} — {Archive}",
                                    dbName, archivePath);
                            }
                        }

                        // 3c. Sıkıştırma başarılıysa ara .bak dosyasını sil
                        bool verifyOk = !plan.VerifyAfterBackup || result.CompressionVerified == true;
                        if (verifyOk && File.Exists(archivePath) && File.Exists(result.BackupFilePath))
                        {
                            try
                            {
                                long bakSize = new FileInfo(result.BackupFilePath).Length;
                                File.Delete(result.BackupFilePath);
                                cleanupPaths.Remove(result.BackupFilePath);
                                Log.Information(
                                    "Ara .bak dosyası silindi: {BakFile} [{Size}]",
                                    Path.GetFileName(result.BackupFilePath), Fmt(bakSize));

                                BackupActivityHub.Raise(new BackupActivityEventArgs
                                {
                                    PlanId = plan.PlanId,
                                    PlanName = plan.PlanName,
                                    DatabaseName = dbName,
                                    ActivityType = BackupActivityType.StepChanged,
                                    StepName = "Temizlik",
                                    Message = $"Ara .bak dosyası silindi: {Path.GetFileName(result.BackupFilePath)} [{Fmt(bakSize)}]"
                                });
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "Ara .bak dosyası silinemedi: {File}", result.BackupFilePath);
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Sıkıştırma hatası: {Database}", dbName);
                    }
                }

                // 4. Cloud Upload — ana dosya (.bak/.7z) ve VSS dosyası bağımsız yüklenir.
                //    İkisinden biri başarısız olsa bile diğeri denenir; en az biri buluta gitmelidir.
                if (CloudOrchestrator != null && plan.CloudTargets != null &&
                    plan.CloudTargets.Any(t => t.IsEnabled))
                {
                    bool mainUploadOk = false;
                    bool vssUploadOk = false;

                    // 4a. Ana dosya yükleme (.bak veya .7z)
                    try
                    {
                        string fileToUpload = !string.IsNullOrEmpty(result.CompressedFilePath)
                            ? result.CompressedFilePath
                            : result.BackupFilePath;

                        string remoteFileName = Path.GetFileName(fileToUpload);

                        result.CloudUploadResults = await CloudOrchestrator.UploadToAllAsync(
                            fileToUpload, remoteFileName, plan.CloudTargets, null, ct,
                            plan.PlanName, plan.PlanId);

                        int successCount = result.CloudUploadResults.Count(r => r.IsSuccess);
                        int totalCount = result.CloudUploadResults.Count;
                        mainUploadOk = successCount > 0;

                        Log.Information(
                            "Bulut upload tamamlandı: {Database} — {Success}/{Total} başarılı",
                            dbName, successCount, totalCount);

                        BackupActivityHub.Raise(new BackupActivityEventArgs
                        {
                            PlanId = plan.PlanId,
                            PlanName = plan.PlanName,
                            DatabaseName = dbName,
                            ActivityType = BackupActivityType.StepChanged,
                            StepName = "Bulut Yükleme",
                            Message = $"Bulut yükleme tamamlandı: {dbName} — {successCount}/{totalCount} başarılı [{Fmt(new FileInfo(fileToUpload).Length)}]"
                        });
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Ana dosya bulut upload hatası: {Database}", dbName);
                    }

                    // 4b. VSS dosyasını da buluta yükle (ana upload sonucundan bağımsız)
                    if (!string.IsNullOrEmpty(result.VssFileCopyPath) && File.Exists(result.VssFileCopyPath))
                    {
                        try
                        {
                            string vssRemoteName = Path.GetFileName(result.VssFileCopyPath);
                            Log.Information("VSS dosyası buluta yükleniyor: {VssFile}", vssRemoteName);

                            BackupActivityHub.Raise(new BackupActivityEventArgs
                            {
                                PlanId = plan.PlanId,
                                PlanName = plan.PlanName,
                                DatabaseName = dbName,
                                ActivityType = BackupActivityType.StepChanged,
                                StepName = "VSS Bulut Yükleme",
                                Message = $"VSS dosyası buluta yükleniyor: {vssRemoteName} [{Fmt(result.VssFileCopySizeBytes)}]"
                            });

                            var vssResults = await CloudOrchestrator.UploadToAllAsync(
                                result.VssFileCopyPath, vssRemoteName, plan.CloudTargets, null, ct,
                                plan.PlanName, plan.PlanId);

                            int vssSuccess = vssResults.Count(r => r.IsSuccess);
                            int vssTotal = vssResults.Count;
                            vssUploadOk = vssSuccess > 0;

                            BackupActivityHub.Raise(new BackupActivityEventArgs
                            {
                                PlanId = plan.PlanId,
                                PlanName = plan.PlanName,
                                DatabaseName = dbName,
                                ActivityType = BackupActivityType.StepChanged,
                                StepName = "VSS Bulut Yükleme",
                                Message = $"VSS bulut yükleme tamamlandı: {dbName} — {vssSuccess}/{vssTotal} başarılı [{Fmt(result.VssFileCopySizeBytes)}]"
                            });
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "VSS dosyası bulut upload hatası: {Database}", dbName);
                        }
                    }

                    // Her iki upload da başarısızsa uyarı ve bildirim
                    if (!mainUploadOk && !vssUploadOk)
                    {
                        Log.Error(
                            "Buluta hiçbir dosya yüklenemedi: {Database} — " +
                            "ne ana yedek ne de VSS kopyası başarılı olamadı.", dbName);
                        cloudAllOk = false;

                        // Başarısız sonuçları topla ve bildir
                        var allFailedResults = new List<CloudUploadResult>();
                        if (result.CloudUploadResults is not null)
                            allFailedResults.AddRange(result.CloudUploadResults.Where(r => !r.IsSuccess));

                        if (allFailedResults.Count > 0 && NotificationService is not null && plan.Notifications is not null)
                        {
                            try
                            {
                                string failedFile = !string.IsNullOrEmpty(result.CompressedFilePath)
                                    ? Path.GetFileName(result.CompressedFilePath)
                                    : Path.GetFileName(result.BackupFilePath);

                                await NotificationService.NotifyCloudUploadFailureAsync(
                                    plan.PlanName, allFailedResults, failedFile, plan.Notifications, ct);
                            }
                            catch (Exception notifyEx)
                            {
                                Log.Warning(notifyEx, "Bulut upload başarısızlık bildirimi gönderilemedi: {Database}", dbName);
                            }
                        }
                    }
                }

                // 5. Retention
                if (RetentionService != null)
                {
                    try
                    {
                        await RetentionService.CleanupAsync(plan, ct);

                        BackupActivityHub.Raise(new BackupActivityEventArgs
                        {
                            PlanId = plan.PlanId,
                            PlanName = plan.PlanName,
                            DatabaseName = dbName,
                            ActivityType = BackupActivityType.StepChanged,
                            StepName = "Temizlik",
                            Message = $"Eski yedek temizliği tamamlandı: {dbName}"
                        });
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Retention temizliği hatası: {Database}", dbName);
                    }
                }

                // 6. History
                SaveHistory(result);

                // 7. Notify
                await NotifyIfConfigured(result, plan, ct);

                // Bu DB başarıyla tamamlandı — ara dosyalarını temizlik listesinden çıkar
                if (cleanupPaths.Count > cleanupSnapshot)
                    cleanupPaths.RemoveRange(cleanupSnapshot, cleanupPaths.Count - cleanupSnapshot);
            }

            return cloudAllOk;
        }

        private async Task<bool> ExecuteFileBackupAsync(BackupPlan plan, string correlationId, CancellationToken ct,
            List<string> cleanupPaths)
        {
            if (FileBackupService == null)
            {
                Log.Error("Dosya yedekleme: FileBackupService null (Autofac inject başarısız). Plan={PlanName}", plan.PlanName);
                return true;
            }

            if (plan.FileBackup == null)
            {
                Log.Warning("Dosya yedekleme: Plan.FileBackup yapılandırması null. Plan={PlanName}", plan.PlanName);
                return true;
            }

            if (!plan.FileBackup.IsEnabled)
            {
                Log.Information("Dosya yedekleme: FileBackup devre dışı. Plan={PlanName}", plan.PlanName);
                return true;
            }

            int enabledSources = plan.FileBackup.Sources?.Count(s => s.IsEnabled) ?? 0;
            string strategyLabel = plan.FileBackup.Strategy switch
            {
                FileBackupStrategy.Differential => "Fark",
                FileBackupStrategy.Incremental => "Artırımlı",
                _ => "Tam"
            };
            Log.Information("Dosya yedekleme başlıyor: Plan={PlanName}, Strateji={Strategy}, Kaynaklar={SourceCount}, CorrelationId={CorrelationId}",
                plan.PlanName, strategyLabel, enabledSources, correlationId);

            BackupActivityHub.Raise(new BackupActivityEventArgs
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                ActivityType = BackupActivityType.StepChanged,
                StepName = "Dosya Yedekleme",
                Message = $"Dosya yedekleme başlıyor ({strategyLabel}): {enabledSources} kaynak"
            });

            var results = await FileBackupService.BackupFilesAsync(plan, null, ct);

            int totalCopied = 0;
            int sourceIndex = 0;
            int totalSources = results.Count;
            foreach (var fileResult in results)
            {
                totalCopied += fileResult.FilesCopied;
                sourceIndex++;
                Log.Information(
                    "Dosya yedekleme tamamlandı: {SourceName} — {FilesCopied} dosya, {Status}",
                    fileResult.SourceName, fileResult.FilesCopied, fileResult.Status);

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    ActivityType = BackupActivityType.StepChanged,
                    StepName = "Dosya Yedekleme",
                    CurrentIndex = sourceIndex,
                    TotalCount = totalSources,
                    Message = $"  {fileResult.SourceName}: {fileResult.FilesCopied} dosya kopyalandı — {fileResult.Status}"
                });
            }

            if (!results.Any(r => r.Status == BackupResultStatus.Success ||
                                   r.Status == BackupResultStatus.PartialSuccess ||
                                   r.FilesCopied > 0))
            {
                Log.Warning("Dosya yedekleme: Hiçbir dosya kopyalanamadı, sıkıştırma atlanıyor. Plan={PlanName}", plan.PlanName);
                return true;
            }

            string filesDir = Path.Combine(plan.LocalPath, "Files");

            // Ara klasör takibi
            if (Directory.Exists(filesDir))
                cleanupPaths.Add(filesDir);

            if (!Directory.Exists(filesDir))
            {
                Log.Warning("Dosya yedekleme: Hedef dizin bulunamadı, sıkıştırma atlanıyor: {FilesDir}", filesDir);
                return true;
            }

            if (!Directory.EnumerateFiles(filesDir, "*.*", SearchOption.AllDirectories).Any())
            {
                Log.Warning("Dosya yedekleme: Hedef dizinde dosya yok, sıkıştırma atlanıyor: {FilesDir}", filesDir);
                return true;
            }

            // Sıkıştır — yapılandırılmışsa o ayarları kullan, yoksa varsayılan (Level 3, şifresiz)
            string archivePath = null;
            try
            {
                if (CompressionService is Engine.Compression.SevenZipCompressionService sevenZip)
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    archivePath = Path.Combine(plan.LocalPath, $"Files_{timestamp}.7z");
                    string password = plan.Compression != null && !string.IsNullOrEmpty(plan.Compression.ArchivePassword)
                        ? PasswordProtector.Unprotect(plan.Compression.ArchivePassword)
                        : null;
                    CompressionLevel level = plan.Compression?.Level ?? CompressionLevel.Normal;

                    await sevenZip.CompressDirectoryAsync(filesDir, archivePath, password, level, null, ct);
                    Log.Information("Dosya yedek arşivi oluşturuldu: {ArchivePath}", archivePath);

                    // Ara dosya takibi: .7z
                    cleanupPaths.Add(archivePath);

                    long archiveSize = 0;
                    try { archiveSize = new FileInfo(archivePath).Length; } catch { }

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.StepChanged,
                        StepName = "Dosya Sıkıştırma",
                        Message = $"Dosya yedek arşivi oluşturuldu: {Path.GetFileName(archivePath)} [{Fmt(archiveSize)}]"
                    });
                }
                else
                {
                    Log.Warning("SevenZipCompressionService bulunamadı, dosya yedekleri sıkıştırılamadı.");
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Dosya yedek sıkıştırma hatası: Plan={PlanName}", plan.PlanName);
            }

            // Arşiv oluşturuldu mu?
            if (archivePath == null || !File.Exists(archivePath))
            {
                Log.Warning("Dosya yedekleme: Arşiv oluşturulamadı veya bulunamadı, bulut yüklemesi atlanıyor. Plan={PlanName}", plan.PlanName);
                return true;
            }

            // Arşiv başarılıysa ara Files klasörünü sil
            if (Directory.Exists(filesDir))
            {
                try
                {
                    int fileCount = Directory.GetFiles(filesDir, "*.*", SearchOption.AllDirectories).Length;
                    Directory.Delete(filesDir, recursive: true);
                    cleanupPaths.Remove(filesDir);
                    Log.Information(
                        "Ara Files klasörü silindi: {FilesDir} ({FileCount} dosya temizlendi)",
                        filesDir, fileCount);

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.StepChanged,
                        StepName = "Temizlik",
                        Message = $"Ara Files klasörü silindi: {fileCount} dosya temizlendi"
                    });
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Ara Files klasörü silinemedi: {FilesDir}", filesDir);
                }
            }

            // Bulut hedefleri hazır mı?
            if (CloudOrchestrator == null)
            {
                Log.Warning("Dosya yedekleme: CloudOrchestrator null, bulut yüklemesi atlanıyor. Plan={PlanName}", plan.PlanName);
                await NotifyFileBackupIfConfiguredAsync(results, plan, null, archivePath, ct);
                return true;
            }

            if (plan.CloudTargets == null || !plan.CloudTargets.Any(t => t.IsEnabled))
            {
                Log.Information("Dosya yedekleme: Aktif bulut hedefi yok, yükleme atlanıyor. Plan={PlanName}", plan.PlanName);
                await NotifyFileBackupIfConfiguredAsync(results, plan, null, archivePath, ct);
                return true;
            }

            // Buluta gönder
            bool fileCloudOk = false;
            int enabledTargetCount = plan.CloudTargets.Count(t => t.IsEnabled);
            string archiveFileName = Path.GetFileName(archivePath);
            Log.Information("Dosya yedek bulut yüklemesi başlıyor: {Archive} — {TargetCount} hedef",
                archivePath, enabledTargetCount);
            try
            {
                var uploadResults = await CloudOrchestrator.UploadToAllAsync(
                    archivePath, archiveFileName,
                    plan.CloudTargets, null, ct, plan.PlanName, plan.PlanId);

                int successCount = uploadResults.Count(r => r.IsSuccess);
                int totalCount = uploadResults.Count;
                fileCloudOk = successCount > 0;

                Log.Information("Dosya yedek bulut yüklemesi tamamlandı. Plan={PlanName}", plan.PlanName);

                long uploadSize = 0;
                try { uploadSize = new FileInfo(archivePath).Length; } catch { }

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    ActivityType = BackupActivityType.StepChanged,
                    StepName = "Dosya Bulut Yükleme",
                    Message = $"Dosya yedek bulut yüklemesi tamamlandı: {archiveFileName} — {successCount}/{totalCount} başarılı [{Fmt(uploadSize)}]"
                });

                // Başarısız hedefler varsa bildirim gönder
                var failedResults = uploadResults.Where(r => !r.IsSuccess).ToList();
                if (failedResults.Count > 0 && NotificationService is not null && plan.Notifications is not null)
                {
                    try
                    {
                        await NotificationService.NotifyCloudUploadFailureAsync(
                            plan.PlanName, failedResults, archiveFileName, plan.Notifications, ct);
                    }
                    catch (Exception notifyEx)
                    {
                        Log.Warning(notifyEx, "Bulut upload başarısızlık bildirimi gönderilemedi: {PlanName}", plan.PlanName);
                    }
                }

                // Dosya yedekleme genel bildirimi (bulut sonuçları dahil)
                await NotifyFileBackupIfConfiguredAsync(results, plan, uploadResults, archivePath, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Dosya yedek cloud upload hatası: Plan={PlanName}", plan.PlanName);
            }

            return fileCloudOk;
        }

        /// <summary>
        /// Bulut hedeflerinde çöp kutusu temizliği yapar.
        /// PermanentDeleteFromTrash=false olan ve çöp kutusu destekleyen hedeflerde birikmiş çöp öğelerini kalıcı olarak siler.
        /// </summary>
        private async Task EmptyTrashIfNeededAsync(BackupPlan plan, CancellationToken ct)
        {
            if (CloudOrchestrator == null || plan.CloudTargets == null || !plan.CloudTargets.Any(t => t.IsEnabled))
                return;

            try
            {
                int trashDeleted = await CloudOrchestrator.EmptyTrashForAllAsync(plan.CloudTargets, ct);
                if (trashDeleted > 0)
                {
                    Log.Information("Bulut çöp kutusu temizlendi: {DeletedCount} öğe silindi. Plan={PlanName}",
                        trashDeleted, plan.PlanName);

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.StepChanged,
                        StepName = "Çöp Kutusu",
                        Message = $"Bulut çöp kutusu temizlendi: {trashDeleted} öğe silindi"
                    });
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulut çöp kutusu temizleme hatası: Plan={PlanName}", plan.PlanName);
            }
        }

        /// <summary>
        /// İptal veya hata durumunda ara dosyaları temizler.
        /// Yalnızca tamamlanmamış (pipeline'ı bitmemiş) dosyaları siler.
        /// </summary>
        private static void CleanupOnFailure(List<string> paths, string planId)
        {
            if (paths == null || paths.Count == 0) return;

            foreach (var path in paths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        long size = 0;
                        try { size = new FileInfo(path).Length; } catch { }
                        File.Delete(path);
                        Log.Information(
                            "İptal/hata temizliği: Dosya silindi — {File} [{Size}], Plan={PlanId}",
                            Path.GetFileName(path), Fmt(size), planId);
                    }
                    else if (Directory.Exists(path))
                    {
                        int fileCount = 0;
                        try { fileCount = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Length; } catch { }
                        Directory.Delete(path, recursive: true);
                        Log.Information(
                            "İptal/hata temizliği: Klasör silindi — {Dir} ({FileCount} dosya), Plan={PlanId}",
                            path, fileCount, planId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "İptal/hata temizliği başarısız: {Path}", path);
                }
            }
        }

        /// <summary>Byte değerini okunabilir boyut metnine dönüştürür (örn. "723.7 MB").</summary>
        private static string Fmt(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }

        /// <summary>Sıkıştırma oranını hesaplar (örn. "%93.4").</summary>
        private static string FmtRatio(long original, long compressed)
        {
            if (original <= 0 || compressed <= 0) return "";
            double ratio = (1.0 - (double)compressed / original) * 100;
            return $"%{ratio:F1}";
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

        private async Task NotifyFileBackupIfConfiguredAsync(
            List<FileBackupResult> results, BackupPlan plan,
            List<CloudUploadResult> cloudUploadResults, string archivePath, CancellationToken ct)
        {
            if (NotificationService == null || plan.Notifications == null)
                return;

            try
            {
                string archiveFileName = !string.IsNullOrEmpty(archivePath) ? Path.GetFileName(archivePath) : null;
                long archiveSizeBytes = 0;
                if (!string.IsNullOrEmpty(archivePath) && File.Exists(archivePath))
                {
                    try { archiveSizeBytes = new FileInfo(archivePath).Length; } catch { }
                }

                await NotificationService.NotifyFileBackupAsync(
                    results, plan, cloudUploadResults, archiveFileName, archiveSizeBytes, ct);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dosya yedek bildirimi gönderilemedi: {PlanName}", plan.PlanName);
            }
        }
    }
}
