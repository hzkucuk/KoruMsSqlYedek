using System;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;

namespace KoruMsSqlYedek.Engine.Scheduling
{
    /// <summary>
    /// Periyodik rapor görevini çalıştıran Quartz.NET job'u.
    /// Quartz JobData'dan planId okur ve IReportingService üzerinden raporu gönderir.
    /// </summary>
    [DisallowConcurrentExecution]
    public class ReportingJob : IJob
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<ReportingJob>();

        // Autofac PropertiesAutowired ile enjekte edilir
        public IReportingService ReportingService { get; set; }
        public IPlanManager PlanManager { get; set; }

        public async Task Execute(IJobExecutionContext context)
        {
            string planId = context.JobDetail.JobDataMap.GetString("planId");
            if (string.IsNullOrEmpty(planId))
            {
                Log.Warning("ReportingJob: planId bulunamadı, job atlanıyor.");
                return;
            }

            var plan = PlanManager?.GetPlanById(planId);
            if (plan == null)
            {
                Log.Warning("ReportingJob: Plan bulunamadı — PlanId: {PlanId}", planId);
                return;
            }

            Log.Information("ReportingJob başlatıldı — Plan: {PlanName} ({PlanId})", plan.PlanName, planId);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                context.CancellationToken);

            try
            {
                await ReportingService.SendReportAsync(plan, cts.Token);
                Log.Information("ReportingJob tamamlandı — Plan: {PlanName}", plan.PlanName);
            }
            catch (OperationCanceledException)
            {
                Log.Warning("ReportingJob iptal edildi — Plan: {PlanId}", planId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ReportingJob hatası — Plan: {PlanId}", planId);
            }
        }
    }
}
