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

            Log.Information("Plan zamanlaması kaldırıldı: {PlanId}", planId);
        }

        public async Task TriggerPlanNowAsync(string planId, CancellationToken cancellationToken)
        {
            if (_scheduler == null)
                throw new InvalidOperationException("Scheduler henüz başlatılmamış.");

            var jobKey = new JobKey($"{planId}_Full", "BackupJobs");
            if (await _scheduler.CheckExists(jobKey, cancellationToken))
            {
                await _scheduler.TriggerJob(jobKey, cancellationToken);
                Log.Information("Plan manuel tetiklendi: {PlanId}", planId);
            }
            else
            {
                Log.Warning("Manuel tetikleme: Job bulunamadı: {PlanId}", planId);
            }
        }

        public async Task<DateTimeOffset?> GetNextFireTimeAsync(string planId, CancellationToken cancellationToken)
        {
            if (_scheduler == null || !IsRunning)
                return null;

            DateTimeOffset? earliest = null;
            string[] types = { "Full", "Differential", "Incremental", "FileBackup" };

            foreach (string type in types)
            {
                var jobKey = new JobKey($"{planId}_{type}", "BackupJobs");
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
    }
}
