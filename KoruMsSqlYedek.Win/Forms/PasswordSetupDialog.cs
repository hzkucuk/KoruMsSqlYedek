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
    /// Şifre oluşturma/değiştirme dialogu.
    /// Yeni şifre + onay + güvenlik sorusu + cevap alanları.
    /// </summary>
    internal sealed partial class PasswordSetupDialog : ModernFormBase
    {
        private readonly AppSettings _settings;
        private readonly IAppSettingsManager _settingsManager;

        public PasswordSetupDialog(AppSettings settings, IAppSettingsManager settingsManager)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(settingsManager);

            _settings = settings;
            _settingsManager = settingsManager;

            InitializeComponent();

            // Mevcut güvenlik sorusu varsa doldur
            if (!string.IsNullOrEmpty(_settings.SecurityQuestion))
                _txtSecurityQuestion.Text = _settings.SecurityQuestion;
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            string password = _txtNewPassword.Text;
            string confirm = _txtConfirmPassword.Text;
            string question = _txtSecurityQuestion.Text.Trim();
            string answer = _txtSecurityAnswer.Text.Trim();

            // Doğrulama
            if (string.IsNullOrWhiteSpace(password))
            {
                ModernMessageBox.Show("Şifre boş bırakılamaz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNewPassword.Focus();
                return;
            }

            if (password.Length < 4)
            {
                ModernMessageBox.Show("Şifre en az 4 karakter olmalıdır.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNewPassword.Focus();
                return;
            }

            if (password != confirm)
            {
                ModernMessageBox.Show("Şifreler eşleşmiyor.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtConfirmPassword.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                ModernMessageBox.Show("Güvenlik sorusu boş bırakılamaz.\nŞifre kurtarma için gereklidir.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSecurityQuestion.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(answer))
            {
                ModernMessageBox.Show("Güvenlik sorusu cevabı boş bırakılamaz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSecurityAnswer.Focus();
                return;
            }

            // Kaydet
            _settings.PasswordHash = PlanPasswordHelper.HashPassword(password);
            _settings.SecurityQuestion = question;
            _settings.SecurityAnswerHash = PlanPasswordHelper.HashPassword(answer.ToLowerInvariant());
            _settings.PasswordEnabled = true;
            _settingsManager.Save(_settings);

            ModernMessageBox.Show(
                "Şifre koruması etkinleştirildi.\nGörev ekleme, düzenleme ve silme işlemleri artık şifre ile korunuyor.",
                "Başarılı",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnRemovePasswordClick(object sender, EventArgs e)
        {
            if (!_settings.HasPassword)
            {
                ModernMessageBox.Show("Şifre koruması zaten tanımlı değil.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = ModernMessageBox.Show(
                "Şifre korumasını tamamen kaldırmak istediğinize emin misiniz?\n(Güvenlik sorusu dahil tüm veriler silinecek)",
                "Şifre Korumasını Kaldır",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _settings.PasswordHash = null;
                _settings.SecurityQuestion = null;
                _settings.SecurityAnswerHash = null;
                _settings.PasswordEnabled = true;
                _settingsManager.Save(_settings);

                ModernMessageBox.Show("Şifre koruması kaldırıldı.", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
