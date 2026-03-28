namespace MikroSqlDbYedek.Win
{
    partial class MainDashboardForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            _pnlHeader = new System.Windows.Forms.Panel();
            _lblTitle = new System.Windows.Forms.Label();
            _lblSubtitle = new System.Windows.Forms.Label();

            _tlpCards = new System.Windows.Forms.TableLayoutPanel();
            _cardStatus = new Theme.ModernCardPanel();
            _lblStatusIcon = new System.Windows.Forms.Label();
            _lblStatusCaption = new System.Windows.Forms.Label();
            _lblStatusValue = new System.Windows.Forms.Label();

            _cardNextBackup = new Theme.ModernCardPanel();
            _lblNextIcon = new System.Windows.Forms.Label();
            _lblNextBackupCaption = new System.Windows.Forms.Label();
            _lblNextBackupValue = new System.Windows.Forms.Label();

            _cardActivePlans = new Theme.ModernCardPanel();
            _lblPlansIcon = new System.Windows.Forms.Label();
            _lblActivePlansCaption = new System.Windows.Forms.Label();
            _lblActivePlansValue = new System.Windows.Forms.Label();

            _pnlGrid = new Theme.ModernCardPanel();
            _lblGridTitle = new System.Windows.Forms.Label();
            _lvLastBackups = new System.Windows.Forms.ListView();
            _colDate = new System.Windows.Forms.ColumnHeader();
            _colPlan = new System.Windows.Forms.ColumnHeader();
            _colDatabase = new System.Windows.Forms.ColumnHeader();
            _colType = new System.Windows.Forms.ColumnHeader();
            _colResult = new System.Windows.Forms.ColumnHeader();
            _colSize = new System.Windows.Forms.ColumnHeader();

            _statusStrip = new System.Windows.Forms.StatusStrip();
            _tslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            _tslVersion = new System.Windows.Forms.ToolStripStatusLabel();

            SuspendLayout();
            _pnlHeader.SuspendLayout();
            _tlpCards.SuspendLayout();
            _statusStrip.SuspendLayout();

            // Header Panel
            _pnlHeader.BackColor = Theme.ModernTheme.SurfaceColor;
            _pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            _pnlHeader.Height = 70;
            _pnlHeader.Padding = new System.Windows.Forms.Padding(20, 14, 20, 8);
            _pnlHeader.Controls.Add(_lblSubtitle);
            _pnlHeader.Controls.Add(_lblTitle);

            _lblTitle.AutoSize = true;
            _lblTitle.Font = Theme.ModernTheme.FontTitle;
            _lblTitle.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblTitle.Location = new System.Drawing.Point(20, 12);
            _lblTitle.Text = "MikroSqlDbYedek";

            _lblSubtitle.AutoSize = true;
            _lblSubtitle.Font = Theme.ModernTheme.FontCaption;
            _lblSubtitle.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblSubtitle.Location = new System.Drawing.Point(22, 44);
            _lblSubtitle.Text = "SQL Server Backup & Cloud Sync Dashboard";

            // Status Cards
            _tlpCards.ColumnCount = 3;
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.34F));
            _tlpCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33F));
            _tlpCards.Dock = System.Windows.Forms.DockStyle.Top;
            _tlpCards.Height = 100;
            _tlpCards.Padding = new System.Windows.Forms.Padding(16, 8, 16, 4);
            _tlpCards.BackColor = Theme.ModernTheme.BackgroundColor;
            _tlpCards.RowCount = 1;
            _tlpCards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpCards.Controls.Add(_cardStatus, 0, 0);
            _tlpCards.Controls.Add(_cardNextBackup, 1, 0);
            _tlpCards.Controls.Add(_cardActivePlans, 2, 0);

            // Card: Status
            _cardStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardStatus.Margin = new System.Windows.Forms.Padding(4);
            _cardStatus.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            _lblStatusIcon.Text = Helpers.SymbolIconHelper.SymbolCheckmark;
            _lblStatusIcon.Font = new System.Drawing.Font("Segoe MDL2 Assets", 18F);
            _lblStatusIcon.ForeColor = Theme.ModernTheme.StatusSuccess;
            _lblStatusIcon.AutoSize = true;
            _lblStatusIcon.Location = new System.Drawing.Point(14, 16);
            _cardStatus.Controls.Add(_lblStatusIcon);
            _lblStatusCaption.AutoSize = true;
            _lblStatusCaption.Font = Theme.ModernTheme.FontCaption;
            _lblStatusCaption.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblStatusCaption.Location = new System.Drawing.Point(52, 12);
            _lblStatusCaption.Text = "Durum";
            _cardStatus.Controls.Add(_lblStatusCaption);
            _lblStatusValue.AutoSize = true;
            _lblStatusValue.Font = Theme.ModernTheme.FontSubtitle;
            _lblStatusValue.ForeColor = Theme.ModernTheme.StatusSuccess;
            _lblStatusValue.Location = new System.Drawing.Point(52, 32);
            _lblStatusValue.Text = "Haz\u0131r";
            _cardStatus.Controls.Add(_lblStatusValue);

            // Card: Next Backup
            _cardNextBackup.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardNextBackup.Margin = new System.Windows.Forms.Padding(4);
            _cardNextBackup.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            _lblNextIcon.Text = Helpers.SymbolIconHelper.SymbolClock;
            _lblNextIcon.Font = new System.Drawing.Font("Segoe MDL2 Assets", 18F);
            _lblNextIcon.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblNextIcon.AutoSize = true;
            _lblNextIcon.Location = new System.Drawing.Point(14, 16);
            _cardNextBackup.Controls.Add(_lblNextIcon);
            _lblNextBackupCaption.AutoSize = true;
            _lblNextBackupCaption.Font = Theme.ModernTheme.FontCaption;
            _lblNextBackupCaption.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblNextBackupCaption.Location = new System.Drawing.Point(52, 12);
            _lblNextBackupCaption.Text = "Son Yedekleme";
            _cardNextBackup.Controls.Add(_lblNextBackupCaption);
            _lblNextBackupValue.AutoSize = true;
            _lblNextBackupValue.Font = Theme.ModernTheme.FontSubtitle;
            _lblNextBackupValue.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblNextBackupValue.Location = new System.Drawing.Point(52, 32);
            _lblNextBackupValue.Text = "\u2014";
            _cardNextBackup.Controls.Add(_lblNextBackupValue);

            // Card: Active Plans
            _cardActivePlans.Dock = System.Windows.Forms.DockStyle.Fill;
            _cardActivePlans.Margin = new System.Windows.Forms.Padding(4);
            _cardActivePlans.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            _lblPlansIcon.Text = Helpers.SymbolIconHelper.SymbolDocument;
            _lblPlansIcon.Font = new System.Drawing.Font("Segoe MDL2 Assets", 18F);
            _lblPlansIcon.ForeColor = Theme.ModernTheme.StatusWarning;
            _lblPlansIcon.AutoSize = true;
            _lblPlansIcon.Location = new System.Drawing.Point(14, 16);
            _cardActivePlans.Controls.Add(_lblPlansIcon);
            _lblActivePlansCaption.AutoSize = true;
            _lblActivePlansCaption.Font = Theme.ModernTheme.FontCaption;
            _lblActivePlansCaption.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblActivePlansCaption.Location = new System.Drawing.Point(52, 12);
            _lblActivePlansCaption.Text = "Aktif Planlar";
            _cardActivePlans.Controls.Add(_lblActivePlansCaption);
            _lblActivePlansValue.AutoSize = true;
            _lblActivePlansValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            _lblActivePlansValue.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblActivePlansValue.Location = new System.Drawing.Point(52, 28);
            _lblActivePlansValue.Text = "0";
            _cardActivePlans.Controls.Add(_lblActivePlansValue);

            // Grid Card
            _pnlGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlGrid.Margin = new System.Windows.Forms.Padding(20, 8, 20, 8);
            _pnlGrid.Padding = new System.Windows.Forms.Padding(0, 36, 0, 0);
            _lblGridTitle.Text = "Son Yedeklemeler";
            _lblGridTitle.Font = Theme.ModernTheme.FontBodyBold;
            _lblGridTitle.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblGridTitle.AutoSize = true;
            _lblGridTitle.Location = new System.Drawing.Point(14, 10);
            _pnlGrid.Controls.Add(_lblGridTitle);
            _lvLastBackups.Dock = System.Windows.Forms.DockStyle.Fill;
            _lvLastBackups.View = System.Windows.Forms.View.Details;
            _lvLastBackups.FullRowSelect = true;
            _lvLastBackups.GridLines = false;
            _lvLastBackups.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _lvLastBackups.Font = Theme.ModernTheme.FontBody;
            _lvLastBackups.ForeColor = Theme.ModernTheme.TextPrimary;
            _lvLastBackups.BackColor = Theme.ModernTheme.SurfaceColor;
            _lvLastBackups.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { _colDate, _colPlan, _colDatabase, _colType, _colResult, _colSize });
            _lvLastBackups.UseCompatibleStateImageBehavior = false;
            _pnlGrid.Controls.Add(_lvLastBackups);
            _colDate.Text = "Tarih"; _colDate.Width = 140;
            _colPlan.Text = "Plan"; _colPlan.Width = 130;
            _colDatabase.Text = "Veritaban\u0131"; _colDatabase.Width = 130;
            _colType.Text = "T\u00fcr"; _colType.Width = 80;
            _colResult.Text = "Sonu\u00e7"; _colResult.Width = 90;
            _colSize.Text = "Boyut"; _colSize.Width = 80;

            // Status Strip
            _statusStrip.BackColor = Theme.ModernTheme.SurfaceColor;
            _statusStrip.ForeColor = Theme.ModernTheme.TextSecondary;
            _statusStrip.Font = Theme.ModernTheme.FontCaption;
            _statusStrip.SizingGrip = false;
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _tslStatus, _tslVersion });
            _tslStatus.Spring = true;
            _tslStatus.Text = "Haz\u0131r";
            _tslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            _tslVersion.Text = "v0.14.0";
            _tslVersion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // Form
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            ClientSize = new System.Drawing.Size(780, 520);
            Font = Theme.ModernTheme.FontBody;
            Controls.Add(_pnlGrid);
            Controls.Add(_tlpCards);
            Controls.Add(_pnlHeader);
            Controls.Add(_statusStrip);
            MinimumSize = new System.Drawing.Size(600, 450);
            Name = "MainDashboardForm";
            ShowInTaskbar = true;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "MikroSqlDbYedek \u2014 Dashboard";

            _pnlHeader.ResumeLayout(false);
            _pnlHeader.PerformLayout();
            _tlpCards.ResumeLayout(false);
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel _pnlHeader;
        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblSubtitle;
        private System.Windows.Forms.TableLayoutPanel _tlpCards;
        private Theme.ModernCardPanel _cardStatus;
        private System.Windows.Forms.Label _lblStatusIcon;
        private System.Windows.Forms.Label _lblStatusCaption;
        private System.Windows.Forms.Label _lblStatusValue;
        private Theme.ModernCardPanel _cardNextBackup;
        private System.Windows.Forms.Label _lblNextIcon;
        private System.Windows.Forms.Label _lblNextBackupCaption;
        private System.Windows.Forms.Label _lblNextBackupValue;
        private Theme.ModernCardPanel _cardActivePlans;
        private System.Windows.Forms.Label _lblPlansIcon;
        private System.Windows.Forms.Label _lblActivePlansCaption;
        private System.Windows.Forms.Label _lblActivePlansValue;
        private Theme.ModernCardPanel _pnlGrid;
        private System.Windows.Forms.Label _lblGridTitle;
        private System.Windows.Forms.ListView _lvLastBackups;
        private System.Windows.Forms.ColumnHeader _colDate;
        private System.Windows.Forms.ColumnHeader _colPlan;
        private System.Windows.Forms.ColumnHeader _colDatabase;
        private System.Windows.Forms.ColumnHeader _colType;
        private System.Windows.Forms.ColumnHeader _colResult;
        private System.Windows.Forms.ColumnHeader _colSize;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _tslStatus;
        private System.Windows.Forms.ToolStripStatusLabel _tslVersion;
    }
}