using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern ComboBox — düz stil, focus kenarlık, özel dropdown oku.
    /// </summary>
    internal class ModernComboBox : ComboBox
    {
        private Color _borderColor = ModernTheme.BorderColor;
        private Color _focusBorderColor = ModernTheme.AccentPrimary;
        private bool _isHovered;

        public ModernComboBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            FlatStyle = FlatStyle.Flat;
            Font = ModernTheme.FontBody;
            BackColor = ModernTheme.SurfaceColor;
            ForeColor = ModernTheme.TextPrimary;
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            ItemHeight = 28;
        }

        /// <summary>Normal kenar rengi.</summary>
        [Category("Modern"), Description("Normal durum kenar rengi.")]
        [DefaultValue(typeof(Color), "218, 220, 228")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        /// <summary>Focus kenar rengi.</summary>
        [Category("Modern"), Description("Focus/hover durum kenar rengi.")]
        [DefaultValue(typeof(Color), "0, 120, 212")]
        public Color FocusBorderColor
        {
            get => _focusBorderColor;
            set { _focusBorderColor = value; Invalidate(); }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color bgColor = isSelected ? ModernTheme.AccentPrimary : ModernTheme.SurfaceColor;
            Color textColor = isSelected ? ModernTheme.TextOnAccent : ModernTheme.TextPrimary;

            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            string text = GetItemText(Items[e.Index]);
            var textRect = new Rectangle(
                e.Bounds.X + 8,
                e.Bounds.Y,
                e.Bounds.Width - 16,
                e.Bounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
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
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // WM_PAINT sonrası kenar çiz
            if (m.Msg == 0x000F /* WM_PAINT */ || m.Msg == 0x0085 /* WM_NCPAINT */)
            {
                DrawBorder();
            }
        }

        private void DrawBorder()
        {
            using (Graphics g = CreateGraphics())
            {
                // Native dropdown butonunu arka plan rengiyle ört
                int dropBtnWidth = SystemInformation.VerticalScrollBarWidth;
                var dropRect = new Rectangle(Width - dropBtnWidth - 1, 1, dropBtnWidth, Height - 2);
                using (var bgBrush = new SolidBrush(BackColor))
                {
                    g.FillRectangle(bgBrush, dropRect);
                }

                // Kenar çiz
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                Color borderClr = (Focused || _isHovered) ? _focusBorderColor : _borderColor;

                using (var pen = new Pen(borderClr, (Focused || _isHovered) ? 2f : 1f))
                {
                    g.DrawRectangle(pen, rect);
                }

                // Özel dropdown oku
                int arrowSize = 8;
                int arrowX = Width - dropBtnWidth / 2 - arrowSize / 2;
                int arrowY = (Height - arrowSize / 2) / 2;

                using (var brush = new SolidBrush(Focused ? _focusBorderColor : ModernTheme.TextSecondary))
                {
                    var arrowPoints = new[]
                    {
                        new Point(arrowX, arrowY),
                        new Point(arrowX + arrowSize, arrowY),
                        new Point(arrowX + arrowSize / 2, arrowY + arrowSize / 2)
                    };
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillPolygon(brush, arrowPoints);
                }
            }
        }
    }
}
