using System;
using System.Text;

namespace KoruMsSqlYedek.Engine.Notification
{
    /// <summary>
    /// Tüm e-posta bildirimleri için ortak HTML şablon oluşturucu.
    /// Tutarlı marka görünümü sağlar: koyu header, temiz tablo, footer.
    /// </summary>
    public sealed class EmailTemplateBuilder
    {
        private const string BrandColor = "#10b981";
        private const string HeaderBg = "#1a2e1a";
        private const string SuccessColor = "#10b981";
        private const string FailureColor = "#ef4444";
        private const string WarningColor = "#f59e0b";
        private const string MutedColor = "#888";
        private const string BorderColor = "#e5e7eb";
        private const string AltRowBg = "#f9fafb";
        private const string FontStack = "Segoe UI, -apple-system, BlinkMacSystemFont, Arial, sans-serif";

        private readonly StringBuilder _sb = new();
        private bool _headerWritten;
        private bool _footerWritten;

        /// <summary>
        /// Başarılı durum için renk döndürür.
        /// </summary>
        public static string GetSuccessColor() => SuccessColor;

        /// <summary>
        /// Başarısız durum için renk döndürür.
        /// </summary>
        public static string GetFailureColor() => FailureColor;

        /// <summary>
        /// Uyarı durumu için renk döndürür.
        /// </summary>
        public static string GetWarningColor() => WarningColor;

        /// <summary>
        /// Duruma göre renk döndürür.
        /// </summary>
        public static string GetStatusColor(bool isSuccess) => isSuccess ? SuccessColor : FailureColor;

        /// <summary>
        /// Koyu arka planlı marka başlığı yazar.
        /// </summary>
        public EmailTemplateBuilder WriteHeader(string title, string subtitle = null)
        {
            if (_headerWritten)
                return this;

            _sb.AppendLine($@"<div style=""font-family:{FontStack}; max-width:680px; margin:0 auto; color:#222;"">");
            _sb.AppendLine($@"  <div style=""background:{HeaderBg}; padding:18px 24px; border-radius:8px 8px 0 0;"">");
            _sb.AppendLine($@"    <h2 style=""color:{BrandColor}; margin:0; font-size:18px; font-weight:600;"">🛡️ {Encode(title)}</h2>");

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                _sb.AppendLine($@"    <p style=""color:#aaa; margin:4px 0 0; font-size:13px;"">{Encode(subtitle)}</p>");
            }

            _sb.AppendLine("  </div>");
            _sb.AppendLine($@"  <div style=""background:#fff; padding:20px 24px; border:1px solid {BorderColor}; border-top:none;"">");

            _headerWritten = true;
            return this;
        }

        /// <summary>
        /// Bölüm başlığı ekler.
        /// </summary>
        public EmailTemplateBuilder WriteSectionTitle(string title)
        {
            _sb.AppendLine($@"    <h3 style=""margin:20px 0 10px; font-size:15px; color:#333; border-bottom:2px solid {BrandColor}; padding-bottom:6px;"">{Encode(title)}</h3>");
            return this;
        }

        /// <summary>
        /// Durum rozeti (büyük, renkli) ekler.
        /// </summary>
        public EmailTemplateBuilder WriteStatusBadge(string text, bool isSuccess)
        {
            string color = isSuccess ? SuccessColor : FailureColor;
            string icon = isSuccess ? "✓" : "✗";
            _sb.AppendLine($@"    <div style=""background:{color}15; border-left:4px solid {color}; padding:12px 16px; margin:0 0 16px; border-radius:0 6px 6px 0;"">");
            _sb.AppendLine($@"      <span style=""color:{color}; font-size:16px; font-weight:600;"">{icon} {Encode(text)}</span>");
            _sb.AppendLine("    </div>");
            return this;
        }

        /// <summary>
        /// Özet tablosu başlatır. <see cref="WriteTableRow"/> ile satır eklenmelidir.
        /// <see cref="EndTable"/> ile kapatılır.
        /// </summary>
        public EmailTemplateBuilder BeginSummaryTable()
        {
            _sb.AppendLine($@"    <table style=""border-collapse:collapse; width:100%; font-size:13px; margin-bottom:16px;"">");
            return this;
        }

        /// <summary>
        /// Özet tablosuna anahtar–değer satırı ekler.
        /// </summary>
        public EmailTemplateBuilder WriteTableRow(string label, string value, string valueColor = null)
        {
            string colorStyle = !string.IsNullOrEmpty(valueColor)
                ? $" color:{valueColor}; font-weight:600;"
                : string.Empty;

            _sb.AppendLine($@"      <tr>");
            _sb.AppendLine($@"        <td style=""padding:8px 12px; background:#f9f9f9; border:1px solid {BorderColor}; width:40%; font-weight:500;"">{Encode(label)}</td>");
            _sb.AppendLine($@"        <td style=""padding:8px 12px; background:#fff; border:1px solid {BorderColor};{colorStyle}"">{value}</td>");
            _sb.AppendLine("      </tr>");
            return this;
        }

