using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Scheduling
{
    /// <summary>
    /// Quartz.NET tabanlı zamanlama servisi.
    /// </summary>
    public class QuartzSchedulerService : ISchedulerService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<QuartzSchedulerService>();
        private readonly Quartz.Spi.IJobFactory _jobFactory;
        private IScheduler _scheduler;

        public bool IsRunning => _scheduler != null && _scheduler.IsStarted && !_scheduler.IsShutdown;

        public QuartzSchedulerService()
        {
        }

        /// <summary>
        /// Autofac tarafından enjekte edilen IJobFactory ile oluşturur.
        /// </summary>
        public QuartzSchedulerService(Quartz.Spi.IJobFactory jobFactory)
        {
            _jobFactory = jobFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new StdSchedulerFactory();
            _scheduler = await factory.GetScheduler(cancellationToken);

            if (_jobFactory != null)
            {
                _scheduler.JobFactory = _jobFactory;
            }

            await _scheduler.Start(cancellationToken);
            Log.Information("Quartz scheduler başlatıldı.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scheduler != null && !_scheduler.IsShutdown)
            {
                await _scheduler.Shutdown(waitForJobsToComplete: true, cancellationToken);
                Log.Information("Quartz scheduler durduruldu (graceful).");
            }
        }

        public async Task SchedulePlanAsync(BackupPlan plan, CancellationToken cancellationToken)
        {
            if (_scheduler == null)
                throw new InvalidOperationException("Scheduler henüz başlatılmamış.");

            await UnschedulePlanAsync(plan.PlanId, cancellationToken);

            if (!plan.IsEnabled)
                return;

            // Full yedek job'u
            if (!string.IsNullOrEmpty(plan.Strategy.FullSchedule))
            {
                await ScheduleJobAsync(
                    plan.PlanId, "Full", plan.Strategy.FullSchedule, cancellationToken);
            }

            // Differential yedek job'u
            if (plan.Strategy.Type >= BackupStrategyType.FullPlusDifferential &&
                !string.IsNullOrEmpty(plan.Strategy.DifferentialSchedule))
            {
                await ScheduleJobAsync(
                    plan.PlanId, "Differential", plan.Strategy.DifferentialSchedule, cancellationToken);
            }

            // Incremental yedek job'u
            if (plan.Strategy.Type == BackupStrategyType.FullPlusDifferentialPlusIncremental &&
                !string.IsNullOrEmpty(plan.Strategy.IncrementalSchedule))
            {
                await ScheduleJobAsync(
                    plan.PlanId, "Incremental", plan.Strategy.IncrementalSchedule, cancellationToken);
            }

            // Dosya yedekleme job'u (ayrı zamanlama varsa)
            if (plan.FileBackup != null && plan.FileBackup.IsEnabled &&
                !string.IsNullOrEmpty(plan.FileBackup.Schedule))
            {
                await ScheduleJobAsync(
                    plan.PlanId, "FileBackup", plan.FileBackup.Schedule, cancellationToken);
            }

            // Periyodik raporlama job'u
            if (plan.Reporting != null && plan.Reporting.IsEnabled)
            {
                string reportCron = BuildReportingCron(plan.Reporting);
                if (!string.IsNullOrEmpty(reportCron))
                {
                    await ScheduleReportingJobAsync(plan.PlanId, reportCron, cancellationToken);
                }
            }

            Log.Information(
                "Plan zamanlandı: {PlanId} - {PlanName} ({Strategy})",
                plan.PlanId, plan.PlanName, plan.Strategy.Type);
        }

        public async Task UnschedulePlanAsync(string planId, CancellationToken cancellationToken)
        {
            if (_scheduler == null)
                return;

            string[] types = { "Full", "Differential", "Incremental", "FileBackup" };
            foreach (string type in types)
            {
                var jobKey = new JobKey($"{planId}_{type}", "BackupJobs");
                if (await _scheduler.CheckExists(jobKey, cancellationToken))
                {
                    await _scheduler.DeleteJob(jobKey, cancellationToken);
                }
            }

            var reportingKey = new JobKey($"{planId}_Reporting", "ReportingJobs");
            if (await _scheduler.CheckExists(reportingKey, cancellationToken))
            {
                await _scheduler.DeleteJob(reportingKey, cancellationToken);
            }

            Log.Information("Plan zamanlaması kaldırıldı: {PlanId}", planId);
        }

        public async Task TriggerPlanNowAsync(string planId, CancellationToken cancellationToken)
        {
            if (_scheduler == null)
                throw new InvalidOperationException("Scheduler henüz başlatılmamış.");

            bool triggered = false;

            // SQL yedek: Full > Differential > Incremental (en yüksek öncelikliyi tetikle)
            string[] sqlTypes = { "Full", "Differential", "Incremental" };
            foreach (string type in sqlTypes)
            {
                var jobKey = new JobKey($"{planId}_{type}", "BackupJobs");
                if (await _scheduler.CheckExists(jobKey, cancellationToken))
                {
                    await _scheduler.TriggerJob(jobKey, cancellationToken);
                    Log.Information("Plan manuel tetiklendi: {PlanId} ({BackupType})", planId, type);
                    triggered = true;
                    break;
                }
            }

            // Dosya yedekleme — ayrı schedule'a sahip FileBackup job'u varsa ayrıca tetikle
            var fileJobKey = new JobKey($"{planId}_FileBackup", "BackupJobs");
            if (await _scheduler.CheckExists(fileJobKey, cancellationToken))
            {
                await _scheduler.TriggerJob(fileJobKey, cancellationToken);
                Log.Information("Dosya yedekleme manuel tetiklendi: {PlanId}", planId);
                triggered = true;
            }

            if (!triggered)
                Log.Warning("Manuel tetikleme: Zamanlanmış job bulunamadı: {PlanId}", planId);
        }

        public async Task<DateTimeOffset?> GetNextFireTimeAsync(string planId, CancellationToken cancellationToken)
        {
            if (_scheduler == null || !IsRunning)
                return null;

            DateTimeOffset? earliest = null;
            string[] types = { "Full", "Differential", "Incremental", "FileBackup", "Reporting" };

            foreach (string type in types)
            {
                string group = type == "Reporting" ? "ReportingJobs" : "BackupJobs";
                var jobKey = new JobKey($"{planId}_{type}", group);
                if (!await _scheduler.CheckExists(jobKey, cancellationToken))
                    continue;

                var triggers = await _scheduler.GetTriggersOfJob(jobKey, cancellationToken);
                foreach (var trigger in triggers)
                {
                    DateTimeOffset? next = trigger.GetNextFireTimeUtc();
                    if (next.HasValue && (!earliest.HasValue || next.Value < earliest.Value))
                        earliest = next.Value;
                }
            }

            return earliest;
        }

        private async Task ScheduleJobAsync(
            string planId, string backupType, string cronExpression,
            CancellationToken cancellationToken)
        {
            var jobKey = new JobKey($"{planId}_{backupType}", "BackupJobs");

            var job = JobBuilder.Create<BackupJobExecutor>()
                .WithIdentity(jobKey)
                .UsingJobData("planId", planId)
                .UsingJobData("backupType", backupType)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{planId}_{backupType}_trigger", "BackupTriggers")
                .WithCronSchedule(cronExpression)
                .Build();

            await _scheduler.ScheduleJob(job, trigger, cancellationToken);

            Log.Debug(
                "Job zamanlandı: {PlanId} ({BackupType}) — Cron: {Cron}",
                planId, backupType, cronExpression);
        }

        private async Task ScheduleReportingJobAsync(
            string planId, string cronExpression,
            CancellationToken cancellationToken)
        {
            var jobKey = new JobKey($"{planId}_Reporting", "ReportingJobs");

            var job = JobBuilder.Create<ReportingJob>()
                .WithIdentity(jobKey)
                .UsingJobData("planId", planId)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"{planId}_Reporting_trigger", "ReportingTriggers")
                .WithCronSchedule(cronExpression)
                .Build();

            await _scheduler.ScheduleJob(job, trigger, cancellationToken);

            Log.Debug(
                "Raporlama job'u zamanlandı: {PlanId} — Cron: {Cron}",
                planId, cronExpression);
        }

        /// <summary>
        /// Raporlama sıklığına ve gönderim saatine göre Quartz cron ifadesi üretir.
        /// </summary>
        public static string BuildReportingCron(ReportingConfig reporting)
        {
            if (reporting == null || !reporting.IsEnabled)
                return null;

            int hour = Math.Clamp(reporting.SendHour, 0, 23);

            switch (reporting.Frequency)
            {
                case ReportFrequency.Daily:
                    return $"0 0 {hour} * * ?";

                case ReportFrequency.Weekly:
                    // Her Pazartesi belirlenen saatte
                    return $"0 0 {hour} ? * MON";

                case ReportFrequency.Monthly:
                    // Her ayın 1'inde belirlenen saatte
                    return $"0 0 {hour} 1 * ?";

                default:
                    return $"0 0 {hour} * * ?";
            }
        }
    }
}
