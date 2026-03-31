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
        private readonly string _appName;
        private readonly string _versionText;
        private int _sidebarWidth;

        public VersionSidebarRenderer(string appName, string versionText)
            : base(new ModernToolStripColorTable())
        {
            _appName = appName;
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

            // Dikey metin (aşağıdan yukarıya) — uygulama adı + versiyon
            using (var nameFont = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            using (var versionFont = new Font("Segoe UI", 7.5F, FontStyle.Regular))
            using (var nameBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
            using (var versionBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            {
                var state = e.Graphics.Save();
                e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                e.Graphics.TranslateTransform(rc.Left, rc.Bottom);
                e.Graphics.RotateTransform(-90);

                // Rotated coordinate: X = sidebar height, Y = sidebar width
                SizeF nameSz = e.Graphics.MeasureString(_appName, nameFont);
                SizeF verSz = e.Graphics.MeasureString(_versionText, versionFont);

                float totalWidth = nameSz.Width + 6 + verSz.Width;
                float startX = (rc.Height - totalWidth) / 2;

                // Versiyon (sidebar alt kısmı = rotated sol)
                float yCenter = (rc.Width - verSz.Height) / 2;
                e.Graphics.DrawString(_versionText, versionFont, versionBrush, startX, yCenter);

                // Uygulama adı (sidebar üst kısmı = rotated sağ)
                yCenter = (rc.Width - nameSz.Height) / 2;
                e.Graphics.DrawString(_appName, nameFont, nameBrush, startX + verSz.Width + 6, yCenter);

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
