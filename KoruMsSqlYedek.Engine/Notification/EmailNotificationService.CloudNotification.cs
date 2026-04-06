using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Notification
{
    partial class EmailNotificationService
    {
        /// <inheritdoc />
        public async Task NotifyCloudUploadFailureAsync(
            string planName,
            List<CloudUploadResult> failedResults,
            string fileName,
            NotificationConfig config,
            CancellationToken cancellationToken)
        {
            if (config == null || !config.EmailEnabled || !config.OnFailure)
                return;

            SmtpProfile profile = ResolveProfile(config);
            if (profile == null || string.IsNullOrWhiteSpace(profile.Host))
            {
                Log.Warning("Bulut upload başarısızlık bildirimi atlandı: SMTP profili bulunamadı.");
                return;
            }

            try
            {
                string recipients = !string.IsNullOrWhiteSpace(config.EmailTo)
                    ? config.EmailTo
                    : profile.RecipientEmails;

                if (string.IsNullOrWhiteSpace(recipients))
                {
                    Log.Warning("Bulut upload başarısızlık bildirimi atlandı: Alıcı adresi tanımlanmamış.");
                    return;
                }

                string senderEmail = !string.IsNullOrWhiteSpace(profile.SenderEmail)
                    ? profile.SenderEmail
                    : profile.Username;
                string senderName = profile.SenderDisplayName ?? "Koru MsSql Yedek";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));

                foreach (string addr in recipients.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = addr.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        message.To.Add(MailboxAddress.Parse(trimmed));
                }

                message.Subject = $"[Koru MsSql Yedek] Bulut Yükleme Başarısız ✗ — {planName}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = BuildCloudFailureEmailBody(planName, failedResults, fileName)
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        profile.Host,
                        profile.Port,
                        profile.UseSsl ? MailKit.Security.SecureSocketOptions.StartTls
                                       : MailKit.Security.SecureSocketOptions.None,
                        cancellationToken);

                    if (!string.IsNullOrEmpty(profile.Username))
                    {
                        string password = PasswordProtector.Unprotect(profile.Password);
                        await client.AuthenticateAsync(profile.Username, password, cancellationToken);
                    }

                    await client.SendAsync(message, cancellationToken);
                    await client.DisconnectAsync(true, cancellationToken);
                }

                Log.Information(
                    "Bulut upload başarısızlık bildirimi gönderildi: {PlanName} — {File} → {Recipients}",
                    planName, fileName, recipients);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulut upload başarısızlık bildirimi gönderilemedi: {PlanName}", planName);
            }
        }

        private string BuildCloudFailureEmailBody(
            string planName, List<CloudUploadResult> failedResults, string fileName)
        {
            var tmpl = new EmailTemplateBuilder();
            tmpl.WriteHeader("Koru MsSql Yedek — Bulut Yükleme Başarısız", planName);
            tmpl.WriteStatusBadge("Bulut Yükleme Başarısız", false);

            tmpl.WriteSectionTitle("Başarısız Yükleme Detayları");
            tmpl.BeginSummaryTable();
            tmpl.WriteTableRow("Plan", EmailTemplateBuilder.Encode(planName));
            tmpl.WriteTableRow("Dosya", EmailTemplateBuilder.Encode(fileName));
            tmpl.WriteTableRow("Tarih", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            tmpl.WriteTableRow("Başarısız Hedef Sayısı", failedResults?.Count.ToString() ?? "0",
                EmailTemplateBuilder.GetFailureColor());
            tmpl.EndTable();

            if (failedResults is { Count: > 0 })
            {
                tmpl.WriteSectionTitle("Provider Detayları");
                tmpl.BeginDetailTable("Hedef", "Tür", "Deneme", "Hata Mesajı");

                int idx = 0;
                foreach (var result in failedResults)
                {
                    string color = EmailTemplateBuilder.GetStatusColor(false);
                    string providerType = result.ProviderType.ToString();

                    tmpl.WriteDetailRow(idx++,
                        (EmailTemplateBuilder.Encode(result.DisplayName), null),
                        (providerType, null),
                        ($"{result.RetryCount + 1} deneme", null),
                        (SanitizeForEmail(result.ErrorMessage), color));
                }

                tmpl.EndDetailTable();
            }

            tmpl.WriteErrorBlock(
                "Tüm bulut yükleme denemeleri başarısız oldu. " +
                "Lütfen bulut sağlayıcı ayarlarınızı (kimlik bilgileri, kota, erişim izinleri) kontrol edin.");

            return tmpl.Build();
        }
    }
}
