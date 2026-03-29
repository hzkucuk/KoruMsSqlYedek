using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;

namespace KoruMsSqlYedek.Service
{
    /// <summary>
    /// Microsoft.Extensions.Hosting ile host edilen Windows Service.
    /// Quartz.NET scheduler'ı başlatır ve durdurur.
    /// Debug modda konsol, production'da Windows Service olarak çalışır.
    /// Plan dosyaları FileSystemWatcher ile izlenir; değişiklik olduğunda
    /// ilgili plan otomatik olarak yeniden zamanlanır.
    /// </summary>
    public class BackupWindowsService : IHostedService, IDisposable
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<BackupWindowsService>();
        private readonly ISchedulerService _schedulerService;
        private readonly IPlanManager _planManager;
        private CancellationTokenSource _cts;
        private FileSystemWatcher _planWatcher;

        // Debounce: aynı dosya için birden fazla Changed olayını engeller
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _lastProcessed
            = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();
        private static readonly TimeSpan DebounceInterval = TimeSpan.FromSeconds(2);

        public BackupWindowsService(
            ISchedulerService schedulerService,
            IPlanManager planManager)
        {
            _schedulerService = schedulerService;
            _planManager = planManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information("KoruMsSqlYedek Service başlatılıyor...");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await _schedulerService.StartAsync(_cts.Token);

            var plans = _planManager.GetAllPlans();
            foreach (var plan in plans)
            {
                if (plan.IsEnabled)
                    await _schedulerService.SchedulePlanAsync(plan, _cts.Token);
            }

            StartPlanWatcher();

            Log.Information(
                "Service başlatıldı: {PlanCount} plan zamanlandı.",
                plans.FindAll(p => p.IsEnabled).Count);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("KoruMsSqlYedek Service durduruluyor...");

            _planWatcher?.Dispose();
            _planWatcher = null;

            _cts?.Cancel();
            await _schedulerService.StopAsync(cancellationToken);

            Log.Information("Service durduruldu (graceful).");
        }

        public void Dispose()
        {
            _planWatcher?.Dispose();
            _cts?.Dispose();
        }

        // ── FileSystemWatcher ─────────────────────────────────────────────

        private void StartPlanWatcher()
        {
            string plansDir = PathHelper.PlansDirectory;
            Directory.CreateDirectory(plansDir);

            _planWatcher = new FileSystemWatcher(plansDir, "*.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _planWatcher.Changed += OnPlanFileChanged;
            _planWatcher.Created += OnPlanFileChanged;
            _planWatcher.Deleted += OnPlanFileDeleted;
            _planWatcher.Renamed += OnPlanFileRenamed;

            Log.Information("Plan dizini izleniyor: {PlansDirectory}", plansDir);
        }

        private void OnPlanFileChanged(object sender, FileSystemEventArgs e)
        {
            string planId = Path.GetFileNameWithoutExtension(e.FullPath);

            // Debounce — kısa sürede gelen tekrar olayları yoksay
            DateTime now = DateTime.UtcNow;
            if (_lastProcessed.TryGetValue(planId, out DateTime last) &&
                (now - last) < DebounceInterval)
                return;

            _lastProcessed[planId] = now;

            // Dosya kilidi için kısa gecikme
            Task.Delay(500, _cts.Token).ContinueWith(async t =>
            {
                if (t.IsCanceled) return;
                try
                {
                    var plan = _planManager.GetPlanById(planId);
                    if (plan != null)
                    {
                        await _schedulerService.SchedulePlanAsync(plan, _cts.Token);
                        Log.Information(
                            "Plan değişikliği algılandı, yeniden zamanlandı: {PlanId} - {PlanName}",
                            planId, plan.PlanName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plan yeniden zamanlanırken hata: {PlanId}", planId);
                }
            }, TaskScheduler.Default);
        }

        private void OnPlanFileDeleted(object sender, FileSystemEventArgs e)
        {
            string planId = Path.GetFileNameWithoutExtension(e.FullPath);
            _lastProcessed.TryRemove(planId, out _);

            Task.Run(async () =>
            {
                try
                {
                    await _schedulerService.UnschedulePlanAsync(planId, _cts.Token);
                    Log.Information("Silinen plan zamanlaması kaldırıldı: {PlanId}", planId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plan zamanlaması kaldırılırken hata: {PlanId}", planId);
                }
            });
        }

        private void OnPlanFileRenamed(object sender, RenamedEventArgs e)
        {
            string oldPlanId = Path.GetFileNameWithoutExtension(e.OldFullPath);
            string newPlanId = Path.GetFileNameWithoutExtension(e.FullPath);
            _lastProcessed.TryRemove(oldPlanId, out _);

            Task.Delay(500, _cts.Token).ContinueWith(async t =>
            {
                if (t.IsCanceled) return;
                try
                {
                    await _schedulerService.UnschedulePlanAsync(oldPlanId, _cts.Token);
                    var plan = _planManager.GetPlanById(newPlanId);
                    if (plan != null)
                        await _schedulerService.SchedulePlanAsync(plan, _cts.Token);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plan yeniden adlandırma zamanlaması hatası: {OldPlanId} → {NewPlanId}",
                        oldPlanId, newPlanId);
                }
            }, TaskScheduler.Default);
        }
    }
}
