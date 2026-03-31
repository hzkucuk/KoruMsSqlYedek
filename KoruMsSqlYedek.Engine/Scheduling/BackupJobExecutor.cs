using System;
using System.Collections.Concurrent;
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

                        // Dosya yedekleme — ayrı zamanlama yoksa veya manuel tetikleme ise SQL yedek ile birlikte çalıştır
                        bool isManualTrigger = context.MergedJobDataMap.GetString("manualTrigger") == "true";
                        if (plan.FileBackup != null && plan.FileBackup.IsEnabled &&
                            (string.IsNullOrEmpty(plan.FileBackup.Schedule) || isManualTrigger))
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
            finally
            {
                if (lockAcquired && _planLocks.TryGetValue(planId, out var releaseLock))
                    releaseLock.Release();
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

                // 1b. Express VSS bilgisi
                if (result.VssFileCopySizeBytes > 0 && !string.IsNullOrEmpty(result.VssFileCopyPath))
                {
                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        DatabaseName = dbName,
                        ActivityType = BackupActivityType.StepChanged,
                        StepName = "Express VSS",
                        Message = $"Express VSS tamamlandı: {dbName} ␦ {Path.GetFileName(result.VssFileCopyPath)} [{Fmt(result.VssFileCopySizeBytes)}]"
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
                            plan.PlanName, plan.PlanId);

                        int successCount = result.CloudUploadResults.Count(r => r.IsSuccess);
                        int totalCount = result.CloudUploadResults.Count;
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

                        // 4b. VSS dosyasını da buluta yükle
                        if (!string.IsNullOrEmpty(result.VssFileCopyPath) && File.Exists(result.VssFileCopyPath))
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
            if (FileBackupService == null)
            {
                Log.Error("Dosya yedekleme: FileBackupService null (Autofac inject başarısız). Plan={PlanName}", plan.PlanName);
                return;
            }

            if (plan.FileBackup == null)
            {
                Log.Warning("Dosya yedekleme: Plan.FileBackup yapılandırması null. Plan={PlanName}", plan.PlanName);
                return;
            }

            if (!plan.FileBackup.IsEnabled)
            {
                Log.Information("Dosya yedekleme: FileBackup devre dışı. Plan={PlanName}", plan.PlanName);
                return;
            }

            int enabledSources = plan.FileBackup.Sources?.Count(s => s.IsEnabled) ?? 0;
            Log.Information("Dosya yedekleme başlıyor: Plan={PlanName}, Kaynaklar={SourceCount}, CorrelationId={CorrelationId}",
                plan.PlanName, enabledSources, correlationId);

            BackupActivityHub.Raise(new BackupActivityEventArgs
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                ActivityType = BackupActivityType.StepChanged,
                StepName = "Dosya Yedekleme",
                Message = $"Dosya yedekleme başlıyor: {enabledSources} kaynak"
            });

            var results = await FileBackupService.BackupFilesAsync(plan, null, ct);

            int totalCopied = 0;
            foreach (var fileResult in results)
            {
                totalCopied += fileResult.FilesCopied;
                Log.Information(
                    "Dosya yedekleme tamamlandı: {SourceName} — {FilesCopied} dosya, {Status}",
                    fileResult.SourceName, fileResult.FilesCopied, fileResult.Status);

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    ActivityType = BackupActivityType.StepChanged,
                    StepName = "Dosya Yedekleme",
                    Message = $"  {fileResult.SourceName}: {fileResult.FilesCopied} dosya kopyalandı — {fileResult.Status}"
                });
            }

            if (!results.Any(r => r.Status == BackupResultStatus.Success ||
                                   r.Status == BackupResultStatus.PartialSuccess ||
                                   r.FilesCopied > 0))
            {
                Log.Warning("Dosya yedekleme: Hiçbir dosya kopyalanamadı, sıkıştırma atlanıyor. Plan={PlanName}", plan.PlanName);
                return;
            }

            string filesDir = Path.Combine(plan.LocalPath, "Files");
            if (!Directory.Exists(filesDir))
            {
                Log.Warning("Dosya yedekleme: Hedef dizin bulunamadı, sıkıştırma atlanıyor: {FilesDir}", filesDir);
                return;
            }

            if (!Directory.EnumerateFiles(filesDir, "*.*", SearchOption.AllDirectories).Any())
            {
                Log.Warning("Dosya yedekleme: Hedef dizinde dosya yok, sıkıştırma atlanıyor: {FilesDir}", filesDir);
                return;
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
            catch (Exception ex)
            {
                Log.Error(ex, "Dosya yedek sıkıştırma hatası: Plan={PlanName}", plan.PlanName);
            }

            // Arşiv oluşturuldu mu?
            if (archivePath == null || !File.Exists(archivePath))
            {
                Log.Warning("Dosya yedekleme: Arşiv oluşturulamadı veya bulunamadı, bulut yüklemesi atlanıyor. Plan={PlanName}", plan.PlanName);
                return;
            }

            // Arşiv başarılıysa ara Files klasörünü sil
            if (Directory.Exists(filesDir))
            {
                try
                {
                    int fileCount = Directory.GetFiles(filesDir, "*.*", SearchOption.AllDirectories).Length;
                    Directory.Delete(filesDir, recursive: true);
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
                return;
            }

            if (plan.CloudTargets == null || !plan.CloudTargets.Any(t => t.IsEnabled))
            {
                Log.Information("Dosya yedekleme: Aktif bulut hedefi yok, yükleme atlanıyor. Plan={PlanName}", plan.PlanName);
                return;
            }

            // Buluta gönder
            int enabledTargetCount = plan.CloudTargets.Count(t => t.IsEnabled);
            Log.Information("Dosya yedek bulut yüklemesi başlıyor: {Archive} — {TargetCount} hedef",
                archivePath, enabledTargetCount);
            try
            {
                await CloudOrchestrator.UploadToAllAsync(
                    archivePath, Path.GetFileName(archivePath),
                    plan.CloudTargets, null, ct, plan.PlanName, plan.PlanId);
                Log.Information("Dosya yedek bulut yüklemesi tamamlandı. Plan={PlanName}", plan.PlanName);

                long uploadSize = 0;
                try { uploadSize = new FileInfo(archivePath).Length; } catch { }

                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    ActivityType = BackupActivityType.StepChanged,
                    StepName = "Dosya Bulut Yükleme",
                    Message = $"Dosya yedek bulut yüklemesi tamamlandı [{Fmt(uploadSize)}]"
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Dosya yedek cloud upload hatası: Plan={PlanName}", plan.PlanName);
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
    }
}
