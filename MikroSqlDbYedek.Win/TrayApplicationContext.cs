using System;
using System.Threading;
using System.Windows.Forms;
using Autofac;
using MikroSqlDbYedek.Win.Forms;
using MikroSqlDbYedek.Win.Helpers;
using Serilog;

namespace MikroSqlDbYedek.Win
{
    /// <summary>
    /// System Tray tabanlı uygulama bağlamı.
    /// NotifyIcon ile tray'de çalışır; menüden Dashboard, Planlar, Log, Ayarlar erişilir.
    /// </summary>
    internal class TrayApplicationContext : ApplicationContext
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<TrayApplicationContext>();

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ILifetimeScope _scope;
        private MainDashboardForm _dashboardForm;

        public TrayApplicationContext(ILifetimeScope scope)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            _scope = scope;

            Log.Information("MikroSqlDbYedek Tray uygulaması başlatılıyor...");

            _contextMenu = CreateContextMenu();
            _notifyIcon = CreateNotifyIcon();

            ShowBalloonTip(Res.Get("AppName"), Res.Get("Tray_BalloonRunning"), ToolTipIcon.Info, 2000);

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

            icon.DoubleClick += OnDashboardClick;

            return icon;
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var version = System.Reflection.Assembly
                .GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.17";
            menu.Renderer = new Theme.VersionSidebarRenderer($"v{version}");

            var tsmDashboard = new ToolStripMenuItem(Res.Get("Tray_MenuDashboard"), null, OnDashboardClick);
            tsmDashboard.Font = new System.Drawing.Font(tsmDashboard.Font, System.Drawing.FontStyle.Bold);

            var tsmPlans = new ToolStripMenuItem(Res.Get("Tray_MenuPlans"), null, OnPlansClick);
            var tsmLog = new ToolStripMenuItem(Res.Get("Tray_MenuLog"), null, OnLogClick);
            var tsmSettings = new ToolStripMenuItem(Res.Get("Tray_MenuSettings"), null, OnSettingsClick);
            var tsmManualBackup = new ToolStripMenuItem(Res.Get("Tray_MenuManualBackup"), null, OnManualBackupClick);
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

        #region Menu Event Handlers

        private void OnDashboardClick(object sender, EventArgs e)
        {
            ShowDashboard();
        }

        private PlanListForm _planListForm;

        private void OnPlansClick(object sender, EventArgs e)
        {
            if (_planListForm == null || _planListForm.IsDisposed)
            {
                _planListForm = _scope.Resolve<PlanListForm>();
                _planListForm.FormClosed += (s, args) => _planListForm = null;
            }

            if (_planListForm.Visible)
            {
                _planListForm.Activate();
            }
            else
            {
                _planListForm.Show();
            }
        }

        private LogViewerForm _logViewerForm;

        private void OnLogClick(object sender, EventArgs e)
        {
            if (_logViewerForm == null || _logViewerForm.IsDisposed)
            {
                _logViewerForm = _scope.Resolve<LogViewerForm>();
                _logViewerForm.FormClosed += (s, args) => _logViewerForm = null;
            }

            if (_logViewerForm.Visible)
            {
                _logViewerForm.Activate();
            }
            else
            {
                _logViewerForm.Show();
            }
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            using (var settingsForm = _scope.Resolve<SettingsForm>())
            {
                settingsForm.ShowDialog();
            }
        }

        private ManualBackupDialog _manualBackupDialog;

        private void OnManualBackupClick(object sender, EventArgs e)
        {
            if (_manualBackupDialog == null || _manualBackupDialog.IsDisposed)
            {
                _manualBackupDialog = _scope.Resolve<ManualBackupDialog>();
                _manualBackupDialog.FormClosed += (s, args) => _manualBackupDialog = null;
            }

            if (_manualBackupDialog.Visible)
            {
                _manualBackupDialog.Activate();
            }
            else
            {
                _manualBackupDialog.Show();
            }
        }

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

        #endregion

        #region Dashboard

        private void ShowDashboard()
        {
            if (_dashboardForm == null || _dashboardForm.IsDisposed)
            {
                _dashboardForm = _scope.Resolve<MainDashboardForm>();
                _dashboardForm.FormClosed += OnDashboardFormClosed;
            }

            if (_dashboardForm.Visible)
            {
                _dashboardForm.Activate();
            }
            else
            {
                _dashboardForm.Show();
            }
        }

        private void OnDashboardFormClosed(object sender, FormClosedEventArgs e)
        {
            _dashboardForm = null;
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

            // Eski ikonu serbest bırak (ancak sistem ikonlarını dispose etme)
            if (oldIcon != null)
            {
                try { NativeMethods.DestroyIcon(oldIcon.Handle); }
                catch { /* ikon zaten serbest bırakılmış olabilir */ }
            }
        }

        #endregion

        #region Cleanup

        private void ExitApplication()
        {
            Log.Information("Tray uygulaması kapatılıyor...");

            _notifyIcon.Visible = false;

            if (_dashboardForm != null && !_dashboardForm.IsDisposed)
            {
                _dashboardForm.Close();
                _dashboardForm = null;
            }

            if (_logViewerForm != null && !_logViewerForm.IsDisposed)
            {
                _logViewerForm.Close();
                _logViewerForm = null;
            }

            if (_manualBackupDialog != null && !_manualBackupDialog.IsDisposed)
            {
                _manualBackupDialog.Close();
                _manualBackupDialog = null;
            }

            _notifyIcon.Dispose();
            _contextMenu.Dispose();

            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();

                if (_dashboardForm != null && !_dashboardForm.IsDisposed)
                {
                    _dashboardForm.Close();
                }

                if (_logViewerForm != null && !_logViewerForm.IsDisposed)
                {
                    _logViewerForm.Close();
                }

                if (_manualBackupDialog != null && !_manualBackupDialog.IsDisposed)
                {
                    _manualBackupDialog.Close();
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
