using System;
using Autofac;
using Quartz;
using Quartz.Spi;
using Serilog;

namespace MikroSqlDbYedek.Engine.Scheduling
{
    /// <summary>
    /// Quartz.NET IJobFactory implementasyonu.
    /// Job'ları Autofac container üzerinden çözümler, böylece
    /// BackupJobExecutor'ın tüm bağımlılıkları otomatik enjekte edilir.
    /// </summary>
    public class AutofacJobFactory : IJobFactory
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<AutofacJobFactory>();
        private readonly ILifetimeScope _lifetimeScope;

        public AutofacJobFactory(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
        }

        /// <summary>
        /// Quartz scheduler tarafından her job tetiklemesinde çağrılır.
        /// Autofac'ten job instance'ı çözümlenir (PropertiesAutowired ile bağımlılıklar doldurulur).
        /// </summary>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var jobType = bundle.JobDetail.JobType;

            try
            {
                var job = (IJob)_lifetimeScope.Resolve(jobType);
                Log.Debug("Job oluşturuldu: {JobType}", jobType.Name);
                return job;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Job oluşturulamadı: {JobType}", jobType.Name);
                throw new SchedulerException($"Job oluşturulamadı: {jobType.FullName}", ex);
            }
        }

        /// <summary>
        /// Job tamamlandığında çağrılır.
        /// Autofac InstancePerDependency ile oluşturulan job'lar GC tarafından toplanır.
        /// </summary>
        public void ReturnJob(IJob job)
        {
            // Autofac InstancePerDependency kullanıldığında
            // IDisposable olan job'lar için dispose çağrılabilir
            if (job is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
