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
using KoruMsSqlYedek.Engine.Update;
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
        /// Güncellemeyi kurar (etkileşimli yol).
        /// Önce servise devreder: servis installer'ı kendisi indirir, SHA-256'sını
        /// doğrular ve SYSTEM olarak çalıştırır. Servis bağlı değilse fallback:
        /// tray (zaten yükseltilmiş) indirir, doğrular ve installer'ı başlatır.
        /// Checksum bilgisi yoksa kurulum yapılmaz — doğrulanmamış kuruluma düşülmez.
        /// </summary>
        private async Task DownloadAndLaunchUpdateAsync(UpdateInfo info)
        {
            if (!HasVerifiableChecksum(info))
            {
                Theme.ModernMessageBox.Show(
                    Res.Get("Update_ChecksumMissing"),
                    Res.Get("AppName"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Önce servis üzerinden kurulumu dene (indirme + doğrulama serviste)
                if (await TryInstallViaServiceAsync(info).ConfigureAwait(true))
                    return;

                // Fallback: yükseltilmiş tray kendisi indirir ve doğrular
                Log.Information("Servis mevcut değil, installer tray tarafından indirilip doğrulanacak.");

                Theme.ModernToast.Show(
                    Res.Get("AppName"),
                    Res.Format("Update_Downloading", 0),
                    Theme.ToastType.Info);

                string installerPath = await DownloadAndVerifyLocallyAsync(info).ConfigureAwait(true);
                if (installerPath is null)
                {
                    Theme.ModernMessageBox.Show(
                        Res.Get("Update_VerifyFailed"),
                        Res.Get("AppName"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                Theme.ModernToast.Success(
                    Res.Get("AppName"),
                    Res.Get("Update_DownloadComplete"));

                Log.Information("Installer doğrulandı, başlatılıyor: {Path}", installerPath);
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
        /// Güncellemeyi sessiz modda kurar.
        /// Önce servise devreder (servis indirir + doğrular + kurar).
        /// Servis bağlı değilse fallback: tray indirir, doğrular ve sessiz kurulumu başlatır.
        /// Checksum bilgisi yoksa kurulum yapılmaz.
        /// </summary>
        private async Task DownloadAndSilentInstallAsync(UpdateInfo info)
        {
            if (!HasVerifiableChecksum(info))
            {
                Theme.ModernToast.Show(
                    Res.Get("AppName"),
                    Res.Get("Update_ChecksumMissing"),
                    Theme.ToastType.Warning, 5000);
                return;
            }

            try
            {
                // Önce servis üzerinden kurulumu dene (indirme + doğrulama serviste)
                if (await TryInstallViaServiceAsync(info).ConfigureAwait(true))
                    return;

                // Fallback: yükseltilmiş tray kendisi indirir ve doğrular
                Log.Information("Servis mevcut değil, sessiz kurulum için installer tray tarafından indirilip doğrulanacak.");

                string installerPath = await DownloadAndVerifyLocallyAsync(info).ConfigureAwait(true);
                if (installerPath is null)
                {
                    Theme.ModernToast.Show(
                        Res.Get("AppName"),
                        Res.Get("Update_VerifyFailed"),
                        Theme.ToastType.Warning, 5000);
                    return;
                }

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
        /// UpdateInfo'da doğrulanabilir bir SHA-256 var mı? Yoksa loglar ve false döner.
        /// </summary>
        private static bool HasVerifiableChecksum(UpdateInfo info)
        {
            if (info is null || string.IsNullOrWhiteSpace(info.Sha256))
            {
                Log.Warning("Güncelleme v{Version} için SHA-256 bilgisi yok — kurulum reddedildi (doğrulanmamış kurulum yapılmaz).",
                    info?.Version);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Installer'ı rastgele adlı, kullanıcıya özel bir geçici alt klasöre indirir ve
        /// SHA-256 + boyut doğrulaması yapar. Doğrulama başarısızsa dosyayı siler ve null döner.
        /// </summary>
        private async Task<string> DownloadAndVerifyLocallyAsync(UpdateInfo info)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            string installerPath = Path.Combine(tempDir, $"KoruMsSqlYedek_Setup_v{info.Version}.exe");

            var progress = new Progress<int>(pct =>
            {
                _notifyIcon.Text = Res.Format("Update_Downloading", pct);
            });

            await _updateService.DownloadInstallerAsync(
                info.DownloadUrl, installerPath, progress).ConfigureAwait(true);

            Log.Information("Installer indirildi, doğrulanıyor: {Path}", installerPath);

            bool verified = await UpdateChecker.VerifyInstallerAsync(
                installerPath, info.Sha256, info.FileSizeBytes).ConfigureAwait(true);

            if (verified)
                return installerPath;

            Log.Error("Installer SHA-256 doğrulaması başarısız, dosya siliniyor: {Path}", installerPath);
            try
            {
                if (File.Exists(installerPath)) File.Delete(installerPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Doğrulanamayan installer silinemedi: {Path}", installerPath);
            }
            return null;
        }

        /// <summary>
        /// Servis pipe üzerinden kurulumu dener. Servis installer'ı URL'den kendisi indirir,
        /// beklenen SHA-256/boyut ile doğrular ve SYSTEM yetkileriyle çalıştırır.
        /// Başarılı olursa uygulamayı kapatır ve true döner.
        /// </summary>
        private async Task<bool> TryInstallViaServiceAsync(UpdateInfo info)
        {
            if (!_pipeClient.IsConnected)
            {
                Log.Information("Servis pipe bağlı değil, servis üzerinden kurulum atlanıyor.");
                return false;
            }

            if (!HasVerifiableChecksum(info))
                return false;

            try
            {
                Log.Information("Servis üzerinden kurulum isteniyor: v{Version} {Url} (SHA-256: {Sha})",
                    info.Version, info.DownloadUrl, info.Sha256);

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
                    await _pipeClient.SendInstallSelfUpdateAsync(
                        info.Version, info.DownloadUrl, info.Sha256, info.FileSizeBytes).ConfigureAwait(true);

                    // Servis yanıtını bekle (servis indirme + doğrulama yaptığı için 5 dakika)
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
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
