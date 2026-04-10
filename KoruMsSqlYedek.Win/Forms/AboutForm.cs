#nullable enable
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KoruMsSqlYedek.Win.Theme;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// Hakkında diyalogu — uygulama adı, versiyon, telif hakkı, GitHub bağlantısı
    /// ve runtime bilgisi gösterir. Logo panelinde GDI+ ile kalkan ikonu çizilir.
    /// </summary>
    internal sealed partial class AboutForm : ModernFormBase
    {
        private const string GitHubUrl = "https://github.com/hzkucuk/KoruMsSqlYedek";

        internal AboutForm()
        {
            InitializeComponent();
            ApplyLocalization();
            SetVersionInfo();
            SetRuntimeInfo();
            SetOpenSourceCredits();
            _pnlLogo.Paint += OnLogoPanelPaint;
        }

        private void ApplyLocalization()
        {
            _lblDescription.Text = Helpers.Res.Get("About_Description");
            _lblCopyright.Text = Helpers.Res.Get("About_Copyright");
            _lblDeveloper.Text = Helpers.Res.Get("About_Developer");
            _lblCreditsTitle.Text = Helpers.Res.Get("About_CreditsTitle");
            _btnClose.Text = Helpers.Res.Get("About_Close");
            Text = Helpers.Res.Get("About_Title");
        }

        /// <summary>Assembly versiyonunu label'a yazar.</summary>
        private void SetVersionInfo()
        {
            string ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
            _lblVersion.Text = $"v{ver}";
        }

        /// <summary>.NET runtime ve OS bilgisini gösterir.</summary>
        private void SetRuntimeInfo()
        {
            string runtime = RuntimeInformation.FrameworkDescription;
            string os = RuntimeInformation.OSDescription;
            string arch = RuntimeInformation.ProcessArchitecture.ToString();
            _lblRuntime.Text = $"{runtime} ({arch}) — {os}";
        }

        /// <summary>Açık kaynak kütüphane atıflarını RichTextBox'a yazar.</summary>
        private void SetOpenSourceCredits()
        {
            string[] credits =
            [
                "Quartz.NET — Zamanlama motoru (Apache 2.0)",
                "Microsoft.Data.SqlClient — SQL Server bağlantısı (MIT)",
                "Microsoft.SqlServer.SqlManagementObjects — SMO yedekleme (MIT)",
                "Serilog — Yapısal loglama (Apache 2.0)",
                "Serilog.Sinks.File — Dosya log hedefi (Apache 2.0)",
                "Serilog.Sinks.Console — Konsol log hedefi (Apache 2.0)",
                "Serilog.Extensions.Hosting — Host entegrasyonu (Apache 2.0)",
                "Newtonsoft.Json — JSON serileştirme (MIT)",
                "Autofac — IoC / Dependency Injection (MIT)",
                "Google.Apis.Drive.v3 — Google Drive API (Apache 2.0)",
                "FluentFTP — FTP istemcisi (MIT)",
                "SSH.NET — SFTP istemcisi (MIT)",
                "MailKit — E-posta gönderimi (MIT)",
                "Squid-Box.SevenZipSharp — 7-Zip sıkıştırma (LGPL-2.1)",
                "AlphaVSS — Volume Shadow Copy (MIT)",
                "ObjectListView.Repack.NET6Plus — Gelişmiş ListView (GPL-3.0)",
                "System.Security.Cryptography.ProtectedData — Veri koruma (MIT)",
                "Microsoft.Extensions.Hosting — Generic Host altyapısı (MIT)",
                "Microsoft.Extensions.Hosting.WindowsServices — Windows Servis desteği (MIT)",
            ];

            _rtbCredits.Text = string.Join(Environment.NewLine, credits);
        }

        /// <summary>GitHub linkine tıklandığında varsayılan tarayıcıda açar.</summary>
        private void OnGitHubLinkClick(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(GitHubUrl) { UseShellExecute = true });
            }
            catch
            {
                // Tarayıcı açılamazsa sessizce devam et
            }
        }

        /// <summary>
        /// Logo paneline GDI+ ile emerald kalkan + "K" harfi çizer.
        /// ModernTheme accent renkleri kullanılır — gradient efekti ile.
        /// </summary>
        private void OnLogoPanelPaint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int shieldSize = 80;
            int x = 20;
            int y = (_pnlLogo.Height - shieldSize) / 2;

            DrawShieldIcon(g, x, y, shieldSize);
        }

        /// <summary>
        /// Emerald gradient kalkan ikonu çizer — uygulamanın "Koru" (koruma) temasını yansıtır.
        /// </summary>
        private static void DrawShieldIcon(Graphics g, int x, int y, int size)
        {
            // Kalkan şekli — GraphicsPath ile
            using GraphicsPath shieldPath = CreateShieldPath(x, y, size);

            // Gradient dolgu — emerald green
            using LinearGradientBrush gradientBrush = new(
                new Rectangle(x, y, size, size),
                ModernTheme.AccentPrimary,
                ModernTheme.AccentPrimaryDark,
                LinearGradientMode.Vertical);

            g.FillPath(gradientBrush, shieldPath);

            // Hafif parlak kenar
            using Pen borderPen = new(ModernTheme.AccentPrimaryHover, 1.5f);
            g.DrawPath(borderPen, shieldPath);

            // "K" harfi — beyaz, kalkan ortasında
            float fontSize = size * 0.45f;
            using Font letterFont = new("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using SolidBrush textBrush = new(Color.White);

            StringFormat sf = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            RectangleF textRect = new(x, y + size * 0.05f, size, size);
            g.DrawString("K", letterFont, textBrush, textRect, sf);
        }

        /// <summary>Kalkan (shield) şeklini GraphicsPath olarak oluşturur.</summary>
        private static GraphicsPath CreateShieldPath(int x, int y, int size)
        {
            GraphicsPath path = new();

            float w = size;
            float h = size;
            float cx = x + w / 2;

            // Üst kısım — yuvarlak köşeli dikdörtgen
            float topHeight = h * 0.55f;
            float radius = w * 0.15f;

            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + w - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddLine(x + w, y + radius, x + w, y + topHeight);

            // Alt kısım — sivri uç
            path.AddLine(x + w, y + topHeight, cx, y + h);
            path.AddLine(cx, y + h, x, y + topHeight);

            path.AddLine(x, y + topHeight, x, y + radius);
            path.CloseFigure();

            return path;
        }
    }
}
