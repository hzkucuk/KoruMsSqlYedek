using Autofac;
using Microsoft.Extensions.Hosting;
using KoruMsSqlYedek.Core.IPC;
using KoruMsSqlYedek.Engine.IoC;
using KoruMsSqlYedek.Service.IPC;

namespace KoruMsSqlYedek.Service.IoC
{
    /// <summary>
    /// Windows Service için Autofac container yapılandırması.
    /// Engine servislerini ve BackupWindowsService'i kaydeder.
    /// </summary>
    internal static class ServiceContainerBootstrap
    {
        /// <summary>
        /// Autofac container builder'ını yapılandırır.
        /// Microsoft.Extensions.Hosting ile entegre çalışır.
        /// </summary>
        public static void Configure(ContainerBuilder builder)
        {
            // Engine katmanı modülü (tüm servisler)
            builder.RegisterModule<EngineModule>();

            // Named Pipe IPC
            builder.RegisterType<BackupCancellationRegistry>()
                .As<IBackupCancellationRegistry>()
                .SingleInstance();

            builder.RegisterType<ServicePipeServer>()
                .AsSelf()
                .SingleInstance();

            // Windows Service (IHostedService olarak kayıtlı)
            builder.RegisterType<BackupWindowsService>()
                .As<IHostedService>()
                .SingleInstance();
        }
    }
}
