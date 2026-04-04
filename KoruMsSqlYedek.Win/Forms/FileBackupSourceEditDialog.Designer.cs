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
            _lblSourcePath = new System.Windows.Forms.Label();
            _txtSourcePath = new System.Windows.Forms.TextBox();
            _btnBrowsePath = new Theme.ModernButton();
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

            int lx = 15, tx = 130, tw = 350;
            int formW = 510;
            int y = 18;

            // Kaynak Adı
            _lblSourceName.Text = "Kaynak Adı:";
            _lblSourceName.AutoSize = true;
            _lblSourceName.Location = new System.Drawing.Point(lx, y + 3);
            _txtSourceName.Location = new System.Drawing.Point(tx, y);
            _txtSourceName.Size = new System.Drawing.Size(tw, 23);
            _toolTip.SetToolTip(_txtSourceName, "Bu kaynağı tanımlayan kısa bir isim.\nÖrnek: Outlook PST, Proje Dosyaları");

            // Kaynak Dizin
            y += 35;
            _lblSourcePath.Text = "Dizin Yolu:";
            _lblSourcePath.AutoSize = true;
            _lblSourcePath.Location = new System.Drawing.Point(lx, y + 3);
            _txtSourcePath.Location = new System.Drawing.Point(tx, y);
            _txtSourcePath.Size = new System.Drawing.Size(tw - 38, 23);
            _btnBrowsePath.Text = "...";
            _btnBrowsePath.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnBrowsePath.Size = new System.Drawing.Size(33, 26);
            _btnBrowsePath.Location = new System.Drawing.Point(tx + tw - 33, y);
            _btnBrowsePath.Click += OnBrowseSourcePath;
            _toolTip.SetToolTip(_txtSourcePath, "Yedeklenecek klasörün tam yolu.\nÖrnek: C:\\Users\\Belgeler");
            _toolTip.SetToolTip(_btnBrowsePath, "Klasör seçmek için tıklayın");

            // Dosya Filtre Kalıpları GroupBox
            y += 42;
            int gpx = 8; // group içi padding-x
            int gtx = 115; // group içi textbox x
            int gtw = tw - 10; // group içi textbox width
            int gy = 22;

            _grpPatterns.Text = "Dosya Filtre Kalıpları";
            _grpPatterns.ForeColor = Theme.ModernTheme.AccentPrimary;
            _grpPatterns.Location = new System.Drawing.Point(lx, y);

            _lblInclude.Text = "Dahil Kalıpları:";
            _lblInclude.AutoSize = true;
            _lblInclude.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblInclude.Location = new System.Drawing.Point(gpx, gy + 3);
            _txtIncludePatterns.Location = new System.Drawing.Point(gtx, gy);
            _txtIncludePatterns.Size = new System.Drawing.Size(gtw, 23);
            _toolTip.SetToolTip(_txtIncludePatterns, "Sadece bu kalıplara uyan dosyalar yedeklenir.\nBoş bırakılırsa tüm dosyalar dahil edilir.\nÖrnek: *.pst; *.docx; *.xlsx");

            gy += 32;
            _lblExclude.Text = "Hariç Kalıpları:";
            _lblExclude.AutoSize = true;
            _lblExclude.ForeColor = Theme.ModernTheme.TextPrimary;
            _lblExclude.Location = new System.Drawing.Point(gpx, gy + 3);
            _txtExcludePatterns.Location = new System.Drawing.Point(gtx, gy);
            _txtExcludePatterns.Size = new System.Drawing.Size(gtw, 23);
            _toolTip.SetToolTip(_txtExcludePatterns, "Bu kalıplara uyan dosyalar yedeklenmez.\nÖrnek: *.tmp; *.log; ~$*");

            gy += 28;
            _lblHint.Text = "ℹ Kalıpları noktalı virgül (;) ile ayırın — Örnek: *.pst; *.docx";
            _lblHint.AutoSize = true;
            _lblHint.ForeColor = Theme.ModernTheme.TextSecondary;
            _lblHint.Location = new System.Drawing.Point(gpx, gy);

            gy += 22;
            _grpPatterns.Size = new System.Drawing.Size(formW - lx * 2, gy + 8);
            _grpPatterns.Controls.Add(_lblInclude);
            _grpPatterns.Controls.Add(_txtIncludePatterns);
            _grpPatterns.Controls.Add(_lblExclude);
            _grpPatterns.Controls.Add(_txtExcludePatterns);
            _grpPatterns.Controls.Add(_lblHint);

            // Seçenekler GroupBox
            y += _grpPatterns.Height + 10;
            int oy = 22;
            _grpOptions.Text = "Seçenekler";
            _grpOptions.ForeColor = Theme.ModernTheme.AccentPrimary;
            _grpOptions.Location = new System.Drawing.Point(lx, y);

            _chkRecursive.Text = "Alt dizinleri dahil et";
            _chkRecursive.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkRecursive.Location = new System.Drawing.Point(gpx + 5, oy);
            _chkRecursive.AutoSize = true;
            _chkRecursive.Checked = true;
            _toolTip.SetToolTip(_chkRecursive, "İşaretlenirse alt klasörlerdeki dosyalar da yedeklenir.");

            oy += 26;
            _chkUseVss.Text = "VSS (Volume Shadow Copy) kullan — açık/kilitli dosyalar için";
            _chkUseVss.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkUseVss.Location = new System.Drawing.Point(gpx + 5, oy);
            _chkUseVss.AutoSize = true;
            _chkUseVss.Checked = true;
            _toolTip.SetToolTip(_chkUseVss, "Outlook PST gibi kilitli dosyaları yedeklemek için\nVolume Shadow Copy kullanır. Önerilir.");

            oy += 26;
            _chkEnabled.Text = "Bu kaynak aktif";
            _chkEnabled.ForeColor = Theme.ModernTheme.TextPrimary;
            _chkEnabled.Location = new System.Drawing.Point(gpx + 5, oy);
            _chkEnabled.AutoSize = true;
            _chkEnabled.Checked = true;
            _toolTip.SetToolTip(_chkEnabled, "Devre dışı bırakılırsa bu kaynak yedekleme sırasında atlanır.");

            oy += 28;
            _grpOptions.Size = new System.Drawing.Size(formW - lx * 2, oy + 8);
            _grpOptions.Controls.Add(_chkRecursive);
            _grpOptions.Controls.Add(_chkUseVss);
            _grpOptions.Controls.Add(_chkEnabled);

            // Butonlar
            y += _grpOptions.Height + 15;
            _btnSave.Text = "\U0001f4be Kaydet";
            _btnSave.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnSave.Size = new System.Drawing.Size(110, 36);
            _btnSave.Location = new System.Drawing.Point(formW - 220, y);
            _btnSave.Click += OnSaveClick;

            _btnCancel.Text = "İptal";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.Size = new System.Drawing.Size(95, 36);
            _btnCancel.Location = new System.Drawing.Point(formW - 105, y);
            _btnCancel.Click += OnCancelClick;

            // Form
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(formW, y + 52);
            Controls.Add(_lblSourceName);
            Controls.Add(_txtSourceName);
            Controls.Add(_lblSourcePath);
            Controls.Add(_txtSourcePath);
            Controls.Add(_btnBrowsePath);
            Controls.Add(_grpPatterns);
            Controls.Add(_grpOptions);
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
        private System.Windows.Forms.Label _lblSourcePath;
        private System.Windows.Forms.TextBox _txtSourcePath;
        private Theme.ModernButton _btnBrowsePath;
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
