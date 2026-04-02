using System;
using System.Drawing;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Theme;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// Şifre doğrulama dialogu — görev düzenleme/silme öncesi kullanıcıdan şifre ister.
    /// "Şifremi Unuttum" bağlantısı güvenlik sorusu akışını başlatır.
    /// </summary>
    internal sealed partial class PasswordDialog : ModernFormBase
    {
        private readonly AppSettings _settings;
        private readonly IAppSettingsManager _settingsManager;

        public PasswordDialog(AppSettings settings, IAppSettingsManager settingsManager)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(settingsManager);

            _settings = settings;
            _settingsManager = settingsManager;

            InitializeComponent();
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                ModernMessageBox.Show("Şifre boş bırakılamaz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPassword.Focus();
                return;
            }

            if (PlanPasswordHelper.VerifyPassword(_txtPassword.Text, _settings.PasswordHash))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ModernMessageBox.Show("Şifre yanlış. Lütfen tekrar deneyin.", "Hatalı Şifre",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _txtPassword.Text = string.Empty;
                _txtPassword.Focus();
            }
        }

        private void OnForgotPasswordClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_settings.SecurityQuestion) ||
                string.IsNullOrEmpty(_settings.SecurityAnswerHash))
            {
                ModernMessageBox.Show(
                    "Güvenlik sorusu tanımlanmamış. Şifre sıfırlanamaz.\n" +
                    "Yardım için uygulama yapılandırma dosyasını manuel olarak düzenleyebilirsiniz.",
                    "Kurtarma Yok",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Güvenlik sorusu panelini göster
            _pnlRecovery.Visible = true;
            _lblSecurityQuestion.Text = _settings.SecurityQuestion;
            _txtSecurityAnswer.Text = string.Empty;
            _txtSecurityAnswer.Focus();

            // Form boyutunu ayarla
            ClientSize = new Size(ClientSize.Width, _pnlRecovery.Bottom + 20);
        }

        private void OnVerifyAnswerClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtSecurityAnswer.Text))
            {
                ModernMessageBox.Show("Cevap boş bırakılamaz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSecurityAnswer.Focus();
                return;
            }

            if (PlanPasswordHelper.VerifyPassword(
                    _txtSecurityAnswer.Text.Trim().ToLowerInvariant(),
                    _settings.SecurityAnswerHash))
            {
                // Doğru cevap — şifreyi sıfırla
                var result = ModernMessageBox.Show(
                    "Güvenlik sorusu doğrulandı. Şifre koruması kaldırılsın mı?\n" +
                    "Ayarlar bölümünden yeni şifre belirleyebilirsiniz.",
                    "Şifre Sıfırlama",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _settings.PasswordHash = null;
                    _settings.SecurityQuestion = null;
                    _settings.SecurityAnswerHash = null;
                    _settingsManager.Save(_settings);

                    ModernMessageBox.Show(
                        "Şifre koruması kaldırıldı. Görevlere erişim serbest.",
                        "Başarılı",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            else
            {
                ModernMessageBox.Show("Cevap yanlış. Lütfen tekrar deneyin.", "Hatalı Cevap",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _txtSecurityAnswer.Text = string.Empty;
                _txtSecurityAnswer.Focus();
            }
        }
    }
}
