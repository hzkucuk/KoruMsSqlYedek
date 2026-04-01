using Autofac;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Engine;
using KoruMsSqlYedek.Engine.Backup;
using KoruMsSqlYedek.Engine.Compression;
using KoruMsSqlYedek.Engine.Update;
using KoruMsSqlYedek.Win.IPC;

namespace KoruMsSqlYedek.Win.IoC
{
    /// <summary>
    /// Tray (Win) uygulaması için hafif Autofac modülü.
    /// Yalnızca UI'ın doğrudan ihtiyaç duyduğu servisleri kayıt eder.
    /// Yedekleme motoru (Quartz, sıkıştırma, bulut, dosya yedekleme) KAYIT EDİLMEZ —
    /// bu servisler artık Windows Service tarafında çalışır.
    /// </summary>
    internal class WinModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Plan & ayar yöneticileri — UI okuma/yazma için gerekli
            builder.RegisterType<PlanManager>()
                .As<IPlanManager>()
                .SingleInstance();

            builder.RegisterType<AppSettingsManager>()
                .As<IAppSettingsManager>()
                .SingleInstance();

            builder.RegisterType<BackupHistoryManager>()
                .As<IBackupHistoryManager>()
                .SingleInstance();

            // SQL servisi — yalnızca PlanEditForm'daki bağlantı testi ve DB listeleme için
            builder.RegisterType<SqlBackupService>()
                .As<ISqlBackupService>()
                .InstancePerDependency();

            // Sıkıştırma servisi — RestoreDialog .7z arşiv açma için gerekli
            builder.RegisterType<SevenZipCompressionService>()
                .As<ICompressionService>()
                .InstancePerDependency();

            // Pipe istemcisi — servis ile IPC iletişimi
            builder.RegisterType<ServicePipeClient>()
                .AsSelf()
                .SingleInstance();

            // Güncelleme kontrolü — GitHub Releases API
            builder.RegisterType<UpdateChecker>()
                .As<IUpdateService>()
                .SingleInstance();
        }
    }
}
