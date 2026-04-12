using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern form temel sınıfı — tutarlı tema, DPI-awareness, tüm alt formlar bu sınıftan türer.
    /// .NET 10 native dark mode ile entegre çalışır. Standart kontroller otomatik dark olur;
    /// bu sınıf yalnızca custom owner-drawn kontrollerin uyumunu sağlar.
    /// </summary>
    public class ModernFormBase : Form
    {
        public ModernFormBase()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Custom tema renkleri (owner-drawn kontroller için)
            BackColor = ModernTheme.BackgroundColor;
            Font = ModernTheme.FontBody;
            ForeColor = ModernTheme.TextPrimary;

            // Global renderer (ToolStrip/StatusStrip için)
            ToolStripManager.Renderer = new ModernToolStripRenderer();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyThemeToCustomControls(this);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            if (ModernTheme.CurrentTheme != ThemeMode.OzgurFilistin)
                return;

            DrawPalestineFlagWatermark(e.Graphics, ClientSize);
        }

        /// <summary>
        /// Filistin bayrağını transparan watermark olarak form arka planına çizer.
        /// 3 yatay şerit (siyah-beyaz-yeşil) ve sol tarafta kırmızı üçgen.
        /// Yalnızca OzgürFilistin teması aktifken çağrılır.
        /// </summary>
        private static void DrawPalestineFlagWatermark(Graphics g, Size clientSize)
        {
            const float flagRatio = 2f; // width:height = 2:1

            // Bayrak boyutu — form genişliğinin ~55%'i, ortada
            int flagWidth = (int)(clientSize.Width * 0.55f);
            int flagHeight = (int)(flagWidth / flagRatio);

            // Form çok küçükse çizme
            if (flagWidth < 100 || flagHeight < 50)
                return;

            int x = (clientSize.Width - flagWidth) / 2;
            int y = (clientSize.Height - flagHeight) / 2;
            int stripeHeight = flagHeight / 3;
            int lastStripeHeight = flagHeight - stripeHeight * 2; // kalan piksel farkını son şerite ver

            SmoothingMode prevSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // ── 3 yatay şerit ──

            // Siyah şerit (üst) — koyu arkaplanda açık gri ile fark edilir kıl
            using (SolidBrush blackBrush = new(Color.FromArgb(35, 160, 160, 160)))
                g.FillRectangle(blackBrush, x, y, flagWidth, stripeHeight);

            // Beyaz şerit (orta)
            using (SolidBrush whiteBrush = new(Color.FromArgb(40, 255, 255, 255)))
                g.FillRectangle(whiteBrush, x, y + stripeHeight, flagWidth, stripeHeight);

            // Yeşil şerit (alt) — Filistin yeşili #009736
            using (SolidBrush greenBrush = new(Color.FromArgb(50, 0, 151, 54)))
                g.FillRectangle(greenBrush, x, y + stripeHeight * 2, flagWidth, lastStripeHeight);

            // ── Kırmızı üçgen (sol taraf) — Filistin kırmızısı #CE1126 ──
            int triangleWidth = (int)(flagWidth * 0.33f);
            Point[] triangle =
            [
                new(x, y),
                new(x + triangleWidth, y + flagHeight / 2),
                new(x, y + flagHeight)
            ];

            using (SolidBrush redBrush = new(Color.FromArgb(55, 206, 17, 38)))
                g.FillPolygon(redBrush, triangle);

            // ── İnce kenarlık — bayrağı çerçevele ──
            using (Pen borderPen = new(Color.FromArgb(25, 200, 200, 200), 1f))
                g.DrawRectangle(borderPen, x, y, flagWidth, flagHeight);

            g.SmoothingMode = prevSmoothing;
        }

        /// <summary>
        /// Çalışma zamanında tema değişikliği sonrası tüm kontrollerin renklerini günceller.
        /// ModernTheme.ApplyTheme() çağrıldıktan sonra bu metod çağrılmalıdır.
        /// </summary>
        internal void RefreshTheme()
        {
            SuspendLayout();

            // Form kendi renklerini güncelle
            BackColor = ModernTheme.BackgroundColor;
            ForeColor = ModernTheme.TextPrimary;

            // Global ToolStrip renderer'ı yenile
            ToolStripManager.Renderer = new ModernToolStripRenderer();

            // Tüm kontrol ağacını güncelle
            RefreshControlTree(this);

            ResumeLayout(true);
            Invalidate(true);
        }

        /// <summary>
        /// Yalnızca custom owner-drawn kontrollere ve özel ayar gerektiren kontrollere tema uygular.
        /// Standart kontroller .NET 10 native dark mode tarafından otomatik yönetilir.
        /// </summary>
        internal static void ApplyThemeToCustomControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                ApplyControlTheme(c);

                if (c.HasChildren)
                {
                    ApplyThemeToCustomControls(c);
                }
            }
        }

        /// <summary>
        /// Tüm kontrol ağacını dolaşarak hem Modern* hem standart kontrollerin
        /// cache'lenmiş renklerini günceller.
        /// </summary>
        private static void RefreshControlTree(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                RefreshSingleControl(c);

                if (c.HasChildren)
                {
                    RefreshControlTree(c);
                }
            }
        }

        private static void RefreshSingleControl(Control c)
        {
            // ── Modern* kontroller — cache'lenmiş renkleri yeniden yükle ──

            if (c is ModernTabControl mtc)
            {
                mtc.RefreshThemeColors();
                return;
            }

            if (c is ModernCardPanel mcp)
            {
                mcp.RefreshThemeColors();
                return;
            }

            if (c is ModernHeaderPanel mhp)
            {
                mhp.RefreshThemeColors();
                return;
            }

            if (c is ModernTextBox mtb)
            {
                mtb.RefreshThemeColors();
                return;
            }

            if (c is ModernSearchBox msb)
            {
                msb.RefreshThemeColors();
                return;
            }

            if (c is ModernNumericUpDown mnud)
            {
                mnud.RefreshThemeColors();
                return;
            }

            if (c is ModernGroupBox mgb)
            {
                mgb.RefreshThemeColors();
                return;
            }

            if (c is ModernCheckBox mcb)
            {
                mcb.RefreshThemeColors();
                return;
            }

            if (c is ModernProgressBar mpb)
            {
                mpb.RefreshThemeColors();
                return;
            }

            if (c is ModernToggleSwitch mts)
            {
                mts.RefreshThemeColors();
                return;
            }

            if (c is ModernDivider md)
            {
                md.RefreshThemeColors();
                return;
            }

            // ModernButton, ModernLoadingOverlay — doğrudan
            // ModernTheme.* okuyor (OnPaint'te), sadece Invalidate yeterli
            if (c is ModernButton or ModernLoadingOverlay)
            {
                c.Invalidate();
                return;
            }

            if (c is ModernComboBox mcmb)
            {
                mcmb.RefreshThemeColors();
                return;
            }

            // ── Standart kontroller ──

            if (c is DataGridView dgv)
            {
                ModernTheme.StyleDataGridView(dgv);
                return;
            }

            // ListView — renkleri ve OwnerDraw güncellemesi
            if (c is ListView lv)
            {
                ModernTheme.StyleListView(lv);
                return;
            }

            if (c is ToolStrip ts && c is not StatusStrip && c is not MenuStrip)
            {
                ModernTheme.StyleToolStrip(ts);
                ts.Renderer = new ModernToolStripRenderer();
                return;
            }

            if (c is StatusStrip ss)
            {
                ModernTheme.StyleStatusStrip(ss);
                ss.Renderer = new ModernToolStripRenderer();
                return;
            }

            if (c is TabPage tp)
            {
                tp.BackColor = ModernTheme.BackgroundColor;
                tp.ForeColor = ModernTheme.TextPrimary;
                return;
            }

            if (c is Panel pnl)
            {
                pnl.BackColor = pnl.Tag is "surface" ? ModernTheme.SurfaceColor : ModernTheme.BackgroundColor;
                return;
            }

            if (c is Label lbl)
            {
                if (lbl.Tag is "accent")
                    lbl.ForeColor = ModernTheme.AccentPrimary;
                else if (lbl.Tag is "secondary")
                    lbl.ForeColor = ModernTheme.TextSecondary;
                return;
            }

            if (c is RichTextBox or TreeView or ListBox)
            {
                ModernTheme.ApplyScrollBarTheme(c);
            }
        }

        private static void ApplyControlTheme(Control c)
        {
            // Custom Modern* kontroller — kendi temalarını yönetir, dokunma
            if (c is ModernButton || c is ModernTextBox || c is ModernComboBox
                || c is ModernCheckBox || c is ModernNumericUpDown || c is ModernProgressBar
                || c is ModernCardPanel || c is ModernGroupBox || c is ModernSearchBox
                || c is ModernToggleSwitch || c is ModernDivider || c is ModernHeaderPanel
                || c is ModernLoadingOverlay)
                return;

            // DataGridView — native dark mode sınırlı, ek stil gerekiyor
            if (c is DataGridView dgv)
            {
                ModernTheme.StyleDataGridView(dgv);
                return;
            }

            // ListView — kolon başlıkları ve satırlar dark mode'da OwnerDraw ile çizilir
            if (c is ListView lv)
            {
                ModernTheme.StyleListView(lv);
                return;
            }

            // ToolStrip — custom renderer
            if (c is ToolStrip ts && !(c is StatusStrip) && !(c is MenuStrip))
            {
                ModernTheme.StyleToolStrip(ts);
                ts.Renderer = new ModernToolStripRenderer();
                return;
            }

            // StatusStrip — custom renderer
            if (c is StatusStrip ss)
            {
                ModernTheme.StyleStatusStrip(ss);
                ss.Renderer = new ModernToolStripRenderer();
                return;
            }

            // TabPage — arka plan rengini eşitle
            if (c is TabPage tp)
            {
                tp.BackColor = ModernTheme.BackgroundColor;
                tp.ForeColor = ModernTheme.TextPrimary;
                return;
            }

            // Panel / FlowLayoutPanel / TableLayoutPanel — arka plan eşitle
            if (c is Panel pnl)
            {
                pnl.BackColor = pnl.Tag is "surface" ? ModernTheme.SurfaceColor : ModernTheme.BackgroundColor;
                return;
            }

            // Label — Tag tabanlı tema renkleri
            if (c is Label lbl)
            {
                if (lbl.Tag is "accent")
                    lbl.ForeColor = ModernTheme.AccentPrimary;
                else if (lbl.Tag is "secondary")
                    lbl.ForeColor = ModernTheme.TextSecondary;
                return;
            }

            // Scrollbar'lı native kontroller — dark scrollbar tema
            if (c is RichTextBox or TreeView or ListBox)
            {
                ModernTheme.ApplyScrollBarTheme(c);
            }
        }
    }
}
