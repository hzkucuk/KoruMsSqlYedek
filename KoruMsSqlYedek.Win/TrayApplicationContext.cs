using System;
using System.Drawing;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac;
using KoruMsSqlYedek.Core.Events;
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

        public TrayApplicationContext(
            ILifetimeScope scope,
            ServicePipeClient pipeClient)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (pipeClient == null) throw new ArgumentNullException(nameof(pipeClient));

            _scope = scope;
            _pipeClient = pipeClient;

            Log.Information("KoruMsSqlYedek Tray uygulaması başlatılıyor...");

            _animTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _animTimer.Tick += OnAnimTimerTick;

            _contextMenu = CreateContextMenu();
            _notifyIcon = CreateNotifyIcon();

            ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_BalloonRunning"), ToolTipIcon.Info, 2000);

            BackupActivityHub.ActivityChanged += OnBackupActivityChanged;
            _pipeClient.ConnectionChanged    += OnPipeConnectionChanged;

            // Başlangıçta bağlı değil — ikonu ayarla
            UpdateTrayStatus(TrayIconStatus.Disconnected, Res.Get("Tray_TooltipDisconnected"));

            // Servis pipe bağlantısını başlat (arka planda otomatik yeniden bağlanır)
            _pipeClient.Start();

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
            menu.Renderer = new Theme.VersionSidebarRenderer($"v{version}");

            var tsmDashboard = new ToolStripMenuItem(Res.Get("Tray_MenuDashboard"), null, (s, e) => OpenMainWindow(0));
            tsmDashboard.Font = new System.Drawing.Font(tsmDashboard.Font, System.Drawing.FontStyle.Bold);

            var tsmPlans = new ToolStripMenuItem(Res.Get("Tray_MenuPlans"), null, (s, e) => OpenMainWindow(1));
            var tsmManualBackup = new ToolStripMenuItem(Res.Get("Tray_MenuManualBackup"), null, (s, e) => OpenMainWindow(2));
            var tsmLog = new ToolStripMenuItem(Res.Get("Tray_MenuLog"), null, (s, e) => OpenMainWindow(3));
            var tsmSettings = new ToolStripMenuItem(Res.Get("Tray_MenuSettings"), null, (s, e) => OpenMainWindow(4));
            var tsmExit = new ToolStripMenuItem(Res.Get("Tray_MenuExit"), null, OnExitClick);

            _tsmServiceStatus  = new ToolStripMenuItem(Res.Get("Tray_ServiceStatusUnknown")) { Enabled = false };
            _tsmServiceStart   = new ToolStripMenuItem(Res.Get("Tray_ServiceStart"),   null, OnServiceStartClick);
            _tsmServiceStop    = new ToolStripMenuItem(Res.Get("Tray_ServiceStop"),    null, OnServiceStopClick);
            _tsmServiceRestart = new ToolStripMenuItem(Res.Get("Tray_ServiceRestart"), null, OnServiceRestartClick);

            menu.Items.Add(tsmDashboard);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmPlans);
            menu.Items.Add(tsmManualBackup);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_tsmServiceStatus);
            menu.Items.Add(_tsmServiceStart);
            menu.Items.Add(_tsmServiceStop);
            menu.Items.Add(_tsmServiceRestart);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmLog);
            menu.Items.Add(tsmSettings);
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
                using var sc = new ServiceController(ServiceName);
                var status = sc.Status;
                bool running = status == ServiceControllerStatus.Running;
                bool stopped = status == ServiceControllerStatus.Stopped;

                _tsmServiceStatus.Text = running
                    ? Res.Get("Tray_ServiceStatusRunning")
                    : stopped
                        ? Res.Get("Tray_ServiceStatusStopped")
                        : Res.Get("Tray_ServiceStatusUnknown");

                _tsmServiceStart.Enabled   = stopped;
                _tsmServiceStop.Enabled    = running;
                _tsmServiceRestart.Enabled = running;
            }
            catch
            {
                _tsmServiceStatus.Text     = Res.Get("Tray_ServiceStatusUnknown");
                _tsmServiceStart.Enabled   = false;
                _tsmServiceStop.Enabled    = false;
                _tsmServiceRestart.Enabled = false;
            }
        }

        private async void OnServiceStartClick(object sender, EventArgs e)
        {
            try
            {
                _tsmServiceStart.Enabled = false;
                await Task.Run(() =>
                {
                    using var sc = new ServiceController(ServiceName);
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                });
                ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_ServiceStarted"), ToolTipIcon.Info, 2500);
                Log.Information("Servis kullanıcı tarafından başlatıldı.");
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
                await Task.Run(() =>
                {
                    using var sc = new ServiceController(ServiceName);
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                });
                ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_ServiceStopped"), ToolTipIcon.Info, 2500);
                Log.Information("Servis kullanıcı tarafından durduruldu.");
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
                await Task.Run(() =>
                {
                    using var sc = new ServiceController(ServiceName);
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                });
                ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_ServiceRestarted"), ToolTipIcon.Info, 2500);
                Log.Information("Servis kullanıcı tarafından yeniden başlatıldı.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Servis yeniden başlatılamadı.");
                ShowBalloonTip(Res.Get("AppName"), Res.Format("Tray_ServiceActionError", ex.Message), ToolTipIcon.Error, 5000);
            }
        }

        #endregion

        #region Exit

        private void OnExitClick(object sender, EventArgs e)
        {
            Log.Information("Kullanıcı çıkış isteğinde bulundu.");

            var result = MessageBox.Show(
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
                try { NativeMethods.DestroyIcon(oldIcon.Handle); }
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

        private void StartTrayAnimation(string tooltipText)
        {
            if (_isAnimating) return;
            _animFrames = SymbolIconHelper.CreateAnimationFrames();
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

            // UpdateTrayStatus, _notifyIcon.Icon'u (= localFrames[lastIndex]) dispose eder
            UpdateTrayStatus(finalStatus, tooltipText);

            // Kalan kareleri temizle
            if (localFrames != null)
            {
                for (int i = 0; i < localFrames.Length; i++)
                {
                    if (i == lastIndex) continue; // UpdateTrayStatus zaten yok etti
                    try { NativeMethods.DestroyIcon(localFrames[i].Handle); } catch { }
                }
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
