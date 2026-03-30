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
            _pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnTest = new Theme.ModernButton();
            _btnSave = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();

            _tlpMain.SuspendLayout();
            _pnlButtons.SuspendLayout();
            SuspendLayout();

            // ── tlpMain ──
            _tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpMain.Padding = new System.Windows.Forms.Padding(16, 12, 16, 8);
            _tlpMain.ColumnCount = 2;
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpMain.RowCount = 10;
            for (int i = 0; i < 9; i++)
                _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            void AddRow(int row, System.Windows.Forms.Label lbl, System.Windows.Forms.Control ctrl)
            {
                lbl.AutoSize = true;
                lbl.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
                lbl.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
                lbl.ForeColor = Theme.ModernTheme.TextPrimary;
                ctrl.Dock = System.Windows.Forms.DockStyle.Fill;
                ctrl.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
                _tlpMain.Controls.Add(lbl, 0, row);
                _tlpMain.Controls.Add(ctrl, 1, row);
            }

            _lblDisplayName.Text = "Profil Adı:";
            AddRow(0, _lblDisplayName, _txtDisplayName);

            _lblHost.Text = "SMTP Sunucu:";
            AddRow(1, _lblHost, _txtHost);

            // Port + SSL satırı
            _lblPort.Text = "Port:";
            _lblPort.AutoSize = true;
            _lblPort.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblPort.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _lblPort.ForeColor = Theme.ModernTheme.TextPrimary;

            var pnlPortRow = new System.Windows.Forms.FlowLayoutPanel();
            pnlPortRow.AutoSize = true;
            pnlPortRow.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlPortRow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            _nudPort.Minimum = 1;
            _nudPort.Maximum = 65535;
            _nudPort.Value = 587;
            _nudPort.Width = 80;
            _nudPort.Margin = new System.Windows.Forms.Padding(0, 2, 12, 0);
            _chkUseSsl.Text = "SSL/TLS Kullan";
            _chkUseSsl.AutoSize = true;
            _chkUseSsl.Checked = true;
            _chkUseSsl.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            pnlPortRow.Controls.Add(_nudPort);
            pnlPortRow.Controls.Add(_chkUseSsl);
            _tlpMain.Controls.Add(_lblPort, 0, 2);
            _tlpMain.Controls.Add(pnlPortRow, 1, 2);

            _lblUsername.Text = "Kullanıcı Adı:";
            AddRow(3, _lblUsername, _txtUsername);

            _lblPassword.Text = "Şifre:";
            _txtPassword.UseSystemPasswordChar = true;
            AddRow(4, _lblPassword, _txtPassword);

            _lblSenderEmail.Text = "Gönderici E-posta:";
            AddRow(5, _lblSenderEmail, _txtSenderEmail);

            _lblSenderName.Text = "Gönderici Adı:";
            _txtSenderName.Text = "Koru MsSql Yedek";
            AddRow(6, _lblSenderName, _txtSenderName);

            _lblRecipients.Text = "Alıcılar:";
            AddRow(7, _lblRecipients, _txtRecipients);

            // ── Buttons ──
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
        private System.Windows.Forms.FlowLayoutPanel _pnlButtons;
        private Theme.ModernButton _btnTest;
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;
    }
}
