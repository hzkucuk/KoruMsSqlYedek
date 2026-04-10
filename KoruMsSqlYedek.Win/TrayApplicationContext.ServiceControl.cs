using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Win.Helpers;
using Serilog;

namespace KoruMsSqlYedek.Win
{
    partial class TrayApplicationContext
    {
        #region Service Control

        private void UpdateServiceMenuItems()
        {
            try
            {
                string state = QueryServiceState();
                bool running = state == "RUNNING";
                bool stopped = state == "STOPPED";

                _tsmServiceStatus.Text = running
                    ? Res.Get("Tray_ServiceStatusRunning")
                    : stopped
                        ? Res.Get("Tray_ServiceStatusStopped")
                        : Res.Format("Tray_ServiceStatusFormat", state);

                _tsmServiceStart.Enabled   = stopped;
                _tsmServiceStop.Enabled    = running;
                _tsmServiceRestart.Enabled = running;
            }
            catch (Exception ex)
            {
                // Servis yüklü değil veya sorgulanamadı
                if (_pipeClient.IsConnected)
                {
                    _tsmServiceStatus.Text     = Res.Get("Tray_ServiceDebugMode");
                    _tsmServiceStart.Enabled   = false;
                    _tsmServiceStop.Enabled    = false;
                    _tsmServiceRestart.Enabled = false;
                }
                else
                {
                    Log.Debug(ex, "Servis durumu sorgulanamadı: {ServiceName}", ServiceName);
                    _tsmServiceStatus.Text     = Res.Get("Tray_ServiceNotInstalled");
                    _tsmServiceStart.Enabled   = false;
                    _tsmServiceStop.Enabled    = false;
                    _tsmServiceRestart.Enabled = false;
                }
            }
        }

        /// <summary>sc.exe query ile servis durumunu sorgular (yönetici yetkisi gerekmez).</summary>
        private string QueryServiceState()
        {
            var psi = new System.Diagnostics.ProcessStartInfo("sc.exe", $"query {ServiceName}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            string output = proc!.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);

            // Parse STATE line: "        STATE              : 4  RUNNING"
            foreach (string line in output.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("STATE", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract last word (RUNNING, STOPPED, etc.)
                    string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length > 0 ? parts[^1] : "UNKNOWN";
                }
            }

            return "UNKNOWN";
        }

        private async void OnServiceStartClick(object sender, EventArgs e)
        {
            try
            {
                _tsmServiceStart.Enabled = false;
                await RunScCommandAsync("start");
                Theme.ModernToast.Success(Res.Get("AppName"), Res.Get("Tray_ServiceStarted"));
                Log.Information("Servis kullanıcı tarafından başlatıldı.");
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // Kullanıcı UAC'ı iptal etti
                Log.Information("Kullanıcı servis başlatma UAC isteğini iptal etti.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Servis başlatılamadı.");
                Theme.ModernToast.Error(Res.Get("AppName"), Res.Format("Tray_ServiceActionError", ex.Message));
            }
        }

        private async void OnServiceStopClick(object sender, EventArgs e)
        {
            try
            {
                _tsmServiceStop.Enabled    = false;
                _tsmServiceRestart.Enabled = false;
                await RunScCommandAsync("stop");
                Theme.ModernToast.Show(Res.Get("AppName"), Res.Get("Tray_ServiceStopped"), Theme.ToastType.Info);
                Log.Information("Servis kullanıcı tarafından durduruldu.");
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                Log.Information("Kullanıcı servis durdurma UAC isteğini iptal etti.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Servis durdurulamadı.");
                Theme.ModernToast.Error(Res.Get("AppName"), Res.Format("Tray_ServiceActionError", ex.Message));
            }
        }

        private async void OnServiceRestartClick(object sender, EventArgs e)
        {
            try
            {
                _tsmServiceStop.Enabled    = false;
                _tsmServiceRestart.Enabled = false;
                await RunScCommandAsync("stop");
                // Servisin tamamen durmasını bekle
                await Task.Delay(2000);
                await RunScCommandAsync("start");
                Theme.ModernToast.Success(Res.Get("AppName"), Res.Get("Tray_ServiceRestarted"));
                Log.Information("Servis kullanıcı tarafından yeniden başlatıldı.");
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                Log.Information("Kullanıcı servis yeniden başlatma UAC isteğini iptal etti.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Servis yeniden başlatılamadı.");
                Theme.ModernToast.Error(Res.Get("AppName"), Res.Format("Tray_ServiceActionError", ex.Message));
            }
        }

        /// <summary>
        /// sc.exe komutu çalıştırır (UAC ile yükseltilmiş).
        /// </summary>
        private static async Task RunScCommandAsync(string command)
        {
            await Task.Run(() =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo("sc.exe", $"{command} {ServiceName}")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                proc?.WaitForExit(30_000);
            });
        }

        #endregion
    }
}
