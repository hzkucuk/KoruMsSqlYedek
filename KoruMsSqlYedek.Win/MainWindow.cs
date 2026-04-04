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
        private AppSettings _settings;

        // Backup state — per-plan tracking
        private readonly HashSet<string> _runningPlanIds = new HashSet<string>();
        private string _viewingPlanId;

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

        // Son yedekler ListView sıralama durumu
        private int _lvSortColumn = 0;
        private bool _lvSortAscending = false;

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
            const int sz = 18;
            _btnStart.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Play, Color.White, sz);
            _btnStart.Text = "Yedeklemeyi Baslat";
            _btnStart.TextImageRelation = TextImageRelation.ImageBeforeText;
            _btnStart.ImageAlign = ContentAlignment.MiddleLeft;

            _btnCancelBackup.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Stop, Color.White, sz);
            _btnCancelBackup.Text = "Iptal Et";
            _btnCancelBackup.TextImageRelation = TextImageRelation.ImageBeforeText;

            // Context menu ikonları
            _ctxBackupNow.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Play, Theme.ModernTheme.StatusSuccess, sz);
            _ctxStopBackup.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Stop, Theme.ModernTheme.StatusError, sz);
            _ctxEditPlan.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.PencilSimple, Theme.ModernTheme.TextPrimary, sz);
            _ctxDeletePlan.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Trash, Theme.ModernTheme.StatusError, sz);
            _ctxExportPlan.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Export, Theme.ModernTheme.TextPrimary, sz);
            _ctxViewPlanLogs.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.FileText, Theme.ModernTheme.AccentPrimary, sz);

            _btnCancelBackup.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Stop, Color.White, sz);
            _btnCancelBackup.Text = "Iptal Et";
            _btnCancelBackup.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnClearLogFilter.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Eraser, Theme.ModernTheme.TextSecondary, sz);
            _btnClearLogFilter.Text = "Temizle";
            _btnClearLogFilter.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnLogRefresh.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.ArrowClockwise, Color.White, sz);
            _btnLogRefresh.Text = "Yenile";
            _btnLogRefresh.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnLogExport.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Export, Theme.ModernTheme.TextSecondary, sz);
            _btnLogExport.Text = "Disa Aktar";
            _btnLogExport.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnBrowseBackupPath.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Folder, Theme.ModernTheme.AccentPrimary, 14);
            _btnBrowseBackupPath.Text = "";
            _btnBrowseBackupPath.ImageAlign = ContentAlignment.MiddleCenter;

            _btnSmtpTest.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Envelope, Color.White, sz);
            _btnSmtpTest.Text = "Test E-postasi Gonder";
            _btnSmtpTest.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnSaveSettings.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.FloppyDisk, Color.White, sz);
            _btnSaveSettings.Text = "Kaydet";
            _btnSaveSettings.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnCancelSettings.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.XCircle, Color.White, sz);
            _btnCancelSettings.Text = "Iptal";
            _btnCancelSettings.TextImageRelation = TextImageRelation.ImageBeforeText;

            // Dashboard KPI kart ikonları (Phosphor)
            _lblStatusIcon.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.CheckCircle, Theme.ModernTheme.StatusSuccess, 24);
            _lblNextIcon.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Clock, Theme.ModernTheme.AccentPrimary, 24);
            _lblPlansIcon.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Database, Theme.ModernTheme.StatusWarning, 24);

            // ToolStrip butonları — Phosphor ikonları
            ApplyToolStripIcons(sz);
        }

        private void ApplyToolStripIcons(int sz)
        {
            var col = Theme.ModernTheme.TextPrimary;
            _tsbNew.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.PlusCircle, col, sz);
            _tsbNew.Text = "Yeni Görev";
            _tsbNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            _tsbNew.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _tsbEdit.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.PencilSimple, col, sz);
            _tsbEdit.Text = "Düzenle";
            _tsbEdit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            _tsbEdit.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _tsbDelete.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Trash, Theme.ModernTheme.StatusError, sz);
            _tsbDelete.Text = "Sil";
            _tsbDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            _tsbDelete.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _tsbExport.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Export, col, sz);
            _tsbExport.Text = "Dışa Aktar";
            _tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            _tsbExport.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _tsbImport.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Download, col, sz);
            _tsbImport.Text = "İçe Aktar";
            _tsbImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            _tsbImport.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _tsbRefreshPlans.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.ArrowClockwise, col, sz);
            _tsbRefreshPlans.Text = "Yenile";
            _tsbRefreshPlans.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            _tsbRefreshPlans.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _tsbPassword.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            _tsbPassword.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            UpdatePasswordButtonIcon();
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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplySplitRatio();
            LoadDashboardData();
            _dashboardTimer.Start();
            Log.Debug("MainWindow gösterildi.");
        }

        private void OnSplitPlansResize(object sender, EventArgs e)
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

        private void OnTabChanged(object sender, EventArgs e)
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

        #region ── TAB 0: Dashboard ─────────────────────────────────────────

        private void LoadDashboardData()
        {
            try
            {
                LoadStatusSummary();
                LoadRecentBackups();
                _tslStatus.Text = Res.Format("Dashboard_LastUpdate", DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dashboard verileri yüklenirken hata oluştu.");
                _tslStatus.Text = Res.Get("Dashboard_DataLoadError");
            }
        }

        private void LoadStatusSummary()
        {
            var plans = _planManager.GetAllPlans();
            int activePlanCount = plans.Count(p => p.IsEnabled);
            _lblActivePlansValue.Text = activePlanCount.ToString();

            var recentHistory = _historyManager.GetRecentHistory(1);
            if (recentHistory.Count > 0)
            {
                UpdateLastBackupStatus(recentHistory[0]);
            }
            else
            {
                _lblStatusValue.Text = Res.Get("Dashboard_NoBackupYet");
                _lblStatusValue.ForeColor = SystemColors.GrayText;
                _lblNextBackupValue.Text = "—";
            }
        }

        private void UpdateLastBackupStatus(BackupResult last)
        {
            switch (last.Status)
            {
                case BackupResultStatus.Success:
                    _lblStatusValue.Text = Res.Get("Dashboard_StatusSuccess");
                    _lblStatusValue.ForeColor = Color.LimeGreen;
                    break;
                case BackupResultStatus.PartialSuccess:
                    _lblStatusValue.Text = Res.Get("Dashboard_StatusPartial");
                    _lblStatusValue.ForeColor = Color.Orange;
                    break;
                case BackupResultStatus.Failed:
                    _lblStatusValue.Text = Res.Get("Dashboard_StatusFailed");
                    _lblStatusValue.ForeColor = Color.Red;
                    break;
                case BackupResultStatus.Cancelled:
                    _lblStatusValue.Text = Res.Get("Dashboard_StatusCancelled");
                    _lblStatusValue.ForeColor = SystemColors.GrayText;
                    break;
            }

            if (last.CompletedAt.HasValue)
            {
                var ago = DateTime.Now - last.CompletedAt.Value;
                _lblNextBackupValue.Text = FormatTimeAgo(ago) + " " + Res.Get("Dashboard_TimeAgo") + " — " + last.PlanName;
            }
        }

        private void LoadRecentBackups()
        {
            var history = _historyManager.GetRecentHistory(50);
            _lvLastBackups.Items.Clear();

            foreach (var result in history)
            {
                var item = new ListViewItem(result.StartedAt.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(result.PlanName ?? "—");
                item.SubItems.Add(result.DatabaseName ?? "—");
                item.SubItems.Add(GetBackupTypeName(result.BackupType));
                item.SubItems.Add(GetStatusName(result.Status));
                item.SubItems.Add(FormatFileSize(result.CompressedSizeBytes > 0
                    ? result.CompressedSizeBytes
                    : result.FileSizeBytes));
                item.Tag = result;

                switch (result.Status)
                {
                    case BackupResultStatus.Failed:
                        item.ForeColor = Color.Red;
                        break;
                    case BackupResultStatus.PartialSuccess:
                        item.ForeColor = Color.Orange;
                        break;
                    case BackupResultStatus.Cancelled:
                        item.ForeColor = SystemColors.GrayText;
                        break;
                }

                _lvLastBackups.Items.Add(item);
            }

            _lvLastBackups.ListViewItemSorter = new LastBackupsItemComparer(_lvSortColumn, _lvSortAscending);
            AutoResizeListViewColumns(_lvLastBackups);
        }

        private static string GetBackupTypeName(SqlBackupType type)
        {
            switch (type)
            {
                case SqlBackupType.Full: return Res.Get("Dashboard_TypeFull");
                case SqlBackupType.Differential: return Res.Get("Dashboard_TypeDiff");
                case SqlBackupType.Incremental: return Res.Get("Dashboard_TypeInc");
                default: return type.ToString();
            }
        }

        private static string GetStatusName(BackupResultStatus status)
        {
            switch (status)
            {
                case BackupResultStatus.Success: return Res.Get("Dashboard_ResultSuccess");
                case BackupResultStatus.PartialSuccess: return Res.Get("Dashboard_ResultPartial");
                case BackupResultStatus.Failed: return Res.Get("Dashboard_ResultFailed");
                case BackupResultStatus.Cancelled: return Res.Get("Dashboard_ResultCancelled");
                default: return status.ToString();
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }

        private static string FormatEta(long bytesRemaining, long speedBytesPerSecond)
        {
            if (speedBytesPerSecond <= 0 || bytesRemaining <= 0) return string.Empty;
            var eta = TimeSpan.FromSeconds(bytesRemaining / (double)speedBytesPerSecond);
            if (eta.TotalSeconds < 60) return $"{(int)eta.TotalSeconds} sn";
            if (eta.TotalMinutes < 60) return $"{(int)eta.TotalMinutes} dk {eta.Seconds} sn";
            return $"{(int)eta.TotalHours} sa {eta.Minutes} dk";
        }

        private static string FormatTimeAgo(TimeSpan span)
        {
            if (span.TotalMinutes < 1) return Res.Get("Dashboard_TimeJustNow");
            if (span.TotalMinutes < 60) return Res.Format("Dashboard_TimeMinFormat", (int)span.TotalMinutes);
            if (span.TotalHours < 24) return Res.Format("Dashboard_TimeHourFormat", (int)span.TotalHours);
            return Res.Format("Dashboard_TimeDayFormat", (int)span.TotalDays);
        }

        private void OnListViewDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using var bgBrush = new SolidBrush(Theme.ModernTheme.GridHeaderBack);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            using var borderPen = new Pen(Theme.ModernTheme.DividerColor);
            e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            bool isSorted = e.ColumnIndex == _lvSortColumn;
            int arrowAreaWidth = isSorted ? 18 : 0;
            var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16 - arrowAreaWidth, e.Bounds.Height);
            using var sf = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            using var textBrush = new SolidBrush(Theme.ModernTheme.GridHeaderText);
            e.Graphics.DrawString(e.Header.Text, Theme.ModernTheme.FontCaptionBold, textBrush, textRect, sf);

            if (isSorted)
            {
                int ax = e.Bounds.Right - 14;
                int ay = e.Bounds.Y + e.Bounds.Height / 2;
                using var arrowBrush = new SolidBrush(Theme.ModernTheme.AccentPrimary);
                Point[] arrow = _lvSortAscending
                    ? new[] { new Point(ax, ay + 3), new Point(ax + 7, ay + 3), new Point(ax + 3, ay - 3) }
                    : new[] { new Point(ax, ay - 3), new Point(ax + 7, ay - 3), new Point(ax + 3, ay + 3) };
                e.Graphics.FillPolygon(arrowBrush, arrow);
            }
        }

        private void OnListViewDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void OnListViewDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void OnLastBackupsColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (_lvSortColumn == e.Column)
                _lvSortAscending = !_lvSortAscending;
            else
            {
                _lvSortColumn = e.Column;
                _lvSortAscending = true;
            }

            _lvLastBackups.ListViewItemSorter = new LastBackupsItemComparer(_lvSortColumn, _lvSortAscending);
            _lvLastBackups.Invalidate();
        }

        private static void AutoResizeListViewColumns(ListView lv)
        {
            for (int i = 0; i < lv.Columns.Count; i++)
            {
                int maxWidth = TextRenderer.MeasureText(lv.Columns[i].Text, Theme.ModernTheme.FontCaptionBold).Width + 28;
                foreach (ListViewItem item in lv.Items)
                {
                    if (i < item.SubItems.Count)
                    {
                        int w = TextRenderer.MeasureText(item.SubItems[i].Text, Theme.ModernTheme.FontBody).Width + 20;
                        if (w > maxWidth) maxWidth = w;
                    }
                }
                lv.Columns[i].Width = maxWidth;
            }
        }

        private sealed class LastBackupsItemComparer : System.Collections.IComparer
        {
            private readonly int _col;
            private readonly bool _asc;

            public LastBackupsItemComparer(int column, bool ascending)
            {
                _col = column;
                _asc = ascending;
            }

            public int Compare(object x, object y)
            {
                var ix = (ListViewItem)x;
                var iy = (ListViewItem)y;
                var rx = ix.Tag as BackupResult;
                var ry = iy.Tag as BackupResult;
                int result = CompareItems(ix, iy, rx, ry);
                return _asc ? result : -result;
            }

            private int CompareItems(ListViewItem ix, ListViewItem iy, BackupResult rx, BackupResult ry)
            {
                switch (_col)
                {
                    case 0: // Tarih
                        if (rx != null && ry != null)
                            return DateTime.Compare(rx.StartedAt, ry.StartedAt);
                        break;
                    case 4: // Sonuç (enum sırası: Success < PartialSuccess < Failed < Cancelled)
                        if (rx != null && ry != null)
                            return rx.Status.CompareTo(ry.Status);
                        break;
                    case 5: // Boyut (bayt cinsinden)
                        if (rx != null && ry != null)
                        {
                            long bx = rx.CompressedSizeBytes > 0 ? rx.CompressedSizeBytes : rx.FileSizeBytes;
                            long by = ry.CompressedSizeBytes > 0 ? ry.CompressedSizeBytes : ry.FileSizeBytes;
                            return bx.CompareTo(by);
                        }
                        break;
                }
                string tx = _col < ix.SubItems.Count ? ix.SubItems[_col].Text : string.Empty;
                string ty = _col < iy.SubItems.Count ? iy.SubItems[_col].Text : string.Empty;
                return string.Compare(tx, ty, StringComparison.CurrentCultureIgnoreCase);
            }
        }

        #endregion

        #region ── TAB 1: Planlar + Yedekleme ─────────────────────────────

        private void RefreshPlanList()
        {
            try
            {
                var plans = _planManager.GetAllPlans();
                _allPlanRows = new List<PlanRowData>(plans.Count);

                foreach (var plan in plans)
                {
                    var dbList = plan.Databases != null && plan.Databases.Count > 0
                        ? string.Join(", ", plan.Databases)
                        : "—";

                    var strategy = GetStrategyDisplayName(plan.Strategy?.Type ?? BackupStrategyType.Full);
                    var schedule = CronDisplayHelper.ToReadableText(plan.Strategy?.FullSchedule);
                    var cloudCount = plan.CloudTargets?.Count(t => t.IsEnabled) ?? 0;
                    string storageLabel = cloudCount > 0
                        ? $"\u2601 Bulut ({cloudCount})"
                        : "\U0001f4be Yerel";

                    string statusText = Res.Get("PlanStatus_Ready");
                    Color statusColor = Theme.ModernTheme.TextSecondary;
                    DateTime? lastRunAt = null;
                    bool lastBackupFailed = false;

                    try
                    {
                        var lastResult = _historyManager.GetHistoryByPlan(plan.PlanId, 1).FirstOrDefault();
                        if (lastResult != null)
                        {
                            lastRunAt = lastResult.StartedAt;
                            string icon;
                            switch (lastResult.Status)
                            {
                                case BackupResultStatus.Success:
                                    icon = "✓";
                                    statusColor = Theme.ModernTheme.StatusSuccess;
                                    break;
                                case BackupResultStatus.PartialSuccess:
                                    icon = "⚠";
                                    statusColor = Theme.ModernTheme.StatusWarning;
                                    break;
                                case BackupResultStatus.Failed:
                                    icon = "✕";
                                    statusColor = Theme.ModernTheme.StatusError;
                                    lastBackupFailed = true;
                                    break;
                                case BackupResultStatus.Cancelled:
                                    icon = "■";
                                    statusColor = Color.Gray;
                                    break;
                                default:
                                    icon = "";
                                    break;
                            }
                            statusText = lastResult.StartedAt.ToString("dd.MM.yyyy HH:mm") + " " + icon;
                        }
                    }
                    catch { /* history okunamazsa varsayılan "Hazır" kalır */ }

                    _allPlanRows.Add(new PlanRowData
                    {
                        Plan = plan,
                        DbList = dbList,
                        Strategy = strategy,
                        Schedule = schedule,
                        Storage = storageLabel,
                        StatusText = statusText,
                        StatusColor = statusColor,
                        LastRunAt = lastRunAt,
                        LastBackupFailed = lastBackupFailed
                    });
                }

                ApplyPlanFilter();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Plan listesi yüklenirken hata oluştu.");
                Theme.ModernMessageBox.Show(Res.Format("PlanList_LoadError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetStrategyDisplayName(BackupStrategyType type)
        {
            switch (type)
            {
                case BackupStrategyType.Full: return Res.Get("PlanList_StratFull");
                case BackupStrategyType.FullPlusDifferential: return Res.Get("PlanList_StratFullDiff");
                case BackupStrategyType.FullPlusDifferentialPlusIncremental: return Res.Get("PlanList_StratFullDiffInc");
                default: return type.ToString();
            }
        }

        /// <summary>Mevcut arama metni ve sıralama durumuna göre grid satırlarını filtreler ve yeniden doldurur.</summary>
        private void ApplyPlanFilter()
        {
            string search = _tstSearch?.Text?.Trim() ?? string.Empty;

            IEnumerable<PlanRowData> rows = _allPlanRows;

            if (!string.IsNullOrEmpty(search))
            {
                rows = rows.Where(r =>
                    (r.Plan.PlanName ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.DbList.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.Strategy.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.Storage.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (_planSortColumn >= 0)
            {
                Func<PlanRowData, IComparable> key;
                switch (_planSortColumn)
                {
                    case 0:  key = r => (IComparable)(r.Plan.IsEnabled ? 0 : 1); break;
                    case 1:  key = r => (IComparable)(r.Plan.PlanName ?? string.Empty); break;
                    case 2:  key = r => (IComparable)r.Strategy; break;
                    case 3:  key = r => (IComparable)r.DbList; break;
                    case 4:  key = r => (IComparable)r.Schedule; break;
                    case 5:  key = r => (IComparable)r.Storage; break;
                    case 6:  key = r => (IComparable)r.Plan.CreatedAt; break;
                    case 7:  key = r => (IComparable)(r.LastRunAt ?? DateTime.MinValue); break;
                    default: key = null; break;
                }

                if (key != null)
                    rows = _planSortAscending ? rows.OrderBy(key) : rows.OrderByDescending(key);
            }

            var sorted = rows.ToList();
            _dgvPlans.SuspendLayout();
            _dgvPlans.Rows.Clear();

            foreach (var row in sorted)
            {
                var plan = row.Plan;
                var rowIndex = _dgvPlans.Rows.Add(
                    plan.IsEnabled,
                    plan.PlanName ?? Res.Get("PlanList_Unnamed"),
                    row.Strategy,
                    row.DbList,
                    row.Schedule,
                    row.Storage,
                    plan.CreatedAt.ToString("dd.MM.yyyy"),
                    row.StatusText,
                    _planProgress.TryGetValue(plan.PlanId, out int pct) ? pct : 0,
                    _nextFireTimes.TryGetValue(plan.PlanId, out string nft) ? nft : "—");

                _dgvPlans.Rows[rowIndex].Tag = plan;
                _dgvPlans.Rows[rowIndex].Cells[_colStatus.Index].Style.ForeColor = row.StatusColor;

                if (row.LastBackupFailed)
                {
                    _dgvPlans.Rows[rowIndex].DefaultCellStyle.BackColor = Theme.ModernTheme.GridErrorRow;
                    _dgvPlans.Rows[rowIndex].DefaultCellStyle.ForeColor = Theme.ModernTheme.TextPrimary;
                }
            }

            _dgvPlans.ResumeLayout(false);

            _tslPlanCount.Text = search.Length > 0
                ? $"{sorted.Count} / {_allPlanRows.Count} görev"
                : Res.Format("PlanList_TotalFormat", _allPlanRows.Count);

            UpdateBackupButtonStates();
        }

        private void OnPlanGridColumnHeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex == _colProgress.Index)
                return;

            if (_planSortColumn == e.ColumnIndex)
                _planSortAscending = !_planSortAscending;
            else
            {
                _planSortColumn = e.ColumnIndex;
                _planSortAscending = true;
            }

            foreach (DataGridViewColumn col in _dgvPlans.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;

            _dgvPlans.Columns[_planSortColumn].HeaderCell.SortGlyphDirection =
                _planSortAscending ? SortOrder.Ascending : SortOrder.Descending;

            ApplyPlanFilter();
        }

        private void OnPlanSearchTextChanged(object sender, EventArgs e)
        {
            ApplyPlanFilter();
        }

        /// <summary>Plan listesi görüntüleme için önceden hesaplanmış satır verisi.</summary>
        private sealed class PlanRowData
        {
            public BackupPlan Plan;
            public string DbList;
            public string Strategy;
            public string Schedule;
            public string Storage;
            public string StatusText;
            public Color StatusColor;
            public DateTime? LastRunAt;
            public bool LastBackupFailed;
        }

        /// <summary>
        /// Şifre koruması etkinse doğrulama dialogu gösterir.
        /// Plan bazlı şifre tanımlıysa yalnızca plan şifresini kabul eder (izolasyon).
        /// Plan şifresi yoksa global (master) şifreyi kontrol eder.
        /// Hiçbir şifre tanımlı değilse true döner.
        /// </summary>
        /// <param name="plan">Düzenlenecek/silinecek plan. Yeni plan için null.</param>
        private bool CheckPlanPassword(BackupPlan plan = null)
        {
            bool hasMaster = _settings != null && _settings.IsPasswordProtected;
            bool hasPlanPw = plan != null && plan.HasPlanPassword;

            if (!hasMaster && !hasPlanPw)
                return true;

            string planHash = hasPlanPw ? plan.PasswordHash : null;
            string recoveryHash = hasPlanPw ? plan.RecoveryPasswordHash : null;

            using (var dlg = new PasswordDialog(_settings ?? new AppSettings(), _settingsManager, planHash, recoveryHash))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;

                // Güvenlik sorusu ile kurtarma yapıldıysa plan şifresini de sıfırla
                if (dlg.PlanPasswordReset && plan != null)
                {
                    plan.PasswordHash = null;
                    plan.RecoveryPasswordHash = null;
                    _planManager.SavePlan(plan);
                }

                return true;
            }
        }

        /// <summary>Şifre koruması ayarları dialogunu açar.</summary>
        private void OnPasswordSetupClick(object sender, EventArgs e)
        {
            // _settings henüz yüklenmediyse yükle
            _settings ??= _settingsManager.Load();

            // Mevcut şifre varsa önce doğrula
            if (_settings.HasPassword)
            {
                if (!CheckPlanPassword()) return;
            }

            using (var dlg = new PasswordSetupDialog(_settings, _settingsManager))
            {
                dlg.ShowDialog(this);
            }

            // Ayarları yeniden yükle (şifre değişmiş olabilir)
            _settings = _settingsManager.Load();
            UpdatePasswordButtonIcon();
        }

        /// <summary>Şifre korumasını aktif/pasif yapar.</summary>
        private void OnPasswordToggleClick(object sender, EventArgs e)
        {
            _settings ??= _settingsManager.Load();

            if (!_settings.HasPassword)
            {
                Theme.ModernMessageBox.Show(
                    "Önce bir şifre tanımlamanız gerekiyor.",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Pasif yapılacaksa şifre doğrulama iste
            if (_settings.PasswordEnabled)
            {
                if (!CheckPlanPassword()) return;

                _settings.PasswordEnabled = false;
                _settingsManager.Save(_settings);

                Theme.ModernMessageBox.Show(
                    "Şifre koruması pasif yapıldı.\nGörev işlemleri şifresiz yapılabilir.",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Aktif yapılacaksa da şifreyi doğrula
                using (var dlg = new PasswordDialog(_settings, _settingsManager))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                }

                _settings.PasswordEnabled = true;
                _settingsManager.Save(_settings);

                Theme.ModernMessageBox.Show(
                    "Şifre koruması aktif yapıldı.\nGörev işlemleri artık şifre ile korunuyor.",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            UpdatePasswordButtonIcon();
        }

        /// <summary>Şifre buton ikonunu duruma göre günceller (3 durum: yok/aktif/pasif).</summary>
        private void UpdatePasswordButtonIcon()
        {
            bool hasPassword = _settings != null && _settings.HasPassword;
            bool isActive = _settings != null && _settings.IsPasswordProtected;

            char icon;
            Color color;
            string tooltip;

            if (!hasPassword)
            {
                icon = Theme.PhosphorIcons.ShieldCheck;
                color = Theme.ModernTheme.TextSecondary;
                tooltip = "Şifre Koruması Ayarla";
            }
            else if (isActive)
            {
                icon = Theme.PhosphorIcons.ShieldCheck;
                color = Theme.ModernTheme.StatusSuccess;
                tooltip = "Şifre Koruması Aktif";
            }
            else
            {
                icon = Theme.PhosphorIcons.ShieldSlash;
                color = Theme.ModernTheme.StatusWarning;
                tooltip = "Şifre Koruması Pasif";
            }

            _tsbPassword.Image = Theme.PhosphorIcons.Render(icon, color, 18);
            _tsbPassword.ToolTipText = tooltip;

            // Dropdown menü metnini güncelle
            if (_tsmiPasswordToggle != null)
            {
                _tsmiPasswordToggle.Text = isActive
                    ? "🔓 Şifre Korumasını Pasif Yap"
                    : "🔒 Şifre Korumasını Aktif Yap";
                _tsmiPasswordToggle.Enabled = hasPassword;
            }
        }

        private async void OnNewPlanClick(object sender, EventArgs e)
        {
            if (!CheckPlanPassword()) return;

            using (var form = new PlanEditForm(_planManager, _sqlBackupService, _settingsManager))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshPlanList();
                }
            }
        }

        private async void OnEditPlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            if (!CheckPlanPassword(plan)) return;

            using (var form = new PlanEditForm(_planManager, _sqlBackupService, _settingsManager, plan))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshPlanList();
                }
            }
        }

        private async void OnDeletePlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            if (!CheckPlanPassword(plan)) return;

            var result = Theme.ModernMessageBox.Show(
                Res.Format("PlanList_DeleteConfirm", plan.PlanName),
                Res.Get("PlanList_DeleteTitle"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _planManager.DeletePlan(plan.PlanId);
                    Log.Information("Plan silindi: {PlanName} ({PlanId})", plan.PlanName, plan.PlanId);
                    RefreshPlanList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plan silinirken hata: {PlanId}", plan.PlanId);
                    Theme.ModernMessageBox.Show(Res.Format("PlanList_DeleteError", ex.Message),
                        Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnExportPlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = Res.Get("PlanList_ExportDialogTitle");
                sfd.Filter = Res.Get("PlanList_ExportFilter");
                sfd.FileName = $"{plan.PlanName ?? "plan"}.json";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _planManager.ExportPlan(plan.PlanId, sfd.FileName);
                        Theme.ModernMessageBox.Show(Res.Get("PlanList_ExportSuccess"), Res.Get("Info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plan export hatası: {PlanId}", plan.PlanId);
                        Theme.ModernMessageBox.Show(Res.Format("PlanList_ExportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void OnImportPlanClick(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = Res.Get("PlanList_ImportDialogTitle");
                ofd.Filter = Res.Get("PlanList_ExportFilter");

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var plan = _planManager.ImportPlan(ofd.FileName);
                        Log.Information("Plan içe aktarıldı: {PlanName} ({PlanId})", plan.PlanName, plan.PlanId);
                        RefreshPlanList();
                        Theme.ModernMessageBox.Show(Res.Format("PlanList_ImportSuccess", plan.PlanName), Res.Get("Info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plan import hatası: {FilePath}", ofd.FileName);
                        Theme.ModernMessageBox.Show(Res.Format("PlanList_ImportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnRefreshPlansClick(object sender, EventArgs e)
        {
            RefreshPlanList();
        }

        private BackupPlan GetSelectedPlan()
        {
            var plan = GetSelectedPlanSilent();
            if (plan == null)
                Theme.ModernMessageBox.Show(Res.Get("ManualBackup_PleaseSelectPlan"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return plan;
        }

        private BackupPlan GetSelectedPlanSilent()
        {
            if (_dgvPlans.SelectedRows.Count == 0)
                return null;
            return _dgvPlans.SelectedRows[0].Tag as BackupPlan;
        }

        private void OnContextMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var plan = GetSelectedPlanSilent();
            bool hasPlan = plan != null;
            bool running = hasPlan && _runningPlanIds.Contains(plan.PlanId);

            _ctxBackupNow.Enabled = hasPlan && !running && _pipeClient.IsConnected;
            _ctxStopBackup.Enabled = running && _pipeClient.IsConnected;
            _ctxEditPlan.Enabled = hasPlan && !running;
            _ctxDeletePlan.Enabled = hasPlan && !running;
            _ctxExportPlan.Enabled = hasPlan;
            _ctxViewPlanLogs.Enabled = hasPlan;
        }

        private void OnCtxBackupNowClick(object sender, EventArgs e) => OnStartBackupClick(sender, e);

        private void OnCtxStopBackupClick(object sender, EventArgs e) => OnCancelBackupClick(sender, e);

        private void OnCtxViewPlanLogsClick(object sender, EventArgs e)
        {
            if (GetSelectedPlanSilent() == null) return;
            _tabControl.SelectedIndex = 2;
        }

        private void OnCtxRestoreClick(object sender, EventArgs e)
        {
            BackupPlan plan = GetSelectedPlanSilent();
            if (plan == null) return;

            using RestoreDialog dlg = new RestoreDialog(plan, _historyManager, _sqlBackupService, _compressionService);
            dlg.ShowDialog(this);
        }

        private void OnPlanGridDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OnEditPlanClick(sender, e);
        }

        private void OnPlanGridSelectionChanged(object sender, EventArgs e)
        {
            UpdateBackupButtonStates();

            var selected = GetSelectedPlanSilent();
            if (selected == null) return;

            // Seçilen plana ait renkli log buffer'ını göster
            _txtBackupLog.Clear();
            if (_planLogs.TryGetValue(selected.PlanId, out var logs) && logs.Count > 0)
            {
                _txtBackupLog.SuspendLayout();
                foreach (var (text, color) in logs)
                {
                    _txtBackupLog.SelectionStart = _txtBackupLog.TextLength;
                    _txtBackupLog.SelectionLength = 0;
                    _txtBackupLog.SelectionColor = color;
                    _txtBackupLog.AppendText(text + Environment.NewLine);
                }
                _txtBackupLog.SelectionColor = Theme.ModernTheme.LogDefault;
                _txtBackupLog.SelectionStart = _txtBackupLog.TextLength;
                _txtBackupLog.ScrollToCaret();
                _txtBackupLog.ResumeLayout();
            }
        }

        #endregion

        #region ── Manuel Yedekleme ────────────────────────────────────────────

        private async void OnStartBackupClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            if (!_pipeClient.IsConnected)
            {
                Theme.ModernMessageBox.Show(Res.Get("Backup_ServiceNotConnected"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_runningPlanIds.Contains(plan.PlanId))
                return;

            _runningPlanIds.Add(plan.PlanId);
            _viewingPlanId = plan.PlanId;
            UpdateBackupButtonStates();

            // Bu plan için önceki log buffer'ını temizle
            _planLogs.Remove(plan.PlanId);
            _planProgress.Remove(plan.PlanId);
            _txtBackupLog.Clear();
            AppendBackupLog(plan.PlanId, string.Format("[{0}] {1}", plan.PlanName, Res.Get("ManualBackup_Starting")), Theme.ModernTheme.LogStarted);

            try
            {
                await _pipeClient.SendManualBackupCommandAsync(plan.PlanId, "Full");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Manuel yedekleme komutu gönderilemedi: {PlanId}", plan.PlanId);
                AppendBackupLog(plan.PlanId, Res.Format("Backup_SendError", ex.Message), Theme.ModernTheme.LogError);
                _runningPlanIds.Remove(plan.PlanId);
                UpdateBackupButtonStates();
            }
        }

        private async void OnCancelBackupClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlanSilent();
            string targetPlanId = plan?.PlanId ?? _viewingPlanId;
            if (string.IsNullOrEmpty(targetPlanId) || !_runningPlanIds.Contains(targetPlanId))
                return;

            try
            {
                await _pipeClient.SendCancelCommandAsync(targetPlanId);
                AppendBackupLog(targetPlanId, Res.Get("ManualBackup_Cancelling"), Theme.ModernTheme.LogWarning);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "İptal komutu gönderilemedi: {PlanId}", targetPlanId);
            }
        }

        private void UpdateBackupButtonStates()
        {
            var plan = GetSelectedPlanSilent();
            bool hasPlan = plan != null;
            bool connected = _pipeClient.IsConnected;
            bool selectedRunning = hasPlan && _runningPlanIds.Contains(plan.PlanId);
            bool anyRunning = _runningPlanIds.Count > 0;

            _btnStart.Enabled = hasPlan && !selectedRunning && connected;
            _btnCancelBackup.Enabled = selectedRunning;

            if (!connected)
                _lblBackupStatus.Text = Res.Get("Backup_ServiceDisconnected");
            else if (selectedRunning)
                _lblBackupStatus.Text = Res.Format("Backup_ReadyForPlan", plan.PlanName);
            else if (anyRunning)
                _lblBackupStatus.Text = string.Format("{0} görev çalışıyor", _runningPlanIds.Count);
            else if (hasPlan)
                _lblBackupStatus.Text = Res.Format("Backup_ReadyForPlan", plan.PlanName);
            else
                _lblBackupStatus.Text = Res.Get("ManualBackup_PleaseSelectPlan");
        }

        private void OnPipeConnectionChanged(object sender, bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnPipeConnectionChanged(sender, connected)));
                return;
            }

            UpdateStatusBarConnection(connected);
            UpdateBackupButtonStates();
        }

        private void UpdateStatusBarConnection(bool connected)
        {
            if (connected)
            {
                _tslStatus.Text      = Res.Get("StatusBar_ServiceConnected");
                _tslStatus.ForeColor = Theme.ModernTheme.StatusSuccess;
            }
            else
            {
                _tslStatus.Text      = Res.Get("StatusBar_ServiceDisconnected");
                _tslStatus.ForeColor = Theme.ModernTheme.StatusError;
            }
        }

        private void OnServiceStatusReceived(object sender, ServiceStatusMessage e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnServiceStatusReceived(sender, e)));
                return;
            }

            if (e.NextFireTimes == null || e.NextFireTimes.Count == 0) return;

            // Önce sözlüğü güncelle
            foreach (var kv in e.NextFireTimes)
            {
                if (kv.Value == null)
                {
                    _nextFireTimes.Remove(kv.Key);
                    continue;
                }

                string displayText;
                if (DateTimeOffset.TryParse(kv.Value, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTimeOffset dto))
                    displayText = dto.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
                else
                    displayText = kv.Value;

                _nextFireTimes[kv.Key] = displayText;
            }

            // Mevcut grid satırlarını güncelle (ApplyPlanFilter'a gerek kalmadan)
            foreach (DataGridViewRow row in _dgvPlans.Rows)
            {
                var plan = row.Tag as BackupPlan;
                if (plan == null) continue;

                if (_nextFireTimes.TryGetValue(plan.PlanId, out string displayTime))
                    row.Cells[_colNextRun.Index].Value = displayTime;
                else
                    row.Cells[_colNextRun.Index].Value = "—";
            }
        }

        /// <summary>
        /// Servisden güncel sonraki çalışma zamanlarını ister (fire-and-forget, hata yutulmaz).
        /// Plans sekmesine her geçişte çağrılarak _nextFireTimes sözlüğünün güncel kalması sağlanır.
        /// </summary>
        private async void RequestNextFireTimesAsync()
        {
            try
            {
                if (_pipeClient.IsConnected)
                    await _pipeClient.RequestStatusAsync();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Sonraki çalışma zamanları isteği gönderilemedi.");
            }
        }

        private void OnBackupActivityChanged(object sender, BackupActivityEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnBackupActivityChanged(sender, e)));
                return;
            }

            switch (e.ActivityType)
            {
                case BackupActivityType.Started:
                    if (!string.IsNullOrEmpty(e.PlanId))
                    {
                        _runningPlanIds.Add(e.PlanId);
                        _planProgressTracker[e.PlanId] = new PlanProgressTracker
                        {
                            DbIndex = 0,
                            DbTotal = e.TotalCount > 0 ? e.TotalCount : 1,
                            SqlDbCount = e.TotalCount,
                            MaxPercent = 0,
                            HasFileBackup = e.HasFileBackup,
                            HasCloudTargets = e.HasCloudTargets
                        };
                    }
                    _viewingPlanId = e.PlanId;
                    _progressBar.Value = 0;
                    _progressBar.ShowPercentage = true;
                    _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                    UpdatePlanRowProgress(e.PlanId, 0);
                    break;

                case BackupActivityType.DatabaseProgress:
                    if (e.TotalCount > 0)
                    {
                        string dbPlanId = !string.IsNullOrEmpty(e.PlanId) ? e.PlanId : _viewingPlanId;

                        if (!_planProgressTracker.TryGetValue(dbPlanId, out PlanProgressTracker dbTracker))
                        {
                            dbTracker = new PlanProgressTracker { MaxPercent = 0 };
                            _planProgressTracker[dbPlanId] = dbTracker;
                        }

                        int pct = dbTracker.CalculateDatabaseProgress(e.CurrentIndex, e.TotalCount);

                        if (dbPlanId == _viewingPlanId)
                        {
                            _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                            _progressBar.Value = pct;
                        }
                        UpdatePlanRowProgress(dbPlanId, pct);
                    }
                    break;

                case BackupActivityType.StepChanged:
                    {
                        string stepPlanId = !string.IsNullOrEmpty(e.PlanId) ? e.PlanId : _viewingPlanId;
                        if (_planProgressTracker.TryGetValue(stepPlanId, out PlanProgressTracker stepTracker))
                        {
                            int stepPct = -1;

                            if (e.StepName == "VSS")
                                stepTracker.HasVssUpload = true;
                            else if (e.StepName == "VSS Bulut Yükleme")
                                stepTracker.IsVssPhase = true;
                            else if (e.StepName == "Dosya Yedekleme" && !stepTracker.IsFileBackupPhase)
                                stepPct = stepTracker.CalculateFileBackupPhaseStart();
                            else if (e.StepName == "Dosya Sıkıştırma" && stepTracker.IsFileBackupPhase)
                                stepPct = stepTracker.CalculateFileCompressionProgress();

                            if (stepPct >= 0 && stepPlanId == _viewingPlanId)
                            {
                                _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                                _progressBar.Value = stepPct;
                            }
                            if (stepPct >= 0)
                                UpdatePlanRowProgress(stepPlanId, stepPct);

                            // Local-mode SQL adım bazlı ilerleme
                            int localPct = stepTracker.CalculateLocalStepProgress(e.StepName);
                            if (localPct >= 0)
                            {
                                if (stepPlanId == _viewingPlanId)
                                {
                                    _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                                    _progressBar.Value = localPct;
                                }
                                UpdatePlanRowProgress(stepPlanId, localPct);
                            }
                        }
                    }
                    break;

                case BackupActivityType.CloudUploadProgress:
                    {
                        string uploadPlanId = !string.IsNullOrEmpty(e.PlanId) ? e.PlanId : _viewingPlanId;

                        int cumPct;
                        if (_planProgressTracker.TryGetValue(uploadPlanId, out PlanProgressTracker upTracker)
                            && upTracker.DbTotal > 0)
                        {
                            cumPct = upTracker.CalculateCloudUploadProgress(
                                e.ProgressPercent, e.CloudTargetIndex, e.CloudTargetTotal);
                        }
                        else
                        {
                            cumPct = e.ProgressPercent;
                        }

                        if (uploadPlanId == _viewingPlanId)
                        {
                            _progressBar.Value = cumPct;
                            if (e.BytesTotal > 0)
                            {
                                _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.CustomText;
                                _progressBar.Text = string.Format("%{0}  {1}/{2}  {3}/s",
                                    cumPct,
                                    FormatFileSize(e.BytesSent),
                                    FormatFileSize(e.BytesTotal),
                                    FormatFileSize(e.SpeedBytesPerSecond));
                            }
                        }
                        UpdatePlanRowProgress(uploadPlanId, cumPct);
                    }
                    break;

                case BackupActivityType.CloudUploadStarted:
                case BackupActivityType.CloudUploadCompleted:
                    // Log satırı ve renk switch sonrasında BuildActivityLogLine + GetLogColor ile işlenir.
                    break;

                case BackupActivityType.Completed:
                case BackupActivityType.Failed:
                case BackupActivityType.Cancelled:
                    if (!string.IsNullOrEmpty(e.PlanId))
                    {
                        _runningPlanIds.Remove(e.PlanId);
                        _planProgressTracker.Remove(e.PlanId);
                    }
                    if (e.PlanId == _viewingPlanId)
                    {
                        // Tamamlandıysa önce %100 göster
                        if (e.ActivityType == BackupActivityType.Completed)
                            _progressBar.Value = 100;
                        _progressBar.ShowPercentage = false;
                        _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                        _progressBar.Value = 0;
                    }
                    UpdatePlanRowProgress(e.PlanId, 0);

                    // Bulut başarısızsa Completed ama uyarı ikonu göster
                    if (e.ActivityType == BackupActivityType.Completed && !e.IsSuccess && !string.IsNullOrEmpty(e.Message))
                        UpdatePlanRowStatusCustom(e.PlanId, "⚠ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.LogWarning);
                    else
                        UpdatePlanRowStatus(e.PlanId, e.ActivityType);
                    break;

                default:
                    Log.Warning("Unhandled BackupActivityType: {ActivityType} — OnBackupActivityChanged güncellenmelidir.", e.ActivityType);
                    break;
            }

            UpdateBackupButtonStates();
            bool isProgress = e.ActivityType == BackupActivityType.CloudUploadProgress;
            Color logColor = GetLogColor(e.ActivityType);

            // Bulut yükleme başarısız ama yedekleme tamamlandıysa uyarı rengi kullan
            if (e.ActivityType == BackupActivityType.Completed && !e.IsSuccess && !string.IsNullOrEmpty(e.Message))
                logColor = Theme.ModernTheme.LogWarning;

            AppendBackupLog(e.PlanId, BuildActivityLogLine(e), logColor, isProgress);
        }

        /// <summary>
        /// Plan grid satırının durum hücresini günceller (ikon + renk).
        /// ⚠️ Yeni terminal BackupActivityType eklendiğinde bu metot güncellenmelidir.
        /// </summary>
        private void UpdatePlanRowStatus(string planId, BackupActivityType activityType)
        {
            (string icon, Color color) = GetStatusDisplay(activityType);
            UpdatePlanRowStatusCustom(planId, icon, color);
        }

        /// <summary>
        /// Plan grid satırının durum hücresini özel ikon ve renkle günceller.
        /// </summary>
        private void UpdatePlanRowStatusCustom(string planId, string icon, Color color)
        {
            foreach (DataGridViewRow row in _dgvPlans.Rows)
            {
                var plan = row.Tag as BackupPlan;
                if (plan == null || plan.PlanId != planId) continue;

                if (row.Cells[_colStatus.Index] != null)
                {
                    row.Cells[_colStatus.Index].Value = icon;
                    row.Cells[_colStatus.Index].Style.ForeColor = color;
                }
                break;
            }
        }

        /// <summary>
        /// BackupActivityType → grid durum ikonu ve rengi.
        /// </summary>
        private static (string Icon, Color Color) GetStatusDisplay(BackupActivityType activityType) => activityType switch
        {
            BackupActivityType.Completed => ("✓ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.StatusSuccess),
            BackupActivityType.Failed    => ("✕ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.StatusError),
            BackupActivityType.Cancelled => ("■ " + DateTime.Now.ToString("HH:mm"), Color.Gray),
            _                            => ("⟳ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.AccentPrimary),
        };

        private void UpdatePlanRowProgress(string planId, int percent)
        {
            if (string.IsNullOrEmpty(planId)) return;
            foreach (DataGridViewRow row in _dgvPlans.Rows)
            {
                var p = row.Tag as BackupPlan;
                if (p == null || p.PlanId != planId) continue;

                if (_colProgress != null && _colProgress.Index >= 0 && _colProgress.Index < row.Cells.Count)
                    row.Cells[_colProgress.Index].Value = percent;
                break;
            }
        }

        /// <summary>
        /// BackupActivityEventArgs → kullanıcıya gösterilecek log satır metni.
        /// ⚠️ Yeni BackupActivityType eklendiğinde bu metot + GetLogColor + UpdatePlanRowStatus
        ///    + OnBackupActivityChanged switch bloğu birlikte güncellenmelidir.
        /// </summary>
        private string BuildActivityLogLine(BackupActivityEventArgs e) => e.ActivityType switch
        {
            BackupActivityType.Started
                => string.Format("[{0}] Yedekleme başladı.", e.PlanName ?? e.PlanId),

            BackupActivityType.DatabaseProgress
                => string.Format("{0} ({1}/{2}) işleniyor.", e.DatabaseName, e.CurrentIndex, e.TotalCount),

            BackupActivityType.StepChanged
                => !string.IsNullOrEmpty(e.Message) ? e.Message : string.Format("Adım: {0}", e.StepName),

            BackupActivityType.CloudUploadStarted
                => string.Format("Bulut yükleme başladı: {0}", e.CloudTargetName),

            BackupActivityType.CloudUploadProgress
                => BuildCloudUploadLogLine(e),

            BackupActivityType.CloudUploadCompleted
                => e.IsSuccess
                    ? string.Format("Bulut {0}: Başarılı ✓", e.CloudTargetName)
                    : string.Format("Bulut {0}: Başarısız ✕ — {1}", e.CloudTargetName, e.Message ?? "Bilinmeyen hata"),

            BackupActivityType.Completed
                => e.IsSuccess || string.IsNullOrEmpty(e.Message)
                    ? string.Format("[{0}] Yedekleme tamamlandı. ✓", e.PlanName ?? e.PlanId)
                    : string.Format("[{0}] Yedekleme tamamlandı (bulut yükleme başarısız). ⚠", e.PlanName ?? e.PlanId),

            BackupActivityType.Failed
                => string.Format("[{0}] Yedekleme başarısız: {1}", e.PlanName ?? e.PlanId, e.Message),

            BackupActivityType.Cancelled
                => string.Format("[{0}] Yedekleme iptal edildi.", e.PlanName ?? e.PlanId),

            _ => throw new ArgumentOutOfRangeException(
                nameof(e.ActivityType), e.ActivityType,
                $"Unhandled BackupActivityType: {e.ActivityType}. Tüm 5 sorumluluk noktasını güncelleyin.")
        };

        /// <summary>
        /// CloudUploadProgress için detaylı log satırı oluşturur (hız/ETA dahil).
        /// </summary>
        private string BuildCloudUploadLogLine(BackupActivityEventArgs e)
        {
            if (e.ProgressPercent >= 100) return string.Empty;
            if (e.BytesTotal > 0)
            {
                long bytesRemaining = e.BytesTotal - e.BytesSent;
                string etaStr = e.SpeedBytesPerSecond > 0
                    ? FormatEta(bytesRemaining, e.SpeedBytesPerSecond)
                    : "";
                string etaPart = etaStr.Length > 0 ? $" | Süre: {etaStr}" : "";
                return string.Format("Yükleniyor {0}: %{1} | Gönderilen: {2}/{3} | Kalan: {4} | Hız: {5}/s{6}",
                    e.CloudTargetName,
                    e.ProgressPercent,
                    FormatFileSize(e.BytesSent),
                    FormatFileSize(e.BytesTotal),
                    FormatFileSize(bytesRemaining),
                    FormatFileSize(e.SpeedBytesPerSecond),
                    etaPart);
            }
            return string.Format("Yükleniyor {0}: %{1}", e.CloudTargetName, e.ProgressPercent);
        }

        /// <summary>
        /// BackupActivityType → "Koru" temalı konsol rengi.
        /// Yeşil = güvenli/başarılı, Mavi = bilgi, Kırmızı = hata, Turkuaz = ilerleme.
        /// ⚠️ Yeni BackupActivityType eklendiğinde bu metot güncellenmelidir.
        /// </summary>
        private static Color GetLogColor(BackupActivityType activityType) => activityType switch
        {
            BackupActivityType.Started => Theme.ModernTheme.LogStarted,
            BackupActivityType.Completed => Theme.ModernTheme.LogSuccess,
            BackupActivityType.Failed => Theme.ModernTheme.LogError,
            BackupActivityType.Cancelled => Theme.ModernTheme.LogWarning,
            BackupActivityType.DatabaseProgress => Theme.ModernTheme.LogInfo,
            BackupActivityType.StepChanged => Theme.ModernTheme.LogInfo,
            BackupActivityType.CloudUploadStarted => Theme.ModernTheme.LogCloud,
            BackupActivityType.CloudUploadProgress => Theme.ModernTheme.LogProgress,
            BackupActivityType.CloudUploadCompleted => Theme.ModernTheme.LogCloud,
            _ => throw new ArgumentOutOfRangeException(
                nameof(activityType), activityType,
                $"Unhandled BackupActivityType: {activityType}. GetLogColor güncellenmelidir.")
        };

        #endregion

        #region ── TAB 2: Loglar ────────────────────────────────────────────

        private void PopulateLogFiles()
        {
            _cmbLogFile.Items.Clear();

            if (!Directory.Exists(_logDirectory))
            {
                _cmbLogFile.Items.Add(Res.Get("LogViewer_NoDirFound"));
                _cmbLogFile.SelectedIndex = 0;
                return;
            }

            var files = Directory.GetFiles(_logDirectory, "*.txt")
                .Concat(Directory.GetFiles(_logDirectory, "*.log"))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToArray();

            if (files.Length == 0)
            {
                _cmbLogFile.Items.Add(Res.Get("LogViewer_NoFilesFound"));
                _cmbLogFile.SelectedIndex = 0;
                return;
            }

            foreach (var file in files)
                _cmbLogFile.Items.Add(Path.GetFileName(file));

            _cmbLogFile.SelectedIndex = 0;
        }

        private void PopulateLevelFilter()
        {
            _cmbLevel.Items.Clear();
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelAll"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelVerbose"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelDebug"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelInfo"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelWarning"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelError"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelFatal"));
            _cmbLevel.SelectedIndex = 0;
        }

        private void PopulateLogPlanFilter()
        {
            _cmbLogPlan.Items.Clear();
            _cmbLogPlan.Items.Add(Res.Get("LogViewer_AllPlans"));
            foreach (var plan in _planManager.GetAllPlans())
                _cmbLogPlan.Items.Add(plan.PlanName ?? plan.PlanId);
            _cmbLogPlan.SelectedIndex = 0;
        }

        private void LoadSelectedLogFile()
        {
            _allLogEntries.Clear();
            _filteredLogEntries.Clear();
            _dgvLogs.RowCount = 0;

            if (_cmbLogFile.SelectedIndex < 0) return;
            var fileName = _cmbLogFile.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("(")) return;

            var filePath = Path.Combine(_logDirectory, fileName);
            if (!File.Exists(filePath)) return;

            try
            {
                string[] lines;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    lines = sr.ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }

                LogEntry currentEntry = null;
                foreach (var rawLine in lines)
                {
                    var line = rawLine.TrimEnd('\r');
                    var match = LogLineRegex.Match(line);

                    if (match.Success)
                    {
                        if (currentEntry != null)
                            _allLogEntries.Add(currentEntry);

                        currentEntry = new LogEntry
                        {
                            Timestamp = match.Groups[1].Value,
                            Level = match.Groups[2].Value,
                            Message = match.Groups[3].Value
                        };
                    }
                    else if (currentEntry != null)
                    {
                        currentEntry.Message += Environment.NewLine + line;
                    }
                }

                if (currentEntry != null)
                    _allLogEntries.Add(currentEntry);

                ApplyLogFilter();
                _tslLogTotal.Text = Res.Format("LogViewer_RecordCount", _allLogEntries.Count);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Log dosyası okunurken hata: {FileName}", fileName);
                _tslLogTotal.Text = Res.Get("LogViewer_ReadError");
            }
        }

        private void ApplyLogFilter()
        {
            string levelFilter = GetSelectedLevelCode();
            string searchText = _txtLogSearch.Text.Trim();
            string planFilter = _cmbLogPlan.SelectedIndex > 0 ? _cmbLogPlan.SelectedItem?.ToString() : null;
            bool hasSearch = !string.IsNullOrEmpty(searchText);

            _filteredLogEntries.Clear();

            foreach (var entry in _allLogEntries)
            {
                if (levelFilter != null && !entry.Level.Equals(levelFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (hasSearch && entry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (planFilter != null && entry.Message.IndexOf(planFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                _filteredLogEntries.Add(entry);
            }

            _dgvLogs.RowCount = 0;
            _dgvLogs.RowCount = _filteredLogEntries.Count;

            _tslLogFiltered.Text = Res.Format("LogViewer_FilteredCount", _filteredLogEntries.Count);
        }

        /// <summary>VirtualMode: Hücre verisini filtrelenmiş listeden sağlar.</summary>
        private void OnLogCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _filteredLogEntries.Count) return;
            var entry = _filteredLogEntries[e.RowIndex];
            switch (e.ColumnIndex)
            {
                case 0: e.Value = entry.Timestamp; break;
                case 1: e.Value = entry.Level; break;
                case 2: e.Value = entry.Message; break;
            }
        }

        /// <summary>VirtualMode: Seviyeye göre satır rengini belirler.</summary>
        private void OnLogCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _filteredLogEntries.Count) return;
            var level = _filteredLogEntries[e.RowIndex].Level;
            switch (level)
            {
                case "ERR":
                case "FTL":
                    e.CellStyle.ForeColor = Color.Red;
                    break;
                case "WRN":
                    e.CellStyle.ForeColor = Color.DarkOrange;
                    break;
                case "DBG":
                case "VRB":
                    e.CellStyle.ForeColor = SystemColors.GrayText;
                    break;
            }
        }

        private string GetSelectedLevelCode()
        {
            if (_cmbLevel.SelectedIndex <= 0) return null;
            switch (_cmbLevel.SelectedIndex)
            {
                case 1: return "VRB";
                case 2: return "DBG";
                case 3: return "INF";
                case 4: return "WRN";
                case 5: return "ERR";
                case 6: return "FTL";
                default: return null;
            }
        }

        private void OnLogFileChanged(object sender, EventArgs e) => LoadSelectedLogFile();
        private void OnLevelFilterChanged(object sender, EventArgs e) => ApplyLogFilter();
        private void OnLogSearchTextChanged(object sender, EventArgs e) => ApplyLogFilter();
        private void OnLogPlanFilterChanged(object sender, EventArgs e) => ApplyLogFilter();

        private void OnLogRefreshClick(object sender, EventArgs e) => LoadSelectedLogFile();

        private void OnAutoTailToggle(object sender, EventArgs e)
        {
            if (_chkAutoTail.Checked)
                _logTimer.Start();
            else
                _logTimer.Stop();
        }

        private void OnLogAutoRefreshTick(object sender, EventArgs e)
        {
            LoadSelectedLogFile();
            if (_dgvLogs.Rows.Count > 0)
                _dgvLogs.FirstDisplayedScrollingRowIndex = _dgvLogs.Rows.Count - 1;
        }

        private void OnLogExportClick(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = Res.Get("LogViewer_ExportDialogTitle");
                sfd.Filter = Res.Get("LogViewer_ExportFilter");
                sfd.FileName = "KoruMsSqlYedek_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (var sw = new StreamWriter(sfd.FileName))
                        {
                            foreach (DataGridViewRow row in _dgvLogs.Rows)
                            {
                                sw.WriteLine("{0}\t[{1}]\t{2}",
                                    row.Cells[0].Value, row.Cells[1].Value, row.Cells[2].Value);
                            }
                        }

                        Theme.ModernMessageBox.Show(
                            Res.Format("LogViewer_ExportSuccessFormat", _dgvLogs.Rows.Count),
                            Res.Get("LogViewer_ExportSuccessTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Theme.ModernMessageBox.Show(Res.Format("LogViewer_ExportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnClearLogFilterClick(object sender, EventArgs e)
        {
            _txtLogSearch.Clear();
            _cmbLevel.SelectedIndex = 0;
            _cmbLogPlan.SelectedIndex = 0;
            ApplyLogFilter();
        }

        private class LogEntry
        {
            public string Timestamp { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
        }

        #endregion

        #region ── TAB 3: Ayarlar ───────────────────────────────────────────

        private void LoadSettings()
        {
            try
            {
                _settings = _settingsManager.Load();
                SettingsToControls(_settings);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayarlar yüklenemedi.");
                Theme.ModernMessageBox.Show(Res.Format("Settings_LoadError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SettingsToControls(AppSettings s)
        {
            _cmbLanguage.SelectedIndex = s.Language == "en-US" ? 1 : 0;
            _cmbTheme.SelectedIndex = s.Theme == "light" ? 1 : 0;
            _chkStartWithWindows.Checked = s.StartWithWindows;
            _chkMinimizeToTray.Checked = s.MinimizeToTray;
            _txtDefaultBackupPath.Text = s.DefaultBackupPath;
            _nudLogRetention.Value = Math.Min(Math.Max(s.LogRetentionDays, 1), 365);
            _nudHistoryRetention.Value = Math.Min(Math.Max(s.HistoryRetentionDays, 1), 365);

            // Log renk şeması
            _cmbLogColorScheme.Items.Clear();
            var schemes = Theme.TerminalColorScheme.GetAll();
            int selectedIdx = 0;
            for (int i = 0; i < schemes.Length; i++)
            {
                _cmbLogColorScheme.Items.Add(schemes[i].DisplayName);
                if (string.Equals(schemes[i].Id, s.LogColorScheme, StringComparison.OrdinalIgnoreCase))
                    selectedIdx = i;
            }
            _cmbLogColorScheme.SelectedIndex = selectedIdx;

            LoadProfileList(s);
        }

        private AppSettings ControlsToSettings()
        {
            var s = _settings ?? new AppSettings();

            s.Language = _cmbLanguage.SelectedIndex == 1 ? "en-US" : "tr-TR";
            s.Theme = _cmbTheme.SelectedIndex == 1 ? "light" : "dark";
            s.StartWithWindows = _chkStartWithWindows.Checked;
            s.MinimizeToTray = _chkMinimizeToTray.Checked;
            s.DefaultBackupPath = _txtDefaultBackupPath.Text.Trim();
            s.LogRetentionDays = (int)_nudLogRetention.Value;
            s.HistoryRetentionDays = (int)_nudHistoryRetention.Value;

            // Log renk şeması
            var schemes = Theme.TerminalColorScheme.GetAll();
            int schemeIdx = _cmbLogColorScheme.SelectedIndex;
            s.LogColorScheme = (schemeIdx >= 0 && schemeIdx < schemes.Length)
                ? schemes[schemeIdx].Id
                : "koru";

            // SMTP profiller Add/Edit/Delete dialoglarında bağımsız kaydedilir; burada dokunulmaz.

            return s;
        }

        private bool ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_txtDefaultBackupPath.Text))
            {
                Theme.ModernMessageBox.Show(Res.Get("Settings_BackupPathRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDefaultBackupPath.Focus();
                return false;
            }

            return true;
        }

        private void OnBrowseBackupPath(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Res.Get("Settings_BrowsePath");
                fbd.SelectedPath = _txtDefaultBackupPath.Text;

                if (fbd.ShowDialog(this) == DialogResult.OK)
                    _txtDefaultBackupPath.Text = fbd.SelectedPath;
            }
        }

        private void OnSaveSettingsClick(object sender, EventArgs e)
        {
            if (!ValidateSettings()) return;

            try
            {
                var settings = ControlsToSettings();
                _settingsManager.Save(settings);
                Theme.ModernTheme.ApplyTheme(settings.Theme == "light"
                    ? Theme.ThemeMode.Light : Theme.ThemeMode.Dark);

                // Log renk şemasını uygula
                Theme.ModernTheme.ApplyLogColorScheme(settings.LogColorScheme);
                _txtBackupLog.BackColor = Theme.ModernTheme.LogConsoleBg;

                Log.Information("Ayarlar kaydedildi.");
                Theme.ModernMessageBox.Show(Res.Get("Settings_SavedMessage"),
                    Res.Get("Settings_SavedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayarlar kaydedilemedi.");
                Theme.ModernMessageBox.Show(Res.Format("Settings_SaveError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancelSettingsClick(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadProfileList(AppSettings s)
        {
            _dgvSmtpProfiles.Rows.Clear();
            _dgvSmtpProfiles.Columns.Clear();
            _dgvSmtpProfiles.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colId", Visible = false });
            _dgvSmtpProfiles.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Profil Adı", FillWeight = 25 });
            _dgvSmtpProfiles.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colHost", HeaderText = "Sunucu", FillWeight = 30 });
            _dgvSmtpProfiles.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colUser", HeaderText = "Kullanıcı", FillWeight = 25 });
            _dgvSmtpProfiles.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "colRecipients", HeaderText = "Alıcılar", FillWeight = 20 });

            foreach (var p in s.SmtpProfiles)
            {
                _dgvSmtpProfiles.Rows.Add(
                    p.Id,
                    p.DisplayName,
                    string.IsNullOrEmpty(p.Host) ? "—" : $"{p.Host}:{p.Port}",
                    p.Username,
                    p.RecipientEmails);
            }
        }

        private void OnSmtpAddClick(object sender, EventArgs e)
        {
            using var dlg = new Forms.SmtpProfileEditDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _settings.SmtpProfiles.Add(dlg.ResultProfile);
            _settingsManager.Save(_settings);
            LoadProfileList(_settings);
        }

        private void OnSmtpEditClick(object sender, EventArgs e)
        {
            if (_dgvSmtpProfiles.SelectedRows.Count == 0) return;

            string profileId = _dgvSmtpProfiles.SelectedRows[0].Cells["colId"].Value?.ToString() ?? string.Empty;
            var existing = _settings.SmtpProfiles.Find(p => p.Id == profileId);
            if (existing == null) return;

            using var dlg = new Forms.SmtpProfileEditDialog(existing);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            int idx = _settings.SmtpProfiles.IndexOf(existing);
            _settings.SmtpProfiles[idx] = dlg.ResultProfile;
            _settingsManager.Save(_settings);
            LoadProfileList(_settings);
        }

        private void OnSmtpDeleteClick(object sender, EventArgs e)
        {
            if (_dgvSmtpProfiles.SelectedRows.Count == 0) return;

            string profileId = _dgvSmtpProfiles.SelectedRows[0].Cells["colId"].Value?.ToString() ?? string.Empty;
            string profileName = _dgvSmtpProfiles.SelectedRows[0].Cells["colName"].Value?.ToString() ?? profileId;

            if (Theme.ModernMessageBox.Show(Res.Format("Settings_SmtpDeleteConfirm", profileName),
                    Res.Get("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _settings.SmtpProfiles.RemoveAll(p => p.Id == profileId);
            _settingsManager.Save(_settings);
            LoadProfileList(_settings);
        }

        private void OnSmtpTestClick(object sender, EventArgs e)
        {
            if (_dgvSmtpProfiles.SelectedRows.Count == 0)
            {
                Theme.ModernMessageBox.Show(Res.Get("Settings_SmtpSelectProfileFirst"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string profileId = _dgvSmtpProfiles.SelectedRows[0].Cells["colId"].Value?.ToString() ?? string.Empty;
            var profile = _settings.SmtpProfiles.Find(p => p.Id == profileId);
            if (profile == null) return;

            if (string.IsNullOrWhiteSpace(profile.Host))
            {
                Theme.ModernMessageBox.Show(Res.Get("Settings_SmtpServerRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.RecipientEmails))
            {
                Theme.ModernMessageBox.Show(Res.Get("Settings_SmtpRecipientTestRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                using var client = new MailKit.Net.Smtp.SmtpClient();
                var options = profile.UseSsl
                    ? MailKit.Security.SecureSocketOptions.StartTls
                    : MailKit.Security.SecureSocketOptions.None;

                client.Connect(profile.Host, profile.Port, options);

                if (!string.IsNullOrEmpty(profile.Username))
                {
                    string plainPwd = string.Empty;
                    if (!string.IsNullOrEmpty(profile.Password))
                    {
                        try { plainPwd = PasswordProtector.Unprotect(profile.Password); }
                        catch { /* şifreli değilse olduğu gibi kullan */ plainPwd = profile.Password; }
                    }
                    client.Authenticate(profile.Username, plainPwd);
                }

                string senderEmail = !string.IsNullOrWhiteSpace(profile.SenderEmail)
                    ? profile.SenderEmail : profile.Username;
                string senderName = !string.IsNullOrWhiteSpace(profile.SenderDisplayName)
                    ? profile.SenderDisplayName : "Koru MsSql Yedek";

                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(senderName, senderEmail));
                string firstRecipient = profile.RecipientEmails.Split(new[] { ';', ',' },
                    System.StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                message.To.Add(MimeKit.MailboxAddress.Parse(firstRecipient));
                message.Subject = Res.Format("Settings_SmtpTestSubject", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                message.Body = new MimeKit.TextPart("plain") { Text = Res.Get("Settings_SmtpTestBody") };

                client.Send(message);
                client.Disconnect(true);

                Theme.ModernMessageBox.Show(Res.Get("Settings_SmtpTestSuccess"), Res.Get("Success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SMTP test e-postası gönderilemedi.");
                Theme.ModernMessageBox.Show(Res.Format("Settings_SmtpTestError", SanitizeErrorMessage(ex.Message)),
                    Res.Get("Settings_SmtpTestErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        #endregion

        #region ── Localization ─────────────────────────────────────────────

        private void ApplyLocalization()
        {
            Text = "Koru MsSql Yedek";

            // Tab headers
            _tabDashboard.Text = Res.Get("Tab_Dashboard");
            _tabPlans.Text = Res.Get("Tab_Plans");
            _tabLogs.Text = Res.Get("Tab_Logs");
            _tabSettings.Text = Res.Get("Tab_Settings");

            // Dashboard
            _lblStatusCaption.Text = Res.Get("Dashboard_StatusCaption");
            _lblStatusValue.Text = Res.Get("Dashboard_Ready");
            _lblNextBackupCaption.Text = Res.Get("Dashboard_NextBackupCaption");
            _lblActivePlansCaption.Text = Res.Get("Dashboard_ActivePlansCaption");
            _lblGridTitle.Text = Res.Get("Dashboard_LastBackupsGroup");
            _colDate.Text = Res.Get("Dashboard_ColDate");
            _colPlan.Text = Res.Get("Dashboard_ColPlan");
            _colDatabase.Text = Res.Get("Dashboard_ColDatabase");
            _colResult.Text = Res.Get("Dashboard_ColResult");
            _colSize.Text = Res.Get("Dashboard_ColSize");

            // Plans
            _tsbNew.Text = Res.Get("PlanList_BtnNew");
            _tsbEdit.Text = Res.Get("PlanList_BtnEdit");
            _tsbDelete.Text = Res.Get("PlanList_BtnDelete");
            _tsbExport.Text = Res.Get("PlanList_BtnExport");
            _tsbImport.Text = Res.Get("PlanList_BtnImport");
            _colEnabled.HeaderText = Res.Get("PlanList_ColEnabled");
            _colPlanName.HeaderText = Res.Get("PlanList_ColPlanName");
            _colStrategy.HeaderText = Res.Get("PlanList_ColStrategy");
            _colDatabases.HeaderText = Res.Get("PlanList_ColDatabases");
            _colSchedule.HeaderText = Res.Get("PlanList_ColSchedule");
            _colCloudTargets.HeaderText = Res.Get("PlanList_ColCloud");
            _colCreatedAt.HeaderText = Res.Get("PlanList_ColCreatedAt");
            _colStatus.HeaderText = Res.Get("PlanList_ColStatus");
            _tslPlanCount.Text = Res.Format("PlanList_TotalFormat", 0);

            // Settings — theme items
            _lblTheme.Text = Res.Get("Settings_Theme");
            int prevThemeIdx = _cmbTheme.SelectedIndex;
            _cmbTheme.Items.Clear();
            _cmbTheme.Items.Add(Res.Get("Theme_Dark"));
            _cmbTheme.Items.Add(Res.Get("Theme_Light"));
            if (prevThemeIdx >= 0 && prevThemeIdx < _cmbTheme.Items.Count)
                _cmbTheme.SelectedIndex = prevThemeIdx;

            // Settings — log color scheme
            _lblLogColorScheme.Text = Res.Get("Settings_LogColorScheme");

            // Status bar
            _tslStatus.Text = Res.Get("Dashboard_Ready");
        }

        #endregion

        #region ── Security Helpers ────────────────────────────────────────

        /// <summary>
        /// Hata mesajından hassas bilgileri (dosya yolları, sunucu adresleri, stack trace) temizler.
        /// Kullanıcıya gösterilecek mesajlarda bilgi sızıntısını önler.
        /// </summary>
        private static string SanitizeErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return Res.Get("Error_Unknown");

            // Stack trace varsa kaldır
            int stackIdx = message.IndexOf("   at ", StringComparison.Ordinal);
            if (stackIdx > 0)
                message = message.Substring(0, stackIdx).Trim();

            // Dosya yollarını gizle (C:\..., \\server\... vb.)
            message = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"[A-Za-z]:\\[^\s""']+|\\\\[^\s""']+",
                "[yol gizlendi]");

            // Uzun mesajları kısalt
            const int maxLength = 300;
            if (message.Length > maxLength)
                message = message.Substring(0, maxLength) + "…";

            return message;
        }

        #endregion
    }
}
