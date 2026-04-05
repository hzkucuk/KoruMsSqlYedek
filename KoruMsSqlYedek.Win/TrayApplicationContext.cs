using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Win.IPC;
using KoruMsSqlYedek.Win.Helpers;
using Serilog;

namespace KoruMsSqlYedek.Win
{
    /// <summary>
    /// System Tray tabanlı uygulama bağlamı.
    /// NotifyIcon ile tray'de çalışır; menüden tek MainWindow açılır, sekme seçimi ile yönlendirilir.
    /// Windows Service ile Named Pipe üzerinden iletişim kurar.
    /// </summary>
    internal class TrayApplicationContext : ApplicationContext
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<TrayApplicationContext>();

        private const string ServiceName = "KoruMsSqlYedekService";

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ILifetimeScope _scope;
        private readonly ServicePipeClient _pipeClient;
        private MainWindow _mainWindow;

        // Servis kontrol menü öğeleri
        private ToolStripMenuItem _tsmServiceStatus;
        private ToolStripMenuItem _tsmServiceStart;
        private ToolStripMenuItem _tsmServiceStop;
        private ToolStripMenuItem _tsmServiceRestart;

        // Yedekleme animasyon durumu
        private readonly System.Windows.Forms.Timer _animTimer;
        private Icon[] _animFrames;
        private int _animFrameIndex;
        private bool _isAnimating;

        // Tamamlanma animasyonu — kısa süre gösterilir, sonra idle'a döner
        private readonly System.Windows.Forms.Timer _completionTimer;
        private Icon[] _completionFrames;
        private int _completionFrameIndex;
        private bool _isCompletionAnimating;

        // Güncelleme kontrolü
        private readonly IUpdateService _updateService;
        private readonly System.Windows.Forms.Timer _updateTimer;
        private ToolStripMenuItem _tsmCheckUpdate;
        private UpdateInfo _pendingUpdate;

        /// <summary>Günlük güncelleme kontrolü aralığı (ms) — 24 saat.</summary>
        private const int UpdateCheckIntervalMs = 24 * 60 * 60 * 1000;

        /// <summary>İlk güncelleme kontrolü gecikmesi (ms) — 60 saniye.</summary>
        private const int UpdateCheckInitialDelayMs = 60_000;

        public TrayApplicationContext(
            ILifetimeScope scope,
            ServicePipeClient pipeClient)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (pipeClient == null) throw new ArgumentNullException(nameof(pipeClient));

            _scope = scope;
            _pipeClient = pipeClient;
            _updateService = scope.Resolve<IUpdateService>();

            Log.Information("KoruMsSqlYedek Tray uygulaması başlatılıyor...");

            _animTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _animTimer.Tick += OnAnimTimerTick;

            _completionTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _completionTimer.Tick += OnCompletionTimerTick;

            _contextMenu = CreateContextMenu();
            _notifyIcon = CreateNotifyIcon();

            ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_BalloonRunning"), ToolTipIcon.Info, 2000);

            BackupActivityHub.ActivityChanged += OnBackupActivityChanged;
            _pipeClient.ConnectionChanged    += OnPipeConnectionChanged;

            // Başlangıçta bağlı değil — ikonu ayarla
            UpdateTrayStatus(TrayIconStatus.Disconnected, Res.Get("Tray_TooltipDisconnected"));

            // Servis pipe bağlantısını başlat (arka planda otomatik yeniden bağlanır)
            _pipeClient.Start();

            // Günlük güncelleme kontrolü — ilk kontrol 60 sn sonra
            _updateTimer = new System.Windows.Forms.Timer { Interval = UpdateCheckInitialDelayMs };
            _updateTimer.Tick += OnUpdateTimerTick;
            _updateTimer.Start();

