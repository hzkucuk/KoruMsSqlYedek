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
        public async Task NotifyJobCompletedAsync(
            JobNotificationData data,
            NotificationConfig config,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (config == null || !config.EmailEnabled)
                return;

            bool shouldNotify = (data.IsSuccess && config.OnSuccess) ||
                                (!data.IsSuccess && config.OnFailure);

            if (!shouldNotify)
                return;

            SmtpProfile profile = ResolveProfile(config);
            if (profile == null || string.IsNullOrWhiteSpace(profile.Host))
            {
                Log.Warning("Konsolide bildirim atlandı: SMTP profili bulunamadı. Plan: {PlanName}", data.PlanName);
                return;
            }

            try
            {
                string recipients = !string.IsNullOrWhiteSpace(config.EmailTo)
                    ? config.EmailTo
                    : profile.RecipientEmails;

                if (string.IsNullOrWhiteSpace(recipients))
                {
                    Log.Warning("Konsolide bildirim atlandı: Alıcı adresi tanımlanmamış. Plan: {PlanName}", data.PlanName);
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

                string statusText = data.IsSuccess ? "Başarılı ✓" : "Başarısız ✗";
                message.Subject = $"[Koru MsSql Yedek] {data.PlanName} — Yedekleme {statusText}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = BuildJobCompletedEmailBody(data)
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
                    "Konsolide bildirim gönderildi: {PlanName} → {Recipients} (Profil: {Profile})",
                    data.PlanName, recipients, profile.DisplayName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Konsolide bildirim gönderilemedi: {PlanName}", data.PlanName);
            }
        }

        /// <summary>
        /// Konsolide görev bildirimi için HTML e-posta gövdesi oluşturur.
        /// SQL + dosya + bulut sonuçları ve log satırları dahil.
        /// </summary>
        private string BuildJobCompletedEmailBody(JobNotificationData data)
        {
            string statusText = data.IsSuccess ? "Başarılı" : "Başarısız";
            TimeSpan duration = data.CompletedAt - data.StartedAt;

            var tmpl = new EmailTemplateBuilder();
            tmpl.WriteHeader("Koru MsSql Yedek — Yedekleme Bildirimi", data.PlanName);
            tmpl.WriteStatusBadge($"Yedekleme {statusText}", data.IsSuccess);

            // ── Genel Özet ──
            tmpl.WriteSectionTitle("Genel Özet");
            tmpl.BeginSummaryTable();
            tmpl.WriteTableRow("Plan", EmailTemplateBuilder.Encode(data.PlanName));
            tmpl.WriteTableRow("Yedek Türü", EmailTemplateBuilder.Encode(data.BackupType));
            tmpl.WriteTableRow("Süre", duration.ToString(@"hh\:mm\:ss"));
            tmpl.WriteTableRow("Başlangıç", data.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            tmpl.WriteTableRow("Bitiş", data.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            tmpl.WriteTableRow("Correlation ID", $"<code>{EmailTemplateBuilder.Encode(data.CorrelationId)}</code>");
            tmpl.EndTable();

            // ── SQL Yedekleme Sonuçları ──
            if (data.SqlResults.Count > 0)
            {
                tmpl.WriteSectionTitle($"SQL Yedekleme ({data.SqlResults.Count} veritabanı)");
                tmpl.BeginDetailTable("Veritabanı", "Tür", "Boyut", "Sıkıştırılmış", "Doğrulama", "Bulut", "Durum");

                int idx = 0;
                foreach (var r in data.SqlResults)
                {
                    bool isOk = r.Status == BackupResultStatus.Success;
                    string icon = isOk ? "✓" : "✗";
                    string color = EmailTemplateBuilder.GetStatusColor(isOk);
                    string sizeText = r.FileSizeBytes > 0 ? FormatBytes(r.FileSizeBytes) : "-";
                    string compText = r.CompressedSizeBytes > 0
                        ? $"{FormatBytes(r.CompressedSizeBytes)} ({FmtRatio(r.FileSizeBytes, r.CompressedSizeBytes)})"
                        : "-";

                    string verifyText = "-";
                    if (r.VerifyResult.HasValue)
                        verifyText = r.VerifyResult.Value ? "✓" : "✗";

                    string cloudText = "-";
                    if (r.CloudUploadResults is { Count: > 0 })
                    {
                        int ok = r.CloudUploadResults.Count(c => c.IsSuccess);
                        cloudText = $"{ok}/{r.CloudUploadResults.Count}";
                    }

                    tmpl.WriteDetailRow(idx++,
                        (EmailTemplateBuilder.Encode(r.DatabaseName), null),
                        (r.BackupType.ToString(), null),
                        (sizeText, null),
                        (compText, null),
                        (verifyText, r.VerifyResult == false ? EmailTemplateBuilder.GetFailureColor() : null),
                        (cloudText, null),
                        ($"{icon} {r.Status}", color));
                }

                tmpl.EndDetailTable();

                // SQL bulut detayları (tüm DB'lerin bulut sonuçları birleşik)
                var allSqlCloudResults = new List<CloudUploadResult>();
                foreach (var r in data.SqlResults)
                {
                    if (r.CloudUploadResults is { Count: > 0 })
                        allSqlCloudResults.AddRange(r.CloudUploadResults);
                }

                if (allSqlCloudResults.Count > 0)
                {
                    tmpl.WriteSectionTitle("SQL Bulut Yükleme Detayları");
                    WriteCloudResultsTable(tmpl, allSqlCloudResults);
                }
            }

            // ── Dosya Yedekleme Sonuçları ──
            if (data.FileResults.Count > 0)
            {
                int totalCopied = data.FileResults.Sum(r => r.FilesCopied);
                int totalSkipped = data.FileResults.Sum(r => r.FilesSkipped);
                long totalSize = data.FileResults.Sum(r => r.TotalSizeBytes);

                tmpl.WriteSectionTitle($"Dosya Yedekleme ({data.FileResults.Count} kaynak)");
                tmpl.BeginSummaryTable();
                tmpl.WriteTableRow("Kopyalanan Dosya", totalCopied.ToString());
                tmpl.WriteTableRow("Atlanan Dosya", totalSkipped.ToString());
                tmpl.WriteTableRow("Toplam Boyut", FormatBytes(totalSize));

                if (!string.IsNullOrEmpty(data.FileArchiveFileName))
                    tmpl.WriteTableRow("Arşiv Dosyası", EmailTemplateBuilder.Encode(data.FileArchiveFileName));

                if (data.FileArchiveSizeBytes > 0)
                {
                    double ratio = totalSize > 0
                        ? (1.0 - (double)data.FileArchiveSizeBytes / totalSize) * 100
                        : 0;
                    tmpl.WriteTableRow("Arşiv Boyutu", $"{FormatBytes(data.FileArchiveSizeBytes)} (%{ratio:F0} kazanç)");
                }

                tmpl.EndTable();

                tmpl.BeginDetailTable("Kaynak", "Dosya", "Boyut", "Süre", "Durum");
                int fIdx = 0;
                foreach (var r in data.FileResults)
                {
                    bool isOk = r.Status == BackupResultStatus.Success;
                    string icon = isOk ? "✓" : "✗";
                    string fColor = EmailTemplateBuilder.GetStatusColor(isOk);
                    string fSize = r.TotalSizeBytes > 0 ? FormatBytes(r.TotalSizeBytes) : "-";
                    string fDur = r.Duration.HasValue ? r.Duration.Value.ToString(@"mm\:ss") : "-";

                    tmpl.WriteDetailRow(fIdx++,
                        (EmailTemplateBuilder.Encode(r.SourceName), null),
                        ($"{r.FilesCopied} dosya", null),
                        (fSize, null),
                        (fDur, null),
                        ($"{icon} {r.Status}", fColor));
                }

                tmpl.EndDetailTable();

                // Dosya bulut yükleme detayları
                if (data.FileCloudUploadResults is { Count: > 0 })
                {
                    tmpl.WriteSectionTitle("Dosya Bulut Yükleme Detayları");
                    WriteCloudResultsTable(tmpl, data.FileCloudUploadResults);
                }
            }

            // ── Log Satırları ──
            if (data.LogLines.Count > 0)
            {
                tmpl.WriteSectionTitle("Görev Logu");
                tmpl.WriteRawHtml(
                    @"    <div style=""background:#1a2e1a; color:#d4d4d4; padding:12px 16px; border-radius:6px; font-family:'Cascadia Code',Consolas,monospace; font-size:11px; line-height:1.6; max-height:400px; overflow-y:auto; white-space:pre-wrap; word-break:break-all;"">");

                foreach (string logLine in data.LogLines)
                {
                    tmpl.WriteRawHtml($"      {EmailTemplateBuilder.Encode(logLine)}<br/>");
                }

                tmpl.WriteRawHtml("    </div>");
            }

            return tmpl.Build();
        }

        /// <summary>
        /// Bulut yükleme sonuçlarını detay tablosu olarak yazar.
        /// </summary>
        private void WriteCloudResultsTable(EmailTemplateBuilder tmpl, List<CloudUploadResult> results)
        {
            tmpl.BeginDetailTable("Hedef", "Durum", "Uzak Yol", "Detay");

            int idx = 0;
            foreach (var cloud in results)
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
                    detail = FormatErrorDetail(cloud.ErrorMessage, cloud.RetryCount);
                }

                tmpl.WriteDetailRow(idx++,
                    (EmailTemplateBuilder.Encode(cloud.DisplayName), null),
                    ($"{statusIcon}", color),
                    (remotePath, null),
                    (detail, cloud.IsSuccess ? null : color));
            }

            tmpl.EndDetailTable();
        }
    }
}
