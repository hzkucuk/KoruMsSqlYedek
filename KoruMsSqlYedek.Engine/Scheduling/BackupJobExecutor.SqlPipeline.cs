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
        /// <summary>
        /// SQL yedekleme pipeline'ı: Backup → Verify → Compress → dosyaları topla.
        /// Bulut yükleme yapılmaz, dosya bilgileri pending listesine eklenir.
        /// </summary>
        private async Task<(List<BackupResult> Results, List<(string FilePath, string RemoteName, BackupResult SqlResult, bool IsVss)> PendingUploads)> ExecuteSqlBackupAsync(
            BackupPlan plan, string backupType, string correlationId, CancellationToken ct,
            List<string> cleanupPaths)
        {
            var sqlResults = new List<BackupResult>();
            var pendingUploads = new List<(string FilePath, string RemoteName, BackupResult SqlResult, bool IsVss)>();
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
                    return (sqlResults, pendingUploads);
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
                    sqlResults.Add(result);
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

                        // Sıkıştırma ilerleme raporlama — büyük dosyalarda (4-10 GB) UI'ın takılmış
                        // görünmemesi için BackupActivityHub üzerinden yüzde bilgisi yayınlanır
                        int lastCompressPct = -1;
                        var compressProgress = new Progress<int>(pct =>
                        {
                            if (pct <= lastCompressPct) return;
                            lastCompressPct = pct;
                            BackupActivityHub.Raise(new BackupActivityEventArgs
                            {
                                PlanId = plan.PlanId,
                                PlanName = plan.PlanName,
                                DatabaseName = dbName,
                                ActivityType = BackupActivityType.StepChanged,
                                StepName = "Sıkıştırma",
                                ProgressPercent = pct,
                                Message = $"Sıkıştırılıyor: {dbName} — %{pct}"
                            });
                        });

                        result.CompressedSizeBytes = await CompressionService.CompressAsync(
                            result.BackupFilePath, archivePath, password, compressProgress, ct);
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

                // 4. Bulut için yüklenecek dosyaları topla (upload sonraya ertelenir)
                if (CloudOrchestrator != null && plan.CloudTargets != null &&
                    plan.CloudTargets.Any(t => t.IsEnabled))
                {
                    // Ana dosya (.bak veya .7z)
                    string fileToUpload = !string.IsNullOrEmpty(result.CompressedFilePath)
                        ? result.CompressedFilePath
                        : result.BackupFilePath;

                    if (!string.IsNullOrEmpty(fileToUpload) && File.Exists(fileToUpload))
                    {
                        pendingUploads.Add((fileToUpload, Path.GetFileName(fileToUpload), result, false));
                    }

                    // VSS dosyası
                    if (!string.IsNullOrEmpty(result.VssFileCopyPath) && File.Exists(result.VssFileCopyPath))
                    {
                        pendingUploads.Add((result.VssFileCopyPath, Path.GetFileName(result.VssFileCopyPath), result, true));
                    }
                }

                // 5. History (cloud sonuçları sonra atanacak)
                sqlResults.Add(result);

                // Bu DB'nin lokal adımları tamamlandı — cleanup paths'ten çıkar
                if (cleanupPaths.Count > cleanupSnapshot)
                    cleanupPaths.RemoveRange(cleanupSnapshot, cleanupPaths.Count - cleanupSnapshot);
            }

            // 6. Retention — tüm DB'ler tamamlandıktan sonra bir kez çalıştır.
            // Döngü içinde çalıştırılırsa prefix-paylaşan DB dosyaları erken silinebilir.
            if (RetentionService != null)
            {
                try
                {
                    await RetentionService.CleanupAsync(plan, ct);

                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.StepChanged,
                        StepName = "Temizlik",
                        Message = "Eski yedek temizliği tamamlandı"
                    });
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Log.Error(ex, "Retention temizliği hatası: Plan={PlanName}", plan.PlanName);
                }
            }

            return (sqlResults, pendingUploads);
        }
    }
}
