using Autofac;
using MikroSqlDbYedek.Engine.IoC;

namespace MikroSqlDbYedek.Win.IoC
{
    /// <summary>
    /// Win (Tray) uygulaması için Autofac container yapılandırması.
    /// Engine servislerini ve MainWindow'u kaydeder.
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

            // TrayApplicationContext — tek instance
            builder.RegisterType<TrayApplicationContext>()
                .AsSelf()
                .SingleInstance();

            // Ana pencere — tek instance (tray'den her açılışta aynı pencere)
            builder.RegisterType<MainWindow>()
                .AsSelf()
                .SingleInstance();

            // Düzenleme formları — ayrı pencere olarak kalır, her açılışta yeni instance
            builder.RegisterType<Forms.PlanEditForm>()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<Forms.CloudTargetEditDialog>()
                .AsSelf()
                .InstancePerDependency();

            builder.RegisterType<Forms.FileBackupSourceEditDialog>()
                .AsSelf()
                .InstancePerDependency();

            return builder.Build();
        }
    }
}
