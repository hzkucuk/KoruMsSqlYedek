namespace KoruMsSqlYedek.Win.Forms
{
    partial class RestoreDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            _tlpMain       = new System.Windows.Forms.TableLayoutPanel();
            _pnlHeader     = new System.Windows.Forms.Panel();
            _lblHeader     = new System.Windows.Forms.Label();
            _dgvHistory    = new System.Windows.Forms.DataGridView();
            _colDate       = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colDatabase   = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colType       = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colFile       = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colSize       = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colStatus     = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _tlpOptions    = new System.Windows.Forms.TableLayoutPanel();
            _lblTargetDb   = new System.Windows.Forms.Label();
            _txtTargetDb   = new Theme.ModernTextBox();
            _chkPreBackup  = new Theme.ModernCheckBox();
            _progressBar   = new Theme.ModernProgressBar();
            _txtLog        = new System.Windows.Forms.TextBox();
            _pnlButtons    = new System.Windows.Forms.Panel();
            _btnVerify     = new Theme.ModernButton();
            _btnRestore    = new Theme.ModernButton();
            _btnClose      = new Theme.ModernButton();

            ((System.ComponentModel.ISupportInitialize)_dgvHistory).BeginInit();
            _tlpMain.SuspendLayout();
            _tlpOptions.SuspendLayout();
            SuspendLayout();

            // ── _tlpMain ────────────────────────────────────────────
            _tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpMain.ColumnCount = 1;
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpMain.RowCount = 6;
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));   // header
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));   // grid
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));        // options
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));   // progress
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));   // log
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));   // buttons
            _tlpMain.Margin = new System.Windows.Forms.Padding(0);
            _tlpMain.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            _tlpMain.Controls.Add(_pnlHeader, 0, 0);
            _tlpMain.Controls.Add(_dgvHistory, 0, 1);
            _tlpMain.Controls.Add(_tlpOptions, 0, 2);
            _tlpMain.Controls.Add(_progressBar, 0, 3);
            _tlpMain.Controls.Add(_txtLog, 0, 4);
            _tlpMain.Controls.Add(_pnlButtons, 0, 5);

            // ── _pnlHeader ──────────────────────────────────────────
            _pnlHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlHeader.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            _pnlHeader.Controls.Add(_lblHeader);

            _lblHeader.AutoSize = false;
            _lblHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            _lblHeader.Font = Theme.ModernTheme.FontTitle;
            _lblHeader.ForeColor = Theme.ModernTheme.AccentPrimary;
            _lblHeader.Text = "Veritabanını Geri Yükle";
            _lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            _lblHeader.AccessibleName = "Başlık";

            // ── _dgvHistory ─────────────────────────────────────────
            _dgvHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvHistory.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            _dgvHistory.AllowUserToAddRows = false;
            _dgvHistory.AllowUserToDeleteRows = false;
            _dgvHistory.AllowUserToResizeRows = false;
            _dgvHistory.ReadOnly = true;
            _dgvHistory.MultiSelect = false;
            _dgvHistory.RowHeadersVisible = false;
            _dgvHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            _dgvHistory.BackgroundColor = Theme.ModernTheme.SurfaceColor;
            _dgvHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvHistory.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvHistory.GridColor = Theme.ModernTheme.DividerColor;
            _dgvHistory.EnableHeadersVisualStyles = false;
            _dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvHistory.ColumnHeadersDefaultCellStyle.Font = Theme.ModernTheme.FontCaptionBold;
            _dgvHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvHistory.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvHistory.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvHistory.ColumnHeadersHeight = 34;
            _dgvHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvHistory.DefaultCellStyle.BackColor = Theme.ModernTheme.SurfaceColor;
            _dgvHistory.DefaultCellStyle.ForeColor = Theme.ModernTheme.TextPrimary;
            _dgvHistory.DefaultCellStyle.Font = Theme.ModernTheme.FontBody;
            _dgvHistory.DefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridSelection;
            _dgvHistory.DefaultCellStyle.SelectionForeColor = Theme.ModernTheme.TextOnAccent;
            _dgvHistory.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(6, 3, 6, 3);
            _dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = Theme.ModernTheme.GridAlternateRow;
            _dgvHistory.RowTemplate.Height = 32;
            _dgvHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                _colDate, _colDatabase, _colType, _colFile, _colSize, _colStatus });
            _dgvHistory.SelectionChanged += OnHistorySelectionChanged;
            _dgvHistory.AccessibleName = "Yedek Geçmişi";

            _colDate.HeaderText = "Tarih"; _colDate.ReadOnly = true; _colDate.FillWeight = 80;
            _colDatabase.HeaderText = "Veritabanı"; _colDatabase.ReadOnly = true; _colDatabase.FillWeight = 100;
            _colType.HeaderText = "Tür"; _colType.ReadOnly = true; _colType.FillWeight = 60;
            _colFile.HeaderText = "Dosya"; _colFile.ReadOnly = true; _colFile.FillWeight = 200;
            _colSize.HeaderText = "Boyut"; _colSize.ReadOnly = true; _colSize.FillWeight = 60;
            _colSize.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            _colStatus.HeaderText = "Durum"; _colStatus.ReadOnly = true; _colStatus.FillWeight = 60;

            // ── _tlpOptions ─────────────────────────────────────────
            _tlpOptions.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpOptions.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            _tlpOptions.ColumnCount = 3;
            _tlpOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpOptions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpOptions.RowCount = 1;
            _tlpOptions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpOptions.Controls.Add(_lblTargetDb, 0, 0);
            _tlpOptions.Controls.Add(_txtTargetDb, 1, 0);
            _tlpOptions.Controls.Add(_chkPreBackup, 2, 0);

            _lblTargetDb.Text = "Hedef veritabanı:";
            _lblTargetDb.AutoSize = true;
            _lblTargetDb.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblTargetDb.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            _lblTargetDb.ForeColor = Theme.ModernTheme.TextPrimary;

            _txtTargetDb.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtTargetDb.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            _txtTargetDb.AccessibleName = "Hedef veritabanı adı";

            _chkPreBackup.Text = "Restore öncesi güvenlik yedeği al";
            _chkPreBackup.Checked = true;
            _chkPreBackup.AutoSize = true;
            _chkPreBackup.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _chkPreBackup.AccessibleName = "Restore öncesi yedek al";

            // ── _progressBar ────────────────────────────────────────
            _progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            _progressBar.Margin = new System.Windows.Forms.Padding(0, 2, 0, 2);
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;
            _progressBar.Value = 0;

            // ── _txtLog ─────────────────────────────────────────────
            _txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtLog.Margin = new System.Windows.Forms.Padding(0, 0, 0, 4);
            _txtLog.Multiline = true;
            _txtLog.ReadOnly = true;
            _txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            _txtLog.BackColor = Theme.ModernTheme.SurfaceColor;
            _txtLog.ForeColor = Theme.ModernTheme.TextSecondary;
            _txtLog.Font = Theme.ModernTheme.FontCaption;
            _txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _txtLog.AccessibleName = "İşlem günlüğü";

            // ── _pnlButtons ─────────────────────────────────────────
            _pnlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            _pnlButtons.Margin = new System.Windows.Forms.Padding(0);
            _pnlButtons.Controls.Add(_btnClose);
            _pnlButtons.Controls.Add(_btnRestore);
            _pnlButtons.Controls.Add(_btnVerify);

            _btnVerify.Text = "Doğrula";
            _btnVerify.Size = new System.Drawing.Size(100, 34);
            _btnVerify.Location = new System.Drawing.Point(0, 8);
            _btnVerify.Enabled = false;
            _btnVerify.AccessibleName = "Yedek dosyasını doğrula";
            _btnVerify.AccessibleDescription = "Seçili yedeği RESTORE VERIFYONLY ile kontrol eder";
            _btnVerify.Click += OnVerifyClick;

            _btnRestore.Text = "Geri Yükle";
            _btnRestore.Size = new System.Drawing.Size(110, 34);
            _btnRestore.Location = new System.Drawing.Point(108, 8);
            _btnRestore.Enabled = false;
            _btnRestore.AccessibleName = "Veritabanını geri yükle";
            _btnRestore.AccessibleDescription = "Seçili yedekten veritabanını geri yükler";
            _btnRestore.Click += OnRestoreClick;

            _btnClose.Text = "Kapat";
            _btnClose.Size = new System.Drawing.Size(90, 34);
            _btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            _btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            _btnClose.AccessibleName = "Kapat";
            _btnClose.Click += OnCloseClick;

            // ── Form ────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 600);
            MinimumSize = new System.Drawing.Size(760, 500);
            Controls.Add(_tlpMain);
            Name = "RestoreDialog";
            Text = "Veritabanını Geri Yükle";
            CancelButton = _btnClose;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

            ((System.ComponentModel.ISupportInitialize)_dgvHistory).EndInit();
            _tlpOptions.ResumeLayout(false);
            _tlpMain.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _tlpMain;
        private System.Windows.Forms.Panel _pnlHeader;
        private System.Windows.Forms.Label _lblHeader;
        private System.Windows.Forms.DataGridView _dgvHistory;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colDatabase;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colFile;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colStatus;
        private System.Windows.Forms.TableLayoutPanel _tlpOptions;
        private System.Windows.Forms.Label _lblTargetDb;
        private Theme.ModernTextBox _txtTargetDb;
        private Theme.ModernCheckBox _chkPreBackup;
        private Theme.ModernProgressBar _progressBar;
        private System.Windows.Forms.TextBox _txtLog;
        private System.Windows.Forms.Panel _pnlButtons;
        private Theme.ModernButton _btnVerify;
        private Theme.ModernButton _btnRestore;
        private Theme.ModernButton _btnClose;
    }
}
