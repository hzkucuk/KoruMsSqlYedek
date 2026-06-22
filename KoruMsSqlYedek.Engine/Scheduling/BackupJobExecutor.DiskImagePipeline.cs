using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Scheduling
{
    partial class BackupJobExecutor
    {
        /// <summary>
        /// Disk imajı yedekleme pipeline'ı: wbadmin ile WIM imajı oluşturur.
        /// </summary>
        /// <param name="plan">Yedekleme planı.</param>
        /// <param name="correlationId">İzleme kimliği.</param>
        /// <param name="ct">İptal jetonu.</param>
        /// <returns>Oluşturulan disk imajı sonuçlarının listesi.</returns>
        private async Task<List<DiskImageResult>> ExecuteDiskImagePipelineAsync(
            BackupPlan plan,
            string correlationId,
            CancellationToken ct)
        {
            if (DiskImageService == null)
            {
                Log.Error("Disk imajı yedekleme: DiskImageService null (Autofac inject başarısız). Plan={PlanName}", plan.PlanName);
                return new List<DiskImageResult>();
            }

            var config = plan.DiskImageBackup;
            if (config is null || !config.IsEnabled)
            {
                Log.Information("Disk imajı yedekleme devre dışı. Plan={PlanName}", plan.PlanName);
                return new List<DiskImageResult>();
            }

            int enabledSources = config.Sources?.Count(s => s.IsEnabled) ?? 0;
            if (enabledSources == 0)
            {
                Log.Warning("Disk imajı yedekleme: Aktif kaynak bulunamadı. Plan={PlanName}", plan.PlanName);
                return new List<DiskImageResult>();
            }

            Log.Information("Disk imajı yedekleme başlıyor: Plan={PlanName}, Kaynak={Count}, CorrelationId={Id}",
                plan.PlanName, enabledSources, correlationId);

            BackupActivityHub.Raise(new BackupActivityEventArgs
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                ActivityType = BackupActivityType.DiskImageStarted,
                StepName = "Disk İmajı",
                HasDiskImageBackup = true,
                Message = $"Disk imajı yedekleme başlıyor: {enabledSources} sürücü"
            });

            var results = new List<DiskImageResult>();

            try
            {
                var progress = new Progress<int>(pct =>
                {
                    BackupActivityHub.Raise(new BackupActivityEventArgs
                    {
                        PlanId = plan.PlanId,
                        PlanName = plan.PlanName,
                        ActivityType = BackupActivityType.DiskImageProgress,
                        StepName = "Disk İmajı",
                        HasDiskImageBackup = true,
                        ProgressPercent = pct,
                        Message = $"Disk imajı oluşturuluyor... %{pct}"
                    });
                });

                results = await DiskImageService.BackupDiskImagesAsync(plan, progress, ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                BackupActivityHub.Raise(new BackupActivityEventArgs
                {
                    PlanId = plan.PlanId,
                    PlanName = plan.PlanName,
                    ActivityType = BackupActivityType.DiskImageCompleted,
                    StepName = "Disk İmajı",
                    HasDiskImageBackup = true,
                    IsSuccess = false,
                    Message = "Disk imajı yedekleme iptal edildi."
                });
                throw;
            }

            bool anySuccess = results.Any(r =>
                r.Status == BackupResultStatus.Success ||
                r.Status == BackupResultStatus.PartialSuccess);

            foreach (var r in results)
            {
                Log.Information("Disk imajı sonucu: Kaynak={Volume}, Durum={Status}, Yol={Path}, Boyut={SizeMB} MB",
                    r.VolumePath, r.Status, r.OutputPath, r.ImageSizeBytes / 1024 / 1024);
            }

            BackupActivityHub.Raise(new BackupActivityEventArgs
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                ActivityType = BackupActivityType.DiskImageCompleted,
                StepName = "Disk İmajı",
                HasDiskImageBackup = true,
                IsSuccess = anySuccess,
                ProgressPercent = 100,
                Message = anySuccess
                    ? $"Disk imajı yedekleme tamamlandı: {results.Count(r => r.Status == BackupResultStatus.Success)} sürücü başarılı"
                    : "Disk imajı yedekleme başarısız"
            });

            return results;
        }
    }
}
