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
        /// <summary>
        /// Dosya yedekleme sonuçları için bildirim gönderir.
        /// </summary>
        public async Task NotifyFileBackupAsync(
            List<FileBackupResult> results,
            BackupPlan plan,
            List<CloudUploadResult> cloudUploadResults,
            string archiveFileName,
            long archiveSizeBytes,
            CancellationToken cancellationToken)
        {
            if (plan?.Notifications == null || !plan.Notifications.EmailEnabled)
                return;

            bool allSuccess = results.All(r => r.Status == BackupResultStatus.Success);
            bool shouldNotify = (allSuccess && plan.Notifications.OnSuccess) ||
                                (!allSuccess && plan.Notifications.OnFailure);

            if (!shouldNotify)
                return;

            SmtpProfile profile = ResolveProfile(plan.Notifications);
            if (profile == null || string.IsNullOrWhiteSpace(profile.Host))
            {
                Log.Warning("Dosya yedek bildirimi atlandı: SMTP profili bulunamadı veya sunucu adresi boş. Plan: {PlanName}", plan.PlanName);
                return;
            }

            try
            {
                string recipients = !string.IsNullOrWhiteSpace(plan.Notifications.EmailTo)
                    ? plan.Notifications.EmailTo
                    : profile.RecipientEmails;

                if (string.IsNullOrWhiteSpace(recipients))
                {
                    Log.Warning("Dosya yedek bildirimi atlandı: Alıcı adresi tanımlanmamış. Plan: {PlanName}", plan.PlanName);
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

                string statusText = allSuccess ? "Başarılı ✓" : "Kısmi Başarı ⚠";
                message.Subject = $"[Koru MsSql Yedek] Dosya Yedekleme — {statusText}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = BuildFileBackupEmailBody(results, plan.PlanName, allSuccess,
                        cloudUploadResults, archiveFileName, archiveSizeBytes)
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
                    "Dosya yedek bildirimi gönderildi: {PlanName} → {Recipients} (Profil: {Profile})",
                    plan.PlanName, recipients, profile.DisplayName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Dosya yedek bildirimi gönderilemedi: {PlanName}", plan.PlanName);
            }
        }

        private string BuildFileBackupEmailBody(
            List<FileBackupResult> results, string planName, bool allSuccess,
            List<CloudUploadResult> cloudUploadResults, string archiveFileName, long archiveSizeBytes)
        {
            string statusText = allSuccess ? "Başarılı" : "Kısmi Başarı";

            int totalCopied = results.Sum(r => r.FilesCopied);
            int totalSkipped = results.Sum(r => r.FilesSkipped);
            long totalSize = results.Sum(r => r.TotalSizeBytes);
            int failedSourceCount = results.Count(r => r.Status == BackupResultStatus.Failed);

            var tmpl = new EmailTemplateBuilder();
            tmpl.WriteHeader("Koru MsSql Yedek — Dosya Yedekleme", planName);
            tmpl.WriteStatusBadge($"Dosya Yedekleme {statusText}", allSuccess);

            tmpl.WriteSectionTitle("Özet");
            tmpl.BeginSummaryTable();
            tmpl.WriteTableRow("Plan", EmailTemplateBuilder.Encode(planName));
            tmpl.WriteTableRow("Kopyalanan Dosya", totalCopied.ToString());
            tmpl.WriteTableRow("Atlanan Dosya", totalSkipped.ToString());
            tmpl.WriteTableRow("Toplam Boyut", FormatBytes(totalSize));

            if (!string.IsNullOrEmpty(archiveFileName))
                tmpl.WriteTableRow("Arşiv Dosyası", EmailTemplateBuilder.Encode(archiveFileName));

            if (archiveSizeBytes > 0)
            {
                double ratio = totalSize > 0
                    ? (1.0 - (double)archiveSizeBytes / totalSize) * 100
                    : 0;
                tmpl.WriteTableRow("Arşiv Boyutu", $"{FormatBytes(archiveSizeBytes)} (%{ratio:F0} kazanç)");
            }

            if (failedSourceCount > 0)
                tmpl.WriteTableRow("Başarısız Kaynak", failedSourceCount.ToString(),
                    EmailTemplateBuilder.GetFailureColor());

            tmpl.EndTable();

            // Kaynak detayları
            tmpl.WriteSectionTitle("Kaynak Detayları");
            tmpl.BeginDetailTable("Kaynak", "Dosya", "Boyut", "Süre", "Durum");

            int idx = 0;
            foreach (var r in results)
            {
                bool isOk = r.Status == BackupResultStatus.Success;
                string statusIcon = isOk ? "✓" : "✗";
                string color = EmailTemplateBuilder.GetStatusColor(isOk);
                string sizeText = r.TotalSizeBytes > 0 ? FormatBytes(r.TotalSizeBytes) : "-";
                string durationText = r.Duration.HasValue
                    ? r.Duration.Value.ToString(@"mm\:ss")
                    : "-";

                tmpl.WriteDetailRow(idx++,
                    (EmailTemplateBuilder.Encode(r.SourceName), null),
                    ($"{r.FilesCopied} dosya", null),
                    (sizeText, null),
                    (durationText, null),
                    ($"{statusIcon} {r.Status}", color));
            }

            tmpl.EndDetailTable();

            // Başarısız kaynakların hata detayları
            var failedSources = results.Where(r =>
                r.Status != BackupResultStatus.Success &&
                (!string.IsNullOrEmpty(r.ErrorMessage) || r.FailedFiles.Count > 0)).ToList();

            if (failedSources.Count > 0)
            {
                tmpl.WriteSectionTitle("Hata Detayları");
                foreach (var failed in failedSources)
                {
                    if (!string.IsNullOrEmpty(failed.ErrorMessage))
                    {
                        tmpl.WriteErrorBlock(
                            $"<b>{EmailTemplateBuilder.Encode(failed.SourceName)}:</b> {SanitizeForEmail(failed.ErrorMessage)}");
                    }

                    if (failed.FailedFiles.Count > 0)
                    {
                        tmpl.BeginDetailTable("Başarısız Dosya", "Hata");
                        int fIdx = 0;
                        foreach (var ff in failed.FailedFiles.Take(20))
                        {
                            tmpl.WriteDetailRow(fIdx++,
                                (SanitizeForEmail(ff.FilePath), null),
                                (SanitizeForEmail(ff.ErrorMessage), EmailTemplateBuilder.GetFailureColor()));
                        }
                        tmpl.EndDetailTable();

                        if (failed.FailedFiles.Count > 20)
                            tmpl.WriteInfoBlock($"* Yalnızca ilk 20 hata gösterilmektedir (toplam {failed.FailedFiles.Count}).");
                    }
                }
            }

            // Bulut yükleme sonuçları
            if (cloudUploadResults is { Count: > 0 })
            {
                tmpl.WriteSectionTitle("Bulut Yükleme");
                tmpl.BeginDetailTable("Hedef", "Durum", "Uzak Yol", "Detay");

                int cIdx = 0;
                foreach (var cloud in cloudUploadResults)
                {
                    string cloudStatusIcon = cloud.IsSuccess ? "✓" : "✗";
                    string cloudColor = EmailTemplateBuilder.GetStatusColor(cloud.IsSuccess);

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

                    tmpl.WriteDetailRow(cIdx++,
                        (EmailTemplateBuilder.Encode(cloud.DisplayName), null),
                        ($"{cloudStatusIcon}", cloudColor),
                        (remotePath, null),
                        (detail, cloud.IsSuccess ? null : cloudColor));
                }

                tmpl.EndDetailTable();
            }

            return tmpl.Build();
        }
    }
}
