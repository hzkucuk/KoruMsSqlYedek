using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    internal enum ThemeMode { Dark, Light }

    /// <summary>
    /// Modern WinForms teması — renk paleti, font tanımları ve kontrol stillendirme metodları.
    /// ApplyTheme() ile çalışma zamanında Dark/Light arasında geçiş yapılabilir.
    /// </summary>
    internal static class ModernTheme
    {
        // ═══════════════ THEME STATE ═══════════════

        internal static ThemeMode CurrentTheme { get; private set; } = ThemeMode.Dark;

        // ═══════════════ COLOR PALETTE (mutable for runtime theme switch) ═══════════════

        internal static Color BackgroundColor = Color.FromArgb(30, 30, 30);
        internal static Color SurfaceColor = Color.FromArgb(40, 40, 40);
        internal static Color BorderColor = Color.FromArgb(70, 70, 70);
        internal static Color DividerColor = Color.FromArgb(50, 50, 50);

        // Accent
        internal static Color AccentPrimary = Color.FromArgb(0, 150, 80);
        internal static Color AccentPrimaryDark = Color.FromArgb(0, 120, 60);
        internal static Color AccentPrimaryHover = Color.FromArgb(0, 175, 95);

        // Status
        internal static Color StatusSuccess = Color.FromArgb(0, 190, 110);
        internal static Color StatusWarning = Color.FromArgb(201, 128, 0);
        internal static Color StatusError = Color.FromArgb(220, 40, 40);
        internal static Color StatusCancelled = Color.FromArgb(128, 128, 128);
        internal static Color StatusInfo = Color.FromArgb(100, 180, 255);

        // Text
        internal static Color TextPrimary = Color.FromArgb(230, 230, 230);
        internal static Color TextSecondary = Color.FromArgb(180, 180, 180);
        internal static Color TextDisabled = Color.FromArgb(120, 120, 120);
        internal static Color TextOnAccent = Color.White;

        // Grid
        internal static Color GridAlternateRow = Color.FromArgb(35, 35, 35);
        internal static Color GridSelection = Color.FromArgb(0, 150, 80);
        internal static Color GridHeaderBack = Color.FromArgb(40, 40, 40);
        internal static Color GridHeaderText = Color.FromArgb(180, 180, 180);

        // ═══════════════ THEME APPLY ═══════════════

        internal static void ApplyTheme(ThemeMode mode)
        {
            CurrentTheme = mode;
            if (mode == ThemeMode.Dark)
                ApplyDarkColors();
            else
                ApplyLightColors();
        }

        private static void ApplyDarkColors()
        {
            BackgroundColor = Color.FromArgb(30, 30, 30);
            SurfaceColor = Color.FromArgb(40, 40, 40);
            BorderColor = Color.FromArgb(70, 70, 70);
            DividerColor = Color.FromArgb(50, 50, 50);

            AccentPrimary = Color.FromArgb(0, 150, 80);
            AccentPrimaryDark = Color.FromArgb(0, 120, 60);
            AccentPrimaryHover = Color.FromArgb(0, 175, 95);

            StatusSuccess = Color.FromArgb(0, 190, 110);
            StatusWarning = Color.FromArgb(201, 128, 0);
            StatusError = Color.FromArgb(220, 40, 40);
            StatusCancelled = Color.FromArgb(128, 128, 128);
            StatusInfo = Color.FromArgb(100, 180, 255);

            TextPrimary = Color.FromArgb(230, 230, 230);
            TextSecondary = Color.FromArgb(180, 180, 180);
            TextDisabled = Color.FromArgb(120, 120, 120);
            TextOnAccent = Color.White;

            GridAlternateRow = Color.FromArgb(35, 35, 35);
            GridSelection = Color.FromArgb(0, 150, 80);
            GridHeaderBack = Color.FromArgb(40, 40, 40);
            GridHeaderText = Color.FromArgb(180, 180, 180);
        }

        private static void ApplyLightColors()
        {
            BackgroundColor = Color.FromArgb(245, 245, 248);
            SurfaceColor = Color.White;
            BorderColor = Color.FromArgb(200, 200, 210);
            DividerColor = Color.FromArgb(220, 220, 225);

            AccentPrimary = Color.FromArgb(0, 140, 70);
            AccentPrimaryDark = Color.FromArgb(0, 110, 55);
            AccentPrimaryHover = Color.FromArgb(0, 165, 85);

            StatusSuccess = Color.FromArgb(0, 155, 80);
            StatusWarning = Color.FromArgb(180, 95, 0);
            StatusError = Color.FromArgb(195, 30, 30);
            StatusCancelled = Color.FromArgb(110, 110, 110);
            StatusInfo = Color.FromArgb(0, 110, 200);

            TextPrimary = Color.FromArgb(32, 31, 30);
            TextSecondary = Color.FromArgb(100, 100, 110);
            TextDisabled = Color.FromArgb(160, 160, 165);
            TextOnAccent = Color.White;

            GridAlternateRow = Color.FromArgb(248, 248, 250);
            GridSelection = Color.FromArgb(0, 140, 70);
            GridHeaderBack = Color.FromArgb(240, 241, 244);
            GridHeaderText = Color.FromArgb(80, 80, 90);
        }

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

        internal const int CardRadius = 8;
        internal const int ButtonRadius = 6;
        internal const int BadgeRadius = 10;
        internal const int PaddingStandard = 12;
        internal const int PaddingLarge = 16;
        internal const int PaddingSmall = 6;

        // ═══════════════ STYLING METHODS ═══════════════

        internal static void ApplyFormTheme(Form form)
        {
            form.BackColor = BackgroundColor;
            form.Font = FontBody;
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
        }

        internal static void ApplySizableFormTheme(Form form)
        {
            form.BackColor = BackgroundColor;
            form.Font = FontBody;
        }

        internal static void StyleDataGridView(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = DividerColor;
            dgv.BackgroundColor = SurfaceColor;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBack;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderText;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontCaptionBold;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBack;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = GridHeaderText;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeight = 38;

            dgv.DefaultCellStyle.BackColor = SurfaceColor;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.Font = FontBody;
            dgv.DefaultCellStyle.SelectionBackColor = GridSelection;
            dgv.DefaultCellStyle.SelectionForeColor = TextOnAccent;
            dgv.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
            dgv.RowTemplate.Height = 36;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = GridAlternateRow;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToResizeRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        internal static void StyleListView(ListView lv)
        {
            lv.BorderStyle = BorderStyle.None;
            lv.FullRowSelect = true;
            lv.GridLines = false;
            lv.Font = FontBody;
            lv.ForeColor = TextPrimary;
            lv.BackColor = SurfaceColor;
        }

        internal static void StyleToolStrip(ToolStrip ts)
        {
            ts.BackColor = SurfaceColor;
            ts.GripStyle = ToolStripGripStyle.Hidden;
            ts.Padding = new Padding(8, 4, 8, 4);
            ts.Font = FontBody;
            ts.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        }

        internal static void StyleStatusStrip(StatusStrip ss)
        {
            ss.BackColor = SurfaceColor;
            ss.ForeColor = TextSecondary;
            ss.Font = FontCaption;
            ss.SizingGrip = false;
        }

        internal static void StyleTabControl(TabControl tc)
        {
            tc.Font = FontBody;
            tc.Padding = new Point(14, 6);
        }

        internal static void StyleTextBox(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Font = FontBody;
            txt.BackColor = SurfaceColor;
            txt.ForeColor = TextPrimary;
        }

        internal static void StyleComboBox(ComboBox cmb)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.Font = FontBody;
            cmb.BackColor = SurfaceColor;
            cmb.ForeColor = TextPrimary;
        }

        internal static void StyleGroupBox(GroupBox grp)
        {
            grp.FlatStyle = FlatStyle.Flat;
            grp.Font = FontBodyBold;
            grp.ForeColor = TextPrimary;
            grp.BackColor = Color.Transparent;
        }

        // ═══════════════ GDI+ HELPERS ═══════════════

        internal static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            if (diameter > rect.Height) diameter = rect.Height;
            if (diameter > rect.Width) diameter = rect.Width;

            var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        internal static void SetHighQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
    }
}
