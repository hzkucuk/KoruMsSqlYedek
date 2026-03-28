namespace MikroSqlDbYedek.Win.Forms
{
    partial class SettingsForm
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

            _tabControl = new System.Windows.Forms.TabControl();
            _tabGeneral = new System.Windows.Forms.TabPage();
            _tabSmtp = new System.Windows.Forms.TabPage();

            _tlpGeneral = new System.Windows.Forms.TableLayoutPanel();
            _lblLanguage = new System.Windows.Forms.Label();
            _cmbLanguage = new System.Windows.Forms.ComboBox();
            _chkStartWithWindows = new System.Windows.Forms.CheckBox();
            _chkMinimizeToTray = new System.Windows.Forms.CheckBox();
            _lblDefaultBackupPath = new System.Windows.Forms.Label();
            _txtDefaultBackupPath = new System.Windows.Forms.TextBox();
            _btnBrowseBackupPath = new System.Windows.Forms.Button();
            _lblLogRetention = new System.Windows.Forms.Label();
            _nudLogRetention = new System.Windows.Forms.NumericUpDown();
            _lblLogRetentionSuffix = new System.Windows.Forms.Label();
            _lblHistoryRetention = new System.Windows.Forms.Label();
            _nudHistoryRetention = new System.Windows.Forms.NumericUpDown();
            _lblHistoryRetentionSuffix = new System.Windows.Forms.Label();

            _tlpSmtp = new System.Windows.Forms.TableLayoutPanel();
            _lblSmtpHost = new System.Windows.Forms.Label();
            _txtSmtpHost = new System.Windows.Forms.TextBox();
            _lblSmtpPort = new System.Windows.Forms.Label();
            _nudSmtpPort = new System.Windows.Forms.NumericUpDown();
            _chkSmtpSsl = new System.Windows.Forms.CheckBox();
            _pnlPortSsl = new System.Windows.Forms.FlowLayoutPanel();
            _lblSmtpUsername = new System.Windows.Forms.Label();
            _txtSmtpUsername = new System.Windows.Forms.TextBox();
            _lblSmtpPassword = new System.Windows.Forms.Label();
            _txtSmtpPassword = new System.Windows.Forms.TextBox();
            _lblSmtpSenderEmail = new System.Windows.Forms.Label();
            _txtSmtpSenderEmail = new System.Windows.Forms.TextBox();
            _lblSmtpSenderName = new System.Windows.Forms.Label();
            _txtSmtpSenderName = new System.Windows.Forms.TextBox();
            _lblSmtpRecipients = new System.Windows.Forms.Label();
            _txtSmtpRecipients = new System.Windows.Forms.TextBox();
            _btnSmtpTest = new System.Windows.Forms.Button();

            _flpButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnSave = new System.Windows.Forms.Button();
            _btnCancel = new System.Windows.Forms.Button();

            _tabControl.SuspendLayout();
            _tabGeneral.SuspendLayout();
            _tabSmtp.SuspendLayout();
            _tlpGeneral.SuspendLayout();
            _tlpSmtp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_nudLogRetention).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_nudHistoryRetention).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_nudSmtpPort).BeginInit();
            _flpButtons.SuspendLayout();
            SuspendLayout();

            // ===== TabControl =====
            _tabControl.Controls.Add(_tabGeneral);
            _tabControl.Controls.Add(_tabSmtp);
            _tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _tabControl.Location = new System.Drawing.Point(0, 0);
            _tabControl.Name = "_tabControl";
            _tabControl.Padding = new System.Drawing.Point(12, 6);
            _tabControl.SelectedIndex = 0;
            _tabControl.Font = Theme.ModernTheme.FontBody;

            // _tabGeneral
            _tabGeneral.Controls.Add(_tlpGeneral);
            _tabGeneral.Name = "_tabGeneral";
            _tabGeneral.Padding = new System.Windows.Forms.Padding(8);
            _tabGeneral.Text = "Genel";
            _tabGeneral.BackColor = Theme.ModernTheme.SurfaceColor;

            // _tabSmtp
            _tabSmtp.Controls.Add(_tlpSmtp);
            _tabSmtp.Name = "_tabSmtp";
            _tabSmtp.Padding = new System.Windows.Forms.Padding(8);
            _tabSmtp.Text = "E-posta (SMTP)";
            _tabSmtp.BackColor = Theme.ModernTheme.SurfaceColor;

            // ===== General TLP =====
            _tlpGeneral.ColumnCount = 3;
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpGeneral.Name = "_tlpGeneral";
            _tlpGeneral.RowCount = 7;
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpGeneral.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // Row 0 — Language
            _tlpGeneral.Controls.Add(_lblLanguage, 0, 0);
            _tlpGeneral.Controls.Add(_cmbLanguage, 1, 0);
            _lblLanguage.Text = "Dil:";
            _lblLanguage.AutoSize = true;
            _lblLanguage.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLanguage.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _cmbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLanguage.Items.AddRange(new object[] { "T\u00fcrk\u00e7e (tr-TR)", "English (en-US)" });
            _cmbLanguage.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _cmbLanguage.Width = 200;

            // Row 1 — Start with Windows
            _tlpGeneral.Controls.Add(_chkStartWithWindows, 1, 1);
            _chkStartWithWindows.Text = "Windows ile birlikte ba\u015flat";
            _chkStartWithWindows.AutoSize = true;
            _chkStartWithWindows.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Row 2 — Minimize to tray
            _tlpGeneral.Controls.Add(_chkMinimizeToTray, 1, 2);
            _chkMinimizeToTray.Text = "Simge durumuna k\u00fc\u00e7\u00fclt\u00fcld\u00fc\u011f\u00fcnde tepside gizle";
            _chkMinimizeToTray.AutoSize = true;
            _chkMinimizeToTray.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);

            // Row 3 — Default backup path
            _tlpGeneral.Controls.Add(_lblDefaultBackupPath, 0, 3);
            _tlpGeneral.Controls.Add(_txtDefaultBackupPath, 1, 3);
            _tlpGeneral.Controls.Add(_btnBrowseBackupPath, 2, 3);
            _lblDefaultBackupPath.Text = "Varsay\u0131lan yedek dizini:";
            _lblDefaultBackupPath.AutoSize = true;
            _lblDefaultBackupPath.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblDefaultBackupPath.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _txtDefaultBackupPath.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtDefaultBackupPath.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _btnBrowseBackupPath.Text = "...";
            _btnBrowseBackupPath.Size = new System.Drawing.Size(32, 23);
            _btnBrowseBackupPath.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            _btnBrowseBackupPath.Click += OnBrowseBackupPath;

            // Row 4 — Log retention
            _tlpGeneral.Controls.Add(_lblLogRetention, 0, 4);
            _tlpGeneral.Controls.Add(_nudLogRetention, 1, 4);
            _tlpGeneral.Controls.Add(_lblLogRetentionSuffix, 2, 4);
            _lblLogRetention.Text = "Log saklama s\u00fcresi:";
            _lblLogRetention.AutoSize = true;
            _lblLogRetention.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLogRetention.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _nudLogRetention.Minimum = 1;
            _nudLogRetention.Maximum = 365;
            _nudLogRetention.Value = 30;
            _nudLogRetention.Width = 80;
            _nudLogRetention.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _lblLogRetentionSuffix.Text = "g\u00fcn";
            _lblLogRetentionSuffix.AutoSize = true;
            _lblLogRetentionSuffix.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblLogRetentionSuffix.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);

            // Row 5 — History retention
            _tlpGeneral.Controls.Add(_lblHistoryRetention, 0, 5);
            _tlpGeneral.Controls.Add(_nudHistoryRetention, 1, 5);
            _tlpGeneral.Controls.Add(_lblHistoryRetentionSuffix, 2, 5);
            _lblHistoryRetention.Text = "Ge\u00e7mi\u015f saklama s\u00fcresi:";
            _lblHistoryRetention.AutoSize = true;
            _lblHistoryRetention.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblHistoryRetention.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _nudHistoryRetention.Minimum = 1;
            _nudHistoryRetention.Maximum = 365;
            _nudHistoryRetention.Value = 90;
            _nudHistoryRetention.Width = 80;
            _nudHistoryRetention.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _lblHistoryRetentionSuffix.Text = "g\u00fcn";
            _lblHistoryRetentionSuffix.AutoSize = true;
            _lblHistoryRetentionSuffix.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblHistoryRetentionSuffix.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);

            // ===== SMTP TLP =====
            _tlpSmtp.ColumnCount = 2;
            _tlpSmtp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpSmtp.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpSmtp.Name = "_tlpSmtp";
            _tlpSmtp.RowCount = 9;
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpSmtp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // Row 0 — SMTP Host
            _tlpSmtp.Controls.Add(_lblSmtpHost, 0, 0);
            _tlpSmtp.Controls.Add(_txtSmtpHost, 1, 0);
            _lblSmtpHost.Text = "SMTP Sunucu:";
            _lblSmtpHost.AutoSize = true;
            _lblSmtpHost.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblSmtpHost.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _txtSmtpHost.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSmtpHost.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Row 1 — Port + SSL
            _tlpSmtp.Controls.Add(_lblSmtpPort, 0, 1);
            _lblSmtpPort.Text = "Port:";
            _lblSmtpPort.AutoSize = true;
            _lblSmtpPort.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblSmtpPort.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _pnlPortSsl.AutoSize = true;
            _pnlPortSsl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _pnlPortSsl.WrapContents = false;
            _pnlPortSsl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            _pnlPortSsl.Controls.Add(_nudSmtpPort);
            _pnlPortSsl.Controls.Add(_chkSmtpSsl);
            _tlpSmtp.Controls.Add(_pnlPortSsl, 1, 1);
            _nudSmtpPort.Minimum = 1;
            _nudSmtpPort.Maximum = 65535;
            _nudSmtpPort.Value = 587;
            _nudSmtpPort.Width = 80;
            _nudSmtpPort.Margin = new System.Windows.Forms.Padding(0, 2, 12, 0);
            _chkSmtpSsl.Text = "SSL/TLS kullan";
            _chkSmtpSsl.AutoSize = true;
            _chkSmtpSsl.Checked = true;
            _chkSmtpSsl.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);

            // Row 2 — Username
            _tlpSmtp.Controls.Add(_lblSmtpUsername, 0, 2);
            _tlpSmtp.Controls.Add(_txtSmtpUsername, 1, 2);
            _lblSmtpUsername.Text = "Kullan\u0131c\u0131 Ad\u0131:";
            _lblSmtpUsername.AutoSize = true;
            _lblSmtpUsername.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblSmtpUsername.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _txtSmtpUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSmtpUsername.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Row 3 — Password
            _tlpSmtp.Controls.Add(_lblSmtpPassword, 0, 3);
            _tlpSmtp.Controls.Add(_txtSmtpPassword, 1, 3);
            _lblSmtpPassword.Text = "\u015Eifre:";
            _lblSmtpPassword.AutoSize = true;
            _lblSmtpPassword.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblSmtpPassword.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _txtSmtpPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSmtpPassword.UseSystemPasswordChar = true;
            _txtSmtpPassword.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Row 4 — Sender email
            _tlpSmtp.Controls.Add(_lblSmtpSenderEmail, 0, 4);
            _tlpSmtp.Controls.Add(_txtSmtpSenderEmail, 1, 4);
            _lblSmtpSenderEmail.Text = "G\u00f6nderici E-posta:";
            _lblSmtpSenderEmail.AutoSize = true;
            _lblSmtpSenderEmail.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblSmtpSenderEmail.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _txtSmtpSenderEmail.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSmtpSenderEmail.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Row 5 — Sender name
            _tlpSmtp.Controls.Add(_lblSmtpSenderName, 0, 5);
            _tlpSmtp.Controls.Add(_txtSmtpSenderName, 1, 5);
            _lblSmtpSenderName.Text = "G\u00f6nderici Ad\u0131:";
            _lblSmtpSenderName.AutoSize = true;
            _lblSmtpSenderName.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblSmtpSenderName.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _txtSmtpSenderName.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSmtpSenderName.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Row 6 — Recipients
            _tlpSmtp.Controls.Add(_lblSmtpRecipients, 0, 6);
            _tlpSmtp.Controls.Add(_txtSmtpRecipients, 1, 6);
            _lblSmtpRecipients.Text = "Al\u0131c\u0131lar:";
            _lblSmtpRecipients.AutoSize = true;
            _lblSmtpRecipients.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblSmtpRecipients.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _txtSmtpRecipients.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSmtpRecipients.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Row 7 — SMTP Test
            _tlpSmtp.Controls.Add(_btnSmtpTest, 1, 7);
            _btnSmtpTest.Text = "\u2709 Test E-postas\u0131 G\u00f6nder";
            _btnSmtpTest.AutoSize = true;
            _btnSmtpTest.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            _btnSmtpTest.Click += OnSmtpTestClick;

            // ===== Bottom Buttons =====
            _flpButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _flpButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            _flpButtons.AutoSize = true;
            _flpButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _flpButtons.Padding = new System.Windows.Forms.Padding(0, 4, 8, 8);
            _flpButtons.BackColor = Theme.ModernTheme.SurfaceColor;
            _flpButtons.Controls.Add(_btnCancel);
            _flpButtons.Controls.Add(_btnSave);

            _btnCancel.Text = "\u0130ptal";
            _btnCancel.Size = new System.Drawing.Size(90, 30);
            _btnCancel.Margin = new System.Windows.Forms.Padding(4);
            _btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            _btnCancel.Click += OnCancelClick;

            _btnSave.Text = "Kaydet";
            _btnSave.Size = new System.Drawing.Size(90, 30);
            _btnSave.Margin = new System.Windows.Forms.Padding(4);
            _btnSave.Click += OnSaveClick;

            // ===== Form =====
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            ClientSize = new System.Drawing.Size(520, 480);
            Controls.Add(_tabControl);
            Controls.Add(_flpButtons);
            Font = Theme.ModernTheme.FontBody;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "MikroSqlDbYedek \u2014 Ayarlar";
            AcceptButton = _btnSave;
            CancelButton = _btnCancel;

            _tabControl.ResumeLayout(false);
            _tabGeneral.ResumeLayout(false);
            _tabSmtp.ResumeLayout(false);
            _tlpGeneral.ResumeLayout(false);
            _tlpGeneral.PerformLayout();
            _tlpSmtp.ResumeLayout(false);
            _tlpSmtp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_nudLogRetention).EndInit();
            ((System.ComponentModel.ISupportInitialize)_nudHistoryRetention).EndInit();
            ((System.ComponentModel.ISupportInitialize)_nudSmtpPort).EndInit();
            _flpButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TabControl _tabControl;
        private System.Windows.Forms.TabPage _tabGeneral;
        private System.Windows.Forms.TabPage _tabSmtp;
        private System.Windows.Forms.TableLayoutPanel _tlpGeneral;
        private System.Windows.Forms.Label _lblLanguage;
        private System.Windows.Forms.ComboBox _cmbLanguage;
        private System.Windows.Forms.CheckBox _chkStartWithWindows;
        private System.Windows.Forms.CheckBox _chkMinimizeToTray;
        private System.Windows.Forms.Label _lblDefaultBackupPath;
        private System.Windows.Forms.TextBox _txtDefaultBackupPath;
        private System.Windows.Forms.Button _btnBrowseBackupPath;
        private System.Windows.Forms.Label _lblLogRetention;
        private System.Windows.Forms.NumericUpDown _nudLogRetention;
        private System.Windows.Forms.Label _lblLogRetentionSuffix;
        private System.Windows.Forms.Label _lblHistoryRetention;
        private System.Windows.Forms.NumericUpDown _nudHistoryRetention;
        private System.Windows.Forms.Label _lblHistoryRetentionSuffix;
        private System.Windows.Forms.TableLayoutPanel _tlpSmtp;
        private System.Windows.Forms.Label _lblSmtpHost;
        private System.Windows.Forms.TextBox _txtSmtpHost;
        private System.Windows.Forms.Label _lblSmtpPort;
        private System.Windows.Forms.NumericUpDown _nudSmtpPort;
        private System.Windows.Forms.CheckBox _chkSmtpSsl;
        private System.Windows.Forms.FlowLayoutPanel _pnlPortSsl;
        private System.Windows.Forms.Label _lblSmtpUsername;
        private System.Windows.Forms.TextBox _txtSmtpUsername;
        private System.Windows.Forms.Label _lblSmtpPassword;
        private System.Windows.Forms.TextBox _txtSmtpPassword;
        private System.Windows.Forms.Label _lblSmtpSenderEmail;
        private System.Windows.Forms.TextBox _txtSmtpSenderEmail;
        private System.Windows.Forms.Label _lblSmtpSenderName;
        private System.Windows.Forms.TextBox _txtSmtpSenderName;
        private System.Windows.Forms.Label _lblSmtpRecipients;
        private System.Windows.Forms.TextBox _txtSmtpRecipients;
        private System.Windows.Forms.Button _btnSmtpTest;
        private System.Windows.Forms.FlowLayoutPanel _flpButtons;
        private System.Windows.Forms.Button _btnSave;
        private System.Windows.Forms.Button _btnCancel;
    }
}
