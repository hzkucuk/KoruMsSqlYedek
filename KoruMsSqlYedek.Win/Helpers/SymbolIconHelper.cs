using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Helpers
{
    /// <summary>
    /// Segoe UI Symbol / Segoe MDL2 Assets fontundan ikon render eden yardımcı sınıf.
    /// İkon kütüphanesi yerine font tabanlı semboller kullanılır.
    /// DPI-aware: SystemInformation.SmallIconSize ile doğru boyutta render eder.
    /// </summary>
    internal static class SymbolIconHelper
    {
        private const string PrimaryFontFamily = "Segoe MDL2 Assets";
        private const string FallbackFontFamily = "Segoe UI Symbol";

        // Sık kullanılan semboller
        /// <summary>Veritabanı simgesi (💾 — floppy disk).</summary>
        internal const string SymbolDatabase = "\uE74E";
        /// <summary>Ayarlar (⚙ — gear).</summary>
        internal const string SymbolSettings = "\uE713";
        /// <summary>Klasör.</summary>
        internal const string SymbolFolder = "\uE8B7";
        /// <summary>Bilgi.</summary>
        internal const string SymbolInfo = "\uE946";
        /// <summary>Çıkış (X).</summary>
        internal const string SymbolExit = "\uE711";
        /// <summary>Bulut upload.</summary>
        internal const string SymbolCloudUpload = "\uE753";
        /// <summary>Onay (✓).</summary>
        internal const string SymbolCheckmark = "\uE73E";
        /// <summary>Uyarı (⚠).</summary>
        internal const string SymbolWarning = "\uE7BA";
        /// <summary>Hata (✕).</summary>
        internal const string SymbolError = "\uE783";
        /// <summary>Yenile.</summary>
        internal const string SymbolRefresh = "\uE72C";
        /// <summary>Log / belge.</summary>
        internal const string SymbolDocument = "\uE8A5";
        /// <summary>Zamanlama / saat.</summary>
        internal const string SymbolClock = "\uE823";
        /// <summary>Dashboard / home.</summary>
        internal const string SymbolHome = "\uE80F";
        /// <summary>Kalkan (güvenlik).</summary>
        internal const string SymbolShield = "\uE83D";

        /// <summary>DPI uyumlu tray ikon boyutu.</summary>
        private static int TrayIconSize => Math.Max(SystemInformation.SmallIconSize.Width, 20);

        /// <summary>
        /// Belirtilen sembolü Icon olarak render eder (NotifyIcon için).
        /// </summary>
        /// <param name="symbol">Unicode sembol karakteri.</param>
        /// <param name="size">İkon boyutu (piksel). 0 verilirse DPI-aware boyut kullanılır.</param>
        /// <param name="foreColor">Sembol rengi.</param>
        /// <param name="backColor">Arka plan rengi (varsayılan: şeffaf).</param>
        internal static Icon RenderIcon(string symbol, int size = 0, Color? foreColor = null, Color? backColor = null)
        {
            int effectiveSize = size > 0 ? size : TrayIconSize;
            using (var bitmap = RenderBitmap(symbol, effectiveSize, effectiveSize, foreColor, backColor))
            {
                var hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        /// <summary>
        /// Belirtilen sembolü Bitmap olarak render eder (menü ikonları için).
        /// </summary>
        /// <param name="symbol">Unicode sembol karakteri.</param>
        /// <param name="width">Bitmap genişliği.</param>
        /// <param name="height">Bitmap yüksekliği.</param>
        /// <param name="foreColor">Sembol rengi.</param>
        /// <param name="backColor">Arka plan rengi (varsayılan: şeffaf).</param>
        internal static Bitmap RenderBitmap(string symbol, int width = 16, int height = 16,
            Color? foreColor = null, Color? backColor = null)
        {
            var fgColor = foreColor ?? Color.White;
            var bgColor = backColor ?? Color.Transparent;

            var bitmap = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                g.Clear(bgColor);

                float fontSize = width * 0.78f;
                string fontFamily = IsFontAvailable(PrimaryFontFamily) ? PrimaryFontFamily : FallbackFontFamily;

                using (var font = new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    var rect = new RectangleF(0, 0, width, height);

                    // Kontrast için yarı-şeffaf koyu gölge (özellikle açık taskbar'larda)
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                    {
                        var shadowRect = new RectangleF(0.5f, 0.5f, width, height);
                        g.DrawString(symbol, font, shadowBrush, shadowRect, sf);
                    }

                    using (var brush = new SolidBrush(fgColor))
                    {
                        g.DrawString(symbol, font, brush, rect, sf);
                    }
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Tray ikonu için varsayılan veritabanı/kalkan ikonunu oluşturur (DPI-aware).
        /// </summary>
        internal static Icon CreateTrayIcon()
        {
            return RenderIcon(SymbolShield, TrayIconSize, Color.FromArgb(0, 230, 118));
        }

        /// <summary>
        /// Durum bazlı tray ikonu oluşturur (DPI-aware).
        /// </summary>
        internal static Icon CreateStatusIcon(TrayIconStatus status)
        {
            int size = TrayIconSize;
            switch (status)
            {
                case TrayIconStatus.Idle:
                    return RenderIcon(SymbolShield, size, Color.FromArgb(0, 230, 118));
                case TrayIconStatus.Running:
                    return RenderIcon(SymbolCloudUpload, size, Color.FromArgb(66, 165, 245));
                case TrayIconStatus.Success:
                    return RenderIcon(SymbolCheckmark, size, Color.FromArgb(102, 187, 106));
                case TrayIconStatus.Warning:
                    return RenderIcon(SymbolWarning, size, Color.FromArgb(255, 213, 79));
                case TrayIconStatus.Error:
                    return RenderIcon(SymbolError, size, Color.FromArgb(255, 82, 82));
                case TrayIconStatus.Disconnected:
                    return RenderIcon(SymbolWarning, size, Color.FromArgb(176, 176, 176));
                default:
                    return CreateTrayIcon();
            }
        }

        /// <summary>
        /// Belirtilen sembolü belirtilen açıda döndürülerek Icon olarak render eder (animasyon için).
        /// </summary>
        internal static Icon RenderRotatedIcon(string symbol, int size, Color foreColor, float angleDegrees)
        {
            using (var bitmap = new Bitmap(size, size))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                g.TranslateTransform(size / 2f, size / 2f);
                g.RotateTransform(angleDegrees);
                g.TranslateTransform(-size / 2f, -size / 2f);

                float fontSize = size * 0.78f;
                string fontFamily = IsFontAvailable(PrimaryFontFamily) ? PrimaryFontFamily : FallbackFontFamily;
                using (var font = new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    // Gölge
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                    {
                        g.DrawString(symbol, font, shadowBrush, new RectangleF(0.5f, 0.5f, size, size), sf);
                    }

                    using (var brush = new SolidBrush(foreColor))
                    {
                        g.DrawString(symbol, font, brush, new RectangleF(0, 0, size, size), sf);
                    }
                }

                var hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        /// <summary>
        /// Yedekleme animasyonu için dönen ikon karelerini oluşturur.
        /// DPI-aware boyut, parlak mavi tonlarıyla yüksek kontrast.
        /// </summary>
        internal static Icon[] CreateAnimationFrames(int frameCount = 8)
        {
            int size = TrayIconSize;
            var frames = new Icon[frameCount];
            float step = 360f / frameCount;

            for (int i = 0; i < frameCount; i++)
            {
                // Her kare için parlaklığı pulse et — daha görünür animasyon
                int brightness = 180 + (int)(75 * Math.Sin(i * Math.PI * 2 / frameCount));
                var color = Color.FromArgb(
                    Math.Clamp(brightness - 120, 0, 255),
                    Math.Clamp(brightness, 0, 255),
                    255);
                frames[i] = RenderRotatedIcon(SymbolRefresh, size, color, i * step);
            }

            return frames;
        }

        private static bool IsFontAvailable(string familyName)
        {
            using (var testFont = new Font(familyName, 10f, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                return string.Equals(testFont.Name, familyName, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Tray ikonu durum göstergesi.
    /// </summary>
    internal enum TrayIconStatus
    {
        Idle,
        Running,
        Success,
        Warning,
        Error,
        Disconnected
    }
}
