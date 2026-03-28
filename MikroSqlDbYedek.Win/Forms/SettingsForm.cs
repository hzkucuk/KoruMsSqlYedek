using System;
using System.Windows.Forms;
using MikroSqlDbYedek.Core.Helpers;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Win.Helpers;
using Serilog;

namespace MikroSqlDbYedek.Win.Forms
{
    /// <summary>
    /// Uygulama genelinde geçerli ayarları düzenleyen form.
    /// Dil, başlangıç, varsayılan dizin, log saklama, SMTP yapılandırması.
    /// </summary>
    public partial class SettingsForm : Form
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SettingsForm>();
        private readonly IAppSettingsManager _settingsManager;
        private AppSettings _settings;

        public SettingsForm(IAppSettingsManager settingsManager)
        {
            if (settingsManager == null) throw new ArgumentNullException(nameof(settingsManager));

            InitializeComponent();
            _settingsManager = settingsManager;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadSettings();
        }

        #region Load / Save

        private void LoadSettings()
        {
            try
            {
                _settings = _settingsManager.Load();
                SettingsToControls(_settings);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayarlar yüklenemedi.");
                MessageBox.Show(Res.Format("Settings_LoadError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SettingsToControls(AppSettings s)
        {
            // Genel
            _cmbLanguage.SelectedIndex = s.Language == "en-US" ? 1 : 0;
            _chkStartWithWindows.Checked = s.StartWithWindows;
            _chkMinimizeToTray.Checked = s.MinimizeToTray;
            _txtDefaultBackupPath.Text = s.DefaultBackupPath;
            _nudLogRetention.Value = Math.Min(Math.Max(s.LogRetentionDays, 1), 365);
            _nudHistoryRetention.Value = Math.Min(Math.Max(s.HistoryRetentionDays, 1), 365);

            // SMTP
            _txtSmtpHost.Text = s.Smtp.Host;
            _nudSmtpPort.Value = Math.Min(Math.Max(s.Smtp.Port, 1), 65535);
            _chkSmtpSsl.Checked = s.Smtp.UseSsl;
            _txtSmtpUsername.Text = s.Smtp.Username;
            _txtSmtpSenderEmail.Text = s.Smtp.SenderEmail;
            _txtSmtpSenderName.Text = s.Smtp.SenderDisplayName;
            _txtSmtpRecipients.Text = s.Smtp.RecipientEmails;

            // Şifreyi decode et (gösterim için)
            if (!string.IsNullOrEmpty(s.Smtp.Password))
            {
                try
                {
                    _txtSmtpPassword.Text = PasswordProtector.Unprotect(s.Smtp.Password);
                }
                catch
                {
                    _txtSmtpPassword.Text = string.Empty;
                }
            }
        }

        private AppSettings ControlsToSettings()
        {
            var s = _settings ?? new AppSettings();

            // Genel
            s.Language = _cmbLanguage.SelectedIndex == 1 ? "en-US" : "tr-TR";
            s.StartWithWindows = _chkStartWithWindows.Checked;
            s.MinimizeToTray = _chkMinimizeToTray.Checked;
            s.DefaultBackupPath = _txtDefaultBackupPath.Text.Trim();
            s.LogRetentionDays = (int)_nudLogRetention.Value;
            s.HistoryRetentionDays = (int)_nudHistoryRetention.Value;

            // SMTP
            s.Smtp.Host = _txtSmtpHost.Text.Trim();
            s.Smtp.Port = (int)_nudSmtpPort.Value;
            s.Smtp.UseSsl = _chkSmtpSsl.Checked;
            s.Smtp.Username = _txtSmtpUsername.Text.Trim();
            s.Smtp.SenderEmail = _txtSmtpSenderEmail.Text.Trim();
            s.Smtp.SenderDisplayName = _txtSmtpSenderName.Text.Trim();
            s.Smtp.RecipientEmails = _txtSmtpRecipients.Text.Trim();

            // Şifreyi DPAPI ile encode et
            string rawPassword = _txtSmtpPassword.Text;
            s.Smtp.Password = !string.IsNullOrEmpty(rawPassword)
                ? PasswordProtector.Protect(rawPassword)
                : null;

            return s;
        }

        #endregion

        #region Validation

        private bool ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_txtDefaultBackupPath.Text))
            {
                ShowValidationError(Res.Get("Settings_BackupPathRequired"), _txtDefaultBackupPath);
                return false;
            }

            // SMTP alanları doldurulmuşsa doğrula
            if (!string.IsNullOrWhiteSpace(_txtSmtpHost.Text))
            {
                if (string.IsNullOrWhiteSpace(_txtSmtpSenderEmail.Text))
                {
                    ShowValidationError(Res.Get("Settings_SmtpSenderRequired"), _txtSmtpSenderEmail);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(_txtSmtpRecipients.Text))
                {
                    ShowValidationError(Res.Get("Settings_SmtpRecipientRequired"), _txtSmtpRecipients);
                    return false;
                }
            }

            return true;
        }

        private void ShowValidationError(string message, Control focusControl)
        {
            MessageBox.Show(message, Res.Get("ValidationError"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            focusControl?.Focus();
        }

        #endregion

        #region Events

        private void OnBrowseBackupPath(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Res.Get("Settings_BrowsePath");
                fbd.SelectedPath = _txtDefaultBackupPath.Text;

                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    _txtDefaultBackupPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (!ValidateSettings())
                return;

            try
            {
                var settings = ControlsToSettings();
                _settingsManager.Save(settings);

                Log.Information("Ayarlar kaydedildi.");
                MessageBox.Show(Res.Get("Settings_SavedMessage"),
                    Res.Get("Settings_SavedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ayarlar kaydedilemedi.");
                MessageBox.Show(Res.Format("Settings_SaveError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnSmtpTestClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtSmtpHost.Text))
            {
                MessageBox.Show(Res.Get("Settings_SmtpServerRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtSmtpRecipients.Text))
            {
                MessageBox.Show(Res.Get("Settings_SmtpRecipientTestRequired"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;

                // Basit SMTP bağlantı testi
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    int port = (int)_nudSmtpPort.Value;
                    var options = _chkSmtpSsl.Checked
                        ? MailKit.Security.SecureSocketOptions.StartTls
                        : MailKit.Security.SecureSocketOptions.None;

                    client.Connect(_txtSmtpHost.Text.Trim(), port, options);

                    string username = _txtSmtpUsername.Text.Trim();
                    if (!string.IsNullOrEmpty(username))
                    {
                        client.Authenticate(username, _txtSmtpPassword.Text);
                    }

                    // Test mesajı gönder
                    string senderEmail = !string.IsNullOrWhiteSpace(_txtSmtpSenderEmail.Text)
                        ? _txtSmtpSenderEmail.Text.Trim()
                        : username;
                    string senderName = !string.IsNullOrWhiteSpace(_txtSmtpSenderName.Text)
                        ? _txtSmtpSenderName.Text.Trim()
                        : "MikroSqlDbYedek";

                    var message = new MimeKit.MimeMessage();
                    message.From.Add(new MimeKit.MailboxAddress(senderName, senderEmail));
                    message.To.Add(MimeKit.MailboxAddress.Parse(_txtSmtpRecipients.Text.Trim().Split(';')[0]));
                    message.Subject = Res.Format("Settings_SmtpTestSubject", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    message.Body = new MimeKit.TextPart("plain")
                    {
                        Text = Res.Get("Settings_SmtpTestBody")
                    };

                    client.Send(message);
                    client.Disconnect(true);
                }

                MessageBox.Show(Res.Get("Settings_SmtpTestSuccess"), Res.Get("Success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SMTP test e-postası gönderilemedi.");
                MessageBox.Show(Res.Format("Settings_SmtpTestError", ex.Message), Res.Get("Settings_SmtpTestErrorTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        #endregion
    }
}
