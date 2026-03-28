using System;
using System.Drawing;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern form temel sınıfı — tutarlı tema, DPI-awareness, tüm alt formlar bu sınıftan türer.
    /// </summary>
    public class ModernFormBase : Form
    {
        public ModernFormBase()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Tema uygula
            BackColor = ModernTheme.BackgroundColor;
            Font = ModernTheme.FontBody;
            ForeColor = ModernTheme.TextPrimary;

            // Global renderer
            ToolStripManager.Renderer = new ModernToolStripRenderer();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Windows 10/11 dark title bar
            if (ModernTheme.CurrentTheme == ThemeMode.Dark)
            {
                try
                {
                    int value = 1;
                    NativeMethods.DwmSetWindowAttribute(
                        Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                        ref value, sizeof(int));
                }
                catch { /* DWM API mevcut değilse sessizce geç */ }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyThemeToAllChildren(this);
        }

        /// <summary>
        /// Tüm alt kontrollere recursively tema uygular.
        /// </summary>
        internal static void ApplyThemeToAllChildren(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                ApplyControlTheme(c);

                if (c.HasChildren)
                {
                    ApplyThemeToAllChildren(c);
                }
            }
        }

        private static readonly Color DefaultControlText = SystemColors.ControlText;
        private static readonly Color DefaultControl = SystemColors.Control;
        private static readonly Color DefaultWindow = SystemColors.Window;

        private static void ApplyControlTheme(Control c)
        {
            // Zaten custom Modern* kontrol ise dokunma
            if (c is ModernButton || c is ModernTextBox || c is ModernComboBox
                || c is ModernCheckBox || c is ModernNumericUpDown || c is ModernProgressBar
                || c is ModernCardPanel || c is ModernGroupBox || c is ModernSearchBox
                || c is ModernToggleSwitch || c is ModernDivider || c is ModernHeaderPanel
                || c is ModernLoadingOverlay)
                return;

            // DataGridView
            if (c is DataGridView dgv)
            {
                ModernTheme.StyleDataGridView(dgv);
                // Dark scrollbar (Windows 10/11)
                try { NativeMethods.SetWindowTheme(dgv.Handle, "DarkMode_Explorer", null); } catch { }
                return;
            }

            // ListView
            if (c is ListView lv)
            {
                ModernTheme.StyleListView(lv);
                try { NativeMethods.SetWindowTheme(lv.Handle, "DarkMode_Explorer", null); } catch { }
                return;
            }

            // ToolStrip
            if (c is ToolStrip ts && !(c is StatusStrip) && !(c is MenuStrip))
            {
                ModernTheme.StyleToolStrip(ts);
                ts.Renderer = new ModernToolStripRenderer();
                return;
            }

            // StatusStrip
            if (c is StatusStrip ss)
            {
                ModernTheme.StyleStatusStrip(ss);
                ss.Renderer = new ModernToolStripRenderer();
                return;
            }

            // TabControl
            if (c is TabControl tc)
            {
                ModernTheme.StyleTabControl(tc);
                return;
            }

            // GroupBox
            if (c is GroupBox grp)
            {
                ModernTheme.StyleGroupBox(grp);
                return;
            }

            // Button
            if (c is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = ModernTheme.BorderColor;
                btn.FlatAppearance.BorderSize = 1;
                btn.BackColor = ModernTheme.SurfaceColor;
                btn.ForeColor = ModernTheme.TextPrimary;
                btn.Font = ModernTheme.FontBody;
                return;
            }

            // TextBox
            if (c is TextBox txt)
            {
                ModernTheme.StyleTextBox(txt);
                return;
            }

            // ComboBox
            if (c is ComboBox cmb)
            {
                ModernTheme.StyleComboBox(cmb);
                return;
            }

            // CheckBox
            if (c is CheckBox chk)
            {
                chk.BackColor = Color.Transparent;
                chk.ForeColor = ModernTheme.TextPrimary;
                chk.Font = ModernTheme.FontBody;
                return;
            }

            // RadioButton
            if (c is RadioButton rb)
            {
                rb.BackColor = Color.Transparent;
                rb.ForeColor = ModernTheme.TextPrimary;
                rb.Font = ModernTheme.FontBody;
                return;
            }

            // NumericUpDown
            if (c is NumericUpDown nud)
            {
                nud.BackColor = ModernTheme.SurfaceColor;
                nud.ForeColor = ModernTheme.TextPrimary;
                nud.Font = ModernTheme.FontBody;
                nud.BorderStyle = BorderStyle.FixedSingle;
                return;
            }

            // CheckedListBox
            if (c is CheckedListBox clb)
            {
                clb.BackColor = ModernTheme.SurfaceColor;
                clb.ForeColor = ModernTheme.TextPrimary;
                clb.Font = ModernTheme.FontBody;
                clb.BorderStyle = BorderStyle.None;
                return;
            }

            // Label — sadece varsayılan renkli ise değiştir (özel renkli label'ları koru)
            if (c is Label lbl)
            {
                if (lbl.ForeColor == DefaultControlText || lbl.ForeColor == Color.Black)
                    lbl.ForeColor = ModernTheme.TextPrimary;
                return;
            }

            // TabPage
            if (c is TabPage tp)
            {
                tp.BackColor = ModernTheme.BackgroundColor;
                tp.ForeColor = ModernTheme.TextPrimary;
                return;
            }

            // Panel / FlowLayoutPanel / TableLayoutPanel
            if (c is Panel pnl)
            {
                if (pnl.BackColor == DefaultControl || pnl.BackColor == DefaultWindow
                    || pnl.BackColor == Color.Transparent)
                {
                    pnl.BackColor = ModernTheme.BackgroundColor;
                }
                return;
            }
        }
    }
}
