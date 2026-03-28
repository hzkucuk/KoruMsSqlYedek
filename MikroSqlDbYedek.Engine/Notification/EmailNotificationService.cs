using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Serilog;
using MikroSqlDbYedek.Core.Helpers;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;

namespace MikroSqlDbYedek.Engine.Notification
{
    /// <summary>
    /// MailKit tabanlı e-posta bildirim servisi.
    /// </summary>
    public class EmailNotificationService : INotificationService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<EmailNotificationService>();

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

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("MikroSqlDbYedek", config.SmtpUsername));
                message.To.Add(MailboxAddress.Parse(config.EmailTo));

                bool isSuccess = result.Status == BackupResultStatus.Success;
                string statusText = isSuccess ? "Başarılı ✓" : "Başarısız ✗";

                message.Subject = $"[MikroSqlDbYedek] {result.DatabaseName} — Yedekleme {statusText}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = BuildEmailBody(result, isSuccess)
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        config.SmtpServer,
                        config.SmtpPort,
                        config.SmtpUseSsl ? MailKit.Security.SecureSocketOptions.StartTls
                                          : MailKit.Security.SecureSocketOptions.None,
                        cancellationToken);

                    if (!string.IsNullOrEmpty(config.SmtpUsername))
                    {
                        string password = PasswordProtector.Unprotect(config.SmtpPassword);
                        await client.AuthenticateAsync(config.SmtpUsername, password, cancellationToken);
                    }

                    await client.SendAsync(message, cancellationToken);
                    await client.DisconnectAsync(true, cancellationToken);
                }

                Log.Information(
                    "Bildirim e-postası gönderildi: {Database} → {Email}",
                    result.DatabaseName, config.EmailTo);
            }
            catch (Exception ex)
            {
                // Bildirim başarısızlığı yedek başarısını ETKİLEMEZ
                Log.Error(ex, "Bildirim e-postası gönderilemedi: {Database}", result.DatabaseName);
            }
        }

        private string BuildEmailBody(BackupResult result, bool isSuccess)
        {
            string color = isSuccess ? "#28a745" : "#dc3545";
            string statusText = isSuccess ? "Başarılı" : "Başarısız";
            string duration = result.Duration.HasValue
                ? result.Duration.Value.ToString(@"hh\:mm\:ss")
                : "-";

            var sb = new StringBuilder();
            sb.AppendLine($@"
<div style='font-family: Segoe UI, Arial; max-width: 600px;'>
    <h2 style='color: {color};'>Yedekleme {statusText}</h2>
    <table style='border-collapse: collapse; width: 100%;'>
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Veritabanı</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{result.DatabaseName}</td></tr>
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Plan</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{result.PlanName}</td></tr>
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Yedek Türü</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{result.BackupType}</td></tr>
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Süre</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{duration}</td></tr>");

            // Dosya boyut bilgileri
            if (result.FileSizeBytes > 0)
            {
                sb.AppendLine($@"
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Dosya Boyutu</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{result.FileSizeBytes / 1048576.0:F1} MB</td></tr>");
            }

            if (result.CompressedSizeBytes > 0)
            {
                double ratio = result.FileSizeBytes > 0
                    ? (1.0 - (double)result.CompressedSizeBytes / result.FileSizeBytes) * 100
                    : 0;
                sb.AppendLine($@"
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Sıkıştırılmış</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{result.CompressedSizeBytes / 1048576.0:F1} MB (%{ratio:F0} kazanç)</td></tr>");
            }

            // Doğrulama sonucu
            if (result.VerifyResult.HasValue)
            {
                string verifyColor = result.VerifyResult.Value ? "#28a745" : "#dc3545";
                string verifyText = result.VerifyResult.Value ? "Geçerli ✓" : "Geçersiz ✗";
                sb.AppendLine($@"
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Doğrulama</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd; color: {verifyColor};'>{verifyText}</td></tr>");
            }

            // Bulut upload sonuçları
            if (result.CloudUploadResults != null && result.CloudUploadResults.Count > 0)
            {
                foreach (var cloud in result.CloudUploadResults)
                {
                    string cloudColor = cloud.IsSuccess ? "#28a745" : "#dc3545";
                    string cloudStatus = cloud.IsSuccess ? "✓" : "✗";
                    sb.AppendLine($@"
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Bulut: {cloud.DisplayName}</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd; color: {cloudColor};'>{cloudStatus} {(cloud.IsSuccess ? "" : cloud.ErrorMessage)}</td></tr>");
                }
            }

            sb.AppendLine($@"
        <tr><td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>Correlation ID</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'><code>{result.CorrelationId}</code></td></tr>
    </table>
    {(isSuccess ? "" : $"<p style='color: red;'><b>Hata:</b> {result.ErrorMessage}</p>")}
    <p style='color: #666; font-size: 12px;'>Bu e-posta MikroSqlDbYedek tarafından otomatik gönderilmiştir.</p>
</div>");

            return sb.ToString();
        }

        /// <summary>
        /// Dosya yedekleme sonuçları için bildirim gönderir.
        /// </summary>
        public async Task NotifyFileBackupAsync(
            List<FileBackupResult> results,
            BackupPlan plan,
            CancellationToken cancellationToken)
        {
            if (plan?.Notifications == null || !plan.Notifications.EmailEnabled)
                return;

            bool allSuccess = results.All(r => r.Status == BackupResultStatus.Success);
            bool shouldNotify = (allSuccess && plan.Notifications.OnSuccess) ||
                                (!allSuccess && plan.Notifications.OnFailure);

            if (!shouldNotify)
                return;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("MikroSqlDbYedek", plan.Notifications.SmtpUsername));
                message.To.Add(MailboxAddress.Parse(plan.Notifications.EmailTo));

                string statusText = allSuccess ? "Başarılı ✓" : "Kısmi Başarı ⚠";
                message.Subject = $"[MikroSqlDbYedek] Dosya Yedekleme — {statusText}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = BuildFileBackupEmailBody(results, plan.PlanName, allSuccess)
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        plan.Notifications.SmtpServer,
                        plan.Notifications.SmtpPort,
                        plan.Notifications.SmtpUseSsl
                            ? MailKit.Security.SecureSocketOptions.StartTls
                            : MailKit.Security.SecureSocketOptions.None,
                        cancellationToken);

                    if (!string.IsNullOrEmpty(plan.Notifications.SmtpUsername))
                    {
                        string password = PasswordProtector.Unprotect(plan.Notifications.SmtpPassword);
                        await client.AuthenticateAsync(plan.Notifications.SmtpUsername, password, cancellationToken);
                    }

                    await client.SendAsync(message, cancellationToken);
                    await client.DisconnectAsync(true, cancellationToken);
                }

                Log.Information("Dosya yedek bildirimi gönderildi: {PlanName}", plan.PlanName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Dosya yedek bildirimi gönderilemedi: {PlanName}", plan.PlanName);
            }
        }

        private string BuildFileBackupEmailBody(
            List<FileBackupResult> results, string planName, bool allSuccess)
        {
            string color = allSuccess ? "#28a745" : "#e67e22";
            string statusText = allSuccess ? "Başarılı" : "Kısmi Başarı";

            int totalCopied = results.Sum(r => r.FilesCopied);
            int totalSkipped = results.Sum(r => r.FilesSkipped);
            long totalSize = results.Sum(r => r.TotalSizeBytes);

            var sb = new StringBuilder();
            sb.AppendLine($@"
<div style='font-family: Segoe UI, Arial; max-width: 600px;'>
    <h2 style='color: {color};'>Dosya Yedekleme {statusText}</h2>
    <p><b>Plan:</b> {planName}</p>
    <p><b>Toplam:</b> {totalCopied} dosya kopyalandı, {totalSkipped} atlandı [{totalSize / 1048576.0:F1} MB]</p>
    <table style='border-collapse: collapse; width: 100%;'>");

            foreach (var r in results)
            {
                string rowColor = r.Status == BackupResultStatus.Success ? "#28a745" : "#dc3545";
                sb.AppendLine($@"
        <tr>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'><b>{r.SourceName}</b></td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{r.FilesCopied} dosya</td>
            <td style='padding: 8px; border-bottom: 1px solid #ddd; color: {rowColor};'>{r.Status}</td>
        </tr>");
            }

            sb.AppendLine(@"
    </table>
    <p style='color: #666; font-size: 12px;'>Bu e-posta MikroSqlDbYedek tarafından otomatik gönderilmiştir.</p>
</div>");

            return sb.ToString();
        }
    }
}
