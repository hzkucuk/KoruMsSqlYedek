#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Core;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.IPC;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Forms;
using KoruMsSqlYedek.Win.Helpers;
using KoruMsSqlYedek.Win.IPC;
using Serilog;

namespace KoruMsSqlYedek.Win
{
    /// <summary>
    /// Tek pencereli ana form. 4 sekme: Dashboard, Planlar, Loglar, Ayarlar.
    /// Tray ikonundan açılır; kapatıldığında gizlenir, uygulama kapanmaz.
    /// </summary>
    public partial class MainWindow : Theme.ModernFormBase
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MainWindow>();
        private const double BytesPerMb = 1048576.0;

        private readonly IPlanManager _planManager;
        private readonly IBackupHistoryManager _historyManager;
        private readonly ISqlBackupService _sqlBackupService;
        private readonly ICompressionService _compressionService;
        private readonly IAppSettingsManager _settingsManager;
        private readonly ServicePipeClient _pipeClient;

        // Timers
        private readonly System.Windows.Forms.Timer _dashboardTimer;
        private readonly System.Windows.Forms.Timer _logTimer;

        // Log viewer state
        private readonly string _logDirectory;
        private List<LogEntry> _allLogEntries = new List<LogEntry>();
        private List<LogEntry> _filteredLogEntries = new List<LogEntry>();
        private const int MaxLogEntries = 10000;
        private static readonly Regex LogLineRegex = new Regex(
            @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})[^\[]*\[(\w{3})\]\s+(.*)",
            RegexOptions.Compiled);

        // Settings state
        private AppSettings? _settings;

        // Backup state — per-plan tracking
        private readonly HashSet<string> _runningPlanIds = new HashSet<string>();
        private string? _viewingPlanId;

        // Per-plan grid progress (planId → yüzde 0-100)
        private readonly Dictionary<string, int> _planProgress = new Dictionary<string, int>();

        // Per-plan cumulative progress tracking
        private readonly Dictionary<string, PlanProgressTracker> _planProgressTracker = new Dictionary<string, PlanProgressTracker>();

        // PlanProgressTracker artık standalone sınıf: PlanProgressTracker.cs

        // Scheduler'dan gelen sonraki çalışma zamanları (planId → lokal saat metni)
        private readonly Dictionary<string, string> _nextFireTimes = new Dictionary<string, string>();

        // Plan listesi sıralama/filtreleme durumu
        private List<PlanRowData> _allPlanRows = new List<PlanRowData>();
        private int _planSortColumn = -1;
        private bool _planSortAscending = true;

        public MainWindow(
            IPlanManager planManager,
            IBackupHistoryManager historyManager,
            ISqlBackupService sqlBackupService,
            ICompressionService compressionService,
            IAppSettingsManager settingsManager,
            ServicePipeClient pipeClient)
        {
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));
            if (historyManager == null) throw new ArgumentNullException(nameof(historyManager));
            if (sqlBackupService == null) throw new ArgumentNullException(nameof(sqlBackupService));
            if (compressionService == null) throw new ArgumentNullException(nameof(compressionService));
            if (settingsManager == null) throw new ArgumentNullException(nameof(settingsManager));
            if (pipeClient == null) throw new ArgumentNullException(nameof(pipeClient));

            _planManager = planManager;
            _historyManager = historyManager;
            _sqlBackupService = sqlBackupService;
            _compressionService = compressionService;
            _settingsManager = settingsManager;
            _pipeClient = pipeClient;

            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KoruMsSqlYedek", "Logs");

            InitializeComponent();

            // Dashboard gruplanmış yedekleme listesi tema ayarları
            // (GroupedBackupListPanel kendi temasını otomatik uygular)

            // Versiyon metnini runtime'da ayarla (Designer'da statik metinlere izin verilmiyor)
            string ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.18";
            _tslVersion.Text = $"v{ver}";
            _tslVersion.IsLink = true;
            _tslVersion.LinkBehavior = LinkBehavior.HoverUnderline;
            _tslVersion.Click += OnVersionLabelClick;

            ApplyIcons();

            _dashboardTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _dashboardTimer.Tick += (s, e) =>
            {
                LoadDashboardData();
                if (_pipeClient.IsConnected)
                    _pipeClient.RequestStatusAsync().ConfigureAwait(false);
            };

            _logTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _logTimer.Tick += OnLogAutoRefreshTick;

            _tabControl.SelectedIndexChanged += OnTabChanged;
            _splitPlans.Resize += OnSplitPlansResize;

            // VirtualMode event — log grid
            _dgvLogs.CellValueNeeded += OnLogCellValueNeeded;
            _dgvLogs.CellFormatting += OnLogCellFormatting;

            BackupActivityHub.ActivityChanged   += OnBackupActivityChanged;
            _pipeClient.ConnectionChanged       += OnPipeConnectionChanged;
            ServiceStatusHub.StatusReceived     += OnServiceStatusReceived;

            // Başlangıçta bağlı değil — durum çubuğunu ayarla
            UpdateStatusBarConnection(false);
        }

        private void ApplyIcons()
        {
            this.Icon = Helpers.SymbolIconHelper.CreateTrayIcon();

            _btnStart.Image = LoadToolStripIcon("Apply_16x16.png");
            _btnStart.Text = Res.Get("Btn_StartBackup");
            _btnStart.TextImageRelation = TextImageRelation.ImageBeforeText;
            _btnStart.ImageAlign = ContentAlignment.MiddleLeft;

            _btnCancelBackup.Image = LoadToolStripIcon("Close_16x16.png");
            _btnCancelBackup.Text = Res.Get("Btn_Cancel");
            _btnCancelBackup.TextImageRelation = TextImageRelation.ImageBeforeText;

            // Context menu ikonları
            _ctxBackupNow.Image = LoadToolStripIcon("Apply_16x16.png");
            _ctxStopBackup.Image = LoadToolStripIcon("Close_16x16.png");
            _ctxEditPlan.Image = LoadToolStripIcon("Edit_16x16.png");
            _ctxDeletePlan.Image = LoadToolStripIcon("Delete_16x16.png");
            _ctxExportPlan.Image = LoadToolStripIcon("Export_16x16.png");
            _ctxViewPlanLogs.Image = LoadToolStripIcon("Article_16x16.png");

            _btnClearLogFilter.Image = LoadToolStripIcon("Clear_16x16.png");
            _btnClearLogFilter.Text = Res.Get("Btn_ClearFilter");
            _btnClearLogFilter.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnLogRefresh.Image = LoadToolStripIcon("Refresh_16x16.png");
            _btnLogRefresh.Text = Res.Get("Btn_Refresh");
            _btnLogRefresh.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnLogExport.Image = LoadToolStripIcon("Export_16x16.png");
            _btnLogExport.Text = Res.Get("Btn_Export");
            _btnLogExport.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnBrowseBackupPath.Image = LoadToolStripIcon("Open_16x16.png");
            _btnBrowseBackupPath.Text = "";
            _btnBrowseBackupPath.ImageAlign = ContentAlignment.MiddleCenter;

            _btnSmtpTest.Image = LoadToolStripIcon("Send_16x16.png");
            _btnSmtpTest.Text = Res.Get("Btn_SmtpTest");
            _btnSmtpTest.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnSaveSettings.Image = LoadToolStripIcon("Save_16x16.png");
            _btnSaveSettings.Text = Res.Get("Btn_Save");
            _btnSaveSettings.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnCancelSettings.Image = LoadToolStripIcon("Cancel_16x16.png");
            _btnCancelSettings.Text = Res.Get("Btn_CancelSettings");
            _btnCancelSettings.TextImageRelation = TextImageRelation.ImageBeforeText;

            // Dashboard KPI kart ikonları
            _lblStatusIcon.Image = LoadToolStripIcon("CheckBox_16x16.png");
            _lblNextIcon.Image = LoadToolStripIcon("Time_16x16.png");
            _lblPlansIcon.Image = LoadToolStripIcon("Database_16x16.png");

            // ToolStrip butonları — DevExpress ikonları
            ApplyToolStripIcons();
        }

        private static Image? LoadToolStripIcon(string name)
        {
            var asm = typeof(MainWindow).Assembly;
            string resourceName = $"KoruMsSqlYedek.Win.Resources.Icons.{name}";
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null) return null;
            return Image.FromStream(stream);
        }

        private void ApplyToolStripIcons()
        {
            _tsbNew.Image = LoadToolStripIcon("New_16x16.png");
            _tsbNew.Text = Res.Get("Tsb_NewPlan");
            _tsbNew.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _tsbNew.TextImageRelation = TextImageRelation.ImageBeforeText;

            _tsbEdit.Image = LoadToolStripIcon("Edit_16x16.png");
            _tsbEdit.Text = Res.Get("Tsb_Edit");
            _tsbEdit.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _tsbEdit.TextImageRelation = TextImageRelation.ImageBeforeText;

            _tsbDelete.Image = LoadToolStripIcon("Delete_16x16.png");
            _tsbDelete.Text = Res.Get("Tsb_Delete");
            _tsbDelete.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _tsbDelete.TextImageRelation = TextImageRelation.ImageBeforeText;

            _tsbExport.Image = LoadToolStripIcon("Export_16x16.png");
            _tsbExport.Text = Res.Get("Tsb_Export");
            _tsbExport.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _tsbExport.TextImageRelation = TextImageRelation.ImageBeforeText;

            _tsbImport.Image = LoadToolStripIcon("Import_16x16.png");
            _tsbImport.Text = Res.Get("Tsb_Import");
            _tsbImport.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _tsbImport.TextImageRelation = TextImageRelation.ImageBeforeText;

            _tsbRefreshPlans.Image = LoadToolStripIcon("Refresh_16x16.png");
            _tsbRefreshPlans.Text = Res.Get("Tsb_Refresh");
            _tsbRefreshPlans.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _tsbRefreshPlans.TextImageRelation = TextImageRelation.ImageBeforeText;

            _tslSearchLabel.Image = LoadToolStripIcon("Find_16x16.png");
            _tslSearchLabel.Text = Res.Get("Tsl_Search");
            _tslSearchLabel.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _tslSearchLabel.TextImageRelation = TextImageRelation.ImageBeforeText;
        }

        /// <summary>
        /// Belirtilen sekmeyi seçer ve pencereyi öne getirir.
        /// </summary>
        public void SelectTab(int index)
        {
            if (index >= 0 && index < _tabControl.TabCount)
                _tabControl.SelectedIndex = index;

            if (Visible)
                Activate();
            else
                Show();
        }

        /// <summary>Versiyon label'ına tıklanınca Hakkında diyalogunu açar.</summary>
        private void OnVersionLabelClick(object? sender, EventArgs e)
        {
            using Forms.AboutForm aboutForm = new();
            aboutForm.ShowDialog(this);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplySplitRatio();
            LoadDashboardData();
            _dashboardTimer.Start();

            // Yedek Türü combobox varsayılan seçimi (Full)
            if (_cmbBackupType.Items.Count > 0 && _cmbBackupType.SelectedIndex < 0)
                _cmbBackupType.SelectedIndex = 0;

            Log.Debug("MainWindow gösterildi.");
        }

        private void OnSplitPlansResize(object? sender, EventArgs e)
        {
            ApplySplitRatio();
        }

        /// <summary>Panel1 (grid) ~60%, Panel2 (manuel yedekleme) ~40% oranını korur.</summary>
        private void ApplySplitRatio()
        {
            const int panel1Min = 120;
            const int panel2Min = 200;

            int available = _splitPlans.Height - _splitPlans.SplitterWidth;
            if (available < panel1Min + panel2Min)
                return; // Form henüz stabil boyuta ulaşmamış

            int panel1 = (int)(available * 0.60);
            panel1 = Math.Max(panel1, panel1Min);
            panel1 = Math.Min(panel1, available - panel2Min);

            if (panel1 != _splitPlans.SplitterDistance)
                _splitPlans.SplitterDistance = panel1;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _dashboardTimer.Stop();
                _logTimer.Stop();
                Log.Debug("MainWindow gizlendi (tray'de çalışmaya devam ediyor).");
                return;
            }

            _dashboardTimer.Stop();
            _dashboardTimer.Dispose();
            _logTimer.Stop();
            _logTimer.Dispose();
            BackupActivityHub.ActivityChanged -= OnBackupActivityChanged;
            _pipeClient.ConnectionChanged    -= OnPipeConnectionChanged;
            ServiceStatusHub.StatusReceived  -= OnServiceStatusReceived;
            base.OnFormClosing(e);
        }

        private void OnTabChanged(object? sender, EventArgs e)
        {
            int idx = _tabControl.SelectedIndex;

            _dashboardTimer.Stop();
            _logTimer.Stop();

            switch (idx)
            {
                case 0: // Dashboard
                    LoadDashboardData();
                    _dashboardTimer.Start();
                    break;
                case 1: // Planlar + Yedekleme
                    RefreshPlanList();
                    // Servisden güncel sonraki çalışma zamanlarını iste
                    RequestNextFireTimesAsync();
                    break;
                case 2: // Loglar
                    if (_allLogEntries.Count == 0)
                    {
                        PopulateLogFiles();
                        PopulateLevelFilter();
                        PopulateLogPlanFilter();
                        LoadSelectedLogFile();
                    }
                    if (_chkAutoTail.Checked)
                        _logTimer.Start();
                    break;
                case 3: // Ayarlar
                    LoadSettings();
                    break;
            }
        }
    }
}
