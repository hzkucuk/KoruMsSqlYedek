using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Engine;
using KoruMsSqlYedek.Win.Helpers;
using Serilog;

namespace KoruMsSqlYedek.Win
{
    partial class TrayApplicationContext
    {
        #region Update Check

        private async void OnUpdateTimerTick(object sender, EventArgs e)
        {
            // İlk tick sonrası aralığı günlük yap
            _updateTimer.Interval = UpdateCheckIntervalMs;

            try
            {
                UpdateInfo info = await _updateService.CheckForUpdateAsync().ConfigureAwait(true);
                if (info is not null)
                {
                    _pendingUpdate = info;
                    _tsmCheckUpdate.Text = Res.Format("Update_BalloonMessage", info.Version);
                    _tsmCheckUpdate.Font = new Font(_tsmCheckUpdate.Font, FontStyle.Bold);

                    // Sessiz güncelleme ayarı açıksa otomatik indir + kur
                    if (IsSilentUpdateEnabled())
                    {
                        Log.Information("Sessiz güncelleme aktif — v{Version} otomatik kurulacak.", info.Version);
                        await DownloadAndSilentInstallAsync(info).ConfigureAwait(true);
                        return;
                    }

                    Theme.ModernToast.Show(
                        Res.Get("Update_BalloonTitle"),
                        Res.Format("Update_BalloonMessage", info.Version),
                        Theme.ToastType.Info, 5000);

                    Log.Information("Yeni sürüm bildirimi gösterildi: v{Version}", info.Version);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Otomatik güncelleme kontrolü başarısız.");
            }
        }

        private async void OnCheckUpdateClick(object sender, EventArgs e)
        {
            _tsmCheckUpdate.Enabled = false;
            _tsmCheckUpdate.Text = Res.Get("Update_Checking");

            try
            {
                UpdateInfo info = await _updateService.CheckForUpdateAsync().ConfigureAwait(true);

                if (info is null)
                {
                    string currentVer = System.Reflection.Assembly
                        .GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
                    Theme.ModernMessageBox.Show(
                        Res.Format("Update_NoUpdateMessage", currentVer),
                        Res.Get("Update_NoUpdate"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                _pendingUpdate = info;
                DialogResult result = Theme.ModernMessageBox.Show(
                    Res.Format("Update_AvailableMessage", info.Version),
                    Res.Get("Update_Available"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1);

                if (result == DialogResult.Yes)
                {
                    await DownloadAndLaunchUpdateAsync(info).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Manuel güncelleme kontrolü başarısız.");
                Theme.ModernMessageBox.Show(
                    Res.Get("Update_CheckFailed"),
                    Res.Get("AppName"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                _tsmCheckUpdate.Enabled = true;
                _tsmCheckUpdate.Text = _pendingUpdate is not null
                    ? Res.Format("Update_BalloonMessage", _pendingUpdate.Version)
                    : Res.Get("Update_MenuCheckForUpdates");
            }
        }

        /// <summary>
        /// Installer'ı interaktif modda başlatır (kullanıcı onaylı güncelleme).
        /// </summary>
        private async Task DownloadAndLaunchUpdateAsync(UpdateInfo info)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "KoruUpdate");
            Directory.CreateDirectory(tempDir);
            string installerPath = Path.Combine(tempDir, $"KoruMsSqlYedek_Setup_v{info.Version}.exe");

            try
            {
                Theme.ModernToast.Show(
                    Res.Get("AppName"),
                    Res.Format("Update_Downloading", 0),
                    Theme.ToastType.Info);

                var progress = new Progress<int>(pct =>
                {
                    _notifyIcon.Text = Res.Format("Update_Downloading", pct);
                });

                await _updateService.DownloadInstallerAsync(
                    info.DownloadUrl, installerPath, progress).ConfigureAwait(true);

                Theme.ModernToast.Success(
                    Res.Get("AppName"),
                    Res.Get("Update_DownloadComplete"));

                Log.Information("Installer indirme tamamlandı, başlatılıyor: {Path}", installerPath);

                // Installer'ı başlat ve uygulamayı kapat
                Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                ExitApplication();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Güncelleme indirme/başlatma hatası.");
                Theme.ModernMessageBox.Show(
                    Res.Format("Update_DownloadFailed", ex.Message),
                    Res.Get("AppName"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Güncellemeyi sessiz modda indirir ve installer'ı /VERYSILENT ile çalıştırır.
        /// Installer tamamlandığında uygulamayı yeniden başlatır.
        /// </summary>
        private async Task DownloadAndSilentInstallAsync(UpdateInfo info)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "KoruUpdate");
            Directory.CreateDirectory(tempDir);
            string installerPath = Path.Combine(tempDir, $"KoruMsSqlYedek_Setup_v{info.Version}.exe");

            try
            {
                Theme.ModernToast.Show(
                    Res.Get("AppName"),
                    Res.Format("Update_SilentInstalling", info.Version),
                    Theme.ToastType.Info, 3000);

                var progress = new Progress<int>(pct =>
                {
                    _notifyIcon.Text = Res.Format("Update_Downloading", pct);
                });

                await _updateService.DownloadInstallerAsync(
                    info.DownloadUrl, installerPath, progress).ConfigureAwait(true);

                Log.Information("Sessiz güncelleme indirme tamamlandı: {Path}", installerPath);

                // InnoSetup /VERYSILENT: Hiçbir UI göstermez, /SUPPRESSMSGBOXES: Hata dialog'larını bastırır
                // /NORESTART: Otomatik restart engellenir, biz kendimiz yönetiriz
                // /CLOSEAPPLICATIONS: Çalışan uygulamaları kapatır
                // /RESTARTAPPLICATIONS: Kurulum sonrası uygulamayı yeniden başlatır
                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                var installerProcess = Process.Start(startInfo);

                if (installerProcess is not null)
                {
                    // Installer'ın bitmesini arka planda bekle, sonra uygulamayı kapat
                    // (InnoSetup /CLOSEAPPLICATIONS zaten kapatacak, ama biz de çıkıyoruz)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await installerProcess.WaitForExitAsync().ConfigureAwait(false);
                            int exitCode = installerProcess.ExitCode;
                            installerProcess.Dispose();

                            if (exitCode == 0)
                            {
                                Log.Information("Sessiz güncelleme başarıyla tamamlandı: v{Version}", info.Version);
                            }
                            else
                            {
                                Log.Warning("Sessiz güncelleme installer çıkış kodu: {ExitCode}", exitCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Sessiz güncelleme installer bekleme hatası.");
                        }
                    });

                    // Uygulamayı kapat — installer /RESTARTAPPLICATIONS ile yeniden başlatacak
                    ExitApplication();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Sessiz güncelleme indirme/başlatma hatası.");

                // Sessiz güncelleme başarısız olursa bildirim göster, uygulamayı kapatma
                Theme.ModernToast.Show(
                    Res.Get("AppName"),
                    Res.Format("Update_SilentFailed", ex.Message),
                    Theme.ToastType.Warning, 5000);
            }
        }

        /// <summary>
        /// Ayarlardan sessiz güncelleme tercihini okur.
        /// </summary>
        private static bool IsSilentUpdateEnabled()
        {
            try
            {
                var settingsManager = new AppSettingsManager();
                var settings = settingsManager.Load();
                return settings.AutoSilentUpdate;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Sessiz güncelleme ayarı okunamadı, varsayılan (false) kullanılıyor.");
                return false;
            }
        }

        #endregion
    }
}
