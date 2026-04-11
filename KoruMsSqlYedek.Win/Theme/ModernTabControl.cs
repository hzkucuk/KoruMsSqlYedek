using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Owner-drawn TabControl — modern flat sekmeler, accent alt çizgi, yuvarlatılmış köşeler.
    /// Standart WinForms TabControl'ün görsel olarak geliştirilmiş versiyonu.
    /// </summary>
    internal class ModernTabControl : TabControl
    {
        private Color _activeTabColor = ModernTheme.SurfaceColor;
        private Color _inactiveTabColor = ModernTheme.BackgroundColor;
        private Color _activeIndicatorColor = ModernTheme.AccentPrimary;
        private Color _activeTextColor = ModernTheme.AccentPrimary;
        private Color _inactiveTextColor = ModernTheme.TextSecondary;
        private int _indicatorHeight = 3;

        public ModernTabControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DrawMode = TabDrawMode.OwnerDrawFixed;
            ItemSize = new Size(120, 36);
            SizeMode = TabSizeMode.Fixed;
            Padding = new Point(16, 6);
            Font = ModernTheme.FontBody;
        }

        [Category("Modern"), Description("Aktif sekme arkaplan rengi.")]
        public Color ActiveTabColor
        {
            get => _activeTabColor;
            set { _activeTabColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Aktif gösterge rengi.")]
        public Color ActiveIndicatorColor
        {
            get => _activeIndicatorColor;
            set { _activeIndicatorColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Gösterge yüksekliği (piksel).")]
        public int IndicatorHeight
        {
            get => _indicatorHeight;
            set { _indicatorHeight = Math.Max(1, Math.Min(6, value)); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            // Arkaplan
            using (var bgBrush = new SolidBrush(_inactiveTabColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Tab page alanı
            if (TabCount > 0)
            {
                var pageRect = GetTabRect(0);
                var contentRect = new Rectangle(
                    0, pageRect.Bottom,
                    Width, Height - pageRect.Bottom);
                using (var contentBrush = new SolidBrush(_activeTabColor))
                {
                    g.FillRectangle(contentBrush, contentRect);
                }

                // Tab header alt çizgisi
                using (var linePen = new Pen(ModernTheme.DividerColor))
                {
                    g.DrawLine(linePen, 0, pageRect.Bottom, Width, pageRect.Bottom);
                }
            }

            // Sekmeleri çiz
            for (int i = 0; i < TabCount; i++)
            {
                DrawTab(g, i);
            }
        }

        private void DrawTab(Graphics g, int index)
        {
            var tabRect = GetTabRect(index);
            bool isSelected = (SelectedIndex == index);

            // Arkaplan
            var bgColor = isSelected ? _activeTabColor : _inactiveTabColor;
            using (var bgBrush = new SolidBrush(bgColor))
            {
                g.FillRectangle(bgBrush, tabRect);
            }

            // Aktif gösterge (alt çizgi)
            if (isSelected)
            {
                var indicatorRect = new Rectangle(
                    tabRect.X + 4,
                    tabRect.Bottom - _indicatorHeight,
                    tabRect.Width - 8,
                    _indicatorHeight);

                using (var indicatorPath = ModernTheme.CreateRoundedRectanglePath(indicatorRect, _indicatorHeight / 2))
                using (var indicatorBrush = new SolidBrush(_activeIndicatorColor))
                {
                    g.FillPath(indicatorBrush, indicatorPath);
                }
            }

            // Metin
            string tabText = TabPages[index].Text;
            var textColor = isSelected ? _activeTextColor : _inactiveTextColor;
            var textFont = isSelected ? ModernTheme.FontBodyBold : ModernTheme.FontBody;

            using (var textBrush = new SolidBrush(textColor))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(tabText, textFont, textBrush, tabRect, sf);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // Owner-draw modunda OnPaint ile çizim yapıldığı için burada işlem yok
        }

        /// <summary>Tema değişikliğinde cache'lenmiş renkleri günceller.</summary>
        internal void RefreshThemeColors()
        {
            _activeTabColor = ModernTheme.SurfaceColor;
            _inactiveTabColor = ModernTheme.BackgroundColor;
            _activeIndicatorColor = ModernTheme.AccentPrimary;
            _activeTextColor = ModernTheme.AccentPrimary;
            _inactiveTextColor = ModernTheme.TextSecondary;
            Invalidate();
        }
    }
}
