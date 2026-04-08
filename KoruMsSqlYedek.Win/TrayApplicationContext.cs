#nullable enable
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
using KoruMsSqlYedek.Win.Forms;
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
    internal partial class TrayApplicationContext : ApplicationContext
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<TrayApplicationContext>();

        private const string ServiceName = "KoruMsSqlYedekService";

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ILifetimeScope _scope;
        private readonly ServicePipeClient _pipeClient;
        private MainWindow? _mainWindow;

        // Servis kontrol menü öğeleri
        private ToolStripMenuItem? _tsmServiceStatus;
        private ToolStripMenuItem? _tsmServiceStart;
        private ToolStripMenuItem? _tsmServiceStop;
        private ToolStripMenuItem? _tsmServiceRestart;

        // Yedekleme animasyon durumu
        private readonly System.Windows.Forms.Timer _animTimer;
        private Icon[]? _animFrames;
        private int _animFrameIndex;
        private bool _isAnimating;

        // Tamamlanma animasyonu — kısa süre gösterilir, sonra idle'a döner
        private readonly System.Windows.Forms.Timer _completionTimer;
        private Icon[]? _completionFrames;
        private int _completionFrameIndex;
        private bool _isCompletionAnimating;

        // Güncelleme kontrolü
        private readonly IUpdateService _updateService;
        private readonly System.Windows.Forms.Timer _updateTimer;
        private ToolStripMenuItem? _tsmCheckUpdate;
        private UpdateInfo? _pendingUpdate;

        // UI thread marshal desteği — tray app'te OpenForms boş olabilir
        private readonly SynchronizationContext _syncContext;
        private readonly int _uiThreadId;

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
            _uiThreadId = Thread.CurrentThread.ManagedThreadId;

            // Application.Run henüz çağrılmadığından SynchronizationContext.Current null olabilir.
            // Context'i oluştur + install et; Application.Run tekrar oluşturmasın.
            if (SynchronizationContext.Current is WindowsFormsSynchronizationContext ctx)
            {
                _syncContext = ctx;
            }
            else
            {
                var newCtx = new WindowsFormsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(newCtx);
                _syncContext = newCtx;
            }

            Log.Information("KoruMsSqlYedek Tray uygulaması başlatılıyor...");

            _animTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _animTimer.Tick += OnAnimTimerTick;

            _completionTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _completionTimer.Tick += OnCompletionTimerTick;

            _contextMenu = CreateContextMenu();
            _notifyIcon = CreateNotifyIcon();

            Theme.ModernToast.Show(Res.Get("AppName"), Res.Get("Tray_BalloonRunning"), Theme.ToastType.Info, 2000);

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
            var tsmAbout = new ToolStripMenuItem("Hakkında", null, OnAboutClick);
            menu.Items.Add(tsmAbout);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(tsmExit);

            menu.Opening += (s, e) => UpdateServiceMenuItems();

            return menu;
        }

        #endregion


        #region MainWindow

        private void OpenMainWindow(int tabIndex)
        {
            // Şifre koruması kontrolü
            var settingsManager = _scope.Resolve<IAppSettingsManager>();
            var settings = settingsManager.Load();

            if (settings.IsPasswordProtected)
            {
                using (var pwdDlg = new PasswordDialog(settings, settingsManager))
                {
                    if (pwdDlg.ShowDialog() != DialogResult.OK)
                    {
                        Log.Information("Şifre doğrulanmadığı için ana pencere açılmadı.");
                        return;
                    }
                }
            }

            if (_mainWindow == null || _mainWindow.IsDisposed)
            {
                _mainWindow = _scope.Resolve<MainWindow>();
                _mainWindow.FormClosed += (s, e) => _mainWindow = null;
            }

            _mainWindow.SelectTab(tabIndex);
        }

        #endregion


        #region About

        private void OnAboutClick(object? sender, EventArgs e)
        {
            using AboutForm aboutForm = new();
            aboutForm.ShowDialog();
        }

        #endregion


        #region Exit

        private void OnExitClick(object? sender, EventArgs e)
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
        internal void UpdateTrayStatus(TrayIconStatus status, string? tooltipText = null)
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
