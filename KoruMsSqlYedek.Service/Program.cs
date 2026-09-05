using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Service.IoC;
using KoruMsSqlYedek.Service.Security;

namespace KoruMsSqlYedek.Service
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            // Uygulama dizinlerini oluştur
            PathHelper.EnsureDirectoriesExist();

            // Serilog yapılandırması (bootstrap logger)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(PathHelper.LogsDirectory, "service-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Veri dizinlerini (Plans, Logs, Config, UploadState, History, Updates) yalnızca
            // SYSTEM + Administrators erişebilecek şekilde kilitle — manuel/taşınabilir kurulumda
            // installer ACL uygulamamış olsa bile sıradan kullanıcılar plan/log/güncelleme
            // dosyalarını değiştiremesin.
            DirectoryAcl.EnsureAppDataDirectoriesRestricted();

            try
            {
                await Host.CreateDefaultBuilder(args)
                    .UseWindowsService(options =>
                    {
                        options.ServiceName = "KoruMsSqlYedekService";
                    })
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                    .ConfigureContainer<ContainerBuilder>(ServiceContainerBootstrap.Configure)
                    .UseSerilog()
                    .Build()
                    .RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Service host başlatılamadı.");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
