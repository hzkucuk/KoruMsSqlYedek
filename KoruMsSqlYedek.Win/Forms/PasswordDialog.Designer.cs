namespace KoruMsSqlYedek.Win.Forms
{
    partial class PasswordDialog
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
            _lblTitle = new System.Windows.Forms.Label();
            _lblPrompt = new System.Windows.Forms.Label();
            _txtPassword = new Theme.ModernTextBox();
            _btnOk = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();
            _lnkForgot = new System.Windows.Forms.LinkLabel();
            _pnlRecovery = new System.Windows.Forms.Panel();
            _lblSecurityQuestion = new System.Windows.Forms.Label();
            _txtSecurityAnswer = new Theme.ModernTextBox();
            _btnVerifyAnswer = new Theme.ModernButton();
            _divider = new Theme.ModernDivider();

            SuspendLayout();

            // _lblTitle
            _lblTitle.AutoSize = true;
            _lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            _lblTitle.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblTitle.Location = new System.Drawing.Point(20, 18);
            _lblTitle.Text = "\U0001f512 Şifre Gerekli";

            // _lblPrompt
            _lblPrompt.AutoSize = true;
            _lblPrompt.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblPrompt.Location = new System.Drawing.Point(20, 52);
            _lblPrompt.Text = "Görev düzenleme için şifrenizi girin:";

            // _txtPassword
            _txtPassword.Location = new System.Drawing.Point(20, 80);
            _txtPassword.Size = new System.Drawing.Size(320, 32);
            _txtPassword.IsPassword = true;
            _txtPassword.Placeholder = "Şifre";

            // _btnOk
            _btnOk.Text = "Giriş";
            _btnOk.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnOk.Size = new System.Drawing.Size(100, 36);
            _btnOk.Location = new System.Drawing.Point(135, 124);
            _btnOk.Click += OnOkClick;

            // _btnCancel
            _btnCancel.Text = "İptal";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.Size = new System.Drawing.Size(100, 36);
            _btnCancel.Location = new System.Drawing.Point(240, 124);
            _btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            // _lnkForgot
            _lnkForgot.AutoSize = true;
            _lnkForgot.Location = new System.Drawing.Point(20, 130);
            _lnkForgot.Text = "Şifremi Unuttum";
            _lnkForgot.LinkColor = Theme.ModernTheme.AccentPrimary;
            _lnkForgot.ActiveLinkColor = Theme.ModernTheme.AccentPrimaryHover;
            _lnkForgot.VisitedLinkColor = Theme.ModernTheme.AccentPrimary;
            _lnkForgot.Click += OnForgotPasswordClick;

            // _divider
            _divider.Location = new System.Drawing.Point(20, 172);
            _divider.Size = new System.Drawing.Size(320, 1);

            // _pnlRecovery — gizli, şifremi unuttum'a tıklayınca görünür
            _pnlRecovery.Location = new System.Drawing.Point(0, 180);
            _pnlRecovery.Size = new System.Drawing.Size(360, 130);
            _pnlRecovery.Visible = false;
            _pnlRecovery.BackColor = Theme.ModernTheme.BackgroundColor;

            // _lblSecurityQuestion
            _lblSecurityQuestion.AutoSize = true;
            _lblSecurityQuestion.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblSecurityQuestion.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            _lblSecurityQuestion.Location = new System.Drawing.Point(20, 8);
            _lblSecurityQuestion.MaximumSize = new System.Drawing.Size(320, 0);
            _lblSecurityQuestion.Text = "(güvenlik sorusu)";

            // _txtSecurityAnswer
            _txtSecurityAnswer.Location = new System.Drawing.Point(20, 40);
            _txtSecurityAnswer.Size = new System.Drawing.Size(320, 32);
            _txtSecurityAnswer.Placeholder = "Cevabınız";

            // _btnVerifyAnswer
            _btnVerifyAnswer.Text = "Doğrula";
            _btnVerifyAnswer.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnVerifyAnswer.Size = new System.Drawing.Size(120, 36);
            _btnVerifyAnswer.Location = new System.Drawing.Point(220, 82);
            _btnVerifyAnswer.Click += OnVerifyAnswerClick;

            _pnlRecovery.Controls.Add(_lblSecurityQuestion);
            _pnlRecovery.Controls.Add(_txtSecurityAnswer);
            _pnlRecovery.Controls.Add(_btnVerifyAnswer);

            // Form
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
            ClientSize = new System.Drawing.Size(360, 175);
            Controls.Add(_lblTitle);
            Controls.Add(_lblPrompt);
            Controls.Add(_txtPassword);
            Controls.Add(_lnkForgot);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);
            Controls.Add(_divider);
            Controls.Add(_pnlRecovery);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Şifre Doğrulama";

            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblPrompt;
        private Theme.ModernTextBox _txtPassword;
        private Theme.ModernButton _btnOk;
        private Theme.ModernButton _btnCancel;
        private System.Windows.Forms.LinkLabel _lnkForgot;
        private Theme.ModernDivider _divider;
        private System.Windows.Forms.Panel _pnlRecovery;
        private System.Windows.Forms.Label _lblSecurityQuestion;
        private Theme.ModernTextBox _txtSecurityAnswer;
        private Theme.ModernButton _btnVerifyAnswer;
    }
}
