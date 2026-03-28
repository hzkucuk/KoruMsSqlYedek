using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern form temel sınıfı — tutarlı tema, DPI-awareness, tüm alt formlar bu sınıftan türer.
    /// </summary>
    internal class ModernFormBase : Form
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

        private static void ApplyControlTheme(Control c)
        {
            // DataGridView
            if (c is DataGridView dgv)
            {
                ModernTheme.StyleDataGridView(dgv);
                return;
            }

            // ListView
            if (c is ListView lv)
            {
                ModernTheme.StyleListView(lv);
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
        }
    }
}
