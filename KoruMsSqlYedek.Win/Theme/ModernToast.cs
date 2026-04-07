using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Serilog;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern toast bildirim — sağ alt köşede beliren geçici bildirim paneli.
    /// Yedekleme başarı/hata bildirimleri için kullanılır.
    /// </summary>
    internal class ModernToast : Form
    {
        private readonly Timer _fadeTimer;
        private readonly Timer _closeTimer;
        private float _opacity = 1f;
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
            BackColor = ModernTheme.SurfaceColor;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            // Rounded corners via Region — TransparencyKey+Opacity combo,
            // tray-only (ownerless) modda layered window render sorununa neden oluyordu.
            using var regionPath = ModernTheme.CreateRoundedRectanglePath(
                new Rectangle(0, 0, ToastWidth, ToastHeight), Radius);
            Region = new Region(regionPath);

            // Pozisyon — sağ alt köşe
            var workingArea = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(
                workingArea.Right - ToastWidth - 16,
                workingArea.Bottom - ToastHeight - 16);

            // Fade-out timer (kapanırken opacity azaltma)
            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += OnFadeTick;

            // Otomatik kapatma timer
            _closeTimer = new Timer { Interval = durationMs };
            _closeTimer.Tick += (s, e) =>
            {
                _closeTimer.Stop();
                _fadeTimer.Start();
            };
        }

        /// <summary>Tray-only modda (owner form yok) form aktivasyonu engeller — toast focus çalmasın.</summary>
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOPMOST    = 0x00000008;
                const int WS_EX_TOOLWINDOW  = 0x00000080;
                const int WS_EX_NOACTIVATE  = 0x08000000;

                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        private void OnFadeTick(object? sender, EventArgs e)
        {
            _opacity -= 0.06f;
            if (_opacity <= 0f)
            {
                _fadeTimer.Stop();
                Close();
                return;
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

            // Ana arkaplan (Region zaten formu yuvarlak klipliyor)
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
            _closeTimer.Stop();
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
            toast._closeTimer.Start();

            Log.Debug("ModernToast gösterildi: Bounds={Bounds}, Visible={Visible}, Handle={Handle}",
                toast.Bounds, toast.Visible, toast.IsHandleCreated);
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