            Log.Information("Tray uygulaması başlatıldı.");
        }


        #region NotifyIcon & Menu

        private NotifyIcon CreateNotifyIcon()
        {
            var icon = new NotifyIcon
            {
                Icon = SymbolIconHelper.CreateTrayIcon(),
                Text = Res.Get("Tray_Tooltip"),
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            icon.DoubleClick += (s, e) => OpenMainWindow(0);

            return icon;
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var version = System.Reflection.Assembly
                .GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.18";
            menu.Renderer = new Theme.VersionSidebarRenderer(Res.Get("AppName"), $"v{version}");

            // Uygulama adı başlık öğesi
            var tsmAppName = new ToolStripMenuItem(Res.Get("AppName"))
            {
                Enabled = false,
                Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold)
            };

            var tsmDashboard = new ToolStripMenuItem(Res.Get("Tray_MenuDashboard"), null, (s, e) => OpenMainWindow(0));
            tsmDashboard.Font = new System.Drawing.Font(tsmDashboard.Font, System.Drawing.FontStyle.Bold);

            var tsmSettings = new ToolStripMenuItem(Res.Get("Tray_MenuSettings"), null, (s, e) => OpenMainWindow(3));
            var tsmExit = new ToolStripMenuItem(Res.Get("Tray_MenuExit"), null, OnExitClick);

            _tsmServiceStatus  = new ToolStripMenuItem(Res.Get("Tray_ServiceStatusUnknown")) { Enabled = false };
            _tsmServiceStart   = new ToolStripMenuItem(Res.Get("Tray_ServiceStart"),   null, OnServiceStartClick);
            _tsmServiceStop    = new ToolStripMenuItem(Res.Get("Tray_ServiceStop"),    null, OnServiceStopClick);
            _tsmServiceRestart = new ToolStripMenuItem(Res.Get("Tray_ServiceRestart"), null, OnServiceRestartClick);

            menu.Items.Add(tsmAppName);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmDashboard);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_tsmServiceStatus);
            menu.Items.Add(_tsmServiceStart);
            menu.Items.Add(_tsmServiceStop);
            menu.Items.Add(_tsmServiceRestart);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmSettings);
            menu.Items.Add(new ToolStripSeparator());
            _tsmCheckUpdate = new ToolStripMenuItem(Res.Get("Update_MenuCheckForUpdates"), null, OnCheckUpdateClick);
            menu.Items.Add(_tsmCheckUpdate);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmExit);

            menu.Opening += (s, e) => UpdateServiceMenuItems();

            return menu;
        }

        #endregion

        #region MainWindow

        private void OpenMainWindow(int tabIndex)
        {
            if (_mainWindow == null || _mainWindow.IsDisposed)
            {
                _mainWindow = _scope.Resolve<MainWindow>();
                _mainWindow.FormClosed += (s, e) => _mainWindow = null;
            }

            _mainWindow.SelectTab(tabIndex);
        }

        #endregion

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
                        : $"Servis: {state}";

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
                ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_ServiceStarted"), ToolTipIcon.Info, 2500);
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
                ShowBalloonTip(Res.Get("AppName"), Res.Format("Tray_ServiceActionError", ex.Message), ToolTipIcon.Error, 5000);
            }
        }

        private async void OnServiceStopClick(object sender, EventArgs e)
        {
            try
            {
                _tsmServiceStop.Enabled    = false;
                _tsmServiceRestart.Enabled = false;
                await RunScCommandAsync("stop");
                ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_ServiceStopped"), ToolTipIcon.Info, 2500);
                Log.Information("Servis kullanıcı tarafından durduruldu.");
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                Log.Information("Kullanıcı servis durdurma UAC isteğini iptal etti.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Servis durdurulamadı.");
                ShowBalloonTip(Res.Get("AppName"), Res.Format("Tray_ServiceActionError", ex.Message), ToolTipIcon.Error, 5000);
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
                ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_ServiceRestarted"), ToolTipIcon.Info, 2500);
                Log.Information("Servis kullanıcı tarafından yeniden başlatıldı.");
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                Log.Information("Kullanıcı servis yeniden başlatma UAC isteğini iptal etti.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Servis yeniden başlatılamadı.");
                ShowBalloonTip(Res.Get("AppName"), Res.Format("Tray_ServiceActionError", ex.Message), ToolTipIcon.Error, 5000);
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

        #region Exit

        private void OnExitClick(object sender, EventArgs e)
        {
            Log.Information("Kullanıcı çıkış isteğinde bulundu.");

            var result = Theme.ModernMessageBox.Show(
                Res.Get("Tray_ExitConfirmMessage"),
                Res.Get("Tray_ExitConfirmTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                ExitApplication();
            }
        }

        private void ExitApplication()
        {
            Log.Information("Tray uygulaması kapatılıyor...");

            _pipeClient?.Stop();
            _animTimer.Stop();
            _animTimer.Dispose();
            _completionTimer.Stop();
            _completionTimer.Dispose();

            _notifyIcon.Visible = false;

            if (_mainWindow != null && !_mainWindow.IsDisposed)
            {
                _mainWindow.FormClosed -= (s, e) => _mainWindow = null;
                _mainWindow.Close();
                _mainWindow = null;
            }

            _notifyIcon.Dispose();
            _contextMenu.Dispose();

            ExitThread();
        }

        #endregion

        #region Balloon Tips & Status

        /// <summary>
        /// Tray'den balloon tip bildirimi gösterir.
        /// </summary>
        internal void ShowBalloonTip(string title, string text, ToolTipIcon icon, int timeout = 3000)
        {
            _notifyIcon.ShowBalloonTip(timeout, title, text, icon);
        }

        /// <summary>
        /// Tray ikonunu durum bazlı günceller.
        /// </summary>
        internal void UpdateTrayStatus(TrayIconStatus status, string tooltipText = null)
        {
            var oldIcon = _notifyIcon.Icon;
            _notifyIcon.Icon = SymbolIconHelper.CreateStatusIcon(status);

            if (tooltipText != null)
            {
                _notifyIcon.Text = tooltipText.Length > 63
                    ? tooltipText.Substring(0, 63)
                    : tooltipText;
            }

            if (oldIcon != null)
            {
                try { oldIcon.Dispose(); }
                catch { /* ikon zaten serbest bırakılmış olabilir */ }
            }
        }

        #endregion

        #region Backup Activity

        private void OnPipeConnectionChanged(object sender, bool connected)
        {
            // Arka plan thread'inden gelebilir — UI thread'e aktar
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0]?.InvokeRequired == true)
            {
                Application.OpenForms[0].BeginInvoke(new Action(() => OnPipeConnectionChanged(sender, connected)));
                return;
            }

            if (connected)
            {
                UpdateTrayStatus(TrayIconStatus.Idle, Res.Get("Tray_Tooltip"));
                ShowBalloonTip(
                    Res.Get("Tray_ServiceConnectionTitle"),
                    Res.Get("Tray_ServiceConnected"),
                    ToolTipIcon.Info, 2000);
                Log.Information("Servis pipe bağlandı.");
            }
            else
            {
                UpdateTrayStatus(TrayIconStatus.Disconnected, Res.Get("Tray_TooltipDisconnected"));
                ShowBalloonTip(
                    Res.Get("Tray_ServiceConnectionTitle"),
                    Res.Get("Tray_ServiceDisconnected"),
                    ToolTipIcon.Warning, 3000);
                Log.Warning("Servis pipe bağlantısı kesildi.");
            }
        }

        private void OnBackupActivityChanged(object sender, BackupActivityEventArgs e)
        {
            // Arka plan thread'inden gelebilir — UI thread'e aktar
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0]?.InvokeRequired == true)
            {
                Application.OpenForms[0].BeginInvoke(new Action(() => OnBackupActivityChanged(sender, e)));
                return;
            }

            switch (e.ActivityType)
            {
                case BackupActivityType.Started:
                    StartTrayAnimation(Res.Format("Tray_BackupRunning", e.PlanName));
                    if (e.ToastEnabled)
                        ShowBalloonTip(
                            Res.Get("Toast_BackupStartedTitle"),
                            Res.Format("Toast_BackupStartedMessage", e.PlanName),
                            ToolTipIcon.Info);
                    break;

                case BackupActivityType.Completed:
                    StopTrayAnimation(TrayIconStatus.Success,
                        Res.Format("Tray_BackupCompleted", e.PlanName));
                    if (e.ToastEnabled)
                        ShowBalloonTip(
                            Res.Get("Toast_BackupCompletedTitle"),
                            Res.Format("Toast_BackupCompletedMessage", e.PlanName),
                            ToolTipIcon.Info);
                    break;

                case BackupActivityType.Failed:
                    StopTrayAnimation(TrayIconStatus.Error,
                        Res.Format("Tray_BackupFailed", e.PlanName));
                    if (e.ToastEnabled)
                        ShowBalloonTip(
                            Res.Get("Toast_BackupFailedTitle"),
                            Res.Format("Toast_BackupFailedMessage", e.PlanName),
                            ToolTipIcon.Error);
                    break;

                case BackupActivityType.Cancelled:
                    StopTrayAnimation(TrayIconStatus.Idle, Res.Get("Tray_Tooltip"));
                    if (e.ToastEnabled)
                        ShowBalloonTip(
                            Res.Get("Toast_BackupCancelledTitle"),
                            Res.Format("Toast_BackupCancelledMessage", e.PlanName),
                            ToolTipIcon.Warning);
                    break;
            }
        }

        private void OnAnimTimerTick(object sender, EventArgs e)
        {
            if (_animFrames == null) return;
            _animFrameIndex = (_animFrameIndex + 1) % _animFrames.Length;
            _notifyIcon.Icon = _animFrames[_animFrameIndex];
        }

        private void OnCompletionTimerTick(object sender, EventArgs e)
        {
            if (_completionFrames == null) return;
            _completionFrameIndex++;

            if (_completionFrameIndex >= _completionFrames.Length)
            {
                // Animasyon tamamlandı — idle'a dön
                StopCompletionAnimation();
                return;
            }

            _notifyIcon.Icon = _completionFrames[_completionFrameIndex];
        }

        private void StartTrayAnimation(string tooltipText)
        {
            if (_isAnimating) return;

            // Tamamlanma animasyonu çalışıyorsa önce durdur
            if (_isCompletionAnimating)
                StopCompletionAnimation();

            _animFrames = SymbolIconHelper.ExtractGifFrames("CloudSync.gif");
            _animFrameIndex = 0;
            _isAnimating = true;

            if (tooltipText != null)
                _notifyIcon.Text = tooltipText.Length > 63
                    ? tooltipText.Substring(0, 63)
                    : tooltipText;

            _notifyIcon.Icon = _animFrames[0];
            _animTimer.Start();
        }

        private void StopTrayAnimation(TrayIconStatus finalStatus, string tooltipText)
        {
            if (!_isAnimating) return;
            _animTimer.Stop();
            _isAnimating = false;

            var localFrames = _animFrames;
            int lastIndex = _animFrameIndex;
            _animFrames = null;

            // Başarılı tamamlandıysa kısa check-mark animasyonu göster
            if (finalStatus == TrayIconStatus.Success)
            {
                StartCompletionAnimation(tooltipText);
            }
            else
            {
                UpdateTrayStatus(finalStatus, tooltipText);
            }

            // Eski kareleri temizle
            DisposeFrames(localFrames, lastIndex);
        }

        /// <summary>
        /// Check-mark GIF animasyonunu başlatır; bitince idle ikona döner.
        /// </summary>
        private void StartCompletionAnimation(string tooltipText)
        {
            _completionFrames = SymbolIconHelper.ExtractGifFrames("CheckMark.gif");
            _completionFrameIndex = 0;
            _isCompletionAnimating = true;

            if (tooltipText != null)
                _notifyIcon.Text = tooltipText.Length > 63
                    ? tooltipText.Substring(0, 63)
                    : tooltipText;

            _notifyIcon.Icon = _completionFrames[0];
            _completionTimer.Start();
        }

        private void StopCompletionAnimation()
        {
            _completionTimer.Stop();
            _isCompletionAnimating = false;

            var localFrames = _completionFrames;
            int lastIndex = _completionFrameIndex;
            _completionFrames = null;

            // Idle ikona geri dön
            UpdateTrayStatus(TrayIconStatus.Idle, Res.Get("Tray_Tooltip"));

            DisposeFrames(localFrames, lastIndex);
        }

        private static void DisposeFrames(Icon[] frames, int skipIndex)
        {
            if (frames == null) return;
            for (int i = 0; i < frames.Length; i++)
            {
                if (i == skipIndex) continue;
                try { frames[i].Dispose(); } catch { }
            }
        }

        #endregion

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

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BackupActivityHub.ActivityChanged -= OnBackupActivityChanged;
                _pipeClient.ConnectionChanged    -= OnPipeConnectionChanged;

                if (_isAnimating)
                {
                    _animTimer.Stop();
                    if (_animFrames != null)
                        foreach (var f in _animFrames)
                            try { NativeMethods.DestroyIcon(f.Handle); } catch { }
                    _animFrames = null;
                    _isAnimating = false;
                }
                _animTimer?.Dispose();

                _updateTimer?.Stop();
                _updateTimer?.Dispose();

                _pipeClient?.Dispose();
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();

                if (_mainWindow != null && !_mainWindow.IsDisposed)
                    _mainWindow.Close();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
