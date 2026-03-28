using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern toast bildirim — sağ alt köşede beliren geçici bildirim paneli.
    /// Yedekleme başarı/hata bildirimleri için kullanılır.
    /// </summary>
    internal class ModernToast : Form
    {
        private readonly Timer _fadeTimer;
        private readonly Timer _closeTimer;
        private float _opacity = 0f;
        private bool _fadingIn = true;
        private readonly string _message;
        private readonly string _title;
        private readonly ToastType _toastType;
        private const int ToastWidth = 340;
        private const int ToastHeight = 80;
        private const int Radius = 10;

        private ModernToast(string title, string message, ToastType type, int durationMs)
        {
            _title = title ?? string.Empty;
            _message = message ?? string.Empty;
            _toastType = type;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(ToastWidth, ToastHeight);
            BackColor = Color.Magenta;
            TransparencyKey = Color.Magenta;
            Opacity = 0;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer,
                true);

            // Pozisyon — sağ alt köşe
            var workingArea = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(
                workingArea.Right - ToastWidth - 16,
                workingArea.Bottom - ToastHeight - 16);

            // Fade-in timer
            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += OnFadeTick;

            // Otomatik kapatma timer
            _closeTimer = new Timer { Interval = durationMs };
            _closeTimer.Tick += (s, e) =>
            {
                _closeTimer.Stop();
                _fadingIn = false;
                _fadeTimer.Start();
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _fadeTimer.Start();
        }

        private void OnFadeTick(object sender, EventArgs e)
        {
            if (_fadingIn)
            {
                _opacity += 0.08f;
                if (_opacity >= 1.0f)
                {
                    _opacity = 1.0f;
                    _fadeTimer.Stop();
                    _closeTimer.Start();
                }
            }
            else
            {
                _opacity -= 0.06f;
                if (_opacity <= 0f)
                {
                    _opacity = 0f;
                    _fadeTimer.Stop();
                    Close();
                    return;
                }
            }

            Opacity = _opacity;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            Color accentColor;
            string iconSymbol;
            GetTypeVisuals(out accentColor, out iconSymbol);

            // Gölge
            var shadowRect = new Rectangle(2, 2, rect.Width, rect.Height);
            using (var shadowPath = ModernTheme.CreateRoundedRectanglePath(shadowRect, Radius))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            // Ana arkaplan
            using (var bgPath = ModernTheme.CreateRoundedRectanglePath(rect, Radius))
            {
                using (var bgBrush = new SolidBrush(ModernTheme.SurfaceColor))
                {
                    g.FillPath(bgBrush, bgPath);
                }

                // Sol accent şerit
                var accentRect = new Rectangle(0, 0, 5, Height);
                using (var accentPath = ModernTheme.CreateRoundedRectanglePath(
                    new Rectangle(0, 0, Radius * 2, Height), Radius))
                {
                    g.SetClip(new Rectangle(0, 0, 5, Height));
                    using (var accentBrush = new SolidBrush(accentColor))
                    {
                        g.FillPath(accentBrush, bgPath);
                    }
                    g.ResetClip();
                }
            }

            // İkon
            using (var iconFont = new Font("Segoe MDL2 Assets", 16f))
            using (var iconBrush = new SolidBrush(accentColor))
            {
                g.DrawString(iconSymbol, iconFont, iconBrush, 16, 18);
            }

            // Başlık
            using (var titleBrush = new SolidBrush(ModernTheme.TextPrimary))
            {
                g.DrawString(_title, ModernTheme.FontBodyBold, titleBrush, 48, 14);
            }

            // Mesaj
            using (var msgBrush = new SolidBrush(ModernTheme.TextSecondary))
            {
                var msgRect = new RectangleF(48, 36, Width - 64, Height - 42);
                var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                g.DrawString(_message, ModernTheme.FontCaption, msgBrush, msgRect, sf);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            _fadingIn = false;
            _fadeTimer.Start();
        }

        private void GetTypeVisuals(out Color color, out string icon)
        {
            switch (_toastType)
            {
                case ToastType.Success:
                    color = ModernTheme.StatusSuccess;
                    icon = Helpers.SymbolIconHelper.SymbolCheckmark;
                    break;
                case ToastType.Warning:
                    color = ModernTheme.StatusWarning;
                    icon = Helpers.SymbolIconHelper.SymbolWarning;
                    break;
                case ToastType.Error:
                    color = ModernTheme.StatusError;
                    icon = Helpers.SymbolIconHelper.SymbolError;
                    break;
                default:
                    color = ModernTheme.StatusInfo;
                    icon = Helpers.SymbolIconHelper.SymbolInfo;
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fadeTimer?.Stop();
                _fadeTimer?.Dispose();
                _closeTimer?.Stop();
                _closeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ═══════════════ STATIC FACTORY ═══════════════

        /// <summary>Toast bildirim gösterir. Fire-and-forget.</summary>
        internal static void Show(string title, string message, ToastType type = ToastType.Info, int durationMs = 4000)
        {
            var toast = new ModernToast(title, message, type, durationMs);
            toast.Show();
        }

        /// <summary>Başarı bildirimi.</summary>
        internal static void Success(string title, string message)
        {
            Show(title, message, ToastType.Success);
        }

        /// <summary>Hata bildirimi.</summary>
        internal static void Error(string title, string message)
        {
            Show(title, message, ToastType.Error, 6000);
        }

        /// <summary>Uyarı bildirimi.</summary>
        internal static void Warning(string title, string message)
        {
            Show(title, message, ToastType.Warning, 5000);
        }
    }

    internal enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
