using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace MikroSqlDbYedek.Win.Helpers
{
    /// <summary>
    /// Segoe UI Symbol / Segoe MDL2 Assets fontundan ikon render eden yardımcı sınıf.
    /// İkon kütüphanesi yerine font tabanlı semboller kullanılır.
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

        /// <summary>
        /// Belirtilen sembolü Icon olarak render eder (NotifyIcon için).
        /// </summary>
        /// <param name="symbol">Unicode sembol karakteri.</param>
        /// <param name="size">İkon boyutu (piksel).</param>
        /// <param name="foreColor">Sembol rengi.</param>
        /// <param name="backColor">Arka plan rengi (varsayılan: şeffaf).</param>
        internal static Icon RenderIcon(string symbol, int size = 16, Color? foreColor = null, Color? backColor = null)
        {
            using (var bitmap = RenderBitmap(symbol, size, size, foreColor, backColor))
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
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                if (bgColor != Color.Transparent)
                {
                    g.Clear(bgColor);
                }
                else
                {
                    g.Clear(Color.Transparent);
                }

                float fontSize = width * 0.7f;
                string fontFamily = IsFontAvailable(PrimaryFontFamily) ? PrimaryFontFamily : FallbackFontFamily;

                using (var font = new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(fgColor))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    var rect = new RectangleF(0, 0, width, height);
                    g.DrawString(symbol, font, brush, rect, sf);
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Tray ikonu için varsayılan veritabanı/kalkan ikonunu oluşturur.
        /// </summary>
        internal static Icon CreateTrayIcon()
        {
            return RenderIcon(SymbolShield, 16, Color.FromArgb(0, 200, 83));
        }

        /// <summary>
        /// Durum bazlı tray ikonu oluşturur.
        /// </summary>
        internal static Icon CreateStatusIcon(TrayIconStatus status)
        {
            switch (status)
            {
                case TrayIconStatus.Idle:
                    return RenderIcon(SymbolShield, 16, Color.FromArgb(0, 200, 83));
                case TrayIconStatus.Running:
                    return RenderIcon(SymbolCloudUpload, 16, Color.FromArgb(33, 150, 243));
                case TrayIconStatus.Success:
                    return RenderIcon(SymbolCheckmark, 16, Color.FromArgb(76, 175, 80));
                case TrayIconStatus.Warning:
                    return RenderIcon(SymbolWarning, 16, Color.FromArgb(255, 193, 7));
                case TrayIconStatus.Error:
                    return RenderIcon(SymbolError, 16, Color.FromArgb(244, 67, 54));
                default:
                    return CreateTrayIcon();
            }
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
        Error
    }
}
