using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using MikroSqlDbYedek.Core.Interfaces;

namespace MikroSqlDbYedek.Service
{
    /// <summary>
    /// Microsoft.Extensions.Hosting ile host edilen Windows Service.
    /// Quartz.NET scheduler'ı başlatır ve durdurur.
    /// Debug modda konsol, production'da Windows Service olarak çalışır.
    /// </summary>
    public class BackupWindowsService : IHostedService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<BackupWindowsService>();
        private readonly ISchedulerService _schedulerService;
        private readonly IPlanManager _planManager;
        private CancellationTokenSource _cts;

        public BackupWindowsService(
            ISchedulerService schedulerService,
            IPlanManager planManager)
        {
            _schedulerService = schedulerService;
            _planManager = planManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information("MikroSqlDbYedek Service başlatılıyor...");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await _schedulerService.StartAsync(_cts.Token);

            var plans = _planManager.GetAllPlans();
            foreach (var plan in plans)
            {
                if (plan.IsEnabled)
                    await _schedulerService.SchedulePlanAsync(plan, _cts.Token);
            }

            Log.Information(
                "Service başlatıldı: {PlanCount} plan zamanlandı.",
                plans.FindAll(p => p.IsEnabled).Count);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("MikroSqlDbYedek Service durduruluyor...");

            _cts?.Cancel();
            await _schedulerService.StopAsync(cancellationToken);

            Log.Information("Service durduruldu (graceful).");
        }
    }
}
