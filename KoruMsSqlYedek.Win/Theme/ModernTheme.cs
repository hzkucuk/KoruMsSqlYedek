using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
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

        internal static Color BackgroundColor = Color.FromArgb(18, 18, 22);
        internal static Color SurfaceColor = Color.FromArgb(30, 30, 36);
        internal static Color SurfaceHover = Color.FromArgb(48, 48, 56);
        internal static Color SurfacePressed = Color.FromArgb(24, 24, 28);
        internal static Color BorderColor = Color.FromArgb(56, 56, 64);
        internal static Color DividerColor = Color.FromArgb(40, 40, 46);

        // Accent — Emerald green
        internal static Color AccentPrimary = Color.FromArgb(16, 185, 129);
        internal static Color AccentPrimaryDark = Color.FromArgb(5, 150, 105);
        internal static Color AccentPrimaryHover = Color.FromArgb(52, 211, 153);

        // Status — Tailwind-inspired
        internal static Color StatusSuccess = Color.FromArgb(16, 185, 129);
        internal static Color StatusWarning = Color.FromArgb(245, 158, 11);
        internal static Color StatusError = Color.FromArgb(239, 68, 68);
        internal static Color StatusCancelled = Color.FromArgb(113, 113, 122);
        internal static Color StatusInfo = Color.FromArgb(96, 165, 250);

        // Text
        internal static Color TextPrimary = Color.FromArgb(240, 240, 245);
        internal static Color TextSecondary = Color.FromArgb(160, 160, 170);
        internal static Color TextDisabled = Color.FromArgb(90, 90, 100);
        internal static Color TextOnAccent = Color.White;

        // Grid
        internal static Color GridAlternateRow = Color.FromArgb(24, 24, 28);
        internal static Color GridSelection = Color.FromArgb(16, 185, 129);
        internal static Color GridHeaderBack = Color.FromArgb(36, 36, 42);
        internal static Color GridHeaderText = Color.FromArgb(160, 160, 170);
        internal static Color GridErrorRow = Color.FromArgb(58, 20, 20);

        // Backup Log Console — "Koru" (Protect) trust-inspired palette
        internal static Color LogDefault = Color.FromArgb(190, 195, 200);
        internal static Color LogTimestamp = Color.FromArgb(90, 160, 120);
        internal static Color LogSuccess = Color.FromArgb(46, 204, 113);
        internal static Color LogError = Color.FromArgb(255, 107, 107);
        internal static Color LogWarning = Color.FromArgb(255, 193, 69);
        internal static Color LogInfo = Color.FromArgb(116, 185, 255);
        internal static Color LogProgress = Color.FromArgb(0, 210, 211);
        internal static Color LogCloud = Color.FromArgb(162, 155, 254);
        internal static Color LogStarted = Color.FromArgb(16, 185, 129);
        internal static Color LogConsoleBg = Color.FromArgb(22, 24, 28);

        // ═══════════════ THEME APPLY ═══════════════

        /// <summary>Şu anda uygulanan log konsolu renk şablonu.</summary>
        internal static TerminalColorScheme ActiveLogScheme { get; private set; } = TerminalColorScheme.Koru;

        internal static void ApplyTheme(ThemeMode mode)
        {
            CurrentTheme = mode;
            if (mode == ThemeMode.Dark)
                ApplyDarkColors();
            else
                ApplyLightColors();

            // Log renkleri tema değişiminde de güncel kalsın
            ApplyLogColorScheme(ActiveLogScheme);
        }

        /// <summary>
        /// Terminal renk şablonunu log konsolu renklerine uygular.
        /// </summary>
        internal static void ApplyLogColorScheme(TerminalColorScheme scheme)
        {
            ActiveLogScheme = scheme ?? TerminalColorScheme.Koru;
            LogDefault   = ActiveLogScheme.Default;
            LogTimestamp  = ActiveLogScheme.Timestamp;
            LogSuccess   = ActiveLogScheme.Success;
            LogError     = ActiveLogScheme.Error;
            LogWarning   = ActiveLogScheme.Warning;
            LogInfo      = ActiveLogScheme.Info;
            LogProgress  = ActiveLogScheme.Progress;
            LogCloud     = ActiveLogScheme.Cloud;
            LogStarted   = ActiveLogScheme.Started;
            LogConsoleBg = ActiveLogScheme.Background;
        }

        /// <summary>
        /// Id ile şablon bulup uygular; bulunamazsa Koru döner.
        /// </summary>
        internal static void ApplyLogColorScheme(string schemeId)
        {
            ApplyLogColorScheme(TerminalColorScheme.FindById(schemeId));
        }

        private static void ApplyDarkColors()
        {
            BackgroundColor = Color.FromArgb(18, 18, 22);
            SurfaceColor = Color.FromArgb(30, 30, 36);
            SurfaceHover = Color.FromArgb(48, 48, 56);
            SurfacePressed = Color.FromArgb(24, 24, 28);
            BorderColor = Color.FromArgb(56, 56, 64);
            DividerColor = Color.FromArgb(40, 40, 46);

            AccentPrimary = Color.FromArgb(16, 185, 129);
            AccentPrimaryDark = Color.FromArgb(5, 150, 105);
            AccentPrimaryHover = Color.FromArgb(52, 211, 153);

            StatusSuccess = Color.FromArgb(16, 185, 129);
            StatusWarning = Color.FromArgb(245, 158, 11);
            StatusError = Color.FromArgb(239, 68, 68);
            StatusCancelled = Color.FromArgb(113, 113, 122);
            StatusInfo = Color.FromArgb(96, 165, 250);

            TextPrimary = Color.FromArgb(240, 240, 245);
            TextSecondary = Color.FromArgb(160, 160, 170);
            TextDisabled = Color.FromArgb(90, 90, 100);
            TextOnAccent = Color.White;

            GridAlternateRow = Color.FromArgb(24, 24, 28);
            GridSelection = Color.FromArgb(16, 185, 129);
            GridHeaderBack = Color.FromArgb(36, 36, 42);
            GridHeaderText = Color.FromArgb(160, 160, 170);
            GridErrorRow = Color.FromArgb(58, 20, 20);
        }

        private static void ApplyLightColors()
        {
            BackgroundColor = Color.FromArgb(245, 245, 248);
            SurfaceColor = Color.White;
            SurfaceHover = Color.FromArgb(240, 240, 244);
            SurfacePressed = Color.FromArgb(230, 230, 234);
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
            GridErrorRow = Color.FromArgb(255, 232, 232);
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
