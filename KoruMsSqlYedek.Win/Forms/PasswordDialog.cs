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
    /// Global (master) şifre ve/veya plan bazlı şifreyi kabul eder.
    /// "Şifremi Unuttum" bağlantısı güvenlik sorusu akışını başlatır.
    /// </summary>
    internal sealed partial class PasswordDialog : ModernFormBase
    {
        private readonly AppSettings _settings;
        private readonly IAppSettingsManager _settingsManager;
        private readonly string _planPasswordHash;
        private readonly string _planRecoveryHash;

        /// <summary>
        /// Güvenlik sorusu ile kurtarma yapıldığında plan şifresinin de sıfırlanması gerektiğini belirtir.
        /// Çağıran taraf bu flag'i kontrol ederek plan.PasswordHash'i temizlemelidir.
        /// </summary>
        public bool PlanPasswordReset { get; private set; }

        /// <param name="settings">Global uygulama ayarları (master şifre).</param>
        /// <param name="settingsManager">Ayar kaydetme servisi.</param>
        /// <param name="planPasswordHash">Plan bazlı şifre hash'i. Null ise yalnızca master geçerli.</param>
        /// <param name="planRecoveryHash">Plan bazlı kurtarma şifresi hash'i.</param>
        public PasswordDialog(AppSettings settings, IAppSettingsManager settingsManager, string planPasswordHash = null, string planRecoveryHash = null)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(settingsManager);

            _settings = settings;
            _settingsManager = settingsManager;
            _planPasswordHash = planPasswordHash;
            _planRecoveryHash = planRecoveryHash;

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

            string input = _txtPassword.Text;

            // Plan şifresi tanımlıysa plan şifresini veya kurtarma şifresini kabul et (izolasyon).
            // Plan şifresi yoksa global (master) şifreyi kontrol et.
            bool accepted;
            if (!string.IsNullOrEmpty(_planPasswordHash))
            {
                accepted = PlanPasswordHelper.VerifyPassword(input, _planPasswordHash);

                // Plan şifresi eşleşmediyse kurtarma şifresini dene
                if (!accepted && !string.IsNullOrEmpty(_planRecoveryHash))
                {
                    accepted = PlanPasswordHelper.VerifyPassword(input, _planRecoveryHash);
                }
            }
            else
            {
                accepted = !string.IsNullOrEmpty(_settings.PasswordHash) &&
                           PlanPasswordHelper.VerifyPassword(input, _settings.PasswordHash);
            }

            if (accepted)
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
                string configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "KoruMsSqlYedek", "Config", "appsettings.json");

                var result = ModernMessageBox.Show(
                    "Güvenlik sorusu tanımlanmamış. Şifre sıfırlanamaz.\n\n" +
                    "Son çare olarak yapılandırma dosyasından şifre alanlarını\n" +
                    "manuel olarak silebilirsiniz:\n\n" +
                    configPath + "\n\n" +
                    "Dosya yolu panoya kopyalansın mı?",
                    "Kurtarma Yok",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    Clipboard.SetText(configPath);
                    ModernMessageBox.Show(
                        "Dosya yolu panoya kopyalandı.\n\n" +
                        "Dosyayı bir metin editöründe açıp şu satırları silin:\n" +
                        "  • \"passwordHash\": \"...\"\n" +
                        "  • \"securityQuestion\": \"...\"\n" +
                        "  • \"securityAnswerHash\": \"...\"\n\n" +
                        "Ardından uygulamayı yeniden başlatın.",
                        "Talimatlar",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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

                    // Plan şifresi varsa onu da sıfırla
                    if (!string.IsNullOrEmpty(_planPasswordHash))
                    {
                        PlanPasswordReset = true;
                    }

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
