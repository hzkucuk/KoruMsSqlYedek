using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern Header Panel — sayfa başlığı, alt başlık ve isteğe bağlı ikon ile.
    /// Form veya UserControl üst kısmında kullanılır.
    /// </summary>
    internal class ModernHeaderPanel : Panel
    {
        private string _title = "Başlık";
        private string _subtitle = string.Empty;
        private string _iconSymbol = string.Empty;
        private Color _accentColor = ModernTheme.AccentPrimary;
        private bool _showBottomBorder = true;

        public ModernHeaderPanel()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = ModernTheme.SurfaceColor;
            Height = 72;
            Dock = DockStyle.Top;
            Padding = new Padding(ModernTheme.PaddingLarge);
        }

        /// <summary>Başlık metni.</summary>
        [Category("Modern"), Description("Başlık metni.")]
        [DefaultValue("Başlık")]
        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        /// <summary>Alt başlık metni.</summary>
        [Category("Modern"), Description("Alt başlık metni.")]
        [DefaultValue("")]
        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value; Invalidate(); }
        }

        /// <summary>İkon sembolü (Segoe MDL2 Assets veya Segoe UI Symbol).</summary>
        [Category("Modern"), Description("Segoe MDL2 Assets / Segoe UI Symbol ikon karakteri.")]
        [DefaultValue("")]
        public string IconSymbol
        {
            get => _iconSymbol;
            set { _iconSymbol = value; Invalidate(); }
        }

        /// <summary>Accent renk (ikon ve alt çizgi için).</summary>
        [Category("Modern"), Description("İkon ve alt çizgi accent rengi.")]
        [DefaultValue(typeof(Color), "0, 120, 212")]
        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        /// <summary>Alt kenar çizgisi gösterilsin mi.</summary>
        [Category("Modern"), Description("Alt kenar çizgisi gösterilsin mi.")]
        [DefaultValue(true)]
        public bool ShowBottomBorder
        {
            get => _showBottomBorder;
            set { _showBottomBorder = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            // Arka plan
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            int x = Padding.Left;
            int contentY = Padding.Top;

            // İkon
            if (!string.IsNullOrEmpty(_iconSymbol))
            {
                using (var iconFont = new Font("Segoe MDL2 Assets", 20F, FontStyle.Regular))
                using (var iconBrush = new SolidBrush(_accentColor))
                {
                    SizeF iconSize = g.MeasureString(_iconSymbol, iconFont);
                    int iconY = (Height - (int)iconSize.Height) / 2;
                    g.DrawString(_iconSymbol, iconFont, iconBrush, x, iconY);
                    x += (int)iconSize.Width + 12;
                }
            }

            // Başlık
            if (!string.IsNullOrEmpty(_title))
            {
                using (var brush = new SolidBrush(ModernTheme.TextPrimary))
                {
                    SizeF titleSize = g.MeasureString(_title, ModernTheme.FontSubtitle);
                    int titleY = string.IsNullOrEmpty(_subtitle)
                        ? (Height - (int)titleSize.Height) / 2
                        : contentY;
                    g.DrawString(_title, ModernTheme.FontSubtitle, brush, x, titleY);
                    contentY = titleY + (int)titleSize.Height + 2;
                }
            }

            // Alt başlık
            if (!string.IsNullOrEmpty(_subtitle))
            {
                using (var brush = new SolidBrush(ModernTheme.TextSecondary))
                {
                    g.DrawString(_subtitle, ModernTheme.FontCaption, brush, x, contentY);
                }
            }

            // Alt kenar çizgisi
            if (_showBottomBorder)
            {
                using (var pen = new Pen(ModernTheme.DividerColor, 1f))
                {
                    g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
                }
            }
        }
    }
}
