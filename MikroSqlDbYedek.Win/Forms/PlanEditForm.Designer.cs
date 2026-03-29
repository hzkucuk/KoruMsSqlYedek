namespace MikroSqlDbYedek.Win.Forms
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
            _btnBack = new Theme.ModernButton();
            _btnNext = new Theme.ModernButton();
            _btnSave = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();

            // === 6 Step panels ===
            _stepPanels = new System.Windows.Forms.Panel[6];
            for (int i = 0; i < 6; i++)
            {
                _stepPanels[i] = new System.Windows.Forms.Panel();
                _stepPanels[i].Dock = System.Windows.Forms.DockStyle.Fill;
                _stepPanels[i].AutoScroll = true;
                _stepPanels[i].Padding = new System.Windows.Forms.Padding(24, 16, 24, 12);
            }

            // === Step indicator labels ===
            _stepLabels = new System.Windows.Forms.Label[6];
            _stepDots = new System.Windows.Forms.Label[6];
            string[] stepTitles = new string[]
            {
                "Ba\u011flant\u0131",
                "Kaynaklar",
                "Zamanlama",
                "S\u0131k\u0131\u015ft\u0131rma",
                "Hedefler",
                "Bildirim"
            };

            // ========== STEP 1: Plan Bilgileri + SQL Bağlantı ==========
            _lblStep1Header = new System.Windows.Forms.Label();
            _lblPlanName = new System.Windows.Forms.Label();
            _txtPlanName = new System.Windows.Forms.TextBox();
            _chkEnabled = new System.Windows.Forms.CheckBox();
            _lblLocalPath = new System.Windows.Forms.Label();
            _txtLocalPath = new System.Windows.Forms.TextBox();
            _btnBrowseLocal = new Theme.ModernButton();
            _lblServer = new System.Windows.Forms.Label();
            _txtServer = new System.Windows.Forms.TextBox();
            _lblAuthMode = new System.Windows.Forms.Label();
            _cmbAuthMode = new System.Windows.Forms.ComboBox();
            _lblSqlUser = new System.Windows.Forms.Label();
            _txtSqlUser = new System.Windows.Forms.TextBox();
            _lblSqlPassword = new System.Windows.Forms.Label();
            _txtSqlPassword = new System.Windows.Forms.TextBox();
            _lblTimeout = new System.Windows.Forms.Label();
            _nudTimeout = new Theme.ModernNumericUpDown();
            _chkTrustCert = new System.Windows.Forms.CheckBox();
            _btnTestSql = new Theme.ModernButton();
            _lblStep1SqlHeader = new System.Windows.Forms.Label();
            _lblBackupMode = new System.Windows.Forms.Label();
            _rbModeLocal = new System.Windows.Forms.RadioButton();
            _rbModeCloud = new System.Windows.Forms.RadioButton();

            // ========== STEP 2: Kaynaklar (Veritabanları + Dosya) ==========
            _lblStep2Header = new System.Windows.Forms.Label();
            _lblStep2Hint = new System.Windows.Forms.Label();
            _clbDatabases = new System.Windows.Forms.CheckedListBox();
            _btnRefreshDatabases = new Theme.ModernButton();
            _chkSelectAll = new System.Windows.Forms.CheckBox();
            _lblStep2FileHeader = new System.Windows.Forms.Label();
            _chkFileBackupEnabled = new System.Windows.Forms.CheckBox();
            _pnlFileBackup = new System.Windows.Forms.Panel();
            _lvFileSources = new System.Windows.Forms.ListView();
            _colFsName = new System.Windows.Forms.ColumnHeader();
            _colFsPath = new System.Windows.Forms.ColumnHeader();
            _colFsVss = new System.Windows.Forms.ColumnHeader();
            _colFsStatus = new System.Windows.Forms.ColumnHeader();
            _btnAddFileSource = new Theme.ModernButton();
            _btnEditFileSource = new Theme.ModernButton();
            _btnRemoveFileSource = new Theme.ModernButton();

            // ========== STEP 3: Strateji + Zamanlama ==========
            _lblStep3Header = new System.Windows.Forms.Label();
            _lblStrategy = new System.Windows.Forms.Label();
            _cmbStrategy = new System.Windows.Forms.ComboBox();
            _lblFullCron = new System.Windows.Forms.Label();
            _cronFull = new Controls.CronBuilderPanel();
            _lblDiffCron = new System.Windows.Forms.Label();
            _cronDiff = new Controls.CronBuilderPanel();
            _lblIncrCron = new System.Windows.Forms.Label();
            _cronIncr = new Controls.CronBuilderPanel();
            _lblAutoPromote = new System.Windows.Forms.Label();
            _nudAutoPromote = new Theme.ModernNumericUpDown();
            _chkVerify = new System.Windows.Forms.CheckBox();
            _lblStep3FileSchedHeader = new System.Windows.Forms.Label();
            _lblFileSchedule = new System.Windows.Forms.Label();
            _cronFileSchedule = new Controls.CronBuilderPanel();

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
            _nudKeepLastN = new Theme.ModernNumericUpDown();
            _lblDeleteDays = new System.Windows.Forms.Label();
            _nudDeleteDays = new Theme.ModernNumericUpDown();

            // ========== STEP 5: Hedefler (Bulut/Uzak) ==========
            _lblStep5Header = new System.Windows.Forms.Label();
            _lblStep5Hint = new System.Windows.Forms.Label();
            _lvCloudTargets = new System.Windows.Forms.ListView();
            _colCtName = new System.Windows.Forms.ColumnHeader();
            _colCtType = new System.Windows.Forms.ColumnHeader();
            _colCtStatus = new System.Windows.Forms.ColumnHeader();
            _btnAddCloud = new Theme.ModernButton();
            _btnEditCloud = new Theme.ModernButton();
            _btnRemoveCloud = new Theme.ModernButton();

            // ========== STEP 6: Bildirim + Raporlama ==========
            _lblStep6Header = new System.Windows.Forms.Label();
            _chkEmailEnabled = new System.Windows.Forms.CheckBox();
            _pnlSmtp = new System.Windows.Forms.Panel();
            _lblEmailTo = new System.Windows.Forms.Label();
            _txtEmailTo = new System.Windows.Forms.TextBox();
            _lblSmtpServer = new System.Windows.Forms.Label();
            _txtSmtpServer = new System.Windows.Forms.TextBox();
            _lblSmtpPort = new System.Windows.Forms.Label();
            _nudSmtpPort = new Theme.ModernNumericUpDown();
            _chkSmtpSsl = new System.Windows.Forms.CheckBox();
            _lblSmtpUser = new System.Windows.Forms.Label();
            _txtSmtpUser = new System.Windows.Forms.TextBox();
            _lblSmtpPassword = new System.Windows.Forms.Label();
            _txtSmtpPassword = new System.Windows.Forms.TextBox();
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
            _nudReportHour = new Theme.ModernNumericUpDown();

            SuspendLayout();

            // --- ToolTip configuration ---
            _toolTip.AutoPopDelay = 15000;
            _toolTip.InitialDelay = 400;
            _toolTip.ReshowDelay = 200;
            _toolTip.ShowAlways = true;

            int y;
            int lx = 0, tx = 150, tw = 420;

            // ===================================================================
            // STEP INDICATOR (top bar)
            // ===================================================================
            _pnlStepIndicator.Dock = System.Windows.Forms.DockStyle.Top;
            _pnlStepIndicator.Height = 56;
            _pnlStepIndicator.BackColor = Theme.ModernTheme.SurfaceColor;
            _pnlStepIndicator.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);

            int stepW = 103;
            int stepStartX = 6;
            for (int i = 0; i < 6; i++)
            {
                _stepDots[i] = new System.Windows.Forms.Label();
                _stepDots[i].Text = (i + 1).ToString();
                _stepDots[i].Size = new System.Drawing.Size(24, 24);
                _stepDots[i].Location = new System.Drawing.Point(stepStartX + i * stepW, 6);
                _stepDots[i].TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                _stepDots[i].Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
                _pnlStepIndicator.Controls.Add(_stepDots[i]);

                _stepLabels[i] = new System.Windows.Forms.Label();
                _stepLabels[i].Text = stepTitles[i];
                _stepLabels[i].AutoSize = false;
                _stepLabels[i].Size = new System.Drawing.Size(stepW - 6, 18);
                _stepLabels[i].Location = new System.Drawing.Point(stepStartX + i * stepW, 32);
                _stepLabels[i].Font = new System.Drawing.Font("Segoe UI", 7.5F);
                _stepLabels[i].ForeColor = Theme.ModernTheme.TextSecondary;
                _pnlStepIndicator.Controls.Add(_stepLabels[i]);
            }

            // ===================================================================
            // STEP 1: Plan Bilgileri + SQL Sunucu Baglantisi
            // ===================================================================
            var step1 = _stepPanels[0];
            y = 5;

            _lblStep1Header.Text = "\u2460 Plan Bilgileri";
            _lblStep1Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep1Header.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep1Header.AutoSize = true;
            _lblStep1Header.Location = new System.Drawing.Point(lx, y);
            step1.Controls.Add(_lblStep1Header);
            y += 30;

            ConfigLabel(_lblPlanName, "Plan Ad\u0131:", lx, y, step1);
            ConfigTextBox(_txtPlanName, tx, y, tw, step1);
            _toolTip.SetToolTip(_txtPlanName, "Bu yedekleme plan\u0131n\u0131n benzersiz ad\u0131.\n\u00d6rnek: \"Mikro Veri Gece Yedek\", \"Haftal\u0131k Ar\u015fiv\"");
            y += 30;

            _chkEnabled.Text = "Plan Aktif";
            _chkEnabled.Location = new System.Drawing.Point(tx, y);
            _chkEnabled.AutoSize = true;
            _chkEnabled.Checked = true;
            _toolTip.SetToolTip(_chkEnabled, "Devre d\u0131\u015f\u0131 b\u0131rak\u0131rsan\u0131z plan zamanlay\u0131c\u0131 taraf\u0131ndan \u00e7al\u0131\u015ft\u0131r\u0131lmaz.\nManuel yedek almaya devam edebilirsiniz.");
            step1.Controls.Add(_chkEnabled);
            y += 32;

            _lblBackupMode.Text = "Yedekleme Modu:";
            _lblBackupMode.AutoSize = true;
            _lblBackupMode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            _lblBackupMode.Location = new System.Drawing.Point(lx, y + 3);
            step1.Controls.Add(_lblBackupMode);
            _rbModeLocal.Text = "Yerel (Disk / UNC / A\u011f Payla\u015f\u0131m\u0131)";
            _rbModeLocal.AutoSize = true;
            _rbModeLocal.Checked = true;
            _rbModeLocal.Location = new System.Drawing.Point(tx, y);
            _rbModeLocal.CheckedChanged += OnBackupModeChanged;
            _toolTip.SetToolTip(_rbModeLocal, "Yedek dosyalar\u0131 yaln\u0131zca yerel diske,\nharici diske veya a\u011f payla\u015f\u0131m\u0131na (UNC) kaydedilir.\nBulut y\u00fckleme ad\u0131m\u0131 atlan\u0131r.");
            step1.Controls.Add(_rbModeLocal);
            y += 22;
            _rbModeCloud.Text = "Bulut (Google Drive / OneDrive / FTP / SFTP)";
            _rbModeCloud.AutoSize = true;
            _rbModeCloud.Location = new System.Drawing.Point(tx, y);
            _toolTip.SetToolTip(_rbModeCloud, "Yedek dosyalar\u0131 \u00f6nce yerele kaydedilir,\nsonra bulut sa\u011flay\u0131c\u0131lar\u0131na y\u00fcklenir.\nHedef yap\u0131land\u0131rma ad\u0131m\u0131 g\u00f6sterilir.");
            step1.Controls.Add(_rbModeCloud);
            y += 30;

            ConfigLabel(_lblLocalPath, "Yerel Yedek Klas\u00f6r\u00fc:", lx, y, step1);
            ConfigTextBox(_txtLocalPath, tx, y, tw - 36, step1);
            _toolTip.SetToolTip(_txtLocalPath, "Yedek dosyalar\u0131n\u0131n kaydedilece\u011fi yerel dizin.\n\u00d6rnek: D:\\Backups\\MikroSqlDbYedek\nNot: Yeterli disk alan\u0131 oldu\u011fundan emin olun.");
            _btnBrowseLocal.Text = "...";
            _btnBrowseLocal.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBrowseLocal.Size = new System.Drawing.Size(30, 26);
            _btnBrowseLocal.Location = new System.Drawing.Point(tx + tw - 30, y);
            _btnBrowseLocal.Click += OnBrowseLocalPathClick;
            step1.Controls.Add(_btnBrowseLocal);
            y += 40;

            _lblStep1SqlHeader.Text = "\u2461 SQL Server Ba\u011flant\u0131s\u0131";
            _lblStep1SqlHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep1SqlHeader.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep1SqlHeader.AutoSize = true;
            _lblStep1SqlHeader.Location = new System.Drawing.Point(lx, y);
            step1.Controls.Add(_lblStep1SqlHeader);
            y += 30;

            ConfigLabel(_lblServer, "Sunucu Ad\u0131 / IP:", lx, y, step1);
            ConfigTextBox(_txtServer, tx, y, tw, step1);
            _toolTip.SetToolTip(_txtServer, "SQL Server sunucu adresi.\n\u00d6rnekler:\n  \u2022 localhost\n  \u2022 192.168.1.100\n  \u2022 SUNUCU\\SQLEXPRESS\n  \u2022 sunucu.domain.local,1433");
            y += 30;

            ConfigLabel(_lblAuthMode, "Kimlik Do\u011frulama:", lx, y, step1);
            _cmbAuthMode.Location = new System.Drawing.Point(tx, y);
            _cmbAuthMode.Size = new System.Drawing.Size(tw, 23);
            _cmbAuthMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbAuthMode.SelectedIndexChanged += OnAuthModeChanged;
            _toolTip.SetToolTip(_cmbAuthMode, "Windows: Mevcut oturum bilgileriyle ba\u011flan\u0131r.\nSQL Server: Kullan\u0131c\u0131 ad\u0131 ve \u015fifre gerekir.");
            step1.Controls.Add(_cmbAuthMode);
            y += 30;

            ConfigLabel(_lblSqlUser, "Kullan\u0131c\u0131 Ad\u0131:", lx, y, step1);
            ConfigTextBox(_txtSqlUser, tx, y, tw, step1);
            _toolTip.SetToolTip(_txtSqlUser, "SQL Server kimlik do\u011frulamas\u0131 i\u00e7in kullan\u0131c\u0131 ad\u0131.\n\u00d6rnek: sa, backupuser");
            y += 30;

            ConfigLabel(_lblSqlPassword, "\u015eifre:", lx, y, step1);
            _txtSqlPassword.Location = new System.Drawing.Point(tx, y);
            _txtSqlPassword.Size = new System.Drawing.Size(tw, 23);
            _txtSqlPassword.UseSystemPasswordChar = true;
            _toolTip.SetToolTip(_txtSqlPassword, "SQL Server \u015fifresi. DPAPI ile \u015fifreli saklan\u0131r,\nd\u00fcz metin olarak kaydedilmez.");
            step1.Controls.Add(_txtSqlPassword);
            y += 30;

            ConfigLabel(_lblTimeout, "Zaman A\u015f\u0131m\u0131 (sn):", lx, y, step1);
            _nudTimeout.Location = new System.Drawing.Point(tx, y);
            _nudTimeout.Size = new System.Drawing.Size(80, 23);
            _nudTimeout.Minimum = 5;
            _nudTimeout.Maximum = 300;
            _nudTimeout.Value = 30;
            _toolTip.SetToolTip(_nudTimeout, "Sunucuya ba\u011flanma s\u00fcresi (saniye).\nVarsay\u0131lan: 30 sn. A\u011f yava\u015fsa art\u0131r\u0131n.");
            step1.Controls.Add(_nudTimeout);

            _chkTrustCert.Text = "Sertifikaya G\u00fcven";
            _chkTrustCert.Location = new System.Drawing.Point(tx + 100, y);
            _chkTrustCert.AutoSize = true;
            _chkTrustCert.Checked = true;
            _toolTip.SetToolTip(_chkTrustCert, "Yerel veya test sunucular\u0131nda self-signed\nSSL sertifikas\u0131 kullan\u0131l\u0131yorsa i\u015faretleyin.\nKapal\u0131ysa ge\u00e7erli CA sertifikas\u0131 gerekir.");
            step1.Controls.Add(_chkTrustCert);
            y += 36;

            _btnTestSql.Text = "Ba\u011flant\u0131y\u0131 S\u0131na";
            _btnTestSql.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnTestSql.Location = new System.Drawing.Point(tx, y);
            _btnTestSql.Size = new System.Drawing.Size(170, 34);
            _btnTestSql.Click += OnTestSqlConnectionClick;
            _toolTip.SetToolTip(_btnTestSql, "Girdi\u011finiz bilgilerle SQL Server ba\u011flant\u0131s\u0131n\u0131 test eder.\nBa\u015far\u0131l\u0131ysa veritabanlar\u0131 otomatik listelenir.");
            step1.Controls.Add(_btnTestSql);

            // ===================================================================
            // STEP 2: Kaynaklar (Veritabanlari + Dosya Yedekleme)
            // ===================================================================
            var step2 = _stepPanels[1];
            y = 5;

            _lblStep2Header.Text = "\u2462 Yedeklenecek Veritabanlar\u0131";
            _lblStep2Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep2Header.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep2Header.AutoSize = true;
            _lblStep2Header.Location = new System.Drawing.Point(lx, y);
            step2.Controls.Add(_lblStep2Header);
            y += 26;

            _lblStep2Hint.Text = "SQL ba\u011flant\u0131s\u0131 ba\u015far\u0131l\u0131ysa veritabanlar\u0131 otomatik listelenir.";
            _lblStep2Hint.AutoSize = true;
            _lblStep2Hint.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblStep2Hint.Location = new System.Drawing.Point(lx, y);
            step2.Controls.Add(_lblStep2Hint);
            y += 22;

            _chkSelectAll.Text = "T\u00fcm\u00fcn\u00fc Se\u00e7";
            _chkSelectAll.AutoSize = true;
            _chkSelectAll.Location = new System.Drawing.Point(lx, y);
            _chkSelectAll.CheckedChanged += OnSelectAllChanged;
            _toolTip.SetToolTip(_chkSelectAll, "Listedeki t\u00fcm veritabanlar\u0131n\u0131 se\u00e7er veya se\u00e7imi kald\u0131r\u0131r.");
            step2.Controls.Add(_chkSelectAll);

            _btnRefreshDatabases.Text = "Yenile";
            _btnRefreshDatabases.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnRefreshDatabases.Size = new System.Drawing.Size(100, 26);
            _btnRefreshDatabases.Location = new System.Drawing.Point(480, y - 2);
            _btnRefreshDatabases.Click += OnRefreshDatabasesClick;
            _toolTip.SetToolTip(_btnRefreshDatabases, "Sunucudaki veritabanlar\u0131n\u0131 yeniden sorgular.");
            step2.Controls.Add(_btnRefreshDatabases);
            y += 28;

            _clbDatabases.Location = new System.Drawing.Point(lx, y);
            _clbDatabases.Size = new System.Drawing.Size(580, 200);
            _clbDatabases.CheckOnClick = true;
            _toolTip.SetToolTip(_clbDatabases, "Yedeklemek istedi\u011finiz veritabanlar\u0131n\u0131 i\u015faretleyin.\nSistem veritabanlar\u0131 (master, model, msdb, tempdb) listelenmez.");
            step2.Controls.Add(_clbDatabases);
            y += 208;

            // --- Dosya Yedekleme Kaynakları ---
            _lblStep2FileHeader.Text = "\u2463 Dosya / Klas\u00f6r Kaynaklar\u0131 (VSS)";
            _lblStep2FileHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep2FileHeader.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep2FileHeader.AutoSize = true;
            _lblStep2FileHeader.Location = new System.Drawing.Point(lx, y);
            step2.Controls.Add(_lblStep2FileHeader);
            y += 26;

            _chkFileBackupEnabled.Text = "Dosya Yedeklemeyi Etkinle\u015ftir";
            _chkFileBackupEnabled.Location = new System.Drawing.Point(lx, y);
            _chkFileBackupEnabled.AutoSize = true;
            _chkFileBackupEnabled.CheckedChanged += OnFileBackupEnabledChanged;
            _toolTip.SetToolTip(_chkFileBackupEnabled, "A\u00e7\u0131k/kilitli dosyalar\u0131 (Outlook PST/OST,\nExcel, SQL MDF) VSS ile yedekler.\nSQL yedeklemeden ba\u011f\u0131ms\u0131z \u00e7al\u0131\u015f\u0131r.");
            step2.Controls.Add(_chkFileBackupEnabled);
            y += 26;

            _pnlFileBackup.Location = new System.Drawing.Point(lx, y);
            _pnlFileBackup.Size = new System.Drawing.Size(580, 140);

            _lvFileSources.Location = new System.Drawing.Point(0, 0);
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

            int fbx = 484;
            _btnAddFileSource.Text = "Ekle";
            _btnAddFileSource.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnAddFileSource.Location = new System.Drawing.Point(fbx, 0);
            _btnAddFileSource.Size = new System.Drawing.Size(90, 28);
            _btnAddFileSource.Click += OnAddFileSource;
            _pnlFileBackup.Controls.Add(_btnAddFileSource);
            _btnEditFileSource.Text = "D\u00fczenle";
            _btnEditFileSource.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnEditFileSource.Location = new System.Drawing.Point(fbx, 32);
            _btnEditFileSource.Size = new System.Drawing.Size(90, 28);
            _btnEditFileSource.Click += OnEditFileSource;
            _pnlFileBackup.Controls.Add(_btnEditFileSource);
            _btnRemoveFileSource.Text = "Kald\u0131r";
            _btnRemoveFileSource.ButtonStyle = Theme.ModernButtonStyle.Danger;
            _btnRemoveFileSource.Location = new System.Drawing.Point(fbx, 64);
            _btnRemoveFileSource.Size = new System.Drawing.Size(90, 28);
            _btnRemoveFileSource.Click += OnRemoveFileSource;
            _pnlFileBackup.Controls.Add(_btnRemoveFileSource);
            step2.Controls.Add(_pnlFileBackup);

            // ===================================================================
            // STEP 3: Yedekleme Stratejisi & Zamanlama
            // ===================================================================
            var step3 = _stepPanels[2];
            y = 5;

            _lblStep3Header.Text = "\u2464 Yedekleme Stratejisi";
            _lblStep3Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep3Header.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep3Header.AutoSize = true;
            _lblStep3Header.Location = new System.Drawing.Point(lx, y);
            step3.Controls.Add(_lblStep3Header);
            y += 30;

            ConfigLabel(_lblStrategy, "Yedek T\u00fcr\u00fc:", lx, y, step3);
            _cmbStrategy.Location = new System.Drawing.Point(tx, y);
            _cmbStrategy.Size = new System.Drawing.Size(tw, 23);
            _cmbStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbStrategy.SelectedIndexChanged += OnStrategyChanged;
            _toolTip.SetToolTip(_cmbStrategy, "Yaln\u0131zca Tam: Her seferinde t\u00fcm veriyi yedekler.\nTam + Fark: D\u00fczenli tam + aradaki de\u011fi\u015fiklikler.\nTam + Fark + Art\u0131r\u0131ml\u0131: En verimli ama en karma\u015f\u0131k.");
            step3.Controls.Add(_cmbStrategy);
            y += 34;

            ConfigLabel(_lblFullCron, "Tam Yedek Zamanlamas\u0131:", lx, y, step3);
            _cronFull.Location = new System.Drawing.Point(tx, y);
            _cronFull.Size = new System.Drawing.Size(tw, 100);
            _toolTip.SetToolTip(_cronFull, "Tam yede\u011fin ne zaman al\u0131naca\u011f\u0131n\u0131 belirleyin.\nTam yedek t\u00fcm veritaban\u0131 verisini i\u00e7erir.");
            step3.Controls.Add(_cronFull);
            y += 90;

            ConfigLabel(_lblDiffCron, "Fark Yedek Zamanlamas\u0131:", lx, y, step3);
            _cronDiff.Location = new System.Drawing.Point(tx, y);
            _cronDiff.Size = new System.Drawing.Size(tw, 100);
            _toolTip.SetToolTip(_cronDiff, "Fark yedek, son tam yedekten bu yana\nde\u011fi\u015fen verileri yedekler. Daha h\u0131zl\u0131 ve k\u00fc\u00e7\u00fck.");
            step3.Controls.Add(_cronDiff);
            y += 90;

            ConfigLabel(_lblIncrCron, "Art\u0131r\u0131ml\u0131 Zamanlama:", lx, y, step3);
            _cronIncr.Location = new System.Drawing.Point(tx, y);
            _cronIncr.Size = new System.Drawing.Size(tw, 100);
            _toolTip.SetToolTip(_cronIncr, "Art\u0131r\u0131ml\u0131 yedek, son yedekten (tam veya art\u0131r\u0131ml\u0131)\nbu yana de\u011fi\u015fen verileri yedekler. En k\u00fc\u00e7\u00fck boyut.");
            step3.Controls.Add(_cronIncr);
            y += 96;

            ConfigLabel(_lblAutoPromote, "Otomatik Tam Yedek E\u015fi\u011fi:", lx, y, step3);
            _nudAutoPromote.Location = new System.Drawing.Point(tx, y);
            _nudAutoPromote.Size = new System.Drawing.Size(80, 23);
            _nudAutoPromote.Minimum = 1;
            _nudAutoPromote.Maximum = 100;
            _nudAutoPromote.Value = 7;
            _toolTip.SetToolTip(_nudAutoPromote, "Bu say\u0131da fark yedekten sonra otomatik\ntam yedek tetiklenir.\nVarsay\u0131lan: 7 (haftada bir tam yedek)");
            step3.Controls.Add(_nudAutoPromote);
            y += 32;

            _chkVerify.Text = "Yedek sonras\u0131 do\u011frulama yap (RESTORE VERIFYONLY)";
            _chkVerify.Location = new System.Drawing.Point(lx, y);
            _chkVerify.AutoSize = true;
            _chkVerify.Checked = true;
            _toolTip.SetToolTip(_chkVerify, "Her yedek sonras\u0131 SQL Server\u2019\u0131n RESTORE VERIFYONLY\nkomutuyla dosya b\u00fct\u00fcnl\u00fc\u011f\u00fc do\u011frulan\u0131r.\nBa\u015far\u0131s\u0131zl\u0131kta bildirim g\u00f6nderilir.");
            step3.Controls.Add(_chkVerify);
            y += 32;

            // --- Dosya zamanlama (file backup enabled ise görünür) ---
            _lblStep3FileSchedHeader.Text = "Dosya Yedekleme Zamanlamas\u0131";
            _lblStep3FileSchedHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            _lblStep3FileSchedHeader.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep3FileSchedHeader.AutoSize = true;
            _lblStep3FileSchedHeader.Location = new System.Drawing.Point(lx, y);
            step3.Controls.Add(_lblStep3FileSchedHeader);
            y += 26;

            ConfigLabel(_lblFileSchedule, "Dosya Zamanlamas\u0131:", lx, y, step3);
            _cronFileSchedule.Location = new System.Drawing.Point(tx, y);
            _cronFileSchedule.Size = new System.Drawing.Size(tw, 100);
            _toolTip.SetToolTip(_cronFileSchedule, "Dosya yedeklemenin zamanlamas\u0131.\nSQL yedekten ba\u011f\u0131ms\u0131z \u00e7al\u0131\u015f\u0131r.");
            step3.Controls.Add(_cronFileSchedule);

            // ===================================================================
            // STEP 4: Sikistirma & Saklama Politikasi
            // ===================================================================
            var step4 = _stepPanels[3];
            y = 5;

            _lblStep4Header.Text = "\u2465 Ar\u015fiv S\u0131k\u0131\u015ft\u0131rma Ayarlar\u0131";
            _lblStep4Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep4Header.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep4Header.AutoSize = true;
            _lblStep4Header.Location = new System.Drawing.Point(lx, y);
            step4.Controls.Add(_lblStep4Header);
            y += 32;

            ConfigLabel(_lblAlgorithm, "S\u0131k\u0131\u015ft\u0131rma Algoritmas\u0131:", lx, y, step4);
            _cmbAlgorithm.Location = new System.Drawing.Point(tx, y);
            _cmbAlgorithm.Size = new System.Drawing.Size(tw, 23);
            _cmbAlgorithm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _toolTip.SetToolTip(_cmbAlgorithm, "LZMA2: En iyi s\u0131k\u0131\u015ft\u0131rma oran\u0131 (\u00f6nerilen).\nLZMA: Eski uyumluluk i\u00e7in.\nBZip2: Orta seviye.\nDeflate: En h\u0131zl\u0131, en b\u00fcy\u00fck dosya.");
            step4.Controls.Add(_cmbAlgorithm);
            y += 34;

            ConfigLabel(_lblLevel, "S\u0131k\u0131\u015ft\u0131rma D\u00fczeyi:", lx, y, step4);
            _cmbLevel.Location = new System.Drawing.Point(tx, y);
            _cmbLevel.Size = new System.Drawing.Size(tw, 23);
            _cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _toolTip.SetToolTip(_cmbLevel, "Yok: S\u0131k\u0131\u015ft\u0131rma yap\u0131lmaz.\nH\u0131zl\u0131: D\u00fc\u015f\u00fck s\u0131k\u0131\u015ft\u0131rma, h\u0131zl\u0131 i\u015flem.\nNormal: Dengeli.\nMaksimum: Y\u00fcksek s\u0131k\u0131\u015ft\u0131rma.\nUltra: En y\u00fcksek s\u0131k\u0131\u015ft\u0131rma, en yava\u015f.");
            step4.Controls.Add(_cmbLevel);
            y += 34;

            ConfigLabel(_lblArchivePassword, "Ar\u015fiv \u015eifresi:", lx, y, step4);
            _txtArchivePassword.Location = new System.Drawing.Point(tx, y);
            _txtArchivePassword.Size = new System.Drawing.Size(tw, 23);
            _txtArchivePassword.UseSystemPasswordChar = true;
            _toolTip.SetToolTip(_txtArchivePassword, "7z ar\u015fivi i\u00e7in AES-256 \u015fifreleme.\nBo\u015f b\u0131rak\u0131rsan\u0131z ar\u015fiv \u015fifresiz olu\u015fturulur.\n\u015eifre DPAPI ile korunarak saklan\u0131r.");
            step4.Controls.Add(_txtArchivePassword);
            y += 48;

            _lblStep4RetHeader.Text = "Saklama (Retention) Politikas\u0131";
            _lblStep4RetHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep4RetHeader.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep4RetHeader.AutoSize = true;
            _lblStep4RetHeader.Location = new System.Drawing.Point(lx, y);
            step4.Controls.Add(_lblStep4RetHeader);
            y += 32;

            ConfigLabel(_lblRetention, "Temizlik Kural\u0131:", lx, y, step4);
            _cmbRetention.Location = new System.Drawing.Point(tx, y);
            _cmbRetention.Size = new System.Drawing.Size(tw, 23);
            _cmbRetention.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbRetention.SelectedIndexChanged += OnRetentionChanged;
            _toolTip.SetToolTip(_cmbRetention, "Son N Adet: En son N yede\u011fi sakla, eskilerini sil.\nG\u00fcn S\u0131n\u0131r\u0131: Belirtilen g\u00fcnden eski yedekleri sil.\nHer \u0130kisi: \u0130ki kural birlikte uygulan\u0131r.");
            step4.Controls.Add(_cmbRetention);
            y += 34;

            ConfigLabel(_lblKeepLastN, "Saklanacak Yedek Say\u0131s\u0131:", lx, y, step4);
            _nudKeepLastN.Location = new System.Drawing.Point(tx, y);
            _nudKeepLastN.Size = new System.Drawing.Size(80, 23);
            _nudKeepLastN.Minimum = 1;
            _nudKeepLastN.Maximum = 999;
            _nudKeepLastN.Value = 30;
            _toolTip.SetToolTip(_nudKeepLastN, "En son ka\u00e7 yedek dosyas\u0131 saklanacak.\nVarsay\u0131lan: 30");
            step4.Controls.Add(_nudKeepLastN);
            y += 34;

            ConfigLabel(_lblDeleteDays, "Silme S\u00fcresi (g\u00fcn):", lx, y, step4);
            _nudDeleteDays.Location = new System.Drawing.Point(tx, y);
            _nudDeleteDays.Size = new System.Drawing.Size(80, 23);
            _nudDeleteDays.Minimum = 1;
            _nudDeleteDays.Maximum = 3650;
            _nudDeleteDays.Value = 90;
            _toolTip.SetToolTip(_nudDeleteDays, "Bu g\u00fcn say\u0131s\u0131ndan eski yedekler silinir.\nVarsay\u0131lan: 90 g\u00fcn (3 ay)");
            step4.Controls.Add(_nudDeleteDays);

            // ===================================================================
            // STEP 5: Hedefler (Bulut / Uzak)
            // ===================================================================
            var step5 = _stepPanels[4];
            y = 5;

            _lblStep5Header.Text = "\u2466 Yedek G\u00f6nderim Hedefleri";
            _lblStep5Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep5Header.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep5Header.AutoSize = true;
            _lblStep5Header.Location = new System.Drawing.Point(lx, y);
            step5.Controls.Add(_lblStep5Header);
            y += 26;

            _lblStep5Hint.Text = "Yedek dosyalar\u0131n\u0131n kopyalanaca\u011f\u0131 bulut veya uzak hedefleri yönetin.\nGoogle Drive, OneDrive, FTP/SFTP, yerel klas\u00f6r ve UNC a\u011f paylas\u0131m\u0131 desteklenir.";
            _lblStep5Hint.AutoSize = true;
            _lblStep5Hint.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblStep5Hint.Location = new System.Drawing.Point(lx, y);
            _lblStep5Hint.MaximumSize = new System.Drawing.Size(580, 0);
            step5.Controls.Add(_lblStep5Hint);
            y += 40;

            _lvCloudTargets.Location = new System.Drawing.Point(lx, y);
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
            step5.Controls.Add(_lvCloudTargets);

            int bx = 484;
            _btnAddCloud.Text = "Ekle";
            _btnAddCloud.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnAddCloud.Location = new System.Drawing.Point(bx, y);
            _btnAddCloud.Size = new System.Drawing.Size(90, 30);
            _btnAddCloud.Click += OnAddCloudTarget;
            step5.Controls.Add(_btnAddCloud);
            _btnEditCloud.Text = "D\u00fczenle";
            _btnEditCloud.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnEditCloud.Location = new System.Drawing.Point(bx, y + 34);
            _btnEditCloud.Size = new System.Drawing.Size(90, 30);
            _btnEditCloud.Click += OnEditCloudTarget;
            step5.Controls.Add(_btnEditCloud);
            _btnRemoveCloud.Text = "Kald\u0131r";
            _btnRemoveCloud.ButtonStyle = Theme.ModernButtonStyle.Danger;
            _btnRemoveCloud.Location = new System.Drawing.Point(bx, y + 68);
            _btnRemoveCloud.Size = new System.Drawing.Size(90, 30);
            _btnRemoveCloud.Click += OnRemoveCloudTarget;
            step5.Controls.Add(_btnRemoveCloud);

            // ===================================================================
            // STEP 6: Bildirim + Raporlama
            // ===================================================================
            var step6 = _stepPanels[5];
            y = 5;

            _lblStep6Header.Text = "\u2467 E-posta Bildirimleri";
            _lblStep6Header.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep6Header.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep6Header.AutoSize = true;
            _lblStep6Header.Location = new System.Drawing.Point(lx, y);
            step6.Controls.Add(_lblStep6Header);
            y += 28;

            _chkEmailEnabled.Text = "E-posta Bildirimi G\u00f6nder";
            _chkEmailEnabled.Location = new System.Drawing.Point(lx, y);
            _chkEmailEnabled.AutoSize = true;
            _chkEmailEnabled.CheckedChanged += OnEmailEnabledChanged;
            _toolTip.SetToolTip(_chkEmailEnabled, "Yedekleme sonu\u00e7lar\u0131 e-posta ile bildirilir.\nSMTP ayarlar\u0131n\u0131n do\u011fru girilmesi gerekir.");
            step6.Controls.Add(_chkEmailEnabled);

            _chkToast.Text = "Windows Bildirim Balonu";
            _chkToast.Location = new System.Drawing.Point(260, y);
            _chkToast.AutoSize = true;
            _chkToast.Checked = true;
            _toolTip.SetToolTip(_chkToast, "G\u00f6rev \u00e7ubu\u011fu bildirim balonuyla\nyedek durumunu g\u00f6sterir.");
            step6.Controls.Add(_chkToast);
            y += 28;

            _pnlSmtp.Location = new System.Drawing.Point(lx, y);
            _pnlSmtp.Size = new System.Drawing.Size(580, 180);
            int sy = 0;
            ConfigLabel(_lblEmailTo, "Al\u0131c\u0131 E-posta:", 0, sy, _pnlSmtp);
            ConfigTextBox(_txtEmailTo, 130, sy, 440, _pnlSmtp);
            _toolTip.SetToolTip(_txtEmailTo, "Bildirim g\u00f6nderilecek e-posta adresi.\nBirden fazla: virg\u00fclle ay\u0131r\u0131n.\n\u00d6rnek: admin@sirket.com, yonetici@sirket.com");
            sy += 28;
            ConfigLabel(_lblSmtpServer, "SMTP Sunucusu:", 0, sy, _pnlSmtp);
            ConfigTextBox(_txtSmtpServer, 130, sy, 280, _pnlSmtp);
            _toolTip.SetToolTip(_txtSmtpServer, "SMTP sunucu adresi.\n\u00d6rnekler: smtp.gmail.com, smtp.office365.com");
            sy += 28;
            ConfigLabel(_lblSmtpPort, "SMTP Portu:", 0, sy, _pnlSmtp);
            _nudSmtpPort.Location = new System.Drawing.Point(130, sy);
            _nudSmtpPort.Size = new System.Drawing.Size(80, 23);
            _nudSmtpPort.Minimum = 1;
            _nudSmtpPort.Maximum = 65535;
            _nudSmtpPort.Value = 587;
            _toolTip.SetToolTip(_nudSmtpPort, "SMTP portu. Yayg\u0131n de\u011ferler:\n  587 = STARTTLS (\u00f6nerilen)\n  465 = SSL/TLS\n  25 = \u015eifresiz (tavsiye edilmez)");
            _pnlSmtp.Controls.Add(_nudSmtpPort);
            _chkSmtpSsl.Text = "SSL/TLS Kullan";
            _chkSmtpSsl.Location = new System.Drawing.Point(230, sy);
            _chkSmtpSsl.AutoSize = true;
            _chkSmtpSsl.Checked = true;
            _toolTip.SetToolTip(_chkSmtpSsl, "G\u00fcvenli ba\u011flant\u0131 (SSL/TLS).\nPort 587 veya 465 i\u00e7in a\u00e7\u0131k b\u0131rak\u0131n.");
            _pnlSmtp.Controls.Add(_chkSmtpSsl);
            sy += 28;
            ConfigLabel(_lblSmtpUser, "SMTP Kullan\u0131c\u0131:", 0, sy, _pnlSmtp);
            ConfigTextBox(_txtSmtpUser, 130, sy, 280, _pnlSmtp);
            _toolTip.SetToolTip(_txtSmtpUser, "SMTP kimlik do\u011frulamas\u0131 i\u00e7in kullan\u0131c\u0131.\nGenellikle e-posta adresiniz.");
            sy += 28;
            ConfigLabel(_lblSmtpPassword, "SMTP \u015eifresi:", 0, sy, _pnlSmtp);
            _txtSmtpPassword.Location = new System.Drawing.Point(130, sy);
            _txtSmtpPassword.Size = new System.Drawing.Size(280, 23);
            _txtSmtpPassword.UseSystemPasswordChar = true;
            _toolTip.SetToolTip(_txtSmtpPassword, "SMTP \u015fifresi. DPAPI ile \u015fifreli saklan\u0131r.\nGmail i\u00e7in \"Uygulama \u015eifresi\" kullan\u0131n.");
            _pnlSmtp.Controls.Add(_txtSmtpPassword);
            sy += 32;
            _chkNotifySuccess.Text = "Ba\u015far\u0131l\u0131 yedekte bildir";
            _chkNotifySuccess.Location = new System.Drawing.Point(0, sy);
            _chkNotifySuccess.AutoSize = true;
            _chkNotifySuccess.Checked = true;
            _pnlSmtp.Controls.Add(_chkNotifySuccess);
            _chkNotifyFailure.Text = "Ba\u015far\u0131s\u0131z yedekte bildir";
            _chkNotifyFailure.Location = new System.Drawing.Point(200, sy);
            _chkNotifyFailure.AutoSize = true;
            _chkNotifyFailure.Checked = true;
            _pnlSmtp.Controls.Add(_chkNotifyFailure);
            step6.Controls.Add(_pnlSmtp);
            y += 186;

            // --- Periyodik Rapor ---
            _lblStep6ReportHeader.Text = "\u2468 Periyodik Yedek Raporu";
            _lblStep6ReportHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            _lblStep6ReportHeader.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblStep6ReportHeader.AutoSize = true;
            _lblStep6ReportHeader.Location = new System.Drawing.Point(lx, y);
            step6.Controls.Add(_lblStep6ReportHeader);
            y += 28;

            _chkReportEnabled.Text = "D\u00fczenli Yedek Raporu G\u00f6nder";
            _chkReportEnabled.Location = new System.Drawing.Point(lx, y);
            _chkReportEnabled.AutoSize = true;
            _chkReportEnabled.CheckedChanged += OnReportEnabledChanged;
            _toolTip.SetToolTip(_chkReportEnabled, "Etkinle\u015ftirirseniz belirtilen s\u0131kl\u0131kta\nyedekleme \u00f6zet raporu e-posta ile g\u00f6nderilir.\n\u0130\u00e7erik: Ba\u015far\u0131l\u0131/ba\u015far\u0131s\u0131z say\u0131s\u0131, boyut, s\u00fcre.");
            step6.Controls.Add(_chkReportEnabled);
            y += 28;

            ConfigLabel(_lblReportFreq, "Rapor S\u0131kl\u0131\u011f\u0131:", lx, y, step6);
            _cmbReportFreq.Location = new System.Drawing.Point(130, y);
            _cmbReportFreq.Size = new System.Drawing.Size(140, 23);
            _cmbReportFreq.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _toolTip.SetToolTip(_cmbReportFreq, "G\u00fcnl\u00fck: Her g\u00fcn \u00f6nceki g\u00fcn\u00fcn \u00f6zeti.\nHaftal\u0131k: Her Pazartesi ge\u00e7en haftan\u0131n \u00f6zeti.\nAyl\u0131k: Her ay\u0131n 1\u2019inde ge\u00e7en ay\u0131n \u00f6zeti.");
            step6.Controls.Add(_cmbReportFreq);

            ConfigLabel(_lblReportHour, "G\u00f6nderim Saati:", 300, y, step6);
            _nudReportHour.Location = new System.Drawing.Point(408, y);
            _nudReportHour.Size = new System.Drawing.Size(60, 23);
            _nudReportHour.Minimum = 0;
            _nudReportHour.Maximum = 23;
            _nudReportHour.Value = 8;
            _toolTip.SetToolTip(_nudReportHour, "Raporun g\u00f6nderilece\u011fi saat (0\u201323).\nVarsay\u0131lan: 08:00");
            step6.Controls.Add(_nudReportHour);
            y += 30;

            ConfigLabel(_lblReportEmail, "Rapor Al\u0131c\u0131s\u0131:", lx, y, step6);
            ConfigTextBox(_txtReportEmail, 130, y, 440, step6);
            _toolTip.SetToolTip(_txtReportEmail, "Rapor g\u00f6nderilecek e-posta adresi.\nBo\u015f b\u0131rak\u0131l\u0131rsa bildirim e-postas\u0131 kullan\u0131l\u0131r.");

            // ===================================================================
            // NAVIGATION BAR (bottom)
            // ===================================================================
            _pnlNavigation.Dock = System.Windows.Forms.DockStyle.Bottom;
            _pnlNavigation.Height = 52;
            _pnlNavigation.BackColor = Theme.ModernTheme.SurfaceColor;
            _pnlNavigation.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);

            _btnCancel.Text = "\u0130ptal";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Ghost;
            _btnCancel.Size = new System.Drawing.Size(80, 34);
            _btnCancel.Dock = System.Windows.Forms.DockStyle.Left;
            _btnCancel.Click += OnCancelClick;

            _btnSave.Text = "Kaydet";
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Size = new System.Drawing.Size(100, 34);
            _btnSave.Dock = System.Windows.Forms.DockStyle.Right;
            _btnSave.Click += OnSaveClick;

            _btnNext.Text = "\u0130leri";
            _btnNext.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnNext.Size = new System.Drawing.Size(90, 34);
            _btnNext.Dock = System.Windows.Forms.DockStyle.Right;
            _btnNext.Click += OnNextClick;

            _btnBack.Text = "Geri";
            _btnBack.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBack.Size = new System.Drawing.Size(90, 34);
            _btnBack.Dock = System.Windows.Forms.DockStyle.Right;
            _btnBack.Click += OnBackClick;

            _pnlNavigation.Controls.Add(_btnCancel);
            _pnlNavigation.Controls.Add(_btnSave);
            _pnlNavigation.Controls.Add(_btnNext);
            _pnlNavigation.Controls.Add(_btnBack);

            // ===================================================================
            // CONTENT PANEL (holds step panels)
            // ===================================================================
            _pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlContent.Padding = new System.Windows.Forms.Padding(8);
            for (int i = 0; i < 6; i++)
            {
                _pnlContent.Controls.Add(_stepPanels[i]);
            }

            // ===================================================================
            // FORM
            // ===================================================================
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            ClientSize = new System.Drawing.Size(660, 680);
            Controls.Add(_pnlContent);
            Controls.Add(_pnlStepIndicator);
            Controls.Add(_pnlNavigation);
            Font = Theme.ModernTheme.FontBody;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PlanEditForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Yedekleme Plan\u0131 Sihirbaz\u0131";

            ResumeLayout(false);
        }

        private void ConfigLabel(System.Windows.Forms.Label lbl, string text, int x, int y, System.Windows.Forms.Control parent)
        {
            lbl.Text = text;
            lbl.AutoSize = true;
            lbl.Location = new System.Drawing.Point(x, y + 3);
            parent.Controls.Add(lbl);
        }

        private void ConfigTextBox(System.Windows.Forms.TextBox txt, int x, int y, int width, System.Windows.Forms.Control parent)
        {
            txt.Location = new System.Drawing.Point(x, y);
            txt.Size = new System.Drawing.Size(width, 23);
            parent.Controls.Add(txt);
        }

        #endregion

        // ToolTip
        private System.Windows.Forms.ToolTip _toolTip;

        // Wizard infrastructure
        private System.Windows.Forms.Panel _pnlStepIndicator;
        private System.Windows.Forms.Panel _pnlContent;
        private System.Windows.Forms.Panel _pnlNavigation;
        private System.Windows.Forms.Panel[] _stepPanels;
        private System.Windows.Forms.Label[] _stepLabels;
        private System.Windows.Forms.Label[] _stepDots;
        private Theme.ModernButton _btnBack;
        private Theme.ModernButton _btnNext;
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;

        // Backup mode selection
        private System.Windows.Forms.Label _lblBackupMode;
        private System.Windows.Forms.RadioButton _rbModeLocal;
        private System.Windows.Forms.RadioButton _rbModeCloud;
        private System.Collections.Generic.List<int> _activeSteps;

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
        private System.Windows.Forms.Label _lblRetention;
        private System.Windows.Forms.ComboBox _cmbRetention;
        private System.Windows.Forms.Label _lblKeepLastN;
        private Theme.ModernNumericUpDown _nudKeepLastN;
        private System.Windows.Forms.Label _lblDeleteDays;
        private Theme.ModernNumericUpDown _nudDeleteDays;

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
        private System.Windows.Forms.Panel _pnlSmtp;
        private System.Windows.Forms.Label _lblEmailTo;
        private System.Windows.Forms.TextBox _txtEmailTo;
        private System.Windows.Forms.Label _lblSmtpServer;
        private System.Windows.Forms.TextBox _txtSmtpServer;
        private System.Windows.Forms.Label _lblSmtpPort;
        private Theme.ModernNumericUpDown _nudSmtpPort;
        private System.Windows.Forms.CheckBox _chkSmtpSsl;
        private System.Windows.Forms.Label _lblSmtpUser;
        private System.Windows.Forms.TextBox _txtSmtpUser;
        private System.Windows.Forms.Label _lblSmtpPassword;
        private System.Windows.Forms.TextBox _txtSmtpPassword;
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
