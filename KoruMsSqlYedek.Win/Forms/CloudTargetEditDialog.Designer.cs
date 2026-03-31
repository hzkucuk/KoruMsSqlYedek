namespace KoruMsSqlYedek.Win.Forms
{
    partial class CloudTargetEditDialog
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
            _toolTipRemotePath = new System.Windows.Forms.ToolTip(components);

            _lblDisplayName = new System.Windows.Forms.Label();
            _txtDisplayName = new System.Windows.Forms.TextBox();
            _lblProviderType = new System.Windows.Forms.Label();
            _cmbProviderType = new System.Windows.Forms.ComboBox();
            _chkEnabled = new System.Windows.Forms.CheckBox();

            // FTP/SFTP grubu
            _grpFtp = new System.Windows.Forms.GroupBox();
            _lblHost = new System.Windows.Forms.Label();
            _txtHost = new System.Windows.Forms.TextBox();
            _lblPort = new System.Windows.Forms.Label();
            _nudPort = new Theme.ModernNumericUpDown();
            _lblUsername = new System.Windows.Forms.Label();
            _txtUsername = new System.Windows.Forms.TextBox();
            _lblPassword = new System.Windows.Forms.Label();
            _txtPassword = new System.Windows.Forms.TextBox();

            // OAuth grubu
            _grpOAuth = new System.Windows.Forms.GroupBox();
            _lblClientId = new System.Windows.Forms.Label();
            _txtClientId = new System.Windows.Forms.TextBox();
            _lblClientSecret = new System.Windows.Forms.Label();
            _txtClientSecret = new System.Windows.Forms.TextBox();
            _btnGoogleAuth = new Theme.ModernButton();
            _lblAuthStatus = new System.Windows.Forms.Label();

            // Yerel/UNC grubu
            _grpLocal = new System.Windows.Forms.GroupBox();
            _lblLocalOrUncPath = new System.Windows.Forms.Label();
            _txtLocalOrUncPath = new System.Windows.Forms.TextBox();
            _btnBrowseLocal = new Theme.ModernButton();
            _lblUncUser = new System.Windows.Forms.Label();
            _txtUncUser = new System.Windows.Forms.TextBox();
            _lblUncPassword = new System.Windows.Forms.Label();
            _txtUncPassword = new System.Windows.Forms.TextBox();

            // Ortak alanlar
            _lblRemotePath = new System.Windows.Forms.Label();
            _txtRemotePath = new System.Windows.Forms.TextBox();
            _lblBandwidth = new System.Windows.Forms.Label();
            _nudBandwidth = new Theme.ModernNumericUpDown();
            _chkPermanentDelete = new System.Windows.Forms.CheckBox();

            // Butonlar
            _btnSave = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();

            _grpFtp.SuspendLayout();
            _grpOAuth.SuspendLayout();
            _grpLocal.SuspendLayout();
            SuspendLayout();

            int lx = 15, tx = 145, tw = 280;

            // Görünen Ad
            _lblDisplayName.Text = "Görünen Ad:";
            _lblDisplayName.AutoSize = true;
            _lblDisplayName.Location = new System.Drawing.Point(lx, 18);
            _txtDisplayName.Location = new System.Drawing.Point(tx, 15);
            _txtDisplayName.Size = new System.Drawing.Size(tw, 23);

            // Provider Türü
            _lblProviderType.Text = "Sağlayıcı Türü:";
            _lblProviderType.AutoSize = true;
            _lblProviderType.Location = new System.Drawing.Point(lx, 53);
            _cmbProviderType.Location = new System.Drawing.Point(tx, 50);
            _cmbProviderType.Size = new System.Drawing.Size(tw, 23);
            _cmbProviderType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbProviderType.SelectedIndexChanged += OnProviderTypeChanged;

            // Aktif
            _chkEnabled.Text = "Aktif";
            _chkEnabled.Checked = true;
            _chkEnabled.Location = new System.Drawing.Point(tx, 80);
            _chkEnabled.AutoSize = true;

            // === FTP/SFTP Grubu ===
            _grpFtp.Text = "FTP / SFTP Ayarları";
            _grpFtp.Location = new System.Drawing.Point(lx, 110);
            _grpFtp.Size = new System.Drawing.Size(tw + 140, 145);

            _lblHost.Text = "Sunucu:";
            _lblHost.AutoSize = true;
            _lblHost.Location = new System.Drawing.Point(6, 25);
            _txtHost.Location = new System.Drawing.Point(130, 22);
            _txtHost.Size = new System.Drawing.Size(200, 23);

            _lblPort.Text = "Port:";
            _lblPort.AutoSize = true;
            _lblPort.Location = new System.Drawing.Point(6, 55);
            _nudPort.Location = new System.Drawing.Point(130, 52);
            _nudPort.Size = new System.Drawing.Size(80, 23);
            _nudPort.Minimum = 1;
            _nudPort.Maximum = 65535;
            _nudPort.Value = 21;

            _lblUsername.Text = "Kullanıcı Adı:";
            _lblUsername.AutoSize = true;
            _lblUsername.Location = new System.Drawing.Point(6, 85);
            _txtUsername.Location = new System.Drawing.Point(130, 82);
            _txtUsername.Size = new System.Drawing.Size(200, 23);

            _lblPassword.Text = "Şifre:";
            _lblPassword.AutoSize = true;
            _lblPassword.Location = new System.Drawing.Point(6, 115);
            _txtPassword.Location = new System.Drawing.Point(130, 112);
            _txtPassword.Size = new System.Drawing.Size(200, 23);
            _txtPassword.UseSystemPasswordChar = true;

            _grpFtp.Controls.Add(_lblHost);
            _grpFtp.Controls.Add(_txtHost);
            _grpFtp.Controls.Add(_lblPort);
            _grpFtp.Controls.Add(_nudPort);
            _grpFtp.Controls.Add(_lblUsername);
            _grpFtp.Controls.Add(_txtUsername);
            _grpFtp.Controls.Add(_lblPassword);
            _grpFtp.Controls.Add(_txtPassword);

            // === OAuth Grubu ===
            _grpOAuth.Text = "OAuth2 Ayarları";
            _grpOAuth.Location = new System.Drawing.Point(lx, 110);
            _grpOAuth.Size = new System.Drawing.Size(tw + 140, 120);

            _lblClientId.Text = "Client ID:";
            _lblClientId.AutoSize = true;
            _lblClientId.Location = new System.Drawing.Point(6, 25);
            _txtClientId.Location = new System.Drawing.Point(130, 22);
            _txtClientId.Size = new System.Drawing.Size(270, 23);

            _lblClientSecret.Text = "Client Secret:";
            _lblClientSecret.AutoSize = true;
            _lblClientSecret.Location = new System.Drawing.Point(6, 55);
            _txtClientSecret.Location = new System.Drawing.Point(130, 52);
            _txtClientSecret.Size = new System.Drawing.Size(270, 23);
            _txtClientSecret.UseSystemPasswordChar = true;

            _btnGoogleAuth.Text = "Hesab\u0131 Ba\u011fla";
            _btnGoogleAuth.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnGoogleAuth.Size = new System.Drawing.Size(160, 28);
            _btnGoogleAuth.Location = new System.Drawing.Point(6, 84);
            _btnGoogleAuth.Click += OnGoogleAuthClick;

            _lblAuthStatus.AutoSize = true;
            _lblAuthStatus.Location = new System.Drawing.Point(174, 89);
            _lblAuthStatus.Text = "Hen\u00fcz do\u011frulanmad\u0131";

            _grpOAuth.Controls.Add(_lblClientId);
            _grpOAuth.Controls.Add(_txtClientId);
            _grpOAuth.Controls.Add(_lblClientSecret);
            _grpOAuth.Controls.Add(_txtClientSecret);
            _grpOAuth.Controls.Add(_btnGoogleAuth);
            _grpOAuth.Controls.Add(_lblAuthStatus);

            // === Yerel/UNC Grubu ===
            _grpLocal.Text = "Yerel / Ağ Yolu";
            _grpLocal.Location = new System.Drawing.Point(lx, 110);
            _grpLocal.Size = new System.Drawing.Size(tw + 140, 145);

            _lblLocalOrUncPath.Text = "Yol:";
            _lblLocalOrUncPath.AutoSize = true;
            _lblLocalOrUncPath.Location = new System.Drawing.Point(6, 25);
            _txtLocalOrUncPath.Location = new System.Drawing.Point(130, 22);
            _txtLocalOrUncPath.Size = new System.Drawing.Size(235, 23);
            _btnBrowseLocal.Text = "...";
            _btnBrowseLocal.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBrowseLocal.Size = new System.Drawing.Size(30, 28);
            _btnBrowseLocal.Location = new System.Drawing.Point(370, 22);
            _btnBrowseLocal.Click += OnBrowseLocalPath;

            _lblUncUser.Text = "Kullanıcı Adı:";
            _lblUncUser.AutoSize = true;
            _lblUncUser.Location = new System.Drawing.Point(6, 55);
            _txtUncUser.Location = new System.Drawing.Point(130, 52);
            _txtUncUser.Size = new System.Drawing.Size(200, 23);

            _lblUncPassword.Text = "Şifre:";
            _lblUncPassword.AutoSize = true;
            _lblUncPassword.Location = new System.Drawing.Point(6, 85);
            _txtUncPassword.Location = new System.Drawing.Point(130, 82);
            _txtUncPassword.Size = new System.Drawing.Size(200, 23);
            _txtUncPassword.UseSystemPasswordChar = true;

            _grpLocal.Controls.Add(_lblLocalOrUncPath);
            _grpLocal.Controls.Add(_txtLocalOrUncPath);
            _grpLocal.Controls.Add(_btnBrowseLocal);
            _grpLocal.Controls.Add(_lblUncUser);
            _grpLocal.Controls.Add(_txtUncUser);
            _grpLocal.Controls.Add(_lblUncPassword);
            _grpLocal.Controls.Add(_txtUncPassword);

            // === Ortak Alanlar (grup altında) ===
            _lblRemotePath.Text = "Uzak Klasör Yolu:";
            _lblRemotePath.AutoSize = true;
            _lblRemotePath.Location = new System.Drawing.Point(lx, 270);
            _txtRemotePath.Location = new System.Drawing.Point(tx, 267);
            _txtRemotePath.Size = new System.Drawing.Size(tw, 23);

            _lblBandwidth.Text = "Hız Limiti (MB/s):";
            _lblBandwidth.AutoSize = true;
            _lblBandwidth.Location = new System.Drawing.Point(lx, 303);
            _nudBandwidth.Location = new System.Drawing.Point(tx, 300);
            _nudBandwidth.Size = new System.Drawing.Size(80, 23);
            _nudBandwidth.Minimum = 0;
            _nudBandwidth.Maximum = 10000;
            _nudBandwidth.Value = 0;

            _chkPermanentDelete.Text = "Silinen dosyaları çöp kutusundan kalıcı temizle";
            _chkPermanentDelete.Location = new System.Drawing.Point(tx, 330);
            _chkPermanentDelete.AutoSize = true;
            _chkPermanentDelete.Checked = true;

            // === Butonlar ===
            _btnSave.Text = "💾 Kaydet";
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Size = new System.Drawing.Size(100, 34);
            _btnSave.Location = new System.Drawing.Point(235, 370);
            _btnSave.Click += OnSaveClick;

            _btnCancel.Text = "İptal";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.Size = new System.Drawing.Size(90, 34);
            _btnCancel.Location = new System.Drawing.Point(345, 370);
            _btnCancel.Click += OnCancelClick;

            // === Form ===
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(450, 415);
            Controls.Add(_lblDisplayName);
            Controls.Add(_txtDisplayName);
            Controls.Add(_lblProviderType);
            Controls.Add(_cmbProviderType);
            Controls.Add(_chkEnabled);
            Controls.Add(_grpFtp);
            Controls.Add(_grpOAuth);
            Controls.Add(_grpLocal);
            Controls.Add(_lblRemotePath);
            Controls.Add(_txtRemotePath);
            Controls.Add(_lblBandwidth);
            Controls.Add(_nudBandwidth);
            Controls.Add(_chkPermanentDelete);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
            Font = Theme.ModernTheme.FontBody;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            BackColor = Theme.ModernTheme.BackgroundColor;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CloudTargetEditDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Bulut Hedef";

            _grpFtp.ResumeLayout(false);
            _grpFtp.PerformLayout();
            _grpOAuth.ResumeLayout(false);
            _grpOAuth.PerformLayout();
            _grpLocal.ResumeLayout(false);
            _grpLocal.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label _lblDisplayName;
        private System.Windows.Forms.TextBox _txtDisplayName;
        private System.Windows.Forms.Label _lblProviderType;
        private System.Windows.Forms.ComboBox _cmbProviderType;
        private System.Windows.Forms.CheckBox _chkEnabled;

        // FTP/SFTP
        private System.Windows.Forms.GroupBox _grpFtp;
        private System.Windows.Forms.Label _lblHost;
        private System.Windows.Forms.TextBox _txtHost;
        private System.Windows.Forms.Label _lblPort;
        private Theme.ModernNumericUpDown _nudPort;
        private System.Windows.Forms.Label _lblUsername;
        private System.Windows.Forms.TextBox _txtUsername;
        private System.Windows.Forms.Label _lblPassword;
        private System.Windows.Forms.TextBox _txtPassword;

        // OAuth
        private System.Windows.Forms.GroupBox _grpOAuth;
        private System.Windows.Forms.Label _lblClientId;
        private System.Windows.Forms.TextBox _txtClientId;
        private System.Windows.Forms.Label _lblClientSecret;
        private System.Windows.Forms.TextBox _txtClientSecret;
        private Theme.ModernButton _btnGoogleAuth;
        private System.Windows.Forms.Label _lblAuthStatus;

        // Yerel/UNC
        private System.Windows.Forms.GroupBox _grpLocal;
        private System.Windows.Forms.Label _lblLocalOrUncPath;
        private System.Windows.Forms.TextBox _txtLocalOrUncPath;
        private Theme.ModernButton _btnBrowseLocal;
        private System.Windows.Forms.Label _lblUncUser;
        private System.Windows.Forms.TextBox _txtUncUser;
        private System.Windows.Forms.Label _lblUncPassword;
        private System.Windows.Forms.TextBox _txtUncPassword;

        // Ortak
        private System.Windows.Forms.Label _lblRemotePath;
        private System.Windows.Forms.TextBox _txtRemotePath;
        private System.Windows.Forms.Label _lblBandwidth;
        private Theme.ModernNumericUpDown _nudBandwidth;
        private System.Windows.Forms.CheckBox _chkPermanentDelete;

        // Butonlar
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;

        // Tooltip
        private System.Windows.Forms.ToolTip _toolTipRemotePath;
    }
}
