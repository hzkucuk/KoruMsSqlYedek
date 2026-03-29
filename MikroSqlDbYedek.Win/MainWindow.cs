using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MikroSqlDbYedek.Core.Helpers;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Win.Forms;
using MikroSqlDbYedek.Win.Helpers;
using Serilog;

namespace MikroSqlDbYedek.Win
{
    /// <summary>
    /// Tek pencereli ana form. 5 sekme: Dashboard, Planlar, Yedekleme, Loglar, Ayarlar.
    /// Tray ikonundan açılır; kapatıldığında gizlenir, uygulama kapanmaz.
    /// </summary>
    public partial class MainWindow : Theme.ModernFormBase
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MainWindow>();

        private readonly IPlanManager _planManager;
        private readonly IBackupHistoryManager _historyManager;
        private readonly ISqlBackupService _sqlBackupService;
        private readonly IAppSettingsManager _settingsManager;
        private readonly ICompressionService _compressionService;
        private readonly ICloudUploadOrchestrator _cloudOrchestrator;
        private readonly IFileBackupService _fileBackupService;

        // Timers
        private readonly System.Windows.Forms.Timer _dashboardTimer;
        private readonly System.Windows.Forms.Timer _logTimer;

        // Log viewer state
        private readonly string _logDirectory;
        private List<LogEntry> _allLogEntries = new List<LogEntry>();
        private static readonly Regex LogLineRegex = new Regex(
            @"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3})\s+\[(\w{3})\]\s+(.*)",
            RegexOptions.Compiled);

        // Settings state
        private AppSettings _settings;

        // Backup state
        private CancellationTokenSource _cts;
        private bool _isBackupRunning;

