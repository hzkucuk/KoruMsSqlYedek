namespace KoruMsSqlYedek.Win
{
    partial class MainWindow
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ── Top-level containers ──────────────────────────────────────
            _tabControl = new Theme.ModernTabControl();
            _tabDashboard = new System.Windows.Forms.TabPage();
            _tabPlans = new System.Windows.Forms.TabPage();
            _tabLogs = new System.Windows.Forms.TabPage();
            _tabSettings = new System.Windows.Forms.TabPage();
            _statusStrip = new System.Windows.Forms.StatusStrip();
            _tslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            _tslVersion = new System.Windows.Forms.ToolStripStatusLabel();

            // ── Tab 0: Dashboard ─────────────────────────────────────────
            _tlpCards = new System.Windows.Forms.TableLayoutPanel();
            _cardStatus = new Theme.ModernCardPanel();
            _lblStatusIcon = new System.Windows.Forms.PictureBox();
            _lblStatusCaption = new System.Windows.Forms.Label();
            _lblStatusValue = new System.Windows.Forms.Label();
            _cardNextBackup = new Theme.ModernCardPanel();
            _lblNextIcon = new System.Windows.Forms.PictureBox();
            _lblNextBackupCaption = new System.Windows.Forms.Label();
            _lblNextBackupValue = new System.Windows.Forms.Label();
            _cardActivePlans = new Theme.ModernCardPanel();
            _lblPlansIcon = new System.Windows.Forms.PictureBox();
            _lblActivePlansCaption = new System.Windows.Forms.Label();
            _lblActivePlansValue = new System.Windows.Forms.Label();
            _pnlGrid = new Theme.ModernCardPanel();
            _lblGridTitle = new System.Windows.Forms.Label();
            _lvLastBackups = new System.Windows.Forms.ListView();
            _colDate = new System.Windows.Forms.ColumnHeader();
            _colPlan = new System.Windows.Forms.ColumnHeader();
            _colDatabase = new System.Windows.Forms.ColumnHeader();
            _colType = new System.Windows.Forms.ColumnHeader();
            _colResult = new System.Windows.Forms.ColumnHeader();
            _colSize = new System.Windows.Forms.ColumnHeader();

            // ── Tab 1: Planlar ───────────────────────────────────────────
            _splitPlans = new System.Windows.Forms.SplitContainer();
            _toolStrip = new System.Windows.Forms.ToolStrip();
            _tsbNew = new System.Windows.Forms.ToolStripButton();
            _tsbEdit = new System.Windows.Forms.ToolStripButton();
            _tsbDelete = new System.Windows.Forms.ToolStripButton();
            _tsSep1 = new System.Windows.Forms.ToolStripSeparator();
            _tsbExport = new System.Windows.Forms.ToolStripButton();
            _tsbImport = new System.Windows.Forms.ToolStripButton();
            _tsSep2 = new System.Windows.Forms.ToolStripSeparator();
            _tsbRefreshPlans = new System.Windows.Forms.ToolStripButton();
            _tsSep3 = new System.Windows.Forms.ToolStripSeparator();
            _tslSearchLabel = new System.Windows.Forms.ToolStripLabel();
            _tstSearch = new System.Windows.Forms.ToolStripTextBox();
            _tsSep4 = new System.Windows.Forms.ToolStripSeparator();
            _tsbPassword = new System.Windows.Forms.ToolStripSplitButton();
            _dgvPlans = new System.Windows.Forms.DataGridView();
            _ctxPlan = new System.Windows.Forms.ContextMenuStrip(components);
            _ctxBackupNow = new System.Windows.Forms.ToolStripMenuItem();
            _ctxStopBackup = new System.Windows.Forms.ToolStripMenuItem();
            _ctxSep1 = new System.Windows.Forms.ToolStripSeparator();
            _ctxEditPlan = new System.Windows.Forms.ToolStripMenuItem();
            _ctxDeletePlan = new System.Windows.Forms.ToolStripMenuItem();
            _ctxSep2 = new System.Windows.Forms.ToolStripSeparator();
            _ctxExportPlan = new System.Windows.Forms.ToolStripMenuItem();
            _ctxSep3 = new System.Windows.Forms.ToolStripSeparator();
            _ctxViewPlanLogs = new System.Windows.Forms.ToolStripMenuItem();
            _ctxSep4 = new System.Windows.Forms.ToolStripSeparator();
            _ctxRestore = new System.Windows.Forms.ToolStripMenuItem();
            _colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            _colPlanName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colDatabases = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colSchedule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colCloudTargets = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colCreatedAt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colProgress = new Theme.DataGridViewProgressBarColumn();
            _colNextRun = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _statusStripPlans = new System.Windows.Forms.StatusStrip();
            _tslPlanCount = new System.Windows.Forms.ToolStripStatusLabel();

            // ── Yedekleme panel kontrolleri (Planlar sekmesi alt panel) ──
            _tlpBackup = new System.Windows.Forms.TableLayoutPanel();
            _lblBackupType = new System.Windows.Forms.Label();
            _cmbBackupType = new Theme.ModernComboBox();
            _lblBackupStatus = new System.Windows.Forms.Label();
            _progressBar = new Theme.ModernProgressBar();
            _txtBackupLog = new System.Windows.Forms.RichTextBox();
            _flpBackupButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnStart = new Theme.ModernButton();
            _btnCancelBackup = new Theme.ModernButton();

            // ── Tab 3: Loglar ────────────────────────────────────────────
            _tlpLogsMain = new System.Windows.Forms.TableLayoutPanel();
            _tlpLogToolbar = new System.Windows.Forms.TableLayoutPanel();
            _lblLogFile = new System.Windows.Forms.Label();
            _cmbLogFile = new Theme.ModernComboBox();
            _lblLevel = new System.Windows.Forms.Label();
            _cmbLevel = new Theme.ModernComboBox();
            _chkAutoTail = new Theme.ModernCheckBox();
            _lblLogSearch = new System.Windows.Forms.Label();
            _txtLogSearch = new System.Windows.Forms.TextBox();
            _btnClearLogFilter = new Theme.ModernButton();
            _btnLogRefresh = new Theme.ModernButton();
            _btnLogExport = new Theme.ModernButton();
            _lblLogPlan = new System.Windows.Forms.Label();
            _cmbLogPlan = new Theme.ModernComboBox();
            _dgvLogs = new System.Windows.Forms.DataGridView();
            _colTimestamp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _statusStripLogs = new System.Windows.Forms.StatusStrip();
            _tslLogTotal = new System.Windows.Forms.ToolStripStatusLabel();
            _tslLogFiltered = new System.Windows.Forms.ToolStripStatusLabel();

            // ── Tab 4: Ayarlar ───────────────────────────────────────────
            _tlpSettingsOuter = new System.Windows.Forms.TableLayoutPanel();
            _tabSettings2 = new Theme.ModernTabControl();
            _tabGeneral = new System.Windows.Forms.TabPage();
            _tabSmtp = new System.Windows.Forms.TabPage();
            _tlpGeneral = new System.Windows.Forms.TableLayoutPanel();
            _lblLanguage = new System.Windows.Forms.Label();
            _cmbLanguage = new Theme.ModernComboBox();
            _lblTheme = new System.Windows.Forms.Label();
            _cmbTheme = new Theme.ModernComboBox();
            _lblLogColorScheme = new System.Windows.Forms.Label();
            _cmbLogColorScheme = new Theme.ModernComboBox();
            _chkStartWithWindows = new Theme.ModernCheckBox();
            _chkMinimizeToTray = new Theme.ModernCheckBox();
            _lblDefaultBackupPath = new System.Windows.Forms.Label();
            _txtDefaultBackupPath = new System.Windows.Forms.TextBox();
            _btnBrowseBackupPath = new Theme.ModernButton();
            _lblLogRetention = new System.Windows.Forms.Label();
            _nudLogRetention = new Theme.ModernNumericUpDown();
            _lblLogRetentionSuffix = new System.Windows.Forms.Label();
            _lblHistoryRetention = new System.Windows.Forms.Label();
            _nudHistoryRetention = new Theme.ModernNumericUpDown();
            _lblHistoryRetentionSuffix = new System.Windows.Forms.Label();
            _tlpSmtp = new System.Windows.Forms.TableLayoutPanel();
            _lblSmtpProfilesTitle = new System.Windows.Forms.Label();
            _dgvSmtpProfiles = new System.Windows.Forms.DataGridView();
            _flpSmtpToolbar = new System.Windows.Forms.FlowLayoutPanel();
            _btnSmtpAdd = new Theme.ModernButton();
            _btnSmtpEdit = new Theme.ModernButton();
            _btnSmtpDelete = new Theme.ModernButton();
            _btnSmtpTest = new Theme.ModernButton();
            _flpSettingsButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnSaveSettings = new Theme.ModernButton();
            _btnCancelSettings = new Theme.ModernButton();

            // ─────────────────────────────────────────────────────────────
            // Begin Init
            // ─────────────────────────────────────────────────────────────
            _tabControl.SuspendLayout();
            _tabDashboard.SuspendLayout();
            _tabPlans.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_splitPlans).BeginInit();
            _splitPlans.Panel1.SuspendLayout();
            _splitPlans.Panel2.SuspendLayout();
            _splitPlans.SuspendLayout();
            _tabLogs.SuspendLayout();
            _tabSettings.SuspendLayout();
            _statusStrip.SuspendLayout();
            _tlpCards.SuspendLayout();
            _toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvPlans).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_lblStatusIcon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_lblNextIcon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_lblPlansIcon).BeginInit();
            _statusStripPlans.SuspendLayout();
            _tlpBackup.SuspendLayout();
            _flpBackupButtons.SuspendLayout();
            _tlpLogsMain.SuspendLayout();
            _tlpLogToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvLogs).BeginInit();
            _statusStripLogs.SuspendLayout();
            _tlpSettingsOuter.SuspendLayout();
            _tabSettings2.SuspendLayout();
            _tabGeneral.SuspendLayout();
            _tabSmtp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvSmtpProfiles).BeginInit();
            _tlpGeneral.SuspendLayout();
            _tlpSmtp.SuspendLayout();
            _flpSettingsButtons.SuspendLayout();
            SuspendLayout();

            // ═════════════════════════════════════════════════════════════
            // TAB CONTROL
            // ═════════════════════════════════════════════════════════════
            _tabControl.Controls.Add(_tabDashboard);
            _tabControl.Controls.Add(_tabPlans);
            _tabControl.Controls.Add(_tabLogs);
            _tabControl.Controls.Add(_tabSettings);
            _tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _tabControl.Font = Theme.ModernTheme.FontBody;
            _tabControl.Padding = new System.Drawing.Point(14, 8);

            // ═════════════════════════════════════════════════════════════
            // TAB 0 — DASHBOARD
            // ═════════════════════════════════════════════════════════════
            _tabDashboard.Text = "Dashboard";
            _tabDashboard.BackColor = Theme.ModernTheme.BackgroundColor;
            _tabDashboard.Controls.Add(_pnlGrid);
            _tabDashboard.Controls.Add(_tlpCards);

            // KPI Cards row
            _tlpCards.ColumnCount = 3;
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            _tlpCards.Dock = System.Windows.Forms.DockStyle.Top;
            _tlpCards.Height = 96;
            _tlpCards.Padding = new System.Windows.Forms.Padding(12, 8, 12, 4);
            _tlpCards.BackColor = Theme.ModernTheme.BackgroundColor;
            _tlpCards.RowCount = 1;
            _tlpCards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpCards.Controls.Add(_cardStatus, 0, 0);
            _tlpCards.Controls.Add(_cardNextBackup, 1, 0);
            _tlpCards.Controls.Add(_cardActivePlans, 2, 0);

            // Card: Status
            _cardStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardStatus.Margin = new System.Windows.Forms.Padding(4);
            _cardStatus.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            _lblStatusIcon.Size = new System.Drawing.Size(28, 28);
            _lblStatusIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            _lblStatusIcon.BackColor = System.Drawing.Color.Transparent;
            _lblStatusIcon.Location = new System.Drawing.Point(14, 14);
            _cardStatus.Controls.Add(_lblStatusIcon);
            _lblStatusCaption.AutoSize = true;
            _lblStatusCaption.Font = Theme.ModernTheme.FontCaption;
            _lblStatusCaption.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblStatusCaption.Location = new System.Drawing.Point(52, 10);
            _lblStatusCaption.Text = "Durum";
            _cardStatus.Controls.Add(_lblStatusCaption);
            _lblStatusValue.AutoSize = true;
            _lblStatusValue.Font = Theme.ModernTheme.FontSubtitle;
            _lblStatusValue.ForeColor = Theme.ModernTheme.StatusSuccess;
            _lblStatusValue.Location = new System.Drawing.Point(52, 30);
            _lblStatusValue.Text = "Hazır";
            _cardStatus.Controls.Add(_lblStatusValue);

            // Card: Next Backup
            _cardNextBackup.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardNextBackup.Margin = new System.Windows.Forms.Padding(4);
            _cardNextBackup.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            _lblNextIcon.Size = new System.Drawing.Size(28, 28);
            _lblNextIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            _lblNextIcon.BackColor = System.Drawing.Color.Transparent;
            _lblNextIcon.Location = new System.Drawing.Point(14, 14);
            _cardNextBackup.Controls.Add(_lblNextIcon);
            _lblNextBackupCaption.AutoSize = true;
            _lblNextBackupCaption.Font = Theme.ModernTheme.FontCaption;
            _lblNextBackupCaption.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblNextBackupCaption.Location = new System.Drawing.Point(52, 10);
            _lblNextBackupCaption.Text = "Son Yedekleme";
            _cardNextBackup.Controls.Add(_lblNextBackupCaption);
            _lblNextBackupValue.AutoSize = true;
            _lblNextBackupValue.Font = Theme.ModernTheme.FontSubtitle;
            _lblNextBackupValue.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblNextBackupValue.Location = new System.Drawing.Point(52, 30);
            _lblNextBackupValue.Text = "—";
            _cardNextBackup.Controls.Add(_lblNextBackupValue);

            // Card: Active Plans
            _cardActivePlans.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardActivePlans.Margin = new System.Windows.Forms.Padding(4);
            _cardActivePlans.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            _lblPlansIcon.Size = new System.Drawing.Size(28, 28);
            _lblPlansIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            _lblPlansIcon.BackColor = System.Drawing.Color.Transparent;
            _lblPlansIcon.Location = new System.Drawing.Point(14, 14);
            _cardActivePlans.Controls.Add(_lblPlansIcon);
            _lblActivePlansCaption.AutoSize = true;
            _lblActivePlansCaption.Font = Theme.ModernTheme.FontCaption;
            _lblActivePlansCaption.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblActivePlansCaption.Location = new System.Drawing.Point(52, 10);
            _lblActivePlansCaption.Text = "Aktif Görevler";
            _cardActivePlans.Controls.Add(_lblActivePlansCaption);
            _lblActivePlansValue.AutoSize = true;
            _lblActivePlansValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            _lblActivePlansValue.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblActivePlansValue.Location = new System.Drawing.Point(52, 26);
            _lblActivePlansValue.Text = "0";
            _cardActivePlans.Controls.Add(_lblActivePlansValue);

            // History grid card
            _pnlGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlGrid.Margin = new System.Windows.Forms.Padding(16, 4, 16, 8);
            _pnlGrid.Padding = new System.Windows.Forms.Padding(0, 34, 0, 0);
            _lblGridTitle.Text = "Son Yedeklemeler";
            _lblGridTitle.Font = Theme.ModernTheme.FontBodyBold;
            _lblGridTitle.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblGridTitle.AutoSize = true;
            _lblGridTitle.Location = new System.Drawing.Point(14, 8);
            _pnlGrid.Controls.Add(_lblGridTitle);
            _lvLastBackups.Dock = System.Windows.Forms.DockStyle.Fill;
            _lvLastBackups.View = System.Windows.Forms.View.Details;
            _lvLastBackups.FullRowSelect = true;
            _lvLastBackups.GridLines = false;
            _lvLastBackups.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _lvLastBackups.Font = Theme.ModernTheme.FontBody;
            _lvLastBackups.ForeColor = Theme.ModernTheme.TextPrimary;
            _lvLastBackups.BackColor = Theme.ModernTheme.SurfaceColor;
            _lvLastBackups.UseCompatibleStateImageBehavior = false;
            _lvLastBackups.OwnerDraw = true;
            _lvLastBackups.DrawColumnHeader += OnListViewDrawColumnHeader;
            _lvLastBackups.DrawItem += OnListViewDrawItem;
            _lvLastBackups.DrawSubItem += OnListViewDrawSubItem;
            _lvLastBackups.ColumnClick += OnLastBackupsColumnClick;
            _lvLastBackups.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                _colDate, _colPlan, _colDatabase, _colType, _colResult, _colSize });
            _pnlGrid.Controls.Add(_lvLastBackups);
            _colDate.Text = "Tarih"; _colDate.Width = 140;
            _colPlan.Text = "Plan"; _colPlan.Width = 130;
            _colDatabase.Text = "Veritabanı"; _colDatabase.Width = 130;
            _colType.Text = "Tür"; _colType.Width = 80;
            _colResult.Text = "Sonuç"; _colResult.Width = 90;
            _colSize.Text = "Boyut"; _colSize.Width = 80;

            // ═════════════════════════════════════════════════════════════
            // TAB 1 — PLANLAR (SplitContainer: üst=grid, alt=yedekleme)
            // ═════════════════════════════════════════════════════════════
            _tabPlans.Text = "Görevler";
            _tabPlans.BackColor = Theme.ModernTheme.BackgroundColor;
            _tabPlans.Controls.Add(_splitPlans);
            _tabPlans.Controls.Add(_toolStrip);

            _toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                _tsbNew, _tsbEdit, _tsbDelete, _tsSep1, _tsbExport, _tsbImport, _tsSep2, _tsbRefreshPlans, _tsSep3, _tslSearchLabel, _tstSearch, _tsSep4, _tsbPassword });
            _toolStrip.BackColor = Theme.ModernTheme.SurfaceColor;
            _toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _toolStrip.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            _toolStrip.Renderer = new Theme.ModernToolStripRenderer();
            _toolStrip.Dock = System.Windows.Forms.DockStyle.Top;

            _tsbNew.Text = "Yeni Görev"; _tsbNew.Click += OnNewPlanClick;
            _tsbEdit.Text = "Düzenle"; _tsbEdit.Click += OnEditPlanClick;
            _tsbDelete.Text = "Sil"; _tsbDelete.Click += OnDeletePlanClick;
            _tsbExport.Text = "Dışa Aktar"; _tsbExport.Click += OnExportPlanClick;
            _tsbImport.Text = "İçe Aktar"; _tsbImport.Click += OnImportPlanClick;
            _tsbRefreshPlans.Text = "Yenile"; _tsbRefreshPlans.Click += OnRefreshPlansClick;
            _tsbPassword.Text = "Şifre";
            _tsbPassword.ButtonClick += OnPasswordSetupClick;
            _tsmiPasswordToggle = new System.Windows.Forms.ToolStripMenuItem();
            _tsmiPasswordToggle.Text = "Şifre Korumasını Pasif Yap";
            _tsmiPasswordToggle.Click += OnPasswordToggleClick;
            _tsmiPasswordSetup = new System.Windows.Forms.ToolStripMenuItem();
            _tsmiPasswordSetup.Text = "Şifre Ayarları...";
            _tsmiPasswordSetup.Click += OnPasswordSetupClick;
            _tsbPassword.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { _tsmiPasswordToggle, _tsmiPasswordSetup });

            _tslSearchLabel.Text = "Ara:";
            _tslSearchLabel.ForeColor = Theme.ModernTheme.TextSecondary;
            _tslSearchLabel.Margin = new System.Windows.Forms.Padding(12, 0, 4, 0);
            _tstSearch.Name = "_tstSearch";
            _tstSearch.Width = 200;
            _tstSearch.Font = Theme.ModernTheme.FontBody;
            _tstSearch.BackColor = Theme.ModernTheme.SurfaceColor;
            _tstSearch.ForeColor = Theme.ModernTheme.TextPrimary;
            _tstSearch.TextChanged += OnPlanSearchTextChanged;

            // SplitContainer
            _splitPlans.Dock = System.Windows.Forms.DockStyle.Fill;
            _splitPlans.Orientation = System.Windows.Forms.Orientation.Horizontal;
            _splitPlans.SplitterDistance = 280;
            _splitPlans.SplitterWidth = 6;
            _splitPlans.BackColor = Theme.ModernTheme.DividerColor;
            _splitPlans.Panel1.BackColor = Theme.ModernTheme.BackgroundColor;
            _splitPlans.Panel2.BackColor = Theme.ModernTheme.BackgroundColor;

            // Panel1: DataGridView + StatusStrip
            _splitPlans.Panel1.Controls.Add(_dgvPlans);
            _splitPlans.Panel1.Controls.Add(_statusStripPlans);

            _dgvPlans.AllowUserToAddRows = false;
            _dgvPlans.AllowUserToDeleteRows = false;
            _dgvPlans.AllowUserToResizeRows = false;
            _dgvPlans.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            _dgvPlans.BackgroundColor = Theme.ModernTheme.SurfaceColor;
            _dgvPlans.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvPlans.GridColor = Theme.ModernTheme.DividerColor;
            _dgvPlans.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvPlans.EnableHeadersVisualStyles = false;
            _dgvPlans.ColumnHeadersDefaultCellStyle.BackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvPlans.ColumnHeadersDefaultCellStyle.ForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvPlans.ColumnHeadersDefaultCellStyle.Font = Theme.ModernTheme.FontCaptionBold;
            _dgvPlans.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvPlans.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvPlans.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvPlans.ColumnHeadersHeight = 38;
            _dgvPlans.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvPlans.DefaultCellStyle.BackColor = Theme.ModernTheme.SurfaceColor;
            _dgvPlans.DefaultCellStyle.ForeColor = Theme.ModernTheme.TextPrimary;
            _dgvPlans.DefaultCellStyle.Font = Theme.ModernTheme.FontBody;
            _dgvPlans.DefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridSelection;
            _dgvPlans.DefaultCellStyle.SelectionForeColor = Theme.ModernTheme.TextOnAccent;
            _dgvPlans.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            _dgvPlans.RowTemplate.Height = 36;
            _dgvPlans.AlternatingRowsDefaultCellStyle.BackColor = Theme.ModernTheme.GridAlternateRow;
            _dgvPlans.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                _colEnabled, _colPlanName, _colStrategy, _colDatabases, _colSchedule, _colCloudTargets, _colCreatedAt, _colStatus, _colProgress, _colNextRun });
            _dgvPlans.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvPlans.MultiSelect = false;
            _dgvPlans.ReadOnly = true;
            _dgvPlans.RowHeadersVisible = false;
            _dgvPlans.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvPlans.CellDoubleClick += OnPlanGridDoubleClick;
            _dgvPlans.ContextMenuStrip = _ctxPlan;
            _dgvPlans.SelectionChanged += OnPlanGridSelectionChanged;
            _dgvPlans.ColumnHeaderMouseClick += OnPlanGridColumnHeaderClick;

            _colEnabled.HeaderText = "Aktif"; _colEnabled.ReadOnly = true; _colEnabled.FillWeight = 30;
            _colPlanName.HeaderText = "Görev Adı"; _colPlanName.ReadOnly = true; _colPlanName.FillWeight = 100;
            _colStrategy.HeaderText = "Strateji"; _colStrategy.ReadOnly = true; _colStrategy.FillWeight = 70;
            _colDatabases.HeaderText = "Veritabanları"; _colDatabases.ReadOnly = true; _colDatabases.FillWeight = 100;
            _colSchedule.HeaderText = "Zamanlama"; _colSchedule.ReadOnly = true; _colSchedule.FillWeight = 80;
            _colCloudTargets.HeaderText = "Depolama"; _colCloudTargets.ReadOnly = true; _colCloudTargets.FillWeight = 60;
            _colCreatedAt.HeaderText = "Oluşturulma"; _colCreatedAt.ReadOnly = true; _colCreatedAt.FillWeight = 60;
            _colStatus.HeaderText = "Son Çalışma"; _colStatus.ReadOnly = true; _colStatus.FillWeight = 90;
            _colProgress.HeaderText = "İlerleme"; _colProgress.ReadOnly = true; _colProgress.FillWeight = 65;
            _colNextRun.HeaderText = "Sonraki Çalışma"; _colNextRun.ReadOnly = true; _colNextRun.FillWeight = 90;

            _statusStripPlans.Dock = System.Windows.Forms.DockStyle.Bottom;
            _statusStripPlans.Items.Add(_tslPlanCount);
            _statusStripPlans.BackColor = Theme.ModernTheme.SurfaceColor;
            _statusStripPlans.ForeColor = Theme.ModernTheme.TextSecondary;
            _statusStripPlans.Font = Theme.ModernTheme.FontCaption;
            _statusStripPlans.SizingGrip = false;
            _statusStripPlans.Renderer = new Theme.ModernToolStripRenderer();
            _tslPlanCount.Text = "Toplam 0 görev";

            // ContextMenuStrip — plan sağ tık menüsü
            _ctxPlan.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                _ctxBackupNow, _ctxStopBackup, _ctxSep1, _ctxEditPlan, _ctxDeletePlan, _ctxSep2, _ctxExportPlan, _ctxSep3, _ctxViewPlanLogs, _ctxSep4, _ctxRestore });
            _ctxPlan.Renderer = new Theme.ModernToolStripRenderer();
            _ctxPlan.Opening += OnContextMenuOpening;

            _ctxBackupNow.Text = "Şimdi Yedekle";
            _ctxBackupNow.Click += OnCtxBackupNowClick;
            _ctxStopBackup.Text = "Yedeklemeyi Durdur";
            _ctxStopBackup.Enabled = false;
            _ctxStopBackup.Click += OnCtxStopBackupClick;
            _ctxEditPlan.Text = "Düzenle";
            _ctxEditPlan.Click += OnEditPlanClick;
            _ctxDeletePlan.Text = "Sil";
            _ctxDeletePlan.Click += OnDeletePlanClick;
            _ctxExportPlan.Text = "Dışa Aktar";
            _ctxExportPlan.Click += OnExportPlanClick;
            _ctxViewPlanLogs.Text = "Görev Logları";
            _ctxViewPlanLogs.Click += OnCtxViewPlanLogsClick;

            _ctxRestore.Text = "Geri Yükle...";
            _ctxRestore.Click += OnCtxRestoreClick;

            // Panel2: Manuel yedekleme kontrolleri
            _splitPlans.Panel2.Controls.Add(_tlpBackup);
            _splitPlans.Panel2.Controls.Add(_flpBackupButtons);

            _tlpBackup.ColumnCount = 2;
            _tlpBackup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpBackup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpBackup.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpBackup.Padding = new System.Windows.Forms.Padding(8, 4, 8, 0);
            _tlpBackup.RowCount = 4;
            _tlpBackup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpBackup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpBackup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpBackup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            _tlpBackup.Controls.Add(_lblBackupType, 0, 0);
            _tlpBackup.Controls.Add(_cmbBackupType, 1, 0);
            _lblBackupType.Text = "Yedek Türü:";
            _lblBackupType.AutoSize = true;
            _lblBackupType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblBackupType.Margin = new System.Windows.Forms.Padding(3, 6, 8, 3);
            _lblBackupType.ForeColor = Theme.ModernTheme.TextPrimary;
            _cmbBackupType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbBackupType.Items.AddRange(new object[] { "Full (Tam)", "Differential (Fark)", "Incremental (Artırımlı)" });
            _cmbBackupType.SelectedIndex = 0;
            _cmbBackupType.Width = 200;
            _cmbBackupType.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);

            _tlpBackup.Controls.Add(_lblBackupStatus, 0, 1);
            _tlpBackup.SetColumnSpan(_lblBackupStatus, 2);
            _lblBackupStatus.Text = "Hazır — listeden plan seçin.";
            _lblBackupStatus.AutoSize = true;
            _lblBackupStatus.Font = Theme.ModernTheme.FontBodyBold;
            _lblBackupStatus.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblBackupStatus.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);

            _tlpBackup.Controls.Add(_progressBar, 0, 2);
            _tlpBackup.SetColumnSpan(_progressBar, 2);
            _progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            _progressBar.Height = 22;
            _progressBar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            _progressBar.ShowPercentage = false;

            _tlpBackup.Controls.Add(_txtBackupLog, 0, 3);
            _tlpBackup.SetColumnSpan(_txtBackupLog, 2);
            _txtBackupLog.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtBackupLog.ReadOnly = true;
            _txtBackupLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _txtBackupLog.Font = new System.Drawing.Font("Cascadia Mono", 9F);
            _txtBackupLog.BackColor = Theme.ModernTheme.LogConsoleBg;
            _txtBackupLog.ForeColor = Theme.ModernTheme.LogDefault;
            _txtBackupLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);

            _flpBackupButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _flpBackupButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            _flpBackupButtons.AutoSize = true;
            _flpBackupButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _flpBackupButtons.Padding = new System.Windows.Forms.Padding(0, 4, 8, 4);
            _flpBackupButtons.BackColor = Theme.ModernTheme.SurfaceColor;
            _flpBackupButtons.Controls.Add(_btnCancelBackup);
            _flpBackupButtons.Controls.Add(_btnStart);

            _btnStart.Text = "▶ Yedeklemeyi Başlat";
            _btnStart.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnStart.AutoSize = true;
            _btnStart.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
            _btnStart.Height = 36;
            _btnStart.Margin = new System.Windows.Forms.Padding(4);
            _btnStart.Click += OnStartBackupClick;

            _btnCancelBackup.Text = "■ İptal Et";
            _btnCancelBackup.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancelBackup.Size = new System.Drawing.Size(110, 36);
            _btnCancelBackup.Margin = new System.Windows.Forms.Padding(4);
            _btnCancelBackup.Enabled = false;
            _btnCancelBackup.Click += OnCancelBackupClick;

            // ═════════════════════════════════════════════════════════════
            // TAB 3 — LOGLAR
            // ═════════════════════════════════════════════════════════════
            _tabLogs.Text = "Loglar";
            _tabLogs.BackColor = Theme.ModernTheme.BackgroundColor;
            _tabLogs.Controls.Add(_tlpLogsMain);
            _tabLogs.Controls.Add(_statusStripLogs);

            _tlpLogsMain.ColumnCount = 1;
            _tlpLogsMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpLogsMain.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpLogsMain.Padding = new System.Windows.Forms.Padding(4);
            _tlpLogsMain.RowCount = 2;
            _tlpLogsMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpLogsMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpLogsMain.Controls.Add(_tlpLogToolbar, 0, 0);
            _tlpLogsMain.Controls.Add(_dgvLogs, 0, 1);

            _tlpLogToolbar.AutoSize = true;
            _tlpLogToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _tlpLogToolbar.ColumnCount = 8;
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpLogToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpLogToolbar.RowCount = 2;
            _tlpLogToolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpLogToolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            _tlpLogToolbar.Controls.Add(_lblLogFile, 0, 0);
            _tlpLogToolbar.Controls.Add(_cmbLogFile, 1, 0);
            _tlpLogToolbar.Controls.Add(_lblLevel, 2, 0);
            _tlpLogToolbar.Controls.Add(_cmbLevel, 3, 0);
            _tlpLogToolbar.Controls.Add(_chkAutoTail, 4, 0);
            _tlpLogToolbar.Controls.Add(_lblLogSearch, 0, 1);
            _tlpLogToolbar.Controls.Add(_txtLogSearch, 1, 1);
            _tlpLogToolbar.SetColumnSpan(_txtLogSearch, 1);
            _tlpLogToolbar.Controls.Add(_lblLogPlan, 2, 1);
            _tlpLogToolbar.Controls.Add(_cmbLogPlan, 3, 1);
            _tlpLogToolbar.Controls.Add(_btnClearLogFilter, 4, 1);
            _tlpLogToolbar.Controls.Add(_btnLogRefresh, 6, 1);
            _tlpLogToolbar.Controls.Add(_btnLogExport, 7, 1);

            _lblLogFile.Text = "Dosya:"; _lblLogFile.AutoSize = true;
            _lblLogFile.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblLogFile.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLogFile.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _cmbLogFile.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLogFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLogFile.Margin = new System.Windows.Forms.Padding(3, 4, 8, 3);
            _cmbLogFile.SelectedIndexChanged += OnLogFileChanged;

            _lblLevel.Text = "Seviye:"; _lblLevel.AutoSize = true;
            _lblLevel.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblLevel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLevel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _cmbLevel.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLevel.Margin = new System.Windows.Forms.Padding(3, 4, 8, 3);
            _cmbLevel.SelectedIndexChanged += OnLevelFilterChanged;

            _chkAutoTail.Text = "Otomatik Takip"; _chkAutoTail.AutoSize = true;
            _chkAutoTail.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkAutoTail.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _chkAutoTail.Margin = new System.Windows.Forms.Padding(8, 6, 3, 3);
            _chkAutoTail.CheckedChanged += OnAutoTailToggle;

            _lblLogSearch.Text = "Ara:"; _lblLogSearch.AutoSize = true;
            _lblLogSearch.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblLogSearch.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLogSearch.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _txtLogSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtLogSearch.Margin = new System.Windows.Forms.Padding(3, 4, 8, 6);
            _txtLogSearch.TextChanged += OnLogSearchTextChanged;

            _lblLogPlan.Text = "Görev:"; _lblLogPlan.AutoSize = true;
            _lblLogPlan.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblLogPlan.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLogPlan.Margin = new System.Windows.Forms.Padding(8, 6, 3, 6);
            _cmbLogPlan.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLogPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLogPlan.Margin = new System.Windows.Forms.Padding(3, 4, 8, 6);
            _cmbLogPlan.SelectedIndexChanged += OnLogPlanFilterChanged;

            _btnClearLogFilter.Text = "Temizle";
            _btnClearLogFilter.ButtonStyle = Theme.ModernButtonStyle.Ghost;
            _btnClearLogFilter.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            _btnClearLogFilter.Click += OnClearLogFilterClick;
            _btnLogRefresh.Text = "↻ Yenile"; _btnLogRefresh.AutoSize = true;
            _btnLogRefresh.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnLogRefresh.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            _btnLogRefresh.Click += OnLogRefreshClick;
            _btnLogExport.Text = "💾 Dışa Aktar"; _btnLogExport.AutoSize = true;
            _btnLogExport.ButtonStyle = Theme.ModernButtonStyle.Ghost;
            _btnLogExport.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            _btnLogExport.Click += OnLogExportClick;

            _dgvLogs.AllowUserToAddRows = false;
            _dgvLogs.AllowUserToDeleteRows = false;
            _dgvLogs.VirtualMode = true;
            _dgvLogs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            _dgvLogs.EnableHeadersVisualStyles = false;
            _dgvLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvLogs.ColumnHeadersHeight = 36;
            _dgvLogs.ColumnHeadersDefaultCellStyle.BackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvLogs.ColumnHeadersDefaultCellStyle.ForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvLogs.ColumnHeadersDefaultCellStyle.Font = Theme.ModernTheme.FontCaptionBold;
            _dgvLogs.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvLogs.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvLogs.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvLogs.DefaultCellStyle.BackColor = Theme.ModernTheme.SurfaceColor;
            _dgvLogs.DefaultCellStyle.ForeColor = Theme.ModernTheme.TextPrimary;
            _dgvLogs.DefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridSelection;
            _dgvLogs.DefaultCellStyle.SelectionForeColor = Theme.ModernTheme.TextOnAccent;
            _dgvLogs.AlternatingRowsDefaultCellStyle.BackColor = Theme.ModernTheme.GridAlternateRow;
            _dgvLogs.GridColor = Theme.ModernTheme.DividerColor;
            _dgvLogs.BackgroundColor = Theme.ModernTheme.SurfaceColor;
            _dgvLogs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvLogs.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvLogs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                _colTimestamp, _colLevel, _colMessage });
            _dgvLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvLogs.ReadOnly = true;
            _dgvLogs.RowHeadersVisible = false;
            _dgvLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvLogs.Font = new System.Drawing.Font("Consolas", 9F);

            _colTimestamp.HeaderText = "Zaman"; _colTimestamp.Width = 160;
            _colLevel.HeaderText = "Seviye"; _colLevel.Width = 55;
            _colMessage.HeaderText = "Mesaj";
            _colMessage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            _colMessage.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;

            _statusStripLogs.Dock = System.Windows.Forms.DockStyle.Bottom;
            _statusStripLogs.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _tslLogTotal, _tslLogFiltered });
            _statusStripLogs.BackColor = Theme.ModernTheme.SurfaceColor;
            _statusStripLogs.ForeColor = Theme.ModernTheme.TextSecondary;
            _statusStripLogs.Font = Theme.ModernTheme.FontCaption;
            _statusStripLogs.SizingGrip = false;
            _statusStripLogs.Renderer = new Theme.ModernToolStripRenderer();
            _tslLogTotal.Spring = true; _tslLogTotal.Text = "0 kayıt";
            _tslLogTotal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            _tslLogFiltered.Text = "0 gösteriliyor";
            _tslLogFiltered.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // ═════════════════════════════════════════════════════════════
            // TAB 4 — AYARLAR
            // ═════════════════════════════════════════════════════════════
            _tabSettings.Text = "Ayarlar";
            _tabSettings.BackColor = Theme.ModernTheme.BackgroundColor;
            _tabSettings.Controls.Add(_tlpSettingsOuter);
            _tabSettings.Controls.Add(_flpSettingsButtons);

            _tlpSettingsOuter.ColumnCount = 1;
            _tlpSettingsOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSettingsOuter.RowCount = 1;
            _tlpSettingsOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSettingsOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpSettingsOuter.Controls.Add(_tabSettings2, 0, 0);

            _tabSettings2.Controls.Add(_tabGeneral);
            _tabSettings2.Controls.Add(_tabSmtp);
            _tabSettings2.Dock = System.Windows.Forms.DockStyle.Fill;
            _tabSettings2.Font = Theme.ModernTheme.FontBody;
            _tabSettings2.Padding = new System.Drawing.Point(10, 5);

            // Tab: Genel
            _tabGeneral.Controls.Add(_tlpGeneral);
            _tabGeneral.Text = "Genel"; _tabGeneral.BackColor = Theme.ModernTheme.SurfaceColor;
            _tabGeneral.Padding = new System.Windows.Forms.Padding(8);

            _tlpGeneral.ColumnCount = 3;
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpGeneral.RowCount = 9;
            for (int i = 0; i < 8; i++)
                _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            _tlpGeneral.Controls.Add(_lblLanguage, 0, 0); _tlpGeneral.Controls.Add(_cmbLanguage, 1, 0);
            _lblLanguage.Text = "Dil:"; _lblLanguage.AutoSize = true;
            _lblLanguage.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblLanguage.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLanguage.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _cmbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLanguage.Items.AddRange(new object[] { "Türkçe (tr-TR)", "English (en-US)" });
            _cmbLanguage.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3); _cmbLanguage.Width = 200;

            _tlpGeneral.Controls.Add(_chkStartWithWindows, 1, 1);
            _chkStartWithWindows.Text = "Windows ile birlikte başlat"; _chkStartWithWindows.AutoSize = true;
            _chkStartWithWindows.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkStartWithWindows.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            _tlpGeneral.Controls.Add(_chkMinimizeToTray, 1, 2);
            _chkMinimizeToTray.Text = "Simge durumuna küçüldüğünde tepside gizle"; _chkMinimizeToTray.AutoSize = true;
            _chkMinimizeToTray.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkMinimizeToTray.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);

            _tlpGeneral.Controls.Add(_lblDefaultBackupPath, 0, 3);
            _tlpGeneral.Controls.Add(_txtDefaultBackupPath, 1, 3);
            _tlpGeneral.Controls.Add(_btnBrowseBackupPath, 2, 3);
            _lblDefaultBackupPath.Text = "Varsayılan yedek dizini:"; _lblDefaultBackupPath.AutoSize = true;
            _lblDefaultBackupPath.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblDefaultBackupPath.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblDefaultBackupPath.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _txtDefaultBackupPath.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtDefaultBackupPath.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _btnBrowseBackupPath.Text = "..."; _btnBrowseBackupPath.Size = new System.Drawing.Size(36, 28);
            _btnBrowseBackupPath.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBrowseBackupPath.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            _btnBrowseBackupPath.Click += OnBrowseBackupPath;

            _tlpGeneral.Controls.Add(_lblLogRetention, 0, 4);
            _tlpGeneral.Controls.Add(_nudLogRetention, 1, 4);
            _tlpGeneral.Controls.Add(_lblLogRetentionSuffix, 2, 4);
            _lblLogRetention.Text = "Log saklama süresi:"; _lblLogRetention.AutoSize = true;
            _lblLogRetention.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblLogRetention.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLogRetention.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _nudLogRetention.Minimum = 1; _nudLogRetention.Maximum = 365; _nudLogRetention.Value = 30;
            _nudLogRetention.Width = 80; _nudLogRetention.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _lblLogRetentionSuffix.Text = "gün"; _lblLogRetentionSuffix.AutoSize = true;
            _lblLogRetentionSuffix.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblLogRetentionSuffix.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLogRetentionSuffix.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);

            _tlpGeneral.Controls.Add(_lblHistoryRetention, 0, 5);
            _tlpGeneral.Controls.Add(_nudHistoryRetention, 1, 5);
            _tlpGeneral.Controls.Add(_lblHistoryRetentionSuffix, 2, 5);
            _lblHistoryRetention.Text = "Geçmiş saklama süresi:"; _lblHistoryRetention.AutoSize = true;
            _lblHistoryRetention.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblHistoryRetention.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblHistoryRetention.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _nudHistoryRetention.Minimum = 1; _nudHistoryRetention.Maximum = 365; _nudHistoryRetention.Value = 90;
            _nudHistoryRetention.Width = 80; _nudHistoryRetention.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _lblHistoryRetentionSuffix.Text = "gün"; _lblHistoryRetentionSuffix.AutoSize = true;
            _lblHistoryRetentionSuffix.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblHistoryRetentionSuffix.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblHistoryRetentionSuffix.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);

            _tlpGeneral.Controls.Add(_lblTheme, 0, 6); _tlpGeneral.Controls.Add(_cmbTheme, 1, 6);
            _lblTheme.Text = "Tema:"; _lblTheme.AutoSize = true;
            _lblTheme.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblTheme.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblTheme.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _cmbTheme.Items.AddRange(new object[] { "Koyu (Dark)", "Açık (Light)" });
            _cmbTheme.Width = 200;
            _cmbTheme.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            _tlpGeneral.Controls.Add(_lblLogColorScheme, 0, 7); _tlpGeneral.Controls.Add(_cmbLogColorScheme, 1, 7);
            _lblLogColorScheme.Text = "Log Konsol Temas\u0131:"; _lblLogColorScheme.AutoSize = true;
            _lblLogColorScheme.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblLogColorScheme.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLogColorScheme.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _cmbLogColorScheme.Width = 250;
            _cmbLogColorScheme.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Tab: SMTP
            _tabSmtp.Controls.Add(_tlpSmtp);
            _tabSmtp.Text = "E-posta (SMTP)"; _tabSmtp.BackColor = Theme.ModernTheme.SurfaceColor;
            _tabSmtp.Padding = new System.Windows.Forms.Padding(8);

            _tlpSmtp.ColumnCount = 1;
            _tlpSmtp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSmtp.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpSmtp.RowCount = 3;
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            _tlpSmtp.Controls.Add(_lblSmtpProfilesTitle, 0, 0);
            _lblSmtpProfilesTitle.Text = "Kayıtlı SMTP Profilleri — birden fazla profil ekleyip görevlerde kullanabilirsiniz:";
            _lblSmtpProfilesTitle.AutoSize = true;
            _lblSmtpProfilesTitle.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblSmtpProfilesTitle.Font = Theme.ModernTheme.FontCaption;
            _lblSmtpProfilesTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 6);

            _tlpSmtp.Controls.Add(_dgvSmtpProfiles, 0, 1);
            _dgvSmtpProfiles.AllowUserToAddRows = false;
            _dgvSmtpProfiles.AllowUserToDeleteRows = false;
            _dgvSmtpProfiles.AllowUserToResizeRows = false;
            _dgvSmtpProfiles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            _dgvSmtpProfiles.BackgroundColor = Theme.ModernTheme.SurfaceColor;
            _dgvSmtpProfiles.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvSmtpProfiles.GridColor = Theme.ModernTheme.DividerColor;
            _dgvSmtpProfiles.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvSmtpProfiles.EnableHeadersVisualStyles = false;
            _dgvSmtpProfiles.ColumnHeadersDefaultCellStyle.BackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvSmtpProfiles.ColumnHeadersDefaultCellStyle.ForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvSmtpProfiles.ColumnHeadersDefaultCellStyle.Font = Theme.ModernTheme.FontCaptionBold;
            _dgvSmtpProfiles.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvSmtpProfiles.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvSmtpProfiles.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvSmtpProfiles.ColumnHeadersHeight = 36;
            _dgvSmtpProfiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvSmtpProfiles.DefaultCellStyle.BackColor = Theme.ModernTheme.SurfaceColor;
            _dgvSmtpProfiles.DefaultCellStyle.ForeColor = Theme.ModernTheme.TextPrimary;
            _dgvSmtpProfiles.DefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridSelection;
            _dgvSmtpProfiles.DefaultCellStyle.SelectionForeColor = Theme.ModernTheme.TextOnAccent;
            _dgvSmtpProfiles.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            _dgvSmtpProfiles.AlternatingRowsDefaultCellStyle.BackColor = Theme.ModernTheme.GridAlternateRow;
            _dgvSmtpProfiles.RowTemplate.Height = 34;
            _dgvSmtpProfiles.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvSmtpProfiles.MultiSelect = false;
            _dgvSmtpProfiles.ReadOnly = true;
            _dgvSmtpProfiles.RowHeadersVisible = false;
            _dgvSmtpProfiles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvSmtpProfiles.CellDoubleClick += OnSmtpEditClick;

            _tlpSmtp.Controls.Add(_flpSmtpToolbar, 0, 2);
            _flpSmtpToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            _flpSmtpToolbar.AutoSize = true;
            _flpSmtpToolbar.WrapContents = false;
            _flpSmtpToolbar.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            _flpSmtpToolbar.Controls.Add(_btnSmtpAdd);
            _flpSmtpToolbar.Controls.Add(_btnSmtpEdit);
            _flpSmtpToolbar.Controls.Add(_btnSmtpDelete);
            _flpSmtpToolbar.Controls.Add(_btnSmtpTest);

            _btnSmtpAdd.Text = "➕ Ekle"; _btnSmtpAdd.AutoSize = true;
            _btnSmtpAdd.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSmtpAdd.Margin = new System.Windows.Forms.Padding(0, 4, 6, 4);
            _btnSmtpAdd.Click += OnSmtpAddClick;

            _btnSmtpEdit.Text = "✏ Düzenle"; _btnSmtpEdit.AutoSize = true;
            _btnSmtpEdit.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnSmtpEdit.Margin = new System.Windows.Forms.Padding(0, 4, 6, 4);
            _btnSmtpEdit.Click += OnSmtpEditClick;

            _btnSmtpDelete.Text = "🗑 Sil"; _btnSmtpDelete.AutoSize = true;
            _btnSmtpDelete.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnSmtpDelete.Margin = new System.Windows.Forms.Padding(0, 4, 6, 4);
            _btnSmtpDelete.Click += OnSmtpDeleteClick;

            _btnSmtpTest.Text = "✉ Test"; _btnSmtpTest.AutoSize = true;
            _btnSmtpTest.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnSmtpTest.Margin = new System.Windows.Forms.Padding(0, 4, 3, 4);
            _btnSmtpTest.Click += OnSmtpTestClick;

            // Settings bottom buttons
            _flpSettingsButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _flpSettingsButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            _flpSettingsButtons.AutoSize = true;
            _flpSettingsButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _flpSettingsButtons.Padding = new System.Windows.Forms.Padding(0, 4, 8, 8);
            _flpSettingsButtons.BackColor = Theme.ModernTheme.SurfaceColor;
            _flpSettingsButtons.Controls.Add(_btnCancelSettings);
            _flpSettingsButtons.Controls.Add(_btnSaveSettings);

            _btnSaveSettings.Text = "Kaydet"; _btnSaveSettings.Size = new System.Drawing.Size(100, 36);
            _btnSaveSettings.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSaveSettings.Margin = new System.Windows.Forms.Padding(4);
            _btnSaveSettings.Click += OnSaveSettingsClick;

            _btnCancelSettings.Text = "İptal"; _btnCancelSettings.Size = new System.Drawing.Size(100, 36);
            _btnCancelSettings.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancelSettings.Margin = new System.Windows.Forms.Padding(4);
            _btnCancelSettings.Click += OnCancelSettingsClick;

            // ═════════════════════════════════════════════════════════════
            // STATUS STRIP (global)
            // ═════════════════════════════════════════════════════════════
            _statusStrip.BackColor = Theme.ModernTheme.SurfaceColor;
            _statusStrip.ForeColor = Theme.ModernTheme.TextSecondary;
            _statusStrip.Font = Theme.ModernTheme.FontCaption;
            _statusStrip.SizingGrip = false;
            _statusStrip.Renderer = new Theme.ModernToolStripRenderer();
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _tslStatus, _tslVersion });
            _tslStatus.Spring = true; _tslStatus.Text = "Hazır";
            _tslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.18";
            _tslVersion.Text = $"v{ver}";
            _tslVersion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // ═════════════════════════════════════════════════════════════
            // FORM
            // ═════════════════════════════════════════════════════════════
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            ClientSize = new System.Drawing.Size(1024, 680);
            Font = Theme.ModernTheme.FontBody;
            Controls.Add(_tabControl);
            Controls.Add(_statusStrip);
            MinimumSize = new System.Drawing.Size(960, 640);
            Name = "MainWindow";
            ShowInTaskbar = true;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Koru MsSql Yedek";

            // ─────────────────────────────────────────────────────────────
            // Resume
            // ─────────────────────────────────────────────────────────────
            ((System.ComponentModel.ISupportInitialize)_lblStatusIcon).EndInit();
            ((System.ComponentModel.ISupportInitialize)_lblNextIcon).EndInit();
            ((System.ComponentModel.ISupportInitialize)_lblPlansIcon).EndInit();
            _tabControl.ResumeLayout(false);
            _tabDashboard.ResumeLayout(false);
            _tabPlans.ResumeLayout(false);
            _tabPlans.PerformLayout();
            _splitPlans.Panel1.ResumeLayout(false);
            _splitPlans.Panel1.PerformLayout();
            _splitPlans.Panel2.ResumeLayout(false);
            _splitPlans.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_splitPlans).EndInit();
            _splitPlans.ResumeLayout(false);
            _tabLogs.ResumeLayout(false);
            _tabLogs.PerformLayout();
            _tabSettings.ResumeLayout(false);
            _tabSettings.PerformLayout();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            _tlpCards.ResumeLayout(false);
            _toolStrip.ResumeLayout(false);
            _toolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvPlans).EndInit();
            _statusStripPlans.ResumeLayout(false);
            _statusStripPlans.PerformLayout();
            _tlpBackup.ResumeLayout(false);
            _tlpBackup.PerformLayout();
            _flpBackupButtons.ResumeLayout(false);
            _tlpLogsMain.ResumeLayout(false);
            _tlpLogsMain.PerformLayout();
            _tlpLogToolbar.ResumeLayout(false);
            _tlpLogToolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvLogs).EndInit();
            _statusStripLogs.ResumeLayout(false);
            _statusStripLogs.PerformLayout();
            _tlpSettingsOuter.ResumeLayout(false);
            _tabSettings2.ResumeLayout(false);
            _tabGeneral.ResumeLayout(false);
            _tabGeneral.PerformLayout();
            _tabSmtp.ResumeLayout(false);
            _tabSmtp.PerformLayout();
            _tlpGeneral.ResumeLayout(false);
            _tlpGeneral.PerformLayout();
            _tlpSmtp.ResumeLayout(false);
            _tlpSmtp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvSmtpProfiles).EndInit();
            _flpSettingsButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // Tab Control
        private Theme.ModernTabControl _tabControl;
        private System.Windows.Forms.TabPage _tabDashboard;
        private System.Windows.Forms.TabPage _tabPlans;
        private System.Windows.Forms.TabPage _tabLogs;
        private System.Windows.Forms.TabPage _tabSettings;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _tslStatus;
        private System.Windows.Forms.ToolStripStatusLabel _tslVersion;

        // Dashboard
        private System.Windows.Forms.TableLayoutPanel _tlpCards;
        private Theme.ModernCardPanel _cardStatus;
        private System.Windows.Forms.PictureBox _lblStatusIcon;
        private System.Windows.Forms.Label _lblStatusCaption;
        private System.Windows.Forms.Label _lblStatusValue;
        private Theme.ModernCardPanel _cardNextBackup;
        private System.Windows.Forms.PictureBox _lblNextIcon;
        private System.Windows.Forms.Label _lblNextBackupCaption;
        private System.Windows.Forms.Label _lblNextBackupValue;
        private Theme.ModernCardPanel _cardActivePlans;
        private System.Windows.Forms.PictureBox _lblPlansIcon;
        private System.Windows.Forms.Label _lblActivePlansCaption;
        private System.Windows.Forms.Label _lblActivePlansValue;
        private Theme.ModernCardPanel _pnlGrid;
        private System.Windows.Forms.Label _lblGridTitle;
        private System.Windows.Forms.ListView _lvLastBackups;
        private System.Windows.Forms.ColumnHeader _colDate;
        private System.Windows.Forms.ColumnHeader _colPlan;
        private System.Windows.Forms.ColumnHeader _colDatabase;
        private System.Windows.Forms.ColumnHeader _colType;
        private System.Windows.Forms.ColumnHeader _colResult;
        private System.Windows.Forms.ColumnHeader _colSize;

        // Plans
        private System.Windows.Forms.SplitContainer _splitPlans;
        private System.Windows.Forms.ToolStrip _toolStrip;
        private System.Windows.Forms.ToolStripButton _tsbNew;
        private System.Windows.Forms.ToolStripButton _tsbEdit;
        private System.Windows.Forms.ToolStripButton _tsbDelete;
        private System.Windows.Forms.ToolStripSeparator _tsSep1;
        private System.Windows.Forms.ToolStripButton _tsbExport;
        private System.Windows.Forms.ToolStripButton _tsbImport;
        private System.Windows.Forms.ToolStripSeparator _tsSep2;
        private System.Windows.Forms.ToolStripButton _tsbRefreshPlans;
        private System.Windows.Forms.ToolStripSeparator _tsSep3;
        private System.Windows.Forms.ToolStripLabel _tslSearchLabel;
        private System.Windows.Forms.ToolStripTextBox _tstSearch;
        private System.Windows.Forms.ToolStripSeparator _tsSep4;
        private System.Windows.Forms.ToolStripSplitButton _tsbPassword;
        private System.Windows.Forms.ToolStripMenuItem _tsmiPasswordToggle;
        private System.Windows.Forms.ToolStripMenuItem _tsmiPasswordSetup;
        private System.Windows.Forms.DataGridView _dgvPlans;
        private System.Windows.Forms.ContextMenuStrip _ctxPlan;
        private System.Windows.Forms.ToolStripMenuItem _ctxBackupNow;
        private System.Windows.Forms.ToolStripMenuItem _ctxStopBackup;
        private System.Windows.Forms.ToolStripSeparator _ctxSep1;
        private System.Windows.Forms.ToolStripMenuItem _ctxEditPlan;
        private System.Windows.Forms.ToolStripMenuItem _ctxDeletePlan;
        private System.Windows.Forms.ToolStripSeparator _ctxSep2;
        private System.Windows.Forms.ToolStripMenuItem _ctxExportPlan;
        private System.Windows.Forms.ToolStripSeparator _ctxSep3;
        private System.Windows.Forms.ToolStripMenuItem _ctxViewPlanLogs;
        private System.Windows.Forms.ToolStripSeparator _ctxSep4;
        private System.Windows.Forms.ToolStripMenuItem _ctxRestore;
        private System.Windows.Forms.DataGridViewCheckBoxColumn _colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colPlanName;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategy;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colDatabases;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colSchedule;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colCloudTargets;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colCreatedAt;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colStatus;
        private Theme.DataGridViewProgressBarColumn _colProgress;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colNextRun;
        private System.Windows.Forms.StatusStrip _statusStripPlans;
        private System.Windows.Forms.ToolStripStatusLabel _tslPlanCount;

        // Backup
        private System.Windows.Forms.TableLayoutPanel _tlpBackup;
        private System.Windows.Forms.Label _lblBackupType;
        private Theme.ModernComboBox _cmbBackupType;
        private System.Windows.Forms.Label _lblBackupStatus;
        private Theme.ModernProgressBar _progressBar;
        private System.Windows.Forms.RichTextBox _txtBackupLog;
        private System.Windows.Forms.FlowLayoutPanel _flpBackupButtons;
        private Theme.ModernButton _btnStart;
        private Theme.ModernButton _btnCancelBackup;

        // Logs
        private System.Windows.Forms.TableLayoutPanel _tlpLogsMain;
        private System.Windows.Forms.TableLayoutPanel _tlpLogToolbar;
        private System.Windows.Forms.Label _lblLogFile;
        private Theme.ModernComboBox _cmbLogFile;
        private System.Windows.Forms.Label _lblLevel;
        private Theme.ModernComboBox _cmbLevel;
        private Theme.ModernCheckBox _chkAutoTail;
        private System.Windows.Forms.Label _lblLogSearch;
        private System.Windows.Forms.TextBox _txtLogSearch;
        private Theme.ModernButton _btnClearLogFilter;
        private System.Windows.Forms.Label _lblLogPlan;
        private Theme.ModernComboBox _cmbLogPlan;
        private Theme.ModernButton _btnLogRefresh;
        private Theme.ModernButton _btnLogExport;
        private System.Windows.Forms.DataGridView _dgvLogs;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colTimestamp;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colMessage;
        private System.Windows.Forms.StatusStrip _statusStripLogs;
        private System.Windows.Forms.ToolStripStatusLabel _tslLogTotal;
        private System.Windows.Forms.ToolStripStatusLabel _tslLogFiltered;

        // Settings
        private System.Windows.Forms.TableLayoutPanel _tlpSettingsOuter;
        private Theme.ModernTabControl _tabSettings2;
        private System.Windows.Forms.TabPage _tabGeneral;
        private System.Windows.Forms.TabPage _tabSmtp;
        private System.Windows.Forms.TableLayoutPanel _tlpGeneral;
        private System.Windows.Forms.Label _lblLanguage;
        private Theme.ModernComboBox _cmbLanguage;
        private System.Windows.Forms.Label _lblTheme;
        private Theme.ModernComboBox _cmbTheme;
        private System.Windows.Forms.Label _lblLogColorScheme;
        private Theme.ModernComboBox _cmbLogColorScheme;
        private Theme.ModernCheckBox _chkStartWithWindows;
        private Theme.ModernCheckBox _chkMinimizeToTray;
        private System.Windows.Forms.Label _lblDefaultBackupPath;
        private System.Windows.Forms.TextBox _txtDefaultBackupPath;
        private Theme.ModernButton _btnBrowseBackupPath;
        private System.Windows.Forms.Label _lblLogRetention;
        private Theme.ModernNumericUpDown _nudLogRetention;
        private System.Windows.Forms.Label _lblLogRetentionSuffix;
        private System.Windows.Forms.Label _lblHistoryRetention;
        private Theme.ModernNumericUpDown _nudHistoryRetention;
        private System.Windows.Forms.Label _lblHistoryRetentionSuffix;
        private System.Windows.Forms.TableLayoutPanel _tlpSmtp;
        private System.Windows.Forms.Label _lblSmtpProfilesTitle;
        private System.Windows.Forms.DataGridView _dgvSmtpProfiles;
        private System.Windows.Forms.FlowLayoutPanel _flpSmtpToolbar;
        private Theme.ModernButton _btnSmtpAdd;
        private Theme.ModernButton _btnSmtpEdit;
        private Theme.ModernButton _btnSmtpDelete;
        private Theme.ModernButton _btnSmtpTest;
        private System.Windows.Forms.FlowLayoutPanel _flpSettingsButtons;
        private Theme.ModernButton _btnSaveSettings;
        private Theme.ModernButton _btnCancelSettings;
    }
}
