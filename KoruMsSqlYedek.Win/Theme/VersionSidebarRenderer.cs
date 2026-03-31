using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Tray menüsünün sol kenarına dikey versiyon sidebar'ı çizen renderer.
    /// Koyu tema ile uyumlu, yeşil gradient sidebar üzerine beyaz dikey metin.
    /// MikroUpdate.Win'deki VersionSidebarRenderer'dan port edilmiştir.
    /// </summary>
    internal sealed class VersionSidebarRenderer : ToolStripProfessionalRenderer
    {
        private readonly string _versionText;
        private int _sidebarWidth;

        public VersionSidebarRenderer(string versionText)
            : base(new ModernToolStripColorTable())
        {
            _versionText = versionText;
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var bg = new SolidBrush(Color.FromArgb(40, 40, 40)))
            {
                e.Graphics.FillRectangle(bg, e.AffectedBounds);
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            Rectangle rc = e.AffectedBounds;
            _sidebarWidth = rc.Right;

            using (var grad = new LinearGradientBrush(
                rc,
                Color.FromArgb(0, 130, 75),
                Color.FromArgb(0, 70, 40),
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(grad, rc);
            }

            // Dikey metin (aşağıdan yukarıya)
            using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                var state = e.Graphics.Save();
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                e.Graphics.TranslateTransform(rc.Left, rc.Bottom);
                e.Graphics.RotateTransform(-90);

                SizeF sz = e.Graphics.MeasureString(_versionText, font);
                float x = (rc.Height - sz.Width) / 2;
                float y = (rc.Width - sz.Height) / 2;
                e.Graphics.DrawString(_versionText, font, brush, x, y);

                e.Graphics.Restore(state);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected && e.Item.Enabled)
            {
                using (var hover = new SolidBrush(Color.FromArgb(60, 60, 60)))
                {
                    var rc = new Rectangle(_sidebarWidth, 0, e.Item.Width - _sidebarWidth, e.Item.Height);
                    e.Graphics.FillRectangle(hover, rc);
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var pen = new Pen(Color.FromArgb(70, 70, 70)))
            {
                e.Graphics.DrawLine(pen, _sidebarWidth + 4, y, e.Item.Width - 4, y);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled && e.Item.Font.Bold)
            {
                // Uygulama başlık öğesi — parlak yeşil accent rengi
                e.TextColor = Color.FromArgb(0, 230, 118);
            }
            else
            {
                e.TextColor = e.Item.Enabled
                    ? Color.FromArgb(230, 230, 230)
                    : Color.FromArgb(120, 120, 120);
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var border = new Pen(Color.FromArgb(70, 70, 70)))
            {
                var rc = new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
                e.Graphics.DrawRectangle(border, rc);
            }
        }
    }
}
