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
            _grpFtp = new System.Windows.Forms.GroupBox();
            _lblHost = new System.Windows.Forms.Label();
            _txtHost = new System.Windows.Forms.TextBox();
            _lblPort = new System.Windows.Forms.Label();
            _nudPort = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _lblUsername = new System.Windows.Forms.Label();
            _txtUsername = new System.Windows.Forms.TextBox();
            _lblPassword = new System.Windows.Forms.Label();
            _txtPassword = new System.Windows.Forms.TextBox();
            _grpOAuth = new System.Windows.Forms.GroupBox();
            _btnGoogleAuth = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _lblAuthStatus = new System.Windows.Forms.Label();
            _lblAccountEmail = new System.Windows.Forms.Label();
            _btnOAuthSettings = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _grpLocal = new System.Windows.Forms.GroupBox();
            _lblLocalOrUncPath = new System.Windows.Forms.Label();
            _txtLocalOrUncPath = new System.Windows.Forms.TextBox();
            _btnBrowseLocal = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _lblUncUser = new System.Windows.Forms.Label();
            _txtUncUser = new System.Windows.Forms.TextBox();
            _lblUncPassword = new System.Windows.Forms.Label();
            _txtUncPassword = new System.Windows.Forms.TextBox();
            _lblRemotePath = new System.Windows.Forms.Label();
            _txtRemotePath = new System.Windows.Forms.TextBox();
            _lblBandwidth = new System.Windows.Forms.Label();
            _nudBandwidth = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _lblTrashRetention = new System.Windows.Forms.Label();
            _nudTrashRetention = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _lblTrashRetentionHint = new System.Windows.Forms.Label();
            _btnSave = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnCancel = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _grpFtp.SuspendLayout();
            _grpOAuth.SuspendLayout();
            _grpLocal.SuspendLayout();
            SuspendLayout();
            // 
            // _lblDisplayName
            // 
            _lblDisplayName.AutoSize = true;
            _lblDisplayName.Location = new System.Drawing.Point(15, 20);
            _lblDisplayName.Name = "_lblDisplayName";
            _lblDisplayName.Size = new System.Drawing.Size(81, 17);
            _lblDisplayName.TabIndex = 0;
            _lblDisplayName.Text = "Görünen Ad:";
            // 
            // _txtDisplayName
            // 
            _txtDisplayName.Location = new System.Drawing.Point(180, 17);
            _txtDisplayName.Name = "_txtDisplayName";
            _txtDisplayName.Size = new System.Drawing.Size(315, 24);
            _txtDisplayName.TabIndex = 1;
            // 
            // _lblProviderType
            // 
            _lblProviderType.AutoSize = true;
            _lblProviderType.Location = new System.Drawing.Point(15, 60);
            _lblProviderType.Name = "_lblProviderType";
            _lblProviderType.Size = new System.Drawing.Size(91, 17);
            _lblProviderType.TabIndex = 2;
            _lblProviderType.Text = "Sağlayıcı Türü:";
            // 
            // _cmbProviderType
            // 
            _cmbProviderType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbProviderType.Location = new System.Drawing.Point(180, 57);
            _cmbProviderType.Name = "_cmbProviderType";
            _cmbProviderType.Size = new System.Drawing.Size(315, 25);
            _cmbProviderType.TabIndex = 3;
            _cmbProviderType.SelectedIndexChanged += OnProviderTypeChanged;
            // 
            // _chkEnabled
            // 
            _chkEnabled.AutoSize = true;
            _chkEnabled.Checked = true;
            _chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            _chkEnabled.Location = new System.Drawing.Point(180, 91);
            _chkEnabled.Name = "_chkEnabled";
            _chkEnabled.Size = new System.Drawing.Size(52, 21);
            _chkEnabled.TabIndex = 4;
            _chkEnabled.Text = "Aktif";
            // 
            // _grpFtp
            // 
            _grpFtp.Controls.Add(_lblHost);
            _grpFtp.Controls.Add(_txtHost);
            _grpFtp.Controls.Add(_lblPort);
            _grpFtp.Controls.Add(_nudPort);
            _grpFtp.Controls.Add(_lblUsername);
            _grpFtp.Controls.Add(_txtUsername);
            _grpFtp.Controls.Add(_lblPassword);
            _grpFtp.Controls.Add(_txtPassword);
            _grpFtp.Location = new System.Drawing.Point(15, 125);
            _grpFtp.Name = "_grpFtp";
            _grpFtp.Size = new System.Drawing.Size(490, 164);
            _grpFtp.TabIndex = 5;
            _grpFtp.TabStop = false;
            _grpFtp.Text = "FTP / SFTP Ayarları";
            // 
            // _lblHost
            // 
            _lblHost.AutoSize = true;
            _lblHost.Location = new System.Drawing.Point(6, 28);
            _lblHost.Name = "_lblHost";
            _lblHost.Size = new System.Drawing.Size(52, 17);
            _lblHost.TabIndex = 0;
            _lblHost.Text = "Sunucu:";
            // 
            // _txtHost
            // 
            _txtHost.Location = new System.Drawing.Point(140, 25);
            _txtHost.Name = "_txtHost";
            _txtHost.Size = new System.Drawing.Size(260, 24);
            _txtHost.TabIndex = 1;
            // 
            // _lblPort
            // 
            _lblPort.AutoSize = true;
            _lblPort.Location = new System.Drawing.Point(6, 62);
            _lblPort.Name = "_lblPort";
            _lblPort.Size = new System.Drawing.Size(35, 17);
            _lblPort.TabIndex = 2;
            _lblPort.Text = "Port:";
            // 
            // _nudPort
            // 
            _nudPort.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _nudPort.Location = new System.Drawing.Point(140, 59);
            _nudPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            _nudPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _nudPort.MinimumSize = new System.Drawing.Size(60, 27);
            _nudPort.Name = "_nudPort";
            _nudPort.Size = new System.Drawing.Size(80, 27);
            _nudPort.TabIndex = 3;
            _nudPort.Value = new decimal(new int[] { 21, 0, 0, 0 });
            // 
            // _lblUsername
            // 
            _lblUsername.AutoSize = true;
            _lblUsername.Location = new System.Drawing.Point(6, 96);
            _lblUsername.Name = "_lblUsername";
            _lblUsername.Size = new System.Drawing.Size(81, 17);
            _lblUsername.TabIndex = 4;
            _lblUsername.Text = "Kullanıcı Adı:";
            // 
            // _txtUsername
            // 
            _txtUsername.Location = new System.Drawing.Point(140, 93);
            _txtUsername.Name = "_txtUsername";
            _txtUsername.Size = new System.Drawing.Size(260, 24);
            _txtUsername.TabIndex = 5;
            // 
            // _lblPassword
            // 
            _lblPassword.AutoSize = true;
            _lblPassword.Location = new System.Drawing.Point(6, 130);
            _lblPassword.Name = "_lblPassword";
            _lblPassword.Size = new System.Drawing.Size(37, 17);
            _lblPassword.TabIndex = 6;
            _lblPassword.Text = "Şifre:";
            // 
            // _txtPassword
            // 
            _txtPassword.Location = new System.Drawing.Point(140, 127);
            _txtPassword.Name = "_txtPassword";
            _txtPassword.Size = new System.Drawing.Size(260, 24);
            _txtPassword.TabIndex = 7;
            _txtPassword.UseSystemPasswordChar = true;
            // 
            // _grpOAuth
            // 
            _grpOAuth.Controls.Add(_btnGoogleAuth);
            _grpOAuth.Controls.Add(_lblAuthStatus);
            _grpOAuth.Controls.Add(_lblAccountEmail);
            _grpOAuth.Controls.Add(_btnOAuthSettings);
            _grpOAuth.Location = new System.Drawing.Point(15, 125);
            _grpOAuth.Name = "_grpOAuth";
            _grpOAuth.Size = new System.Drawing.Size(490, 90);
            _grpOAuth.TabIndex = 6;
            _grpOAuth.TabStop = false;
            _grpOAuth.Text = "Hesap Bağlama";
            // 
            // _btnGoogleAuth
            // 
            _btnGoogleAuth.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnGoogleAuth.CornerRadius = 6;
            _btnGoogleAuth.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnGoogleAuth.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnGoogleAuth.IconSymbol = "";
            _btnGoogleAuth.Location = new System.Drawing.Point(6, 27);
            _btnGoogleAuth.Name = "_btnGoogleAuth";
            _btnGoogleAuth.Size = new System.Drawing.Size(160, 32);
            _btnGoogleAuth.TabIndex = 0;
            _btnGoogleAuth.Text = "Hesabı Bağla";
            _btnGoogleAuth.Click += OnGoogleAuthClick;
            // 
            // _lblAuthStatus
            // 
            _lblAuthStatus.AutoSize = true;
            _lblAuthStatus.Location = new System.Drawing.Point(174, 33);
            _lblAuthStatus.Name = "_lblAuthStatus";
            _lblAuthStatus.Size = new System.Drawing.Size(130, 17);
            _lblAuthStatus.TabIndex = 1;
            _lblAuthStatus.Text = "Henüz doğrulanmadı";
            // 
            // _lblAccountEmail
            // 
            _lblAccountEmail.AutoSize = true;
            _lblAccountEmail.Location = new System.Drawing.Point(6, 64);
            _lblAccountEmail.Name = "_lblAccountEmail";
            _lblAccountEmail.Size = new System.Drawing.Size(200, 17);
            _lblAccountEmail.TabIndex = 3;
            _lblAccountEmail.Text = "";
            _lblAccountEmail.Tag = "secondary";
            // 
            // _btnOAuthSettings
            //
            _btnOAuthSettings.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnOAuthSettings.CornerRadius = 6;
            _btnOAuthSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnOAuthSettings.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnOAuthSettings.IconSymbol = "";
            _btnOAuthSettings.Location = new System.Drawing.Point(450, 27);
            _btnOAuthSettings.Name = "_btnOAuthSettings";
            _btnOAuthSettings.Size = new System.Drawing.Size(30, 32);
            _btnOAuthSettings.TabIndex = 2;
            _btnOAuthSettings.Click += OnOAuthSettingsClick;
            // 
            // _grpLocal
            // 
            _grpLocal.Controls.Add(_lblLocalOrUncPath);
            _grpLocal.Controls.Add(_txtLocalOrUncPath);
            _grpLocal.Controls.Add(_btnBrowseLocal);
            _grpLocal.Controls.Add(_lblUncUser);
            _grpLocal.Controls.Add(_txtUncUser);
            _grpLocal.Controls.Add(_lblUncPassword);
            _grpLocal.Controls.Add(_txtUncPassword);
            _grpLocal.Location = new System.Drawing.Point(15, 125);
            _grpLocal.Name = "_grpLocal";
            _grpLocal.Size = new System.Drawing.Size(490, 164);
            _grpLocal.TabIndex = 7;
            _grpLocal.TabStop = false;
            _grpLocal.Text = "Yerel / Ağ Yolu";
            // 
            // _lblLocalOrUncPath
            // 
            _lblLocalOrUncPath.AutoSize = true;
            _lblLocalOrUncPath.Location = new System.Drawing.Point(6, 28);
            _lblLocalOrUncPath.Name = "_lblLocalOrUncPath";
            _lblLocalOrUncPath.Size = new System.Drawing.Size(28, 17);
            _lblLocalOrUncPath.TabIndex = 0;
            _lblLocalOrUncPath.Text = "Yol:";
            // 
            // _txtLocalOrUncPath
            // 
            _txtLocalOrUncPath.Location = new System.Drawing.Point(140, 25);
            _txtLocalOrUncPath.Name = "_txtLocalOrUncPath";
            _txtLocalOrUncPath.Size = new System.Drawing.Size(260, 24);
            _txtLocalOrUncPath.TabIndex = 1;
            // 
            // _btnBrowseLocal
            // 
            _btnBrowseLocal.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBrowseLocal.CornerRadius = 6;
            _btnBrowseLocal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnBrowseLocal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnBrowseLocal.IconSymbol = "";
            _btnBrowseLocal.Location = new System.Drawing.Point(410, 25);
            _btnBrowseLocal.Name = "_btnBrowseLocal";
            _btnBrowseLocal.Size = new System.Drawing.Size(30, 32);
            _btnBrowseLocal.TabIndex = 2;
            _btnBrowseLocal.Text = "...";
            _btnBrowseLocal.Click += OnBrowseLocalPath;
            // 
            // _lblUncUser
            // 
            _lblUncUser.AutoSize = true;
            _lblUncUser.Location = new System.Drawing.Point(6, 62);
            _lblUncUser.Name = "_lblUncUser";
            _lblUncUser.Size = new System.Drawing.Size(81, 17);
            _lblUncUser.TabIndex = 3;
            _lblUncUser.Text = "Kullanıcı Adı:";
            // 
            // _txtUncUser
            // 
            _txtUncUser.Location = new System.Drawing.Point(140, 59);
            _txtUncUser.Name = "_txtUncUser";
            _txtUncUser.Size = new System.Drawing.Size(260, 24);
            _txtUncUser.TabIndex = 4;
            // 
            // _lblUncPassword
            // 
            _lblUncPassword.AutoSize = true;
            _lblUncPassword.Location = new System.Drawing.Point(6, 96);
            _lblUncPassword.Name = "_lblUncPassword";
            _lblUncPassword.Size = new System.Drawing.Size(37, 17);
            _lblUncPassword.TabIndex = 5;
            _lblUncPassword.Text = "Şifre:";
            // 
            // _txtUncPassword
            // 
            _txtUncPassword.Location = new System.Drawing.Point(140, 93);
            _txtUncPassword.Name = "_txtUncPassword";
            _txtUncPassword.Size = new System.Drawing.Size(260, 24);
            _txtUncPassword.TabIndex = 6;
            _txtUncPassword.UseSystemPasswordChar = true;
            // 
            // _lblRemotePath
            // 
            _lblRemotePath.AutoSize = true;
            _lblRemotePath.Location = new System.Drawing.Point(15, 306);
            _lblRemotePath.Name = "_lblRemotePath";
            _lblRemotePath.Size = new System.Drawing.Size(108, 17);
            _lblRemotePath.TabIndex = 8;
            _lblRemotePath.Text = "Uzak Klasör Yolu:";
            // 
            // _txtRemotePath
            // 
            _txtRemotePath.Location = new System.Drawing.Point(180, 303);
            _txtRemotePath.Name = "_txtRemotePath";
            _txtRemotePath.Size = new System.Drawing.Size(315, 24);
            _txtRemotePath.TabIndex = 9;
            // 
            // _lblBandwidth
            // 
            _lblBandwidth.AutoSize = true;
            _lblBandwidth.Location = new System.Drawing.Point(15, 343);
            _lblBandwidth.Name = "_lblBandwidth";
            _lblBandwidth.Size = new System.Drawing.Size(105, 17);
            _lblBandwidth.TabIndex = 10;
            _lblBandwidth.Text = "Hız Limiti (MB/s):";
            // 
            // _nudBandwidth
            // 
            _nudBandwidth.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _nudBandwidth.Location = new System.Drawing.Point(180, 340);
            _nudBandwidth.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            _nudBandwidth.MinimumSize = new System.Drawing.Size(60, 27);
            _nudBandwidth.Name = "_nudBandwidth";
            _nudBandwidth.Size = new System.Drawing.Size(80, 27);
            _nudBandwidth.TabIndex = 11;
            // 
            // _lblTrashRetention
            // 
            _lblTrashRetention.AutoSize = true;
            _lblTrashRetention.Location = new System.Drawing.Point(15, 377);
            _lblTrashRetention.Name = "_lblTrashRetention";
            _lblTrashRetention.Size = new System.Drawing.Size(125, 17);
            _lblTrashRetention.TabIndex = 12;
            _lblTrashRetention.Text = "\u00C7\u00F6p Saklama (G\u00FCn):";
            // 
            // _nudTrashRetention
            // 
            _nudTrashRetention.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _nudTrashRetention.Location = new System.Drawing.Point(180, 374);
            _nudTrashRetention.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            _nudTrashRetention.MinimumSize = new System.Drawing.Size(60, 27);
            _nudTrashRetention.Name = "_nudTrashRetention";
            _nudTrashRetention.Size = new System.Drawing.Size(80, 27);
            _nudTrashRetention.TabIndex = 13;
            // 
            // _lblTrashRetentionHint
            // 
            _lblTrashRetentionHint.AutoSize = true;
            _lblTrashRetentionHint.Tag = "secondary";
            _lblTrashRetentionHint.Location = new System.Drawing.Point(265, 377);
            _lblTrashRetentionHint.Name = "_lblTrashRetentionHint";
            _lblTrashRetentionHint.Size = new System.Drawing.Size(100, 17);
            _lblTrashRetentionHint.TabIndex = 14;
            _lblTrashRetentionHint.Text = "(0 = hemen sil)";
            // 
            // _btnSave
            // 
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.CornerRadius = 6;
            _btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnSave.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnSave.IconSymbol = "";
            _btnSave.Location = new System.Drawing.Point(305, 419);
            _btnSave.Name = "_btnSave";
            _btnSave.Size = new System.Drawing.Size(100, 39);
            _btnSave.TabIndex = 15;
            _btnSave.Text = "💾 Kaydet";
            _btnSave.Click += OnSaveClick;
            // 
            // _btnCancel
            // 
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.CornerRadius = 6;
            _btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnCancel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            _btnCancel.IconSymbol = "";
            _btnCancel.Location = new System.Drawing.Point(415, 419);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new System.Drawing.Size(90, 39);
            _btnCancel.TabIndex = 16;
            _btnCancel.Text = "İptal";
            _btnCancel.Click += OnCancelClick;
            // 
            // CloudTargetEditDialog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(520, 470);
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
            Controls.Add(_lblTrashRetention);
            Controls.Add(_nudTrashRetention);
            Controls.Add(_lblTrashRetentionHint);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
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
        private Theme.ModernButton _btnGoogleAuth;
        private System.Windows.Forms.Label _lblAuthStatus;
        private System.Windows.Forms.Label _lblAccountEmail;
        private Theme.ModernButton _btnOAuthSettings;

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
        private System.Windows.Forms.Label _lblTrashRetention;
        private Theme.ModernNumericUpDown _nudTrashRetention;
        private System.Windows.Forms.Label _lblTrashRetentionHint;

        // Butonlar
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;

        // Tooltip
        private System.Windows.Forms.ToolTip _toolTipRemotePath;
    }
}
