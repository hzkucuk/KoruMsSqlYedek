using System;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Engine.Cloud;
using KoruMsSqlYedek.Win.Theme;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// Google OAuth 2.0 Client ID / Client Secret giriş dialogu.
    /// Kullanıcı kendi Google Cloud Console credential'larını buradan yönetir.
    /// </summary>
    internal sealed partial class GoogleOAuthSettingsDialog : ModernFormBase
    {
        private readonly AppSettings _settings;
        private readonly IAppSettingsManager _settingsManager;

        public GoogleOAuthSettingsDialog(AppSettings settings, IAppSettingsManager settingsManager)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(settingsManager);

            _settings = settings;
            _settingsManager = settingsManager;

            InitializeComponent();
            ApplyLocalization();
            LoadCurrentValues();
        }

        private void ApplyLocalization()
        {
            _lblTitle.Text = Helpers.Res.Get("GoogleOAuth_Title");
            _lblInfo.Text = Helpers.Res.Get("GoogleOAuth_Info");
            _lblClientId.Text = Helpers.Res.Get("GoogleOAuth_ClientId");
            _lblClientSecret.Text = Helpers.Res.Get("GoogleOAuth_ClientSecret");
            _btnSave.Text = Helpers.Res.Get("GoogleOAuth_Save");
            _btnCancel.Text = Helpers.Res.Get("GoogleOAuth_Cancel");
            _btnRemove.Text = Helpers.Res.Get("GoogleOAuth_Remove");
            Text = Helpers.Res.Get("GoogleOAuth_Title");

            // ── Rich Tooltips ────────────────────────────────────────────
            _toolTip.SetToolTip(_txtClientId, Helpers.Res.Get("Tip_GoogleOAuth_ClientId"));
            _toolTip.SetToolTip(_txtClientSecret, Helpers.Res.Get("Tip_GoogleOAuth_ClientSecret"));
            _toolTip.SetToolTip(_btnSave, Helpers.Res.Get("Tip_GoogleOAuth_Save"));
            _toolTip.SetToolTip(_btnRemove, Helpers.Res.Get("Tip_GoogleOAuth_Remove"));
        }

        private void LoadCurrentValues()
        {
            if (_settings.HasCustomGoogleOAuth)
            {
                _txtClientId.Text = _settings.GoogleOAuthClientId ?? "";

                try
                {
                    _txtClientSecret.Text = PasswordProtector.Unprotect(_settings.GoogleOAuthClientSecret);
                }
                catch
                {
                    _txtClientSecret.Text = "";
                }

                _lblStatus.Text = "\u2714 \u00d6zel kimlik bilgileri tan\u0131ml\u0131";
                _lblStatus.ForeColor = ModernTheme.AccentPrimary;
                _btnRemove.Visible = true;
            }
            else
            {
                _lblStatus.Text = "\u2139 G\u00f6m\u00fcl\u00fc varsay\u0131lan de\u011ferler kullan\u0131l\u0131yor";
                _lblStatus.ForeColor = ModernTheme.TextSecondary;
                _btnRemove.Visible = false;
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            string clientId = _txtClientId.Text.Trim();
            string clientSecret = _txtClientSecret.Text.Trim();

            // Her ikisi de boşsa → embedded kullanılacak
            if (string.IsNullOrEmpty(clientId) && string.IsNullOrEmpty(clientSecret))
            {
                ClearCredentials();
                return;
            }

            // Birisi boş diğeri dolu → uyarı
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                ModernMessageBox.Show(
                    "Client ID ve Client Secret birlikte girilmelidir.\nHer ikisini de bo\u015f b\u0131rak\u0131rsan\u0131z varsay\u0131lan de\u011ferler kullan\u0131l\u0131r.",
                    "Uyar\u0131", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Basit format doğrulama
            if (!clientId.Contains(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
            {
                ModernMessageBox.Show(
                    "Client ID format\u0131 ge\u00e7ersiz.\n\u00d6rnek: xxxxxxxx.apps.googleusercontent.com",
                    "Uyar\u0131", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtClientId.Focus();
                return;
            }

            // Kaydet — Secret DPAPI ile şifrelenir
            _settings.GoogleOAuthClientId = clientId;
            _settings.GoogleOAuthClientSecret = PasswordProtector.Protect(clientSecret);
            _settingsManager.Save(_settings);

            // Hemen aktif et
            GoogleDriveAuthHelper.SetCustomCredentials(clientId, clientSecret);

            ModernMessageBox.Show(
                "Google OAuth kimlik bilgileri kaydedildi.\nYeni kimlik bilgileri bir sonraki hesap ba\u011flama i\u015fleminde kullan\u0131lacakt\u0131r.",
                "Ba\u015far\u0131l\u0131", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnRemoveClick(object sender, EventArgs e)
        {
            var result = ModernMessageBox.Show(
                "\u00d6zel kimlik bilgilerini kald\u0131rmak istiyor musunuz?\nG\u00f6m\u00fcl\u00fc varsay\u0131lan de\u011ferler kullan\u0131lacakt\u0131r.",
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            ClearCredentials();
        }

        private void ClearCredentials()
        {
            _settings.GoogleOAuthClientId = null;
            _settings.GoogleOAuthClientSecret = null;
            _settingsManager.Save(_settings);

            // Gömülü credential'lara dön
            GoogleDriveAuthHelper.SetCustomCredentials(null, null);

            ModernMessageBox.Show(
                "Özel kimlik bilgileri kaldırıldı.\nGömülü varsayılan değerler kullanılacaktır.",
                "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
