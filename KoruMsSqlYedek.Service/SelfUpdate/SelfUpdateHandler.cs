using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace KoruMsSqlYedek.Service.SelfUpdate
{
    /// <summary>
    /// Self-update sonrası tray app yeniden başlatma işlemlerini yönetir.
    /// Restart flag dosyası üzerinden servis ↔ installer koordinasyonu sağlar.
    /// </summary>
    internal sealed class SelfUpdateHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SelfUpdateHandler>();

        /// <summary>
        /// ProgramData altındaki restart flag dosyasının yolu.
        /// Self-update installer tamamlandıktan sonra tray app'i yeniden başlatmak için kullanılır.
        /// </summary>
        internal static string RestartFlagPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KoruMsSqlYedek", "pending_restart.flag");

        /// <summary>
        /// Restart flag dosyasını yazar. Installer tamamlandıktan sonra tray app yolunu saklar.
        /// </summary>
        internal async Task WriteFlagAsync(string trayAppPath, CancellationToken cancellationToken)
        {
            string flagDir = Path.GetDirectoryName(RestartFlagPath)!;

            if (!Directory.Exists(flagDir))
                Directory.CreateDirectory(flagDir);

            await File.WriteAllTextAsync(RestartFlagPath, trayAppPath, cancellationToken)
                .ConfigureAwait(false);

            Log.Information("Restart flag yazıldı: {FlagPath}", RestartFlagPath);
        }

        /// <summary>
        /// Servis başlangıcında bekleyen restart flag'i kontrol eder.
        /// Installer tamamlandıktan sonra servis yeniden başlatıldıysa tray app'i kullanıcı oturumunda başlatır.
        /// </summary>
        internal async Task CheckPendingAppRestartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(RestartFlagPath))
                {
                    Log.Debug("Bekleyen restart flag yok, devam ediliyor.");
                    return;
                }

                string flagContent = File.ReadAllText(RestartFlagPath).Trim();
                Log.Information(
                    "Bekleyen restart flag bulundu: {FlagPath}, İçerik: {Content}",
                    RestartFlagPath, flagContent);

                // Installer hâlâ çalışıyor olabilir — sistemin oturmasını bekle
                Log.Information("Installer'ın tamamlanması bekleniyor (5 saniye)...");
                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);

                const int maxRetries = 3;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Log.Information(
                        "Tray app başlatma denemesi {Attempt}/{MaxRetries}",
                        attempt, maxRetries);

                    bool launched = LaunchTrayAppInUserSession(flagContent);

                    if (launched)
                    {
                        Log.Information(
                            "Tray app başarıyla başlatıldı (deneme {Attempt}/{MaxRetries}).",
                            attempt, maxRetries);
                        return;
                    }

                    if (attempt < maxRetries)
                    {
                        int delaySeconds = attempt * 5;
                        Log.Warning(
                            "Tray app başlatılamadı (deneme {Attempt}/{MaxRetries}), {Delay} saniye sonra tekrar denenecek.",
                            attempt, maxRetries, delaySeconds);

                        await Task.Delay(delaySeconds * 1000, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        Log.Error("Tray app {MaxRetries} denemede de başlatılamadı.", maxRetries);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Tray app restart kontrolü iptal edildi.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Bekleyen restart kontrolü hatası.");
            }
            finally
            {
                TryDeleteRestartFlag();
            }
        }

        /// <summary>
        /// Tray app'i aktif kullanıcı oturumunda başlatır.
        /// </summary>
        internal bool LaunchTrayAppInUserSession(string flagContent)
        {
            string trayAppPath = flagContent;

            if (string.IsNullOrWhiteSpace(trayAppPath) || !File.Exists(trayAppPath))
            {
                // Fallback: servis dizininden tahmin et
                string serviceDir = AppContext.BaseDirectory;
                trayAppPath = Path.GetFullPath(
                    Path.Combine(serviceDir, "..", "KoruMsSqlYedek.exe"));
                Log.Information(
                    "Flag içeriği geçersiz, fallback yol kullanılıyor: {Path}", trayAppPath);
            }

            if (!File.Exists(trayAppPath))
            {
                Log.Error("Tray app bulunamadı: {Path}", trayAppPath);
                return false;
            }

            Log.Information("Tray app başlatılıyor: {Path}", trayAppPath);
            return UserSessionLauncher.LaunchInUserSession(trayAppPath);
        }

        /// <summary>
        /// Restart flag dosyasını güvenli şekilde siler.
        /// </summary>
        internal void TryDeleteRestartFlag()
        {
            try
            {
                if (File.Exists(RestartFlagPath))
                    File.Delete(RestartFlagPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Restart flag dosyası silinemedi: {Path}", RestartFlagPath);
            }
        }
    }
}
