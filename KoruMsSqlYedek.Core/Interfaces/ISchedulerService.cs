using System;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Core.Interfaces
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
        /// <param name="planId">Plan kimliği.</param>
        /// <param name="cancellationToken">İptal belirteci.</param>
        /// <param name="backupType">Yedek türü (Full, Differential, Incremental). null ise en yüksek öncelikli tetiklenir.</param>
        Task TriggerPlanNowAsync(string planId, CancellationToken cancellationToken, string backupType = null);

        /// <summary>Planın bir sonraki tetiklenme zamanını döndürür. Plan zamanlanmamışsa null döner.</summary>
        Task<DateTimeOffset?> GetNextFireTimeAsync(string planId, CancellationToken cancellationToken);

        /// <summary>Scheduler çalışıyor mu?</summary>
        bool IsRunning { get; }
    }
}
