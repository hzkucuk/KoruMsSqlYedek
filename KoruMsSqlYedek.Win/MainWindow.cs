using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        private readonly IAppSettingsManager _settingsManager;
        private readonly ServicePipeClient _pipeClient;

        // Timers
        private readonly System.Windows.Forms.Timer _dashboardTimer;
        private readonly System.Windows.Forms.Timer _logTimer;

        // Log viewer state
        private readonly string _logDirectory;
        private List<LogEntry> _allLogEntries = new List<LogEntry>();
        private static readonly Regex LogLineRegex = new Regex(
            @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})[^\[]*\[(\w{3})\]\s+(.*)",
            RegexOptions.Compiled);

        // Settings state
        private AppSettings _settings;

        // Backup state
        private bool _isBackupRunning;
        private string _activePlanId;

        // Per-plan log buffer (planId → satır listesi)
        private readonly Dictionary<string, List<string>> _planLogs = new Dictionary<string, List<string>>();

        // Per-plan grid progress (planId → yüzde 0-100)
        private readonly Dictionary<string, int> _planProgress = new Dictionary<string, int>();

        // Plan listesi sıralama/filtreleme durumu
        private List<PlanRowData> _allPlanRows = new List<PlanRowData>();
        private int _planSortColumn = -1;
        private bool _planSortAscending = true;

        public MainWindow(
            IPlanManager planManager,
            IBackupHistoryManager historyManager,
            ISqlBackupService sqlBackupService,
            IAppSettingsManager settingsManager,
            ServicePipeClient pipeClient)
        {
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));
            if (historyManager == null) throw new ArgumentNullException(nameof(historyManager));
            if (sqlBackupService == null) throw new ArgumentNullException(nameof(sqlBackupService));
            if (settingsManager == null) throw new ArgumentNullException(nameof(settingsManager));
            if (pipeClient == null) throw new ArgumentNullException(nameof(pipeClient));

            _planManager = planManager;
            _historyManager = historyManager;
            _sqlBackupService = sqlBackupService;
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
                    break;
                case 2: // Loglar
                    if (_allLogEntries.Count == 0)
                    {
                        PopulateLogFiles();
                        PopulateLevelFilter();
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

            using var textBrush = new SolidBrush(Theme.ModernTheme.GridHeaderText);
            var textRect = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height);
            using var sf = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            e.Graphics.DrawString(e.Header.Text, Theme.ModernTheme.FontCaptionBold, textBrush, textRect, sf);
        }

        private void OnListViewDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void OnListViewDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
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
                        LastRunAt = lastRunAt
                    });
                }

                ApplyPlanFilter();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Plan listesi yüklenirken hata oluştu.");
                MessageBox.Show(Res.Format("PlanList_LoadError", ex.Message),
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
                    "...");

                _dgvPlans.Rows[rowIndex].Tag = plan;
                _dgvPlans.Rows[rowIndex].Cells[_colStatus.Index].Style.ForeColor = row.StatusColor;
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
        }

        private async void OnNewPlanClick(object sender, EventArgs e)
        {
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

            var result = MessageBox.Show(
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
                    MessageBox.Show(Res.Format("PlanList_DeleteError", ex.Message),
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
                        MessageBox.Show(Res.Get("PlanList_ExportSuccess"), Res.Get("Info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plan export hatası: {PlanId}", plan.PlanId);
                        MessageBox.Show(Res.Format("PlanList_ExportError", ex.Message),
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
                        MessageBox.Show(Res.Format("PlanList_ImportSuccess", plan.PlanName), Res.Get("Info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plan import hatası: {FilePath}", ofd.FileName);
                        MessageBox.Show(Res.Format("PlanList_ImportError", ex.Message),
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
                MessageBox.Show(Res.Get("ManualBackup_PleaseSelectPlan"), Res.Get("Warning"),
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
            bool running = _isBackupRunning && _activePlanId == plan?.PlanId;

            _ctxBackupNow.Enabled = hasPlan && !_isBackupRunning && _pipeClient.IsConnected;
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

            using RestoreDialog dlg = new RestoreDialog(plan, _historyManager, _sqlBackupService);
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

            // Seçilen plana ait log buffer'ını göster
            _txtBackupLog.Clear();
            if (_planLogs.TryGetValue(selected.PlanId, out var logs) && logs.Count > 0)
            {
                _txtBackupLog.Text = string.Join(Environment.NewLine, logs) + Environment.NewLine;
                _txtBackupLog.SelectionStart = _txtBackupLog.Text.Length;
                _txtBackupLog.ScrollToCaret();
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
                MessageBox.Show(Res.Get("Backup_ServiceNotConnected"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _activePlanId = plan.PlanId;
            _isBackupRunning = true;
            UpdateBackupButtonStates();

            // Bu plan için önceki log buffer'ını temizle
            _planLogs.Remove(plan.PlanId);
            _planProgress.Remove(plan.PlanId);
            _txtBackupLog.Clear();
            AppendBackupLog(string.Format("[{0}] {1}", plan.PlanName, Res.Get("ManualBackup_Starting")));

            try
            {
                await _pipeClient.SendManualBackupCommandAsync(plan.PlanId, "Full");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Manuel yedekleme komutu gönderilemedi: {PlanId}", plan.PlanId);
                AppendBackupLog(Res.Format("Backup_SendError", ex.Message));
                _isBackupRunning = false;
                _activePlanId = null;
                UpdateBackupButtonStates();
            }
        }

        private async void OnCancelBackupClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_activePlanId)) return;
            try
            {
                await _pipeClient.SendCancelCommandAsync(_activePlanId);
                AppendBackupLog(Res.Get("ManualBackup_Cancelling"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "İptal komutu gönderilemedi: {PlanId}", _activePlanId);
            }
        }

        private void UpdateBackupButtonStates()
        {
            var plan = GetSelectedPlanSilent();
            bool hasPlan = plan != null;
            bool connected = _pipeClient.IsConnected;

            _btnStart.Enabled = hasPlan && !_isBackupRunning && connected;
            _btnCancelBackup.Enabled = _isBackupRunning;

            if (!connected)
                _lblBackupStatus.Text = Res.Get("Backup_ServiceDisconnected");
            else if (_isBackupRunning)
                _lblBackupStatus.Text = Res.Format("Backup_ReadyForPlan", plan?.PlanName ?? _activePlanId);
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

            foreach (DataGridViewRow row in _dgvPlans.Rows)
            {
                var plan = row.Tag as BackupPlan;
                if (plan == null) continue;

                if (e.NextFireTimes.TryGetValue(plan.PlanId, out var nextFire))
                    row.Cells[_colNextRun.Index].Value = nextFire;
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
                    _isBackupRunning = true;
                    _activePlanId = e.PlanId;
                    _progressBar.Value = 0;
                    _progressBar.ShowPercentage = true;
                    _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                    UpdatePlanRowProgress(e.PlanId, 0);
                    break;

                case BackupActivityType.DatabaseProgress:
                    if (e.TotalCount > 0)
                    {
                        int pct = (int)((double)e.CurrentIndex / e.TotalCount * 50);
                        _progressBar.Value = pct;
                        UpdatePlanRowProgress(e.PlanId ?? _activePlanId, pct);
                    }
                    break;

                case BackupActivityType.CloudUploadProgress:
                    _progressBar.Value = e.ProgressPercent;
                    if (e.BytesTotal > 0)
                    {
                        _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.CustomText;
                        _progressBar.Text = string.Format("{0}%  {1}/{2}  {3}/s",
                            e.ProgressPercent,
                            FormatFileSize(e.BytesSent),
                            FormatFileSize(e.BytesTotal),
                            FormatFileSize(e.SpeedBytesPerSecond));
                    }
                    UpdatePlanRowProgress(_activePlanId, e.ProgressPercent);
                    break;

                case BackupActivityType.Completed:
                case BackupActivityType.Failed:
                case BackupActivityType.Cancelled:
                    _isBackupRunning = false;
                    _activePlanId = null;
                    _progressBar.ShowPercentage = false;
                    _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                    _progressBar.Value = 0;
                    UpdatePlanRowProgress(e.PlanId, 0);
                    UpdatePlanRowStatus(e.PlanId, e.ActivityType);
                    break;
            }

            UpdateBackupButtonStates();
            AppendBackupLog(BuildActivityLogLine(e));
        }

        private void UpdatePlanRowStatus(string planId, BackupActivityType activityType)
        {
            foreach (DataGridViewRow row in _dgvPlans.Rows)
            {
                var plan = row.Tag as BackupPlan;
                if (plan == null || plan.PlanId != planId) continue;

                string icon;
                Color color;
                switch (activityType)
                {
                    case BackupActivityType.Completed:
                        icon = "✓ " + DateTime.Now.ToString("HH:mm");
                        color = Theme.ModernTheme.StatusSuccess;
                        break;
                    case BackupActivityType.Failed:
                        icon = "✕ " + DateTime.Now.ToString("HH:mm");
                        color = Theme.ModernTheme.StatusError;
                        break;
                    case BackupActivityType.Cancelled:
                        icon = "■ " + DateTime.Now.ToString("HH:mm");
                        color = Color.Gray;
                        break;
                    default:
                        icon = "⟳ " + DateTime.Now.ToString("HH:mm");
                        color = Theme.ModernTheme.AccentPrimary;
                        break;
                }

                if (row.Cells[_colStatus.Index] != null)
                {
                    row.Cells[_colStatus.Index].Value = icon;
                    row.Cells[_colStatus.Index].Style.ForeColor = color;
                }
                break;
            }
        }

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

        private void AppendBackupLog(string line)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendBackupLog(line)));
                return;
            }

            string effectivePlanId = _activePlanId;
            string formatted = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line;

            // Plan'a ait buffer'a ekle
            if (!string.IsNullOrEmpty(effectivePlanId))
            {
                if (!_planLogs.ContainsKey(effectivePlanId))
                    _planLogs[effectivePlanId] = new List<string>();
                _planLogs[effectivePlanId].Add(formatted);
            }

            // Sadece seçili plan aktif planla eşleşiyorsa UI'yi güncelle
            var selected = GetSelectedPlanSilent();
            if (selected?.PlanId == effectivePlanId || string.IsNullOrEmpty(effectivePlanId))
            {
                _txtBackupLog.AppendText(formatted + Environment.NewLine);
            }
        }

        private string BuildActivityLogLine(BackupActivityEventArgs e)
        {
            switch (e.ActivityType)
            {
                case BackupActivityType.Started:
                    return string.Format("[{0}] Yedekleme başladı.", e.PlanName ?? e.PlanId);
                case BackupActivityType.DatabaseProgress:
                    return string.Format("{0} ({1}/{2}) işleniyor.", e.DatabaseName, e.CurrentIndex, e.TotalCount);
                case BackupActivityType.StepChanged:
                    return string.Format("Adım: {0}", e.StepName ?? e.Message);
                case BackupActivityType.CloudUploadStarted:
                    return string.Format("Bulut yükleme başladı: {0}", e.CloudTargetName);
                case BackupActivityType.CloudUploadProgress:
                    if (e.BytesTotal > 0)
                        return string.Format("Yükleniyor {0}: %{1} | Gönderilen: {2}/{3} | Hız: {4}/s",
                            e.CloudTargetName,
                            e.ProgressPercent,
                            FormatFileSize(e.BytesSent),
                            FormatFileSize(e.BytesTotal),
                            FormatFileSize(e.SpeedBytesPerSecond));
                    return string.Format("Yükleniyor {0}: %{1}", e.CloudTargetName, e.ProgressPercent);
                case BackupActivityType.CloudUploadCompleted:
                    return string.Format("Bulut {0}: {1}", e.CloudTargetName, e.IsSuccess ? "Başarılı ✓" : "Başarısız ✕");
                case BackupActivityType.Completed:
                    return string.Format("[{0}] Yedekleme tamamlandı. ✓", e.PlanName ?? e.PlanId);
                case BackupActivityType.Failed:
                    return string.Format("[{0}] Yedekleme başarısız: {1}", e.PlanName ?? e.PlanId, e.Message);
                case BackupActivityType.Cancelled:
                    return string.Format("[{0}] Yedekleme iptal edildi.", e.PlanName ?? e.PlanId);
                default:
                    return e.Message ?? string.Empty;
            }
        }

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

        private void LoadSelectedLogFile()
        {
            _allLogEntries.Clear();
            _dgvLogs.Rows.Clear();

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
            _dgvLogs.Rows.Clear();

            string levelFilter = GetSelectedLevelCode();
            string searchText = _txtLogSearch.Text.Trim();
            bool hasSearch = !string.IsNullOrEmpty(searchText);

            foreach (var entry in _allLogEntries)
            {
                if (levelFilter != null && !entry.Level.Equals(levelFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (hasSearch && entry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                int idx = _dgvLogs.Rows.Add(entry.Timestamp, entry.Level, entry.Message);
                ColorizeLogRow(_dgvLogs.Rows[idx], entry.Level);
            }

            _tslLogFiltered.Text = Res.Format("LogViewer_FilteredCount", _dgvLogs.Rows.Count);
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

        private static void ColorizeLogRow(DataGridViewRow row, string level)
        {
            switch (level)
            {
                case "ERR":
                case "FTL":
                    row.DefaultCellStyle.ForeColor = Color.Red;
                    break;
                case "WRN":
                    row.DefaultCellStyle.ForeColor = Color.DarkOrange;
                    break;
                case "DBG":
                case "VRB":
                    row.DefaultCellStyle.ForeColor = SystemColors.GrayText;
                    break;
            }
        }

        private void OnLogFileChanged(object sender, EventArgs e) => LoadSelectedLogFile();
        private void OnLevelFilterChanged(object sender, EventArgs e) => ApplyLogFilter();
        private void OnLogSearchTextChanged(object sender, EventArgs e) => ApplyLogFilter();

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

                        MessageBox.Show(
                            Res.Format("LogViewer_ExportSuccessFormat", _dgvLogs.Rows.Count),
                            Res.Get("LogViewer_ExportSuccessTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Res.Format("LogViewer_ExportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnClearLogFilterClick(object sender, EventArgs e)
        {
            _txtLogSearch.Clear();
            _cmbLevel.SelectedIndex = 0;
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
                MessageBox.Show(Res.Format("Settings_LoadError", ex.Message),
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

            // SMTP profiller Add/Edit/Delete dialoglarında bağımsız kaydedilir; burada dokunulmaz.

            return s;
        }

        private bool ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_txtDefaultBackupPath.Text))
            {
                MessageBox.Show(Res.Get("Settings_BackupPathRequired"), Res.Get("ValidationError"),
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
                Log.Information("Ayarlar kaydedildi.");
                MessageBox.Show(Res.Get("Settings_SavedMessage"),
                    Res.Get("Settings_SavedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayarlar kaydedilemedi.");
                MessageBox.Show(Res.Format("Settings_SaveError", ex.Message),
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

        private void OnSmtpAddClick(object? sender, EventArgs e)
        {
            using var dlg = new Forms.SmtpProfileEditDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _settings.SmtpProfiles.Add(dlg.ResultProfile);
            _settingsManager.Save(_settings);
            LoadProfileList(_settings);
        }

        private void OnSmtpEditClick(object? sender, EventArgs e)
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

        private void OnSmtpDeleteClick(object? sender, EventArgs e)
        {
            if (_dgvSmtpProfiles.SelectedRows.Count == 0) return;

            string profileId = _dgvSmtpProfiles.SelectedRows[0].Cells["colId"].Value?.ToString() ?? string.Empty;
            string profileName = _dgvSmtpProfiles.SelectedRows[0].Cells["colName"].Value?.ToString() ?? profileId;

            if (MessageBox.Show(Res.Format("Settings_SmtpDeleteConfirm", profileName),
                    Res.Get("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _settings.SmtpProfiles.RemoveAll(p => p.Id == profileId);
            _settingsManager.Save(_settings);
            LoadProfileList(_settings);
        }

        private void OnSmtpTestClick(object? sender, EventArgs e)
        {
            if (_dgvSmtpProfiles.SelectedRows.Count == 0)
            {
                MessageBox.Show(Res.Get("Settings_SmtpSelectProfileFirst"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string profileId = _dgvSmtpProfiles.SelectedRows[0].Cells["colId"].Value?.ToString() ?? string.Empty;
            var profile = _settings.SmtpProfiles.Find(p => p.Id == profileId);
            if (profile == null) return;

            if (string.IsNullOrWhiteSpace(profile.Host))
            {
                MessageBox.Show(Res.Get("Settings_SmtpServerRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.RecipientEmails))
            {
                MessageBox.Show(Res.Get("Settings_SmtpRecipientTestRequired"), Res.Get("Warning"),
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

                MessageBox.Show(Res.Get("Settings_SmtpTestSuccess"), Res.Get("Success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SMTP test e-postası gönderilemedi.");
                MessageBox.Show(Res.Format("Settings_SmtpTestError", SanitizeErrorMessage(ex.Message)),
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
