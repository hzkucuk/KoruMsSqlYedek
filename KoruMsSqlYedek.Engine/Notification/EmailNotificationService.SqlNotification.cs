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
        public async Task NotifyAsync(
            BackupResult result,
            NotificationConfig config,
            CancellationToken cancellationToken)
        {
            if (config == null || !config.EmailEnabled)
                return;

            bool shouldNotify = (result.Status == BackupResultStatus.Success && config.OnSuccess) ||
                                (result.Status != BackupResultStatus.Success && config.OnFailure);

            if (!shouldNotify)
                return;

            // Profil çözümleme: önce SmtpProfileId, yoksa eski per-plan alanlardan oluşturulan geçici profil
            SmtpProfile profile = ResolveProfile(config);
            if (profile == null || string.IsNullOrWhiteSpace(profile.Host))
            {
                Log.Warning("E-posta bildirimi atlandı: SMTP profili bulunamadı veya sunucu adresi boş.");
                return;
            }

            try
            {
                string recipients = !string.IsNullOrWhiteSpace(config.EmailTo)
                    ? config.EmailTo
                    : profile.RecipientEmails;

                if (string.IsNullOrWhiteSpace(recipients))
                {
                    Log.Warning("E-posta bildirimi atlandı: Alıcı adresi tanımlanmamış.");
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

                bool isSuccess = result.Status == BackupResultStatus.Success;
                string statusText = isSuccess ? "Başarılı ✓" : "Başarısız ✗";

                message.Subject = $"[Koru MsSql Yedek] {result.DatabaseName} — Yedekleme {statusText}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = BuildEmailBody(result, isSuccess)
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
                    "Bildirim e-postası gönderildi: {Database} → {Recipients} (Profil: {Profile})",
                    result.DatabaseName, recipients, profile.DisplayName);
            }
            catch (Exception ex)
            {
                // Bildirim başarısızlığı yedek başarısını ETKİLEMEZ
                Log.Error(ex, "Bildirim e-postası gönderilemedi: {Database}", result.DatabaseName);
            }
        }

        private string BuildEmailBody(BackupResult result, bool isSuccess)
        {
            string statusText = isSuccess ? "Başarılı" : "Başarısız";
            string duration = result.Duration.HasValue
                ? result.Duration.Value.ToString(@"hh\:mm\:ss")
                : "-";

            var tmpl = new EmailTemplateBuilder();
            tmpl.WriteHeader("Koru MsSql Yedek — Yedekleme Bildirimi", $"{result.PlanName} · {result.DatabaseName}");
            tmpl.WriteStatusBadge($"Yedekleme {statusText}", isSuccess);

            tmpl.WriteSectionTitle("Yedekleme Özeti");
            tmpl.BeginSummaryTable();
            tmpl.WriteTableRow("Veritabanı", EmailTemplateBuilder.Encode(result.DatabaseName));
            tmpl.WriteTableRow("Plan", EmailTemplateBuilder.Encode(result.PlanName));
            tmpl.WriteTableRow("Yedek Türü", result.BackupType.ToString());
            tmpl.WriteTableRow("Süre", duration);

            if (result.FileSizeBytes > 0)
            {
                tmpl.WriteTableRow("Dosya Boyutu", $"{result.FileSizeBytes / BytesPerMb:F1} MB");
            }

            if (result.CompressedSizeBytes > 0)
            {
                double ratio = result.FileSizeBytes > 0
                    ? (1.0 - (double)result.CompressedSizeBytes / result.FileSizeBytes) * 100
                    : 0;
                tmpl.WriteTableRow("Sıkıştırılmış", $"{result.CompressedSizeBytes / BytesPerMb:F1} MB (%{ratio:F0} kazanç)");
            }

            if (result.VssFileCopySizeBytes > 0)
            {
                tmpl.WriteTableRow("VSS Dosya Kopyası", $"{result.VssFileCopySizeBytes / BytesPerMb:F1} MB");
            }

            if (result.VerifyResult.HasValue)
            {
                bool ok = result.VerifyResult.Value;
                tmpl.WriteTableRow("SQL Doğrulama",
                    ok ? "Geçerli ✓" : "Geçersiz ✗",
                    EmailTemplateBuilder.GetStatusColor(ok));
            }

            if (result.CompressionVerified.HasValue)
            {
                bool ok = result.CompressionVerified.Value;
                tmpl.WriteTableRow("Arşiv Doğrulama",
                    ok ? "Geçerli ✓" : "Geçersiz ✗",
                    EmailTemplateBuilder.GetStatusColor(ok));
            }

            tmpl.WriteTableRow("Correlation ID", $"<code>{EmailTemplateBuilder.Encode(result.CorrelationId)}</code>");
            tmpl.EndTable();

            // Bulut upload sonuçları
            if (result.CloudUploadResults is { Count: > 0 })
            {
                tmpl.WriteSectionTitle("Bulut Yükleme");
                tmpl.BeginDetailTable("Hedef", "Durum", "Uzak Yol", "Detay");

                int idx = 0;
                foreach (var cloud in result.CloudUploadResults)
                {
                    string statusIcon = cloud.IsSuccess ? "✓" : "✗";
                    string color = EmailTemplateBuilder.GetStatusColor(cloud.IsSuccess);

                    string remotePath = cloud.IsSuccess && !string.IsNullOrEmpty(cloud.RemoteFilePath)
                        ? EmailTemplateBuilder.Encode(cloud.RemoteFilePath)
                        : "-";

                    string detail;
                    if (cloud.IsSuccess)
                    {
                        detail = cloud.RemoteFileSizeBytes > 0
                            ? FormatBytes(cloud.RemoteFileSizeBytes)
                            : "Yüklendi";
                    }
                    else
                    {
                        string errorDetail = FormatErrorDetail(cloud.ErrorMessage, cloud.RetryCount);
                        detail = errorDetail;
                    }

                    tmpl.WriteDetailRow(idx++,
                        (EmailTemplateBuilder.Encode(cloud.DisplayName), null),
                        ($"{statusIcon}", color),
                        (remotePath, null),
                        (detail, cloud.IsSuccess ? null : color));
                }

                tmpl.EndDetailTable();
            }

            if (!isSuccess)
            {
                tmpl.WriteErrorBlock(SanitizeForEmail(result.ErrorMessage));
            }

            return tmpl.Build();
        }
    }
}
