using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern buton — flat tasarım, hover/press efektleri, accent renk desteği.
    /// Primary, Secondary ve Danger varyantlarını destekler.
    /// </summary>
    internal class ModernButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;
        private ModernButtonStyle _buttonStyle = ModernButtonStyle.Primary;
        private int _radius = ModernTheme.ButtonRadius;
        private string _iconSymbol = string.Empty;

        public ModernButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = ModernTheme.FontBody;
            Cursor = Cursors.Hand;
            Size = new Size(120, 36);
        }

        [Category("Modern"), Description("Buton stili.")]
        public ModernButtonStyle ButtonStyle
        {
            get => _buttonStyle;
            set { _buttonStyle = value; Invalidate(); }
        }

        [Category("Modern"), Description("Köşe yuvarlaklık yarıçapı.")]
        public int CornerRadius
        {
            get => _radius;
            set { _radius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Modern"), Description("Segoe MDL2 Assets ikon sembolü.")]
        public string IconSymbol
        {
            get => _iconSymbol;
            set { _iconSymbol = value ?? string.Empty; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            ModernTheme.SetHighQuality(g);

            // Parent arkaplan
            if (Parent != null)
            {
                using (var bgBrush = new SolidBrush(Parent.BackColor))
                {
                    g.FillRectangle(bgBrush, ClientRectangle);
                }
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            Color bgColor, fgColor, borderColor;
            GetStateColors(out bgColor, out fgColor, out borderColor);

            // Buton arkaplan
            using (var path = ModernTheme.CreateRoundedRectanglePath(rect, _radius))
            {
                using (var fillBrush = new SolidBrush(bgColor))
                {
                    g.FillPath(fillBrush, path);
                }

                if (borderColor != Color.Transparent)
                {
                    using (var borderPen = new Pen(borderColor, 1f))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }
            }

            // İçerik (ikon + metin)
            DrawContent(g, rect, fgColor);
        }

        private void DrawContent(Graphics g, Rectangle rect, Color fgColor)
        {
            // Phosphor / custom Image destegi
            if (Image != null)
            {
                DrawImageAndText(g, rect, fgColor);
                return;
            }

            // Segoe MDL2 Assets ikon destegi
            if (!string.IsNullOrEmpty(_iconSymbol))
            {
                using (var iconFont = new Font("Segoe MDL2 Assets", 10f))
                using (var iconBrush = new SolidBrush(fgColor))
                {
                    var iconSize = g.MeasureString(_iconSymbol, iconFont);
                    var textSize = g.MeasureString(Text, Font);
                    float totalWidth = iconSize.Width + 4 + textSize.Width;
                    float startX = (rect.Width - totalWidth) / 2;
                    float iconY = (rect.Height - iconSize.Height) / 2;

                    g.DrawString(_iconSymbol, iconFont, iconBrush, startX, iconY);

                    using (var textBrush = new SolidBrush(fgColor))
                    {
                        float textY = (rect.Height - textSize.Height) / 2;
                        g.DrawString(Text, Font, textBrush, startX + iconSize.Width + 4, textY);
                    }
                    return;
                }
            }

            // Sadece metin (ortalı)
            using (var textBrush = new SolidBrush(fgColor))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(Text, Font, textBrush, rect, sf);
            }
        }

        private void DrawImageAndText(Graphics g, Rectangle rect, Color fgColor)
        {
            var img = Image;
            bool hasText = !string.IsNullOrEmpty(Text);

            if (!hasText)
            {
                // Sadece ikon — ortala
                int ix = rect.X + (rect.Width - img.Width) / 2;
                int iy = rect.Y + (rect.Height - img.Height) / 2;
                g.DrawImage(img, ix, iy, img.Width, img.Height);
                return;
            }

            // Ikon + metin yatay ortalı
            var textSize = g.MeasureString(Text, Font);
            int gap = 5;
            float totalW = img.Width + gap + textSize.Width;
            float startX = rect.X + (rect.Width - totalW) / 2f;
            float imgY = rect.Y + (rect.Height - img.Height) / 2f;
            float textY = rect.Y + (rect.Height - textSize.Height) / 2f;

            g.DrawImage(img, startX, imgY, img.Width, img.Height);

            using (var brush = new SolidBrush(fgColor))
            {
                g.DrawString(Text, Font, brush, startX + img.Width + gap, textY);
            }
        }

        private void GetStateColors(out Color bg, out Color fg, out Color border)
        {
            if (!Enabled)
            {
                bg = ModernTheme.DividerColor;
                fg = ModernTheme.TextDisabled;
                border = ModernTheme.BorderColor;
                return;
            }

            switch (_buttonStyle)
            {
                case ModernButtonStyle.Primary:
                    bg = _isPressed
                        ? ModernTheme.AccentPrimaryDark
                        : _isHovered
                            ? ModernTheme.AccentPrimaryHover
                            : ModernTheme.AccentPrimary;
                    fg = ModernTheme.TextOnAccent;
                    border = Color.Transparent;
                    break;

                case ModernButtonStyle.Secondary:
                    bg = _isPressed
                        ? Color.FromArgb(230, 230, 234)
                        : _isHovered
                            ? Color.FromArgb(240, 240, 244)
                            : ModernTheme.SurfaceColor;
                    fg = ModernTheme.TextPrimary;
                    border = ModernTheme.BorderColor;
                    break;

                case ModernButtonStyle.Danger:
                    bg = _isPressed
                        ? Color.FromArgb(160, 30, 20)
                        : _isHovered
                            ? Color.FromArgb(210, 50, 35)
                            : ModernTheme.StatusError;
                    fg = ModernTheme.TextOnAccent;
                    border = Color.Transparent;
                    break;

                case ModernButtonStyle.Ghost:
                    bg = _isPressed
                        ? Color.FromArgb(20, 0, 0, 0)
                        : _isHovered
                            ? Color.FromArgb(10, 0, 0, 0)
                            : Color.Transparent;
                    fg = ModernTheme.AccentPrimary;
                    border = Color.Transparent;
                    break;

                default:
                    bg = ModernTheme.SurfaceColor;
                    fg = ModernTheme.TextPrimary;
                    border = ModernTheme.BorderColor;
                    break;
            }
        }
    }

    /// <summary>
    /// ModernButton görsel stili.
    /// </summary>
    internal enum ModernButtonStyle
    {
        /// <summary>Birincil eylem — accent renk arkaplan.</summary>
        Primary,
        /// <summary>İkincil eylem — beyaz arkaplan, kenarlıklı.</summary>
        Secondary,
        /// <summary>Tehlikeli eylem — kırmızı arkaplan.</summary>
        Danger,
        /// <summary>Hayalet buton — şeffaf arkaplan, metin linki gibi.</summary>
        Ghost
    }
}