        public MainWindow(
            IPlanManager planManager,
            IBackupHistoryManager historyManager,
            ISqlBackupService sqlBackupService,
            IAppSettingsManager settingsManager,
            ICompressionService compressionService,
            ICloudUploadOrchestrator cloudOrchestrator,
            IFileBackupService fileBackupService)
        {
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));
            if (historyManager == null) throw new ArgumentNullException(nameof(historyManager));
            if (sqlBackupService == null) throw new ArgumentNullException(nameof(sqlBackupService));
            if (settingsManager == null) throw new ArgumentNullException(nameof(settingsManager));
            if (compressionService == null) throw new ArgumentNullException(nameof(compressionService));
            if (fileBackupService == null) throw new ArgumentNullException(nameof(fileBackupService));

            _planManager = planManager;
            _historyManager = historyManager;
            _sqlBackupService = sqlBackupService;
            _settingsManager = settingsManager;
            _compressionService = compressionService;
            _cloudOrchestrator = cloudOrchestrator;
            _fileBackupService = fileBackupService;

            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MikroSqlDbYedek", "Logs");

            InitializeComponent();
            ApplyIcons();

            _dashboardTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _dashboardTimer.Tick += (s, e) => LoadDashboardData();

            _logTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _logTimer.Tick += OnLogAutoRefreshTick;

            _tabControl.SelectedIndexChanged += OnTabChanged;
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
            _tsbNew.Text = "Yeni Plan";
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
            // Dashboard ilk açılışta yükle
            LoadDashboardData();
            _dashboardTimer.Start();
            Log.Debug("MainWindow gösterildi.");
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
            _cts?.Dispose();
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
                case 1: // Planlar
                    RefreshPlanList();
                    break;
                case 2: // Yedekleme
                    // Plan listesi güncel olsun
                    LoadBackupPlans();
                    break;
                case 3: // Loglar
                    if (_allLogEntries.Count == 0)
                    {
                        PopulateLogFiles();
                        PopulateLevelFilter();
                        LoadSelectedLogFile();
                    }
                    if (_chkAutoTail.Checked)
                        _logTimer.Start();
                    break;
                case 4: // Ayarlar
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

        #region ── TAB 1: Planlar ───────────────────────────────────────────

        private void RefreshPlanList()
        {
            try
            {
                var plans = _planManager.GetAllPlans();
                _dgvPlans.Rows.Clear();

                foreach (var plan in plans)
                {
                    var dbList = plan.Databases != null && plan.Databases.Count > 0
                        ? string.Join(", ", plan.Databases)
                        : "—";

                    var strategy = GetStrategyDisplayName(plan.Strategy?.Type ?? BackupStrategyType.Full);
                    var schedule = plan.Strategy?.FullSchedule ?? "—";
                    var cloudCount = plan.CloudTargets?.Count(t => t.IsEnabled) ?? 0;

                    var rowIndex = _dgvPlans.Rows.Add(
                        plan.IsEnabled,
                        plan.PlanName ?? Res.Get("PlanList_Unnamed"),
                        strategy,
                        dbList,
                        schedule,
                        Res.Format("PlanList_TargetFormat", cloudCount),
                        plan.CreatedAt.ToString("dd.MM.yyyy"));

                    _dgvPlans.Rows[rowIndex].Tag = plan;
                }

                _tslPlanCount.Text = Res.Format("PlanList_TotalFormat", plans.Count);
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

        private void OnNewPlanClick(object sender, EventArgs e)
        {
            using (var form = new PlanEditForm(_planManager, _sqlBackupService))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    RefreshPlanList();
            }
        }

        private void OnEditPlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            using (var form = new PlanEditForm(_planManager, _sqlBackupService, plan))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                    RefreshPlanList();
            }
        }

        private void OnDeletePlanClick(object sender, EventArgs e)
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

        private void OnImportPlanClick(object sender, EventArgs e)
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

        private void OnPlanGridDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OnEditPlanClick(sender, e);
        }

        private BackupPlan GetSelectedPlan()
        {
            if (_dgvPlans.CurrentRow == null || _dgvPlans.CurrentRow.Tag == null)
            {
                MessageBox.Show(Res.Get("PlanList_SelectPlan"), Res.Get("Info"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return _dgvPlans.CurrentRow.Tag as BackupPlan;
        }

        #endregion

        #region ── TAB 2: Manuel Yedekleme ─────────────────────────────────

        private void LoadBackupPlans()
        {
            _cmbPlan.Items.Clear();
            _cmbPlan.Items.Add(Res.Get("ManualBackup_SelectPlanDefault"));

            try
            {
                var plans = _planManager.GetAllPlans();
                foreach (var plan in plans.Where(p => p.IsEnabled))
                    _cmbPlan.Items.Add(plan);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Yedekleme planları yüklenemedi.");
            }

            _cmbPlan.SelectedIndex = 0;
            _cmbPlan.DisplayMember = "PlanName";
        }

        private void OnPlanSelectedChanged(object sender, EventArgs e)
        {
            _clbDatabases.Items.Clear();

            var plan = _cmbPlan.SelectedItem as BackupPlan;
            if (plan == null)
            {
                UpdateBackupButtonStates();
                return;
            }

            foreach (string db in plan.Databases)
                _clbDatabases.Items.Add(db, true);

            UpdateBackupButtonStates();
        }

        private async void OnStartBackupClick(object sender, EventArgs e)
        {
            var plan = _cmbPlan.SelectedItem as BackupPlan;
            if (plan == null)
            {
                MessageBox.Show(Res.Get("ManualBackup_PleaseSelectPlan"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedDatabases = _clbDatabases.CheckedItems.Cast<string>().ToList();
            if (selectedDatabases.Count == 0)
            {
                MessageBox.Show(Res.Get("ManualBackup_PleaseSelectDb"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SqlBackupType backupType;
            switch (_cmbBackupType.SelectedIndex)
            {
                case 1: backupType = SqlBackupType.Differential; break;
                case 2: backupType = SqlBackupType.Incremental; break;
                default: backupType = SqlBackupType.Full; break;
            }

            _isBackupRunning = true;
            _cts = new CancellationTokenSource();
            UpdateBackupButtonStates();
            _progressBar.Value = 0;
            _progressBar.Maximum = selectedDatabases.Count * 100;
            _lblBackupStatus.Text = Res.Get("ManualBackup_Starting");
            _lblBackupStatus.ForeColor = Theme.ModernTheme.TextSecondary;
            _txtBackupLog.Clear();

            int successCount = 0;
            int failCount = 0;
            int totalProgress = 0;
            string correlationId = Guid.NewGuid().ToString("N");

            try
            {
                for (int i = 0; i < selectedDatabases.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    string dbName = selectedDatabases[i];
                    _lblBackupStatus.Text = Res.Format("ManualBackup_ProgressFormat", i + 1, selectedDatabases.Count, dbName);
                    AppendBackupLog(Res.Format("ManualBackup_BackingUpFormat", dbName, backupType));

                    var progress = new Progress<int>(pct =>
                    {
                        int current = totalProgress + pct;
                        if (current <= _progressBar.Maximum)
                            _progressBar.Value = current;
                    });

                    try
                    {
                        // 1. SQL Backup
                        var result = await _sqlBackupService.BackupDatabaseAsync(
                            plan.SqlConnection, dbName, backupType, plan.LocalPath,
                            progress, _cts.Token);

                        if (result.Status != BackupResultStatus.Success)
                        {
                            failCount++;
                            AppendBackupLog(Res.Format("ManualBackup_FailedFormat", result.ErrorMessage));
                            SaveBackupHistory(result, plan, correlationId);
                            continue;
                        }

                        string sizeMb = (result.FileSizeBytes / 1048576.0).ToString("F1");
                        AppendBackupLog(Res.Format("ManualBackup_SuccessFormat", sizeMb, result.BackupFilePath));

                        // 2. Verify (isteğe bağlı)
                        if (plan.VerifyAfterBackup)
                        {
                            AppendBackupLog($"  ↳ Doğrulanıyor (RESTORE VERIFYONLY)...");
                            result.VerifyResult = await _sqlBackupService.VerifyBackupAsync(
                                plan.SqlConnection, result.BackupFilePath, _cts.Token);
                            AppendBackupLog(result.VerifyResult == true
                                ? "  ✓ Doğrulama başarılı."
                                : "  ✗ Doğrulama başarısız!");
                        }

                        // 3. Compress
                        if (plan.Compression != null)
                        {
                            try
                            {
                                string archivePath = Path.ChangeExtension(result.BackupFilePath, ".7z");
                                string password = !string.IsNullOrEmpty(plan.Compression.ArchivePassword)
                                    ? PasswordProtector.Unprotect(plan.Compression.ArchivePassword)
                                    : null;

                                AppendBackupLog($"  ↳ Sıkıştırılıyor → {Path.GetFileName(archivePath)}");
                                result.CompressedSizeBytes = await _compressionService.CompressAsync(
                                    result.BackupFilePath, archivePath, password, null, _cts.Token);
                                result.CompressedFilePath = archivePath;

                                string compMb = (result.CompressedSizeBytes / 1048576.0).ToString("F1");
                                AppendBackupLog($"  ✓ Sıkıştırma tamamlandı: {compMb} MB");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Manuel yedekleme sıkıştırma hatası: {Database}", dbName);
                                AppendBackupLog($"  ✗ Sıkıştırma hatası: {ex.Message}");
                            }
                        }

                        // 4. Cloud Upload (bulut modundaysa)
                        if (_cloudOrchestrator != null && plan.CloudTargets != null
                            && plan.CloudTargets.Any(t => t.IsEnabled)
                            && plan.Mode == BackupMode.Cloud)
                        {
                            try
                            {
                                string fileToUpload = !string.IsNullOrEmpty(result.CompressedFilePath)
                                    ? result.CompressedFilePath
                                    : result.BackupFilePath;
                                string remoteFileName = Path.GetFileName(fileToUpload);

                                AppendBackupLog($"  ↳ Bulut hedeflere yükleniyor...");
                                result.CloudUploadResults = await _cloudOrchestrator.UploadToAllAsync(
                                    fileToUpload, remoteFileName, plan.CloudTargets, null, _cts.Token);

                                int upSuccess = result.CloudUploadResults.Count(r => r.IsSuccess);
                                int upTotal = result.CloudUploadResults.Count;
                                AppendBackupLog($"  ✓ Bulut upload: {upSuccess}/{upTotal} başarılı");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Manuel yedekleme bulut upload hatası: {Database}", dbName);
                                AppendBackupLog($"  ✗ Bulut upload hatası: {ex.Message}");
                            }
                        }

                        // 5. History
                        SaveBackupHistory(result, plan, correlationId);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        AppendBackupLog(Res.Format("ManualBackup_ErrorFormat", ex.Message));
                        Log.Error(ex, "Manuel yedekleme hatası: {Database}", dbName);
                    }

                    totalProgress += 100;
                    _progressBar.Value = Math.Min(totalProgress, _progressBar.Maximum);
                }

                // ── Dosya Yedekleme ──
                if (plan.FileBackup != null && plan.FileBackup.IsEnabled
                    && plan.FileBackup.Sources.Any(s => s.IsEnabled))
                {
                    try
                    {
                        AppendBackupLog("");
                        AppendBackupLog("── Dosya Yedekleme ──");
                        _lblBackupStatus.Text = "Dosya yedekleme çalışıyor...";

                        var fileResults = await _fileBackupService.BackupFilesAsync(plan, null, _cts.Token);

                        foreach (var fr in fileResults)
                        {
                            if (fr.Status == BackupResultStatus.Success)
                            {
                                string sizeMb = (fr.TotalSizeBytes / 1048576.0).ToString("F1");
                                AppendBackupLog($"  ✓ {fr.SourceName}: {fr.FilesCopied} dosya, {sizeMb} MB");
                            }
                            else
                            {
                                AppendBackupLog($"  ✗ {fr.SourceName}: başarısız");
                            }
                        }

                        // Dosya yedekleri sıkıştırma
                        if (plan.Compression != null && fileResults.Any(r => r.Status == BackupResultStatus.Success))
                        {
                            string filesDir = Path.Combine(plan.LocalPath, "Files");
                            if (Directory.Exists(filesDir))
                            {
                                try
                                {
                                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                    string archivePath = Path.Combine(plan.LocalPath, $"Files_{timestamp}.7z");
                                    string password = !string.IsNullOrEmpty(plan.Compression.ArchivePassword)
                                        ? PasswordProtector.Unprotect(plan.Compression.ArchivePassword)
                                        : null;

                                    AppendBackupLog($"  ↳ Dosya yedekleri sıkıştırılıyor → {Path.GetFileName(archivePath)}");

                                    var sevenZip = _compressionService as Engine.Compression.SevenZipCompressionService;
                                    if (sevenZip != null)
                                    {
                                        long archiveSize = await sevenZip.CompressDirectoryAsync(
                                            filesDir, archivePath, password,
                                            plan.Compression.Level, null, _cts.Token);

                                        string compMb = (archiveSize / 1048576.0).ToString("F1");
                                        AppendBackupLog($"  ✓ Dosya arşivi tamamlandı: {compMb} MB");

                                        // Bulut upload
                                        if (_cloudOrchestrator != null && plan.CloudTargets != null
                                            && plan.CloudTargets.Any(t => t.IsEnabled)
                                            && plan.Mode == BackupMode.Cloud)
                                        {
                                            AppendBackupLog($"  ↳ Dosya arşivi buluta yükleniyor...");
                                            var uploadResults = await _cloudOrchestrator.UploadToAllAsync(
                                                archivePath, Path.GetFileName(archivePath),
                                                plan.CloudTargets, null, _cts.Token);

                                            int upOk = uploadResults.Count(r => r.IsSuccess);
                                            AppendBackupLog($"  ✓ Dosya arşiv upload: {upOk}/{uploadResults.Count} başarılı");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Dosya yedek sıkıştırma/upload hatası");
                                    AppendBackupLog($"  ✗ Dosya sıkıştırma hatası: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Dosya yedekleme hatası");
                        AppendBackupLog($"  ✗ Dosya yedekleme hatası: {ex.Message}");
                    }
                }

                _lblBackupStatus.Text = Res.Format("ManualBackup_CompletedFormat", successCount, failCount);
                _lblBackupStatus.ForeColor = failCount == 0 ? Color.LimeGreen : Color.OrangeRed;
                AppendBackupLog(Res.Format("ManualBackup_ResultFormat", successCount, failCount));
            }
            catch (OperationCanceledException)
            {
                _lblBackupStatus.Text = Res.Get("ManualBackup_Cancelled");
                _lblBackupStatus.ForeColor = Color.Gray;
                AppendBackupLog(Res.Get("ManualBackup_CancelledLog"));
            }
            catch (Exception ex)
            {
                _lblBackupStatus.Text = Res.Get("ManualBackup_UnexpectedError");
                _lblBackupStatus.ForeColor = Color.Red;
                AppendBackupLog(Res.Format("ManualBackup_UnexpectedErrorLog", ex.Message));
                Log.Error(ex, "Manuel yedekleme genel hatası.");
            }
            finally
            {
                _isBackupRunning = false;
                _cts?.Dispose();
                _cts = null;
                UpdateBackupButtonStates();
            }
        }

        private void OnCancelBackupClick(object sender, EventArgs e)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _lblBackupStatus.Text = Res.Get("ManualBackup_Cancelling");
                _btnCancelBackup.Enabled = false;
            }
        }

        private void OnDatabaseItemCheck(object sender, ItemCheckEventArgs e)
        {
            BeginInvoke(new Action(UpdateBackupButtonStates));
        }

        private void UpdateBackupButtonStates()
        {
            bool hasPlan = _cmbPlan.SelectedItem is BackupPlan;
            bool hasDb = _clbDatabases.CheckedItems.Count > 0;

            _btnStart.Enabled = !_isBackupRunning && hasPlan && hasDb;
            _btnCancelBackup.Enabled = _isBackupRunning;
            _cmbPlan.Enabled = !_isBackupRunning;
            _cmbBackupType.Enabled = !_isBackupRunning;
            _clbDatabases.Enabled = !_isBackupRunning;
        }

        private void AppendBackupLog(string text)
        {
            _txtBackupLog.AppendText(text + Environment.NewLine);
        }

        private void SaveBackupHistory(BackupResult result, BackupPlan plan, string correlationId)
        {
            result.PlanId = plan.PlanId;
            result.PlanName = plan.PlanName;
            result.CorrelationId = correlationId;

            try
            {
                _historyManager.SaveResult(result);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Yedek geçmişi kaydedilemedi: {CorrelationId}", correlationId);
            }
        }

        #endregion

        #region ── TAB 3: Loglar ────────────────────────────────────────────

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
                sfd.FileName = "MikroSqlDbYedek_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

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

        #region ── TAB 4: Ayarlar ───────────────────────────────────────────

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

            _txtSmtpHost.Text = s.Smtp.Host;
            _nudSmtpPort.Value = Math.Min(Math.Max(s.Smtp.Port, 1), 65535);
            _chkSmtpSsl.Checked = s.Smtp.UseSsl;
            _txtSmtpUsername.Text = s.Smtp.Username;
            _txtSmtpSenderEmail.Text = s.Smtp.SenderEmail;
            _txtSmtpSenderName.Text = s.Smtp.SenderDisplayName;
            _txtSmtpRecipients.Text = s.Smtp.RecipientEmails;

            if (!string.IsNullOrEmpty(s.Smtp.Password))
            {
                try { _txtSmtpPassword.Text = PasswordProtector.Unprotect(s.Smtp.Password); }
                catch { _txtSmtpPassword.Text = string.Empty; }
            }
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

            s.Smtp.Host = _txtSmtpHost.Text.Trim();
            s.Smtp.Port = (int)_nudSmtpPort.Value;
            s.Smtp.UseSsl = _chkSmtpSsl.Checked;
            s.Smtp.Username = _txtSmtpUsername.Text.Trim();
            s.Smtp.SenderEmail = _txtSmtpSenderEmail.Text.Trim();
            s.Smtp.SenderDisplayName = _txtSmtpSenderName.Text.Trim();
            s.Smtp.RecipientEmails = _txtSmtpRecipients.Text.Trim();

            string rawPassword = _txtSmtpPassword.Text;
            s.Smtp.Password = !string.IsNullOrEmpty(rawPassword)
                ? PasswordProtector.Protect(rawPassword)
                : null;

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

            if (!string.IsNullOrWhiteSpace(_txtSmtpHost.Text))
            {
                if (string.IsNullOrWhiteSpace(_txtSmtpSenderEmail.Text))
                {
                    MessageBox.Show(Res.Get("Settings_SmtpSenderRequired"), Res.Get("ValidationError"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtSmtpSenderEmail.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(_txtSmtpRecipients.Text))
                {
                    MessageBox.Show(Res.Get("Settings_SmtpRecipientRequired"), Res.Get("ValidationError"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtSmtpRecipients.Focus();
                    return false;
                }
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

        private void OnSmtpTestClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtSmtpHost.Text))
            {
                MessageBox.Show(Res.Get("Settings_SmtpServerRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtSmtpRecipients.Text))
            {
                MessageBox.Show(Res.Get("Settings_SmtpRecipientTestRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    int port = (int)_nudSmtpPort.Value;
                    var options = _chkSmtpSsl.Checked
                        ? MailKit.Security.SecureSocketOptions.StartTls
                        : MailKit.Security.SecureSocketOptions.None;

                    client.Connect(_txtSmtpHost.Text.Trim(), port, options);

                    string username = _txtSmtpUsername.Text.Trim();
                    if (!string.IsNullOrEmpty(username))
                        client.Authenticate(username, _txtSmtpPassword.Text);

                    string senderEmail = !string.IsNullOrWhiteSpace(_txtSmtpSenderEmail.Text)
                        ? _txtSmtpSenderEmail.Text.Trim() : username;
                    string senderName = !string.IsNullOrWhiteSpace(_txtSmtpSenderName.Text)
                        ? _txtSmtpSenderName.Text.Trim() : "MikroSqlDbYedek";

                    var message = new MimeKit.MimeMessage();
                    message.From.Add(new MimeKit.MailboxAddress(senderName, senderEmail));
                    message.To.Add(MimeKit.MailboxAddress.Parse(_txtSmtpRecipients.Text.Trim().Split(';')[0]));
                    message.Subject = Res.Format("Settings_SmtpTestSubject", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    message.Body = new MimeKit.TextPart("plain") { Text = Res.Get("Settings_SmtpTestBody") };

                    client.Send(message);
                    client.Disconnect(true);
                }

                MessageBox.Show(Res.Get("Settings_SmtpTestSuccess"), Res.Get("Success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SMTP test e-postası gönderilemedi.");
                MessageBox.Show(Res.Format("Settings_SmtpTestError", ex.Message),
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
            Text = "MikroSqlDbYedek";

            // Tab headers
            _tabDashboard.Text = Res.Get("Tab_Dashboard");
            _tabPlans.Text = Res.Get("Tab_Plans");
            _tabBackup.Text = Res.Get("Tab_Backup");
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
    }
}
