using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace KoruMsSqlYedek.Service.SelfUpdate
{
    /// <summary>
    /// Self-update sonrası tray uygulamasının yeniden başlatılmasını koordine eder.
    /// Restart flag dosyası ile installer → servis arası iletişim sağlar.
    /// Flag dosyası: %ProgramData%\KoruMsSqlYedek\pending_restart.flag
    /// İçerik: yeniden başlatılacak tray exe yolu.
    /// </summary>
    internal sealed class SelfUpdateHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SelfUpdateHandler>();

        private static readonly string FlagDirectory =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KoruMsSqlYedek");

        private static readonly string RestartFlagPath =
            Path.Combine(FlagDirectory, "pending_restart.flag");

        /// <summary>
        /// Restart flag dosyasını oluşturur.
        /// İçeriğe tray app yolunu yazar — servis startup'ta bu yolu okuyup tray'i başlatır.
        /// </summary>
        public async Task WriteFlagAsync(string trayAppPath, CancellationToken ct)
        {
            try
            {
                Directory.CreateDirectory(FlagDirectory);
                await File.WriteAllTextAsync(RestartFlagPath, trayAppPath, ct)
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
        /// Retry: 3 deneme (1s, 3s, 5s) — installer dosyaları kopyalıyor olabilir.
        /// </summary>
        public async Task CheckPendingAppRestartAsync(CancellationToken ct)
        {
            if (!File.Exists(RestartFlagPath))
                return;

            Log.Information("Pending restart flag bulundu: {FlagPath}", RestartFlagPath);

            string trayAppPath;
            try
            {
                trayAppPath = (await File.ReadAllTextAsync(RestartFlagPath, ct)
                    .ConfigureAwait(false)).Trim();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Restart flag okunamadı.");
                TryDeleteRestartFlag();
                return;
            }

            if (string.IsNullOrWhiteSpace(trayAppPath))
            {
                Log.Warning("Restart flag boş — tray app yolu yok.");
                TryDeleteRestartFlag();
                return;
            }

            // Exe'nin mevcut olmasını bekle (installer kopyalıyor olabilir)
            int[] delaysMs = [1000, 3000, 5000];
            for (int i = 0; i < delaysMs.Length; i++)
            {
                if (File.Exists(trayAppPath))
                    break;

                Log.Information(
                    "Tray exe henüz mevcut değil, bekleniyor... Deneme {Attempt}/{Max}",
                    i + 1, delaysMs.Length);

                await Task.Delay(delaysMs[i], ct).ConfigureAwait(false);
            }

            if (!File.Exists(trayAppPath))
            {
                Log.Error("Tray exe bulunamadı (tüm denemeler tükendi): {Path}", trayAppPath);
                TryDeleteRestartFlag();
                return;
            }

            LaunchTrayAppInUserSession(trayAppPath);
            TryDeleteRestartFlag();
        }

        /// <summary>
        /// Tray uygulamasını aktif kullanıcının masaüstü oturumunda başlatır.
        /// UserSessionLauncher (CreateProcessAsUser) kullanır.
        /// </summary>
        public void LaunchTrayAppInUserSession(string trayAppPath)
        {
            if (string.IsNullOrWhiteSpace(trayAppPath))
            {
                // Fallback: servis dizininden bir üst klasördeki exe'yi dene
                string serviceDir = AppContext.BaseDirectory;
                trayAppPath = Path.GetFullPath(
                    Path.Combine(serviceDir, "..", "KoruMsSqlYedek.exe"));
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
