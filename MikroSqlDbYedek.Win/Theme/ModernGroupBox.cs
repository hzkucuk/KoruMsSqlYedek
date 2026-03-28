using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern GroupBox — accent sol kenar çizgisi, yuvarlatılmış köşeler ve özelleştirilebilir başlık.
    /// </summary>
    internal class ModernGroupBox : GroupBox
    {
        private Color _accentColor = ModernTheme.AccentPrimary;
        private int _accentWidth = 3;
        private int _cornerRadius = 6;
        private bool _showAccentBorder = true;

        public ModernGroupBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Font = ModernTheme.FontBody;
            ForeColor = ModernTheme.TextPrimary;
            Padding = new Padding(ModernTheme.PaddingStandard, ModernTheme.PaddingLarge + 8, ModernTheme.PaddingStandard, ModernTheme.PaddingStandard);
        }

        /// <summary>Sol kenar accent rengi.</summary>
        [Category("Modern"), Description("Sol kenar accent rengi.")]
        [DefaultValue(typeof(Color), "0, 120, 212")]
        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        /// <summary>Accent kenar kalınlığı.</summary>
        [Category("Modern"), Description("Accent kenar kalınlığı (piksel).")]
        [DefaultValue(3)]
        public int AccentWidth
        {
            get => _accentWidth;
            set { _accentWidth = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>Köşe yuvarlaklık yarıçapı.</summary>
        [Category("Modern"), Description("Köşe yuvarlaklık yarıçapı.")]
        [DefaultValue(6)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>Accent kenar çizgisi gösterilsin mi.</summary>
        [Category("Modern"), Description("Sol accent kenar çizgisi gösterilsin mi.")]
        [DefaultValue(true)]
        public bool ShowAccentBorder
        {
            get => _showAccentBorder;
            set { _showAccentBorder = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            var rect = ClientRectangle;

            // Başlık yüksekliğini hesapla
            int titleHeight = (int)Math.Ceiling(g.MeasureString(Text ?? "Ag", Font).Height);
            int topOffset = titleHeight / 2;

            // Kutu alanı (başlık yarısından başlar)
            var boxRect = new Rectangle(
                rect.X,
                rect.Y + topOffset,
                rect.Width - 1,
                rect.Height - topOffset - 1);

            // Arka plan — yuvarlatılmış dikdörtgen
            using (var path = ModernTheme.CreateRoundedRectanglePath(boxRect, _cornerRadius))
            {
                using (var brush = new SolidBrush(ModernTheme.SurfaceColor))
                {
                    g.FillPath(brush, path);
                }

                // Kenar çizgisi
                using (var pen = new Pen(ModernTheme.BorderColor, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Sol accent kenar
            if (_showAccentBorder && _accentWidth > 0)
            {
                var accentRect = new Rectangle(
                    boxRect.X,
                    boxRect.Y + _cornerRadius,
                    _accentWidth,
                    boxRect.Height - _cornerRadius * 2);

                using (var brush = new SolidBrush(_accentColor))
                {
                    g.FillRectangle(brush, accentRect);
                }
            }

            // Başlık metni
            if (!string.IsNullOrEmpty(Text))
            {
                SizeF textSize = g.MeasureString(Text, Font);
                float textX = ModernTheme.PaddingStandard + (_showAccentBorder ? _accentWidth + 4 : 0);

                // Başlık arka planı — kenar çizgisini kapatmak için
                var textBgRect = new RectangleF(textX - 4, 0, textSize.Width + 8, titleHeight);
                using (var brush = new SolidBrush(BackColor != Color.Empty && BackColor != Color.Transparent
                    ? BackColor
                    : Parent?.BackColor ?? ModernTheme.BackgroundColor))
                {
                    g.FillRectangle(brush, textBgRect);
                }

                // Başlık metni
                using (var brush = new SolidBrush(_accentColor))
                using (var titleFont = new Font(Font.FontFamily, Font.Size, FontStyle.Bold))
                {
                    g.DrawString(Text, titleFont, brush, textX, 0);
                }
            }
        }
    }
}
