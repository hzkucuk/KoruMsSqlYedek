namespace MikroSqlDbYedek.Win.Forms
{
    partial class LogViewerForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer üretilen kod

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            _tlpMain = new System.Windows.Forms.TableLayoutPanel();
            _tlpToolbar = new System.Windows.Forms.TableLayoutPanel();
            _lblLogFile = new System.Windows.Forms.Label();
            _cmbLogFile = new System.Windows.Forms.ComboBox();
            _lblLevel = new System.Windows.Forms.Label();
            _cmbLevel = new System.Windows.Forms.ComboBox();
            _lblSearch = new System.Windows.Forms.Label();
            _txtSearch = new System.Windows.Forms.TextBox();
            _btnRefresh = new System.Windows.Forms.Button();
            _btnExport = new System.Windows.Forms.Button();
            _btnClearFilter = new System.Windows.Forms.Button();
            _chkAutoTail = new System.Windows.Forms.CheckBox();
            _dgvLogs = new System.Windows.Forms.DataGridView();
            _colTimestamp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colMessage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _statusStrip = new System.Windows.Forms.StatusStrip();
            _tslTotalLines = new System.Windows.Forms.ToolStripStatusLabel();
            _tslFilteredLines = new System.Windows.Forms.ToolStripStatusLabel();

            ((System.ComponentModel.ISupportInitialize)_dgvLogs).BeginInit();
            _tlpMain.SuspendLayout();
            _tlpToolbar.SuspendLayout();
            _statusStrip.SuspendLayout();
            SuspendLayout();

            // _tlpMain
            _tlpMain.ColumnCount = 1;
            _tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpMain.Controls.Add(_tlpToolbar, 0, 0);
            _tlpMain.Controls.Add(_dgvLogs, 0, 1);
            _tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpMain.Location = new System.Drawing.Point(0, 0);
            _tlpMain.Name = "_tlpMain";
            _tlpMain.Padding = new System.Windows.Forms.Padding(4);
            _tlpMain.RowCount = 2;
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpMain.Size = new System.Drawing.Size(884, 511);

            // _tlpToolbar — 2 rows: file/level selectors, search/buttons
            _tlpToolbar.AutoSize = true;
            _tlpToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            _tlpToolbar.ColumnCount = 8;
            _tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            _tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            _tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            _tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpToolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            _tlpToolbar.Name = "_tlpToolbar";
            _tlpToolbar.RowCount = 2;
            _tlpToolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            _tlpToolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            // Row 0: LogFile + Level
            _tlpToolbar.Controls.Add(_lblLogFile, 0, 0);
            _tlpToolbar.Controls.Add(_cmbLogFile, 1, 0);
            _tlpToolbar.Controls.Add(_lblLevel, 2, 0);
            _tlpToolbar.Controls.Add(_cmbLevel, 3, 0);
            _tlpToolbar.Controls.Add(_chkAutoTail, 4, 0);

            // Row 1: Search + Buttons
            _tlpToolbar.Controls.Add(_lblSearch, 0, 1);
            _tlpToolbar.Controls.Add(_txtSearch, 1, 1);
            _tlpToolbar.SetColumnSpan(_txtSearch, 3);
            _tlpToolbar.Controls.Add(_btnClearFilter, 4, 1);
            _tlpToolbar.Controls.Add(_btnRefresh, 6, 1);
            _tlpToolbar.Controls.Add(_btnExport, 7, 1);

            // Labels and controls
            _lblLogFile.Text = "Dosya:";
            _lblLogFile.AutoSize = true;
            _lblLogFile.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLogFile.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            _cmbLogFile.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLogFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLogFile.Margin = new System.Windows.Forms.Padding(3, 4, 8, 3);
            _cmbLogFile.SelectedIndexChanged += OnLogFileChanged;

            _lblLevel.Text = "Seviye:";
            _lblLevel.AutoSize = true;
            _lblLevel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblLevel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            _cmbLevel.Dock = System.Windows.Forms.DockStyle.Fill;
            _cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cmbLevel.Margin = new System.Windows.Forms.Padding(3, 4, 8, 3);
            _cmbLevel.SelectedIndexChanged += OnLevelFilterChanged;

            _chkAutoTail.Text = "Otomatik Takip";
            _chkAutoTail.AutoSize = true;
            _chkAutoTail.Anchor = System.Windows.Forms.AnchorStyles.Left;
            _chkAutoTail.Margin = new System.Windows.Forms.Padding(8, 6, 3, 3);
            _chkAutoTail.CheckedChanged += OnAutoTailToggle;

            _lblSearch.Text = "Ara:";
            _lblSearch.AutoSize = true;
            _lblSearch.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            _lblSearch.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);

            _txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            _txtSearch.Margin = new System.Windows.Forms.Padding(3, 4, 8, 6);
            _txtSearch.TextChanged += OnSearchTextChanged;

            _btnClearFilter.Text = "Temizle";
            _btnClearFilter.AutoSize = true;
            _btnClearFilter.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            _btnClearFilter.Click += OnClearFilterClick;

            _btnRefresh.Text = "\u21BB Yenile";
            _btnRefresh.AutoSize = true;
            _btnRefresh.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            _btnRefresh.Click += OnRefreshClick;

            _btnExport.Text = "\U0001F4BE D\u0131\u015Fa Aktar";
            _btnExport.AutoSize = true;
            _btnExport.Margin = new System.Windows.Forms.Padding(3, 3, 3, 6);
            _btnExport.Click += OnExportClick;

            // _dgvLogs
            _dgvLogs.AllowUserToAddRows = false;
            _dgvLogs.AllowUserToDeleteRows = false;
            _dgvLogs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            _dgvLogs.EnableHeadersVisualStyles = false;
            _dgvLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvLogs.ColumnHeadersHeight = 36;
            _dgvLogs.ColumnHeadersDefaultCellStyle.BackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvLogs.ColumnHeadersDefaultCellStyle.ForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvLogs.ColumnHeadersDefaultCellStyle.Font = Theme.ModernTheme.FontCaptionBold;
            _dgvLogs.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvLogs.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvLogs.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvLogs.DefaultCellStyle.BackColor = Theme.ModernTheme.SurfaceColor;
            _dgvLogs.DefaultCellStyle.ForeColor = Theme.ModernTheme.TextPrimary;
            _dgvLogs.DefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridSelection;
            _dgvLogs.DefaultCellStyle.SelectionForeColor = Theme.ModernTheme.TextOnAccent;
            _dgvLogs.AlternatingRowsDefaultCellStyle.BackColor = Theme.ModernTheme.GridAlternateRow;
            _dgvLogs.GridColor = Theme.ModernTheme.DividerColor;
            _dgvLogs.BackgroundColor = Theme.ModernTheme.SurfaceColor;
            _dgvLogs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvLogs.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvLogs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
            {
                _colTimestamp, _colLevel, _colMessage
            });
            _dgvLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvLogs.Name = "_dgvLogs";
            _dgvLogs.ReadOnly = true;
            _dgvLogs.RowHeadersVisible = false;
            _dgvLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvLogs.Font = new System.Drawing.Font("Consolas", 9F);

            // Columns
            _colTimestamp.HeaderText = "Zaman";
            _colTimestamp.Name = "_colTimestamp";
            _colTimestamp.Width = 160;

            _colLevel.HeaderText = "Seviye";
            _colLevel.Name = "_colLevel";
            _colLevel.Width = 55;

            _colMessage.HeaderText = "Mesaj";
            _colMessage.Name = "_colMessage";
            _colMessage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            _colMessage.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;

            // _statusStrip
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                _tslTotalLines, _tslFilteredLines
            });
            _statusStrip.Location = new System.Drawing.Point(0, 511);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Size = new System.Drawing.Size(884, 22);
            _statusStrip.BackColor = Theme.ModernTheme.SurfaceColor;
            _statusStrip.ForeColor = Theme.ModernTheme.TextSecondary;
            _statusStrip.Font = Theme.ModernTheme.FontCaption;
            _statusStrip.SizingGrip = false;
            _statusStrip.Renderer = new Theme.ModernToolStripRenderer();

            _tslTotalLines.Name = "_tslTotalLines";
            _tslTotalLines.Size = new System.Drawing.Size(434, 17);
            _tslTotalLines.Spring = true;
            _tslTotalLines.Text = "0 kayıt";
            _tslTotalLines.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            _tslFilteredLines.Name = "_tslFilteredLines";
            _tslFilteredLines.Size = new System.Drawing.Size(434, 17);
            _tslFilteredLines.Text = "0 gösteriliyor";
            _tslFilteredLines.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // Form
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            ClientSize = new System.Drawing.Size(884, 533);
            Controls.Add(_tlpMain);
            Controls.Add(_statusStrip);
            Font = Theme.ModernTheme.FontBody;
            MinimumSize = new System.Drawing.Size(600, 400);
            Name = "LogViewerForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "MikroSqlDbYedek — Log G\u00f6r\u00fcnt\u00fcleyici";

            ((System.ComponentModel.ISupportInitialize)_dgvLogs).EndInit();
            _tlpMain.ResumeLayout(false);
            _tlpMain.PerformLayout();
            _tlpToolbar.ResumeLayout(false);
            _tlpToolbar.PerformLayout();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _tlpMain;
        private System.Windows.Forms.TableLayoutPanel _tlpToolbar;
        private System.Windows.Forms.Label _lblLogFile;
        private System.Windows.Forms.ComboBox _cmbLogFile;
        private System.Windows.Forms.Label _lblLevel;
        private System.Windows.Forms.ComboBox _cmbLevel;
        private System.Windows.Forms.Label _lblSearch;
        private System.Windows.Forms.TextBox _txtSearch;
        private System.Windows.Forms.Button _btnRefresh;
        private System.Windows.Forms.Button _btnExport;
        private System.Windows.Forms.Button _btnClearFilter;
        private System.Windows.Forms.CheckBox _chkAutoTail;
        private System.Windows.Forms.DataGridView _dgvLogs;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colTimestamp;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colMessage;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _tslTotalLines;
        private System.Windows.Forms.ToolStripStatusLabel _tslFilteredLines;
    }
}
