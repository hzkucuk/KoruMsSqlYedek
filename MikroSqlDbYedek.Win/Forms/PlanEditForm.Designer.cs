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
            _tabControl = new System.Windows.Forms.TabControl();

            // === Tab pages ===
            _tabGeneral = new System.Windows.Forms.TabPage();
            _tabSql = new System.Windows.Forms.TabPage();
            _tabStrategy = new System.Windows.Forms.TabPage();
            _tabCompression = new System.Windows.Forms.TabPage();
            _tabCloud = new System.Windows.Forms.TabPage();
            _tabRetention = new System.Windows.Forms.TabPage();
            _tabNotification = new System.Windows.Forms.TabPage();
            _tabFileBackup = new System.Windows.Forms.TabPage();

            // === Bottom buttons ===
            _pnlButtons = new System.Windows.Forms.Panel();
            _btnSave = new System.Windows.Forms.Button();
            _btnCancel = new System.Windows.Forms.Button();

            // ========== TAB 1: Genel ==========
            _lblPlanName = new System.Windows.Forms.Label();
            _txtPlanName = new System.Windows.Forms.TextBox();
            _chkEnabled = new System.Windows.Forms.CheckBox();
            _lblLocalPath = new System.Windows.Forms.Label();
            _txtLocalPath = new System.Windows.Forms.TextBox();
            _btnBrowseLocal = new System.Windows.Forms.Button();

            // ========== TAB 2: SQL ==========
            _lblServer = new System.Windows.Forms.Label();
            _txtServer = new System.Windows.Forms.TextBox();
            _lblAuthMode = new System.Windows.Forms.Label();
            _cmbAuthMode = new System.Windows.Forms.ComboBox();
            _lblSqlUser = new System.Windows.Forms.Label();
            _txtSqlUser = new System.Windows.Forms.TextBox();
            _lblSqlPassword = new System.Windows.Forms.Label();
            _txtSqlPassword = new System.Windows.Forms.TextBox();
            _lblTimeout = new System.Windows.Forms.Label();
            _nudTimeout = new System.Windows.Forms.NumericUpDown();
            _btnTestSql = new System.Windows.Forms.Button();
            _grpDatabases = new System.Windows.Forms.GroupBox();
            _clbDatabases = new System.Windows.Forms.CheckedListBox();

            // ========== TAB 3: Strateji ==========
            _lblStrategy = new System.Windows.Forms.Label();
            _cmbStrategy = new System.Windows.Forms.ComboBox();
            _lblFullCron = new System.Windows.Forms.Label();
            _txtFullCron = new System.Windows.Forms.TextBox();
            _lblDiffCron = new System.Windows.Forms.Label();
            _txtDiffCron = new System.Windows.Forms.TextBox();
            _lblIncrCron = new System.Windows.Forms.Label();
            _txtIncrCron = new System.Windows.Forms.TextBox();
            _lblAutoPromote = new System.Windows.Forms.Label();
            _nudAutoPromote = new System.Windows.Forms.NumericUpDown();
            _chkVerify = new System.Windows.Forms.CheckBox();

            // ========== TAB 4: Sıkıştırma ==========
            _lblAlgorithm = new System.Windows.Forms.Label();
            _cmbAlgorithm = new System.Windows.Forms.ComboBox();
            _lblLevel = new System.Windows.Forms.Label();
            _cmbLevel = new System.Windows.Forms.ComboBox();
            _lblArchivePassword = new System.Windows.Forms.Label();
            _txtArchivePassword = new System.Windows.Forms.TextBox();

            // ========== TAB 5: Bulut ==========
            _lvCloudTargets = new System.Windows.Forms.ListView();
            _colCtName = new System.Windows.Forms.ColumnHeader();
            _colCtType = new System.Windows.Forms.ColumnHeader();
            _colCtStatus = new System.Windows.Forms.ColumnHeader();
            _btnAddCloud = new System.Windows.Forms.Button();
            _btnEditCloud = new System.Windows.Forms.Button();
            _btnRemoveCloud = new System.Windows.Forms.Button();

            // ========== TAB 6: Retention ==========
            _lblRetention = new System.Windows.Forms.Label();
            _cmbRetention = new System.Windows.Forms.ComboBox();
            _lblKeepLastN = new System.Windows.Forms.Label();
            _nudKeepLastN = new System.Windows.Forms.NumericUpDown();
            _lblDeleteDays = new System.Windows.Forms.Label();
            _nudDeleteDays = new System.Windows.Forms.NumericUpDown();

            // ========== TAB 7: Bildirim ==========
            _chkEmailEnabled = new System.Windows.Forms.CheckBox();
            _pnlSmtp = new System.Windows.Forms.Panel();
            _lblEmailTo = new System.Windows.Forms.Label();
            _txtEmailTo = new System.Windows.Forms.TextBox();
            _lblSmtpServer = new System.Windows.Forms.Label();
            _txtSmtpServer = new System.Windows.Forms.TextBox();
            _lblSmtpPort = new System.Windows.Forms.Label();
            _nudSmtpPort = new System.Windows.Forms.NumericUpDown();
            _chkSmtpSsl = new System.Windows.Forms.CheckBox();
            _lblSmtpUser = new System.Windows.Forms.Label();
            _txtSmtpUser = new System.Windows.Forms.TextBox();
            _lblSmtpPassword = new System.Windows.Forms.Label();
            _txtSmtpPassword = new System.Windows.Forms.TextBox();
            _chkNotifySuccess = new System.Windows.Forms.CheckBox();
            _chkNotifyFailure = new System.Windows.Forms.CheckBox();
            _chkToast = new System.Windows.Forms.CheckBox();

            // ========== TAB 8: Dosya Yedekleme ==========
            _chkFileBackupEnabled = new System.Windows.Forms.CheckBox();
            _pnlFileBackup = new System.Windows.Forms.Panel();
            _lblFileSchedule = new System.Windows.Forms.Label();
            _txtFileSchedule = new System.Windows.Forms.TextBox();
            _lvFileSources = new System.Windows.Forms.ListView();
            _colFsName = new System.Windows.Forms.ColumnHeader();
            _colFsPath = new System.Windows.Forms.ColumnHeader();
            _colFsVss = new System.Windows.Forms.ColumnHeader();
            _colFsStatus = new System.Windows.Forms.ColumnHeader();
            _btnAddFileSource = new System.Windows.Forms.Button();
            _btnEditFileSource = new System.Windows.Forms.Button();
            _btnRemoveFileSource = new System.Windows.Forms.Button();

            SuspendLayout();

            int y; // reusable y-position tracker
            int lx = 15, tx = 150, tw = 300; // label x, textbox x, textbox width

            // ========== TAB 1: Genel ==========
            y = 20;
            ConfigLabel(_lblPlanName, "Plan Adı:", lx, y, _tabGeneral);
            ConfigTextBox(_txtPlanName, tx, y, tw, _tabGeneral);
            y += 35;
            _chkEnabled.Text = "Plan Aktif";
            _chkEnabled.Location = new System.Drawing.Point(tx, y);
            _chkEnabled.AutoSize = true;
            _tabGeneral.Controls.Add(_chkEnabled);
            y += 35;
            ConfigLabel(_lblLocalPath, "Yerel Yedek Yolu:", lx, y, _tabGeneral);
            ConfigTextBox(_txtLocalPath, tx, y, tw - 35, _tabGeneral);
            _btnBrowseLocal.Text = "...";
            _btnBrowseLocal.Size = new System.Drawing.Size(30, 23);
            _btnBrowseLocal.Location = new System.Drawing.Point(tx + tw - 30, y);
            _btnBrowseLocal.Click += OnBrowseLocalPathClick;
            _tabGeneral.Controls.Add(_btnBrowseLocal);

            // ========== TAB 2: SQL ==========
            y = 20;
            ConfigLabel(_lblServer, "Sunucu:", lx, y, _tabSql);
            ConfigTextBox(_txtServer, tx, y, tw, _tabSql);
            y += 35;
            ConfigLabel(_lblAuthMode, "Kimlik Doğrulama:", lx, y, _tabSql);
            _cmbAuthMode.Location = new System.Drawing.Point(tx, y);
            _cmbAuthMode.Size = new System.Drawing.Size(tw, 23);
            _cmbAuthMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbAuthMode.SelectedIndexChanged += OnAuthModeChanged;
            _tabSql.Controls.Add(_cmbAuthMode);
            y += 35;
            ConfigLabel(_lblSqlUser, "Kullanıcı Adı:", lx, y, _tabSql);
            ConfigTextBox(_txtSqlUser, tx, y, tw, _tabSql);
            y += 35;
            ConfigLabel(_lblSqlPassword, "Şifre:", lx, y, _tabSql);
            _txtSqlPassword.Location = new System.Drawing.Point(tx, y);
            _txtSqlPassword.Size = new System.Drawing.Size(tw, 23);
            _txtSqlPassword.UseSystemPasswordChar = true;
            _tabSql.Controls.Add(_txtSqlPassword);
            y += 35;
            ConfigLabel(_lblTimeout, "Zaman Aşımı (sn):", lx, y, _tabSql);
            _nudTimeout.Location = new System.Drawing.Point(tx, y);
            _nudTimeout.Size = new System.Drawing.Size(80, 23);
            _nudTimeout.Minimum = 5;
            _nudTimeout.Maximum = 300;
            _nudTimeout.Value = 30;
            _tabSql.Controls.Add(_nudTimeout);
            y += 35;
            _btnTestSql.Text = "\U0001f50c Bağlantıyı Test Et";
            _btnTestSql.Location = new System.Drawing.Point(tx, y);
            _btnTestSql.AutoSize = true;
            _btnTestSql.Click += OnTestSqlConnectionClick;
            _tabSql.Controls.Add(_btnTestSql);
            y += 40;
            _grpDatabases.Text = "Veritabanları (bağlantı testinden sonra listelenir)";
            _grpDatabases.Location = new System.Drawing.Point(lx, y);
            _grpDatabases.Size = new System.Drawing.Size(tw + 145, 180);
            _clbDatabases.Location = new System.Drawing.Point(6, 18);
            _clbDatabases.Size = new System.Drawing.Size(tw + 130, 155);
            _clbDatabases.CheckOnClick = true;
            _grpDatabases.Controls.Add(_clbDatabases);
            _tabSql.Controls.Add(_grpDatabases);

            // ========== TAB 3: Strateji ==========
            y = 20;
            ConfigLabel(_lblStrategy, "Strateji:", lx, y, _tabStrategy);
            _cmbStrategy.Location = new System.Drawing.Point(tx, y);
            _cmbStrategy.Size = new System.Drawing.Size(tw, 23);
            _cmbStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbStrategy.SelectedIndexChanged += OnStrategyChanged;
            _tabStrategy.Controls.Add(_cmbStrategy);
            y += 35;
            ConfigLabel(_lblFullCron, "Tam Yedek Cron:", lx, y, _tabStrategy);
            ConfigTextBox(_txtFullCron, tx, y, tw, _tabStrategy);
            y += 35;
            ConfigLabel(_lblDiffCron, "Fark Yedek Cron:", lx, y, _tabStrategy);
            ConfigTextBox(_txtDiffCron, tx, y, tw, _tabStrategy);
            y += 35;
            ConfigLabel(_lblIncrCron, "Artırımlı Cron:", lx, y, _tabStrategy);
            ConfigTextBox(_txtIncrCron, tx, y, tw, _tabStrategy);
            y += 35;
            ConfigLabel(_lblAutoPromote, "Otomatik Full Sonrası:", lx, y, _tabStrategy);
            _nudAutoPromote.Location = new System.Drawing.Point(tx, y);
            _nudAutoPromote.Size = new System.Drawing.Size(80, 23);
            _nudAutoPromote.Minimum = 1;
            _nudAutoPromote.Maximum = 100;
            _nudAutoPromote.Value = 7;
            _tabStrategy.Controls.Add(_nudAutoPromote);
            y += 35;
            _chkVerify.Text = "Yedek sonrası RESTORE VERIFYONLY doğrulama";
            _chkVerify.Location = new System.Drawing.Point(tx, y);
            _chkVerify.AutoSize = true;
            _tabStrategy.Controls.Add(_chkVerify);

            // ========== TAB 4: Sıkıştırma ==========
            y = 20;
            ConfigLabel(_lblAlgorithm, "Algoritma:", lx, y, _tabCompression);
            _cmbAlgorithm.Location = new System.Drawing.Point(tx, y);
            _cmbAlgorithm.Size = new System.Drawing.Size(tw, 23);
            _cmbAlgorithm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _tabCompression.Controls.Add(_cmbAlgorithm);
            y += 35;
            ConfigLabel(_lblLevel, "Seviye:", lx, y, _tabCompression);
            _cmbLevel.Location = new System.Drawing.Point(tx, y);
            _cmbLevel.Size = new System.Drawing.Size(tw, 23);
            _cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _tabCompression.Controls.Add(_cmbLevel);
            y += 35;
            ConfigLabel(_lblArchivePassword, "Arşiv Şifresi:", lx, y, _tabCompression);
            _txtArchivePassword.Location = new System.Drawing.Point(tx, y);
            _txtArchivePassword.Size = new System.Drawing.Size(tw, 23);
            _txtArchivePassword.UseSystemPasswordChar = true;
            _tabCompression.Controls.Add(_txtArchivePassword);

            // ========== TAB 5: Bulut ==========
            _lvCloudTargets.Location = new System.Drawing.Point(lx, 15);
            _lvCloudTargets.Size = new System.Drawing.Size(380, 280);
            _lvCloudTargets.View = System.Windows.Forms.View.Details;
            _lvCloudTargets.FullRowSelect = true;
            _lvCloudTargets.GridLines = true;
            _colCtName.Text = "Hedef Adı";
            _colCtName.Width = 160;
            _colCtType.Text = "Tür";
            _colCtType.Width = 120;
            _colCtStatus.Text = "Durum";
            _colCtStatus.Width = 70;
            _lvCloudTargets.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { _colCtName, _colCtType, _colCtStatus });
            _tabCloud.Controls.Add(_lvCloudTargets);

            int bx = 405;
            _btnAddCloud.Text = "➕ Ekle";
            _btnAddCloud.Location = new System.Drawing.Point(bx, 15);
            _btnAddCloud.Size = new System.Drawing.Size(85, 28);
            _btnAddCloud.Click += OnAddCloudTarget;
            _tabCloud.Controls.Add(_btnAddCloud);

            _btnEditCloud.Text = "✏️ Düzenle";
            _btnEditCloud.Location = new System.Drawing.Point(bx, 50);
            _btnEditCloud.Size = new System.Drawing.Size(85, 28);
            _btnEditCloud.Click += OnEditCloudTarget;
            _tabCloud.Controls.Add(_btnEditCloud);

            _btnRemoveCloud.Text = "🗑️ Kaldır";
            _btnRemoveCloud.Location = new System.Drawing.Point(bx, 85);
            _btnRemoveCloud.Size = new System.Drawing.Size(85, 28);
            _btnRemoveCloud.Click += OnRemoveCloudTarget;
            _tabCloud.Controls.Add(_btnRemoveCloud);

            // ========== TAB 6: Retention ==========
            y = 20;
            ConfigLabel(_lblRetention, "Politika:", lx, y, _tabRetention);
            _cmbRetention.Location = new System.Drawing.Point(tx, y);
            _cmbRetention.Size = new System.Drawing.Size(tw, 23);
            _cmbRetention.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbRetention.SelectedIndexChanged += OnRetentionChanged;
            _tabRetention.Controls.Add(_cmbRetention);
            y += 35;
            ConfigLabel(_lblKeepLastN, "Son N Adet:", lx, y, _tabRetention);
            _nudKeepLastN.Location = new System.Drawing.Point(tx, y);
            _nudKeepLastN.Size = new System.Drawing.Size(80, 23);
            _nudKeepLastN.Minimum = 1;
            _nudKeepLastN.Maximum = 999;
            _nudKeepLastN.Value = 30;
            _tabRetention.Controls.Add(_nudKeepLastN);
            y += 35;
            ConfigLabel(_lblDeleteDays, "Gün Sonra Sil:", lx, y, _tabRetention);
            _nudDeleteDays.Location = new System.Drawing.Point(tx, y);
            _nudDeleteDays.Size = new System.Drawing.Size(80, 23);
            _nudDeleteDays.Minimum = 1;
            _nudDeleteDays.Maximum = 3650;
            _nudDeleteDays.Value = 90;
            _tabRetention.Controls.Add(_nudDeleteDays);

            // ========== TAB 7: Bildirim ==========
            _chkEmailEnabled.Text = "E-posta Bildirimi Aktif";
            _chkEmailEnabled.Location = new System.Drawing.Point(lx, 15);
            _chkEmailEnabled.AutoSize = true;
            _chkEmailEnabled.CheckedChanged += OnEmailEnabledChanged;
            _tabNotification.Controls.Add(_chkEmailEnabled);

            _pnlSmtp.Location = new System.Drawing.Point(lx, 40);
            _pnlSmtp.Size = new System.Drawing.Size(480, 210);
            y = 5;
            ConfigLabel(_lblEmailTo, "Alıcı E-posta:", 0, y, _pnlSmtp);
            ConfigTextBox(_txtEmailTo, 135, y, 300, _pnlSmtp);
            y += 30;
            ConfigLabel(_lblSmtpServer, "SMTP Sunucu:", 0, y, _pnlSmtp);
            ConfigTextBox(_txtSmtpServer, 135, y, 200, _pnlSmtp);
            y += 30;
            ConfigLabel(_lblSmtpPort, "Port:", 0, y, _pnlSmtp);
            _nudSmtpPort.Location = new System.Drawing.Point(135, y);
            _nudSmtpPort.Size = new System.Drawing.Size(80, 23);
            _nudSmtpPort.Minimum = 1;
            _nudSmtpPort.Maximum = 65535;
            _nudSmtpPort.Value = 587;
            _pnlSmtp.Controls.Add(_nudSmtpPort);
            _chkSmtpSsl.Text = "SSL/TLS";
            _chkSmtpSsl.Location = new System.Drawing.Point(225, y);
            _chkSmtpSsl.AutoSize = true;
            _pnlSmtp.Controls.Add(_chkSmtpSsl);
            y += 30;
            ConfigLabel(_lblSmtpUser, "Kullanıcı:", 0, y, _pnlSmtp);
            ConfigTextBox(_txtSmtpUser, 135, y, 200, _pnlSmtp);
            y += 30;
            ConfigLabel(_lblSmtpPassword, "Şifre:", 0, y, _pnlSmtp);
            _txtSmtpPassword.Location = new System.Drawing.Point(135, y);
            _txtSmtpPassword.Size = new System.Drawing.Size(200, 23);
            _txtSmtpPassword.UseSystemPasswordChar = true;
            _pnlSmtp.Controls.Add(_txtSmtpPassword);
            y += 30;
            _chkNotifySuccess.Text = "Başarılı yedekte bildir";
            _chkNotifySuccess.Location = new System.Drawing.Point(0, y);
            _chkNotifySuccess.AutoSize = true;
            _pnlSmtp.Controls.Add(_chkNotifySuccess);
            _chkNotifyFailure.Text = "Başarısız yedekte bildir";
            _chkNotifyFailure.Location = new System.Drawing.Point(200, y);
            _chkNotifyFailure.AutoSize = true;
            _pnlSmtp.Controls.Add(_chkNotifyFailure);
            _tabNotification.Controls.Add(_pnlSmtp);

            _chkToast.Text = "Windows Toast Bildirimi";
            _chkToast.Location = new System.Drawing.Point(lx, 260);
            _chkToast.AutoSize = true;
            _tabNotification.Controls.Add(_chkToast);

            // ========== TAB 8: Dosya Yedekleme ==========
            _chkFileBackupEnabled.Text = "Dosya Yedekleme Aktif";
            _chkFileBackupEnabled.Location = new System.Drawing.Point(lx, 15);
            _chkFileBackupEnabled.AutoSize = true;
            _chkFileBackupEnabled.CheckedChanged += OnFileBackupEnabledChanged;
            _tabFileBackup.Controls.Add(_chkFileBackupEnabled);

            _pnlFileBackup.Location = new System.Drawing.Point(lx, 40);
            _pnlFileBackup.Size = new System.Drawing.Size(500, 260);

            ConfigLabel(_lblFileSchedule, "Cron Zamanlama:", 0, 5, _pnlFileBackup);
            ConfigTextBox(_txtFileSchedule, 135, 5, 250, _pnlFileBackup);

            _lvFileSources.Location = new System.Drawing.Point(0, 38);
            _lvFileSources.Size = new System.Drawing.Size(400, 210);
            _lvFileSources.View = System.Windows.Forms.View.Details;
            _lvFileSources.FullRowSelect = true;
            _lvFileSources.GridLines = true;
            _colFsName.Text = "Kaynak Adı";
            _colFsName.Width = 100;
            _colFsPath.Text = "Yol";
            _colFsPath.Width = 160;
            _colFsVss.Text = "VSS";
            _colFsVss.Width = 50;
            _colFsStatus.Text = "Durum";
            _colFsStatus.Width = 60;
            _lvFileSources.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { _colFsName, _colFsPath, _colFsVss, _colFsStatus });
            _pnlFileBackup.Controls.Add(_lvFileSources);

            _btnAddFileSource.Text = "➕ Ekle";
            _btnAddFileSource.Location = new System.Drawing.Point(410, 38);
            _btnAddFileSource.Size = new System.Drawing.Size(85, 28);
            _btnAddFileSource.Click += OnAddFileSource;
            _pnlFileBackup.Controls.Add(_btnAddFileSource);

            _btnEditFileSource.Text = "✏️ Düzenle";
            _btnEditFileSource.Location = new System.Drawing.Point(410, 73);
            _btnEditFileSource.Size = new System.Drawing.Size(85, 28);
            _btnEditFileSource.Click += OnEditFileSource;
            _pnlFileBackup.Controls.Add(_btnEditFileSource);

            _btnRemoveFileSource.Text = "🗑️ Kaldır";
            _btnRemoveFileSource.Location = new System.Drawing.Point(410, 108);
            _btnRemoveFileSource.Size = new System.Drawing.Size(85, 28);
            _btnRemoveFileSource.Click += OnRemoveFileSource;
            _pnlFileBackup.Controls.Add(_btnRemoveFileSource);

            _tabFileBackup.Controls.Add(_pnlFileBackup);

            // ========== TabControl ==========
            _tabGeneral.Text = "Genel";
            _tabSql.Text = "SQL Bağlantı";
            _tabStrategy.Text = "Strateji";
            _tabCompression.Text = "Sıkıştırma";
            _tabCloud.Text = "Bulut Hedefler";
            _tabRetention.Text = "Saklama";
            _tabNotification.Text = "Bildirim";
            _tabFileBackup.Text = "Dosya Yedekleme";

            _tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            _tabSql.Padding = new System.Windows.Forms.Padding(3);
            _tabStrategy.Padding = new System.Windows.Forms.Padding(3);
            _tabCompression.Padding = new System.Windows.Forms.Padding(3);
            _tabCloud.Padding = new System.Windows.Forms.Padding(3);
            _tabRetention.Padding = new System.Windows.Forms.Padding(3);
            _tabNotification.Padding = new System.Windows.Forms.Padding(3);
            _tabFileBackup.Padding = new System.Windows.Forms.Padding(3);

            _tabControl.TabPages.AddRange(new System.Windows.Forms.TabPage[] {
                _tabGeneral, _tabSql, _tabStrategy, _tabCompression,
                _tabCloud, _tabRetention, _tabNotification, _tabFileBackup
            });
            _tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _tabControl.Name = "_tabControl";
            _tabControl.Padding = new System.Drawing.Point(14, 6);
            _tabControl.Font = Theme.ModernTheme.FontBody;

            // ========== Bottom buttons ==========
            _pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            _pnlButtons.Height = 50;
            _pnlButtons.Padding = new System.Windows.Forms.Padding(0, 10, 16, 6);
            _pnlButtons.BackColor = Theme.ModernTheme.SurfaceColor;

            _btnSave.Text = "💾 Kaydet";
            _btnSave.Size = new System.Drawing.Size(90, 30);
            _btnSave.Dock = System.Windows.Forms.DockStyle.Right;
            _btnSave.Click += OnSaveClick;

            _btnCancel.Text = "İptal";
            _btnCancel.Size = new System.Drawing.Size(80, 30);
            _btnCancel.Dock = System.Windows.Forms.DockStyle.Right;
            _btnCancel.Click += OnCancelClick;

            _pnlButtons.Controls.Add(_btnSave);
            _pnlButtons.Controls.Add(_btnCancel);

            // ========== Form ==========
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            ClientSize = new System.Drawing.Size(560, 500);
            Controls.Add(_tabControl);
            Controls.Add(_pnlButtons);
            Font = Theme.ModernTheme.FontBody;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PlanEditForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Yedekleme Planı";

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

        // Tab control
        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _tabGeneral;
        private System.Windows.Forms.TabPage _tabSql;
        private System.Windows.Forms.TabPage _tabStrategy;
        private System.Windows.Forms.TabPage _tabCompression;
        private System.Windows.Forms.TabPage _tabCloud;
        private System.Windows.Forms.TabPage _tabRetention;
        private System.Windows.Forms.TabPage _tabNotification;
        private System.Windows.Forms.TabPage _tabFileBackup;

        // Bottom
        private System.Windows.Forms.Panel _pnlButtons;
        private System.Windows.Forms.Button _btnSave;
        private System.Windows.Forms.Button _btnCancel;

        // Tab 1: Genel
        private System.Windows.Forms.Label _lblPlanName;
        private System.Windows.Forms.TextBox _txtPlanName;
        private System.Windows.Forms.CheckBox _chkEnabled;
        private System.Windows.Forms.Label _lblLocalPath;
        private System.Windows.Forms.TextBox _txtLocalPath;
        private System.Windows.Forms.Button _btnBrowseLocal;

        // Tab 2: SQL
        private System.Windows.Forms.Label _lblServer;
        private System.Windows.Forms.TextBox _txtServer;
        private System.Windows.Forms.Label _lblAuthMode;
        private System.Windows.Forms.ComboBox _cmbAuthMode;
        private System.Windows.Forms.Label _lblSqlUser;
        private System.Windows.Forms.TextBox _txtSqlUser;
        private System.Windows.Forms.Label _lblSqlPassword;
        private System.Windows.Forms.TextBox _txtSqlPassword;
        private System.Windows.Forms.Label _lblTimeout;
        private System.Windows.Forms.NumericUpDown _nudTimeout;
        private System.Windows.Forms.Button _btnTestSql;
        private System.Windows.Forms.GroupBox _grpDatabases;
        private System.Windows.Forms.CheckedListBox _clbDatabases;

        // Tab 3: Strateji
        private System.Windows.Forms.Label _lblStrategy;
        private System.Windows.Forms.ComboBox _cmbStrategy;
        private System.Windows.Forms.Label _lblFullCron;
        private System.Windows.Forms.TextBox _txtFullCron;
        private System.Windows.Forms.Label _lblDiffCron;
        private System.Windows.Forms.TextBox _txtDiffCron;
        private System.Windows.Forms.Label _lblIncrCron;
        private System.Windows.Forms.TextBox _txtIncrCron;
        private System.Windows.Forms.Label _lblAutoPromote;
        private System.Windows.Forms.NumericUpDown _nudAutoPromote;
        private System.Windows.Forms.CheckBox _chkVerify;

        // Tab 4: Sıkıştırma
        private System.Windows.Forms.Label _lblAlgorithm;
        private System.Windows.Forms.ComboBox _cmbAlgorithm;
        private System.Windows.Forms.Label _lblLevel;
        private System.Windows.Forms.ComboBox _cmbLevel;
        private System.Windows.Forms.Label _lblArchivePassword;
        private System.Windows.Forms.TextBox _txtArchivePassword;

        // Tab 5: Bulut
        private System.Windows.Forms.ListView _lvCloudTargets;
        private System.Windows.Forms.ColumnHeader _colCtName;
        private System.Windows.Forms.ColumnHeader _colCtType;
        private System.Windows.Forms.ColumnHeader _colCtStatus;
        private System.Windows.Forms.Button _btnAddCloud;
        private System.Windows.Forms.Button _btnEditCloud;
        private System.Windows.Forms.Button _btnRemoveCloud;

        // Tab 6: Retention
        private System.Windows.Forms.Label _lblRetention;
        private System.Windows.Forms.ComboBox _cmbRetention;
        private System.Windows.Forms.Label _lblKeepLastN;
        private System.Windows.Forms.NumericUpDown _nudKeepLastN;
        private System.Windows.Forms.Label _lblDeleteDays;
        private System.Windows.Forms.NumericUpDown _nudDeleteDays;

        // Tab 7: Bildirim
        private System.Windows.Forms.CheckBox _chkEmailEnabled;
        private System.Windows.Forms.Panel _pnlSmtp;
        private System.Windows.Forms.Label _lblEmailTo;
        private System.Windows.Forms.TextBox _txtEmailTo;
        private System.Windows.Forms.Label _lblSmtpServer;
        private System.Windows.Forms.TextBox _txtSmtpServer;
        private System.Windows.Forms.Label _lblSmtpPort;
        private System.Windows.Forms.NumericUpDown _nudSmtpPort;
        private System.Windows.Forms.CheckBox _chkSmtpSsl;
        private System.Windows.Forms.Label _lblSmtpUser;
        private System.Windows.Forms.TextBox _txtSmtpUser;
        private System.Windows.Forms.Label _lblSmtpPassword;
        private System.Windows.Forms.TextBox _txtSmtpPassword;
        private System.Windows.Forms.CheckBox _chkNotifySuccess;
        private System.Windows.Forms.CheckBox _chkNotifyFailure;
        private System.Windows.Forms.CheckBox _chkToast;

        // Tab 8: Dosya Yedekleme
        private System.Windows.Forms.CheckBox _chkFileBackupEnabled;
        private System.Windows.Forms.Panel _pnlFileBackup;
        private System.Windows.Forms.Label _lblFileSchedule;
        private System.Windows.Forms.TextBox _txtFileSchedule;
        private System.Windows.Forms.ListView _lvFileSources;
        private System.Windows.Forms.ColumnHeader _colFsName;
        private System.Windows.Forms.ColumnHeader _colFsPath;
        private System.Windows.Forms.ColumnHeader _colFsVss;
        private System.Windows.Forms.ColumnHeader _colFsStatus;
        private System.Windows.Forms.Button _btnAddFileSource;
        private System.Windows.Forms.Button _btnEditFileSource;
        private System.Windows.Forms.Button _btnRemoveFileSource;
    }
}
