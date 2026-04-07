namespace KoruMsSqlYedek.Win.Forms
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _pnlLogo = new System.Windows.Forms.Panel();
            _lblAppName = new System.Windows.Forms.Label();
            _lblVersion = new System.Windows.Forms.Label();
            _lblDescription = new System.Windows.Forms.Label();
            _divider1 = new Theme.ModernDivider();
            _lblCopyright = new System.Windows.Forms.Label();
            _lblDeveloper = new System.Windows.Forms.Label();
            _lnkGitHub = new System.Windows.Forms.LinkLabel();
            _divider2 = new Theme.ModernDivider();
            _lblCreditsTitle = new System.Windows.Forms.Label();
            _rtbCredits = new System.Windows.Forms.RichTextBox();
            _divider3 = new Theme.ModernDivider();
            _lblRuntime = new System.Windows.Forms.Label();
            _btnClose = new Theme.ModernButton();

            SuspendLayout();

            // _pnlLogo
            _pnlLogo.Location = new System.Drawing.Point(0, 0);
            _pnlLogo.Size = new System.Drawing.Size(420, 120);
            _pnlLogo.Dock = System.Windows.Forms.DockStyle.Top;
            _pnlLogo.BackColor = Theme.ModernTheme.SurfaceColor;

            // _lblAppName
            _lblAppName.AutoSize = true;
            _lblAppName.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            _lblAppName.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblAppName.Location = new System.Drawing.Point(120, 24);
            _lblAppName.Text = "Koru MsSql Yedek";

            // _lblVersion
            _lblVersion.AutoSize = true;
            _lblVersion.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular);
            _lblVersion.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblVersion.Location = new System.Drawing.Point(122, 64);
            _lblVersion.Text = "v0.97.0";

            // _lblDescription
            _lblDescription.AutoSize = true;
            _lblDescription.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblDescription.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            _lblDescription.Location = new System.Drawing.Point(122, 88);
            _lblDescription.Text = "SQL Server Yedekleme & Bulut Senkronizasyon Sistemi";

            // _divider1
            _divider1.Location = new System.Drawing.Point(20, 130);
            _divider1.Size = new System.Drawing.Size(380, 1);

            // _lblCopyright
            _lblCopyright.AutoSize = true;
            _lblCopyright.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblCopyright.Location = new System.Drawing.Point(20, 146);
            _lblCopyright.Text = "\u00a9 2026 Zafer Bilgisayar";

            // _lblDeveloper
            _lblDeveloper.AutoSize = true;
            _lblDeveloper.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblDeveloper.Location = new System.Drawing.Point(20, 170);
            _lblDeveloper.Text = "Geli\u015ftirici: H\u00fczeyin K\u00fc\u00e7\u00fck";

            // _lnkGitHub
            _lnkGitHub.AutoSize = true;
            _lnkGitHub.Location = new System.Drawing.Point(20, 194);
            _lnkGitHub.Text = "\ue774  github.com/hzkucuk/KoruMsSqlYedek";
            _lnkGitHub.LinkColor = Theme.ModernTheme.AccentPrimary;
            _lnkGitHub.ActiveLinkColor = Theme.ModernTheme.AccentPrimaryHover;
            _lnkGitHub.VisitedLinkColor = Theme.ModernTheme.AccentPrimary;
            _lnkGitHub.Click += OnGitHubLinkClick;

            // _divider2
            _divider2.Location = new System.Drawing.Point(20, 224);
            _divider2.Size = new System.Drawing.Size(380, 1);

            // _lblCreditsTitle
            _lblCreditsTitle.AutoSize = true;
            _lblCreditsTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            _lblCreditsTitle.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblCreditsTitle.Location = new System.Drawing.Point(20, 236);
            _lblCreditsTitle.Text = "A\u00e7\u0131k Kaynak K\u00fct\u00fcphaneler";

            // _rtbCredits
            _rtbCredits.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _rtbCredits.ReadOnly = true;
            _rtbCredits.BackColor = Theme.ModernTheme.SurfaceColor;
            _rtbCredits.ForeColor = Theme.ModernTheme.TextSecondary;
            _rtbCredits.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            _rtbCredits.Location = new System.Drawing.Point(20, 258);
            _rtbCredits.Size = new System.Drawing.Size(380, 160);
            _rtbCredits.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;

            // _divider3
            _divider3.Location = new System.Drawing.Point(20, 426);
            _divider3.Size = new System.Drawing.Size(380, 1);

            // _lblRuntime
            _lblRuntime.AutoSize = true;
            _lblRuntime.ForeColor = Theme.ModernTheme.TextDisabled;
            _lblRuntime.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            _lblRuntime.Location = new System.Drawing.Point(20, 436);
            _lblRuntime.Text = ".NET Runtime";

            // _btnClose
            _btnClose.Text = "Kapat";
            _btnClose.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnClose.Size = new System.Drawing.Size(100, 36);
            _btnClose.Location = new System.Drawing.Point(300, 468);
            _btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            // Form
            CancelButton = _btnClose;
            ClientSize = new System.Drawing.Size(420, 520);
            Controls.Add(_pnlLogo);
            Controls.Add(_lblAppName);
            Controls.Add(_lblVersion);
            Controls.Add(_lblDescription);
            Controls.Add(_divider1);
            Controls.Add(_lblCopyright);
            Controls.Add(_lblDeveloper);
            Controls.Add(_lnkGitHub);
            Controls.Add(_divider2);
            Controls.Add(_lblCreditsTitle);
            Controls.Add(_rtbCredits);
            Controls.Add(_divider3);
            Controls.Add(_lblRuntime);
            Controls.Add(_btnClose);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Hakk\u0131nda \u2014 Koru MsSql Yedek";

            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Panel _pnlLogo;
        private System.Windows.Forms.Label _lblAppName;
        private System.Windows.Forms.Label _lblVersion;
        private System.Windows.Forms.Label _lblDescription;
        private Theme.ModernDivider _divider1;
        private System.Windows.Forms.Label _lblCopyright;
        private System.Windows.Forms.Label _lblDeveloper;
        private System.Windows.Forms.LinkLabel _lnkGitHub;
        private Theme.ModernDivider _divider2;
        private System.Windows.Forms.Label _lblCreditsTitle;
        private System.Windows.Forms.RichTextBox _rtbCredits;
        private Theme.ModernDivider _divider3;
        private System.Windows.Forms.Label _lblRuntime;
        private Theme.ModernButton _btnClose;
    }
}