        /// <summary>
        /// Tabloyu kapatır.
        /// </summary>
        public EmailTemplateBuilder EndTable()
        {
            _sb.AppendLine("    </table>");
            return this;
        }

        /// <summary>
        /// Koyu başlıklı detay tablosu başlatır.
        /// </summary>
        public EmailTemplateBuilder BeginDetailTable(params string[] columns)
        {
            _sb.AppendLine($@"    <table style=""border-collapse:collapse; width:100%; font-size:12px;"">");
            _sb.AppendLine($@"      <thead><tr style=""background:{HeaderBg}; color:#fff;"">");

            foreach (string col in columns)
            {
                _sb.AppendLine($@"        <th style=""padding:8px 10px; text-align:left;"">{Encode(col)}</th>");
            }

            _sb.AppendLine("      </tr></thead>");
            _sb.AppendLine("      <tbody>");
            return this;
        }

        /// <summary>
        /// Detay tablosuna satır ekler. Zebra deseni otomatik uygulanır.
        /// </summary>
        public EmailTemplateBuilder WriteDetailRow(int rowIndex, params (string Text, string Color)[] cells)
        {
            string bg = rowIndex % 2 == 0 ? "#fff" : AltRowBg;
            _sb.AppendLine($@"      <tr style=""background:{bg};"">");

            foreach (var (text, color) in cells)
            {
                string colorStyle = !string.IsNullOrEmpty(color) ? $" color:{color}; font-weight:600;" : string.Empty;
                _sb.AppendLine($@"        <td style=""padding:6px 10px; border-bottom:1px solid {BorderColor};{colorStyle}"">{text}</td>");
            }

            _sb.AppendLine("      </tr>");
            return this;
        }

        /// <summary>
        /// Detay tablosu gövdesini ve tabloyu kapatır.
        /// </summary>
        public EmailTemplateBuilder EndDetailTable()
        {
            _sb.AppendLine("      </tbody>");
            _sb.AppendLine("    </table>");
            return this;
        }

        /// <summary>
        /// Hata mesajı bloğu ekler. Null/boş mesaj güvenle atlanır.
        /// </summary>
        public EmailTemplateBuilder WriteErrorBlock(string sanitizedMessage)
        {
            if (string.IsNullOrWhiteSpace(sanitizedMessage))
                return this;

            _sb.AppendLine($@"    <div style=""background:#fef2f2; border-left:4px solid {FailureColor}; padding:12px 16px; margin:12px 0; border-radius:0 6px 6px 0;"">");
            _sb.AppendLine($@"      <span style=""color:{FailureColor}; font-size:13px;""><b>Hata:</b> {sanitizedMessage}</span>");
            _sb.AppendLine("    </div>");
            return this;
        }

        /// <summary>
        /// Bilgi notu bloğu ekler.
        /// </summary>
        public EmailTemplateBuilder WriteInfoBlock(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return this;

            _sb.AppendLine($@"    <p style=""color:{MutedColor}; font-size:12px; margin:8px 0;"">{Encode(message)}</p>");
            return this;
        }

        /// <summary>
        /// Ham HTML ekler (önceden oluşturulmuş içerik için).
        /// </summary>
        public EmailTemplateBuilder WriteRawHtml(string html)
        {
            _sb.AppendLine(html);
            return this;
        }

        /// <summary>
        /// Footer ve kapanış etiketlerini yazar.
        /// </summary>
        public EmailTemplateBuilder WriteFooter()
        {
            if (_footerWritten)
                return this;

            _sb.AppendLine($@"    <p style=""font-size:11px; color:#bbb; margin-top:24px; border-top:1px solid {BorderColor}; padding-top:10px;"">");
            _sb.AppendLine($@"      Bu e-posta <b>Koru MsSql Yedek</b> tarafından otomatik gönderilmiştir. · {DateTime.Now:dd.MM.yyyy HH:mm}");
            _sb.AppendLine("    </p>");
            _sb.AppendLine("  </div>");
            _sb.AppendLine("</div>");

            _footerWritten = true;
            return this;
        }

        /// <summary>
        /// Oluşturulan HTML çıktısını döndürür. Footer yazılmadıysa otomatik eklenir.
        /// </summary>
        public string Build()
        {
            if (!_footerWritten)
                WriteFooter();

            return _sb.ToString();
        }

        /// <summary>
        /// HTML encode uygular — XSS önlemi.
        /// </summary>
        public static string Encode(string text) =>
            string.IsNullOrEmpty(text) ? string.Empty : System.Net.WebUtility.HtmlEncode(text);
    }
}
