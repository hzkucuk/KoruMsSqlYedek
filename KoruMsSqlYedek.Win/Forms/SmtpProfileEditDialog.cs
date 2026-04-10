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

        /// <summary>Kaydedilen profil (DialogResult == OK ise geçerli).</summary>
        public SmtpProfile ResultProfile => _profile;

        /// <summary>Yeni profil oluşturma.</summary>
        public SmtpProfileEditDialog() : this(null) { }

        /// <summary>Mevcut profili düzenleme.</summary>
        public SmtpProfileEditDialog(SmtpProfile existing)
        {
            InitializeComponent();
            ApplyLocalization();

            if (existing != null)
            {
                _profile = existing;
                Text = Helpers.Res.Get("Smtp_TitleEdit");
            }
            else
            {
                _profile = new SmtpProfile();
                Text = Helpers.Res.Get("Smtp_TitleNew");
            }
        }

        private void ApplyLocalization()
        {
            _lblDisplayName.Text = Helpers.Res.Get("Smtp_DisplayName");
            _lblHost.Text = Helpers.Res.Get("Smtp_Host");
            _chkUseSsl.Text = Helpers.Res.Get("Smtp_UseSsl");
            _lblUsername.Text = Helpers.Res.Get("Smtp_Username");
            _lblPassword.Text = Helpers.Res.Get("Smtp_Password");
            _lblSenderEmail.Text = Helpers.Res.Get("Smtp_SenderEmail");
            _lblSenderName.Text = Helpers.Res.Get("Smtp_SenderName");
            _lblRecipients.Text = Helpers.Res.Get("Smtp_Recipients");
            _btnCancel.Text = Helpers.Res.Get("Smtp_Cancel");
            _btnSave.Text = Helpers.Res.Get("Smtp_Save");
            _btnTest.Text = Helpers.Res.Get("Smtp_Test");

            // ── Rich Tooltips ────────────────────────────────────────────
            _toolTip.SetToolTip(_txtDisplayName, Helpers.Res.Get("Tip_Smtp_DisplayName"));
            _toolTip.SetToolTip(_txtHost, Helpers.Res.Get("Tip_Smtp_Host"));
            _toolTip.SetToolTip(_nudPort, Helpers.Res.Get("Tip_Smtp_Port"));
            _toolTip.SetToolTip(_chkUseSsl, Helpers.Res.Get("Tip_Smtp_UseSsl"));
            _toolTip.SetToolTip(_txtUsername, Helpers.Res.Get("Tip_Smtp_Username"));
            _toolTip.SetToolTip(_txtPassword, Helpers.Res.Get("Tip_Smtp_Password"));
            _toolTip.SetToolTip(_txtSenderEmail, Helpers.Res.Get("Tip_Smtp_SenderEmail"));
            _toolTip.SetToolTip(_txtSenderName, Helpers.Res.Get("Tip_Smtp_SenderName"));
            _toolTip.SetToolTip(_txtRecipients, Helpers.Res.Get("Tip_Smtp_Recipients"));
            _toolTip.SetToolTip(_btnTest, Helpers.Res.Get("Tip_Smtp_Test"));
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
                Theme.ModernMessageBox.Show("Profil adı boş bırakılamaz.", "Doğrulama Hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDisplayName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_txtHost.Text))
            {
                Theme.ModernMessageBox.Show("SMTP sunucu adresi boş bırakılamaz.", "Doğrulama Hatası",
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
                Theme.ModernMessageBox.Show(Helpers.Res.Get("Smtp_HostRequired"), Helpers.Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtRecipients.Text))
            {
                Theme.ModernMessageBox.Show(Helpers.Res.Get("Smtp_RecipientRequired"), Helpers.Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnTest.Enabled = false;
            _btnTest.Text = Helpers.Res.Get("Smtp_Sending");

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
                    ? _txtSenderName.Text.Trim() : "Koru MsSql Yedek"; // brand name — intentionally not localized

                string firstRecipient = _txtRecipients.Text.Trim().Split(new[] { ';', ',' },
                    StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(MailboxAddress.Parse(firstRecipient));
                message.Subject = $"[Koru MsSql Yedek] SMTP Test — {DateTime.Now:yyyy-MM-dd HH:mm}";
                message.Body = new TextPart("plain")
                {
                    Text = Helpers.Res.Format("Smtp_TestBody", _txtDisplayName.Text, DateTime.Now)
                };

                client.Send(message);
                client.Disconnect(true);

                Theme.ModernMessageBox.Show(Helpers.Res.Get("Smtp_TestSuccess"), Helpers.Res.Get("Success"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SMTP test e-postası gönderilemedi.");
                string safeMessage = ex.Message.Length > 200 ? ex.Message[..200] + "..." : ex.Message;
                Theme.ModernMessageBox.Show(Helpers.Res.Format("Smtp_TestFailed", safeMessage), Helpers.Res.Get("Smtp_TestErrorTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnTest.Enabled = true;
                _btnTest.Text = Helpers.Res.Get("Smtp_Test");
            }
        }
    }
}
