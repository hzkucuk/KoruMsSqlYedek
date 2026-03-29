using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern flat ToolStrip renderer — gölgesiz, temiz kenarlıklı, hover efektli.
    /// </summary>
    internal class ModernToolStripRenderer : ToolStripProfessionalRenderer
    {
        public ModernToolStripRenderer()
            : base(new ModernToolStripColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(ModernTheme.SurfaceColor))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }

            // Alt kenar çizgisi
            using (var pen = new Pen(ModernTheme.DividerColor))
            {
                e.Graphics.DrawLine(pen, 0, e.AffectedBounds.Bottom - 1,
                    e.AffectedBounds.Right, e.AffectedBounds.Bottom - 1);
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                var rect = new Rectangle(2, 2, e.Item.Width - 4, e.Item.Height - 4);
                var g = e.Graphics;
                ModernTheme.SetHighQuality(g);

                var bgColor = e.Item.Pressed
                    ? Color.FromArgb(55, 255, 255, 255)
                    : Color.FromArgb(35, 255, 255, 255);

                using (var path = ModernTheme.CreateRoundedRectanglePath(rect, 4))
                using (var brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled
                ? ModernTheme.TextPrimary
                : ModernTheme.TextDisabled;
            e.TextFont = ModernTheme.FontBody;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var pen = new Pen(ModernTheme.DividerColor))
            {
                e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Image margin (sol gümüş şerit) bastır — dark temada görünmemeli
            using (var brush = new SolidBrush(ModernTheme.SurfaceColor))
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Varsayılan 3D bordürü bastır — alt çizgi zaten çiziliyor
        }

        protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
        {
            // Grip çizimini bastır
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected && e.Item.Enabled)
            {
                var rect = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
                var r = ModernTheme.AccentPrimary.R;
                var g2 = ModernTheme.AccentPrimary.G;
                var b = ModernTheme.AccentPrimary.B;
                using (var brush = new SolidBrush(Color.FromArgb(30, r, g2, b)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            }
        }
    }

    /// <summary>
    /// Modern renk tablosu — flat arka plan, temiz renkler.
    /// </summary>
    internal class ModernToolStripColorTable : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => ModernTheme.SurfaceColor;
        public override Color ToolStripGradientMiddle => ModernTheme.SurfaceColor;
        public override Color ToolStripGradientEnd => ModernTheme.SurfaceColor;
        public override Color ToolStripBorder => ModernTheme.DividerColor;
        public override Color MenuStripGradientBegin => ModernTheme.SurfaceColor;
        public override Color MenuStripGradientEnd => ModernTheme.SurfaceColor;
        public override Color StatusStripGradientBegin => ModernTheme.SurfaceColor;
        public override Color StatusStripGradientEnd => ModernTheme.SurfaceColor;
        public override Color ImageMarginGradientBegin => ModernTheme.SurfaceColor;
        public override Color ImageMarginGradientMiddle => ModernTheme.SurfaceColor;
        public override Color ImageMarginGradientEnd => ModernTheme.SurfaceColor;
        public override Color SeparatorDark => ModernTheme.DividerColor;
        public override Color SeparatorLight => ModernTheme.SurfaceColor;
        public override Color MenuItemSelected => Color.FromArgb(30, ModernTheme.AccentPrimary);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color ButtonSelectedHighlight => Color.FromArgb(35, 255, 255, 255);
        public override Color ButtonSelectedGradientBegin => Color.FromArgb(35, 255, 255, 255);
        public override Color ButtonSelectedGradientEnd => Color.FromArgb(35, 255, 255, 255);
        public override Color ButtonPressedGradientBegin => Color.FromArgb(55, 255, 255, 255);
        public override Color ButtonPressedGradientEnd => Color.FromArgb(55, 255, 255, 255);
    }
}
