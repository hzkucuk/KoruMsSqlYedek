using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.IPC;
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
        /// Installer'ı indirir ve kurar.
        /// Önce servis üzerinden UAC'sız kurulumu dener.
        /// Servis bağlı değilse fallback olarak runas ile başlatır.
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

                // Önce servis üzerinden UAC'sız kurulumu dene
                if (await TryInstallViaServiceAsync(installerPath).ConfigureAwait(true))
                    return;

                // Fallback: runas ile başlat (UAC gerekir)
                Log.Information("Servis mevcut değil, doğrudan runas ile başlatılıyor.");
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
        /// Güncellemeyi sessiz modda indirir ve kurar.
        /// Önce servis üzerinden UAC'sız kurulumu dener.
        /// Servis bağlı değilse fallback olarak runas ile çalıştırır.
        /// </summary>
        private async Task DownloadAndSilentInstallAsync(UpdateInfo info)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "KoruUpdate");
            Directory.CreateDirectory(tempDir);
            string installerPath = Path.Combine(tempDir, $"KoruMsSqlYedek_Setup_v{info.Version}.exe");

            try
            {
                var progress = new Progress<int>(pct =>
                {
                    _notifyIcon.Text = Res.Format("Update_Downloading", pct);
                });

                await _updateService.DownloadInstallerAsync(
                    info.DownloadUrl, installerPath, progress).ConfigureAwait(true);

                Log.Information("Sessiz güncelleme indirme tamamlandı: {Path}", installerPath);

                // Önce servis üzerinden UAC'sız kurulumu dene
                if (await TryInstallViaServiceAsync(installerPath).ConfigureAwait(true))
                    return;

                // Fallback: runas ile başlat (UAC gerekir)
                Log.Information("Servis mevcut değil, doğrudan runas ile sessiz kurulum başlatılıyor.");
                const string silentArgs = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /SP-";
                Log.Information("Installer başlatılıyor — FileName: {FileName}, Arguments: {Arguments}",
                    installerPath, silentArgs);

                Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = silentArgs,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                ExitApplication();
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // Kullanıcı UAC'yi iptal etti
                Log.Information("Sessiz güncelleme: Kullanıcı UAC onayını iptal etti.");
                Theme.ModernToast.Show(
                    Res.Get("AppName"),
                    Res.Get("Update_Cancelled"),
                    Theme.ToastType.Warning, 3000);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Sessiz güncelleme indirme/başlatma hatası.");

                Theme.ModernToast.Show(
                    Res.Get("AppName"),
                    Res.Format("Update_SilentFailed", ex.Message),
                    Theme.ToastType.Warning, 5000);
            }
        }

        /// <summary>
        /// Servis pipe üzerinden UAC'sız installer çalıştırmayı dener.
        /// Servis SYSTEM yetkileriyle installer'ı başlatır — UAC penceresi açılmaz.
        /// Başarılı olursa uygulamayı kapatır ve true döner.
        /// </summary>
        private async Task<bool> TryInstallViaServiceAsync(string installerPath)
        {
            if (!_pipeClient.IsConnected)
            {
                Log.Information("Servis pipe bağlı değil, UAC'sız kurulum atlanıyor.");
                return false;
            }

            try
            {
                Log.Information("Servis üzerinden UAC'sız kurulum başlatılıyor: {Path}", installerPath);

                // Yanıt beklemek için TaskCompletionSource kullan
                var tcs = new TaskCompletionSource<InstallSelfUpdateResponseMessage>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                void OnResponse(object sender, InstallSelfUpdateResponseMessage response)
                {
                    tcs.TrySetResult(response);
                }

                _pipeClient.SelfUpdateResponseReceived += OnResponse;

                try
                {
                    await _pipeClient.SendInstallSelfUpdateAsync(installerPath).ConfigureAwait(true);

                    // Servis yanıtını bekle (15 saniye timeout)
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    using (cts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        InstallSelfUpdateResponseMessage result = await tcs.Task.ConfigureAwait(true);

                        if (result.Success)
                        {
                            Log.Information("Self-update servise devredildi, uygulama kapatılıyor.");
                            ExitApplication();
                            return true;
                        }

                        Log.Warning("Servis üzerinden self-update başarısız: {Message}", result.Message);
                        return false;
                    }
                }
                finally
                {
                    _pipeClient.SelfUpdateResponseReceived -= OnResponse;
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Servis self-update yanıtı zaman aşımına uğradı.");
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Servis üzerinden self-update denenirken hata.");
                return false;
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
