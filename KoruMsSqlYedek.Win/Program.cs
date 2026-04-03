using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Autofac;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Engine;
using KoruMsSqlYedek.Engine.Cloud;
using KoruMsSqlYedek.Win.Helpers;
using KoruMsSqlYedek.Win.IoC;
using Serilog;

namespace KoruMsSqlYedek.Win
{
    internal static class Program
    {
        private const string MutexName = "Global\\KoruMsSqlYedek_SingleInstance_7A3F";
        private static readonly uint WM_SHOWFIRSTINSTANCE = NativeMethods.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE_KoruMsSqlYedek");

        /// <summary>
        /// Uygulamanın ana girdi noktası.
        /// Tek instance (Mutex), Serilog, global exception handler.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Serilog yapılandırması
            ConfigureLogging();

            // Eski AppData klasöründen (MikroSqlDbYedek) migrasyon
            PathHelper.MigrateLegacyAppData();
            PathHelper.EnsureDirectoriesExist();

            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // Zaten çalışan bir instance var — onu ön plana getir
                    Log.Warning("KoruMsSqlYedek zaten çalışıyor. Mevcut instance ön plana getiriliyor.");
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
                ApplyGoogleOAuthOverride();

                // .NET 10 native dark mode — tema ayarına göre
                ApplyNativeColorMode();

                Log.Information("KoruMsSqlYedek başlatılıyor — v{Version}",
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
                    Theme.ModernMessageBox.Show(
                        Res.Get("Program_CriticalErrorMessage"),
                        Res.Get("Program_CriticalErrorTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                finally
                {
                    Log.Information("KoruMsSqlYedek kapatıldı.");
                    Log.CloseAndFlush();
                }
            }
        }

        private static void ConfigureLogging()
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KoruMsSqlYedek", "Logs", "mikrosqldb-.log");

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
                Theme.ModernTheme.ApplyLogColorScheme(settings.LogColorScheme);
                Log.Information("Tema ayarı uygulandı: {Theme}, Log şeması: {Scheme}",
                    settings.Theme, settings.LogColorScheme);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Tema ayarı uygulanırken hata, varsayılan (dark) kullanılacak.");
            }
        }

        /// <summary>
        /// .NET 10 native dark mode — ModernTheme ayarına göre SystemColorMode belirler.
        /// UI oluşturulmadan önce çağrılmalıdır.
        /// </summary>
        private static void ApplyNativeColorMode()
        {
            try
            {
                var colorMode = Theme.ModernTheme.CurrentTheme == Theme.ThemeMode.Dark
                    ? SystemColorMode.Dark
                    : SystemColorMode.Classic;
                Application.SetColorMode(colorMode);
                Log.Information(".NET 10 native color mode uygulandı: {ColorMode}", colorMode);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Native color mode uygulanırken hata.");
            }
        }

        /// <summary>
        /// AppSettings'te özel Google OAuth credential varsa GoogleDriveAuthHelper'a yükler.
        /// </summary>
        private static void ApplyGoogleOAuthOverride()
        {
            try
            {
                var settingsManager = new AppSettingsManager();
                var settings = settingsManager.Load();

                if (settings.HasCustomGoogleOAuth)
                {
                    string secret = PasswordProtector.Unprotect(settings.GoogleOAuthClientSecret);
                    GoogleDriveAuthHelper.SetCustomCredentials(settings.GoogleOAuthClientId, secret);
                    Log.Information("Özel Google OAuth credential yüklendi.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Google OAuth override uygulanırken hata, gömülü credential kullanılacak.");
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error(e.Exception, "UI thread'de yakalanmamış hata.");

            Theme.ModernMessageBox.Show(
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
