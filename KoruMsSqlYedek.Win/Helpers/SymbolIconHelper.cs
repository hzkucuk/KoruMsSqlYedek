using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
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
                return CloneIconFromBitmap(bitmap);
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
        /// Bitmap'ten yönetilen (managed) Icon oluşturur — native handle sızıntısını önler.
        /// </summary>
        private static Icon CloneIconFromBitmap(Bitmap bitmap)
        {
            var hIcon = bitmap.GetHicon();
            var icon = (Icon)Icon.FromHandle(hIcon).Clone();
            NativeMethods.DestroyIcon(hIcon);
            return icon;
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
        /// Modern circular badge ikonu — flat gradient arka plan + beyaz sembol.
        /// Windows 11 taskbar'da (koyu/açık) yüksek kontrast sağlar.
        /// </summary>
        private static Icon CreateBadgeIcon(string symbol, Color gradientTop, Color gradientBottom)
        {
            int size = TrayIconSize;
            using (var bitmap = new Bitmap(size, size))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                float pad = size * 0.06f;
                var badgeRect = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);

                // Hafif dış gölge (modern flat stil)
                using (var shadowPath = CreateRoundedRect(new RectangleF(pad, pad + 0.5f, badgeRect.Width, badgeRect.Height), badgeRect.Width / 2f))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Ana daire — yumuşak gradient
                using (var circlePath = CreateRoundedRect(badgeRect, badgeRect.Width / 2f))
                using (var gradBrush = new LinearGradientBrush(
                    badgeRect, gradientTop, gradientBottom, LinearGradientMode.Vertical))
                {
                    g.FillPath(gradBrush, circlePath);
                }

                // İnce yarı-şeffaf kenar (taskbar kontrastı)
                using (var borderPath = CreateRoundedRect(badgeRect, badgeRect.Width / 2f))
                using (var borderPen = new Pen(Color.FromArgb(80, 255, 255, 255), 0.8f))
                {
                    g.DrawPath(borderPen, borderPath);
                }

                // Beyaz sembol — gölgesiz, temiz
                float fontSize = size * 0.50f;
                string fontFamily = IsFontAvailable(PrimaryFontFamily) ? PrimaryFontFamily : FallbackFontFamily;
                using (var font = new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(symbol, font, brush,
                            new RectangleF(0, 0, size, size), sf);
                    }
                }

                return CloneIconFromBitmap(bitmap);
            }
        }

        /// <summary>
        /// Yedekleme animasyonu için modern dönen spinner kareleri oluşturur.
        /// Windows 11 tarzı mavi daire + ince dönen beyaz ark (progress spinner).
        /// </summary>
        internal static Icon[] CreateAnimationFrames(int frameCount = 12)
        {
            int size = TrayIconSize;
            var frames = new Icon[frameCount];
            float sweepAngle = 120f;  // Daha kısa ark — modern spinner stili
            float step = 360f / frameCount;

            Color bgTop = Color.FromArgb(56, 152, 236);   // Windows 11 accent blue
            Color bgBottom = Color.FromArgb(24, 90, 189);

            for (int i = 0; i < frameCount; i++)
            {
                using (var bitmap = new Bitmap(size, size))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);

                    float pad = size * 0.06f;
                    var badgeRect = new RectangleF(pad, pad, size - pad * 2, size - pad * 2);

                    // Arka plan dairesi — yumuşak gradient
                    using (var circlePath = CreateRoundedRect(badgeRect, badgeRect.Width / 2f))
                    using (var gradBrush = new LinearGradientBrush(
                        badgeRect, bgTop, bgBottom, LinearGradientMode.Vertical))
                    {
                        g.FillPath(gradBrush, circlePath);
                    }

                    // Kenar — yarı şeffaf beyaz
                    using (var borderPath = CreateRoundedRect(badgeRect, badgeRect.Width / 2f))
                    using (var borderPen = new Pen(Color.FromArgb(50, 255, 255, 255), 0.6f))
                    {
                        g.DrawPath(borderPen, borderPath);
                    }

                    // Dönen ark — ince yarı-şeffaf beyaz track (360°)
                    float arcPad = size * 0.20f;
                    var arcRect = new RectangleF(arcPad, arcPad, size - arcPad * 2, size - arcPad * 2);
                    float arcThickness = Math.Max(size * 0.09f, 1.5f);

                    using (var trackPen = new Pen(Color.FromArgb(35, 255, 255, 255), arcThickness))
                    {
                        trackPen.StartCap = LineCap.Round;
                        trackPen.EndCap = LineCap.Round;
                        g.DrawArc(trackPen, arcRect, 0, 360);
                    }

                    // Aktif dönen ark — parlak beyaz, kısa yay
                    float startAngle = i * step - 90;
                    using (var arcPen = new Pen(Color.FromArgb(230, 255, 255, 255), arcThickness))
                    {
                        arcPen.StartCap = LineCap.Round;
                        arcPen.EndCap = LineCap.Round;
                        g.DrawArc(arcPen, arcRect, startAngle, sweepAngle);
                    }

                    frames[i] = CloneIconFromBitmap(bitmap);
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

                return CloneIconFromBitmap(bitmap);
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

        /// <summary>
        /// Gömülü animated GIF dosyasından tray ikonu boyutuna uygun Icon[] çıkarır.
        /// </summary>
        /// <param name="gifResourceName">Embedded resource adı (örn. "CloudSync.gif")</param>
        internal static Icon[] ExtractGifFrames(string gifResourceName)
        {
            var asm = typeof(SymbolIconHelper).Assembly;
            string fullName = $"KoruMsSqlYedek.Win.Resources.TrayIcons.{gifResourceName}";
            using var stream = asm.GetManifestResourceStream(fullName);
            if (stream is null)
                return CreateAnimationFrames(); // Fallback — spinner

            return ExtractGifFramesFromStream(stream);
        }

        /// <summary>
        /// Stream'den animated GIF karelerini tray ikonu boyutuna ölçekleyerek Icon[] döndürür.
        /// </summary>
        private static Icon[] ExtractGifFramesFromStream(Stream stream)
        {
            int targetSize = TrayIconSize;

            using var gif = Image.FromStream(stream);
            var dimension = new FrameDimension(gif.FrameDimensionsList[0]);
            int frameCount = gif.GetFrameCount(dimension);

            if (frameCount <= 0)
                return CreateAnimationFrames(); // Fallback

            var frames = new Icon[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                gif.SelectActiveFrame(dimension, i);

                using var resized = new Bitmap(targetSize, targetSize);
                using (var g = Graphics.FromImage(resized))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);
                    g.DrawImage(gif, 0, 0, targetSize, targetSize);
                }

                frames[i] = CloneIconFromBitmap(resized);
            }

            return frames;
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
