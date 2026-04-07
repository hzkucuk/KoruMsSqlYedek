namespace KoruMsSqlYedek.Win.Forms
{
    partial class FileBackupSourceEditDialog
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
            _toolTip = new System.Windows.Forms.ToolTip(components);

            _lblSourceName = new System.Windows.Forms.Label();
            _txtSourceName = new System.Windows.Forms.TextBox();
            _treeView = new Theme.FileSystemCheckedTreeView();
            _lblStatus = new System.Windows.Forms.Label();
            _grpPatterns = new System.Windows.Forms.GroupBox();
            _lblInclude = new System.Windows.Forms.Label();
            _txtIncludePatterns = new System.Windows.Forms.TextBox();
            _lblExclude = new System.Windows.Forms.Label();
            _txtExcludePatterns = new System.Windows.Forms.TextBox();
            _lblHint = new System.Windows.Forms.Label();
            _grpOptions = new System.Windows.Forms.GroupBox();
            _chkRecursive = new System.Windows.Forms.CheckBox();
            _chkUseVss = new System.Windows.Forms.CheckBox();
            _chkEnabled = new System.Windows.Forms.CheckBox();
            _btnSave = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();

            SuspendLayout();
            _grpPatterns.SuspendLayout();
            _grpOptions.SuspendLayout();

            // ── _lblSourceName ──
            _lblSourceName.AutoSize = true;
            _lblSourceName.Location = new System.Drawing.Point(15, 18);
            _lblSourceName.Name = "_lblSourceName";
            _lblSourceName.Text = "Kaynak Ad\u0131:";

            // ── _txtSourceName ──
            _txtSourceName.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            _txtSourceName.Location = new System.Drawing.Point(120, 15);
            _txtSourceName.Name = "_txtSourceName";
            _txtSourceName.Size = new System.Drawing.Size(665, 23);
            _toolTip.SetToolTip(_txtSourceName, "Bu kayna\u011f\u0131 tan\u0131mlayan k\u0131sa bir isim.\n\u00d6rnek: Outlook PST, Proje Dosyalar\u0131");

            // ── _btnNavigate ──
            _btnNavigate = new Theme.ModernButton();
            _btnNavigate.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Right;
            _btnNavigate.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnNavigate.Location = new System.Drawing.Point(645, 46);
            _btnNavigate.Name = "_btnNavigate";
            _btnNavigate.Size = new System.Drawing.Size(140, 26);
            _btnNavigate.Text = "\uD83D\uDCC1 Konuma Git...";
            _btnNavigate.Click += OnNavigateToFolder;
            _toolTip.SetToolTip(_btnNavigate, "Bir klas\u00f6re h\u0131zl\u0131ca gidin.\nTreeView'da o konumu a\u00e7ar.");

            // ── _treeView ──
            _treeView.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            _treeView.Location = new System.Drawing.Point(15, 77);
            _treeView.Name = "_treeView";
            _treeView.Size = new System.Drawing.Size(770, 379);

            // ── _lblStatus ──
            _lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left;
            _lblStatus.AutoSize = true;
            _lblStatus.Font = Theme.ModernTheme.FontCaption;
            _lblStatus.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblStatus.Location = new System.Drawing.Point(15, 462);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Text = "Dosya se\u00e7mek i\u00e7in klas\u00f6rlere g\u00f6z at\u0131n ve onay kutular\u0131n\u0131 i\u015faretleyin";

            // ── _grpPatterns ──
            _grpPatterns.Anchor = System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            _grpPatterns.ForeColor = Theme.ModernTheme.AccentPrimary;
            _grpPatterns.Location = new System.Drawing.Point(15, 482);
            _grpPatterns.Name = "_grpPatterns";
            _grpPatterns.Size = new System.Drawing.Size(770, 98);
            _grpPatterns.Text = "Dosya Filtre Kal\u0131plar\u0131";

            _lblInclude.AutoSize = true;
            _lblInclude.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblInclude.Location = new System.Drawing.Point(8, 24);
            _lblInclude.Name = "_lblInclude";
            _lblInclude.Text = "Dahil Kal\u0131plar\u0131:";

            _txtIncludePatterns.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            _txtIncludePatterns.Location = new System.Drawing.Point(115, 21);
            _txtIncludePatterns.Name = "_txtIncludePatterns";
            _txtIncludePatterns.Size = new System.Drawing.Size(645, 23);
            _toolTip.SetToolTip(_txtIncludePatterns, "Sadece bu kal\u0131plara uyan dosyalar yedeklenir.\nBo\u015f b\u0131rak\u0131l\u0131rsa t\u00fcm dosyalar dahil edilir.\n\u00d6rnek: *.pst; *.docx; *.xlsx");

            _lblExclude.AutoSize = true;
            _lblExclude.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblExclude.Location = new System.Drawing.Point(8, 53);
            _lblExclude.Name = "_lblExclude";
            _lblExclude.Text = "Hari\u00e7 Kal\u0131plar\u0131:";

            _txtExcludePatterns.Anchor = System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            _txtExcludePatterns.Location = new System.Drawing.Point(115, 50);
            _txtExcludePatterns.Name = "_txtExcludePatterns";
            _txtExcludePatterns.Size = new System.Drawing.Size(645, 23);
            _toolTip.SetToolTip(_txtExcludePatterns, "Bu kal\u0131plara uyan dosyalar yedeklenmez.\n\u00d6rnek: *.tmp; *.log; ~$*");

            _lblHint.AutoSize = true;
            _lblHint.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblHint.Location = new System.Drawing.Point(8, 78);
            _lblHint.Name = "_lblHint";
            _lblHint.Text = "Kal\u0131plar\u0131 noktal\u0131 virg\u00fcl (;) ile ay\u0131r\u0131n \u2014 \u00d6rnek: *.pst; *.docx";

            _grpPatterns.Controls.Add(_lblInclude);
            _grpPatterns.Controls.Add(_txtIncludePatterns);
            _grpPatterns.Controls.Add(_lblExclude);
            _grpPatterns.Controls.Add(_txtExcludePatterns);
            _grpPatterns.Controls.Add(_lblHint);

            // ── _grpOptions ──
            _grpOptions.Anchor = System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right;
            _grpOptions.ForeColor = Theme.ModernTheme.AccentPrimary;
            _grpOptions.Location = new System.Drawing.Point(15, 586);
            _grpOptions.Name = "_grpOptions";
            _grpOptions.Size = new System.Drawing.Size(770, 56);
            _grpOptions.Text = "Se\u00e7enekler";

            _chkRecursive.AutoSize = true;
            _chkRecursive.Checked = true;
            _chkRecursive.CheckState = System.Windows.Forms.CheckState.Checked;
            _chkRecursive.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkRecursive.Location = new System.Drawing.Point(13, 25);
            _chkRecursive.Name = "_chkRecursive";
            _chkRecursive.Text = "Alt dizinleri dahil et";
            _toolTip.SetToolTip(_chkRecursive, "\u0130\u015faretlenirse alt klas\u00f6rlerdeki dosyalar da yedeklenir.");

            _chkUseVss.AutoSize = true;
            _chkUseVss.Checked = true;
            _chkUseVss.CheckState = System.Windows.Forms.CheckState.Checked;
            _chkUseVss.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkUseVss.Location = new System.Drawing.Point(190, 25);
            _chkUseVss.Name = "_chkUseVss";
            _chkUseVss.Text = "VSS kullan (a\u00e7\u0131k/kilitli dosyalar)";
            _toolTip.SetToolTip(_chkUseVss, "Outlook PST gibi kilitli dosyalar\u0131 yedeklemek i\u00e7in\nVolume Shadow Copy kullan\u0131r.");

            _chkEnabled.AutoSize = true;
            _chkEnabled.Checked = true;
            _chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            _chkEnabled.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkEnabled.Location = new System.Drawing.Point(440, 25);
            _chkEnabled.Name = "_chkEnabled";
            _chkEnabled.Text = "Bu kaynak aktif";
            _toolTip.SetToolTip(_chkEnabled, "Devre d\u0131\u015f\u0131 b\u0131rak\u0131l\u0131rsa bu kaynak yedekleme s\u0131ras\u0131nda atlan\u0131r.");

            _grpOptions.Controls.Add(_chkRecursive);
            _grpOptions.Controls.Add(_chkUseVss);
            _grpOptions.Controls.Add(_chkEnabled);

            // ── _btnSave ──
            _btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right;
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Location = new System.Drawing.Point(570, 652);
            _btnSave.Name = "_btnSave";
            _btnSave.Size = new System.Drawing.Size(110, 36);
            _btnSave.Text = "Kaydet";
            _btnSave.Click += OnSaveClick;

            // ── _btnCancel ──
            _btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Right;
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.Location = new System.Drawing.Point(690, 652);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new System.Drawing.Size(95, 36);
            _btnCancel.Text = "\u0130ptal";
            _btnCancel.Click += OnCancelClick;

            // ── Form ──
            AcceptButton = _btnSave;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = Theme.ModernTheme.BackgroundColor;
            CancelButton = _btnCancel;
            ClientSize = new System.Drawing.Size(800, 700);
            MinimumSize = new System.Drawing.Size(680, 560);
            Controls.Add(_lblSourceName);
            Controls.Add(_txtSourceName);
            Controls.Add(_btnNavigate);
            Controls.Add(_treeView);
            Controls.Add(_lblStatus);
            Controls.Add(_grpPatterns);
            Controls.Add(_grpOptions);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
            Font = Theme.ModernTheme.FontBody;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            Name = "FileBackupSourceEditDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Dosya Kayna\u011f\u0131";

            _grpPatterns.ResumeLayout(false);
            _grpPatterns.PerformLayout();
            _grpOptions.ResumeLayout(false);
            _grpOptions.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolTip _toolTip;
        private System.Windows.Forms.Label _lblSourceName;
        private System.Windows.Forms.TextBox _txtSourceName;
        private Theme.ModernButton _btnNavigate;
        private Theme.FileSystemCheckedTreeView _treeView;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.GroupBox _grpPatterns;
        private System.Windows.Forms.Label _lblInclude;
        private System.Windows.Forms.TextBox _txtIncludePatterns;
        private System.Windows.Forms.Label _lblExclude;
        private System.Windows.Forms.TextBox _txtExcludePatterns;
        private System.Windows.Forms.Label _lblHint;
        private System.Windows.Forms.GroupBox _grpOptions;
        private System.Windows.Forms.CheckBox _chkRecursive;
        private System.Windows.Forms.CheckBox _chkUseVss;
        private System.Windows.Forms.CheckBox _chkEnabled;
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;
    }
}
