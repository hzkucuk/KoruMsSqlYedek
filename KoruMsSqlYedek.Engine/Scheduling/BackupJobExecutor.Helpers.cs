using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Scheduling
{
    partial class BackupJobExecutor
    {
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

        private async Task SendConsolidatedNotificationAsync(
            BackupPlan plan, JobNotificationData data, CancellationToken ct)
        {
            if (NotificationService == null || plan.Notifications == null)
                return;

            try
            {
                await NotificationService.NotifyJobCompletedAsync(data, plan.Notifications, ct);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Konsolide bildirim gönderilemedi: {PlanName}", plan.PlanName);
            }
        }

        /// <summary>Dosya boyutunu güvenli şekilde alır.</summary>
        private static long GetFileSize(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
            try { return new FileInfo(path).Length; } catch { return 0; }
        }

        /// <summary>
        /// BackupActivityEventArgs → log satır metni (e-posta log bölümü için).
        /// </summary>
        private static string FormatActivityLogLine(BackupActivityEventArgs e) => e.ActivityType switch
        {
            BackupActivityType.Started
                => $"[{e.PlanName}] Yedekleme başladı.",
            BackupActivityType.DatabaseProgress
                => $"{e.DatabaseName} ({e.CurrentIndex}/{e.TotalCount}) işleniyor.",
            BackupActivityType.StepChanged
                => !string.IsNullOrEmpty(e.Message) ? e.Message : $"Adım: {e.StepName}",
            BackupActivityType.CloudUploadStarted
                => $"Bulut yükleme başladı: {e.CloudTargetName}",
            BackupActivityType.CloudUploadCompleted
                => e.IsSuccess
                    ? $"Bulut {e.CloudTargetName}: Başarılı ✓"
                    : $"Bulut {e.CloudTargetName}: Başarısız ✕ — {e.Message ?? "Bilinmeyen hata"}",
            BackupActivityType.CloudUploadAbandoned
                => e.AbandonedFiles is { Count: > 0 }
                    ? $"⚠ Bulut yükleme terk edildi ({e.AbandonedFiles.Count} dosya)"
                    : $"⚠ Bulut yükleme terk edildi: {e.Message ?? "Maksimum deneme aşıldı"}",
            BackupActivityType.Completed
                => e.IsSuccess || string.IsNullOrEmpty(e.Message)
                    ? $"[{e.PlanName}] Yedekleme tamamlandı. ✓"
                    : $"[{e.PlanName}] Yedekleme tamamlandı (bulut yükleme başarısız). ⚠",
            BackupActivityType.Failed
                => $"[{e.PlanName}] Yedekleme başarısız: {e.Message}",
            BackupActivityType.Cancelled
                => $"[{e.PlanName}] Yedekleme iptal edildi.",
            _ => null
        };
    }
}
