namespace KoruMsSqlYedek.Win.Forms
{
    partial class PasswordSetupDialog
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

            _lblTitle = new System.Windows.Forms.Label();
            _lblNewPassword = new System.Windows.Forms.Label();
            _txtNewPassword = new Theme.ModernTextBox();
            _lblConfirmPassword = new System.Windows.Forms.Label();
            _txtConfirmPassword = new Theme.ModernTextBox();
            _divider = new Theme.ModernDivider();
            _lblSecurityTitle = new System.Windows.Forms.Label();
            _lblQuestion = new System.Windows.Forms.Label();
            _txtSecurityQuestion = new Theme.ModernTextBox();
            _lblAnswer = new System.Windows.Forms.Label();
            _txtSecurityAnswer = new Theme.ModernTextBox();
            _btnSave = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();
            _btnRemovePassword = new Theme.ModernButton();

            SuspendLayout();

            // _lblTitle
            _lblTitle.AutoSize = true;
            _lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            _lblTitle.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblTitle.Location = new System.Drawing.Point(20, 16);
            _lblTitle.Text = "\U0001f511 Şifre Koruması Ayarları";

            // _lblNewPassword
            _lblNewPassword.AutoSize = true;
            _lblNewPassword.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblNewPassword.Location = new System.Drawing.Point(20, 54);
            _lblNewPassword.Text = "Yeni Şifre:";

            // _txtNewPassword
            _txtNewPassword.Location = new System.Drawing.Point(20, 76);
            _txtNewPassword.Size = new System.Drawing.Size(340, 32);
            _txtNewPassword.IsPassword = true;

            // _lblConfirmPassword
            _lblConfirmPassword.AutoSize = true;
            _lblConfirmPassword.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblConfirmPassword.Location = new System.Drawing.Point(20, 116);
            _lblConfirmPassword.Text = "Şifre Tekrar:";

            // _txtConfirmPassword
            _txtConfirmPassword.Location = new System.Drawing.Point(20, 138);
            _txtConfirmPassword.Size = new System.Drawing.Size(340, 32);
            _txtConfirmPassword.IsPassword = true;

            // _divider
            _divider.Location = new System.Drawing.Point(20, 182);
            _divider.Size = new System.Drawing.Size(340, 1);

            // _lblSecurityTitle
            _lblSecurityTitle.AutoSize = true;
            _lblSecurityTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            _lblSecurityTitle.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblSecurityTitle.Location = new System.Drawing.Point(20, 196);
            _lblSecurityTitle.Text = "Güvenlik Sorusu (Şifre Kurtarma)";

            // _lblQuestion
            _lblQuestion.AutoSize = true;
            _lblQuestion.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblQuestion.Location = new System.Drawing.Point(20, 226);
            _lblQuestion.Text = "Soru:";

            // _txtSecurityQuestion
            _txtSecurityQuestion.Location = new System.Drawing.Point(20, 248);
            _txtSecurityQuestion.Size = new System.Drawing.Size(340, 32);
            _txtSecurityQuestion.Placeholder = "Ör: İlk evcil hayvanınızın adı neydi?";

            // _lblAnswer
            _lblAnswer.AutoSize = true;
            _lblAnswer.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblAnswer.Location = new System.Drawing.Point(20, 288);
            _lblAnswer.Text = "Cevap:";

            // _txtSecurityAnswer
            _txtSecurityAnswer.Location = new System.Drawing.Point(20, 310);
            _txtSecurityAnswer.Size = new System.Drawing.Size(340, 32);
            _txtSecurityAnswer.Placeholder = "Cevabınız (büyük/küçük harf duyarsız)";

            // _btnSave
            _btnSave.Text = "Kaydet";
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Size = new System.Drawing.Size(110, 36);
            _btnSave.Location = new System.Drawing.Point(140, 360);
            _btnSave.Click += OnSaveClick;

            // _btnCancel
            _btnCancel.Text = "İptal";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.Size = new System.Drawing.Size(100, 36);
            _btnCancel.Location = new System.Drawing.Point(260, 360);
            _btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            // _btnRemovePassword
            _btnRemovePassword.Text = "Şifreyi Kaldır";
            _btnRemovePassword.ButtonStyle = Theme.ModernButtonStyle.Danger;
            _btnRemovePassword.Size = new System.Drawing.Size(110, 36);
            _btnRemovePassword.Location = new System.Drawing.Point(20, 360);
            _btnRemovePassword.Click += OnRemovePasswordClick;

            // Form
            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
            ClientSize = new System.Drawing.Size(380, 412);
            Controls.Add(_lblTitle);
            Controls.Add(_lblNewPassword);
            Controls.Add(_txtNewPassword);
            Controls.Add(_lblConfirmPassword);
            Controls.Add(_txtConfirmPassword);
            Controls.Add(_divider);
            Controls.Add(_lblSecurityTitle);
            Controls.Add(_lblQuestion);
            Controls.Add(_txtSecurityQuestion);
            Controls.Add(_lblAnswer);
            Controls.Add(_txtSecurityAnswer);
            Controls.Add(_btnRemovePassword);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Şifre Koruması";

            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblNewPassword;
        private Theme.ModernTextBox _txtNewPassword;
        private System.Windows.Forms.Label _lblConfirmPassword;
        private Theme.ModernTextBox _txtConfirmPassword;
        private Theme.ModernDivider _divider;
        private System.Windows.Forms.Label _lblSecurityTitle;
        private System.Windows.Forms.Label _lblQuestion;
        private Theme.ModernTextBox _txtSecurityQuestion;
        private System.Windows.Forms.Label _lblAnswer;
        private Theme.ModernTextBox _txtSecurityAnswer;
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;
        private Theme.ModernButton _btnRemovePassword;
        private System.Windows.Forms.ToolTip _toolTip;
    }
}
