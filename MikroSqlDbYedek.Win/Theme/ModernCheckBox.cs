using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern CheckBox — özel çizim, accent renk, yuvarlatılmış köşeler.
    /// </summary>
    internal class ModernCheckBox : CheckBox
    {
        private const int BoxSize = 18;
        private const int BoxRadius = 4;

        private Color _checkedColor = ModernTheme.AccentPrimary;
        private Color _uncheckedBorderColor = ModernTheme.BorderColor;
        private bool _isHovered;

        public ModernCheckBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Font = ModernTheme.FontBody;
            ForeColor = ModernTheme.TextPrimary;
            Cursor = Cursors.Hand;
        }

        /// <summary>İşaretli durumda kutu rengi.</summary>
        [Category("Modern"), Description("İşaretli durumdaki arka plan rengi.")]
        [DefaultValue(typeof(Color), "0, 120, 212")]
        public Color CheckedColor
        {
            get => _checkedColor;
            set { _checkedColor = value; Invalidate(); }
        }

        /// <summary>İşaretsiz durumda kenar rengi.</summary>
        [Category("Modern"), Description("İşaretsiz durumda kenar rengi.")]
        [DefaultValue(typeof(Color), "218, 220, 228")]
        public Color UncheckedBorderColor
        {
            get => _uncheckedBorderColor;
            set { _uncheckedBorderColor = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            // Temiz arka plan
            using (var bgBrush = new SolidBrush(Parent?.BackColor ?? ModernTheme.BackgroundColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Checkbox kutusu
            int boxY = (Height - BoxSize) / 2;
            var boxRect = new Rectangle(0, boxY, BoxSize, BoxSize);

            using (var path = CreateRoundedRect(boxRect, BoxRadius))
            {
                if (Checked || CheckState == CheckState.Indeterminate)
                {
                    // İşaretli — accent arka plan
                    Color fillColor = _isHovered
                        ? ModernTheme.AccentPrimaryHover
                        : _checkedColor;

                    using (var brush = new SolidBrush(fillColor))
                    {
                        g.FillPath(brush, path);
                    }

                    // Onay işareti veya tire
                    using (var pen = new Pen(Color.White, 2f))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;

                        if (CheckState == CheckState.Indeterminate)
                        {
                            // Tire (indeterminate)
                            g.DrawLine(pen,
                                boxRect.X + 5, boxRect.Y + BoxSize / 2,
                                boxRect.Right - 5, boxRect.Y + BoxSize / 2);
                        }
                        else
                        {
                            // Onay işareti ✓
                            g.DrawLines(pen, new[]
                            {
                                new Point(boxRect.X + 4, boxRect.Y + BoxSize / 2),
                                new Point(boxRect.X + BoxSize / 2 - 1, boxRect.Bottom - 5),
                                new Point(boxRect.Right - 4, boxRect.Y + 5)
                            });
                        }
                    }
                }
                else
                {
                    // İşaretsiz — beyaz arka plan + kenar
                    using (var brush = new SolidBrush(ModernTheme.SurfaceColor))
                    {
                        g.FillPath(brush, path);
                    }

                    Color borderColor = _isHovered
                        ? ModernTheme.AccentPrimary
                        : _uncheckedBorderColor;

                    using (var pen = new Pen(borderColor, _isHovered ? 2f : 1.5f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Metin
            if (!string.IsNullOrEmpty(Text))
            {
                var textRect = new Rectangle(
                    BoxSize + 8,
                    0,
                    Width - BoxSize - 8,
                    Height);

                TextRenderer.DrawText(
                    g,
                    Text,
                    Font,
                    textRect,
                    Enabled ? ForeColor : ModernTheme.TextDisabled,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void OnMouseEnter(EventArgs eventargs)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(eventargs);
        }

        protected override void OnMouseLeave(EventArgs eventargs)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(eventargs);
        }

        private static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
