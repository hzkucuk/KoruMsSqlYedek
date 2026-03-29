using Autofac;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Engine.Backup;
using MikroSqlDbYedek.Engine.Cloud;
using MikroSqlDbYedek.Engine.Compression;
using MikroSqlDbYedek.Engine.FileBackup;
using MikroSqlDbYedek.Engine.Notification;
using MikroSqlDbYedek.Engine.Retention;
using MikroSqlDbYedek.Engine.Scheduling;

namespace MikroSqlDbYedek.Engine.IoC
{
    /// <summary>
    /// Engine katmanı Autofac modülü.
    /// Tüm servis kayıtlarını merkezi olarak yönetir.
    /// </summary>
    public class EngineModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Plan & Settings yöneticileri (singleton — uygulama genelinde tek instance)
            builder.RegisterType<PlanManager>()
                .As<IPlanManager>()
                .SingleInstance();

            builder.RegisterType<AppSettingsManager>()
                .As<IAppSettingsManager>()
                .SingleInstance();

            builder.RegisterType<BackupHistoryManager>()
                .As<IBackupHistoryManager>()
                .SingleInstance();

            // Yedekleme servisleri (instance per dependency — her çağrıda yeni)
            builder.RegisterType<SqlBackupService>()
                .As<ISqlBackupService>()
                .InstancePerDependency();

            builder.RegisterType<BackupChainValidator>()
                .AsSelf()
                .InstancePerDependency();

            // Sıkıştırma
            builder.RegisterType<SevenZipCompressionService>()
                .As<ICompressionService>()
                .AsSelf()
                .SingleInstance();

            // Dosya yedekleme & VSS
            builder.RegisterType<VssSnapshotService>()
                .As<IVssService>()
                .InstancePerDependency();

            builder.RegisterType<FileBackupService>()
                .As<IFileBackupService>()
                .InstancePerDependency();

            // Retention
            builder.RegisterType<RetentionCleanupService>()
                .As<IRetentionService>()
                .InstancePerDependency();

            // Bildirim
            builder.RegisterType<EmailNotificationService>()
                .As<INotificationService>()
                .InstancePerDependency();

            // Bulut provider fabrikası & orkestratör
            builder.RegisterType<CloudProviderFactory>()
                .As<ICloudProviderFactory>()
                .SingleInstance();

            builder.RegisterType<CloudUploadOrchestrator>()
                .UsingConstructor(typeof(ICloudProviderFactory))
                .As<ICloudUploadOrchestrator>()
                .InstancePerDependency();

            // Zamanlama
            // Zamanlama — AutofacJobFactory ile Quartz.NET IJob çözümleme
            builder.RegisterType<AutofacJobFactory>()
                .As<Quartz.Spi.IJobFactory>()
                .SingleInstance();

            builder.RegisterType<QuartzSchedulerService>()
                .As<ISchedulerService>()
                .SingleInstance();

            // Quartz IJob — property injection ile bağımlılıkları enjekte et
            builder.RegisterType<BackupJobExecutor>()
                .AsSelf()
                .PropertiesAutowired()
                .InstancePerDependency();
        }
    }
}
