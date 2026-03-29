using System;
using System.Windows.Forms;
using Autofac;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Win.Helpers;
using Serilog;

namespace KoruMsSqlYedek.Win
{
    /// <summary>
    /// System Tray tabanlı uygulama bağlamı.
    /// NotifyIcon ile tray'de çalışır; menüden tek MainWindow açılır, sekme seçimi ile yönlendirilir.
    /// Quartz scheduler'ı başlatır ve uygulama kapanırken durdurur.
    /// </summary>
    internal class TrayApplicationContext : ApplicationContext
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<TrayApplicationContext>();

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ILifetimeScope _scope;
        private readonly ISchedulerService _schedulerService;
        private readonly IPlanManager _planManager;
        private MainWindow _mainWindow;

        public TrayApplicationContext(
            ILifetimeScope scope,
            ISchedulerService schedulerService,
            IPlanManager planManager)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (schedulerService == null) throw new ArgumentNullException(nameof(schedulerService));
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));

            _scope = scope;
            _schedulerService = schedulerService;
            _planManager = planManager;

            Log.Information("KoruMsSqlYedek Tray uygulaması başlatılıyor...");

            _contextMenu = CreateContextMenu();
            _notifyIcon = CreateNotifyIcon();

            ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_BalloonRunning"), ToolTipIcon.Info, 2000);

            BackupActivityHub.ActivityChanged += OnBackupActivityChanged;

            // Scheduler'ı başlat ve tüm aktif planları zamanla
            _ = StartSchedulerAsync();

            Log.Information("Tray uygulaması başlatıldı.");
        }

        private async System.Threading.Tasks.Task StartSchedulerAsync()
        {
            try
            {
                await _schedulerService.StartAsync(System.Threading.CancellationToken.None);

                var plans = _planManager.GetAllPlans();
                int scheduled = 0;
                foreach (var plan in plans)
                {
                    if (plan.IsEnabled)
                    {
                        await _schedulerService.SchedulePlanAsync(plan, System.Threading.CancellationToken.None);
                        scheduled++;
                    }
                }

                Log.Information("Scheduler başlatıldı: {ScheduledCount}/{TotalCount} plan zamanlandı.",
                    scheduled, plans.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Scheduler başlatılamadı.");
            }
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

            menu.Items.Add(tsmDashboard);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmPlans);
            menu.Items.Add(tsmManualBackup);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmLog);
            menu.Items.Add(tsmSettings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmExit);

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

            // Scheduler'ı durdur
            if (_schedulerService.IsRunning)
            {
                try
                {
                    _schedulerService.StopAsync(System.Threading.CancellationToken.None)
                        .GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Scheduler durdurulurken hata.");
                }
            }

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

        private void OnBackupActivityChanged(object sender, BackupActivityEventArgs e)
        {
            switch (e.ActivityType)
            {
                case BackupActivityType.Started:
                    UpdateTrayStatus(TrayIconStatus.Running,
                        Res.Format("Tray_BackupRunning", e.PlanName));
                    ShowBalloonTip(
                        Res.Get("Toast_BackupStartedTitle"),
                        Res.Format("Toast_BackupStartedMessage", e.PlanName),
                        ToolTipIcon.Info);
                    break;

                case BackupActivityType.Completed:
                    UpdateTrayStatus(TrayIconStatus.Success,
                        Res.Format("Tray_BackupCompleted", e.PlanName));
                    ShowBalloonTip(
                        Res.Get("Toast_BackupCompletedTitle"),
                        Res.Format("Toast_BackupCompletedMessage", e.PlanName),
                        ToolTipIcon.Info);
                    break;

                case BackupActivityType.Failed:
                    UpdateTrayStatus(TrayIconStatus.Error,
                        Res.Format("Tray_BackupFailed", e.PlanName));
                    ShowBalloonTip(
                        Res.Get("Toast_BackupFailedTitle"),
                        Res.Format("Toast_BackupFailedMessage", e.PlanName),
                        ToolTipIcon.Error);
                    break;

                case BackupActivityType.Cancelled:
                    UpdateTrayStatus(TrayIconStatus.Idle,
                        Res.Get("Tray_Tooltip"));
                    ShowBalloonTip(
                        Res.Get("Toast_BackupCancelledTitle"),
                        Res.Format("Toast_BackupCancelledMessage", e.PlanName),
                        ToolTipIcon.Warning);
                    break;
            }
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BackupActivityHub.ActivityChanged -= OnBackupActivityChanged;
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
