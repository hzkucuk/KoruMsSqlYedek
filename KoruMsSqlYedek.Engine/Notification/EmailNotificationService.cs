using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Notification
{
    /// MailKit tabanlı e-posta bildirim servisi.
    /// </summary>
    public class EmailNotificationService : INotificationService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<EmailNotificationService>();
        private const double BytesPerMb = 1048576.0;

        private readonly IAppSettingsManager _settingsManager;

        public EmailNotificationService(IAppSettingsManager settingsManager)
        {
            ArgumentNullException.ThrowIfNull(settingsManager);
            _settingsManager = settingsManager;
        }

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

        /// <summary>
        /// SmtpProfileId üzerinden profil arar; bulamazsa eski per-plan alanlarından geçici profil oluşturur.
        /// </summary>
        private SmtpProfile ResolveProfile(NotificationConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.SmtpProfileId))
            {
                var appSettings = _settingsManager.Load();
                var profile = appSettings.SmtpProfiles?.Find(p => p.Id == config.SmtpProfileId);
                if (profile != null)
                    return profile;

                Log.Warning("SmtpProfileId '{Id}' bulunamadı; eski SMTP alanları deneniyor.", config.SmtpProfileId);
            }

            // Geriye uyumluluk: eski planlarda per-plan SMTP alanları dolu olabilir
            if (!string.IsNullOrWhiteSpace(config.SmtpServer))
            {
                return new SmtpProfile
                {
                    DisplayName = "(eski plan ayarı)",
                    Host = config.SmtpServer,
                    Port = config.SmtpPort ?? 587,
                    UseSsl = config.SmtpUseSsl ?? true,
                    Username = config.SmtpUsername,
                    Password = config.SmtpPassword,
                    SenderEmail = config.SmtpUsername,
                    RecipientEmails = config.EmailTo
                };
            }

            return null;
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

        /// <summary>
        /// E-posta gövdesine eklenmeden önce
        /// Dosya yolları, sunucu adresleri ve stack trace bilgilerini gizler; HTML encode uygular.
        /// </summary>
        private static string SanitizeForEmail(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "Bilinmeyen hata";

            // Stack trace varsa kaldır
            int stackIdx = message.IndexOf("   at ", StringComparison.Ordinal);
            if (stackIdx > 0)
                message = message.Substring(0, stackIdx).Trim();

            // Dosya yollarını gizle
            message = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"[A-Za-z]:\\[^\s""']+|\\\\[^\s""']+",
                "[yol gizlendi]");

            // Uzun mesajları kısalt
            const int maxLength = 500;
            if (message.Length > maxLength)
                message = message.Substring(0, maxLength) + "…";

            // HTML encode — XSS önlemi
            return System.Net.WebUtility.HtmlEncode(message);
        }

        /// <summary>
        /// Hata mesajını detaylı formatta biçimlendirir (deneme sayısı + mesaj).
        /// </summary>
        private static string FormatErrorDetail(string errorMessage, int retryCount)
        {
            string sanitized = SanitizeForEmail(errorMessage);
            string retryInfo = retryCount > 0 ? $"({retryCount + 1} deneme) " : "";
            return $"{retryInfo}{sanitized}";
        }

        /// <summary>
        /// Byte değerini okunabilir boyut metnine dönüştürür.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }

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
