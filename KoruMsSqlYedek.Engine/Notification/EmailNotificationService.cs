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
    public partial class EmailNotificationService : INotificationService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<EmailNotificationService>();
        private const double BytesPerMb = 1048576.0;

        private readonly IAppSettingsManager _settingsManager;

        public EmailNotificationService(IAppSettingsManager settingsManager)
        {
            ArgumentNullException.ThrowIfNull(settingsManager);
            _settingsManager = settingsManager;
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

        /// <summary>
        /// Sıkıştırma oranını hesaplar (örn. "%93.4").
        /// </summary>
        private static string FmtRatio(long original, long compressed)
        {
            if (original <= 0 || compressed <= 0) return "";
            double ratio = (1.0 - (double)compressed / original) * 100;
            return $"%{ratio:F1}";
        }
    }
}
