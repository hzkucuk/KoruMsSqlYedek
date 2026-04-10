namespace KoruMsSqlYedek.Win.Forms
{
    partial class PlanEditForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer üretilen kod

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _toolTip = new System.Windows.Forms.ToolTip(components);

            // === Wizard infrastructure ===
            _pnlStepIndicator = new System.Windows.Forms.Panel();
            _pnlContent = new System.Windows.Forms.Panel();
            _pnlNavigation = new System.Windows.Forms.Panel();
            _btnBack = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnNext = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnSave = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnCancel = new KoruMsSqlYedek.Win.Theme.ModernButton();

            // === 6 Step panels ===
            _pnlStep1 = new System.Windows.Forms.Panel();
            _pnlStep2 = new System.Windows.Forms.Panel();
            _pnlStep3 = new System.Windows.Forms.Panel();
            _pnlStep4 = new System.Windows.Forms.Panel();
            _pnlStep5 = new System.Windows.Forms.Panel();
            _pnlStep6 = new System.Windows.Forms.Panel();

            // === Step indicator labels ===
            _lblStepNum1 = new System.Windows.Forms.Label();
            _lblStepNum2 = new System.Windows.Forms.Label();
            _lblStepNum3 = new System.Windows.Forms.Label();
            _lblStepNum4 = new System.Windows.Forms.Label();
            _lblStepNum5 = new System.Windows.Forms.Label();
            _lblStepNum6 = new System.Windows.Forms.Label();
            _lblStepTitle1 = new System.Windows.Forms.Label();
            _lblStepTitle2 = new System.Windows.Forms.Label();
            _lblStepTitle3 = new System.Windows.Forms.Label();
            _lblStepTitle4 = new System.Windows.Forms.Label();
            _lblStepTitle5 = new System.Windows.Forms.Label();
            _lblStepTitle6 = new System.Windows.Forms.Label();

            // ========== STEP 1: Plan Bilgileri + SQL Bağlantı ==========
            _lblStep1Header = new System.Windows.Forms.Label();
            _lblPlanName = new System.Windows.Forms.Label();
            _txtPlanName = new System.Windows.Forms.TextBox();
            _chkEnabled = new System.Windows.Forms.CheckBox();
            _lblLocalPath = new System.Windows.Forms.Label();
            _txtLocalPath = new System.Windows.Forms.TextBox();
            _btnBrowseLocal = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _lblServer = new System.Windows.Forms.Label();
            _txtServer = new System.Windows.Forms.TextBox();
            _lblAuthMode = new System.Windows.Forms.Label();
            _cmbAuthMode = new System.Windows.Forms.ComboBox();
            _lblSqlUser = new System.Windows.Forms.Label();
            _txtSqlUser = new System.Windows.Forms.TextBox();
            _lblSqlPassword = new System.Windows.Forms.Label();
            _txtSqlPassword = new System.Windows.Forms.TextBox();
            _lblTimeout = new System.Windows.Forms.Label();
            _nudTimeout = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _chkTrustCert = new System.Windows.Forms.CheckBox();
            _btnTestSql = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _lblStep1SqlHeader = new System.Windows.Forms.Label();

            // ========== STEP 2: Kaynaklar ==========
            _lblStep2Header = new System.Windows.Forms.Label();
            _lblStep2Hint = new System.Windows.Forms.Label();
            _clbDatabases = new System.Windows.Forms.CheckedListBox();
            _btnRefreshDatabases = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _chkSelectAll = new System.Windows.Forms.CheckBox();
            _lblStep2FileHeader = new System.Windows.Forms.Label();
            _chkFileBackupEnabled = new System.Windows.Forms.CheckBox();
            _pnlFileBackup = new System.Windows.Forms.Panel();
            _lvFileSources = new System.Windows.Forms.ListView();
            _colFsName = new System.Windows.Forms.ColumnHeader();
            _colFsPath = new System.Windows.Forms.ColumnHeader();
            _colFsVss = new System.Windows.Forms.ColumnHeader();
            _colFsStatus = new System.Windows.Forms.ColumnHeader();
            _btnAddFileSource = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnEditFileSource = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnRemoveFileSource = new KoruMsSqlYedek.Win.Theme.ModernButton();

            // ========== STEP 3: Strateji + Zamanlama ==========
            _lblStep3Header = new System.Windows.Forms.Label();
            _lblStrategy = new System.Windows.Forms.Label();
            _cmbStrategy = new System.Windows.Forms.ComboBox();
            _lblFullCron = new System.Windows.Forms.Label();
            _cronFull = new KoruMsSqlYedek.Win.Controls.CronBuilderPanel();
            _lblDiffCron = new System.Windows.Forms.Label();
            _cronDiff = new KoruMsSqlYedek.Win.Controls.CronBuilderPanel();
            _lblIncrCron = new System.Windows.Forms.Label();
            _cronIncr = new KoruMsSqlYedek.Win.Controls.CronBuilderPanel();
            _lblAutoPromote = new System.Windows.Forms.Label();
            _nudAutoPromote = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _chkVerify = new System.Windows.Forms.CheckBox();
            _lblStep3FileSchedHeader = new System.Windows.Forms.Label();
            _lblStep3FileSep = new System.Windows.Forms.Label();
            _lblFileSchedule = new System.Windows.Forms.Label();
            _cronFileSchedule = new KoruMsSqlYedek.Win.Controls.CronBuilderPanel();

            // ========== STEP 4: Sıkıştırma + Saklama ==========
            _lblStep4Header = new System.Windows.Forms.Label();
            _lblAlgorithm = new System.Windows.Forms.Label();
            _cmbAlgorithm = new System.Windows.Forms.ComboBox();
            _lblLevel = new System.Windows.Forms.Label();
            _cmbLevel = new System.Windows.Forms.ComboBox();
            _lblArchivePassword = new System.Windows.Forms.Label();
            _txtArchivePassword = new System.Windows.Forms.TextBox();
            _lblStep4RetHeader = new System.Windows.Forms.Label();
            _lblRetention = new System.Windows.Forms.Label();
            _cmbRetention = new System.Windows.Forms.ComboBox();
            _lblKeepLastN = new System.Windows.Forms.Label();
            _nudKeepLastN = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _lblDeleteDays = new System.Windows.Forms.Label();
            _nudDeleteDays = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _lblRetentionTemplate = new System.Windows.Forms.Label();
            _cmbRetentionTemplate = new System.Windows.Forms.ComboBox();
            _lblRetentionTemplateInfo = new System.Windows.Forms.Label();
            _chkProtectPlan = new System.Windows.Forms.CheckBox();
            _txtPlanPassword = new System.Windows.Forms.TextBox();
            _txtRecoveryPassword = new System.Windows.Forms.TextBox();

            // ========== STEP 5: Hedefler ==========
            _lblStep5Header = new System.Windows.Forms.Label();
            _lblStep5Hint = new System.Windows.Forms.Label();
            _lvCloudTargets = new System.Windows.Forms.ListView();
            _colCtName = new System.Windows.Forms.ColumnHeader();
            _colCtType = new System.Windows.Forms.ColumnHeader();
            _colCtStatus = new System.Windows.Forms.ColumnHeader();
            _btnAddCloud = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnEditCloud = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnRemoveCloud = new KoruMsSqlYedek.Win.Theme.ModernButton();

            // ========== STEP 6: Bildirim + Raporlama ==========
            _lblStep6Header = new System.Windows.Forms.Label();
            _chkEmailEnabled = new System.Windows.Forms.CheckBox();
            _lblSmtpProfile = new System.Windows.Forms.Label();
            _cmbSmtpProfile = new System.Windows.Forms.ComboBox();
            _lnkOpenSmtpSettings = new System.Windows.Forms.LinkLabel();
            _chkNotifySuccess = new System.Windows.Forms.CheckBox();
            _chkNotifyFailure = new System.Windows.Forms.CheckBox();
            _chkToast = new System.Windows.Forms.CheckBox();
            _lblStep6ReportHeader = new System.Windows.Forms.Label();
            _chkReportEnabled = new System.Windows.Forms.CheckBox();
            _lblReportFreq = new System.Windows.Forms.Label();
            _cmbReportFreq = new System.Windows.Forms.ComboBox();
            _lblReportEmail = new System.Windows.Forms.Label();
            _txtReportEmail = new System.Windows.Forms.TextBox();
            _lblReportHour = new System.Windows.Forms.Label();
            _nudReportHour = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();

            SuspendLayout();
            _pnlStepIndicator.SuspendLayout();
            _pnlStep1.SuspendLayout();
            _pnlStep2.SuspendLayout();
            _pnlStep3.SuspendLayout();
            _pnlStep4.SuspendLayout();
            _pnlStep5.SuspendLayout();
            _pnlStep6.SuspendLayout();
            _pnlNavigation.SuspendLayout();
            _pnlFileBackup.SuspendLayout();

            // --- ToolTip configuration ---
            _toolTip.AutoPopDelay = 15000;
            _toolTip.InitialDelay = 400;
            _toolTip.ReshowDelay = 200;
            _toolTip.ShowAlways = true;

            // ===================================================================
            // STEP INDICATOR (top bar)
            // ===================================================================
            _pnlStepIndicator.Dock = System.Windows.Forms.DockStyle.Top;
            _pnlStepIndicator.Name = "_pnlStepIndicator";
            _pnlStepIndicator.Height = 56;
            _pnlStepIndicator.BackColor = System.Drawing.Color.FromArgb(30, 30, 36);
            _pnlStepIndicator.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);

            // Step num 1
            _lblStepNum1.Text = "1";
            _lblStepNum1.Name = "_lblStepNum1";
            _lblStepNum1.Size = new System.Drawing.Size(24, 24);
            _lblStepNum1.Location = new System.Drawing.Point(6, 6);
            _lblStepNum1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _lblStepNum1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblStepNum1.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepNum1.Tag = 0;
            _lblStepNum1.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepNum1);

            _lblStepTitle1.Text = "Ba\u011flant\u0131";
            _lblStepTitle1.Name = "_lblStepTitle1";
            _lblStepTitle1.AutoSize = false;
            _lblStepTitle1.Size = new System.Drawing.Size(97, 18);
            _lblStepTitle1.Location = new System.Drawing.Point(6, 32);
            _lblStepTitle1.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            _lblStepTitle1.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _lblStepTitle1.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepTitle1.Tag = 0;
            _lblStepTitle1.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepTitle1);

            // Step num 2
            _lblStepNum2.Text = "2";
            _lblStepNum2.Name = "_lblStepNum2";
            _lblStepNum2.Size = new System.Drawing.Size(24, 24);
            _lblStepNum2.Location = new System.Drawing.Point(109, 6);
            _lblStepNum2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _lblStepNum2.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblStepNum2.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepNum2.Tag = 1;
            _lblStepNum2.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepNum2);

            _lblStepTitle2.Text = "Kaynaklar";
            _lblStepTitle2.Name = "_lblStepTitle2";
            _lblStepTitle2.AutoSize = false;
            _lblStepTitle2.Size = new System.Drawing.Size(97, 18);
            _lblStepTitle2.Location = new System.Drawing.Point(109, 32);
            _lblStepTitle2.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            _lblStepTitle2.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _lblStepTitle2.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepTitle2.Tag = 1;
            _lblStepTitle2.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepTitle2);

            // Step num 3
            _lblStepNum3.Text = "3";
            _lblStepNum3.Name = "_lblStepNum3";
            _lblStepNum3.Size = new System.Drawing.Size(24, 24);
            _lblStepNum3.Location = new System.Drawing.Point(212, 6);
            _lblStepNum3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _lblStepNum3.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblStepNum3.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepNum3.Tag = 2;
            _lblStepNum3.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepNum3);

            _lblStepTitle3.Text = "Zamanlama";
            _lblStepTitle3.Name = "_lblStepTitle3";
            _lblStepTitle3.AutoSize = false;
            _lblStepTitle3.Size = new System.Drawing.Size(97, 18);
            _lblStepTitle3.Location = new System.Drawing.Point(212, 32);
            _lblStepTitle3.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            _lblStepTitle3.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _lblStepTitle3.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepTitle3.Tag = 2;
            _lblStepTitle3.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepTitle3);

            // Step num 4
            _lblStepNum4.Text = "4";
            _lblStepNum4.Name = "_lblStepNum4";
            _lblStepNum4.Size = new System.Drawing.Size(24, 24);
            _lblStepNum4.Location = new System.Drawing.Point(315, 6);
            _lblStepNum4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _lblStepNum4.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblStepNum4.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepNum4.Tag = 3;
            _lblStepNum4.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepNum4);

            _lblStepTitle4.Text = "S\u0131k\u0131\u015ft\u0131rma";
            _lblStepTitle4.Name = "_lblStepTitle4";
            _lblStepTitle4.AutoSize = false;
            _lblStepTitle4.Size = new System.Drawing.Size(97, 18);
            _lblStepTitle4.Location = new System.Drawing.Point(315, 32);
            _lblStepTitle4.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            _lblStepTitle4.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _lblStepTitle4.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepTitle4.Tag = 3;
            _lblStepTitle4.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepTitle4);

            // Step num 5
            _lblStepNum5.Text = "5";
            _lblStepNum5.Name = "_lblStepNum5";
            _lblStepNum5.Size = new System.Drawing.Size(24, 24);
            _lblStepNum5.Location = new System.Drawing.Point(418, 6);
            _lblStepNum5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _lblStepNum5.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblStepNum5.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepNum5.Tag = 4;
            _lblStepNum5.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepNum5);

            _lblStepTitle5.Text = "Hedefler";
            _lblStepTitle5.Name = "_lblStepTitle5";
            _lblStepTitle5.AutoSize = false;
            _lblStepTitle5.Size = new System.Drawing.Size(97, 18);
            _lblStepTitle5.Location = new System.Drawing.Point(418, 32);
            _lblStepTitle5.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            _lblStepTitle5.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _lblStepTitle5.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepTitle5.Tag = 4;
            _lblStepTitle5.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepTitle5);

            // Step num 6
            _lblStepNum6.Text = "6";
            _lblStepNum6.Name = "_lblStepNum6";
            _lblStepNum6.Size = new System.Drawing.Size(24, 24);
            _lblStepNum6.Location = new System.Drawing.Point(521, 6);
            _lblStepNum6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _lblStepNum6.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblStepNum6.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepNum6.Tag = 5;
            _lblStepNum6.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepNum6);

            _lblStepTitle6.Text = "Bildirim";
            _lblStepTitle6.Name = "_lblStepTitle6";
            _lblStepTitle6.AutoSize = false;
            _lblStepTitle6.Size = new System.Drawing.Size(97, 18);
            _lblStepTitle6.Location = new System.Drawing.Point(521, 32);
            _lblStepTitle6.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            _lblStepTitle6.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _lblStepTitle6.Cursor = System.Windows.Forms.Cursors.Hand;
            _lblStepTitle6.Tag = 5;
            _lblStepTitle6.Click += OnStepIndicatorClick;
            _pnlStepIndicator.Controls.Add(_lblStepTitle6);

            // ===================================================================
            // STEP PANELS — common properties
            // ===================================================================
            _pnlStep1.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlStep1.Name = "_pnlStep1";
            _pnlStep1.AutoScroll = true;
            _pnlStep1.Padding = new System.Windows.Forms.Padding(24, 16, 24, 12);

            _pnlStep2.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlStep2.Name = "_pnlStep2";
            _pnlStep2.AutoScroll = true;
            _pnlStep2.Padding = new System.Windows.Forms.Padding(24, 16, 24, 12);

            _pnlStep3.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlStep3.Name = "_pnlStep3";
            _pnlStep3.AutoScroll = true;
            _pnlStep3.Padding = new System.Windows.Forms.Padding(24, 16, 24, 12);

            _pnlStep4.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlStep4.Name = "_pnlStep4";
            _pnlStep4.AutoScroll = true;
            _pnlStep4.Padding = new System.Windows.Forms.Padding(24, 16, 24, 12);

            _pnlStep5.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlStep5.Name = "_pnlStep5";
            _pnlStep5.AutoScroll = true;
            _pnlStep5.Padding = new System.Windows.Forms.Padding(24, 16, 24, 12);

            _pnlStep6.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlStep6.Name = "_pnlStep6";
            _pnlStep6.AutoScroll = true;
            _pnlStep6.Padding = new System.Windows.Forms.Padding(24, 16, 24, 12);

            // ===================================================================
            // STEP 1: Plan Bilgileri + SQL Sunucu Baglantisi
            // ===================================================================
            _lblStep1Header.Text = "\u2460 Plan Bilgileri";
            _lblStep1Header.Name = "_lblStep1Header";
            _lblStep1Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep1Header.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep1Header.AutoSize = true;
            _lblStep1Header.Location = new System.Drawing.Point(0, 5);
            _pnlStep1.Controls.Add(_lblStep1Header);

            _lblPlanName.Text = "Plan Ad\u0131:";
            _lblPlanName.Name = "_lblPlanName";
            _lblPlanName.AutoSize = true;
            _lblPlanName.Location = new System.Drawing.Point(0, 38);
            _pnlStep1.Controls.Add(_lblPlanName);

            _txtPlanName.Location = new System.Drawing.Point(180, 35);
            _txtPlanName.Name = "_txtPlanName";
            _txtPlanName.Size = new System.Drawing.Size(390, 23);
            _toolTip.SetToolTip(_txtPlanName, "Bu yedekleme plan\u0131n\u0131n benzersiz ad\u0131.\n\u00d6rnek: \"Mikro Veri Gece Yedek\", \"Haftal\u0131k Ar\u015fiv\"");
            _pnlStep1.Controls.Add(_txtPlanName);

            _chkEnabled.Text = "Plan Aktif";
            _chkEnabled.Name = "_chkEnabled";
            _chkEnabled.Location = new System.Drawing.Point(180, 65);
            _chkEnabled.AutoSize = true;
            _chkEnabled.Checked = true;
            _toolTip.SetToolTip(_chkEnabled, "Devre d\u0131\u015f\u0131 b\u0131rak\u0131rsan\u0131z plan zamanlay\u0131c\u0131 taraf\u0131ndan \u00e7al\u0131\u015ft\u0131r\u0131lmaz.\nManuel yedek almaya devam edebilirsiniz.");
            _pnlStep1.Controls.Add(_chkEnabled);

            _lblLocalPath.Text = "Yerel Yedek Klas\u00f6r\u00fc:";
            _lblLocalPath.Name = "_lblLocalPath";
            _lblLocalPath.AutoSize = true;
            _lblLocalPath.Location = new System.Drawing.Point(0, 100);
            _pnlStep1.Controls.Add(_lblLocalPath);

            _txtLocalPath.Location = new System.Drawing.Point(180, 97);
            _txtLocalPath.Name = "_txtLocalPath";
            _txtLocalPath.Size = new System.Drawing.Size(354, 23);
            _toolTip.SetToolTip(_txtLocalPath, "Yedek dosyalar\u0131n\u0131n kaydedilece\u011fi yerel dizin.\n\u00d6rnek: D:\\Backups\\KoruMsSqlYedek\nBulut hedef eklerseniz dosyalar \u00f6nce buraya kaydedilir, sonra buluta y\u00fcklenir.\nNot: Yeterli disk alan\u0131 oldu\u011fundan emin olun.");
            _pnlStep1.Controls.Add(_txtLocalPath);

            _btnBrowseLocal.Text = "...";
            _btnBrowseLocal.Name = "_btnBrowseLocal";
            _btnBrowseLocal.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBrowseLocal.Size = new System.Drawing.Size(30, 26);
            _btnBrowseLocal.Location = new System.Drawing.Point(540, 97);
            _btnBrowseLocal.Click += OnBrowseLocalPathClick;
            _pnlStep1.Controls.Add(_btnBrowseLocal);

            _lblStep1SqlHeader.Text = "\u2461 SQL Server Ba\u011flant\u0131s\u0131";
            _lblStep1SqlHeader.Name = "_lblStep1SqlHeader";
            _lblStep1SqlHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep1SqlHeader.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep1SqlHeader.AutoSize = true;
            _lblStep1SqlHeader.Location = new System.Drawing.Point(0, 137);
            _pnlStep1.Controls.Add(_lblStep1SqlHeader);

            _lblServer.Text = "Sunucu Ad\u0131 / IP:";
            _lblServer.Name = "_lblServer";
            _lblServer.AutoSize = true;
            _lblServer.Location = new System.Drawing.Point(0, 170);
            _pnlStep1.Controls.Add(_lblServer);

            _txtServer.Location = new System.Drawing.Point(180, 167);
            _txtServer.Name = "_txtServer";
            _txtServer.Size = new System.Drawing.Size(390, 23);
            _toolTip.SetToolTip(_txtServer, "SQL Server sunucu adresi.\n\u00d6rnekler:\n  \u2022 localhost\n  \u2022 192.168.1.100\n  \u2022 SUNUCU\\SQLEXPRESS\n  \u2022 sunucu.domain.local,1433");
            _pnlStep1.Controls.Add(_txtServer);

            _lblAuthMode.Text = "Kimlik Do\u011frulama:";
            _lblAuthMode.Name = "_lblAuthMode";
            _lblAuthMode.AutoSize = true;
            _lblAuthMode.Location = new System.Drawing.Point(0, 200);
            _pnlStep1.Controls.Add(_lblAuthMode);

            _cmbAuthMode.Location = new System.Drawing.Point(180, 197);
            _cmbAuthMode.Name = "_cmbAuthMode";
            _cmbAuthMode.Size = new System.Drawing.Size(390, 23);
            _cmbAuthMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbAuthMode.SelectedIndexChanged += OnAuthModeChanged;
            _toolTip.SetToolTip(_cmbAuthMode, "Windows: Mevcut oturum bilgileriyle ba\u011flan\u0131r.\nSQL Server: Kullan\u0131c\u0131 ad\u0131 ve \u015fifre gerekir.");
            _pnlStep1.Controls.Add(_cmbAuthMode);

            _lblSqlUser.Text = "Kullan\u0131c\u0131 Ad\u0131:";
            _lblSqlUser.Name = "_lblSqlUser";
            _lblSqlUser.AutoSize = true;
            _lblSqlUser.Location = new System.Drawing.Point(0, 230);
            _pnlStep1.Controls.Add(_lblSqlUser);

            _txtSqlUser.Location = new System.Drawing.Point(180, 227);
            _txtSqlUser.Name = "_txtSqlUser";
            _txtSqlUser.Size = new System.Drawing.Size(390, 23);
            _toolTip.SetToolTip(_txtSqlUser, "SQL Server kimlik do\u011frulamas\u0131 i\u00e7in kullan\u0131c\u0131 ad\u0131.\n\u00d6rnek: sa, backupuser");
            _pnlStep1.Controls.Add(_txtSqlUser);

            _lblSqlPassword.Text = "\u015eifre:";
            _lblSqlPassword.Name = "_lblSqlPassword";
            _lblSqlPassword.AutoSize = true;
            _lblSqlPassword.Location = new System.Drawing.Point(0, 260);
            _pnlStep1.Controls.Add(_lblSqlPassword);

            _txtSqlPassword.Location = new System.Drawing.Point(180, 257);
            _txtSqlPassword.Name = "_txtSqlPassword";
            _txtSqlPassword.Size = new System.Drawing.Size(390, 23);
            _txtSqlPassword.UseSystemPasswordChar = true;
            _toolTip.SetToolTip(_txtSqlPassword, "SQL Server \u015fifresi. DPAPI ile \u015fifreli saklan\u0131r,\nd\u00fcz metin olarak kaydedilmez.");
            _pnlStep1.Controls.Add(_txtSqlPassword);

            _lblTimeout.Text = "Zaman A\u015f\u0131m\u0131 (sn):";
            _lblTimeout.Name = "_lblTimeout";
            _lblTimeout.AutoSize = true;
            _lblTimeout.Location = new System.Drawing.Point(0, 290);
            _pnlStep1.Controls.Add(_lblTimeout);

            _nudTimeout.Location = new System.Drawing.Point(180, 287);
            _nudTimeout.Name = "_nudTimeout";
            _nudTimeout.Size = new System.Drawing.Size(80, 23);
            _nudTimeout.Minimum = 5;
            _nudTimeout.Maximum = 300;
            _nudTimeout.Value = 30;
            _toolTip.SetToolTip(_nudTimeout, "Sunucuya ba\u011flanma s\u00fcresi (saniye).\nVarsay\u0131lan: 30 sn. A\u011f yava\u015fsa art\u0131r\u0131n.");
            _pnlStep1.Controls.Add(_nudTimeout);

            _chkTrustCert.Text = "Sertifikaya G\u00fcven";
            _chkTrustCert.Name = "_chkTrustCert";
            _chkTrustCert.Location = new System.Drawing.Point(280, 287);
            _chkTrustCert.AutoSize = true;
            _chkTrustCert.Checked = true;
            _toolTip.SetToolTip(_chkTrustCert, "Yerel veya test sunucular\u0131nda self-signed\nSSL sertifikas\u0131 kullan\u0131l\u0131yorsa i\u015faretleyin.\nKapal\u0131ysa ge\u00e7erli CA sertifikas\u0131 gerekir.");
            _pnlStep1.Controls.Add(_chkTrustCert);

            _btnTestSql.Text = "Ba\u011flant\u0131y\u0131 S\u0131na";
            _btnTestSql.Name = "_btnTestSql";
            _btnTestSql.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnTestSql.Location = new System.Drawing.Point(180, 323);
            _btnTestSql.Size = new System.Drawing.Size(170, 34);
            _btnTestSql.Click += OnTestSqlConnectionClick;
            _toolTip.SetToolTip(_btnTestSql, "Girdi\u011finiz bilgilerle SQL Server ba\u011flant\u0131s\u0131n\u0131 test eder.\nBa\u015far\u0131l\u0131ysa veritabanlar\u0131 otomatik listelenir.");
            _pnlStep1.Controls.Add(_btnTestSql);

            // ===================================================================
            // STEP 2: Kaynaklar (Veritabanlari + Dosya Yedekleme)
            // ===================================================================
            _lblStep2Header.Text = "\u2462 Yedeklenecek Veritabanlar\u0131";
            _lblStep2Header.Name = "_lblStep2Header";
            _lblStep2Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep2Header.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep2Header.AutoSize = true;
            _lblStep2Header.Location = new System.Drawing.Point(0, 5);
            _pnlStep2.Controls.Add(_lblStep2Header);

            _lblStep2Hint.Text = "SQL ba\u011flant\u0131s\u0131 ba\u015far\u0131l\u0131ysa veritabanlar\u0131 otomatik listelenir.";
            _lblStep2Hint.Name = "_lblStep2Hint";
            _lblStep2Hint.AutoSize = true;
            _lblStep2Hint.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _lblStep2Hint.Location = new System.Drawing.Point(0, 31);
            _pnlStep2.Controls.Add(_lblStep2Hint);

            _chkSelectAll.Text = "T\u00fcm\u00fcn\u00fc Se\u00e7";
            _chkSelectAll.Name = "_chkSelectAll";
            _chkSelectAll.AutoSize = true;
            _chkSelectAll.Location = new System.Drawing.Point(0, 53);
            _chkSelectAll.CheckedChanged += OnSelectAllChanged;
            _toolTip.SetToolTip(_chkSelectAll, "Listedeki t\u00fcm veritabanlar\u0131n\u0131 se\u00e7er veya se\u00e7imi kald\u0131r\u0131r.");
            _pnlStep2.Controls.Add(_chkSelectAll);

            _btnRefreshDatabases.Text = "Yenile";
            _btnRefreshDatabases.Name = "_btnRefreshDatabases";
            _btnRefreshDatabases.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnRefreshDatabases.Size = new System.Drawing.Size(100, 26);
            _btnRefreshDatabases.Location = new System.Drawing.Point(480, 51);
            _btnRefreshDatabases.Click += OnRefreshDatabasesClick;
            _toolTip.SetToolTip(_btnRefreshDatabases, "Sunucudaki veritabanlar\u0131n\u0131 yeniden sorgular.");
            _pnlStep2.Controls.Add(_btnRefreshDatabases);

            _clbDatabases.Location = new System.Drawing.Point(0, 81);
            _clbDatabases.Name = "_clbDatabases";
            _clbDatabases.Size = new System.Drawing.Size(580, 200);
            _clbDatabases.CheckOnClick = true;
            _toolTip.SetToolTip(_clbDatabases, "Yedeklemek istedi\u011finiz veritabanlar\u0131n\u0131 i\u015faretleyin.\nSistem veritabanlar\u0131 (master, model, msdb, tempdb) listelenmez.");
            _pnlStep2.Controls.Add(_clbDatabases);

            // --- Dosya Yedekleme Kaynakları ---
            _lblStep2FileHeader.Text = "\u2463 Dosya / Klas\u00f6r Kaynaklar\u0131 (VSS)";
            _lblStep2FileHeader.Name = "_lblStep2FileHeader";
            _lblStep2FileHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep2FileHeader.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep2FileHeader.AutoSize = true;
            _lblStep2FileHeader.Location = new System.Drawing.Point(0, 289);
            _pnlStep2.Controls.Add(_lblStep2FileHeader);

            _chkFileBackupEnabled.Text = "Dosya Yedeklemeyi Etkinle\u015ftir";
            _chkFileBackupEnabled.Name = "_chkFileBackupEnabled";
            _chkFileBackupEnabled.Location = new System.Drawing.Point(0, 315);
            _chkFileBackupEnabled.AutoSize = true;
            _chkFileBackupEnabled.CheckedChanged += OnFileBackupEnabledChanged;
            _toolTip.SetToolTip(_chkFileBackupEnabled, "A\u00e7\u0131k/kilitli dosyalar\u0131 (Outlook PST/OST,\nExcel, SQL MDF) VSS ile yedekler.\nSQL yedeklemeden ba\u011f\u0131ms\u0131z \u00e7al\u0131\u015f\u0131r.");
            _pnlStep2.Controls.Add(_chkFileBackupEnabled);

            _pnlFileBackup.Location = new System.Drawing.Point(0, 341);
            _pnlFileBackup.Name = "_pnlFileBackup";
            _pnlFileBackup.Size = new System.Drawing.Size(580, 140);

            _lvFileSources.Location = new System.Drawing.Point(0, 0);
            _lvFileSources.Name = "_lvFileSources";
            _lvFileSources.Size = new System.Drawing.Size(478, 134);
            _lvFileSources.View = System.Windows.Forms.View.Details;
            _lvFileSources.FullRowSelect = true;
            _lvFileSources.GridLines = true;
            _colFsName.Text = "Kaynak Ad\u0131";
            _colFsName.Width = 130;
            _colFsPath.Text = "Dosya Yolu";
            _colFsPath.Width = 200;
            _colFsVss.Text = "VSS";
            _colFsVss.Width = 50;
            _colFsStatus.Text = "Durum";
            _colFsStatus.Width = 70;
            _lvFileSources.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { _colFsName, _colFsPath, _colFsVss, _colFsStatus });
            _toolTip.SetToolTip(_lvFileSources, "Yedeklenecek dosya/klas\u00f6r kaynaklar\u0131.\nHer kaynak i\u00e7in ayr\u0131 include/exclude deseni tan\u0131mlanabilir.");
            _pnlFileBackup.Controls.Add(_lvFileSources);

            _btnAddFileSource.Text = "Ekle";
            _btnAddFileSource.Name = "_btnAddFileSource";
            _btnAddFileSource.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnAddFileSource.Location = new System.Drawing.Point(484, 0);
            _btnAddFileSource.Size = new System.Drawing.Size(90, 28);
            _btnAddFileSource.Click += OnAddFileSource;
            _pnlFileBackup.Controls.Add(_btnAddFileSource);

            _btnEditFileSource.Text = "D\u00fczenle";
            _btnEditFileSource.Name = "_btnEditFileSource";
            _btnEditFileSource.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnEditFileSource.Location = new System.Drawing.Point(484, 32);
            _btnEditFileSource.Size = new System.Drawing.Size(90, 28);
            _btnEditFileSource.Click += OnEditFileSource;
            _pnlFileBackup.Controls.Add(_btnEditFileSource);

            _btnRemoveFileSource.Text = "Kald\u0131r";
            _btnRemoveFileSource.Name = "_btnRemoveFileSource";
            _btnRemoveFileSource.ButtonStyle = Theme.ModernButtonStyle.Danger;
            _btnRemoveFileSource.Location = new System.Drawing.Point(484, 64);
            _btnRemoveFileSource.Size = new System.Drawing.Size(90, 28);
            _btnRemoveFileSource.Click += OnRemoveFileSource;
            _pnlFileBackup.Controls.Add(_btnRemoveFileSource);
            _pnlStep2.Controls.Add(_pnlFileBackup);

            // ===================================================================
            // STEP 3: Görev Zamanlama & Strateji
            // ===================================================================
            _lblStep3Header.Text = "\u2464 G\u00f6rev Zamanlama";
            _lblStep3Header.Name = "_lblStep3Header";
            _lblStep3Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep3Header.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep3Header.AutoSize = true;
            _lblStep3Header.Location = new System.Drawing.Point(0, 5);
            _pnlStep3.Controls.Add(_lblStep3Header);

            _lblStrategy.Text = "Yedekleme Stratejisi:";
            _lblStrategy.Name = "_lblStrategy";
            _lblStrategy.AutoSize = true;
            _lblStrategy.Location = new System.Drawing.Point(0, 38);
            _pnlStep3.Controls.Add(_lblStrategy);

            _cmbStrategy.Location = new System.Drawing.Point(180, 35);
            _cmbStrategy.Name = "_cmbStrategy";
            _cmbStrategy.Size = new System.Drawing.Size(390, 23);
            _cmbStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbStrategy.SelectedIndexChanged += OnStrategyChanged;
            _toolTip.SetToolTip(_cmbStrategy, "Yaln\u0131zca Tam: Her seferinde t\u00fcm veriyi yedekler. Basit ama b\u00fcy\u00fck.\nTam + Fark: D\u00fczenli tam + aradaki de\u011fi\u015fiklikler. Dengeli \u00e7\u00f6z\u00fcm.\nTam + Fark + Art\u0131r\u0131ml\u0131: En az alan, en h\u0131zl\u0131 \u2014 kurumsal \u00f6nerimiz.");
            _pnlStep3.Controls.Add(_cmbStrategy);

            _lblFullCron.Text = "Tam Yedek G\u00f6revi:";
            _lblFullCron.Name = "_lblFullCron";
            _lblFullCron.AutoSize = true;
            _lblFullCron.Location = new System.Drawing.Point(0, 72);
            _pnlStep3.Controls.Add(_lblFullCron);

            _cronFull.Location = new System.Drawing.Point(180, 69);
            _cronFull.Name = "_cronFull";
            _cronFull.Size = new System.Drawing.Size(390, 50);
            _cronFull.HeightChanged += OnCronPanelHeightChanged;
            _toolTip.SetToolTip(_cronFull, "Tam yede\u011fin \u00e7al\u0131\u015facak zaman\u0131 belirleyin.\nT\u00fcm veritaban\u0131 verisini i\u00e7erir \u2014 geri y\u00fcklemede tek ba\u015f\u0131na yeterli.\n\u00d6neri: Haftada 1 (\u00f6rn. Pazar gece 02:00)");
            _pnlStep3.Controls.Add(_cronFull);

            _lblDiffCron.Text = "Fark Yedek G\u00f6revi:";
            _lblDiffCron.Name = "_lblDiffCron";
            _lblDiffCron.AutoSize = true;
            _lblDiffCron.Location = new System.Drawing.Point(0, 126);
            _pnlStep3.Controls.Add(_lblDiffCron);

            _cronDiff.Location = new System.Drawing.Point(180, 123);
            _cronDiff.Name = "_cronDiff";
            _cronDiff.Size = new System.Drawing.Size(390, 50);
            _cronDiff.HeightChanged += OnCronPanelHeightChanged;
            _toolTip.SetToolTip(_cronDiff, "Son tam yedekten bu yana de\u011fi\u015fen verileri yedekler.\nGeri y\u00fcklemede: Tam + son Fark yedek gerekir.\n\u00d6neri: G\u00fcnde 1 (\u00f6rn. her gece 02:00)");
            _pnlStep3.Controls.Add(_cronDiff);

            _lblIncrCron.Text = "Art\u0131r\u0131ml\u0131 G\u00f6revi:";
            _lblIncrCron.Name = "_lblIncrCron";
            _lblIncrCron.AutoSize = true;
            _lblIncrCron.Location = new System.Drawing.Point(0, 180);
            _pnlStep3.Controls.Add(_lblIncrCron);

            _cronIncr.Location = new System.Drawing.Point(180, 177);
            _cronIncr.Name = "_cronIncr";
            _cronIncr.Size = new System.Drawing.Size(390, 50);
            _cronIncr.HeightChanged += OnCronPanelHeightChanged;
            _toolTip.SetToolTip(_cronIncr, "Son yedekten (tam veya art\u0131r\u0131ml\u0131) bu yana\nde\u011fi\u015fen verileri yedekler. En k\u00fc\u00e7\u00fck boyut.\nGeri y\u00fcklemede: Tam + Fark + t\u00fcm Art\u0131r\u0131ml\u0131 zincir gerekir.\n\u00d6neri: Saatte 1 (\u00f6rn. mesai saatleri 09-18)");
            _pnlStep3.Controls.Add(_cronIncr);

            _lblAutoPromote.Text = "Oto. Tam Yedek E\u015fi\u011fi:";
            _lblAutoPromote.Name = "_lblAutoPromote";
            _lblAutoPromote.AutoSize = true;
            _lblAutoPromote.Location = new System.Drawing.Point(0, 236);
            _pnlStep3.Controls.Add(_lblAutoPromote);

            _nudAutoPromote.Location = new System.Drawing.Point(180, 233);
            _nudAutoPromote.Name = "_nudAutoPromote";
            _nudAutoPromote.Size = new System.Drawing.Size(80, 23);
            _nudAutoPromote.Minimum = 1;
            _nudAutoPromote.Maximum = 100;
            _nudAutoPromote.Value = 7;
            _toolTip.SetToolTip(_nudAutoPromote, "Bu say\u0131da fark/art\u0131r\u0131ml\u0131 yedekten sonra\notomatik olarak tam yedek tetiklenir.\nZincir k\u0131r\u0131lmas\u0131n\u0131 \u00f6nler, geri y\u00fckleme g\u00fcvenilirli\u011fini art\u0131r\u0131r.\nVarsay\u0131lan: 7 (haftada bir tam yedek otomatik al\u0131n\u0131r)");
            _pnlStep3.Controls.Add(_nudAutoPromote);

            _chkVerify.Text = "Yedek sonras\u0131 b\u00fct\u00fcnl\u00fck do\u011frulamas\u0131 yap (RESTORE VERIFYONLY)";
            _chkVerify.Name = "_chkVerify";
            _chkVerify.Location = new System.Drawing.Point(0, 265);
            _chkVerify.AutoSize = true;
            _chkVerify.Checked = true;
            _toolTip.SetToolTip(_chkVerify, "Her yedek g\u00f6revinden sonra SQL Server\u2019\u0131n RESTORE VERIFYONLY\nkomutuyla dosya b\u00fct\u00fcnl\u00fc\u011f\u00fc do\u011frulan\u0131r.\nBozuk yedek tespit edilirse hemen bildirim g\u00f6nderilir.\n\u00d6neri: Her zaman a\u00e7\u0131k tutun.");
            _pnlStep3.Controls.Add(_chkVerify);

            // --- Dosya Yedekleme Zamanlaması (gizli) ---
            _lblStep3FileSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            _lblStep3FileSep.Name = "_lblStep3FileSep";
            _lblStep3FileSep.Location = new System.Drawing.Point(0, 301);
            _lblStep3FileSep.Size = new System.Drawing.Size(570, 2);
            _lblStep3FileSep.Visible = false;
            _pnlStep3.Controls.Add(_lblStep3FileSep);

            _lblStep3FileSchedHeader.Text = "\U0001f4c1 Dosya Yedekleme G\u00f6revi";
            _lblStep3FileSchedHeader.Name = "_lblStep3FileSchedHeader";
            _lblStep3FileSchedHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            _lblStep3FileSchedHeader.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep3FileSchedHeader.AutoSize = true;
            _lblStep3FileSchedHeader.Location = new System.Drawing.Point(0, 301);
            _lblStep3FileSchedHeader.Visible = false;
            _pnlStep3.Controls.Add(_lblStep3FileSchedHeader);

            _lblFileSchedule.Text = "Dosya G\u00f6revi:";
            _lblFileSchedule.Name = "_lblFileSchedule";
            _lblFileSchedule.AutoSize = true;
            _lblFileSchedule.Location = new System.Drawing.Point(0, 304);
            _lblFileSchedule.Visible = false;
            _pnlStep3.Controls.Add(_lblFileSchedule);

            _cronFileSchedule.Location = new System.Drawing.Point(180, 301);
            _cronFileSchedule.Name = "_cronFileSchedule";
            _cronFileSchedule.Size = new System.Drawing.Size(390, 80);
            _cronFileSchedule.Visible = false;
            _pnlStep3.Controls.Add(_cronFileSchedule);

            // ===================================================================
            // STEP 4: Sikistirma & Saklama Politikasi
            // ===================================================================
            _lblStep4Header.Text = "\u2465 Ar\u015fiv S\u0131k\u0131\u015ft\u0131rma Ayarlar\u0131";
            _lblStep4Header.Name = "_lblStep4Header";
            _lblStep4Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep4Header.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep4Header.AutoSize = true;
            _lblStep4Header.Location = new System.Drawing.Point(0, 5);
            _pnlStep4.Controls.Add(_lblStep4Header);

            _lblAlgorithm.Text = "S\u0131k\u0131\u015ft\u0131rma Algoritmas\u0131:";
            _lblAlgorithm.Name = "_lblAlgorithm";
            _lblAlgorithm.AutoSize = true;
            _lblAlgorithm.Location = new System.Drawing.Point(0, 40);
            _pnlStep4.Controls.Add(_lblAlgorithm);

            _cmbAlgorithm.Location = new System.Drawing.Point(180, 37);
            _cmbAlgorithm.Name = "_cmbAlgorithm";
            _cmbAlgorithm.Size = new System.Drawing.Size(390, 23);
            _cmbAlgorithm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _toolTip.SetToolTip(_cmbAlgorithm, "LZMA2: En iyi s\u0131k\u0131\u015ft\u0131rma oran\u0131 (\u00f6nerilen).\nLZMA: Eski uyumluluk i\u00e7in.\nBZip2: Orta seviye.\nDeflate: En h\u0131zl\u0131, en b\u00fcy\u00fck dosya.");
            _pnlStep4.Controls.Add(_cmbAlgorithm);

            _lblLevel.Text = "S\u0131k\u0131\u015ft\u0131rma D\u00fczeyi:";
            _lblLevel.Name = "_lblLevel";
            _lblLevel.AutoSize = true;
            _lblLevel.Location = new System.Drawing.Point(0, 74);
            _pnlStep4.Controls.Add(_lblLevel);

            _cmbLevel.Location = new System.Drawing.Point(180, 71);
            _cmbLevel.Name = "_cmbLevel";
            _cmbLevel.Size = new System.Drawing.Size(390, 23);
            _cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _toolTip.SetToolTip(_cmbLevel, "Yok: S\u0131k\u0131\u015ft\u0131rma yap\u0131lmaz.\nH\u0131zl\u0131: D\u00fc\u015f\u00fck s\u0131k\u0131\u015ft\u0131rma, h\u0131zl\u0131 i\u015flem.\nNormal: Dengeli.\nMaksimum: Y\u00fcksek s\u0131k\u0131\u015ft\u0131rma.\nUltra: En y\u00fcksek s\u0131k\u0131\u015ft\u0131rma, en yava\u015f.");
            _pnlStep4.Controls.Add(_cmbLevel);

            _lblArchivePassword.Text = "Ar\u015fiv \u015eifresi:";
            _lblArchivePassword.Name = "_lblArchivePassword";
            _lblArchivePassword.AutoSize = true;
            _lblArchivePassword.Location = new System.Drawing.Point(0, 108);
            _pnlStep4.Controls.Add(_lblArchivePassword);

            _txtArchivePassword.Location = new System.Drawing.Point(180, 105);
            _txtArchivePassword.Name = "_txtArchivePassword";
            _txtArchivePassword.Size = new System.Drawing.Size(390, 23);
            _txtArchivePassword.UseSystemPasswordChar = true;
            _toolTip.SetToolTip(_txtArchivePassword, "7z ar\u015fivi i\u00e7in AES-256 \u015fifreleme.\nBo\u015f b\u0131rak\u0131rsan\u0131z ar\u015fiv \u015fifresiz olu\u015fturulur.\n\u015eifre DPAPI ile korunarak saklan\u0131r.");
            _pnlStep4.Controls.Add(_txtArchivePassword);

            _lblStep4RetHeader.Text = "Saklama (Retention) Politikas\u0131";
            _lblStep4RetHeader.Name = "_lblStep4RetHeader";
            _lblStep4RetHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep4RetHeader.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep4RetHeader.AutoSize = true;
            _lblStep4RetHeader.Location = new System.Drawing.Point(0, 153);
            _pnlStep4.Controls.Add(_lblStep4RetHeader);

            _lblRetentionTemplate.Text = "Saklama \u015eablonu:";
            _lblRetentionTemplate.Name = "_lblRetentionTemplate";
            _lblRetentionTemplate.AutoSize = true;
            _lblRetentionTemplate.Location = new System.Drawing.Point(0, 188);
            _pnlStep4.Controls.Add(_lblRetentionTemplate);

            _cmbRetentionTemplate.Location = new System.Drawing.Point(180, 185);
            _cmbRetentionTemplate.Name = "_cmbRetentionTemplate";
            _cmbRetentionTemplate.Size = new System.Drawing.Size(390, 23);
            _cmbRetentionTemplate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbRetentionTemplate.SelectedIndexChanged += OnRetentionTemplateChanged;
            _toolTip.SetToolTip(_cmbRetentionTemplate, "Haz\u0131r \u015fablon se\u00e7in veya \u00d6zel se\u00e7erek elle belirleyin.");
            _pnlStep4.Controls.Add(_cmbRetentionTemplate);

            _lblRetentionTemplateInfo.Location = new System.Drawing.Point(180, 219);
            _lblRetentionTemplateInfo.Name = "_lblRetentionTemplateInfo";
            _lblRetentionTemplateInfo.Size = new System.Drawing.Size(390, 36);
            _lblRetentionTemplateInfo.AutoSize = false;
            _lblRetentionTemplateInfo.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic);
            _lblRetentionTemplateInfo.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _pnlStep4.Controls.Add(_lblRetentionTemplateInfo);

            _lblRetention.Text = "Temizlik Kural\u0131:";
            _lblRetention.Name = "_lblRetention";
            _lblRetention.AutoSize = true;
            _lblRetention.Location = new System.Drawing.Point(0, 268);
            _pnlStep4.Controls.Add(_lblRetention);

            _cmbRetention.Location = new System.Drawing.Point(180, 265);
            _cmbRetention.Name = "_cmbRetention";
            _cmbRetention.Size = new System.Drawing.Size(390, 23);
            _cmbRetention.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbRetention.SelectedIndexChanged += OnRetentionChanged;
            _toolTip.SetToolTip(_cmbRetention, "Son N Adet: En son N yede\u011fi sakla, eskilerini sil.\nG\u00fcn S\u0131n\u0131r\u0131: Belirtilen g\u00fcnden eski yedekleri sil.\nHer \u0130kisi: \u0130ki kural birlikte uygulan\u0131r.");
            _pnlStep4.Controls.Add(_cmbRetention);

            _lblKeepLastN.Text = "Saklanacak Yedek Say\u0131s\u0131:";
            _lblKeepLastN.Name = "_lblKeepLastN";
            _lblKeepLastN.AutoSize = true;
            _lblKeepLastN.Location = new System.Drawing.Point(0, 302);
            _pnlStep4.Controls.Add(_lblKeepLastN);

            _nudKeepLastN.Location = new System.Drawing.Point(180, 299);
            _nudKeepLastN.Name = "_nudKeepLastN";
            _nudKeepLastN.Size = new System.Drawing.Size(80, 23);
            _nudKeepLastN.Minimum = 1;
            _nudKeepLastN.Maximum = 999;
            _nudKeepLastN.Value = 30;
            _toolTip.SetToolTip(_nudKeepLastN, "En son ka\u00e7 yedek dosyas\u0131 saklanacak.\nVarsay\u0131lan: 30");
            _pnlStep4.Controls.Add(_nudKeepLastN);

            _lblDeleteDays.Text = "Silme S\u00fcresi (g\u00fcn):";
            _lblDeleteDays.Name = "_lblDeleteDays";
            _lblDeleteDays.AutoSize = true;
            _lblDeleteDays.Location = new System.Drawing.Point(0, 336);
            _pnlStep4.Controls.Add(_lblDeleteDays);

            _nudDeleteDays.Location = new System.Drawing.Point(180, 333);
            _nudDeleteDays.Name = "_nudDeleteDays";
            _nudDeleteDays.Size = new System.Drawing.Size(80, 23);
            _nudDeleteDays.Minimum = 1;
            _nudDeleteDays.Maximum = 3650;
            _nudDeleteDays.Value = 90;
            _toolTip.SetToolTip(_nudDeleteDays, "Bu g\u00fcn say\u0131s\u0131ndan eski yedekler silinir.\nVarsay\u0131lan: 90 g\u00fcn (3 ay)");
            _pnlStep4.Controls.Add(_nudDeleteDays);

            _chkProtectPlan.AutoSize = true;
            _chkProtectPlan.Name = "_chkProtectPlan";
            _chkProtectPlan.Location = new System.Drawing.Point(0, 381);
            _chkProtectPlan.Text = "\U0001f512 Bu g\u00f6revi \u015fifre ile koru";
            _chkProtectPlan.Font = new System.Drawing.Font("Segoe UI", 10F);
            _chkProtectPlan.ForeColor = System.Drawing.Color.FromArgb(240, 240, 245);
            _chkProtectPlan.CheckedChanged += OnProtectPlanChanged;
            _toolTip.SetToolTip(_chkProtectPlan, "\u0130\u015faretlerseniz plan d\u00fczenleme ve silme i\u015flemlerinde \u015fifre sorulur.");
            _pnlStep4.Controls.Add(_chkProtectPlan);

            _txtPlanPassword.Location = new System.Drawing.Point(24, 411);
            _txtPlanPassword.Name = "_txtPlanPassword";
            _txtPlanPassword.Size = new System.Drawing.Size(390, 23);
            _txtPlanPassword.UseSystemPasswordChar = true;
            _txtPlanPassword.PlaceholderText = "\u015eifre girin...";
            _txtPlanPassword.Visible = false;
            _toolTip.SetToolTip(_txtPlanPassword, "Plan d\u00fczenleme ve silme i\u015flemlerinde sorulacak \u015fifre.");
            _pnlStep4.Controls.Add(_txtPlanPassword);

            _txtRecoveryPassword.Location = new System.Drawing.Point(24, 439);
            _txtRecoveryPassword.Name = "_txtRecoveryPassword";
            _txtRecoveryPassword.Size = new System.Drawing.Size(390, 23);
            _txtRecoveryPassword.UseSystemPasswordChar = true;
            _txtRecoveryPassword.PlaceholderText = "Kurtarma \u015fifresi (iste\u011fe ba\u011fl\u0131)...";
            _txtRecoveryPassword.Visible = false;
            _toolTip.SetToolTip(_txtRecoveryPassword, "Plan \u015fifresini unutursan\u0131z bu \u015fifre ile eri\u015fim sa\u011flayabilirsiniz.");
            _pnlStep4.Controls.Add(_txtRecoveryPassword);

            // ===================================================================
            // STEP 5: Hedefler (Bulut / Uzak)
            // ===================================================================
            _lblStep5Header.Text = "\u2466 Yedek G\u00f6nderim Hedefleri";
            _lblStep5Header.Name = "_lblStep5Header";
            _lblStep5Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep5Header.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep5Header.AutoSize = true;
            _lblStep5Header.Location = new System.Drawing.Point(0, 5);
            _pnlStep5.Controls.Add(_lblStep5Header);

            _lblStep5Hint.Text = "Yedek dosyalar\u0131n\u0131n kopyalanaca\u011f\u0131 bulut veya uzak hedefleri y\u00f6netin.\nGoogle Drive, FTP/SFTP ve UNC a\u011f payla\u015f\u0131m\u0131 desteklenir.";
            _lblStep5Hint.Name = "_lblStep5Hint";
            _lblStep5Hint.AutoSize = true;
            _lblStep5Hint.ForeColor = System.Drawing.Color.FromArgb(160, 160, 170);
            _lblStep5Hint.Location = new System.Drawing.Point(0, 31);
            _lblStep5Hint.MaximumSize = new System.Drawing.Size(580, 0);
            _pnlStep5.Controls.Add(_lblStep5Hint);

            _lvCloudTargets.Location = new System.Drawing.Point(0, 71);
            _lvCloudTargets.Name = "_lvCloudTargets";
            _lvCloudTargets.Size = new System.Drawing.Size(478, 380);
            _lvCloudTargets.View = System.Windows.Forms.View.Details;
            _lvCloudTargets.FullRowSelect = true;
            _lvCloudTargets.GridLines = true;
            _colCtName.Text = "Hedef Ad\u0131";
            _colCtName.Width = 200;
            _colCtType.Text = "T\u00fcr";
            _colCtType.Width = 160;
            _colCtStatus.Text = "Durum";
            _colCtStatus.Width = 90;
            _lvCloudTargets.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { _colCtName, _colCtType, _colCtStatus });
            _toolTip.SetToolTip(_lvCloudTargets, "Yedek dosyalar\u0131n\u0131n g\u00f6nderilece\u011fi hedefler.\nBirden fazla hedef tan\u0131mlayabilirsiniz.");
            _pnlStep5.Controls.Add(_lvCloudTargets);

            _btnAddCloud.Text = "Ekle";
            _btnAddCloud.Name = "_btnAddCloud";
            _btnAddCloud.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnAddCloud.Location = new System.Drawing.Point(484, 71);
            _btnAddCloud.Size = new System.Drawing.Size(90, 30);
            _btnAddCloud.Click += OnAddCloudTarget;
            _pnlStep5.Controls.Add(_btnAddCloud);

            _btnEditCloud.Text = "D\u00fczenle";
            _btnEditCloud.Name = "_btnEditCloud";
            _btnEditCloud.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnEditCloud.Location = new System.Drawing.Point(484, 105);
            _btnEditCloud.Size = new System.Drawing.Size(90, 30);
            _btnEditCloud.Click += OnEditCloudTarget;
            _pnlStep5.Controls.Add(_btnEditCloud);

            _btnRemoveCloud.Text = "Kald\u0131r";
            _btnRemoveCloud.Name = "_btnRemoveCloud";
            _btnRemoveCloud.ButtonStyle = Theme.ModernButtonStyle.Danger;
            _btnRemoveCloud.Location = new System.Drawing.Point(484, 139);
            _btnRemoveCloud.Size = new System.Drawing.Size(90, 30);
            _btnRemoveCloud.Click += OnRemoveCloudTarget;
            _pnlStep5.Controls.Add(_btnRemoveCloud);

            // ===================================================================
            // STEP 6: Bildirim + Raporlama
            // ===================================================================
            _lblStep6Header.Text = "\u2467 E-posta Bildirimleri";
            _lblStep6Header.Name = "_lblStep6Header";
            _lblStep6Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep6Header.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep6Header.AutoSize = true;
            _lblStep6Header.Location = new System.Drawing.Point(0, 5);
            _pnlStep6.Controls.Add(_lblStep6Header);

            _chkEmailEnabled.Text = "E-posta Bildirimi G\u00f6nder";
            _chkEmailEnabled.Name = "_chkEmailEnabled";
            _chkEmailEnabled.Location = new System.Drawing.Point(0, 33);
            _chkEmailEnabled.AutoSize = true;
            _chkEmailEnabled.CheckedChanged += OnEmailEnabledChanged;
            _toolTip.SetToolTip(_chkEmailEnabled, "Yedekleme sonu\u00e7lar\u0131 e-posta ile bildirilir.\nSMTP ayarlar\u0131n\u0131n do\u011fru girilmesi gerekir.");
            _pnlStep6.Controls.Add(_chkEmailEnabled);

            _chkToast.Text = "Windows Bildirim Balonu";
            _chkToast.Name = "_chkToast";
            _chkToast.Location = new System.Drawing.Point(260, 33);
            _chkToast.AutoSize = true;
            _chkToast.Checked = true;
            _toolTip.SetToolTip(_chkToast, "G\u00f6rev \u00e7ubu\u011fu bildirim balonuyla\nyedek durumunu g\u00f6sterir.");
            _pnlStep6.Controls.Add(_chkToast);

            _lblSmtpProfile.Text = "SMTP Profili:";
            _lblSmtpProfile.Name = "_lblSmtpProfile";
            _lblSmtpProfile.AutoSize = true;
            _lblSmtpProfile.Location = new System.Drawing.Point(0, 61);
            _pnlStep6.Controls.Add(_lblSmtpProfile);

            _cmbSmtpProfile.Location = new System.Drawing.Point(130, 59);
            _cmbSmtpProfile.Name = "_cmbSmtpProfile";
            _cmbSmtpProfile.Size = new System.Drawing.Size(300, 23);
            _cmbSmtpProfile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbSmtpProfile.DisplayMember = "DisplayName";
            _cmbSmtpProfile.ValueMember = "Id";
            _pnlStep6.Controls.Add(_cmbSmtpProfile);

            _lnkOpenSmtpSettings.Text = "Profil ekle / d\u00fczenle";
            _lnkOpenSmtpSettings.Name = "_lnkOpenSmtpSettings";
            _lnkOpenSmtpSettings.AutoSize = true;
            _lnkOpenSmtpSettings.Location = new System.Drawing.Point(440, 61);
            _lnkOpenSmtpSettings.LinkClicked += OnOpenSmtpSettingsClick;
            _pnlStep6.Controls.Add(_lnkOpenSmtpSettings);

            _chkNotifySuccess.Text = "Ba\u015far\u0131l\u0131 yedekte bildir";
            _chkNotifySuccess.Name = "_chkNotifySuccess";
            _chkNotifySuccess.Location = new System.Drawing.Point(130, 91);
            _chkNotifySuccess.AutoSize = true;
            _chkNotifySuccess.Checked = true;
            _pnlStep6.Controls.Add(_chkNotifySuccess);

            _chkNotifyFailure.Text = "Ba\u015far\u0131s\u0131z yedekte bildir";
            _chkNotifyFailure.Name = "_chkNotifyFailure";
            _chkNotifyFailure.Location = new System.Drawing.Point(330, 91);
            _chkNotifyFailure.AutoSize = true;
            _chkNotifyFailure.Checked = true;
            _pnlStep6.Controls.Add(_chkNotifyFailure);

            // --- Periyodik Rapor ---
            _lblStep6ReportHeader.Text = "\u2468 Periyodik Yedek Raporu";
            _lblStep6ReportHeader.Name = "_lblStep6ReportHeader";
            _lblStep6ReportHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep6ReportHeader.ForeColor = System.Drawing.Color.FromArgb(16, 185, 129);
            _lblStep6ReportHeader.AutoSize = true;
            _lblStep6ReportHeader.Location = new System.Drawing.Point(0, 123);
            _pnlStep6.Controls.Add(_lblStep6ReportHeader);

            _chkReportEnabled.Text = "D\u00fczenli Yedek Raporu G\u00f6nder";
            _chkReportEnabled.Name = "_chkReportEnabled";
            _chkReportEnabled.Location = new System.Drawing.Point(0, 151);
            _chkReportEnabled.AutoSize = true;
            _chkReportEnabled.CheckedChanged += OnReportEnabledChanged;
            _toolTip.SetToolTip(_chkReportEnabled, "Etkinle\u015ftirirseniz belirtilen s\u0131kl\u0131kta\nyedekleme \u00f6zet raporu e-posta ile g\u00f6nderilir.\n\u0130\u00e7erik: Ba\u015far\u0131l\u0131/ba\u015far\u0131s\u0131z say\u0131s\u0131, boyut, s\u00fcre.");
            _pnlStep6.Controls.Add(_chkReportEnabled);

            _lblReportFreq.Text = "Rapor S\u0131kl\u0131\u011f\u0131:";
            _lblReportFreq.Name = "_lblReportFreq";
            _lblReportFreq.AutoSize = true;
            _lblReportFreq.Location = new System.Drawing.Point(0, 182);
            _pnlStep6.Controls.Add(_lblReportFreq);

            _cmbReportFreq.Location = new System.Drawing.Point(130, 179);
            _cmbReportFreq.Name = "_cmbReportFreq";
            _cmbReportFreq.Size = new System.Drawing.Size(140, 23);
            _cmbReportFreq.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _toolTip.SetToolTip(_cmbReportFreq, "G\u00fcnl\u00fck: Her g\u00fcn \u00f6nceki g\u00fcn\u00fcn \u00f6zeti.\nHaftal\u0131k: Her Pazartesi ge\u00e7en haftan\u0131n \u00f6zeti.\nAyl\u0131k: Her ay\u0131n 1\u2019inde ge\u00e7en ay\u0131n \u00f6zeti.");
            _pnlStep6.Controls.Add(_cmbReportFreq);

            _lblReportHour.Text = "G\u00f6nderim Saati:";
            _lblReportHour.Name = "_lblReportHour";
            _lblReportHour.AutoSize = true;
            _lblReportHour.Location = new System.Drawing.Point(300, 182);
            _pnlStep6.Controls.Add(_lblReportHour);

            _nudReportHour.Location = new System.Drawing.Point(408, 179);
            _nudReportHour.Name = "_nudReportHour";
            _nudReportHour.Size = new System.Drawing.Size(60, 23);
            _nudReportHour.Minimum = 0;
            _nudReportHour.Maximum = 23;
            _nudReportHour.Value = 8;
            _toolTip.SetToolTip(_nudReportHour, "Raporun g\u00f6nderilece\u011fi saat (0\u201323).\nVarsay\u0131lan: 08:00");
            _pnlStep6.Controls.Add(_nudReportHour);

            _lblReportEmail.Text = "Rapor Al\u0131c\u0131s\u0131:";
            _lblReportEmail.Name = "_lblReportEmail";
            _lblReportEmail.AutoSize = true;
            _lblReportEmail.Location = new System.Drawing.Point(0, 212);
            _pnlStep6.Controls.Add(_lblReportEmail);

            _txtReportEmail.Location = new System.Drawing.Point(130, 209);
            _txtReportEmail.Name = "_txtReportEmail";
            _txtReportEmail.Size = new System.Drawing.Size(440, 23);
            _toolTip.SetToolTip(_txtReportEmail, "Rapor g\u00f6nderilecek e-posta adresi.\nBo\u015f b\u0131rak\u0131l\u0131rsa bildirim e-postas\u0131 kullan\u0131l\u0131r.");
            _pnlStep6.Controls.Add(_txtReportEmail);

            // ===================================================================
            // NAVIGATION BAR (bottom)
            // ===================================================================
            _pnlNavigation.Dock = System.Windows.Forms.DockStyle.Bottom;
            _pnlNavigation.Name = "_pnlNavigation";
            _pnlNavigation.Height = 52;
            _pnlNavigation.BackColor = System.Drawing.Color.FromArgb(30, 30, 36);
            _pnlNavigation.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);

            _btnCancel.Text = "\u0130ptal";
            _btnCancel.Name = "_btnCancel";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Ghost;
            _btnCancel.Size = new System.Drawing.Size(80, 34);
            _btnCancel.Dock = System.Windows.Forms.DockStyle.Left;
            _btnCancel.Click += OnCancelClick;

            _btnSave.Text = "Kaydet";
            _btnSave.Name = "_btnSave";
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Size = new System.Drawing.Size(100, 34);
            _btnSave.Dock = System.Windows.Forms.DockStyle.Right;
            _btnSave.Click += OnSaveClick;

            _btnNext.Text = "\u0130leri";
            _btnNext.Name = "_btnNext";
            _btnNext.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnNext.Size = new System.Drawing.Size(100, 34);
            _btnNext.Dock = System.Windows.Forms.DockStyle.Right;
            _btnNext.Click += OnNextClick;

            _btnBack.Text = "Geri";
            _btnBack.Name = "_btnBack";
            _btnBack.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBack.Size = new System.Drawing.Size(100, 34);
            _btnBack.Dock = System.Windows.Forms.DockStyle.Right;
            _btnBack.Click += OnBackClick;

            _pnlNavigation.Controls.Add(_btnCancel);
            _pnlNavigation.Controls.Add(_btnBack);
            _pnlNavigation.Controls.Add(_btnNext);
            _pnlNavigation.Controls.Add(_btnSave);

            // ===================================================================
            // CONTENT PANEL (holds step panels)
            // ===================================================================
            _pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlContent.Name = "_pnlContent";
            _pnlContent.Padding = new System.Windows.Forms.Padding(8);
            _pnlContent.Controls.Add(_pnlStep1);
            _pnlContent.Controls.Add(_pnlStep2);
            _pnlContent.Controls.Add(_pnlStep3);
            _pnlContent.Controls.Add(_pnlStep4);
            _pnlContent.Controls.Add(_pnlStep5);
            _pnlContent.Controls.Add(_pnlStep6);

            // ===================================================================
            // FORM
            // ===================================================================
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(18, 18, 22);
            ClientSize = new System.Drawing.Size(760, 640);
            MinimumSize = new System.Drawing.Size(780, 580);
            Controls.Add(_pnlContent);
            Controls.Add(_pnlStepIndicator);
            Controls.Add(_pnlNavigation);
            Font = new System.Drawing.Font("Segoe UI", 9.5F);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PlanEditForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Yedekleme Plan\u0131 Sihirbaz\u0131";

            _pnlFileBackup.ResumeLayout(false);
            _pnlNavigation.ResumeLayout(false);
            _pnlStep1.ResumeLayout(false);
            _pnlStep1.PerformLayout();
            _pnlStep2.ResumeLayout(false);
            _pnlStep2.PerformLayout();
            _pnlStep3.ResumeLayout(false);
            _pnlStep3.PerformLayout();
            _pnlStep4.ResumeLayout(false);
            _pnlStep4.PerformLayout();
            _pnlStep5.ResumeLayout(false);
            _pnlStep5.PerformLayout();
            _pnlStep6.ResumeLayout(false);
            _pnlStep6.PerformLayout();
            _pnlStepIndicator.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // ToolTip
        private System.Windows.Forms.ToolTip _toolTip;

        // Wizard infrastructure
        private System.Windows.Forms.Panel _pnlStepIndicator;
        private System.Windows.Forms.Panel _pnlContent;
        private System.Windows.Forms.Panel _pnlNavigation;
        private Theme.ModernButton _btnBack;
        private Theme.ModernButton _btnNext;
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;

        // Step panels (named for Designer)
        private System.Windows.Forms.Panel _pnlStep1;
        private System.Windows.Forms.Panel _pnlStep2;
        private System.Windows.Forms.Panel _pnlStep3;
        private System.Windows.Forms.Panel _pnlStep4;
        private System.Windows.Forms.Panel _pnlStep5;
        private System.Windows.Forms.Panel _pnlStep6;

        // Step indicator number labels
        private System.Windows.Forms.Label _lblStepNum1;
        private System.Windows.Forms.Label _lblStepNum2;
        private System.Windows.Forms.Label _lblStepNum3;
        private System.Windows.Forms.Label _lblStepNum4;
        private System.Windows.Forms.Label _lblStepNum5;
        private System.Windows.Forms.Label _lblStepNum6;

        // Step indicator title labels
        private System.Windows.Forms.Label _lblStepTitle1;
        private System.Windows.Forms.Label _lblStepTitle2;
        private System.Windows.Forms.Label _lblStepTitle3;
        private System.Windows.Forms.Label _lblStepTitle4;
        private System.Windows.Forms.Label _lblStepTitle5;
        private System.Windows.Forms.Label _lblStepTitle6;

        // Step 1: Plan + SQL
        private System.Windows.Forms.Label _lblStep1Header;
        private System.Windows.Forms.Label _lblPlanName;
        private System.Windows.Forms.TextBox _txtPlanName;
        private System.Windows.Forms.CheckBox _chkEnabled;
        private System.Windows.Forms.Label _lblLocalPath;
        private System.Windows.Forms.TextBox _txtLocalPath;
        private Theme.ModernButton _btnBrowseLocal;
        private System.Windows.Forms.Label _lblStep1SqlHeader;
        private System.Windows.Forms.Label _lblServer;
        private System.Windows.Forms.TextBox _txtServer;
        private System.Windows.Forms.Label _lblAuthMode;
        private System.Windows.Forms.ComboBox _cmbAuthMode;
        private System.Windows.Forms.Label _lblSqlUser;
        private System.Windows.Forms.TextBox _txtSqlUser;
        private System.Windows.Forms.Label _lblSqlPassword;
        private System.Windows.Forms.TextBox _txtSqlPassword;
        private System.Windows.Forms.Label _lblTimeout;
        private Theme.ModernNumericUpDown _nudTimeout;
        private System.Windows.Forms.CheckBox _chkTrustCert;
        private Theme.ModernButton _btnTestSql;

        // Step 2: Kaynaklar (DB + Dosya)
        private System.Windows.Forms.Label _lblStep2Header;
        private System.Windows.Forms.Label _lblStep2Hint;
        private System.Windows.Forms.CheckedListBox _clbDatabases;
        private Theme.ModernButton _btnRefreshDatabases;
        private System.Windows.Forms.CheckBox _chkSelectAll;
        private System.Windows.Forms.Label _lblStep2FileHeader;
        private System.Windows.Forms.CheckBox _chkFileBackupEnabled;
        private System.Windows.Forms.Panel _pnlFileBackup;
        private System.Windows.Forms.ListView _lvFileSources;
        private System.Windows.Forms.ColumnHeader _colFsName;
        private System.Windows.Forms.ColumnHeader _colFsPath;
        private System.Windows.Forms.ColumnHeader _colFsVss;
        private System.Windows.Forms.ColumnHeader _colFsStatus;
        private Theme.ModernButton _btnAddFileSource;
        private Theme.ModernButton _btnEditFileSource;
        private Theme.ModernButton _btnRemoveFileSource;

        // Step 3: Strateji + Zamanlama
        private System.Windows.Forms.Label _lblStep3Header;
        private System.Windows.Forms.Label _lblStrategy;
        private System.Windows.Forms.ComboBox _cmbStrategy;
        private System.Windows.Forms.Label _lblFullCron;
        private Controls.CronBuilderPanel _cronFull;
        private System.Windows.Forms.Label _lblDiffCron;
        private Controls.CronBuilderPanel _cronDiff;
        private System.Windows.Forms.Label _lblIncrCron;
        private Controls.CronBuilderPanel _cronIncr;
        private System.Windows.Forms.Label _lblAutoPromote;
        private Theme.ModernNumericUpDown _nudAutoPromote;
        private System.Windows.Forms.CheckBox _chkVerify;
        private System.Windows.Forms.Label _lblStep3FileSchedHeader;
        private System.Windows.Forms.Label _lblStep3FileSep;
        private System.Windows.Forms.Label _lblFileSchedule;
        private Controls.CronBuilderPanel _cronFileSchedule;

        // Step 4: Sikistirma + Saklama
        private System.Windows.Forms.Label _lblStep4Header;
        private System.Windows.Forms.Label _lblAlgorithm;
        private System.Windows.Forms.ComboBox _cmbAlgorithm;
        private System.Windows.Forms.Label _lblLevel;
        private System.Windows.Forms.ComboBox _cmbLevel;
        private System.Windows.Forms.Label _lblArchivePassword;
        private System.Windows.Forms.TextBox _txtArchivePassword;
        private System.Windows.Forms.Label _lblStep4RetHeader;
        private System.Windows.Forms.Label _lblRetentionTemplate;
        private System.Windows.Forms.ComboBox _cmbRetentionTemplate;
        private System.Windows.Forms.Label _lblRetentionTemplateInfo;
        private System.Windows.Forms.Label _lblRetention;
        private System.Windows.Forms.ComboBox _cmbRetention;
        private System.Windows.Forms.Label _lblKeepLastN;
        private Theme.ModernNumericUpDown _nudKeepLastN;
        private System.Windows.Forms.Label _lblDeleteDays;
        private Theme.ModernNumericUpDown _nudDeleteDays;
        private System.Windows.Forms.CheckBox _chkProtectPlan;
        private System.Windows.Forms.TextBox _txtPlanPassword;
        private System.Windows.Forms.TextBox _txtRecoveryPassword;

        // Step 5: Hedefler
        private System.Windows.Forms.Label _lblStep5Header;
        private System.Windows.Forms.Label _lblStep5Hint;
        private System.Windows.Forms.ListView _lvCloudTargets;
        private System.Windows.Forms.ColumnHeader _colCtName;
        private System.Windows.Forms.ColumnHeader _colCtType;
        private System.Windows.Forms.ColumnHeader _colCtStatus;
        private Theme.ModernButton _btnAddCloud;
        private Theme.ModernButton _btnEditCloud;
        private Theme.ModernButton _btnRemoveCloud;

        // Step 6: Bildirim + Raporlama
        private System.Windows.Forms.Label _lblStep6Header;
        private System.Windows.Forms.CheckBox _chkEmailEnabled;
        private System.Windows.Forms.Label _lblSmtpProfile;
        private System.Windows.Forms.ComboBox _cmbSmtpProfile;
        private System.Windows.Forms.LinkLabel _lnkOpenSmtpSettings;
        private System.Windows.Forms.CheckBox _chkNotifySuccess;
        private System.Windows.Forms.CheckBox _chkNotifyFailure;
        private System.Windows.Forms.CheckBox _chkToast;
        private System.Windows.Forms.Label _lblStep6ReportHeader;
        private System.Windows.Forms.CheckBox _chkReportEnabled;
        private System.Windows.Forms.Label _lblReportFreq;
        private System.Windows.Forms.ComboBox _cmbReportFreq;
        private System.Windows.Forms.Label _lblReportEmail;
        private System.Windows.Forms.TextBox _txtReportEmail;
        private System.Windows.Forms.Label _lblReportHour;
        private Theme.ModernNumericUpDown _nudReportHour;
    }
}
