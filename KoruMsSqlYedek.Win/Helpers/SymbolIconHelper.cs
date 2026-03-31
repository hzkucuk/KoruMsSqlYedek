using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Helpers
{
    /// <summary>
    /// Modern circular badge tarzı tray ikonu oluşturur.
    /// GDI+ gradientleri ile yüksek kontrastlı, DPI-aware ikonlar üretir.
    /// </summary>
    internal static class SymbolIconHelper
    {
        private const string PrimaryFontFamily = "Segoe MDL2 Assets";
        private const string FallbackFontFamily = "Segoe UI Symbol";

        // Sık kullanılan semboller
        internal const string SymbolDatabase = "\uE74E";
        internal const string SymbolSettings = "\uE713";
        internal const string SymbolFolder = "\uE8B7";
        internal const string SymbolInfo = "\uE946";
        internal const string SymbolExit = "\uE711";
        internal const string SymbolCloudUpload = "\uE753";
        internal const string SymbolCheckmark = "\uE73E";
        internal const string SymbolWarning = "\uE7BA";
        internal const string SymbolError = "\uE783";
        internal const string SymbolRefresh = "\uE72C";
        internal const string SymbolDocument = "\uE8A5";
        internal const string SymbolClock = "\uE823";
        internal const string SymbolHome = "\uE80F";
        internal const string SymbolShield = "\uE83D";

        /// <summary>DPI uyumlu tray ikon boyutu.</summary>
        private static int TrayIconSize => Math.Max(SystemInformation.SmallIconSize.Width, 20);

        /// <summary>
        /// Belirtilen sembolü Icon olarak render eder (NotifyIcon için).
        /// </summary>
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
        /// Tray ikonu için varsayılan ikonunu oluşturur (DPI-aware).
        /// </summary>
        internal static Icon CreateTrayIcon()
        {
            return CreateBadgeIcon(SymbolShield, Color.FromArgb(0, 180, 90), Color.FromArgb(0, 120, 60));
        }

        /// <summary>
        /// Durum bazlı tray ikonu oluşturur — modern daire badge stili (DPI-aware).
        /// </summary>
        internal static Icon CreateStatusIcon(TrayIconStatus status)
        {
            switch (status)
            {
                case TrayIconStatus.Idle:
                    return CreateBadgeIcon(SymbolShield, Color.FromArgb(0, 180, 90), Color.FromArgb(0, 120, 60));
                case TrayIconStatus.Running:
                    return CreateBadgeIcon(SymbolCloudUpload, Color.FromArgb(33, 150, 243), Color.FromArgb(13, 71, 161));
                case TrayIconStatus.Success:
                    return CreateBadgeIcon(SymbolCheckmark, Color.FromArgb(76, 175, 80), Color.FromArgb(27, 94, 32));
                case TrayIconStatus.Warning:
                    return CreateBadgeIcon(SymbolWarning, Color.FromArgb(255, 193, 7), Color.FromArgb(230, 150, 0));
                case TrayIconStatus.Error:
                    return CreateBadgeIcon(SymbolError, Color.FromArgb(244, 67, 54), Color.FromArgb(183, 28, 28));
                case TrayIconStatus.Disconnected:
                    return CreateBadgeIcon(SymbolWarning, Color.FromArgb(120, 120, 120), Color.FromArgb(66, 66, 66));
                default:
                    return CreateTrayIcon();
            }
        }

        /// <summary>
        /// Modern circular badge ikonu — gradient arka plan + beyaz sembol.
        /// Koyu ve açık taskbar'larda yüksek kontrast sağlar.
        /// </summary>
        private static Icon CreateBadgeIcon(string symbol, Color gradientTop, Color gradientBottom)
        {
            int size = TrayIconSize;
            using (var bitmap = new Bitmap(size, size))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float pad = size * 0.04f;
                var badgeRect = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);

                // Dış koyu halka (shadow/depth efekti)
                using (var shadowPath = CreateRoundedRect(new RectangleF(0, 0.5f, size, size), size / 2f))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Ana daire — gradient arka plan
                using (var circlePath = CreateRoundedRect(badgeRect, badgeRect.Width / 2f))
                using (var gradBrush = new LinearGradientBrush(
                    badgeRect, gradientTop, gradientBottom, LinearGradientMode.ForwardDiagonal))
                {
                    g.FillPath(gradBrush, circlePath);
                }

                // İnce parlak kenar (iç highlight)
                using (var innerRect = CreateRoundedRect(
                    new RectangleF(pad + 0.5f, pad + 0.5f, badgeRect.Width - 1, badgeRect.Height - 1),
                    (badgeRect.Width - 1) / 2f))
                using (var highlightPen = new Pen(Color.FromArgb(60, 255, 255, 255), 0.7f))
                {
                    g.DrawPath(highlightPen, innerRect);
                }

                // Beyaz sembol
                float fontSize = size * 0.52f;
                string fontFamily = IsFontAvailable(PrimaryFontFamily) ? PrimaryFontFamily : FallbackFontFamily;
                using (var font = new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    // Sembol gölgesi
                    using (var sBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                    {
                        g.DrawString(symbol, font, sBrush,
                            new RectangleF(0.5f, 0.5f, size, size), sf);
                    }

                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(symbol, font, brush,
                            new RectangleF(0, 0, size, size), sf);
                    }
                }

                var hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        /// <summary>
        /// Yedekleme animasyonu için modern dönen ark kareleri oluşturur.
        /// Mavi gradient daire + üzerinde dönen beyaz ark (progress spinner).
        /// </summary>
        internal static Icon[] CreateAnimationFrames(int frameCount = 12)
        {
            int size = TrayIconSize;
            var frames = new Icon[frameCount];
            float sweepAngle = 270f;
            float step = 360f / frameCount;

            Color bgTop = Color.FromArgb(33, 150, 243);
            Color bgBottom = Color.FromArgb(13, 71, 161);

            for (int i = 0; i < frameCount; i++)
            {
                using (var bitmap = new Bitmap(size, size))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);

                    float pad = size * 0.04f;
                    var badgeRect = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);

                    // Arka plan dairesi
                    using (var circlePath = CreateRoundedRect(badgeRect, badgeRect.Width / 2f))
                    using (var gradBrush = new LinearGradientBrush(
                        badgeRect, bgTop, bgBottom, LinearGradientMode.ForwardDiagonal))
                    {
                        g.FillPath(gradBrush, circlePath);
                    }

                    // Dönen ark — yarı-şeffaf beyaz track
                    float arcPad = size * 0.18f;
                    var arcRect = new RectangleF(arcPad, arcPad, size - arcPad * 2, size - arcPad * 2);

                    using (var trackPen = new Pen(Color.FromArgb(40, 255, 255, 255), Math.Max(size * 0.1f, 1.5f)))
                    {
                        trackPen.StartCap = LineCap.Round;
                        trackPen.EndCap = LineCap.Round;
                        g.DrawArc(trackPen, arcRect, 0, 360);
                    }

                    // Aktif dönen ark — parlak beyaz
                    float startAngle = i * step - 90;
                    using (var arcPen = new Pen(Color.FromArgb(240, 255, 255, 255), Math.Max(size * 0.12f, 2f)))
                    {
                        arcPen.StartCap = LineCap.Round;
                        arcPen.EndCap = LineCap.Round;
                        g.DrawArc(arcPen, arcRect, startAngle, sweepAngle);
                    }

                    var hIcon = bitmap.GetHicon();
                    frames[i] = Icon.FromHandle(hIcon);
                }
            }

            return frames;
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

        /// <summary>Yuvarlak köşeli dikdörtgen GraphicsPath oluşturur.</summary>
        private static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
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
