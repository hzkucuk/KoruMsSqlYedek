namespace KoruMsSqlYedek.Win.Forms
{
    partial class GoogleOAuthSettingsDialog
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
            _lblTitle = new System.Windows.Forms.Label();
            _lblInfo = new System.Windows.Forms.Label();
            _lblClientId = new System.Windows.Forms.Label();
            _txtClientId = new Theme.ModernTextBox();
            _lblClientSecret = new System.Windows.Forms.Label();
            _txtClientSecret = new Theme.ModernTextBox();
            _lblStatus = new System.Windows.Forms.Label();
            _btnSave = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();
            _btnRemove = new Theme.ModernButton();

            SuspendLayout();

            // _lblTitle
            _lblTitle.AutoSize = true;
            _lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            _lblTitle.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblTitle.Location = new System.Drawing.Point(20, 16);
            _lblTitle.Text = "\u2601 Google API Kimlik Bilgileri";

            // _lblInfo
            _lblInfo.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblInfo.Location = new System.Drawing.Point(20, 48);
            _lblInfo.Size = new System.Drawing.Size(380, 44);
            _lblInfo.Text = "Google Cloud Console'dan ald\u0131\u011f\u0131n\u0131z OAuth 2.0 Desktop Client bilgilerini girin. " +
                "Bo\u015f b\u0131rak\u0131l\u0131rsa g\u00f6m\u00fcl\u00fc varsay\u0131lan de\u011ferler kullan\u0131l\u0131r.";

            // _lblClientId
            _lblClientId.AutoSize = true;
            _lblClientId.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblClientId.Location = new System.Drawing.Point(20, 100);
            _lblClientId.Text = "Client ID:";

            // _txtClientId
            _txtClientId.Location = new System.Drawing.Point(20, 122);
            _txtClientId.Size = new System.Drawing.Size(380, 32);
            _txtClientId.Placeholder = "xxxxxxxx.apps.googleusercontent.com";

            // _lblClientSecret
            _lblClientSecret.AutoSize = true;
            _lblClientSecret.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblClientSecret.Location = new System.Drawing.Point(20, 164);
            _lblClientSecret.Text = "Client Secret:";

            // _txtClientSecret
            _txtClientSecret.Location = new System.Drawing.Point(20, 186);
            _txtClientSecret.Size = new System.Drawing.Size(380, 32);
            _txtClientSecret.IsPassword = true;
            _txtClientSecret.Placeholder = "GOCSPX-...";

            // _lblStatus
            _lblStatus.AutoSize = true;
            _lblStatus.Location = new System.Drawing.Point(20, 228);
            _lblStatus.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblStatus.Text = "";

            // _btnSave
            _btnSave.Text = "Kaydet";
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Size = new System.Drawing.Size(100, 34);
            _btnSave.Location = new System.Drawing.Point(190, 260);
            _btnSave.Click += OnSaveClick;

            // _btnCancel
            _btnCancel.Text = "\u0130ptal";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.Size = new System.Drawing.Size(90, 34);
            _btnCancel.Location = new System.Drawing.Point(300, 260);
            _btnCancel.Click += OnCancelClick;

            // _btnRemove
            _btnRemove.Text = "Temizle";
            _btnRemove.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnRemove.Size = new System.Drawing.Size(100, 34);
            _btnRemove.Location = new System.Drawing.Point(20, 260);
            _btnRemove.ForeColor = Theme.ModernTheme.StatusError;
            _btnRemove.Click += OnRemoveClick;

            // Form
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(420, 310);
            Controls.Add(_lblTitle);
            Controls.Add(_lblInfo);
            Controls.Add(_lblClientId);
            Controls.Add(_txtClientId);
            Controls.Add(_lblClientSecret);
            Controls.Add(_txtClientSecret);
            Controls.Add(_lblStatus);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
            Controls.Add(_btnRemove);
            Font = Theme.ModernTheme.FontBody;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            BackColor = Theme.ModernTheme.BackgroundColor;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GoogleOAuthSettingsDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Google OAuth Ayarlar\u0131";

            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblInfo;
        private System.Windows.Forms.Label _lblClientId;
        private Theme.ModernTextBox _txtClientId;
        private System.Windows.Forms.Label _lblClientSecret;
        private Theme.ModernTextBox _txtClientSecret;
        private System.Windows.Forms.Label _lblStatus;
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;
        private Theme.ModernButton _btnRemove;
    }
}
