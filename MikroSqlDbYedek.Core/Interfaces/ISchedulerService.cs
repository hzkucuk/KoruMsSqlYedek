using System.Threading;
using System.Threading.Tasks;
using MikroSqlDbYedek.Core.Models;

namespace MikroSqlDbYedek.Core.Interfaces
{
    /// <summary>
    /// Quartz.NET tabanlı zamanlama servisi.
    /// </summary>
    public interface ISchedulerService
    {
        /// <summary>Scheduler'ı başlatır.</summary>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>Scheduler'ı durdurur (graceful shutdown).</summary>
        Task StopAsync(CancellationToken cancellationToken);

        /// <summary>Plan için zamanlanmış job oluşturur/günceller.</summary>
        Task SchedulePlanAsync(BackupPlan plan, CancellationToken cancellationToken);

        /// <summary>Plan'ın zamanlanmış job'ını kaldırır.</summary>
        Task UnschedulePlanAsync(string planId, CancellationToken cancellationToken);

        /// <summary>Planı hemen çalıştırır (manuel tetikleme).</summary>
        Task TriggerPlanNowAsync(string planId, CancellationToken cancellationToken);

        /// <summary>Scheduler çalışıyor mu?</summary>
        bool IsRunning { get; }
    }
}
