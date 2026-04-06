using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Scheduling
{
    partial class BackupJobExecutor
    {
        private async Task<(bool CloudOk, List<FileBackupResult> FileResults, List<CloudUploadResult> FileCloudResults, string ArchivePath)> ExecuteFileBackupAsync(BackupPlan plan, string correlationId, CancellationToken ct,
            List<string> cleanupPaths)
        {
            if (FileBackupService == null)
            {
                Log.Error("Dosya yedekleme: FileBackupService null (Autofac inject başarısız). Plan={PlanName}", plan.PlanName);
                return (true, new List<FileBackupResult>(), null, null);
            }

            if (plan.FileBackup == null)
            {
                Log.Warning("Dosya yedekleme: Plan.FileBackup yapılandırması null. Plan={PlanName}", plan.PlanName);
                return (true, new List<FileBackupResult>(), null, null);
            }

            if (!plan.FileBackup.IsEnabled)
            {
                Log.Information("Dosya yedekleme: FileBackup devre dışı. Plan={PlanName}", plan.PlanName);
                return (true, new List<FileBackupResult>(), null, null);
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
                return (true, results, null, null);
            }

            string filesDir = Path.Combine(plan.LocalPath, "Files");

            // Ara klasör takibi
            if (Directory.Exists(filesDir))
                cleanupPaths.Add(filesDir);

            if (!Directory.Exists(filesDir))
            {
                Log.Warning("Dosya yedekleme: Hedef dizin bulunamadı, sıkıştırma atlanıyor: {FilesDir}", filesDir);
                return (true, results, null, null);
            }

            if (!Directory.EnumerateFiles(filesDir, "*.*", SearchOption.AllDirectories).Any())
            {
                Log.Warning("Dosya yedekleme: Hedef dizinde dosya yok, sıkıştırma atlanıyor: {FilesDir}", filesDir);
                return (true, results, null, null);
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
                return (true, results, null, null);
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
                return (true, results, null, archivePath);
            }

            if (plan.CloudTargets == null || !plan.CloudTargets.Any(t => t.IsEnabled))
            {
                Log.Information("Dosya yedekleme: Aktif bulut hedefi yok, yükleme atlanıyor. Plan={PlanName}", plan.PlanName);
                return (true, results, null, archivePath);
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

                // Sonuçları döndür — bildirim Execute() içinde konsolide gönderilecek
                return (fileCloudOk, results, uploadResults, archivePath);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Dosya yedek cloud upload hatası: Plan={PlanName}", plan.PlanName);
            }

            return (fileCloudOk, results, null, archivePath);
        }
    }
}
