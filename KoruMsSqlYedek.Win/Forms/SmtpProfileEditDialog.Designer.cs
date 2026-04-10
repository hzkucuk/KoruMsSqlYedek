namespace KoruMsSqlYedek.Win.Forms
{
    partial class SmtpProfileEditDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _toolTip = new System.Windows.Forms.ToolTip(components);

            _tlpMain = new System.Windows.Forms.TableLayoutPanel();
            _lblDisplayName = new System.Windows.Forms.Label();
            _txtDisplayName = new System.Windows.Forms.TextBox();
            _lblHost = new System.Windows.Forms.Label();
            _txtHost = new System.Windows.Forms.TextBox();
            _lblPort = new System.Windows.Forms.Label();
            _nudPort = new Theme.ModernNumericUpDown();
            _chkUseSsl = new Theme.ModernCheckBox();
            _lblUsername = new System.Windows.Forms.Label();
            _txtUsername = new System.Windows.Forms.TextBox();
            _lblPassword = new System.Windows.Forms.Label();
            _txtPassword = new System.Windows.Forms.TextBox();
            _lblSenderEmail = new System.Windows.Forms.Label();
            _txtSenderEmail = new System.Windows.Forms.TextBox();
            _lblSenderName = new System.Windows.Forms.Label();
            _txtSenderName = new System.Windows.Forms.TextBox();
            _lblRecipients = new System.Windows.Forms.Label();
            _txtRecipients = new System.Windows.Forms.TextBox();
            _pnlPortRow = new System.Windows.Forms.FlowLayoutPanel();
            _pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnTest = new Theme.ModernButton();
            _btnSave = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();

            _tlpMain.SuspendLayout();
            _pnlPortRow.SuspendLayout();
            _pnlButtons.SuspendLayout();
            SuspendLayout();

            // ── tlpMain ──
            _tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpMain.Padding = new System.Windows.Forms.Padding(16, 12, 16, 8);
            _tlpMain.ColumnCount = 2;
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpMain.RowCount = 10;
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            // ── Row 0: Profil Adı ──
            _lblDisplayName.AutoSize = true;
            _lblDisplayName.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblDisplayName.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblDisplayName.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblDisplayName.Text = "Profil Adı:";
            _txtDisplayName.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtDisplayName.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _tlpMain.Controls.Add(_lblDisplayName, 0, 0);
            _tlpMain.Controls.Add(_txtDisplayName, 1, 0);

            // ── Row 1: SMTP Sunucu ──
            _lblHost.AutoSize = true;
            _lblHost.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblHost.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblHost.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblHost.Text = "SMTP Sunucu:";
            _txtHost.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtHost.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _tlpMain.Controls.Add(_lblHost, 0, 1);
            _tlpMain.Controls.Add(_txtHost, 1, 1);

            // ── Row 2: Port + SSL ──
            _lblPort.Text = "Port:";
            _lblPort.AutoSize = true;
            _lblPort.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblPort.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblPort.ForeColor = Theme.ModernTheme.TextPrimary;
            _pnlPortRow.AutoSize = true;
            _pnlPortRow.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlPortRow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            _nudPort.Minimum = 1;
            _nudPort.Maximum = 65535;
            _nudPort.Value = 587;
            _nudPort.Width = 80;
            _nudPort.Margin = new System.Windows.Forms.Padding(0, 2, 12, 0);
            _chkUseSsl.Text = "SSL/TLS Kullan";
            _chkUseSsl.AutoSize = true;
            _chkUseSsl.Checked = true;
            _chkUseSsl.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            _pnlPortRow.Controls.Add(_nudPort);
            _pnlPortRow.Controls.Add(_chkUseSsl);
            _tlpMain.Controls.Add(_lblPort, 0, 2);
            _tlpMain.Controls.Add(_pnlPortRow, 1, 2);

            // ── Row 3: Kullanıcı Adı ──
            _lblUsername.AutoSize = true;
            _lblUsername.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblUsername.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblUsername.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblUsername.Text = "Kullanıcı Adı:";
            _txtUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtUsername.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _tlpMain.Controls.Add(_lblUsername, 0, 3);
            _tlpMain.Controls.Add(_txtUsername, 1, 3);

            // ── Row 4: Şifre ──
            _lblPassword.AutoSize = true;
            _lblPassword.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblPassword.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblPassword.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblPassword.Text = "Şifre:";
            _txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtPassword.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _txtPassword.UseSystemPasswordChar = true;
            _tlpMain.Controls.Add(_lblPassword, 0, 4);
            _tlpMain.Controls.Add(_txtPassword, 1, 4);

            // ── Row 5: Gönderici E-posta ──
            _lblSenderEmail.AutoSize = true;
            _lblSenderEmail.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblSenderEmail.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblSenderEmail.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblSenderEmail.Text = "Gönderici E-posta:";
            _txtSenderEmail.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSenderEmail.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _tlpMain.Controls.Add(_lblSenderEmail, 0, 5);
            _tlpMain.Controls.Add(_txtSenderEmail, 1, 5);

            // ── Row 6: Gönderici Adı ──
            _lblSenderName.AutoSize = true;
            _lblSenderName.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblSenderName.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblSenderName.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblSenderName.Text = "Gönderici Adı:";
            _txtSenderName.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSenderName.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _txtSenderName.Text = "Koru MsSql Yedek";
            _tlpMain.Controls.Add(_lblSenderName, 0, 6);
            _tlpMain.Controls.Add(_txtSenderName, 1, 6);

            // ── Row 7: Alıcılar ──
            _lblRecipients.AutoSize = true;
            _lblRecipients.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblRecipients.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblRecipients.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblRecipients.Text = "Alıcılar:";
            _txtRecipients.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtRecipients.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _tlpMain.Controls.Add(_lblRecipients, 0, 7);
            _tlpMain.Controls.Add(_txtRecipients, 1, 7);

            // ── Row 9: Buttons ──
            _pnlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _pnlButtons.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            _tlpMain.SetColumnSpan(_pnlButtons, 2);
            _tlpMain.Controls.Add(_pnlButtons, 0, 9);

            _btnCancel.Text = "İptal";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Ghost;
            _btnCancel.Size = new System.Drawing.Size(80, 30);
            _btnCancel.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            _btnCancel.Click += OnCancelClick;

            _btnSave.Text = "Kaydet";
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Size = new System.Drawing.Size(90, 30);
            _btnSave.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            _btnSave.Click += OnSaveClick;

            _btnTest.Text = "\u2709 Test E-postas\u0131 G\u00f6nder";
            _btnTest.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnTest.AutoSize = true;
            _btnTest.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            _btnTest.Click += OnTestClick;

            _pnlButtons.Controls.Add(_btnCancel);
            _pnlButtons.Controls.Add(_btnSave);
            _pnlButtons.Controls.Add(_btnTest);

            // ── Form ──
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(440, 400);
            Controls.Add(_tlpMain);
            MinimumSize = new System.Drawing.Size(440, 400);
            Text = "SMTP Profili";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

            _pnlPortRow.ResumeLayout(false);
            _tlpMain.ResumeLayout(false);
            _pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel _tlpMain;
        private System.Windows.Forms.Label _lblDisplayName;
        private System.Windows.Forms.TextBox _txtDisplayName;
        private System.Windows.Forms.Label _lblHost;
        private System.Windows.Forms.TextBox _txtHost;
        private System.Windows.Forms.Label _lblPort;
        private Theme.ModernNumericUpDown _nudPort;
        private Theme.ModernCheckBox _chkUseSsl;
        private System.Windows.Forms.Label _lblUsername;
        private System.Windows.Forms.TextBox _txtUsername;
        private System.Windows.Forms.Label _lblPassword;
        private System.Windows.Forms.TextBox _txtPassword;
        private System.Windows.Forms.Label _lblSenderEmail;
        private System.Windows.Forms.TextBox _txtSenderEmail;
        private System.Windows.Forms.Label _lblSenderName;
        private System.Windows.Forms.TextBox _txtSenderName;
        private System.Windows.Forms.Label _lblRecipients;
        private System.Windows.Forms.TextBox _txtRecipients;
        private System.Windows.Forms.FlowLayoutPanel _pnlPortRow;
        private System.Windows.Forms.FlowLayoutPanel _pnlButtons;
        private Theme.ModernButton _btnTest;
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;
        private System.Windows.Forms.ToolTip _toolTip;
    }
}
