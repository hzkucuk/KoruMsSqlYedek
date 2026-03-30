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
            string rateColor = successRate >= 90 ? "#10b981" : successRate >= 70 ? "#f59e0b" : "#ef4444";

            var sb = new StringBuilder();
            sb.AppendLine($@"
<div style='font-family: Segoe UI, Arial; max-width: 680px; color: #222;'>
  <div style='background:#1a2e1a; padding:16px 24px; border-radius:6px 6px 0 0;'>
    <h2 style='color:#10b981; margin:0; font-size:18px;'>Koru MsSql Yedek — Periyodik Rapor</h2>
    <p style='color:#aaa; margin:4px 0 0; font-size:13px;'>{plan.PlanName} · {periodLabel}</p>
  </div>
  <div style='background:#f9f9f9; padding:20px 24px; border:1px solid #ddd;'>
    <h3 style='margin:0 0 12px; font-size:15px;'>Özet</h3>
    <table style='border-collapse:collapse; width:100%; font-size:13px;'>
      <tr>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee; width:40%;'><b>Dönem</b></td>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'>{from:dd.MM.yyyy} – {to:dd.MM.yyyy}</td>
      </tr>
      <tr>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'><b>Toplam Yedekleme</b></td>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'>{total}</td>
      </tr>
      <tr>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'><b>Başarılı</b></td>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee; color:#10b981;'><b>{success}</b></td>
      </tr>
      <tr>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'><b>Başarısız</b></td>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee; color:#ef4444;'><b>{failed}</b></td>
      </tr>");

            if (partial > 0)
            {
                sb.AppendLine($@"
      <tr>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'><b>Kısmi Başarı</b></td>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee; color:#f59e0b;'><b>{partial}</b></td>
      </tr>");
            }

            sb.AppendLine($@"
      <tr>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'><b>Başarı Oranı</b></td>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee; color:{rateColor};'><b>%{successRate:F0}</b></td>
      </tr>");

            if (totalBytes > 0)
            {
                sb.AppendLine($@"
      <tr>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'><b>Toplam Veri</b></td>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'>{totalBytes / BytesPerMb:F1} MB</td>
      </tr>");
            }

            if (totalCompressed > 0 && totalBytes > 0)
            {
                double ratio = (1.0 - (double)totalCompressed / totalBytes) * 100;
                sb.AppendLine($@"
      <tr>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'><b>Sıkıştırılmış Toplam</b></td>
        <td style='padding:6px 10px; background:#fff; border:1px solid #eee;'>{totalCompressed / BytesPerMb:F1} MB (%{ratio:F0} kazanç)</td>
      </tr>");
            }

            sb.AppendLine("    </table>");

            // Detay tablosu (en fazla 50 kayıt)
            if (results.Count > 0)
            {
                sb.AppendLine(@"
    <h3 style='margin:20px 0 10px; font-size:15px;'>Yedekleme Detayları</h3>
    <table style='border-collapse:collapse; width:100%; font-size:12px;'>
      <thead>
        <tr style='background:#1a2e1a; color:#fff;'>
          <th style='padding:6px 8px; text-align:left;'>Tarih</th>
          <th style='padding:6px 8px; text-align:left;'>Veritabanı</th>
          <th style='padding:6px 8px; text-align:left;'>Tür</th>
          <th style='padding:6px 8px; text-align:left;'>Durum</th>
          <th style='padding:6px 8px; text-align:right;'>Boyut</th>
          <th style='padding:6px 8px; text-align:right;'>Süre</th>
        </tr>
      </thead>
      <tbody>");

                int rowIndex = 0;
                foreach (var r in results.OrderByDescending(x => x.StartedAt).Take(50))
                {
                    string bg = rowIndex++ % 2 == 0 ? "#fff" : "#f5f5f5";
                    string statusColor = r.Status == BackupResultStatus.Success
                        ? "#10b981"
                        : r.Status == BackupResultStatus.Failed ? "#ef4444" : "#f59e0b";
                    string statusIcon = r.Status == BackupResultStatus.Success ? "✓"
                        : r.Status == BackupResultStatus.Failed ? "✗" : "⚠";
                    string sizeText = r.FileSizeBytes > 0
                        ? $"{r.FileSizeBytes / BytesPerMb:F1} MB"
                        : "-";
                    string durationText = r.Duration.HasValue
                        ? r.Duration.Value.ToString(@"mm\:ss")
                        : "-";

                    sb.AppendLine($@"
        <tr style='background:{bg};'>
          <td style='padding:5px 8px; border-bottom:1px solid #eee;'>{r.StartedAt:dd.MM.yy HH:mm}</td>
          <td style='padding:5px 8px; border-bottom:1px solid #eee;'>{System.Security.SecurityElement.Escape(r.DatabaseName ?? "-")}</td>
          <td style='padding:5px 8px; border-bottom:1px solid #eee;'>{r.BackupType}</td>
          <td style='padding:5px 8px; border-bottom:1px solid #eee; color:{statusColor};'><b>{statusIcon}</b></td>
          <td style='padding:5px 8px; border-bottom:1px solid #eee; text-align:right;'>{sizeText}</td>
          <td style='padding:5px 8px; border-bottom:1px solid #eee; text-align:right;'>{durationText}</td>
        </tr>");
                }

                sb.AppendLine(@"
      </tbody>
    </table>");

                if (results.Count > 50)
                {
                    sb.AppendLine($@"
    <p style='font-size:11px; color:#888; margin-top:6px;'>* Yalnızca son 50 kayıt gösterilmektedir (toplam {results.Count}).</p>");
                }
            }
            else
            {
                sb.AppendLine(@"
    <p style='color:#888; font-size:13px; margin-top:16px;'>Bu dönemde yedekleme kaydı bulunamadı.</p>");
            }

            sb.AppendLine($@"
    <p style='font-size:11px; color:#bbb; margin-top:20px; border-top:1px solid #eee; padding-top:8px;'>
      Bu rapor Koru MsSql Yedek tarafından otomatik oluşturulmuştur. · {DateTime.Now:dd.MM.yyyy HH:mm}
    </p>
  </div>
</div>");

            return sb.ToString();
        }
    }
}
