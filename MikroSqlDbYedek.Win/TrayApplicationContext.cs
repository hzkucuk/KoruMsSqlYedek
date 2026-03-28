using System;
using System.Windows.Forms;
using Autofac;
using MikroSqlDbYedek.Win.Helpers;
using Serilog;

namespace MikroSqlDbYedek.Win
{
    /// <summary>
    /// System Tray tabanlı uygulama bağlamı.
    /// NotifyIcon ile tray'de çalışır; menüden tek MainWindow açılır, sekme seçimi ile yönlendirilir.
    /// </summary>
    internal class TrayApplicationContext : ApplicationContext
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<TrayApplicationContext>();

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ILifetimeScope _scope;
        private MainWindow _mainWindow;

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

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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
