using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern WinForms teması — renk paleti, font tanımları ve kontrol stillendirme metodları.
    /// Tüm formlar bu sınıfı kullanarak tutarlı görünüm sağlar.
    /// </summary>
    internal static class ModernTheme
    {
        // ═══════════════ COLOR PALETTE ═══════════════

        /// <summary>Ana arka plan (koyu gri).</summary>
        internal static readonly Color BackgroundColor = Color.FromArgb(30, 30, 30);

        /// <summary>Kart / panel arka plan (orta koyu).</summary>
        internal static readonly Color SurfaceColor = Color.FromArgb(40, 40, 40);

        /// <summary>Kenar çizgisi rengi.</summary>
        internal static readonly Color BorderColor = Color.FromArgb(70, 70, 70);

        /// <summary>İnce ayırıcı çizgi rengi.</summary>
        internal static readonly Color DividerColor = Color.FromArgb(50, 50, 50);

        // --- Accent Colors ---
        /// <summary>Birincil vurgu (yeşil).</summary>
        internal static readonly Color AccentPrimary = Color.FromArgb(0, 150, 80);

        /// <summary>İkincil vurgu (koyu yeşil).</summary>
        internal static readonly Color AccentPrimaryDark = Color.FromArgb(0, 120, 60);

        /// <summary>Hover durumu accent.</summary>
        internal static readonly Color AccentPrimaryHover = Color.FromArgb(0, 175, 95);

        /// <summary>Başarı / yeşil.</summary>
        internal static readonly Color StatusSuccess = Color.FromArgb(0, 190, 110);

        /// <summary>Uyarı / turuncu.</summary>
        internal static readonly Color StatusWarning = Color.FromArgb(201, 128, 0);

        /// <summary>Hata / kırmızı.</summary>
        internal static readonly Color StatusError = Color.FromArgb(220, 40, 40);

        /// <summary>İptal / gri.</summary>
        internal static readonly Color StatusCancelled = Color.FromArgb(128, 128, 128);

        /// <summary>Bilgi / açık mavi.</summary>
        internal static readonly Color StatusInfo = Color.FromArgb(100, 180, 255);

        // --- Text Colors ---
        /// <summary>Birincil metin (açık).</summary>
        internal static readonly Color TextPrimary = Color.FromArgb(230, 230, 230);

        /// <summary>İkincil metin (gri).</summary>
        internal static readonly Color TextSecondary = Color.FromArgb(180, 180, 180);

        /// <summary>Devre dışı / pasif metin.</summary>
        internal static readonly Color TextDisabled = Color.FromArgb(120, 120, 120);

        /// <summary>Beyaz metin (koyu arkaplan üzerinde).</summary>
        internal static readonly Color TextOnAccent = Color.White;

        // --- DataGridView Colors ---
        /// <summary>Satır alternatif arkaplan.</summary>
        internal static readonly Color GridAlternateRow = Color.FromArgb(35, 35, 35);

        /// <summary>Seçili satır.</summary>
        internal static readonly Color GridSelection = Color.FromArgb(0, 150, 80);

        /// <summary>Grid header arkaplan.</summary>
        internal static readonly Color GridHeaderBack = Color.FromArgb(40, 40, 40);

        /// <summary>Grid header metin.</summary>
        internal static readonly Color GridHeaderText = Color.FromArgb(180, 180, 180);

        // ═══════════════ FONTS ═══════════════

        private const string FontFamily = "Segoe UI";

        internal static readonly Font FontTitle = new Font(FontFamily, 16F, FontStyle.Bold);
        internal static readonly Font FontSubtitle = new Font(FontFamily, 12F, FontStyle.Bold);
        internal static readonly Font FontBody = new Font(FontFamily, 9.5F, FontStyle.Regular);
        internal static readonly Font FontBodyBold = new Font(FontFamily, 9.5F, FontStyle.Bold);
        internal static readonly Font FontCaption = new Font(FontFamily, 8.5F, FontStyle.Regular);
        internal static readonly Font FontCaptionBold = new Font(FontFamily, 8.5F, FontStyle.Bold);
        internal static readonly Font FontSmall = new Font(FontFamily, 8F, FontStyle.Regular);
        internal static readonly Font FontBadge = new Font(FontFamily, 7.5F, FontStyle.Bold);
        internal static readonly Font FontIcon = new Font(FontFamily, 11F, FontStyle.Regular);

        // ═══════════════ METRICS ═══════════════

        /// <summary>Kart köşe yuvarlaklık yarıçapı.</summary>
        internal const int CardRadius = 8;

        /// <summary>Buton köşe yuvarlaklık yarıçapı.</summary>
        internal const int ButtonRadius = 6;

        /// <summary>Badge köşe yuvarlaklık yarıçapı.</summary>
        internal const int BadgeRadius = 10;

        /// <summary>Standart padding.</summary>
        internal const int PaddingStandard = 12;

        /// <summary>Büyük padding.</summary>
        internal const int PaddingLarge = 16;

        /// <summary>Küçük padding.</summary>
        internal const int PaddingSmall = 6;

        // ═══════════════ STYLING METHODS ═══════════════

        /// <summary>
        /// Forma modern tema uygular (arka plan, font, border).
        /// </summary>
        internal static void ApplyFormTheme(Form form)
        {
            form.BackColor = BackgroundColor;
            form.Font = FontBody;
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
        }

        /// <summary>
        /// Forma sizable modern tema uygular.
        /// </summary>
        internal static void ApplySizableFormTheme(Form form)
        {
            form.BackColor = BackgroundColor;
            form.Font = FontBody;
        }

        /// <summary>
        /// DataGridView'e modern stil uygular.
        /// </summary>
        internal static void StyleDataGridView(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = DividerColor;
            dgv.BackgroundColor = SurfaceColor;

            // Header style
            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBack;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderText;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontCaptionBold;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBack;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = GridHeaderText;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeight = 38;

            // Row style
            dgv.DefaultCellStyle.BackColor = SurfaceColor;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.Font = FontBody;
            dgv.DefaultCellStyle.SelectionBackColor = GridSelection;
            dgv.DefaultCellStyle.SelectionForeColor = TextOnAccent;
            dgv.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
            dgv.RowTemplate.Height = 36;

            // Alternating row
            dgv.AlternatingRowsDefaultCellStyle.BackColor = GridAlternateRow;

            // Scrollbars
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToResizeRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        /// <summary>
        /// ListView'e modern stil uygular.
        /// </summary>
        internal static void StyleListView(ListView lv)
        {
            lv.BorderStyle = BorderStyle.None;
            lv.FullRowSelect = true;
            lv.GridLines = false;
            lv.Font = FontBody;
            lv.ForeColor = TextPrimary;
            lv.BackColor = SurfaceColor;
        }

        /// <summary>
        /// ToolStrip'e modern stil uygular.
        /// </summary>
        internal static void StyleToolStrip(ToolStrip ts)
        {
            ts.BackColor = SurfaceColor;
            ts.GripStyle = ToolStripGripStyle.Hidden;
            ts.Padding = new Padding(8, 4, 8, 4);
            ts.Font = FontBody;
            ts.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        }

        /// <summary>
        /// StatusStrip'e modern stil uygular.
        /// </summary>
        internal static void StyleStatusStrip(StatusStrip ss)
        {
            ss.BackColor = SurfaceColor;
            ss.ForeColor = TextSecondary;
            ss.Font = FontCaption;
            ss.SizingGrip = false;
        }

        /// <summary>
        /// TabControl'e modern stil uygular.
        /// </summary>
        internal static void StyleTabControl(TabControl tc)
        {
            tc.Font = FontBody;
            tc.Padding = new Point(14, 6);
        }

        /// <summary>
        /// TextBox'a modern stil uygular.
        /// </summary>
        internal static void StyleTextBox(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Font = FontBody;
            txt.BackColor = SurfaceColor;
            txt.ForeColor = TextPrimary;
        }

        /// <summary>
        /// ComboBox'a modern stil uygular.
        /// </summary>
        internal static void StyleComboBox(ComboBox cmb)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.Font = FontBody;
            cmb.BackColor = SurfaceColor;
            cmb.ForeColor = TextPrimary;
        }

        /// <summary>
        /// GroupBox'a modern stil uygular.
        /// </summary>
        internal static void StyleGroupBox(GroupBox grp)
        {
            grp.FlatStyle = FlatStyle.Flat;
            grp.Font = FontBodyBold;
            grp.ForeColor = TextPrimary;
            grp.BackColor = Color.Transparent;
        }

        // ═══════════════ GDI+ HELPERS ═══════════════

        /// <summary>
        /// Yuvarlatılmış dikdörtgen GraphicsPath oluşturur.
        /// </summary>
        internal static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            if (diameter > rect.Height) diameter = rect.Height;
            if (diameter > rect.Width) diameter = rect.Width;

            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            // Sol üst
            path.AddArc(arc, 180, 90);

            // Sağ üst
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Sağ alt
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Sol alt
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Antialiased çizim için Graphics ayarlarını yapılandırır.
        /// </summary>
        internal static void SetHighQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
    }
}
