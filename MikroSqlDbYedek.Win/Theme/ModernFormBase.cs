using System;
using System.Drawing;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
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
                pnl.BackColor = ModernTheme.BackgroundColor;
                return;
            }
        }
    }
}
