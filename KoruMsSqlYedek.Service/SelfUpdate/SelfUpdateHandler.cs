using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Service.Security;

namespace KoruMsSqlYedek.Service.SelfUpdate
{
    /// <summary>
    /// Self-update sonrası tray uygulamasının yeniden başlatılmasını koordine eder.
    /// Restart flag dosyası ile installer → servis arası iletişim sağlar.
    /// Flag dosyası: %ProgramData%\KoruMsSqlYedek\Updates\pending_restart.flag
    /// (Updates dizini yalnızca SYSTEM + Administrators erişimlidir.)
    /// GÜVENLİK: Flag yalnızca bir işaretçidir — içeriğine asla güvenilmez.
    /// Başlatılacak tray exe yolu her zaman kurulum düzeninden hesaplanır
    /// ({app}\Service\ → {app}\KoruMsSqlYedek.Win.exe).
    /// </summary>
    internal sealed class SelfUpdateHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SelfUpdateHandler>();

        private const string TrayExeName = "KoruMsSqlYedek.Win.exe";

        private static string FlagDirectory => DirectoryAcl.UpdatesDirectory;

        private static string RestartFlagPath => Path.Combine(FlagDirectory, "pending_restart.flag");

        /// <summary>
        /// Kurulum düzeninden tray exe yolunu hesaplar: servis dizininin bir üstü.
        /// </summary>
        public static string GetTrayAppPath()
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", TrayExeName));
        }

        /// <summary>
        /// Restart flag dosyasını kısıtlı Updates dizininde oluşturur.
        /// İçerik yalnızca bilgi amaçlıdır (zaman damgası); okunurken kullanılmaz.
        /// </summary>
        public async Task WriteFlagAsync(string trayAppPath, CancellationToken ct)
        {
            try
            {
                DirectoryAcl.EnsureDirectory(FlagDirectory, DirectoryAcl.UsersAccess.None);
                await File.WriteAllTextAsync(
                        RestartFlagPath,
                        $"{DateTime.UtcNow:O} {trayAppPath}",
                        ct)
                    .ConfigureAwait(false);
                Log.Information("Restart flag yazıldı: {FlagPath}", RestartFlagPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Restart flag yazılamadı: {FlagPath}", RestartFlagPath);
                throw;
            }
        }

        /// <summary>
        /// Servis başlangıcında bekleyen tray app restart'ı kontrol eder.
        /// Installer sonrası servis yeniden başladığında flag varsa tray'i kullanıcı oturumunda başlatır.
        /// Flag içeriği okunmaz; exe yolu kurulum düzeninden hesaplanır ve mevcut olması zorunludur.
        /// Exe henüz yoksa installer bitene kadar bekler (maks 5 dk, 10s aralık).
        /// </summary>
        public async Task CheckPendingAppRestartAsync(CancellationToken ct)
        {
            if (!File.Exists(RestartFlagPath))
                return;

            Log.Information("Pending restart flag bulundu: {FlagPath}", RestartFlagPath);

            string trayAppPath = GetTrayAppPath();

            // Exe'nin mevcut olmasını bekle — installer bitene kadar (maks 5 dk, 10s aralık)
            const int maxWaitMs = 5 * 60 * 1000;
            const int intervalMs = 10_000;
            int waited = 0;
            while (!File.Exists(trayAppPath))
            {
                if (waited >= maxWaitMs)
                {
                    Log.Error("Tray exe bulunamadı (tüm denemeler tükendi): {Path}", trayAppPath);
                    TryDeleteRestartFlag();
                    return;
                }

                Log.Information(
                    "Tray exe henüz mevcut değil, bekleniyor... ({Waited}s/{Max}s)",
                    waited / 1000, maxWaitMs / 1000);

                await Task.Delay(intervalMs, ct).ConfigureAwait(false);
                waited += intervalMs;
            }

            LaunchTrayAppInUserSession(trayAppPath);
            TryDeleteRestartFlag();
        }

        /// <summary>
        /// Tray uygulamasını aktif kullanıcının masaüstü oturumunda başlatır.
        /// UserSessionLauncher (CreateProcessAsUser) kullanır.
        /// Yol verilmezse kurulum düzeninden hesaplanır; dosya yoksa başlatılmaz.
        /// </summary>
        public void LaunchTrayAppInUserSession(string trayAppPath)
        {
            if (string.IsNullOrWhiteSpace(trayAppPath))
                trayAppPath = GetTrayAppPath();

            if (!File.Exists(trayAppPath))
            {
                Log.Error("Tray uygulaması bulunamadı, başlatılmadı: {Path}", trayAppPath);
                return;
            }

            Log.Information("Tray uygulaması kullanıcı oturumunda başlatılıyor: {Path}", trayAppPath);
            bool launched = UserSessionLauncher.LaunchInUserSession(trayAppPath);

            if (launched)
                Log.Information("Tray uygulaması başarıyla başlatıldı.");
            else
                Log.Error("Tray uygulaması başlatılamadı: {Path}", trayAppPath);
        }

        /// <summary>Restart flag dosyasını siler. Hata durumunda sessizce loglar.</summary>
        public void TryDeleteRestartFlag()
        {
            try
            {
                if (File.Exists(RestartFlagPath))
                {
                    File.Delete(RestartFlagPath);
                    Log.Debug("Restart flag silindi.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Restart flag silinemedi: {Path}", RestartFlagPath);
            }
        }
    }
}
