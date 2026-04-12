using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    internal enum ThemeMode { Dark, Light, OzgurFilistin }

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
        internal static Color GridSelectionBack = Color.FromArgb(27, 66, 58);
        internal static Color GridSelectionBackUnfocused = Color.FromArgb(28, 54, 51);
        internal static Color GridHeaderBack = Color.FromArgb(36, 36, 42);
        internal static Color GridHeaderText = Color.FromArgb(160, 160, 170);
        internal static Color GridErrorRow = Color.FromArgb(58, 20, 20);

        // Backup Log Console — Özgür Filistin varsayılan palette
        internal static Color LogDefault = Color.FromArgb(232, 228, 220);
        internal static Color LogTimestamp = Color.FromArgb(85, 105, 72);
        internal static Color LogSuccess = Color.FromArgb(0, 158, 73);
        internal static Color LogError = Color.FromArgb(214, 40, 52);
        internal static Color LogWarning = Color.FromArgb(218, 178, 52);
        internal static Color LogInfo = Color.FromArgb(120, 180, 228);
        internal static Color LogProgress = Color.FromArgb(46, 204, 120);
        internal static Color LogCloud = Color.FromArgb(195, 100, 125);
        internal static Color LogStarted = Color.FromArgb(0, 168, 82);
        internal static Color LogConsoleBg = Color.FromArgb(10, 10, 14);

        // ═══════════════ THEME APPLY ═══════════════

        /// <summary>Şu anda uygulanan log konsolu renk şablonu.</summary>
        internal static TerminalColorScheme ActiveLogScheme { get; private set; } = TerminalColorScheme.OzgurFilistin;

        internal static void ApplyTheme(ThemeMode mode)
        {
            CurrentTheme = mode;
            switch (mode)
            {
                case ThemeMode.OzgurFilistin:
                    ApplyOzgurFilistinColors();
                    break;
                case ThemeMode.Light:
                    ApplyLightColors();
                    break;
                default:
                    ApplyDarkColors();
                    break;
            }

            // Log renkleri tema değişiminde de güncel kalsın
            ApplyLogColorScheme(ActiveLogScheme);
        }

        /// <summary>
        /// Terminal renk şablonunu log konsolu renklerine uygular.
        /// </summary>
        internal static void ApplyLogColorScheme(TerminalColorScheme scheme)
        {
            ActiveLogScheme = scheme ?? TerminalColorScheme.OzgurFilistin;
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
            GridSelectionBack = Color.FromArgb(27, 66, 58);
            GridSelectionBackUnfocused = Color.FromArgb(28, 54, 51);
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
            GridSelectionBack = Color.FromArgb(218, 242, 232);
            GridSelectionBackUnfocused = Color.FromArgb(230, 245, 238);
            GridHeaderBack = Color.FromArgb(240, 241, 244);
            GridHeaderText = Color.FromArgb(80, 80, 90);
            GridErrorRow = Color.FromArgb(255, 232, 232);
        }

        /// <summary>
        /// Özgür Filistin teması — Filistin bayrak renkleri (Siyah, Beyaz, Yeşil, Kırmızı)
        /// ile boyanmış koyu tema varyantı.
        /// </summary>
        private static void ApplyOzgurFilistinColors()
        {
            // Siyah (kararlılık / gece) — arka plan ailesi
            BackgroundColor = Color.FromArgb(12, 12, 14);
            SurfaceColor = Color.FromArgb(24, 24, 28);
            SurfaceHover = Color.FromArgb(38, 42, 38);
            SurfacePressed = Color.FromArgb(18, 18, 20);
            BorderColor = Color.FromArgb(0, 105, 55);
            DividerColor = Color.FromArgb(34, 38, 34);

            // Yeşil (toprak / bereket) — accent ailesi
            AccentPrimary = Color.FromArgb(0, 158, 73);
            AccentPrimaryDark = Color.FromArgb(0, 120, 55);
            AccentPrimaryHover = Color.FromArgb(0, 188, 90);

            // Kırmızı (cesaret / direniş) — status vurguları
            StatusSuccess = Color.FromArgb(0, 168, 82);
            StatusWarning = Color.FromArgb(245, 180, 42);
            StatusError = Color.FromArgb(214, 40, 52);
            StatusCancelled = Color.FromArgb(120, 120, 128);
            StatusInfo = Color.FromArgb(232, 228, 220);

            // Beyaz (barış / umut) — metin ailesi
            TextPrimary = Color.FromArgb(240, 236, 228);
            TextSecondary = Color.FromArgb(180, 176, 168);
            TextDisabled = Color.FromArgb(90, 88, 82);
            TextOnAccent = Color.White;

            // Grid — Filistin yeşili vurgulu
            GridAlternateRow = Color.FromArgb(18, 20, 18);
            GridSelection = Color.FromArgb(0, 158, 73);
            GridSelectionBack = Color.FromArgb(16, 62, 42);
            GridSelectionBackUnfocused = Color.FromArgb(20, 48, 36);
            GridHeaderBack = Color.FromArgb(28, 32, 28);
            GridHeaderText = Color.FromArgb(180, 176, 168);
            GridErrorRow = Color.FromArgb(62, 18, 22);
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

            // DoubleBuffered — watermark ve scroll sırasında flicker önleme
            typeof(DataGridView)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
                .SetValue(dgv, true);

            // Filistin bayrağı watermark — Paint event
            dgv.Paint -= DataGridView_PalestineWatermark;
            dgv.Paint += DataGridView_PalestineWatermark;

            ApplyScrollBarTheme(dgv);
        }

        internal static void StyleListView(ListView lv)
        {
            lv.BorderStyle = BorderStyle.None;
            lv.FullRowSelect = true;
            lv.GridLines = false;
            lv.Font = FontBody;
            lv.ForeColor = TextPrimary;
            lv.BackColor = SurfaceColor;

            // OwnerDraw — kolon başlıkları ve satırlar dark mode'da düzgün görünsün
            if (!lv.OwnerDraw)
            {
                lv.OwnerDraw = true;
                lv.DrawColumnHeader += ListView_DrawColumnHeader;
                lv.DrawItem += ListView_DrawItem;
                lv.DrawSubItem += ListView_DrawSubItem;
            }

            ApplyScrollBarTheme(lv);
        }

        private static void ListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using SolidBrush bgBrush = new(GridHeaderBack);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            // Alt kenarlık çizgisi
            using Pen borderPen = new(BorderColor);
            e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1,
                                            e.Bounds.Right, e.Bounds.Bottom - 1);

            TextRenderer.DrawText(e.Graphics, e.Header?.Text, FontBody,
                new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                GridHeaderText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static void ListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            // DrawSubItem ile çizilecek; burada sadece arka planı hazırla
            e.DrawDefault = false;
        }

        private static void ListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            Color bgColor;
            Color fgColor;

            if (e.Item is not null && e.Item.Selected)
            {
                bgColor = GridSelectionBack;
                fgColor = TextPrimary;
            }
            else
            {
                bgColor = (e.ItemIndex % 2 == 0) ? SurfaceColor : GridAlternateRow;
                fgColor = TextPrimary;
            }

            using SolidBrush bgBrush = new(bgColor);
            e.Graphics!.FillRectangle(bgBrush, e.Bounds);

            string text = e.SubItem?.Text ?? string.Empty;
            TextRenderer.DrawText(e.Graphics, text, FontBody,
                new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                fgColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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

        // ═══════════════ SCROLLBAR DARK MODE ═══════════════

        /// <summary>
        /// Kontrolün native scrollbar'ını dark/light temaya göre ayarlar.
        /// Dark modda "DarkMode_Explorer" teması uygulanır; light modda varsayılan "Explorer" temasına döner.
        /// HandleCreated sonrası çağrılmalıdır.
        /// </summary>
        internal static void ApplyScrollBarTheme(Control control)
        {
            if (!control.IsHandleCreated)
                return;

            string theme = CurrentTheme == ThemeMode.Light ? "Explorer" : "DarkMode_Explorer";
            NativeMethods.SetWindowTheme(control.Handle, theme, null);
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

        /// <summary>
        /// Filistin bayrağını transparan watermark olarak çizer.
        /// 3 yatay şerit (siyah-beyaz-yeşil) ve sol tarafta kırmızı üçgen.
        /// <paramref name="opacity"/> değeri 0.0–1.0 arasında alfa çarpanını belirler.
        /// </summary>
        internal static void DrawPalestineFlagWatermark(Graphics g, Size clientSize, float opacity = 1.0f)
        {
            const float flagRatio = 2f; // width:height = 2:1

            int flagWidth = (int)(clientSize.Width * 0.55f);
            int flagHeight = (int)(flagWidth / flagRatio);

            if (flagWidth < 100 || flagHeight < 50)
                return;

            int x = (clientSize.Width - flagWidth) / 2;
            int y = (clientSize.Height - flagHeight) / 2;
            int stripeHeight = flagHeight / 3;
            int lastStripeHeight = flagHeight - stripeHeight * 2;

            SmoothingMode prevSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Siyah şerit (üst)
            using (SolidBrush blackBrush = new(Color.FromArgb((int)(35 * opacity), 160, 160, 160)))
                g.FillRectangle(blackBrush, x, y, flagWidth, stripeHeight);

            // Beyaz şerit (orta)
            using (SolidBrush whiteBrush = new(Color.FromArgb((int)(40 * opacity), 255, 255, 255)))
                g.FillRectangle(whiteBrush, x, y + stripeHeight, flagWidth, stripeHeight);

            // Yeşil şerit (alt) — #009736
            using (SolidBrush greenBrush = new(Color.FromArgb((int)(50 * opacity), 0, 151, 54)))
                g.FillRectangle(greenBrush, x, y + stripeHeight * 2, flagWidth, lastStripeHeight);

            // Kırmızı üçgen (sol) — #CE1126
            int triangleWidth = (int)(flagWidth * 0.33f);
            Point[] triangle =
            [
                new(x, y),
                new(x + triangleWidth, y + flagHeight / 2),
                new(x, y + flagHeight)
            ];

            using (SolidBrush redBrush = new(Color.FromArgb((int)(55 * opacity), 206, 17, 38)))
                g.FillPolygon(redBrush, triangle);

            // İnce kenarlık
            using (Pen borderPen = new(Color.FromArgb((int)(25 * opacity), 200, 200, 200), 1f))
                g.DrawRectangle(borderPen, x, y, flagWidth, flagHeight);

            g.SmoothingMode = prevSmoothing;
        }

        /// <summary>DataGridView arka planına transparan Filistin bayrağı watermark çizer.</summary>
        private static void DataGridView_PalestineWatermark(object? sender, PaintEventArgs e)
        {
            if (CurrentTheme != ThemeMode.OzgurFilistin)
                return;

            if (sender is not DataGridView dgv)
                return;

            DrawPalestineFlagWatermark(e.Graphics, dgv.ClientSize, 0.5f);
        }
    }
}
