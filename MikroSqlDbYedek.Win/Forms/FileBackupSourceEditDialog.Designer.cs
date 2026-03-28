namespace MikroSqlDbYedek.Win.Forms
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

            _lblSourceName = new System.Windows.Forms.Label();
            _txtSourceName = new System.Windows.Forms.TextBox();
            _lblSourcePath = new System.Windows.Forms.Label();
            _txtSourcePath = new System.Windows.Forms.TextBox();
            _btnBrowsePath = new Theme.ModernButton();
            _chkRecursive = new System.Windows.Forms.CheckBox();
            _chkUseVss = new System.Windows.Forms.CheckBox();
            _chkEnabled = new System.Windows.Forms.CheckBox();
            _lblInclude = new System.Windows.Forms.Label();
            _txtIncludePatterns = new System.Windows.Forms.TextBox();
            _lblExclude = new System.Windows.Forms.Label();
            _txtExcludePatterns = new System.Windows.Forms.TextBox();
            _lblHint = new System.Windows.Forms.Label();
            _btnSave = new Theme.ModernButton();
            _btnCancel = new Theme.ModernButton();

            SuspendLayout();

            int lx = 15, tx = 145, tw = 280;
            int y = 18;

            // Kaynak Adı
            _lblSourceName.Text = "Kaynak Adı:";
            _lblSourceName.AutoSize = true;
            _lblSourceName.Location = new System.Drawing.Point(lx, y + 3);
            _txtSourceName.Location = new System.Drawing.Point(tx, y);
            _txtSourceName.Size = new System.Drawing.Size(tw, 23);

            // Kaynak Dizin
            y += 35;
            _lblSourcePath.Text = "Dizin Yolu:";
            _lblSourcePath.AutoSize = true;
            _lblSourcePath.Location = new System.Drawing.Point(lx, y + 3);
            _txtSourcePath.Location = new System.Drawing.Point(tx, y);
            _txtSourcePath.Size = new System.Drawing.Size(tw - 35, 23);
            _btnBrowsePath.Text = "...";
            _btnBrowsePath.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBrowsePath.Size = new System.Drawing.Size(30, 28);
            _btnBrowsePath.Location = new System.Drawing.Point(tx + tw - 30, y);
            _btnBrowsePath.Click += OnBrowseSourcePath;

            // Include Patterns
            y += 40;
            _lblInclude.Text = "Dahil Kalıpları:";
            _lblInclude.AutoSize = true;
            _lblInclude.Location = new System.Drawing.Point(lx, y + 3);
            _txtIncludePatterns.Location = new System.Drawing.Point(tx, y);
            _txtIncludePatterns.Size = new System.Drawing.Size(tw, 23);

            // Exclude Patterns
            y += 35;
            _lblExclude.Text = "Hariç Kalıpları:";
            _lblExclude.AutoSize = true;
            _lblExclude.Location = new System.Drawing.Point(lx, y + 3);
            _txtExcludePatterns.Location = new System.Drawing.Point(tx, y);
            _txtExcludePatterns.Size = new System.Drawing.Size(tw, 23);

            // Hint
            y += 30;
            _lblHint.Text = "Kalıplar: *.pst; *.ost; *.docx (noktalı virgül ile ayırın)";
            _lblHint.AutoSize = true;
            _lblHint.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblHint.Location = new System.Drawing.Point(tx, y);

            // Checkboxes
            y += 30;
            _chkRecursive.Text = "Alt dizinleri dahil et";
            _chkRecursive.Location = new System.Drawing.Point(tx, y);
            _chkRecursive.AutoSize = true;
            _chkRecursive.Checked = true;

            y += 28;
            _chkUseVss.Text = "VSS (Volume Shadow Copy) kullan — açık/kilitli dosyalar için";
            _chkUseVss.Location = new System.Drawing.Point(tx, y);
            _chkUseVss.AutoSize = true;
            _chkUseVss.Checked = true;

            y += 28;
            _chkEnabled.Text = "Aktif";
            _chkEnabled.Location = new System.Drawing.Point(tx, y);
            _chkEnabled.AutoSize = true;
            _chkEnabled.Checked = true;

            // Butonlar
            y += 45;
            _btnSave.Text = "\U0001f4be Kaydet";
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Size = new System.Drawing.Size(100, 34);
            _btnSave.Location = new System.Drawing.Point(235, y);
            _btnSave.Click += OnSaveClick;

            _btnCancel.Text = "İptal";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.Size = new System.Drawing.Size(90, 34);
            _btnCancel.Location = new System.Drawing.Point(345, y);
            _btnCancel.Click += OnCancelClick;

            // Form
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(450, y + 50);
            Controls.Add(_lblSourceName);
            Controls.Add(_txtSourceName);
            Controls.Add(_lblSourcePath);
            Controls.Add(_txtSourcePath);
            Controls.Add(_btnBrowsePath);
            Controls.Add(_lblInclude);
            Controls.Add(_txtIncludePatterns);
            Controls.Add(_lblExclude);
            Controls.Add(_txtExcludePatterns);
            Controls.Add(_lblHint);
            Controls.Add(_chkRecursive);
            Controls.Add(_chkUseVss);
            Controls.Add(_chkEnabled);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);
            Font = Theme.ModernTheme.FontBody;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            BackColor = Theme.ModernTheme.BackgroundColor;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FileBackupSourceEditDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Dosya Kaynağı";

            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label _lblSourceName;
        private System.Windows.Forms.TextBox _txtSourceName;
        private System.Windows.Forms.Label _lblSourcePath;
        private System.Windows.Forms.TextBox _txtSourcePath;
        private Theme.ModernButton _btnBrowsePath;
        private System.Windows.Forms.CheckBox _chkRecursive;
        private System.Windows.Forms.CheckBox _chkUseVss;
        private System.Windows.Forms.CheckBox _chkEnabled;
        private System.Windows.Forms.Label _lblInclude;
        private System.Windows.Forms.TextBox _txtIncludePatterns;
        private System.Windows.Forms.Label _lblExclude;
        private System.Windows.Forms.TextBox _txtExcludePatterns;
        private System.Windows.Forms.Label _lblHint;
        private Theme.ModernButton _btnSave;
        private Theme.ModernButton _btnCancel;
    }
}
