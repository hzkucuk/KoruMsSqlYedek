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
            _toolTip = new System.Windows.Forms.ToolTip(components);
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            _tabControl = new KoruMsSqlYedek.Win.Theme.ModernTabControl();
            _tabDashboard = new System.Windows.Forms.TabPage();
            _pnlGrid = new KoruMsSqlYedek.Win.Theme.ModernCardPanel();
            _lblGridTitle = new System.Windows.Forms.Label();
            _olvLastBackups = new KoruMsSqlYedek.Win.Controls.GroupedBackupListPanel();
            _tlpCards = new System.Windows.Forms.TableLayoutPanel();
            _cardStatus = new KoruMsSqlYedek.Win.Theme.ModernCardPanel();
            _lblStatusIcon = new System.Windows.Forms.PictureBox();
            _lblStatusCaption = new System.Windows.Forms.Label();
            _lblStatusValue = new System.Windows.Forms.Label();
            _cardNextBackup = new KoruMsSqlYedek.Win.Theme.ModernCardPanel();
            _lblNextIcon = new System.Windows.Forms.PictureBox();
            _lblNextBackupCaption = new System.Windows.Forms.Label();
            _lblNextBackupValue = new System.Windows.Forms.Label();
            _cardActivePlans = new KoruMsSqlYedek.Win.Theme.ModernCardPanel();
            _lblPlansIcon = new System.Windows.Forms.PictureBox();
            _lblActivePlansCaption = new System.Windows.Forms.Label();
            _lblActivePlansValue = new System.Windows.Forms.Label();
            _tabPlans = new System.Windows.Forms.TabPage();
            _splitPlans = new System.Windows.Forms.SplitContainer();
            _dgvPlans = new System.Windows.Forms.DataGridView();
            _colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            _colPlanName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colDatabases = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colSchedule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colCloudTargets = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colCreatedAt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colProgress = new KoruMsSqlYedek.Win.Theme.DataGridViewProgressBarColumn();
            _colNextRun = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            _statusStripPlans = new System.Windows.Forms.StatusStrip();
            _tslPlanCount = new System.Windows.Forms.ToolStripStatusLabel();
            _tlpBackup = new System.Windows.Forms.TableLayoutPanel();
            _lblBackupType = new System.Windows.Forms.Label();
            _cmbBackupType = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _lblBackupStatus = new System.Windows.Forms.Label();
            _progressBar = new KoruMsSqlYedek.Win.Theme.ModernProgressBar();
            _txtBackupLog = new System.Windows.Forms.RichTextBox();
            _flpBackupButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnCancelBackup = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnStart = new KoruMsSqlYedek.Win.Theme.ModernButton();
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
            _tabLogs = new System.Windows.Forms.TabPage();
            _tlpLogsMain = new System.Windows.Forms.TableLayoutPanel();
            _tlpLogToolbar = new System.Windows.Forms.TableLayoutPanel();
            _lblLogFile = new System.Windows.Forms.Label();
            _cmbLogFile = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _lblLevel = new System.Windows.Forms.Label();
            _cmbLevel = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _chkAutoTail = new KoruMsSqlYedek.Win.Theme.ModernCheckBox();
            _lblLogSearch = new System.Windows.Forms.Label();
            _txtLogSearch = new System.Windows.Forms.TextBox();
            _lblLogPlan = new System.Windows.Forms.Label();
            _cmbLogPlan = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _btnClearLogFilter = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnLogRefresh = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnLogExport = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _dgvLogs = new System.Windows.Forms.DataGridView();
            _colTimestamp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _statusStripLogs = new System.Windows.Forms.StatusStrip();
            _tslLogTotal = new System.Windows.Forms.ToolStripStatusLabel();
            _tslLogFiltered = new System.Windows.Forms.ToolStripStatusLabel();
            _tabSettings = new System.Windows.Forms.TabPage();
            _tlpSettingsOuter = new System.Windows.Forms.TableLayoutPanel();
            _tabSettings2 = new KoruMsSqlYedek.Win.Theme.ModernTabControl();
            _tabGeneral = new System.Windows.Forms.TabPage();
            _tlpGeneral = new System.Windows.Forms.TableLayoutPanel();
            _lblLanguage = new System.Windows.Forms.Label();
            _cmbLanguage = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _chkStartWithWindows = new KoruMsSqlYedek.Win.Theme.ModernCheckBox();
            _chkMinimizeToTray = new KoruMsSqlYedek.Win.Theme.ModernCheckBox();
            _lblDefaultBackupPath = new System.Windows.Forms.Label();
            _txtDefaultBackupPath = new System.Windows.Forms.TextBox();
            _btnBrowseBackupPath = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _lblLogRetention = new System.Windows.Forms.Label();
            _nudLogRetention = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _lblLogRetentionSuffix = new System.Windows.Forms.Label();
            _lblHistoryRetention = new System.Windows.Forms.Label();
            _nudHistoryRetention = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _lblHistoryRetentionSuffix = new System.Windows.Forms.Label();
            _lblTheme = new System.Windows.Forms.Label();
            _cmbTheme = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _lblLogColorScheme = new System.Windows.Forms.Label();
            _cmbLogColorScheme = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _tabSmtp = new System.Windows.Forms.TabPage();
            _tlpSmtp = new System.Windows.Forms.TableLayoutPanel();
            _lblSmtpProfilesTitle = new System.Windows.Forms.Label();
            _dgvSmtpProfiles = new System.Windows.Forms.DataGridView();
            _flpSmtpToolbar = new System.Windows.Forms.FlowLayoutPanel();
            _btnSmtpAdd = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnSmtpEdit = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnSmtpDelete = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnSmtpTest = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _tabSecurity = new System.Windows.Forms.TabPage();
            _tlpSecurity = new System.Windows.Forms.TableLayoutPanel();
            _lblSecurityTitle = new System.Windows.Forms.Label();
            _btnPasswordSetup = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _lblSecurityInfo = new System.Windows.Forms.Label();
            _flpSettingsButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnCancelSettings = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnSaveSettings = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _statusStrip = new System.Windows.Forms.StatusStrip();
            _tslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            _tslVersion = new System.Windows.Forms.ToolStripStatusLabel();
            _tabControl.SuspendLayout();
            _tabDashboard.SuspendLayout();
            _pnlGrid.SuspendLayout();
            _tlpCards.SuspendLayout();
            _cardStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_lblStatusIcon).BeginInit();
            _cardNextBackup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_lblNextIcon).BeginInit();
            _cardActivePlans.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_lblPlansIcon).BeginInit();
            _tabPlans.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_splitPlans).BeginInit();
            _splitPlans.Panel1.SuspendLayout();
            _splitPlans.Panel2.SuspendLayout();
            _splitPlans.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvPlans).BeginInit();
            _ctxPlan.SuspendLayout();
            _statusStripPlans.SuspendLayout();
            _tlpBackup.SuspendLayout();
            _flpBackupButtons.SuspendLayout();
            _toolStrip.SuspendLayout();
            _tabLogs.SuspendLayout();
            _tlpLogsMain.SuspendLayout();
            _tlpLogToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvLogs).BeginInit();
            _statusStripLogs.SuspendLayout();
            _tabSettings.SuspendLayout();
            _tlpSettingsOuter.SuspendLayout();
            _tabSettings2.SuspendLayout();
            _tabGeneral.SuspendLayout();
            _tlpGeneral.SuspendLayout();
            _tabSmtp.SuspendLayout();
            _tlpSmtp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvSmtpProfiles).BeginInit();
            _flpSmtpToolbar.SuspendLayout();
            _flpSettingsButtons.SuspendLayout();
            _statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // _tabControl
            // 
            _tabControl.Controls.Add(_tabDashboard);
            _tabControl.Controls.Add(_tabPlans);
            _tabControl.Controls.Add(_tabLogs);
            _tabControl.Controls.Add(_tabSettings);
            _tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _tabControl.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            _tabControl.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _tabControl.IndicatorHeight = 3;
            _tabControl.ItemSize = new System.Drawing.Size(120, 36);
            _tabControl.Location = new System.Drawing.Point(0, 0);
            _tabControl.Name = "_tabControl";
            _tabControl.Padding = new System.Drawing.Point(14, 8);
            _tabControl.SelectedIndex = 0;
            _tabControl.Size = new System.Drawing.Size(1024, 749);
            _tabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            _tabControl.TabIndex = 1;
            // 
            // _tabDashboard
            // 
            _tabDashboard.Controls.Add(_pnlGrid);
            _tabDashboard.Controls.Add(_tlpCards);
            _tabDashboard.Location = new System.Drawing.Point(4, 40);
            _tabDashboard.Name = "_tabDashboard";
            _tabDashboard.Size = new System.Drawing.Size(1016, 705);
            _tabDashboard.TabIndex = 0;
            _tabDashboard.Text = "Dashboard";
            // 
            // _pnlGrid
            // 
            _pnlGrid.Controls.Add(_lblGridTitle);
            _pnlGrid.Controls.Add(_olvLastBackups);
            _pnlGrid.CornerRadius = 8;
            _pnlGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlGrid.HeaderIcon = "";
            _pnlGrid.HeaderText = "";
            _pnlGrid.Location = new System.Drawing.Point(0, 109);
            _pnlGrid.Margin = new System.Windows.Forms.Padding(16, 5, 16, 9);
            _pnlGrid.Name = "_pnlGrid";
            _pnlGrid.Padding = new System.Windows.Forms.Padding(0, 39, 0, 0);
            _pnlGrid.ShowShadow = true;
            _pnlGrid.Size = new System.Drawing.Size(1016, 596);
            _pnlGrid.TabIndex = 0;
            // 
            // _lblGridTitle
            // 
            _lblGridTitle.AutoSize = true;
            _lblGridTitle.Location = new System.Drawing.Point(14, 9);
            _lblGridTitle.Name = "_lblGridTitle";
            _lblGridTitle.Size = new System.Drawing.Size(111, 17);
            _lblGridTitle.TabIndex = 0;
            _lblGridTitle.Text = "Son Yedeklemeler";
            // 
            // _olvLastBackups
            // 
            _olvLastBackups.Dock = System.Windows.Forms.DockStyle.Fill;
            _olvLastBackups.Location = new System.Drawing.Point(0, 39);
            _olvLastBackups.Name = "_olvLastBackups";
            _olvLastBackups.Size = new System.Drawing.Size(1016, 557);
            _olvLastBackups.TabIndex = 1;
            // 
            // _tlpCards
            // 
            _tlpCards.ColumnCount = 3;
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            _tlpCards.Controls.Add(_cardStatus, 0, 0);
            _tlpCards.Controls.Add(_cardNextBackup, 1, 0);
            _tlpCards.Controls.Add(_cardActivePlans, 2, 0);
            _tlpCards.Dock = System.Windows.Forms.DockStyle.Top;
            _tlpCards.Location = new System.Drawing.Point(0, 0);
            _tlpCards.Name = "_tlpCards";
            _tlpCards.Padding = new System.Windows.Forms.Padding(12, 9, 12, 5);
            _tlpCards.RowCount = 1;
            _tlpCards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpCards.Size = new System.Drawing.Size(1016, 109);
            _tlpCards.TabIndex = 1;
            // 
            // _cardStatus
            // 
            _cardStatus.Controls.Add(_lblStatusIcon);
            _cardStatus.Controls.Add(_lblStatusCaption);
            _cardStatus.Controls.Add(_lblStatusValue);
            _cardStatus.CornerRadius = 8;
            _cardStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardStatus.HeaderIcon = "";
            _cardStatus.HeaderText = "";
            _cardStatus.Location = new System.Drawing.Point(16, 14);
            _cardStatus.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            _cardStatus.Name = "_cardStatus";
            _cardStatus.Padding = new System.Windows.Forms.Padding(14, 11, 14, 11);
            _cardStatus.ShowShadow = true;
            _cardStatus.Size = new System.Drawing.Size(322, 85);
            _cardStatus.TabIndex = 0;
            // 
            // _lblStatusIcon
            // 
            _lblStatusIcon.BackColor = System.Drawing.Color.Transparent;
            _lblStatusIcon.Location = new System.Drawing.Point(14, 16);
            _lblStatusIcon.Name = "_lblStatusIcon";
            _lblStatusIcon.Size = new System.Drawing.Size(28, 32);
            _lblStatusIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            _lblStatusIcon.TabIndex = 0;
            _lblStatusIcon.TabStop = false;
            // 
            // _lblStatusCaption
            // 
            _lblStatusCaption.AutoSize = true;
            _lblStatusCaption.Location = new System.Drawing.Point(52, 11);
            _lblStatusCaption.Name = "_lblStatusCaption";
            _lblStatusCaption.Size = new System.Drawing.Size(47, 17);
            _lblStatusCaption.TabIndex = 1;
            _lblStatusCaption.Text = "Durum";
            // 
            // _lblStatusValue
            // 
            _lblStatusValue.AutoSize = true;
            _lblStatusValue.Location = new System.Drawing.Point(52, 34);
            _lblStatusValue.Name = "_lblStatusValue";
            _lblStatusValue.Size = new System.Drawing.Size(38, 17);
            _lblStatusValue.TabIndex = 2;
            _lblStatusValue.Text = "Hazır";
            // 
            // _cardNextBackup
            // 
            _cardNextBackup.Controls.Add(_lblNextIcon);
            _cardNextBackup.Controls.Add(_lblNextBackupCaption);
            _cardNextBackup.Controls.Add(_lblNextBackupValue);
            _cardNextBackup.CornerRadius = 8;
            _cardNextBackup.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardNextBackup.HeaderIcon = "";
            _cardNextBackup.HeaderText = "";
            _cardNextBackup.Location = new System.Drawing.Point(346, 14);
            _cardNextBackup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            _cardNextBackup.Name = "_cardNextBackup";
            _cardNextBackup.Padding = new System.Windows.Forms.Padding(14, 11, 14, 11);
            _cardNextBackup.ShowShadow = true;
            _cardNextBackup.Size = new System.Drawing.Size(322, 85);
            _cardNextBackup.TabIndex = 1;
            // 
            // _lblNextIcon
            // 
            _lblNextIcon.BackColor = System.Drawing.Color.Transparent;
            _lblNextIcon.Location = new System.Drawing.Point(14, 16);
            _lblNextIcon.Name = "_lblNextIcon";
            _lblNextIcon.Size = new System.Drawing.Size(28, 32);
            _lblNextIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            _lblNextIcon.TabIndex = 0;
            _lblNextIcon.TabStop = false;
            // 
            // _lblNextBackupCaption
            // 
            _lblNextBackupCaption.AutoSize = true;
            _lblNextBackupCaption.Location = new System.Drawing.Point(52, 11);
            _lblNextBackupCaption.Name = "_lblNextBackupCaption";
            _lblNextBackupCaption.Size = new System.Drawing.Size(96, 17);
            _lblNextBackupCaption.TabIndex = 1;
            _lblNextBackupCaption.Text = "Son Yedekleme";
            // 
            // _lblNextBackupValue
            // 
            _lblNextBackupValue.AutoSize = true;
            _lblNextBackupValue.Location = new System.Drawing.Point(52, 34);
            _lblNextBackupValue.Name = "_lblNextBackupValue";
            _lblNextBackupValue.Size = new System.Drawing.Size(21, 17);
            _lblNextBackupValue.TabIndex = 2;
            _lblNextBackupValue.Text = "—";
            // 
            // _cardActivePlans
            // 
            _cardActivePlans.Controls.Add(_lblPlansIcon);
            _cardActivePlans.Controls.Add(_lblActivePlansCaption);
            _cardActivePlans.Controls.Add(_lblActivePlansValue);
            _cardActivePlans.CornerRadius = 8;
            _cardActivePlans.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardActivePlans.HeaderIcon = "";
            _cardActivePlans.HeaderText = "";
            _cardActivePlans.Location = new System.Drawing.Point(676, 14);
            _cardActivePlans.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            _cardActivePlans.Name = "_cardActivePlans";
            _cardActivePlans.Padding = new System.Windows.Forms.Padding(14, 11, 14, 11);
            _cardActivePlans.ShowShadow = true;
            _cardActivePlans.Size = new System.Drawing.Size(324, 85);
            _cardActivePlans.TabIndex = 2;
            // 
            // _lblPlansIcon
            // 
            _lblPlansIcon.BackColor = System.Drawing.Color.Transparent;
            _lblPlansIcon.Location = new System.Drawing.Point(14, 16);
            _lblPlansIcon.Name = "_lblPlansIcon";
            _lblPlansIcon.Size = new System.Drawing.Size(28, 32);
            _lblPlansIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            _lblPlansIcon.TabIndex = 0;
            _lblPlansIcon.TabStop = false;
            // 
            // _lblActivePlansCaption
            // 
            _lblActivePlansCaption.AutoSize = true;
            _lblActivePlansCaption.Location = new System.Drawing.Point(52, 11);
            _lblActivePlansCaption.Name = "_lblActivePlansCaption";
            _lblActivePlansCaption.Size = new System.Drawing.Size(87, 17);
            _lblActivePlansCaption.TabIndex = 1;
            _lblActivePlansCaption.Text = "Aktif Görevler";
            // 
            // _lblActivePlansValue
            // 
            _lblActivePlansValue.AutoSize = true;
            _lblActivePlansValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            _lblActivePlansValue.Location = new System.Drawing.Point(52, 29);
            _lblActivePlansValue.Name = "_lblActivePlansValue";
            _lblActivePlansValue.Size = new System.Drawing.Size(33, 37);
            _lblActivePlansValue.TabIndex = 2;
            _lblActivePlansValue.Text = "0";
            // 
            // _tabPlans
            // 
            _tabPlans.Controls.Add(_splitPlans);
            _tabPlans.Controls.Add(_toolStrip);
            _tabPlans.Location = new System.Drawing.Point(4, 40);
            _tabPlans.Name = "_tabPlans";
            _tabPlans.Size = new System.Drawing.Size(1016, 705);
            _tabPlans.TabIndex = 1;
            _tabPlans.Text = "Görevler";
            // 
            // _splitPlans
            // 
            _splitPlans.Dock = System.Windows.Forms.DockStyle.Fill;
            _splitPlans.Location = new System.Drawing.Point(0, 37);
            _splitPlans.Name = "_splitPlans";
            _splitPlans.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // _splitPlans.Panel1
            // 
            _splitPlans.Panel1.Controls.Add(_dgvPlans);
            _splitPlans.Panel1.Controls.Add(_statusStripPlans);
            // 
            // _splitPlans.Panel2
            // 
            _splitPlans.Panel2.Controls.Add(_tlpBackup);
            _splitPlans.Panel2.Controls.Add(_flpBackupButtons);
            _splitPlans.Size = new System.Drawing.Size(1016, 668);
            _splitPlans.SplitterDistance = 474;
            _splitPlans.SplitterWidth = 7;
            _splitPlans.TabIndex = 0;
            // 
            // _dgvPlans
            // 
            _dgvPlans.AllowUserToAddRows = false;
            _dgvPlans.AllowUserToDeleteRows = false;
            _dgvPlans.AllowUserToResizeRows = false;
            _dgvPlans.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            _dgvPlans.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvPlans.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvPlans.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvPlans.ColumnHeadersHeight = 38;
            _dgvPlans.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvPlans.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _colEnabled, _colPlanName, _colStrategy, _colDatabases, _colSchedule, _colCloudTargets, _colCreatedAt, _colStatus, _colProgress, _colNextRun });
            _dgvPlans.ContextMenuStrip = _ctxPlan;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            _dgvPlans.DefaultCellStyle = dataGridViewCellStyle1;
            _dgvPlans.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvPlans.EnableHeadersVisualStyles = false;
            _dgvPlans.Location = new System.Drawing.Point(0, 0);
            _dgvPlans.MultiSelect = false;
            _dgvPlans.Name = "_dgvPlans";
            _dgvPlans.ReadOnly = true;
            _dgvPlans.RowHeadersVisible = false;
            _dgvPlans.RowTemplate.Height = 36;
            _dgvPlans.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvPlans.Size = new System.Drawing.Size(1016, 452);
            _dgvPlans.TabIndex = 0;
            _dgvPlans.CellDoubleClick += OnPlanGridDoubleClick;
            _dgvPlans.ColumnHeaderMouseClick += OnPlanGridColumnHeaderClick;
            _dgvPlans.SelectionChanged += OnPlanGridSelectionChanged;
            // 
            // _colEnabled
            // 
            _colEnabled.FillWeight = 30F;
            _colEnabled.HeaderText = "Aktif";
            _colEnabled.Name = "_colEnabled";
            _colEnabled.ReadOnly = true;
            // 
            // _colPlanName
            // 
            _colPlanName.HeaderText = "Görev Adı";
            _colPlanName.Name = "_colPlanName";
            _colPlanName.ReadOnly = true;
            // 
            // _colStrategy
            // 
            _colStrategy.FillWeight = 70F;
            _colStrategy.HeaderText = "Strateji";
            _colStrategy.Name = "_colStrategy";
            _colStrategy.ReadOnly = true;
            // 
            // _colDatabases
            // 
            _colDatabases.HeaderText = "Veritabanları";
            _colDatabases.Name = "_colDatabases";
            _colDatabases.ReadOnly = true;
            // 
            // _colSchedule
            // 
            _colSchedule.FillWeight = 80F;
            _colSchedule.HeaderText = "Zamanlama";
            _colSchedule.Name = "_colSchedule";
            _colSchedule.ReadOnly = true;
            // 
            // _colCloudTargets
            // 
            _colCloudTargets.FillWeight = 60F;
            _colCloudTargets.HeaderText = "Depolama";
            _colCloudTargets.Name = "_colCloudTargets";
            _colCloudTargets.ReadOnly = true;
            // 
            // _colCreatedAt
            // 
            _colCreatedAt.FillWeight = 60F;
            _colCreatedAt.HeaderText = "Oluşturulma";
            _colCreatedAt.Name = "_colCreatedAt";
            _colCreatedAt.ReadOnly = true;
            // 
            // _colStatus
            // 
            _colStatus.FillWeight = 90F;
            _colStatus.HeaderText = "Son Çalışma";
            _colStatus.Name = "_colStatus";
            _colStatus.ReadOnly = true;
            // 
            // _colProgress
            // 
            _colProgress.FillWeight = 65F;
            _colProgress.HeaderText = "İlerleme";
            _colProgress.Name = "_colProgress";
            _colProgress.ReadOnly = true;
            // 
            // _colNextRun
            // 
            _colNextRun.FillWeight = 90F;
            _colNextRun.HeaderText = "Sonraki Çalışma";
            _colNextRun.Name = "_colNextRun";
            _colNextRun.ReadOnly = true;
            // 
            // _ctxPlan
            // 
            _ctxPlan.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _ctxBackupNow, _ctxStopBackup, _ctxSep1, _ctxEditPlan, _ctxDeletePlan, _ctxSep2, _ctxExportPlan, _ctxSep3, _ctxViewPlanLogs, _ctxSep4, _ctxRestore });
            _ctxPlan.Name = "_ctxPlan";
            _ctxPlan.Size = new System.Drawing.Size(181, 182);
            _ctxPlan.Opening += OnContextMenuOpening;
            // 
            // _ctxBackupNow
            // 
            _ctxBackupNow.Name = "_ctxBackupNow";
            _ctxBackupNow.Size = new System.Drawing.Size(180, 22);
            _ctxBackupNow.Text = "Şimdi Yedekle";
            _ctxBackupNow.Click += OnCtxBackupNowClick;
            // 
            // _ctxStopBackup
            // 
            _ctxStopBackup.Enabled = false;
            _ctxStopBackup.Name = "_ctxStopBackup";
            _ctxStopBackup.Size = new System.Drawing.Size(180, 22);
            _ctxStopBackup.Text = "Yedeklemeyi Durdur";
            _ctxStopBackup.Click += OnCtxStopBackupClick;
            // 
            // _ctxSep1
            // 
            _ctxSep1.Name = "_ctxSep1";
            _ctxSep1.Size = new System.Drawing.Size(177, 6);
            // 
            // _ctxEditPlan
            // 
            _ctxEditPlan.Name = "_ctxEditPlan";
            _ctxEditPlan.Size = new System.Drawing.Size(180, 22);
            _ctxEditPlan.Text = "Düzenle";
            _ctxEditPlan.Click += OnEditPlanClick;
            // 
            // _ctxDeletePlan
            // 
            _ctxDeletePlan.Name = "_ctxDeletePlan";
            _ctxDeletePlan.Size = new System.Drawing.Size(180, 22);
            _ctxDeletePlan.Text = "Sil";
            _ctxDeletePlan.Click += OnDeletePlanClick;
            // 
            // _ctxSep2
            // 
            _ctxSep2.Name = "_ctxSep2";
            _ctxSep2.Size = new System.Drawing.Size(177, 6);
            // 
            // _ctxExportPlan
            // 
            _ctxExportPlan.Name = "_ctxExportPlan";
            _ctxExportPlan.Size = new System.Drawing.Size(180, 22);
            _ctxExportPlan.Text = "Dışa Aktar";
            _ctxExportPlan.Click += OnExportPlanClick;
            // 
            // _ctxSep3
            // 
            _ctxSep3.Name = "_ctxSep3";
            _ctxSep3.Size = new System.Drawing.Size(177, 6);
            // 
            // _ctxViewPlanLogs
            // 
            _ctxViewPlanLogs.Name = "_ctxViewPlanLogs";
            _ctxViewPlanLogs.Size = new System.Drawing.Size(180, 22);
            _ctxViewPlanLogs.Text = "Görev Logları";
            _ctxViewPlanLogs.Click += OnCtxViewPlanLogsClick;
            // 
            // _ctxSep4
            // 
            _ctxSep4.Name = "_ctxSep4";
            _ctxSep4.Size = new System.Drawing.Size(177, 6);
            // 
            // _ctxRestore
            // 
            _ctxRestore.Name = "_ctxRestore";
            _ctxRestore.Size = new System.Drawing.Size(180, 22);
            _ctxRestore.Text = "Geri Yükle...";
            _ctxRestore.Click += OnCtxRestoreClick;
            // 
            // _statusStripPlans
            // 
            _statusStripPlans.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _tslPlanCount });
            _statusStripPlans.Location = new System.Drawing.Point(0, 452);
            _statusStripPlans.Name = "_statusStripPlans";
            _statusStripPlans.Size = new System.Drawing.Size(1016, 22);
            _statusStripPlans.SizingGrip = false;
            _statusStripPlans.TabIndex = 1;
            // 
            // _tslPlanCount
            // 
            _tslPlanCount.Name = "_tslPlanCount";
            _tslPlanCount.Size = new System.Drawing.Size(89, 17);
            _tslPlanCount.Text = "Toplam 0 görev";
            // 
            // _tlpBackup
            // 
            _tlpBackup.ColumnCount = 2;
            _tlpBackup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _tlpBackup.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpBackup.Controls.Add(_lblBackupType, 0, 0);
            _tlpBackup.Controls.Add(_cmbBackupType, 1, 0);
            _tlpBackup.Controls.Add(_lblBackupStatus, 0, 1);
            _tlpBackup.Controls.Add(_progressBar, 0, 2);
            _tlpBackup.Controls.Add(_txtBackupLog, 0, 3);
            _tlpBackup.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpBackup.Location = new System.Drawing.Point(0, 0);
            _tlpBackup.Name = "_tlpBackup";
            _tlpBackup.Padding = new System.Windows.Forms.Padding(8, 5, 8, 0);
            _tlpBackup.RowCount = 4;
            _tlpBackup.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpBackup.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpBackup.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpBackup.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpBackup.Size = new System.Drawing.Size(1016, 125);
            _tlpBackup.TabIndex = 0;
            // 
            // _lblBackupType
            // 
            _lblBackupType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblBackupType.AutoSize = true;
            _lblBackupType.Location = new System.Drawing.Point(11, 19);
            _lblBackupType.Margin = new System.Windows.Forms.Padding(3, 7, 8, 3);
            _lblBackupType.Name = "_lblBackupType";
            _lblBackupType.Size = new System.Drawing.Size(75, 17);
            _lblBackupType.TabIndex = 0;
            _lblBackupType.Text = "Yedek Türü:";
            // 
            // _cmbBackupType
            // 
            _cmbBackupType.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            _cmbBackupType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbBackupType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _cmbBackupType.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _cmbBackupType.ItemHeight = 28;
            _cmbBackupType.Items.AddRange(new object[] { "Full (Tam)", "Differential (Fark)", "Incremental (Artırımlı)" });
            _cmbBackupType.Location = new System.Drawing.Point(97, 10);
            _cmbBackupType.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            _cmbBackupType.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbBackupType.Name = "_cmbBackupType";
            _cmbBackupType.Size = new System.Drawing.Size(250, 34);
            _cmbBackupType.TabIndex = 1;
            // 
            // _lblBackupStatus
            // 
            _lblBackupStatus.AutoSize = true;
            _tlpBackup.SetColumnSpan(_lblBackupStatus, 2);
            _lblBackupStatus.Location = new System.Drawing.Point(11, 56);
            _lblBackupStatus.Margin = new System.Windows.Forms.Padding(3, 9, 3, 3);
            _lblBackupStatus.Name = "_lblBackupStatus";
            _lblBackupStatus.Size = new System.Drawing.Size(169, 17);
            _lblBackupStatus.TabIndex = 2;
            _lblBackupStatus.Text = "Hazır — listeden plan seçin.";
            // 
            // _progressBar
            // 
            _progressBar.BackColor = System.Drawing.Color.Transparent;
            _tlpBackup.SetColumnSpan(_progressBar, 2);
            _progressBar.CornerRadius = 6;
            _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
            _progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            _progressBar.Location = new System.Drawing.Point(11, 81);
            _progressBar.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            _progressBar.Maximum = 100;
            _progressBar.Minimum = 0;
            _progressBar.Name = "_progressBar";
            _progressBar.ShowPercentage = false;
            _progressBar.Size = new System.Drawing.Size(994, 25);
            _progressBar.TabIndex = 3;
            _progressBar.Value = 0;
            // 
            // _txtBackupLog
            // 
            _txtBackupLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _tlpBackup.SetColumnSpan(_txtBackupLog, 2);
            _txtBackupLog.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtBackupLog.Font = new System.Drawing.Font("Cascadia Mono", 9F);
            _txtBackupLog.Location = new System.Drawing.Point(11, 116);
            _txtBackupLog.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            _txtBackupLog.Name = "_txtBackupLog";
            _txtBackupLog.ReadOnly = true;
            _txtBackupLog.Size = new System.Drawing.Size(994, 6);
            _txtBackupLog.TabIndex = 4;
            _txtBackupLog.Text = "";
            // 
            // _flpBackupButtons
            // 
            _flpBackupButtons.AutoSize = true;
            _flpBackupButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _flpBackupButtons.Controls.Add(_btnCancelBackup);
            _flpBackupButtons.Controls.Add(_btnStart);
            _flpBackupButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            _flpBackupButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _flpBackupButtons.Location = new System.Drawing.Point(0, 125);
            _flpBackupButtons.Name = "_flpBackupButtons";
            _flpBackupButtons.Padding = new System.Windows.Forms.Padding(0, 5, 8, 5);
            _flpBackupButtons.Size = new System.Drawing.Size(1016, 62);
            _flpBackupButtons.TabIndex = 1;
            // 
            // _btnCancelBackup
            // 
            _btnCancelBackup.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancelBackup.CornerRadius = 6;
            _btnCancelBackup.Enabled = false;
            _btnCancelBackup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnCancelBackup.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnCancelBackup.IconSymbol = "";
            _btnCancelBackup.Location = new System.Drawing.Point(894, 10);
            _btnCancelBackup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            _btnCancelBackup.Name = "_btnCancelBackup";
            _btnCancelBackup.Size = new System.Drawing.Size(110, 41);
            _btnCancelBackup.TabIndex = 0;
            _btnCancelBackup.Text = "■ İptal Et";
            _btnCancelBackup.Click += OnCancelBackupClick;
            // 
            // _btnStart
            // 
            _btnStart.AutoSize = true;
            _btnStart.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnStart.CornerRadius = 6;
            _btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnStart.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnStart.IconSymbol = "";
            _btnStart.Location = new System.Drawing.Point(722, 10);
            _btnStart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            _btnStart.Name = "_btnStart";
            _btnStart.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            _btnStart.Size = new System.Drawing.Size(164, 42);
            _btnStart.TabIndex = 1;
            _btnStart.Text = "▶ Yedeklemeyi Başlat";
            _btnStart.Click += OnStartBackupClick;
            // 
            // _toolStrip
            // 
            _toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _tsbNew, _tsbEdit, _tsbDelete, _tsSep1, _tsbExport, _tsbImport, _tsSep2, _tsbRefreshPlans, _tsSep3, _tslSearchLabel, _tstSearch });
            _toolStrip.Location = new System.Drawing.Point(0, 0);
            _toolStrip.Name = "_toolStrip";
            _toolStrip.Padding = new System.Windows.Forms.Padding(12, 7, 12, 7);
            _toolStrip.Size = new System.Drawing.Size(1016, 37);
            _toolStrip.TabIndex = 1;
            // 
            // _tsbNew
            // 
            _tsbNew.Name = "_tsbNew";
            _tsbNew.Size = new System.Drawing.Size(67, 20);
            _tsbNew.Text = "Yeni Görev";
            _tsbNew.Click += OnNewPlanClick;
            // 
            // _tsbEdit
            // 
            _tsbEdit.Name = "_tsbEdit";
            _tsbEdit.Size = new System.Drawing.Size(53, 20);
            _tsbEdit.Text = "Düzenle";
            _tsbEdit.Click += OnEditPlanClick;
            // 
            // _tsbDelete
            // 
            _tsbDelete.Name = "_tsbDelete";
            _tsbDelete.Size = new System.Drawing.Size(23, 20);
            _tsbDelete.Text = "Sil";
            _tsbDelete.Click += OnDeletePlanClick;
            // 
            // _tsSep1
            // 
            _tsSep1.Name = "_tsSep1";
            _tsSep1.Size = new System.Drawing.Size(6, 23);
            // 
            // _tsbExport
            // 
            _tsbExport.Name = "_tsbExport";
            _tsbExport.Size = new System.Drawing.Size(64, 20);
            _tsbExport.Text = "Dışa Aktar";
            _tsbExport.Click += OnExportPlanClick;
            // 
            // _tsbImport
            // 
            _tsbImport.Name = "_tsbImport";
            _tsbImport.Size = new System.Drawing.Size(57, 20);
            _tsbImport.Text = "İçe Aktar";
            _tsbImport.Click += OnImportPlanClick;
            // 
            // _tsSep2
            // 
            _tsSep2.Name = "_tsSep2";
            _tsSep2.Size = new System.Drawing.Size(6, 23);
            // 
            // _tsbRefreshPlans
            // 
            _tsbRefreshPlans.Name = "_tsbRefreshPlans";
            _tsbRefreshPlans.Size = new System.Drawing.Size(42, 20);
            _tsbRefreshPlans.Text = "Yenile";
            _tsbRefreshPlans.Click += OnRefreshPlansClick;
            // 
            // _tsSep3
            // 
            _tsSep3.Name = "_tsSep3";
            _tsSep3.Size = new System.Drawing.Size(6, 23);
            // 
            // _tslSearchLabel
            // 
            _tslSearchLabel.Margin = new System.Windows.Forms.Padding(12, 0, 4, 0);
            _tslSearchLabel.Name = "_tslSearchLabel";
            _tslSearchLabel.Size = new System.Drawing.Size(28, 23);
            _tslSearchLabel.Text = "Ara:";
            // 
            // _tstSearch
            // 
            _tstSearch.Name = "_tstSearch";
            _tstSearch.Size = new System.Drawing.Size(100, 23);
            _tstSearch.TextChanged += OnPlanSearchTextChanged;
            // 
            // _tabLogs
            // 
            _tabLogs.Controls.Add(_tlpLogsMain);
            _tabLogs.Controls.Add(_statusStripLogs);
            _tabLogs.Location = new System.Drawing.Point(4, 40);
            _tabLogs.Name = "_tabLogs";
            _tabLogs.Size = new System.Drawing.Size(192, 69);
            _tabLogs.TabIndex = 2;
            _tabLogs.Text = "Loglar";
            // 
            // _tlpLogsMain
            // 
            _tlpLogsMain.ColumnCount = 1;
            _tlpLogsMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpLogsMain.Controls.Add(_tlpLogToolbar, 0, 0);
            _tlpLogsMain.Controls.Add(_dgvLogs, 0, 1);
            _tlpLogsMain.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpLogsMain.Location = new System.Drawing.Point(0, 0);
            _tlpLogsMain.Name = "_tlpLogsMain";
            _tlpLogsMain.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            _tlpLogsMain.RowCount = 2;
            _tlpLogsMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpLogsMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpLogsMain.Size = new System.Drawing.Size(192, 47);
            _tlpLogsMain.TabIndex = 0;
            // 
            // _tlpLogToolbar
            // 
            _tlpLogToolbar.AutoSize = true;
            _tlpLogToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _tlpLogToolbar.ColumnCount = 8;
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _tlpLogToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _tlpLogToolbar.Controls.Add(_lblLogFile, 0, 0);
            _tlpLogToolbar.Controls.Add(_cmbLogFile, 1, 0);
            _tlpLogToolbar.Controls.Add(_lblLevel, 2, 0);
            _tlpLogToolbar.Controls.Add(_cmbLevel, 3, 0);
            _tlpLogToolbar.Controls.Add(_chkAutoTail, 4, 0);
            _tlpLogToolbar.Controls.Add(_lblLogSearch, 0, 1);
            _tlpLogToolbar.Controls.Add(_txtLogSearch, 1, 1);
            _tlpLogToolbar.Controls.Add(_lblLogPlan, 2, 1);
            _tlpLogToolbar.Controls.Add(_cmbLogPlan, 3, 1);
            _tlpLogToolbar.Controls.Add(_btnClearLogFilter, 4, 1);
            _tlpLogToolbar.Controls.Add(_btnLogRefresh, 6, 1);
            _tlpLogToolbar.Controls.Add(_btnLogExport, 7, 1);
            _tlpLogToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpLogToolbar.Location = new System.Drawing.Point(7, 8);
            _tlpLogToolbar.Name = "_tlpLogToolbar";
            _tlpLogToolbar.RowCount = 2;
            _tlpLogToolbar.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpLogToolbar.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpLogToolbar.Size = new System.Drawing.Size(178, 93);
            _tlpLogToolbar.TabIndex = 0;
            // 
            // _lblLogFile
            // 
            _lblLogFile.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLogFile.AutoSize = true;
            _lblLogFile.Location = new System.Drawing.Point(3, 14);
            _lblLogFile.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _lblLogFile.Name = "_lblLogFile";
            _lblLogFile.Size = new System.Drawing.Size(47, 17);
            _lblLogFile.TabIndex = 0;
            _lblLogFile.Text = "Dosya:";
            // 
            // _cmbLogFile
            // 
            _cmbLogFile.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLogFile.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            _cmbLogFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLogFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _cmbLogFile.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _cmbLogFile.ItemHeight = 28;
            _cmbLogFile.Location = new System.Drawing.Point(56, 5);
            _cmbLogFile.Margin = new System.Windows.Forms.Padding(3, 5, 8, 3);
            _cmbLogFile.Name = "_cmbLogFile";
            _cmbLogFile.Size = new System.Drawing.Size(169, 34);
            _cmbLogFile.TabIndex = 1;
            _cmbLogFile.SelectedIndexChanged += OnLogFileChanged;
            // 
            // _lblLevel
            // 
            _lblLevel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLevel.AutoSize = true;
            _lblLevel.Location = new System.Drawing.Point(236, 14);
            _lblLevel.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _lblLevel.Name = "_lblLevel";
            _lblLevel.Size = new System.Drawing.Size(51, 17);
            _lblLevel.TabIndex = 2;
            _lblLevel.Text = "Seviye:";
            // 
            // _cmbLevel
            // 
            _cmbLevel.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLevel.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            _cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLevel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _cmbLevel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _cmbLevel.ItemHeight = 28;
            _cmbLevel.Location = new System.Drawing.Point(293, 5);
            _cmbLevel.Margin = new System.Windows.Forms.Padding(3, 5, 8, 3);
            _cmbLevel.Name = "_cmbLevel";
            _cmbLevel.Size = new System.Drawing.Size(139, 34);
            _cmbLevel.TabIndex = 3;
            _cmbLevel.SelectedIndexChanged += OnLevelFilterChanged;
            // 
            // _chkAutoTail
            // 
            _chkAutoTail.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _chkAutoTail.AutoSize = true;
            _chkAutoTail.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _chkAutoTail.Location = new System.Drawing.Point(448, 12);
            _chkAutoTail.Margin = new System.Windows.Forms.Padding(8, 7, 3, 3);
            _chkAutoTail.Name = "_chkAutoTail";
            _chkAutoTail.Size = new System.Drawing.Size(114, 21);
            _chkAutoTail.TabIndex = 4;
            _chkAutoTail.Text = "Otomatik Takip";
            _chkAutoTail.CheckedChanged += OnAutoTailToggle;
            // 
            // _lblLogSearch
            // 
            _lblLogSearch.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLogSearch.AutoSize = true;
            _lblLogSearch.Location = new System.Drawing.Point(3, 61);
            _lblLogSearch.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _lblLogSearch.Name = "_lblLogSearch";
            _lblLogSearch.Size = new System.Drawing.Size(47, 17);
            _lblLogSearch.TabIndex = 5;
            _lblLogSearch.Text = "Ara:";
            // 
            // _txtLogSearch
            // 
            _txtLogSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtLogSearch.Location = new System.Drawing.Point(56, 47);
            _txtLogSearch.Margin = new System.Windows.Forms.Padding(3, 5, 8, 7);
            _txtLogSearch.Name = "_txtLogSearch";
            _txtLogSearch.Size = new System.Drawing.Size(169, 24);
            _txtLogSearch.TabIndex = 6;
            _txtLogSearch.TextChanged += OnLogSearchTextChanged;
            // 
            // _lblLogPlan
            // 
            _lblLogPlan.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLogPlan.AutoSize = true;
            _lblLogPlan.Location = new System.Drawing.Point(241, 59);
            _lblLogPlan.Margin = new System.Windows.Forms.Padding(8, 7, 3, 7);
            _lblLogPlan.Name = "_lblLogPlan";
            _lblLogPlan.Size = new System.Drawing.Size(46, 17);
            _lblLogPlan.TabIndex = 7;
            _lblLogPlan.Text = "Görev:";
            // 
            // _cmbLogPlan
            // 
            _cmbLogPlan.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLogPlan.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            _cmbLogPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLogPlan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _cmbLogPlan.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _cmbLogPlan.ItemHeight = 28;
            _cmbLogPlan.Location = new System.Drawing.Point(293, 47);
            _cmbLogPlan.Margin = new System.Windows.Forms.Padding(3, 5, 8, 7);
            _cmbLogPlan.Name = "_cmbLogPlan";
            _cmbLogPlan.Size = new System.Drawing.Size(139, 34);
            _cmbLogPlan.TabIndex = 8;
            _cmbLogPlan.SelectedIndexChanged += OnLogPlanFilterChanged;
            // 
            // _btnClearLogFilter
            // 
            _btnClearLogFilter.ButtonStyle = Theme.ModernButtonStyle.Ghost;
            _btnClearLogFilter.CornerRadius = 6;
            _btnClearLogFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnClearLogFilter.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnClearLogFilter.IconSymbol = "";
            _btnClearLogFilter.Location = new System.Drawing.Point(443, 45);
            _btnClearLogFilter.Margin = new System.Windows.Forms.Padding(3, 3, 3, 7);
            _btnClearLogFilter.Name = "_btnClearLogFilter";
            _btnClearLogFilter.Size = new System.Drawing.Size(120, 41);
            _btnClearLogFilter.TabIndex = 9;
            _btnClearLogFilter.Text = "Temizle";
            _btnClearLogFilter.Click += OnClearLogFilterClick;
            // 
            // _btnLogRefresh
            // 
            _btnLogRefresh.AutoSize = true;
            _btnLogRefresh.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnLogRefresh.CornerRadius = 6;
            _btnLogRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnLogRefresh.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnLogRefresh.IconSymbol = "";
            _btnLogRefresh.Location = new System.Drawing.Point(-71, 45);
            _btnLogRefresh.Margin = new System.Windows.Forms.Padding(3, 3, 3, 7);
            _btnLogRefresh.Name = "_btnLogRefresh";
            _btnLogRefresh.Size = new System.Drawing.Size(120, 41);
            _btnLogRefresh.TabIndex = 10;
            _btnLogRefresh.Text = "↻ Yenile";
            _btnLogRefresh.Click += OnLogRefreshClick;
            // 
            // _btnLogExport
            // 
            _btnLogExport.AutoSize = true;
            _btnLogExport.ButtonStyle = Theme.ModernButtonStyle.Ghost;
            _btnLogExport.CornerRadius = 6;
            _btnLogExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnLogExport.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnLogExport.IconSymbol = "";
            _btnLogExport.Location = new System.Drawing.Point(55, 45);
            _btnLogExport.Margin = new System.Windows.Forms.Padding(3, 3, 3, 7);
            _btnLogExport.Name = "_btnLogExport";
            _btnLogExport.Size = new System.Drawing.Size(120, 41);
            _btnLogExport.TabIndex = 11;
            _btnLogExport.Text = "💾 Dışa Aktar";
            _btnLogExport.Click += OnLogExportClick;
            // 
            // _dgvLogs
            // 
            _dgvLogs.AllowUserToAddRows = false;
            _dgvLogs.AllowUserToDeleteRows = false;
            _dgvLogs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            _dgvLogs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvLogs.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvLogs.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvLogs.ColumnHeadersHeight = 36;
            _dgvLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvLogs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { _colTimestamp, _colLevel, _colMessage });
            _dgvLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvLogs.EnableHeadersVisualStyles = false;
            _dgvLogs.Font = new System.Drawing.Font("Consolas", 9F);
            _dgvLogs.Location = new System.Drawing.Point(7, 107);
            _dgvLogs.Name = "_dgvLogs";
            _dgvLogs.ReadOnly = true;
            _dgvLogs.RowHeadersVisible = false;
            _dgvLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvLogs.Size = new System.Drawing.Size(178, 1);
            _dgvLogs.TabIndex = 1;
            _dgvLogs.VirtualMode = true;
            // 
            // _colTimestamp
            // 
            _colTimestamp.HeaderText = "Zaman";
            _colTimestamp.Name = "_colTimestamp";
            _colTimestamp.ReadOnly = true;
            _colTimestamp.Width = 160;
            // 
            // _colLevel
            // 
            _colLevel.HeaderText = "Seviye";
            _colLevel.Name = "_colLevel";
            _colLevel.ReadOnly = true;
            _colLevel.Width = 55;
            // 
            // _colMessage
            // 
            _colMessage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            _colMessage.DefaultCellStyle = dataGridViewCellStyle2;
            _colMessage.HeaderText = "Mesaj";
            _colMessage.Name = "_colMessage";
            _colMessage.ReadOnly = true;
            // 
            // _statusStripLogs
            // 
            _statusStripLogs.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _tslLogTotal, _tslLogFiltered });
            _statusStripLogs.Location = new System.Drawing.Point(0, 47);
            _statusStripLogs.Name = "_statusStripLogs";
            _statusStripLogs.Size = new System.Drawing.Size(192, 22);
            _statusStripLogs.SizingGrip = false;
            _statusStripLogs.TabIndex = 1;
            // 
            // _tslLogTotal
            // 
            _tslLogTotal.Name = "_tslLogTotal";
            _tslLogTotal.Size = new System.Drawing.Size(102, 17);
            _tslLogTotal.Spring = true;
            _tslLogTotal.Text = "0 kayıt";
            _tslLogTotal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tslLogFiltered
            // 
            _tslLogFiltered.Name = "_tslLogFiltered";
            _tslLogFiltered.Size = new System.Drawing.Size(75, 17);
            _tslLogFiltered.Text = "0 gösteriliyor";
            _tslLogFiltered.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _tabSettings
            // 
            _tabSettings.Controls.Add(_tlpSettingsOuter);
            _tabSettings.Controls.Add(_flpSettingsButtons);
            _tabSettings.Location = new System.Drawing.Point(4, 40);
            _tabSettings.Name = "_tabSettings";
            _tabSettings.Size = new System.Drawing.Size(1016, 705);
            _tabSettings.TabIndex = 3;
            _tabSettings.Text = "Ayarlar";
            // 
            // _tlpSettingsOuter
            // 
            _tlpSettingsOuter.ColumnCount = 1;
            _tlpSettingsOuter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSettingsOuter.Controls.Add(_tabSettings2, 0, 0);
            _tlpSettingsOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpSettingsOuter.Location = new System.Drawing.Point(0, 0);
            _tlpSettingsOuter.Name = "_tlpSettingsOuter";
            _tlpSettingsOuter.RowCount = 1;
            _tlpSettingsOuter.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSettingsOuter.Size = new System.Drawing.Size(1016, 640);
            _tlpSettingsOuter.TabIndex = 0;
            // 
            // _tabSettings2
            // 
            _tabSettings2.Controls.Add(_tabGeneral);
            _tabSettings2.Controls.Add(_tabSmtp);
            _tabSettings2.Controls.Add(_tabSecurity);
            _tabSettings2.Dock = System.Windows.Forms.DockStyle.Fill;
            _tabSettings2.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            _tabSettings2.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _tabSettings2.IndicatorHeight = 3;
            _tabSettings2.ItemSize = new System.Drawing.Size(120, 36);
            _tabSettings2.Location = new System.Drawing.Point(3, 3);
            _tabSettings2.Name = "_tabSettings2";
            _tabSettings2.Padding = new System.Drawing.Point(10, 5);
            _tabSettings2.SelectedIndex = 0;
            _tabSettings2.Size = new System.Drawing.Size(1010, 634);
            _tabSettings2.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            _tabSettings2.TabIndex = 0;
            // 
            // _tabGeneral
            // 
            _tabGeneral.Controls.Add(_tlpGeneral);
            _tabGeneral.Location = new System.Drawing.Point(4, 40);
            _tabGeneral.Name = "_tabGeneral";
            _tabGeneral.Padding = new System.Windows.Forms.Padding(8, 9, 8, 9);
            _tabGeneral.Size = new System.Drawing.Size(1002, 590);
            _tabGeneral.TabIndex = 0;
            _tabGeneral.Text = "Genel";
            // 
            // _tlpGeneral
            // 
            _tlpGeneral.ColumnCount = 3;
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            _tlpGeneral.Controls.Add(_lblLanguage, 0, 0);
            _tlpGeneral.Controls.Add(_cmbLanguage, 1, 0);
            _tlpGeneral.Controls.Add(_chkStartWithWindows, 1, 1);
            _tlpGeneral.Controls.Add(_chkMinimizeToTray, 1, 2);
            _tlpGeneral.Controls.Add(_lblDefaultBackupPath, 0, 3);
            _tlpGeneral.Controls.Add(_txtDefaultBackupPath, 1, 3);
            _tlpGeneral.Controls.Add(_btnBrowseBackupPath, 2, 3);
            _tlpGeneral.Controls.Add(_lblLogRetention, 0, 4);
            _tlpGeneral.Controls.Add(_nudLogRetention, 1, 4);
            _tlpGeneral.Controls.Add(_lblLogRetentionSuffix, 2, 4);
            _tlpGeneral.Controls.Add(_lblHistoryRetention, 0, 5);
            _tlpGeneral.Controls.Add(_nudHistoryRetention, 1, 5);
            _tlpGeneral.Controls.Add(_lblHistoryRetentionSuffix, 2, 5);
            _tlpGeneral.Controls.Add(_lblTheme, 0, 6);
            _tlpGeneral.Controls.Add(_cmbTheme, 1, 6);
            _tlpGeneral.Controls.Add(_lblLogColorScheme, 0, 7);
            _tlpGeneral.Controls.Add(_cmbLogColorScheme, 1, 7);
            _tlpGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpGeneral.Location = new System.Drawing.Point(8, 9);
            _tlpGeneral.Name = "_tlpGeneral";
            _tlpGeneral.RowCount = 9;
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpGeneral.Size = new System.Drawing.Size(986, 572);
            _tlpGeneral.TabIndex = 0;
            // 
            // _lblLanguage
            // 
            _lblLanguage.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLanguage.AutoSize = true;
            _lblLanguage.Location = new System.Drawing.Point(3, 16);
            _lblLanguage.Margin = new System.Windows.Forms.Padding(3, 9, 8, 3);
            _lblLanguage.Name = "_lblLanguage";
            _lblLanguage.Size = new System.Drawing.Size(26, 17);
            _lblLanguage.TabIndex = 0;
            _lblLanguage.Text = "Dil:";
            // 
            // _cmbLanguage
            // 
            _cmbLanguage.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            _cmbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLanguage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _cmbLanguage.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _cmbLanguage.ItemHeight = 28;
            _cmbLanguage.Items.AddRange(new object[] { "Türkçe (tr-TR)", "English (en-US)" });
            _cmbLanguage.Location = new System.Drawing.Point(156, 7);
            _cmbLanguage.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _cmbLanguage.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLanguage.Name = "_cmbLanguage";
            _cmbLanguage.Size = new System.Drawing.Size(250, 34);
            _cmbLanguage.TabIndex = 1;
            // 
            // _chkStartWithWindows
            // 
            _chkStartWithWindows.AutoSize = true;
            _chkStartWithWindows.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _chkStartWithWindows.Location = new System.Drawing.Point(156, 51);
            _chkStartWithWindows.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _chkStartWithWindows.Name = "_chkStartWithWindows";
            _chkStartWithWindows.Size = new System.Drawing.Size(179, 21);
            _chkStartWithWindows.TabIndex = 2;
            _chkStartWithWindows.Text = "Windows ile birlikte başlat";
            // 
            // _chkMinimizeToTray
            // 
            _chkMinimizeToTray.AutoSize = true;
            _chkMinimizeToTray.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _chkMinimizeToTray.Location = new System.Drawing.Point(156, 78);
            _chkMinimizeToTray.Name = "_chkMinimizeToTray";
            _chkMinimizeToTray.Size = new System.Drawing.Size(289, 21);
            _chkMinimizeToTray.TabIndex = 3;
            _chkMinimizeToTray.Text = "Simge durumuna küçüldüğünde tepside gizle";
            // 
            // _lblDefaultBackupPath
            // 
            _lblDefaultBackupPath.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblDefaultBackupPath.AutoSize = true;
            _lblDefaultBackupPath.Location = new System.Drawing.Point(3, 117);
            _lblDefaultBackupPath.Margin = new System.Windows.Forms.Padding(3, 9, 8, 3);
            _lblDefaultBackupPath.Name = "_lblDefaultBackupPath";
            _lblDefaultBackupPath.Size = new System.Drawing.Size(141, 17);
            _lblDefaultBackupPath.TabIndex = 4;
            _lblDefaultBackupPath.Text = "Varsayılan yedek dizini:";
            // 
            // _txtDefaultBackupPath
            // 
            _txtDefaultBackupPath.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtDefaultBackupPath.Location = new System.Drawing.Point(156, 109);
            _txtDefaultBackupPath.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _txtDefaultBackupPath.Name = "_txtDefaultBackupPath";
            _txtDefaultBackupPath.Size = new System.Drawing.Size(785, 24);
            _txtDefaultBackupPath.TabIndex = 5;
            // 
            // _btnBrowseBackupPath
            // 
            _btnBrowseBackupPath.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBrowseBackupPath.CornerRadius = 6;
            _btnBrowseBackupPath.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnBrowseBackupPath.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnBrowseBackupPath.IconSymbol = "";
            _btnBrowseBackupPath.Location = new System.Drawing.Point(947, 108);
            _btnBrowseBackupPath.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _btnBrowseBackupPath.Name = "_btnBrowseBackupPath";
            _btnBrowseBackupPath.Size = new System.Drawing.Size(36, 32);
            _btnBrowseBackupPath.TabIndex = 6;
            _btnBrowseBackupPath.Text = "...";
            _btnBrowseBackupPath.Click += OnBrowseBackupPath;
            // 
            // _lblLogRetention
            // 
            _lblLogRetention.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLogRetention.AutoSize = true;
            _lblLogRetention.Location = new System.Drawing.Point(3, 158);
            _lblLogRetention.Margin = new System.Windows.Forms.Padding(3, 9, 8, 3);
            _lblLogRetention.Name = "_lblLogRetention";
            _lblLogRetention.Size = new System.Drawing.Size(122, 17);
            _lblLogRetention.TabIndex = 7;
            _lblLogRetention.Text = "Log saklama süresi:";
            // 
            // _nudLogRetention
            // 
            _nudLogRetention.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _nudLogRetention.Location = new System.Drawing.Point(156, 150);
            _nudLogRetention.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _nudLogRetention.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            _nudLogRetention.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _nudLogRetention.MinimumSize = new System.Drawing.Size(60, 27);
            _nudLogRetention.Name = "_nudLogRetention";
            _nudLogRetention.Size = new System.Drawing.Size(60, 32);
            _nudLogRetention.TabIndex = 8;
            _nudLogRetention.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // _lblLogRetentionSuffix
            // 
            _lblLogRetentionSuffix.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLogRetentionSuffix.AutoSize = true;
            _lblLogRetentionSuffix.Location = new System.Drawing.Point(947, 158);
            _lblLogRetentionSuffix.Margin = new System.Windows.Forms.Padding(3, 9, 3, 3);
            _lblLogRetentionSuffix.Name = "_lblLogRetentionSuffix";
            _lblLogRetentionSuffix.Size = new System.Drawing.Size(30, 17);
            _lblLogRetentionSuffix.TabIndex = 9;
            _lblLogRetentionSuffix.Text = "gün";
            // 
            // _lblHistoryRetention
            // 
            _lblHistoryRetention.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblHistoryRetention.AutoSize = true;
            _lblHistoryRetention.Location = new System.Drawing.Point(3, 200);
            _lblHistoryRetention.Margin = new System.Windows.Forms.Padding(3, 9, 8, 3);
            _lblHistoryRetention.Name = "_lblHistoryRetention";
            _lblHistoryRetention.Size = new System.Drawing.Size(142, 17);
            _lblHistoryRetention.TabIndex = 10;
            _lblHistoryRetention.Text = "Geçmiş saklama süresi:";
            // 
            // _nudHistoryRetention
            // 
            _nudHistoryRetention.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _nudHistoryRetention.Location = new System.Drawing.Point(156, 192);
            _nudHistoryRetention.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _nudHistoryRetention.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            _nudHistoryRetention.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _nudHistoryRetention.MinimumSize = new System.Drawing.Size(60, 27);
            _nudHistoryRetention.Name = "_nudHistoryRetention";
            _nudHistoryRetention.Size = new System.Drawing.Size(60, 32);
            _nudHistoryRetention.TabIndex = 11;
            _nudHistoryRetention.Value = new decimal(new int[] { 90, 0, 0, 0 });
            // 
            // _lblHistoryRetentionSuffix
            // 
            _lblHistoryRetentionSuffix.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblHistoryRetentionSuffix.AutoSize = true;
            _lblHistoryRetentionSuffix.Location = new System.Drawing.Point(947, 200);
            _lblHistoryRetentionSuffix.Margin = new System.Windows.Forms.Padding(3, 9, 3, 3);
            _lblHistoryRetentionSuffix.Name = "_lblHistoryRetentionSuffix";
            _lblHistoryRetentionSuffix.Size = new System.Drawing.Size(30, 17);
            _lblHistoryRetentionSuffix.TabIndex = 12;
            _lblHistoryRetentionSuffix.Text = "gün";
            // 
            // _lblTheme
            // 
            _lblTheme.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblTheme.AutoSize = true;
            _lblTheme.Location = new System.Drawing.Point(3, 243);
            _lblTheme.Margin = new System.Windows.Forms.Padding(3, 9, 8, 3);
            _lblTheme.Name = "_lblTheme";
            _lblTheme.Size = new System.Drawing.Size(42, 17);
            _lblTheme.TabIndex = 13;
            _lblTheme.Text = "Tema:";
            // 
            // _cmbTheme
            // 
            _cmbTheme.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            _cmbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbTheme.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _cmbTheme.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _cmbTheme.ItemHeight = 28;
            _cmbTheme.Items.AddRange(new object[] { "Koyu (Dark)", "Açık (Light)" });
            _cmbTheme.Location = new System.Drawing.Point(156, 234);
            _cmbTheme.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _cmbTheme.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbTheme.Name = "_cmbTheme";
            _cmbTheme.Size = new System.Drawing.Size(250, 34);
            _cmbTheme.TabIndex = 14;
            // 
            // _lblLogColorScheme
            // 
            _lblLogColorScheme.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLogColorScheme.AutoSize = true;
            _lblLogColorScheme.Location = new System.Drawing.Point(3, 287);
            _lblLogColorScheme.Margin = new System.Windows.Forms.Padding(3, 9, 8, 3);
            _lblLogColorScheme.Name = "_lblLogColorScheme";
            _lblLogColorScheme.Size = new System.Drawing.Size(121, 17);
            _lblLogColorScheme.TabIndex = 15;
            _lblLogColorScheme.Text = "Log Konsol Teması:";
            // 
            // _cmbLogColorScheme
            // 
            _cmbLogColorScheme.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            _cmbLogColorScheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLogColorScheme.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _cmbLogColorScheme.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _cmbLogColorScheme.ItemHeight = 28;
            _cmbLogColorScheme.Location = new System.Drawing.Point(156, 278);
            _cmbLogColorScheme.Margin = new System.Windows.Forms.Padding(3, 7, 3, 3);
            _cmbLogColorScheme.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLogColorScheme.Name = "_cmbLogColorScheme";
            _cmbLogColorScheme.Size = new System.Drawing.Size(250, 34);
            _cmbLogColorScheme.TabIndex = 16;
            // 
            // _tabSmtp
            // 
            _tabSmtp.Controls.Add(_tlpSmtp);
            _tabSmtp.Location = new System.Drawing.Point(4, 40);
            _tabSmtp.Name = "_tabSmtp";
            _tabSmtp.Padding = new System.Windows.Forms.Padding(8, 9, 8, 9);
            _tabSmtp.Size = new System.Drawing.Size(1002, 590);
            _tabSmtp.TabIndex = 1;
            _tabSmtp.Text = "E-posta (SMTP)";
            // 
            // _tlpSmtp
            // 
            _tlpSmtp.ColumnCount = 1;
            _tlpSmtp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSmtp.Controls.Add(_lblSmtpProfilesTitle, 0, 0);
            _tlpSmtp.Controls.Add(_dgvSmtpProfiles, 0, 1);
            _tlpSmtp.Controls.Add(_flpSmtpToolbar, 0, 2);
            _tlpSmtp.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpSmtp.Location = new System.Drawing.Point(8, 9);
            _tlpSmtp.Name = "_tlpSmtp";
            _tlpSmtp.RowCount = 3;
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpSmtp.Size = new System.Drawing.Size(986, 572);
            _tlpSmtp.TabIndex = 0;
            // 
            // _lblSmtpProfilesTitle
            // 
            _lblSmtpProfilesTitle.AutoSize = true;
            _lblSmtpProfilesTitle.Location = new System.Drawing.Point(3, 5);
            _lblSmtpProfilesTitle.Margin = new System.Windows.Forms.Padding(3, 5, 3, 7);
            _lblSmtpProfilesTitle.Name = "_lblSmtpProfilesTitle";
            _lblSmtpProfilesTitle.Size = new System.Drawing.Size(462, 17);
            _lblSmtpProfilesTitle.TabIndex = 0;
            _lblSmtpProfilesTitle.Text = "Kayıtlı SMTP Profilleri — birden fazla profil ekleyip görevlerde kullanabilirsiniz:";
            // 
            // _dgvSmtpProfiles
            // 
            _dgvSmtpProfiles.AllowUserToAddRows = false;
            _dgvSmtpProfiles.AllowUserToDeleteRows = false;
            _dgvSmtpProfiles.AllowUserToResizeRows = false;
            _dgvSmtpProfiles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            _dgvSmtpProfiles.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvSmtpProfiles.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvSmtpProfiles.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvSmtpProfiles.ColumnHeadersHeight = 36;
            _dgvSmtpProfiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            _dgvSmtpProfiles.DefaultCellStyle = dataGridViewCellStyle3;
            _dgvSmtpProfiles.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvSmtpProfiles.EnableHeadersVisualStyles = false;
            _dgvSmtpProfiles.Location = new System.Drawing.Point(3, 32);
            _dgvSmtpProfiles.MultiSelect = false;
            _dgvSmtpProfiles.Name = "_dgvSmtpProfiles";
            _dgvSmtpProfiles.ReadOnly = true;
            _dgvSmtpProfiles.RowHeadersVisible = false;
            _dgvSmtpProfiles.RowTemplate.Height = 34;
            _dgvSmtpProfiles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvSmtpProfiles.Size = new System.Drawing.Size(980, 475);
            _dgvSmtpProfiles.TabIndex = 1;
            _dgvSmtpProfiles.CellDoubleClick += OnSmtpEditClick;
            // 
            // _flpSmtpToolbar
            // 
            _flpSmtpToolbar.AutoSize = true;
            _flpSmtpToolbar.Controls.Add(_btnSmtpAdd);
            _flpSmtpToolbar.Controls.Add(_btnSmtpEdit);
            _flpSmtpToolbar.Controls.Add(_btnSmtpDelete);
            _flpSmtpToolbar.Controls.Add(_btnSmtpTest);
            _flpSmtpToolbar.Location = new System.Drawing.Point(3, 513);
            _flpSmtpToolbar.Name = "_flpSmtpToolbar";
            _flpSmtpToolbar.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            _flpSmtpToolbar.Size = new System.Drawing.Size(501, 56);
            _flpSmtpToolbar.TabIndex = 2;
            _flpSmtpToolbar.WrapContents = false;
            // 
            // _btnSmtpAdd
            // 
            _btnSmtpAdd.AutoSize = true;
            _btnSmtpAdd.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSmtpAdd.CornerRadius = 6;
            _btnSmtpAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnSmtpAdd.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnSmtpAdd.IconSymbol = "";
            _btnSmtpAdd.Location = new System.Drawing.Point(0, 10);
            _btnSmtpAdd.Margin = new System.Windows.Forms.Padding(0, 5, 6, 5);
            _btnSmtpAdd.Name = "_btnSmtpAdd";
            _btnSmtpAdd.Size = new System.Drawing.Size(120, 41);
            _btnSmtpAdd.TabIndex = 0;
            _btnSmtpAdd.Text = "➕ Ekle";
            _btnSmtpAdd.Click += OnSmtpAddClick;
            // 
            // _btnSmtpEdit
            // 
            _btnSmtpEdit.AutoSize = true;
            _btnSmtpEdit.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnSmtpEdit.CornerRadius = 6;
            _btnSmtpEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnSmtpEdit.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnSmtpEdit.IconSymbol = "";
            _btnSmtpEdit.Location = new System.Drawing.Point(126, 10);
            _btnSmtpEdit.Margin = new System.Windows.Forms.Padding(0, 5, 6, 5);
            _btnSmtpEdit.Name = "_btnSmtpEdit";
            _btnSmtpEdit.Size = new System.Drawing.Size(120, 41);
            _btnSmtpEdit.TabIndex = 1;
            _btnSmtpEdit.Text = "✏ Düzenle";
            _btnSmtpEdit.Click += OnSmtpEditClick;
            // 
            // _btnSmtpDelete
            // 
            _btnSmtpDelete.AutoSize = true;
            _btnSmtpDelete.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnSmtpDelete.CornerRadius = 6;
            _btnSmtpDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnSmtpDelete.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnSmtpDelete.IconSymbol = "";
            _btnSmtpDelete.Location = new System.Drawing.Point(252, 10);
            _btnSmtpDelete.Margin = new System.Windows.Forms.Padding(0, 5, 6, 5);
            _btnSmtpDelete.Name = "_btnSmtpDelete";
            _btnSmtpDelete.Size = new System.Drawing.Size(120, 41);
            _btnSmtpDelete.TabIndex = 2;
            _btnSmtpDelete.Text = "🗑 Sil";
            _btnSmtpDelete.Click += OnSmtpDeleteClick;
            // 
            // _btnSmtpTest
            // 
            _btnSmtpTest.AutoSize = true;
            _btnSmtpTest.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnSmtpTest.CornerRadius = 6;
            _btnSmtpTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnSmtpTest.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnSmtpTest.IconSymbol = "";
            _btnSmtpTest.Location = new System.Drawing.Point(378, 10);
            _btnSmtpTest.Margin = new System.Windows.Forms.Padding(0, 5, 3, 5);
            _btnSmtpTest.Name = "_btnSmtpTest";
            _btnSmtpTest.Size = new System.Drawing.Size(120, 41);
            _btnSmtpTest.TabIndex = 3;
            _btnSmtpTest.Text = "✉ Test";
            _btnSmtpTest.Click += OnSmtpTestClick;
            // 
            // _tabSecurity
            // 
            _tabSecurity.Controls.Add(_tlpSecurity);
            _tabSecurity.Location = new System.Drawing.Point(4, 40);
            _tabSecurity.Name = "_tabSecurity";
            _tabSecurity.Padding = new System.Windows.Forms.Padding(8, 9, 8, 9);
            _tabSecurity.Size = new System.Drawing.Size(1002, 590);
            _tabSecurity.TabIndex = 2;
            _tabSecurity.Text = "Güvenlik";
            // 
            // _tlpSecurity
            // 
            _tlpSecurity.ColumnCount = 1;
            _tlpSecurity.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSecurity.Controls.Add(_lblSecurityTitle, 0, 0);
            _tlpSecurity.Controls.Add(_btnPasswordSetup, 0, 1);
            _tlpSecurity.Controls.Add(_lblSecurityInfo, 0, 2);
            _tlpSecurity.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpSecurity.Location = new System.Drawing.Point(8, 9);
            _tlpSecurity.Name = "_tlpSecurity";
            _tlpSecurity.RowCount = 4;
            _tlpSecurity.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpSecurity.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpSecurity.RowStyles.Add(new System.Windows.Forms.RowStyle());
            _tlpSecurity.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSecurity.Size = new System.Drawing.Size(986, 572);
            _tlpSecurity.TabIndex = 0;
            // 
            // _lblSecurityTitle
            // 
            _lblSecurityTitle.AutoSize = true;
            _lblSecurityTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblSecurityTitle.Location = new System.Drawing.Point(3, 5);
            _lblSecurityTitle.Margin = new System.Windows.Forms.Padding(3, 5, 3, 15);
            _lblSecurityTitle.Name = "_lblSecurityTitle";
            _lblSecurityTitle.Size = new System.Drawing.Size(116, 20);
            _lblSecurityTitle.TabIndex = 0;
            _lblSecurityTitle.Text = "Şifre Koruması";
            // 
            // _btnPasswordSetup
            // 
            _btnPasswordSetup.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnPasswordSetup.CornerRadius = 6;
            _btnPasswordSetup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnPasswordSetup.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnPasswordSetup.IconSymbol = "";
            _btnPasswordSetup.Location = new System.Drawing.Point(3, 45);
            _btnPasswordSetup.Margin = new System.Windows.Forms.Padding(3, 5, 3, 10);
            _btnPasswordSetup.Name = "_btnPasswordSetup";
            _btnPasswordSetup.Size = new System.Drawing.Size(200, 41);
            _btnPasswordSetup.TabIndex = 1;
            _btnPasswordSetup.Text = "Şifre Belirle / Değiştir";
            _btnPasswordSetup.Click += OnPasswordSetupClick;
            // 
            // _lblSecurityInfo
            // 
            _lblSecurityInfo.AutoSize = true;
            _lblSecurityInfo.Tag = "secondary";
            _lblSecurityInfo.Location = new System.Drawing.Point(3, 101);
            _lblSecurityInfo.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            _lblSecurityInfo.Name = "_lblSecurityInfo";
            _lblSecurityInfo.Size = new System.Drawing.Size(550, 34);
            _lblSecurityInfo.TabIndex = 2;
            _lblSecurityInfo.Text = "Uygulama açılışında ve kritik işlemlerde (görev silme vb.) şifre sorulmasını sağlar.\r\nŞifrenizi unutursanız güvenlik sorusu ile sıfırlayabilirsiniz.";
            // 
            // _flpSettingsButtons
            // 
            _flpSettingsButtons.AutoSize = true;
            _flpSettingsButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _flpSettingsButtons.Controls.Add(_btnCancelSettings);
            _flpSettingsButtons.Controls.Add(_btnSaveSettings);
            _flpSettingsButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            _flpSettingsButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _flpSettingsButtons.Location = new System.Drawing.Point(0, 640);
            _flpSettingsButtons.Name = "_flpSettingsButtons";
            _flpSettingsButtons.Padding = new System.Windows.Forms.Padding(0, 5, 8, 9);
            _flpSettingsButtons.Size = new System.Drawing.Size(1016, 65);
            _flpSettingsButtons.TabIndex = 1;
            // 
            // _btnCancelSettings
            // 
            _btnCancelSettings.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancelSettings.CornerRadius = 6;
            _btnCancelSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnCancelSettings.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnCancelSettings.IconSymbol = "";
            _btnCancelSettings.Location = new System.Drawing.Point(904, 10);
            _btnCancelSettings.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            _btnCancelSettings.Name = "_btnCancelSettings";
            _btnCancelSettings.Size = new System.Drawing.Size(100, 41);
            _btnCancelSettings.TabIndex = 0;
            _btnCancelSettings.Text = "İptal";
            _btnCancelSettings.Click += OnCancelSettingsClick;
            // 
            // _btnSaveSettings
            // 
            _btnSaveSettings.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSaveSettings.CornerRadius = 6;
            _btnSaveSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnSaveSettings.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnSaveSettings.IconSymbol = "";
            _btnSaveSettings.Location = new System.Drawing.Point(796, 10);
            _btnSaveSettings.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            _btnSaveSettings.Name = "_btnSaveSettings";
            _btnSaveSettings.Size = new System.Drawing.Size(100, 41);
            _btnSaveSettings.TabIndex = 1;
            _btnSaveSettings.Text = "Kaydet";
            _btnSaveSettings.Click += OnSaveSettingsClick;
            // 
            // _statusStrip
            // 
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _tslStatus, _tslVersion });
            _statusStrip.Location = new System.Drawing.Point(0, 749);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Size = new System.Drawing.Size(1024, 22);
            _statusStrip.SizingGrip = false;
            _statusStrip.TabIndex = 2;
            // 
            // _tslStatus
            // 
            _tslStatus.Name = "_tslStatus";
            _tslStatus.Size = new System.Drawing.Size(972, 17);
            _tslStatus.Spring = true;
            _tslStatus.Text = "Hazır";
            _tslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _tslVersion
            // 
            _tslVersion.Name = "_tslVersion";
            _tslVersion.Size = new System.Drawing.Size(37, 17);
            _tslVersion.Text = "v0.0.0";
            _tslVersion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1024, 771);
            Controls.Add(_tabControl);
            Controls.Add(_statusStrip);
            MinimumSize = new System.Drawing.Size(960, 720);
            Name = "MainWindow";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Koru MsSql Yedek";
            _tabControl.ResumeLayout(false);
            _tabDashboard.ResumeLayout(false);
            _pnlGrid.ResumeLayout(false);
            _pnlGrid.PerformLayout();
            _tlpCards.ResumeLayout(false);
            _cardStatus.ResumeLayout(false);
            _cardStatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_lblStatusIcon).EndInit();
            _cardNextBackup.ResumeLayout(false);
            _cardNextBackup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_lblNextIcon).EndInit();
            _cardActivePlans.ResumeLayout(false);
            _cardActivePlans.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_lblPlansIcon).EndInit();
            _tabPlans.ResumeLayout(false);
            _tabPlans.PerformLayout();
            _splitPlans.Panel1.ResumeLayout(false);
            _splitPlans.Panel1.PerformLayout();
            _splitPlans.Panel2.ResumeLayout(false);
            _splitPlans.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_splitPlans).EndInit();
            _splitPlans.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_dgvPlans).EndInit();
            _ctxPlan.ResumeLayout(false);
            _statusStripPlans.ResumeLayout(false);
            _statusStripPlans.PerformLayout();
            _tlpBackup.ResumeLayout(false);
            _tlpBackup.PerformLayout();
            _flpBackupButtons.ResumeLayout(false);
            _flpBackupButtons.PerformLayout();
            _toolStrip.ResumeLayout(false);
            _toolStrip.PerformLayout();
            _tabLogs.ResumeLayout(false);
            _tabLogs.PerformLayout();
            _tlpLogsMain.ResumeLayout(false);
            _tlpLogsMain.PerformLayout();
            _tlpLogToolbar.ResumeLayout(false);
            _tlpLogToolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvLogs).EndInit();
            _statusStripLogs.ResumeLayout(false);
            _statusStripLogs.PerformLayout();
            _tabSettings.ResumeLayout(false);
            _tabSettings.PerformLayout();
            _tlpSettingsOuter.ResumeLayout(false);
            _tabSettings2.ResumeLayout(false);
            _tabGeneral.ResumeLayout(false);
            _tlpGeneral.ResumeLayout(false);
            _tlpGeneral.PerformLayout();
            _tabSmtp.ResumeLayout(false);
            _tlpSmtp.ResumeLayout(false);
            _tlpSmtp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvSmtpProfiles).EndInit();
            _flpSmtpToolbar.ResumeLayout(false);
            _flpSmtpToolbar.PerformLayout();
            _flpSettingsButtons.ResumeLayout(false);
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
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
        private Controls.GroupedBackupListPanel _olvLastBackups;

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
        private System.Windows.Forms.TabPage _tabSecurity;
        private System.Windows.Forms.TableLayoutPanel _tlpSecurity;
        private System.Windows.Forms.Label _lblSecurityTitle;
        private Theme.ModernButton _btnPasswordSetup;
        private System.Windows.Forms.Label _lblSecurityInfo;
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
        private System.Windows.Forms.ToolTip _toolTip;
    }
}
