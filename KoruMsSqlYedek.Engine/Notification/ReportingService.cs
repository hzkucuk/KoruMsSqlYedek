using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// Periyodik yedekleme özet raporu oluşturan ve SMTP ile gönderen servis.
    /// Günlük / Haftalık / Aylık modlarda ilgili dönemin geçmiş kayıtlarını özetler.
    /// </summary>
    public class ReportingService : IReportingService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<ReportingService>();
        private const double BytesPerMb = 1048576.0;

        private readonly IBackupHistoryManager _historyManager;
        private readonly IAppSettingsManager _settingsManager;

        public ReportingService(
            IBackupHistoryManager historyManager,
            IAppSettingsManager settingsManager)
        {
            ArgumentNullException.ThrowIfNull(historyManager);
            ArgumentNullException.ThrowIfNull(settingsManager);
            _historyManager = historyManager;
            _settingsManager = settingsManager;
        }

        /// <inheritdoc/>
        public async Task SendReportAsync(BackupPlan plan, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(plan);

            var reporting = plan.Reporting;
            if (reporting == null || !reporting.IsEnabled)
                return;

            SmtpProfile profile = ResolveProfile(plan);
            if (profile == null || string.IsNullOrWhiteSpace(profile.Host))
            {
                Log.Warning("Periyodik rapor atlandı — Plan: {PlanId}: SMTP profili bulunamadı.", plan.PlanId);
                return;
            }

            string recipients = !string.IsNullOrWhiteSpace(reporting.EmailTo)
                ? reporting.EmailTo
                : profile.RecipientEmails;

            if (string.IsNullOrWhiteSpace(recipients))
            {
                Log.Warning("Periyodik rapor atlandı — Plan: {PlanId}: Alıcı adresi tanımlanmamış.", plan.PlanId);
                return;
            }

            (DateTime from, DateTime to) = GetReportingPeriod(reporting.Frequency);
            var results = _historyManager.GetHistoryByDateRange(from, to)
                .Where(r => r.PlanId == plan.PlanId)
                .ToList();

            try
            {
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

                string periodLabel = GetPeriodLabel(reporting.Frequency, from);
                message.Subject = $"[Koru MsSql Yedek] {plan.PlanName} — {periodLabel} Raporu";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = BuildReportHtml(plan, results, reporting.Frequency, from, to)
                };
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        profile.Host,
                        profile.Port,
                        profile.UseSsl
                            ? MailKit.Security.SecureSocketOptions.StartTls
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
                    "Periyodik rapor gönderildi — Plan: {PlanName}, Dönem: {Period}, Kayıt: {Count}",
                    plan.PlanName, periodLabel, results.Count);
            }
            catch (Exception ex)
            {
                // Rapor hatası yedek işlemini etkilemez
                Log.Error(ex, "Periyodik rapor gönderilemedi — Plan: {PlanId}", plan.PlanId);
            }
        }

        /// <summary>
        /// Plan'ın bildirim ayarlarından SMTP profilini çözer.
        /// Raporlama kendine özgü bir SMTP seçimi yapmaz; plan bildirimiyle aynı profili kullanır.
        /// </summary>
        private SmtpProfile ResolveProfile(BackupPlan plan)
        {
            var notif = plan.Notifications;
            if (notif == null)
                return null;

            if (!string.IsNullOrWhiteSpace(notif.SmtpProfileId))
            {
                var appSettings = _settingsManager.Load();
                var profile = appSettings.SmtpProfiles?.Find(p => p.Id == notif.SmtpProfileId);
                if (profile != null)
                    return profile;

                Log.Warning("Rapor: SmtpProfileId '{Id}' bulunamadı; eski SMTP alanları deneniyor.", notif.SmtpProfileId);
            }

            if (!string.IsNullOrWhiteSpace(notif.SmtpServer))
            {
                return new SmtpProfile
                {
                    DisplayName = "(eski plan ayarı)",
                    Host = notif.SmtpServer,
                    Port = notif.SmtpPort ?? 587,
                    UseSsl = notif.SmtpUseSsl ?? true,
                    Username = notif.SmtpUsername,
                    Password = notif.SmtpPassword,
                    SenderEmail = notif.SmtpUsername,
                    RecipientEmails = notif.EmailTo
                };
            }

            return null;
        }

        /// <summary>
        /// Raporlama sıklığına göre dönem başlangıç ve bitiş tarihlerini hesaplar.
        /// </summary>
        public static (DateTime From, DateTime To) GetReportingPeriod(ReportFrequency frequency)
        {
            DateTime now = DateTime.Now;
            DateTime to = now;

            switch (frequency)
            {
                case ReportFrequency.Daily:
                    return (now.Date.AddDays(-1), now.Date);

                case ReportFrequency.Weekly:
                    int daysSinceMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                    DateTime weekStart = now.Date.AddDays(-daysSinceMonday - 7);
                    return (weekStart, weekStart.AddDays(7));

                case ReportFrequency.Monthly:
                    DateTime firstOfLastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    DateTime firstOfThisMonth = new DateTime(now.Year, now.Month, 1);
                    return (firstOfLastMonth, firstOfThisMonth);

                default:
                    return (now.Date.AddDays(-1), now.Date);
            }
        }

        private static string GetPeriodLabel(ReportFrequency frequency, DateTime from)
        {
            switch (frequency)
            {
                case ReportFrequency.Daily:
                    return $"Günlük ({from:dd.MM.yyyy})";
                case ReportFrequency.Weekly:
                    return $"Haftalık ({from:dd.MM.yyyy})";
                case ReportFrequency.Monthly:
                    return $"Aylık ({from:MMMM yyyy})";
                default:
                    return $"Rapor ({from:dd.MM.yyyy})";
            }
        }

        private string BuildReportHtml(
            BackupPlan plan,
            List<BackupResult> results,
            ReportFrequency frequency,
            DateTime from,
            DateTime to)
        {
            int total = results.Count;
            int success = results.Count(r => r.Status == BackupResultStatus.Success);
            int failed = results.Count(r => r.Status == BackupResultStatus.Failed);
            int partial = total - success - failed;
            double successRate = total > 0 ? (success * 100.0 / total) : 0;
            long totalBytes = results.Sum(r => r.FileSizeBytes);
            long totalCompressed = results.Sum(r => r.CompressedSizeBytes);

            string periodLabel = GetPeriodLabel(frequency, from);
            string rateColor = successRate >= 90
                ? EmailTemplateBuilder.GetSuccessColor()
                : successRate >= 70
                    ? EmailTemplateBuilder.GetWarningColor()
                    : EmailTemplateBuilder.GetFailureColor();

            var builder = new EmailTemplateBuilder();

            // ── Header ──
            builder.WriteHeader("Koru MsSql Yedek — Periyodik Rapor", $"{plan.PlanName} · {periodLabel}");

            // ── Özet Tablosu ──
            builder.WriteSectionTitle("Özet");
            builder.BeginSummaryTable();
            builder.WriteTableRow("Dönem", $"{from:dd.MM.yyyy} – {to:dd.MM.yyyy}");
            builder.WriteTableRow("Toplam Yedekleme", total.ToString());
            builder.WriteTableRow("Başarılı", success.ToString(), EmailTemplateBuilder.GetSuccessColor());
            builder.WriteTableRow("Başarısız", failed.ToString(), failed > 0 ? EmailTemplateBuilder.GetFailureColor() : null);

            if (partial > 0)
                builder.WriteTableRow("Kısmi Başarı", partial.ToString(), EmailTemplateBuilder.GetWarningColor());

            builder.WriteTableRow("Başarı Oranı", $"%{successRate:F0}", rateColor);

            if (totalBytes > 0)
                builder.WriteTableRow("Toplam Veri", $"{totalBytes / BytesPerMb:F1} MB");

            if (totalCompressed > 0 && totalBytes > 0)
            {
                double compressionRatio = (1.0 - (double)totalCompressed / totalBytes) * 100;
                builder.WriteTableRow("Sıkıştırılmış Toplam",
                    $"{totalCompressed / BytesPerMb:F1} MB (%{compressionRatio:F0} kazanç)");
            }

            // ── Ek İstatistikler ──
            var successResults = results.Where(r => r.Status == BackupResultStatus.Success).ToList();

            if (successResults.Count > 0)
            {
                var withDuration = successResults.Where(r => r.Duration.HasValue).ToList();
                if (withDuration.Count > 0)
                {
                    double avgSeconds = withDuration.Average(r => r.Duration.Value.TotalSeconds);
                    builder.WriteTableRow("Ortalama Süre", TimeSpan.FromSeconds(avgSeconds).ToString(@"mm\:ss"));
                }

                var largestDb = successResults.OrderByDescending(r => r.FileSizeBytes).First();
                if (largestDb.FileSizeBytes > 0)
                {
                    builder.WriteTableRow("En Büyük Yedek",
                        $"{EmailTemplateBuilder.Encode(largestDb.DatabaseName ?? "-")} ({largestDb.FileSizeBytes / BytesPerMb:F1} MB)");
                }
            }

            builder.EndTable();

            // ── Veritabanı Bazlı Özet ──
            var dbGroups = results.GroupBy(r => r.DatabaseName ?? "-").OrderBy(g => g.Key).ToList();
            if (dbGroups.Count > 1)
            {
                builder.WriteSectionTitle("Veritabanı Özeti");
                builder.BeginDetailTable("Veritabanı", "Toplam", "Başarılı", "Başarısız", "Ort. Süre", "Toplam Boyut");

                int dbRowIdx = 0;
                foreach (var grp in dbGroups)
                {
                    int grpTotal = grp.Count();
                    int grpSuccess = grp.Count(r => r.Status == BackupResultStatus.Success);
                    int grpFailed = grp.Count(r => r.Status == BackupResultStatus.Failed);
                    var grpWithDuration = grp.Where(r => r.Duration.HasValue).ToList();
                    string grpAvgDuration = grpWithDuration.Count > 0
                        ? TimeSpan.FromSeconds(grpWithDuration.Average(r => r.Duration.Value.TotalSeconds)).ToString(@"mm\:ss")
                        : "-";
                    long grpBytes = grp.Sum(r => r.FileSizeBytes);
                    string grpSize = grpBytes > 0 ? $"{grpBytes / BytesPerMb:F1} MB" : "-";

                    string failColor = grpFailed > 0 ? EmailTemplateBuilder.GetFailureColor() : null;
                    builder.WriteDetailRow(dbRowIdx++,
                        (EmailTemplateBuilder.Encode(grp.Key), null),
                        (grpTotal.ToString(), null),
                        (grpSuccess.ToString(), EmailTemplateBuilder.GetSuccessColor()),
                        (grpFailed.ToString(), failColor),
                        (grpAvgDuration, null),
                        (grpSize, null));
                }

                builder.EndDetailTable();
            }

            // ── Detay Tablosu (en fazla 50 kayıt) ──
            if (results.Count > 0)
            {
                builder.WriteSectionTitle("Yedekleme Detayları");
                builder.BeginDetailTable("Tarih", "Veritabanı", "Tür", "Durum", "Boyut", "Süre", "Bulut");

                int rowIndex = 0;
                foreach (var r in results.OrderByDescending(x => x.StartedAt).Take(50))
                {
                    string statusColor = r.Status == BackupResultStatus.Success
                        ? EmailTemplateBuilder.GetSuccessColor()
                        : r.Status == BackupResultStatus.Failed
                            ? EmailTemplateBuilder.GetFailureColor()
                            : EmailTemplateBuilder.GetWarningColor();
                    string statusIcon = r.Status == BackupResultStatus.Success ? "✓"
                        : r.Status == BackupResultStatus.Failed ? "✗" : "⚠";
                    string sizeText = r.FileSizeBytes > 0
                        ? $"{r.FileSizeBytes / BytesPerMb:F1} MB"
                        : "-";
                    string durationText = r.Duration.HasValue
                        ? r.Duration.Value.ToString(@"mm\:ss")
                        : "-";

                    // Bulut yükleme özeti
                    string cloudText = "-";
                    string cloudColor = null;
                    if (r.CloudUploadResults is { Count: > 0 })
                    {
                        int cloudOk = r.CloudUploadResults.Count(c => c.IsSuccess);
                        int cloudTotal = r.CloudUploadResults.Count;
                        cloudText = $"{cloudOk}/{cloudTotal}";
                        cloudColor = cloudOk == cloudTotal
                            ? EmailTemplateBuilder.GetSuccessColor()
                            : cloudOk > 0
                                ? EmailTemplateBuilder.GetWarningColor()
                                : EmailTemplateBuilder.GetFailureColor();
                    }

                    builder.WriteDetailRow(rowIndex++,
                        ($"{r.StartedAt:dd.MM.yy HH:mm}", null),
                        (EmailTemplateBuilder.Encode(r.DatabaseName ?? "-"), null),
                        (r.BackupType.ToString(), null),
                        ($"{statusIcon}", statusColor),
                        (sizeText, null),
                        (durationText, null),
                        (cloudText, cloudColor));
                }

                builder.EndDetailTable();

                if (results.Count > 50)
                    builder.WriteInfoBlock($"* Yalnızca son 50 kayıt gösterilmektedir (toplam {results.Count}).");

                // ── Başarısız Yedeklemelerin Hata Detayları ──
                var failedBackups = results
                    .Where(r => r.Status == BackupResultStatus.Failed && !string.IsNullOrEmpty(r.ErrorMessage))
                    .OrderByDescending(x => x.StartedAt)
                    .Take(10)
                    .ToList();

                if (failedBackups.Count > 0)
                {
                    builder.WriteSectionTitle("Hata Detayları");
                    builder.BeginDetailTable("Tarih", "Veritabanı", "Hata Mesajı");

                    int errIdx = 0;
                    foreach (var r in failedBackups)
                    {
                        string errMsg = SanitizeForReport(r.ErrorMessage);
                        builder.WriteDetailRow(errIdx++,
                            ($"{r.StartedAt:dd.MM.yy HH:mm}", null),
                            (EmailTemplateBuilder.Encode(r.DatabaseName ?? "-"), null),
                            (errMsg, EmailTemplateBuilder.GetFailureColor()));
                    }

                    builder.EndDetailTable();
                }

                // ── Başarısız Bulut Yüklemeleri ──
                var failedCloudBackups = results
                    .Where(r => r.CloudUploadResults is { Count: > 0 } && r.CloudUploadResults.Any(c => !c.IsSuccess))
                    .OrderByDescending(x => x.StartedAt)
                    .Take(10)
                    .ToList();

                if (failedCloudBackups.Count > 0)
                {
                    builder.WriteSectionTitle("Başarısız Bulut Yüklemeleri");
                    builder.BeginDetailTable("Tarih", "Veritabanı", "Hedef", "Deneme", "Hata");

                    int cloudErrIdx = 0;
                    foreach (var r in failedCloudBackups)
                    {
                        foreach (var c in r.CloudUploadResults.Where(x => !x.IsSuccess))
                        {
                            builder.WriteDetailRow(cloudErrIdx++,
                                ($"{r.StartedAt:dd.MM.yy HH:mm}", null),
                                (EmailTemplateBuilder.Encode(r.DatabaseName ?? "-"), null),
                                (EmailTemplateBuilder.Encode(c.DisplayName ?? c.ProviderType.ToString()), null),
                                ($"{c.RetryCount + 1}", null),
                                (SanitizeForReport(c.ErrorMessage), EmailTemplateBuilder.GetFailureColor()));
                        }
                    }

                    builder.EndDetailTable();
                }
            }
            else
            {
                builder.WriteInfoBlock("Bu dönemde yedekleme kaydı bulunamadı.");
            }

            return builder.Build();
        }

        /// <summary>
        /// Rapor için hata mesajını güvenli biçimde kısaltır.
        /// </summary>
        private static string SanitizeForReport(string message)
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
                "[yol]");

            const int maxLength = 400;
            if (message.Length > maxLength)
                message = message.Substring(0, maxLength) + "…";

            return System.Net.WebUtility.HtmlEncode(message);
        }
    }
}
