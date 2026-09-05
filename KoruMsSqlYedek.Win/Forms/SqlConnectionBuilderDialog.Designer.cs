namespace KoruMsSqlYedek.Win.Forms
{
    partial class SqlConnectionBuilderDialog
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
            _tlpMain = new System.Windows.Forms.TableLayoutPanel();
            _lblServer = new System.Windows.Forms.Label();
            _pnlServer = new System.Windows.Forms.Panel();
            _cmbServer = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _btnBrowseServers = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _lblAuth = new System.Windows.Forms.Label();
            _cmbAuthMode = new KoruMsSqlYedek.Win.Theme.ModernComboBox();
            _pnlCredentials = new System.Windows.Forms.Panel();
            _lblUsername = new System.Windows.Forms.Label();
            _txtUsername = new KoruMsSqlYedek.Win.Theme.ModernTextBox();
            _lblPassword = new System.Windows.Forms.Label();
            _txtPassword = new KoruMsSqlYedek.Win.Theme.ModernTextBox();
            _pnlAdvanced = new System.Windows.Forms.Panel();
            _chkTrustCert = new System.Windows.Forms.CheckBox();
            _lblTimeout = new System.Windows.Forms.Label();
            _nudTimeout = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _lblTimeoutSuffix = new System.Windows.Forms.Label();
            _lblStatus = new System.Windows.Forms.Label();
            _grpPreview = new System.Windows.Forms.GroupBox();
            _txtPreview = new System.Windows.Forms.TextBox();
            _pnlButtons = new System.Windows.Forms.Panel();
            _btnTestConn = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnOk = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnCancel = new KoruMsSqlYedek.Win.Theme.ModernButton();

            _tlpMain.SuspendLayout();
            _pnlServer.SuspendLayout();
            _pnlCredentials.SuspendLayout();
            _pnlAdvanced.SuspendLayout();
            _grpPreview.SuspendLayout();
            _pnlButtons.SuspendLayout();
            SuspendLayout();

            // _tlpMain
            _tlpMain.Location = new System.Drawing.Point(12, 12);
            _tlpMain.Name = "_tlpMain";
            _tlpMain.Size = new System.Drawing.Size(500, 260);
            _tlpMain.ColumnCount = 2;
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpMain.RowCount = 5;
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));

            // _lblServer
            _lblServer.Text = "SQL Server:";
            _lblServer.Name = "_lblServer";
            _lblServer.AutoSize = true;
            _lblServer.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _tlpMain.Controls.Add(_lblServer, 0, 0);

            // _pnlServer
            _pnlServer.Name = "_pnlServer";
            _pnlServer.Dock = System.Windows.Forms.DockStyle.Fill;

            // _cmbServer
            _cmbServer.Name = "_cmbServer";
            _cmbServer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            _cmbServer.Location = new System.Drawing.Point(0, 7);
            _cmbServer.Size = new System.Drawing.Size(268, 26);
            _cmbServer.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top;
            _cmbServer.TextChanged += OnServerTextChanged;
            _pnlServer.Controls.Add(_cmbServer);

            // _btnBrowseServers
            _btnBrowseServers.Text = "Tara";
            _btnBrowseServers.Name = "_btnBrowseServers";
            _btnBrowseServers.ButtonStyle = KoruMsSqlYedek.Win.Theme.ModernButtonStyle.Secondary;
            _btnBrowseServers.IconSymbol = "\uE721";
            _btnBrowseServers.Size = new System.Drawing.Size(78, 26);
            _btnBrowseServers.Location = new System.Drawing.Point(272, 7);
            _btnBrowseServers.Click += OnBrowseServersClick;
            _pnlServer.Controls.Add(_btnBrowseServers);

            _tlpMain.Controls.Add(_pnlServer, 1, 0);

            // _lblAuth
            _lblAuth.Text = "Kimlik Do\u011frulama:";
            _lblAuth.Name = "_lblAuth";
            _lblAuth.AutoSize = true;
            _lblAuth.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _tlpMain.Controls.Add(_lblAuth, 0, 1);

            // _cmbAuthMode
            _cmbAuthMode.Name = "_cmbAuthMode";
            _cmbAuthMode.Location = new System.Drawing.Point(0, 7);
            _cmbAuthMode.Size = new System.Drawing.Size(350, 26);
            _cmbAuthMode.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top;
            _cmbAuthMode.SelectedIndexChanged += OnAuthModeChanged;
            _tlpMain.Controls.Add(_cmbAuthMode, 1, 1);

            // _pnlCredentials (satır 2 — SQL Auth ise görünür)
            _pnlCredentials.Name = "_pnlCredentials";
            _pnlCredentials.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlCredentials.Visible = false;
            _pnlCredentials.AutoSize = true;
            _pnlCredentials.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;

            // _lblUsername
            _lblUsername.Text = "Kullan\u0131c\u0131 Ad\u0131:";
            _lblUsername.Name = "_lblUsername";
            _lblUsername.AutoSize = true;
            _lblUsername.Location = new System.Drawing.Point(0, 7);
            _pnlCredentials.Controls.Add(_lblUsername);

            // _txtUsername
            _txtUsername.Name = "_txtUsername";
            _txtUsername.Location = new System.Drawing.Point(120, 4);
            _txtUsername.Size = new System.Drawing.Size(230, 26);
            _txtUsername.TextChanged += OnFieldChanged;
            _pnlCredentials.Controls.Add(_txtUsername);

            // _lblPassword
            _lblPassword.Text = "\u015eifre:";
            _lblPassword.Name = "_lblPassword";
            _lblPassword.AutoSize = true;
            _lblPassword.Location = new System.Drawing.Point(0, 42);
            _pnlCredentials.Controls.Add(_lblPassword);

            // _txtPassword
            _txtPassword.Name = "_txtPassword";
            _txtPassword.Location = new System.Drawing.Point(120, 38);
            _txtPassword.Size = new System.Drawing.Size(230, 26);
            _txtPassword.IsPassword = true;
            _txtPassword.TextChanged += OnFieldChanged;
            _pnlCredentials.Controls.Add(_txtPassword);

            _tlpMain.Controls.Add(_pnlCredentials, 1, 2);
            _tlpMain.SetColumnSpan(_pnlCredentials, 1);

            // _pnlAdvanced (satır 3)
            _pnlAdvanced.Name = "_pnlAdvanced";
            _pnlAdvanced.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlAdvanced.AutoSize = true;

            // _chkTrustCert
            // Baglanti her zaman sifrelidir; bu secenek yalnizca sertifika dogrulamasini atlar
            _chkTrustCert.Text = "Sertifika do\u011frulamas\u0131n\u0131 atla (ba\u011flant\u0131 her zaman \u015fifreli)";
            _chkTrustCert.Name = "_chkTrustCert";
            _chkTrustCert.AutoSize = true;
            _chkTrustCert.Checked = true;
            _chkTrustCert.Location = new System.Drawing.Point(0, 8);
            _chkTrustCert.CheckedChanged += OnFieldChanged;
            _pnlAdvanced.Controls.Add(_chkTrustCert);

            // _lblTimeout
            _lblTimeout.Text = "Ba\u011flant\u0131 zaman a\u015f\u0131m\u0131:";
            _lblTimeout.Name = "_lblTimeout";
            _lblTimeout.AutoSize = true;
            _lblTimeout.Location = new System.Drawing.Point(0, 40);
            _pnlAdvanced.Controls.Add(_lblTimeout);

            // _nudTimeout
            _nudTimeout.Name = "_nudTimeout";
            _nudTimeout.Location = new System.Drawing.Point(140, 37);
            _nudTimeout.Size = new System.Drawing.Size(70, 26);
            _nudTimeout.Minimum = 5;
            _nudTimeout.Maximum = 300;
            _nudTimeout.Value = 30;
            _nudTimeout.ValueChanged += OnFieldChanged;
            _pnlAdvanced.Controls.Add(_nudTimeout);

            // _lblTimeoutSuffix
            _lblTimeoutSuffix.Text = "saniye";
            _lblTimeoutSuffix.Name = "_lblTimeoutSuffix";
            _lblTimeoutSuffix.AutoSize = true;
            _lblTimeoutSuffix.Location = new System.Drawing.Point(218, 40);
            _pnlAdvanced.Controls.Add(_lblTimeoutSuffix);

            _tlpMain.Controls.Add(_pnlAdvanced, 1, 3);

            // _lblStatus (satır 4)
            _lblStatus.Text = "";
            _lblStatus.Name = "_lblStatus";
            _lblStatus.AutoSize = true;
            _lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _tlpMain.Controls.Add(_lblStatus, 0, 4);
            _tlpMain.SetColumnSpan(_lblStatus, 2);

            // _grpPreview
            _grpPreview.Text = "Olu\u015fturulan Ba\u011flant\u0131 Dizisi";
            _grpPreview.Name = "_grpPreview";
            _grpPreview.Location = new System.Drawing.Point(12, 280);
            _grpPreview.Size = new System.Drawing.Size(500, 52);

            // _txtPreview
            _txtPreview.Name = "_txtPreview";
            _txtPreview.ReadOnly = true;
            _txtPreview.TabStop = false;
            _txtPreview.Location = new System.Drawing.Point(8, 20);
            _txtPreview.Size = new System.Drawing.Size(484, 23);
            _txtPreview.Font = new System.Drawing.Font("Consolas", 8.5F);
            _txtPreview.Tag = "surface";
            _grpPreview.Controls.Add(_txtPreview);

            // _pnlButtons
            _pnlButtons.Name = "_pnlButtons";
            _pnlButtons.Location = new System.Drawing.Point(12, 344);
            _pnlButtons.Size = new System.Drawing.Size(500, 44);

            // _btnTestConn
            _btnTestConn.Text = "Ba\u011flant\u0131y\u0131 Test Et";
            _btnTestConn.Name = "_btnTestConn";
            _btnTestConn.ButtonStyle = KoruMsSqlYedek.Win.Theme.ModernButtonStyle.Secondary;
            _btnTestConn.IconSymbol = "\uE946";
            _btnTestConn.Size = new System.Drawing.Size(150, 34);
            _btnTestConn.Location = new System.Drawing.Point(0, 5);
            _btnTestConn.Click += OnTestConnectionClick;
            _pnlButtons.Controls.Add(_btnTestConn);

            // _btnOk
            _btnOk.Text = "Tamam";
            _btnOk.Name = "_btnOk";
            _btnOk.ButtonStyle = KoruMsSqlYedek.Win.Theme.ModernButtonStyle.Primary;
            _btnOk.Size = new System.Drawing.Size(110, 34);
            _btnOk.Location = new System.Drawing.Point(278, 5);
            _btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            _btnOk.Click += OnOkClick;
            _pnlButtons.Controls.Add(_btnOk);

            // _btnCancel
            _btnCancel.Text = "\u0130ptal";
            _btnCancel.Name = "_btnCancel";
            _btnCancel.ButtonStyle = KoruMsSqlYedek.Win.Theme.ModernButtonStyle.Secondary;
            _btnCancel.Size = new System.Drawing.Size(100, 34);
            _btnCancel.Location = new System.Drawing.Point(394, 5);
            _btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            _pnlButtons.Controls.Add(_btnCancel);

            // Form
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(524, 468);
            Controls.Add(_tlpMain);
            Controls.Add(_grpPreview);
            Controls.Add(_pnlButtons);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SqlConnectionBuilderDialog";
            Text = "SQL Server Ba\u011flant\u0131s\u0131 Yap\u0131land\u0131r";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            _tlpMain.ResumeLayout(false);
            _pnlServer.ResumeLayout(false);
            _pnlCredentials.ResumeLayout(false);
            _pnlAdvanced.ResumeLayout(false);
            _grpPreview.ResumeLayout(false);
            _pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _tlpMain;
        private System.Windows.Forms.Label _lblServer;
        private System.Windows.Forms.Panel _pnlServer;
        private KoruMsSqlYedek.Win.Theme.ModernComboBox _cmbServer;
        private KoruMsSqlYedek.Win.Theme.ModernButton _btnBrowseServers;
        private System.Windows.Forms.Label _lblAuth;
        private KoruMsSqlYedek.Win.Theme.ModernComboBox _cmbAuthMode;
        private System.Windows.Forms.Panel _pnlCredentials;
        private System.Windows.Forms.Label _lblUsername;
        private KoruMsSqlYedek.Win.Theme.ModernTextBox _txtUsername;
        private System.Windows.Forms.Label _lblPassword;
        private KoruMsSqlYedek.Win.Theme.ModernTextBox _txtPassword;
        private System.Windows.Forms.Panel _pnlAdvanced;
        private System.Windows.Forms.CheckBox _chkTrustCert;
        private System.Windows.Forms.Label _lblTimeout;
        private KoruMsSqlYedek.Win.Theme.ModernNumericUpDown _nudTimeout;
        private System.Windows.Forms.Label _lblTimeoutSuffix;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.GroupBox _grpPreview;
        private System.Windows.Forms.TextBox _txtPreview;
        private System.Windows.Forms.Panel _pnlButtons;
        private KoruMsSqlYedek.Win.Theme.ModernButton _btnTestConn;
        private KoruMsSqlYedek.Win.Theme.ModernButton _btnOk;
        private KoruMsSqlYedek.Win.Theme.ModernButton _btnCancel;
    }
}
