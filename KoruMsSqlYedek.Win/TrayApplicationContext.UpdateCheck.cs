using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Interfaces;
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

                    ShowBalloonTip(
                        Res.Get("Update_BalloonTitle"),
                        Res.Format("Update_BalloonMessage", info.Version),
                        ToolTipIcon.Info, 5000);

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

        private async Task DownloadAndLaunchUpdateAsync(UpdateInfo info)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "KoruUpdate");
            Directory.CreateDirectory(tempDir);
            string installerPath = Path.Combine(tempDir, $"KoruMsSqlYedek_Setup_v{info.Version}.exe");

            try
            {
                ShowBalloonTip(
                    Res.Get("AppName"),
                    Res.Format("Update_Downloading", 0),
                    ToolTipIcon.Info, 3000);

                var progress = new Progress<int>(pct =>
                {
                    _notifyIcon.Text = Res.Format("Update_Downloading", pct);
                });

                await _updateService.DownloadInstallerAsync(
                    info.DownloadUrl, installerPath, progress).ConfigureAwait(true);

                ShowBalloonTip(
                    Res.Get("AppName"),
                    Res.Get("Update_DownloadComplete"),
                    ToolTipIcon.Info, 2000);

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

        #endregion
    }
}
