using Autofac;
using MikroSqlDbYedek.Engine.IoC;

namespace MikroSqlDbYedek.Win.IoC
{
    /// <summary>
    /// Win (Tray) uygulaması için Autofac container yapılandırması.
    /// Engine servislerini ve Win formlarını kaydeder.
    /// </summary>
    internal static class WinContainerBootstrap
    {
        /// <summary>
        /// Autofac container'ını yapılandırır ve oluşturur.
        /// </summary>
        public static IContainer Build()
        {
            var builder = new ContainerBuilder();

            // Engine katmanı modülü (tüm servisler)
            builder.RegisterModule<EngineModule>();

            // Win formları — her çağrıda yeni instance
            builder.RegisterType<TrayApplicationContext>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<MainDashboardForm>()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<Forms.PlanListForm>()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<Forms.PlanEditForm>()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<Forms.ManualBackupDialog>()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<Forms.SettingsForm>()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<Forms.LogViewerForm>()
                .AsSelf()
                .InstancePerDependency();

            return builder.Build();
        }
    }
}
