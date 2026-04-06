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
        private async Task<(bool CloudOk, List<BackupResult> Results)> ExecuteSqlBackupAsync(
            BackupPlan plan, string backupType, string correlationId, CancellationToken ct,
            List<string> cleanupPaths)
        {
            bool cloudAllOk = true;
            var sqlResults = new List<BackupResult>();
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
                    return (true, sqlResults);
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

                // 7. Sonuçları topla (konsolide bildirim için)
                sqlResults.Add(result);

                // Bu DB başarıyla tamamlandı — ara dosyalarını temizlik listesinden çıkar
                if (cleanupPaths.Count > cleanupSnapshot)
                    cleanupPaths.RemoveRange(cleanupSnapshot, cleanupPaths.Count - cleanupSnapshot);
            }

            return (cloudAllOk, sqlResults);
        }
    }
}
