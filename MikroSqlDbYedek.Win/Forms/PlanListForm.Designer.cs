namespace MikroSqlDbYedek.Win.Forms
{
    partial class PlanListForm
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

        #region Windows Form Designer üretilen kod

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _toolStrip = new System.Windows.Forms.ToolStrip();
            _tsbNew = new System.Windows.Forms.ToolStripButton();
            _tsbEdit = new System.Windows.Forms.ToolStripButton();
            _tsbDelete = new System.Windows.Forms.ToolStripButton();
            _tsSep1 = new System.Windows.Forms.ToolStripSeparator();
            _tsbExport = new System.Windows.Forms.ToolStripButton();
            _tsbImport = new System.Windows.Forms.ToolStripButton();
            _tsSep2 = new System.Windows.Forms.ToolStripSeparator();
            _tsbRefresh = new System.Windows.Forms.ToolStripButton();
            _dgvPlans = new System.Windows.Forms.DataGridView();
            _colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            _colPlanName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colDatabases = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colSchedule = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colCloudTargets = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _colCreatedAt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            _statusStrip = new System.Windows.Forms.StatusStrip();
            _tslPlanCount = new System.Windows.Forms.ToolStripStatusLabel();

            _toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvPlans).BeginInit();
            _statusStrip.SuspendLayout();
            SuspendLayout();

            // _toolStrip
            _toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                _tsbNew, _tsbEdit, _tsbDelete, _tsSep1, _tsbExport, _tsbImport, _tsSep2, _tsbRefresh
            });
            _toolStrip.Location = new System.Drawing.Point(0, 0);
            _toolStrip.Name = "_toolStrip";
            _toolStrip.Size = new System.Drawing.Size(784, 40);
            _toolStrip.TabIndex = 0;
            _toolStrip.BackColor = Theme.ModernTheme.SurfaceColor;
            _toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _toolStrip.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            _toolStrip.Renderer = new Theme.ModernToolStripRenderer();

            // _tsbNew
            _tsbNew.Name = "_tsbNew";
            _tsbNew.Text = "➕ Yeni Plan";
            _tsbNew.Click += OnNewPlanClick;

            // _tsbEdit
            _tsbEdit.Name = "_tsbEdit";
            _tsbEdit.Text = "✏️ Düzenle";
            _tsbEdit.Click += OnEditPlanClick;

            // _tsbDelete
            _tsbDelete.Name = "_tsbDelete";
            _tsbDelete.Text = "🗑️ Sil";
            _tsbDelete.Click += OnDeletePlanClick;

            // _tsSep1
            _tsSep1.Name = "_tsSep1";

            // _tsbExport
            _tsbExport.Name = "_tsbExport";
            _tsbExport.Text = "📤 Dışa Aktar";
            _tsbExport.Click += OnExportClick;

            // _tsbImport
            _tsbImport.Name = "_tsbImport";
            _tsbImport.Text = "📥 İçe Aktar";
            _tsbImport.Click += OnImportClick;

            // _tsSep2
            _tsSep2.Name = "_tsSep2";

            // _tsbRefresh
            _tsbRefresh.Name = "_tsbRefresh";
            _tsbRefresh.Text = "🔄 Yenile";
            _tsbRefresh.Click += OnRefreshClick;

            // _dgvPlans
            _dgvPlans.AllowUserToAddRows = false;
            _dgvPlans.AllowUserToDeleteRows = false;
            _dgvPlans.AllowUserToResizeRows = false;
            _dgvPlans.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            _dgvPlans.BackgroundColor = Theme.ModernTheme.SurfaceColor;
            _dgvPlans.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            _dgvPlans.GridColor = Theme.ModernTheme.DividerColor;
            _dgvPlans.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _dgvPlans.EnableHeadersVisualStyles = false;
            _dgvPlans.ColumnHeadersDefaultCellStyle.BackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvPlans.ColumnHeadersDefaultCellStyle.ForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvPlans.ColumnHeadersDefaultCellStyle.Font = Theme.ModernTheme.FontCaptionBold;
            _dgvPlans.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridHeaderBack;
            _dgvPlans.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.ModernTheme.GridHeaderText;
            _dgvPlans.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            _dgvPlans.ColumnHeadersHeight = 38;
            _dgvPlans.DefaultCellStyle.BackColor = Theme.ModernTheme.SurfaceColor;
            _dgvPlans.DefaultCellStyle.ForeColor = Theme.ModernTheme.TextPrimary;
            _dgvPlans.DefaultCellStyle.Font = Theme.ModernTheme.FontBody;
            _dgvPlans.DefaultCellStyle.SelectionBackColor = Theme.ModernTheme.GridSelection;
            _dgvPlans.DefaultCellStyle.SelectionForeColor = Theme.ModernTheme.TextOnAccent;
            _dgvPlans.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            _dgvPlans.RowTemplate.Height = 36;
            _dgvPlans.AlternatingRowsDefaultCellStyle.BackColor = Theme.ModernTheme.GridAlternateRow;
            _dgvPlans.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _dgvPlans.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                _colEnabled, _colPlanName, _colStrategy, _colDatabases, _colSchedule, _colCloudTargets, _colCreatedAt
            });
            _dgvPlans.Dock = System.Windows.Forms.DockStyle.Fill;
            _dgvPlans.Location = new System.Drawing.Point(0, 25);
            _dgvPlans.MultiSelect = false;
            _dgvPlans.Name = "_dgvPlans";
            _dgvPlans.ReadOnly = true;
            _dgvPlans.RowHeadersVisible = false;
            _dgvPlans.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            _dgvPlans.Size = new System.Drawing.Size(784, 414);
            _dgvPlans.TabIndex = 1;
            _dgvPlans.CellDoubleClick += OnGridCellDoubleClick;

            // Columns
            _colEnabled.HeaderText = "Aktif";
            _colEnabled.Name = "_colEnabled";
            _colEnabled.ReadOnly = true;
            _colEnabled.Width = 45;
            _colEnabled.FillWeight = 30;

            _colPlanName.HeaderText = "Plan Adı";
            _colPlanName.Name = "_colPlanName";
            _colPlanName.ReadOnly = true;
            _colPlanName.FillWeight = 100;

            _colStrategy.HeaderText = "Strateji";
            _colStrategy.Name = "_colStrategy";
            _colStrategy.ReadOnly = true;
            _colStrategy.FillWeight = 70;

            _colDatabases.HeaderText = "Veritabanları";
            _colDatabases.Name = "_colDatabases";
            _colDatabases.ReadOnly = true;
            _colDatabases.FillWeight = 100;

            _colSchedule.HeaderText = "Zamanlama";
            _colSchedule.Name = "_colSchedule";
            _colSchedule.ReadOnly = true;
            _colSchedule.FillWeight = 80;

            _colCloudTargets.HeaderText = "Bulut";
            _colCloudTargets.Name = "_colCloudTargets";
            _colCloudTargets.ReadOnly = true;
            _colCloudTargets.FillWeight = 50;

            _colCreatedAt.HeaderText = "Oluşturulma";
            _colCreatedAt.Name = "_colCreatedAt";
            _colCreatedAt.ReadOnly = true;
            _colCreatedAt.FillWeight = 60;

            // _statusStrip
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _tslPlanCount });
            _statusStrip.Location = new System.Drawing.Point(0, 439);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Size = new System.Drawing.Size(784, 22);
            _statusStrip.TabIndex = 2;

            _tslPlanCount.Name = "_tslPlanCount";
            _tslPlanCount.Text = "Toplam 0 plan";

            // StatusStrip modern styling
            _statusStrip.BackColor = Theme.ModernTheme.SurfaceColor;
            _statusStrip.ForeColor = Theme.ModernTheme.TextSecondary;
            _statusStrip.Font = Theme.ModernTheme.FontCaption;
            _statusStrip.SizingGrip = false;
            _statusStrip.Renderer = new Theme.ModernToolStripRenderer();

            // PlanListForm
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            ClientSize = new System.Drawing.Size(784, 500);
            Controls.Add(_dgvPlans);
            Controls.Add(_toolStrip);
            Controls.Add(_statusStrip);
            Font = Theme.ModernTheme.FontBody;
            MinimumSize = new System.Drawing.Size(600, 380);
            Name = "PlanListForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Yedekleme Planları";

            _toolStrip.ResumeLayout(false);
            _toolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_dgvPlans).EndInit();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip _toolStrip;
        private System.Windows.Forms.ToolStripButton _tsbNew;
        private System.Windows.Forms.ToolStripButton _tsbEdit;
        private System.Windows.Forms.ToolStripButton _tsbDelete;
        private System.Windows.Forms.ToolStripSeparator _tsSep1;
        private System.Windows.Forms.ToolStripButton _tsbExport;
        private System.Windows.Forms.ToolStripButton _tsbImport;
        private System.Windows.Forms.ToolStripSeparator _tsSep2;
        private System.Windows.Forms.ToolStripButton _tsbRefresh;
        private System.Windows.Forms.DataGridView _dgvPlans;
        private System.Windows.Forms.DataGridViewCheckBoxColumn _colEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colPlanName;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colStrategy;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colDatabases;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colSchedule;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colCloudTargets;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colCreatedAt;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _tslPlanCount;
    }
}
