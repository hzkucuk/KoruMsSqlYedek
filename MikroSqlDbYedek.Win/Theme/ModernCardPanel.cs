using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern kart paneli — yuvarlatılmış köşeler, ince kenarlık, hafif gölge efekti.
    /// Dashboard ve diğer formlarda bilgi kartları için kullanılır.
    /// </summary>
    internal class ModernCardPanel : Panel
    {
        private Color _borderColor = ModernTheme.BorderColor;
        private Color _shadowColor = Color.FromArgb(50, 0, 0, 0);
        private int _radius = ModernTheme.CardRadius;
        private bool _showShadow = true;
        private string _headerText = string.Empty;
        private string _headerIcon = string.Empty;

        public ModernCardPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = ModernTheme.SurfaceColor;
            Padding = new Padding(ModernTheme.PaddingStandard);
        }

        [Category("Modern"), Description("Kenarlık rengi.")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Köşe yuvarlaklık yarıçapı.")]
        public int CornerRadius
        {
            get => _radius;
            set { _radius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Modern"), Description("Gölge efekti gösterilsin mi?")]
        public bool ShowShadow
        {
            get => _showShadow;
            set { _showShadow = value; Invalidate(); }
        }

        [Category("Modern"), Description("Kart başlık metni.")]
        public string HeaderText
        {
            get => _headerText;
            set { _headerText = value ?? string.Empty; Invalidate(); }
        }

        [Category("Modern"), Description("Kart başlık ikonu (Segoe MDL2 Assets).")]
        public string HeaderIcon
        {
            get => _headerIcon;
            set { _headerIcon = value ?? string.Empty; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            var shadowOffset = _showShadow ? 2 : 0;
            var cardRect = new Rectangle(
                1,
                1,
                Width - 3 - shadowOffset,
                Height - 3 - shadowOffset);

            // Gölge
            if (_showShadow)
            {
                var shadowRect = new Rectangle(
                    cardRect.X + 2,
                    cardRect.Y + 2,
                    cardRect.Width,
                    cardRect.Height);
                using (var shadowPath = ModernTheme.CreateRoundedRectanglePath(shadowRect, _radius))
                using (var shadowBrush = new SolidBrush(_shadowColor))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }
            }

            // Kart arkaplanı
            using (var path = ModernTheme.CreateRoundedRectanglePath(cardRect, _radius))
            {
                using (var bgBrush = new SolidBrush(BackColor))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var borderPen = new Pen(_borderColor, 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Header
            if (!string.IsNullOrEmpty(_headerText))
            {
                DrawHeader(g, cardRect);
            }
        }

        private void DrawHeader(Graphics g, Rectangle cardRect)
        {
            int headerY = cardRect.Y + ModernTheme.PaddingSmall;
            int headerX = cardRect.X + ModernTheme.PaddingStandard;

            // Icon
            if (!string.IsNullOrEmpty(_headerIcon))
            {
                using (var iconFont = new Font("Segoe MDL2 Assets", 11f))
                using (var iconBrush = new SolidBrush(ModernTheme.AccentPrimary))
                {
                    g.DrawString(_headerIcon, iconFont, iconBrush, headerX, headerY);
                    headerX += 24;
                }
            }

            // Title
            using (var titleBrush = new SolidBrush(ModernTheme.TextPrimary))
            {
                g.DrawString(_headerText, ModernTheme.FontCaptionBold, titleBrush, headerX, headerY + 1);
            }

            // Divider line
            int dividerY = headerY + 22;
            using (var dividerPen = new Pen(ModernTheme.DividerColor, 1f))
            {
                g.DrawLine(dividerPen,
                    cardRect.X + ModernTheme.PaddingSmall,
                    dividerY,
                    cardRect.Right - ModernTheme.PaddingSmall,
                    dividerY);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Parent arkaplanını çiz (transparan destek)
            if (Parent != null)
            {
                using (var bgBrush = new SolidBrush(Parent.BackColor))
                {
                    e.Graphics.FillRectangle(bgBrush, ClientRectangle);
                }
            }
            else
            {
                base.OnPaintBackground(e);
            }
        }
    }
}
