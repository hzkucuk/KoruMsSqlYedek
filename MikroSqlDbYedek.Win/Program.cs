using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Autofac;
using MikroSqlDbYedek.Engine;
using MikroSqlDbYedek.Win.Helpers;
using MikroSqlDbYedek.Win.IoC;
using Serilog;

namespace MikroSqlDbYedek.Win
{
    internal static class Program
    {
        private const string MutexName = "Global\\MikroSqlDbYedek_SingleInstance_7A3F";
        private static readonly uint WM_SHOWFIRSTINSTANCE = NativeMethods.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE_MikroSqlDbYedek");

        /// <summary>
        /// Uygulamanın ana girdi noktası.
        /// Tek instance (Mutex), Serilog, global exception handler.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Serilog yapılandırması
            ConfigureLogging();

            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // Zaten çalışan bir instance var — onu ön plana getir
                    Log.Warning("MikroSqlDbYedek zaten çalışıyor. Mevcut instance ön plana getiriliyor.");
                    NativeMethods.SendMessage(
                        (IntPtr)NativeMethods.HWND_BROADCAST,
                        WM_SHOWFIRSTINSTANCE,
                        IntPtr.Zero,
                        IntPtr.Zero);
                    return;
                }

                // Global exception handler'lar
                Application.ThreadException += OnThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Dil ve tema ayarlarını uygula (container'dan önce)
                ApplyLanguageSetting();
                ApplyThemeSetting();

                Log.Information("MikroSqlDbYedek başlatılıyor — v{Version}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                try
                {
                    using (var container = WinContainerBootstrap.Build())
                    {
                        var trayContext = container.Resolve<TrayApplicationContext>();
                        Application.Run(trayContext);
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Uygulama beklenmeyen bir hata ile sonlandı.");
                    MessageBox.Show(
                        Res.Get("Program_CriticalErrorMessage"),
                        Res.Get("Program_CriticalErrorTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    Log.Information("MikroSqlDbYedek kapatıldı.");
                    Log.CloseAndFlush();
                }
            }
        }

        private static void ConfigureLogging()
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MikroSqlDbYedek", "Logs", "mikrosqldb-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logPath,
                    rollingInterval: Serilog.RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        /// <summary>
        /// AppSettings'ten dil ayarını okur ve CurrentUICulture'ı ayarlar.
        /// Container oluşturulmadan önce çağrılır.
        /// </summary>
        private static void ApplyLanguageSetting()
        {
            try
            {
                var settingsManager = new AppSettingsManager();
                var settings = settingsManager.Load();
                var cultureName = string.IsNullOrWhiteSpace(settings.Language) ? "tr-TR" : settings.Language;

                var culture = new CultureInfo(cultureName);
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;

                Log.Information("Dil ayarı uygulandı: {Culture}", cultureName);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dil ayarı uygulanırken hata, varsayılan (tr-TR) kullanılacak.");
            }
        }

        private static void ApplyThemeSetting()
        {
            try
            {
                var settingsManager = new AppSettingsManager();
                var settings = settingsManager.Load();
                var mode = settings.Theme == "light"
                    ? Theme.ThemeMode.Light
                    : Theme.ThemeMode.Dark;
                Theme.ModernTheme.ApplyTheme(mode);
                Log.Information("Tema ayarı uygulandı: {Theme}", settings.Theme);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Tema ayarı uygulanırken hata, varsayılan (dark) kullanılacak.");
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error(e.Exception, "UI thread'de yakalanmamış hata.");

            MessageBox.Show(
                Res.Get("Program_ThreadErrorMessage"),
                Res.Get("Program_ThreadErrorTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Fatal(ex, "İşlenemeyen hata (IsTerminating={IsTerminating}).", e.IsTerminating);
        }
    }
}
