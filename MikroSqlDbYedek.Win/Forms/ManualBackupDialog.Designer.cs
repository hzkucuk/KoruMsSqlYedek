namespace MikroSqlDbYedek.Win.Forms
{
    partial class ManualBackupDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            _tlpMain = new System.Windows.Forms.TableLayoutPanel();
            _lblPlan = new System.Windows.Forms.Label();
            _cmbPlan = new System.Windows.Forms.ComboBox();
            _lblBackupType = new System.Windows.Forms.Label();
            _cmbBackupType = new System.Windows.Forms.ComboBox();
            _lblDatabases = new System.Windows.Forms.Label();
            _clbDatabases = new System.Windows.Forms.CheckedListBox();
            _lblStatus = new System.Windows.Forms.Label();
            _progressBar = new System.Windows.Forms.ProgressBar();
            _txtLog = new System.Windows.Forms.TextBox();
            _flpButtons = new System.Windows.Forms.FlowLayoutPanel();
            _btnStart = new System.Windows.Forms.Button();
            _btnCancelBackup = new System.Windows.Forms.Button();
            _btnClose = new System.Windows.Forms.Button();

            _tlpMain.SuspendLayout();
            _flpButtons.SuspendLayout();
            SuspendLayout();

            // ===== Main TLP =====
            _tlpMain.ColumnCount = 2;
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpMain.Padding = new System.Windows.Forms.Padding(8);
            _tlpMain.Name = "_tlpMain";
            _tlpMain.RowCount = 7;
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // Row 0 — Plan
            _tlpMain.Controls.Add(_lblPlan, 0, 0);
            _tlpMain.Controls.Add(_cmbPlan, 1, 0);
            _lblPlan.Text = "Plan:";
            _lblPlan.AutoSize = true;
            _lblPlan.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblPlan.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _cmbPlan.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbPlan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbPlan.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _cmbPlan.SelectedIndexChanged += OnPlanSelectedChanged;

            // Row 1 — Backup type
            _tlpMain.Controls.Add(_lblBackupType, 0, 1);
            _tlpMain.Controls.Add(_cmbBackupType, 1, 1);
            _lblBackupType.Text = "Yedek T\u00fcr\u00fc:";
            _lblBackupType.AutoSize = true;
            _lblBackupType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _lblBackupType.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);
            _cmbBackupType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbBackupType.Items.AddRange(new object[] { "Full (Tam)", "Differential (Fark)", "Incremental (Art\u0131r\u0131ml\u0131)" });
            _cmbBackupType.SelectedIndex = 0;
            _cmbBackupType.Width = 200;
            _cmbBackupType.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            // Row 2 — Databases label
            _tlpMain.Controls.Add(_lblDatabases, 0, 2);
            _lblDatabases.Text = "Veritabanlar\u0131:";
            _lblDatabases.AutoSize = true;
            _lblDatabases.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top;
            _lblDatabases.Margin = new System.Windows.Forms.Padding(3, 8, 8, 3);

            // Row 3 — Databases checklist
            _tlpMain.Controls.Add(_clbDatabases, 1, 2);
            _tlpMain.SetRowSpan(_clbDatabases, 2);
            _clbDatabases.Dock = System.Windows.Forms.DockStyle.Fill;
            _clbDatabases.CheckOnClick = true;
            _clbDatabases.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            _clbDatabases.ItemCheck += OnDatabaseItemCheck;

            // Row 4 — Status
            _tlpMain.Controls.Add(_lblStatus, 0, 4);
            _tlpMain.SetColumnSpan(_lblStatus, 2);
            _lblStatus.Text = "Haz\u0131r.";
            _lblStatus.AutoSize = true;
            _lblStatus.Font = Theme.ModernTheme.FontBodyBold;
            _lblStatus.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblStatus.Margin = new System.Windows.Forms.Padding(3, 10, 3, 3);

            // Row 5 — Progress bar
            _tlpMain.Controls.Add(_progressBar, 0, 5);
            _tlpMain.SetColumnSpan(_progressBar, 2);
            _progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            _progressBar.Height = 22;
            _progressBar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            _progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            _progressBar.ForeColor = Theme.ModernTheme.AccentPrimary;

            // Row 6 — Log output
            _tlpMain.Controls.Add(_txtLog, 0, 6);
            _tlpMain.SetColumnSpan(_txtLog, 2);
            _txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtLog.Multiline = true;
            _txtLog.ReadOnly = true;
            _txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            _txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            _txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            _txtLog.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            _txtLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);

            // ===== Bottom Buttons =====
            _flpButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            _flpButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            _flpButtons.AutoSize = true;
            _flpButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _flpButtons.Padding = new System.Windows.Forms.Padding(0, 4, 8, 8);
            _flpButtons.BackColor = Theme.ModernTheme.SurfaceColor;
            _flpButtons.Controls.Add(_btnClose);
            _flpButtons.Controls.Add(_btnCancelBackup);
            _flpButtons.Controls.Add(_btnStart);

            _btnStart.Text = "\u25B6 Yedeklemeyi Ba\u015flat";
            _btnStart.Size = new System.Drawing.Size(140, 32);
            _btnStart.Margin = new System.Windows.Forms.Padding(4);
            _btnStart.Click += OnStartClick;

            _btnCancelBackup.Text = "\u25A0 \u0130ptal Et";
            _btnCancelBackup.Size = new System.Drawing.Size(100, 32);
            _btnCancelBackup.Margin = new System.Windows.Forms.Padding(4);
            _btnCancelBackup.Enabled = false;
            _btnCancelBackup.Click += OnCancelBackupClick;

            _btnClose.Text = "Kapat";
            _btnClose.Size = new System.Drawing.Size(90, 32);
            _btnClose.Margin = new System.Windows.Forms.Padding(4);
            _btnClose.Click += OnCloseClick;

            // ===== Form =====
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            ClientSize = new System.Drawing.Size(580, 520);
            Controls.Add(_tlpMain);
            Controls.Add(_flpButtons);
            Font = Theme.ModernTheme.FontBody;
            MinimumSize = new System.Drawing.Size(500, 400);
            Name = "ManualBackupDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "MikroSqlDbYedek \u2014 Manuel Yedekleme";

            _tlpMain.ResumeLayout(false);
            _tlpMain.PerformLayout();
            _flpButtons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _tlpMain;
        private System.Windows.Forms.Label _lblPlan;
        private System.Windows.Forms.ComboBox _cmbPlan;
        private System.Windows.Forms.Label _lblBackupType;
        private System.Windows.Forms.ComboBox _cmbBackupType;
        private System.Windows.Forms.Label _lblDatabases;
        private System.Windows.Forms.CheckedListBox _clbDatabases;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.TextBox _txtLog;
        private System.Windows.Forms.FlowLayoutPanel _flpButtons;
        private System.Windows.Forms.Button _btnStart;
        private System.Windows.Forms.Button _btnCancelBackup;
        private System.Windows.Forms.Button _btnClose;
    }
}
