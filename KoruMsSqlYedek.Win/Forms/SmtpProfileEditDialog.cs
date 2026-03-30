using System;
using System.Threading;
using System.Windows.Forms;
using MailKit.Net.Smtp;
using MimeKit;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// SMTP profilini oluşturmak veya düzenlemek için diyalog.
    /// </summary>
    internal partial class SmtpProfileEditDialog : Theme.ModernFormBase
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SmtpProfileEditDialog>();

        private readonly SmtpProfile _profile;
        private readonly bool _isNew;

        /// <summary>Kaydedilen profil (DialogResult == OK ise geçerli).</summary>
        public SmtpProfile ResultProfile => _profile;

        /// <summary>Yeni profil oluşturma.</summary>
        public SmtpProfileEditDialog() : this(null) { }

        /// <summary>Mevcut profili düzenleme.</summary>
        public SmtpProfileEditDialog(SmtpProfile existing)
        {
            InitializeComponent();

            if (existing != null)
            {
                _profile = existing;
                _isNew = false;
                Text = "SMTP Profilini Düzenle";
            }
            else
            {
                _profile = new SmtpProfile();
                _isNew = true;
                Text = "Yeni SMTP Profili";
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadProfileToUi();
        }

        private void LoadProfileToUi()
        {
            _txtDisplayName.Text = _profile.DisplayName ?? "";
            _txtHost.Text = _profile.Host ?? "";
            _nudPort.Value = Math.Min(Math.Max(_profile.Port, 1), 65535);
            _chkUseSsl.Checked = _profile.UseSsl;
            _txtUsername.Text = _profile.Username ?? "";
            _txtPassword.Text = "";
            _txtSenderEmail.Text = _profile.SenderEmail ?? "";
            _txtSenderName.Text = string.IsNullOrWhiteSpace(_profile.SenderDisplayName)
                ? "Koru MsSql Yedek" : _profile.SenderDisplayName;
            _txtRecipients.Text = _profile.RecipientEmails ?? "";
        }

        private bool SaveUiToProfile()
        {
            if (string.IsNullOrWhiteSpace(_txtDisplayName.Text))
            {
                MessageBox.Show("Profil adı boş bırakılamaz.", "Doğrulama Hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDisplayName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_txtHost.Text))
            {
                MessageBox.Show("SMTP sunucu adresi boş bırakılamaz.", "Doğrulama Hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtHost.Focus();
                return false;
            }

            _profile.DisplayName = _txtDisplayName.Text.Trim();
            _profile.Host = _txtHost.Text.Trim();
            _profile.Port = (int)_nudPort.Value;
            _profile.UseSsl = _chkUseSsl.Checked;
            _profile.Username = _txtUsername.Text.Trim();
            _profile.SenderEmail = _txtSenderEmail.Text.Trim();
            _profile.SenderDisplayName = _txtSenderName.Text.Trim();
            _profile.RecipientEmails = _txtRecipients.Text.Trim();

            string rawPassword = _txtPassword.Text;
            if (!string.IsNullOrEmpty(rawPassword))
                _profile.Password = PasswordProtector.Protect(rawPassword);

            return true;
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (!SaveUiToProfile()) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnTestClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtHost.Text))
            {
                MessageBox.Show("SMTP sunucu adresi giriniz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtRecipients.Text))
            {
                MessageBox.Show("Test e-postası için en az bir alıcı adresi giriniz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnTest.Enabled = false;
            _btnTest.Text = "Gönderiliyor...";

            try
            {
                int port = (int)_nudPort.Value;
                var options = _chkUseSsl.Checked
                    ? MailKit.Security.SecureSocketOptions.StartTls
                    : MailKit.Security.SecureSocketOptions.None;

                using var client = new SmtpClient();
                client.Connect(_txtHost.Text.Trim(), port, options);

                string username = _txtUsername.Text.Trim();
                if (!string.IsNullOrEmpty(username))
                    client.Authenticate(username, _txtPassword.Text);

                string senderEmail = !string.IsNullOrWhiteSpace(_txtSenderEmail.Text)
                    ? _txtSenderEmail.Text.Trim() : username;
                string senderName = !string.IsNullOrWhiteSpace(_txtSenderName.Text)
                    ? _txtSenderName.Text.Trim() : "Koru MsSql Yedek";

                string firstRecipient = _txtRecipients.Text.Trim().Split(new[] { ';', ',' },
                    StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(MailboxAddress.Parse(firstRecipient));
                message.Subject = $"[Koru MsSql Yedek] SMTP Test — {DateTime.Now:yyyy-MM-dd HH:mm}";
                message.Body = new TextPart("plain")
                {
                    Text = $"Bu bir test e-postasıdır.\nProfil: {_txtDisplayName.Text}\nZaman: {DateTime.Now}"
                };

                client.Send(message);
                client.Disconnect(true);

                MessageBox.Show("Test e-postası başarıyla gönderildi.", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SMTP test e-postası gönderilemedi.");
                string safeMessage = ex.Message.Length > 200 ? ex.Message[..200] + "..." : ex.Message;
                MessageBox.Show($"SMTP bağlantısı kurulamadı:\n{safeMessage}", "SMTP Test Hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnTest.Enabled = true;
                _btnTest.Text = "✉ Test E-postası Gönder";
            }
        }
    }
}
