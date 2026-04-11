using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern yatay ayırıcı çizgi — bölümler arası görsel ayrım.
    /// Label veya Panel ile divider yapma yerine bu kontrol kullanılır.
    /// </summary>
    internal class ModernDivider : Control
    {
        private Color _lineColor = ModernTheme.DividerColor;
        private int _thickness = 1;
        private DividerOrientation _orientation = DividerOrientation.Horizontal;
        private string _labelText = string.Empty;

        public ModernDivider()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Size = new Size(300, 1);
            TabStop = false;
        }

        [Category("Modern"), Description("Çizgi rengi.")]
        public Color LineColor
        {
            get => _lineColor;
            set { _lineColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Çizgi kalınlığı.")]
        public int Thickness
        {
            get => _thickness;
            set { _thickness = Math.Max(1, value); Height = _thickness; Invalidate(); }
        }

        [Category("Modern"), Description("Yön.")]
        public DividerOrientation Orientation
        {
            get => _orientation;
            set { _orientation = value; Invalidate(); }
        }

        [Category("Modern"), Description("Ortada gösterilecek etiket metni.")]
        public string LabelText
        {
            get => _labelText;
            set { _labelText = value ?? string.Empty; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;

            if (string.IsNullOrEmpty(_labelText))
            {
                // Düz çizgi
                using (var pen = new Pen(_lineColor, _thickness))
                {
                    if (_orientation == DividerOrientation.Horizontal)
                    {
                        int y = Height / 2;
                        g.DrawLine(pen, 0, y, Width, y);
                    }
                    else
                    {
                        int x = Width / 2;
                        g.DrawLine(pen, x, 0, x, Height);
                    }
                }
            }
            else
            {
                // Etiketli ayırıcı — "——— Metin ———"
                var textSize = g.MeasureString(_labelText, ModernTheme.FontCaption);
                int textPad = 8;
                float textX = (Width - textSize.Width) / 2;
                float textY = (Height - textSize.Height) / 2;
                int lineY = Height / 2;

                using (var pen = new Pen(_lineColor, _thickness))
                {
                    g.DrawLine(pen, 0, lineY, textX - textPad, lineY);
                    g.DrawLine(pen, textX + textSize.Width + textPad, lineY, Width, lineY);
                }

                using (var textBrush = new SolidBrush(ModernTheme.TextSecondary))
                {
                    g.DrawString(_labelText, ModernTheme.FontCaption, textBrush, textX, textY);
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null)
            {
                using (var bgBrush = new SolidBrush(Parent.BackColor))
                {
                    e.Graphics.FillRectangle(bgBrush, ClientRectangle);
                }
            }
        }

        /// <summary>Tema değişikliğinde cache'lenmiş renkleri günceller.</summary>
        internal void RefreshThemeColors()
        {
            _lineColor = ModernTheme.DividerColor;
            Invalidate();
        }
    }

    internal enum DividerOrientation
    {
        Horizontal,
        Vertical
    }
}
