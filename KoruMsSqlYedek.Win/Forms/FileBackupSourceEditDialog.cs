using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// Dosya yedekleme kaynağı ekleme/düzenleme dialogu.
    /// Kaynak adı, dizin yolu, include/exclude pattern, recursive ve VSS ayarları.
    /// </summary>
    public partial class FileBackupSourceEditDialog : Theme.ModernFormBase
    {
        private readonly FileBackupSource _source;
        private bool _suppressFilterUpdate;

        /// <summary>Düzenlenen/oluşturulan dosya yedekleme kaynağı.</summary>
        public FileBackupSource Source => _source;

        /// <summary>Yeni dosya yedekleme kaynağı oluşturma.</summary>
        public FileBackupSourceEditDialog() : this(null) { }

        /// <summary>Mevcut kaynağı düzenleme. null ise yeni kaynak.</summary>
        public FileBackupSourceEditDialog(FileBackupSource existing)
        {
            InitializeComponent();
            ApplyIcons();
            WireEvents();

            if (existing != null)
            {
                _source = existing;
                Text = Res.Get("FileSource_TitleEdit");
            }
            else
            {
                _source = new FileBackupSource();
                Text = Res.Get("FileSource_TitleNew");
            }
        }

        private void ApplyIcons()
        {
            const int sz = 16;
            _btnSave.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.FloppyDisk, System.Drawing.Color.White, sz);
            _btnSave.Text = "Kaydet";
            _btnSave.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnCancel.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.XCircle, System.Drawing.Color.White, sz);
            _btnCancel.Text = "Iptal";
            _btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;

            _btnBrowsePath.Image = Theme.PhosphorIcons.Render(Theme.PhosphorIcons.Folder, Theme.ModernTheme.AccentPrimary, 14);
            _btnBrowsePath.Text = "";
        }

        private void WireEvents()
        {
            _treeView.CheckStateChanged += OnTreeCheckStateChanged;
            _txtIncludePatterns.TextChanged += OnFilterPatternsChanged;
            _txtExcludePatterns.TextChanged += OnFilterPatternsChanged;
            _txtSourcePath.KeyDown += OnSourcePathKeyDown;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadSourceToUi();
        }

        #region Load / Save

        private void LoadSourceToUi()
        {
            _suppressFilterUpdate = true;

            _txtSourceName.Text = _source.SourceName ?? "";
            _txtSourcePath.Text = _source.SourcePath ?? "";
            _chkRecursive.Checked = _source.Recursive;
            _chkUseVss.Checked = _source.UseVss;
            _chkEnabled.Checked = _source.IsEnabled;

            _txtIncludePatterns.Text = _source.IncludePatterns is not null
                ? string.Join("; ", _source.IncludePatterns)
                : "";

            _txtExcludePatterns.Text = _source.ExcludePatterns is not null
                ? string.Join("; ", _source.ExcludePatterns)
                : "";

            _suppressFilterUpdate = false;

            // Kalıpları TreeView'a uygula ve kaynak dizine git
            ApplyPatternsToTree();

            if (!string.IsNullOrWhiteSpace(_source.SourcePath))
                _treeView.NavigateAndExpand(_source.SourcePath);

            UpdateStatusLabel();
        }

        private bool SaveUiToSource()
        {
            if (string.IsNullOrWhiteSpace(_txtSourceName.Text))
            {
                Theme.ModernMessageBox.Show(Res.Get("FileSource_NameRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSourceName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_txtSourcePath.Text))
            {
                Theme.ModernMessageBox.Show(Res.Get("FileSource_PathRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSourcePath.Focus();
                return false;
            }

            _source.SourceName = _txtSourceName.Text.Trim();
            _source.SourcePath = _txtSourcePath.Text.Trim();
            _source.Recursive = _chkRecursive.Checked;
            _source.UseVss = _chkUseVss.Checked;
            _source.IsEnabled = _chkEnabled.Checked;

            _source.IncludePatterns.Clear();
            if (!string.IsNullOrWhiteSpace(_txtIncludePatterns.Text))
            {
                foreach (var pattern in _txtIncludePatterns.Text.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = pattern.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        _source.IncludePatterns.Add(trimmed);
                }
            }

            _source.ExcludePatterns.Clear();
            if (!string.IsNullOrWhiteSpace(_txtExcludePatterns.Text))
            {
                foreach (var pattern in _txtExcludePatterns.Text.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = pattern.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        _source.ExcludePatterns.Add(trimmed);
                }
            }

            return true;
        }

        #endregion

        #region Events

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (!SaveUiToSource()) return;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OnBrowseSourcePath(object sender, EventArgs e)
        {
            using FolderBrowserDialog fbd = new();
            fbd.Description = Res.Get("FileSource_BrowsePath");
            if (!string.IsNullOrEmpty(_txtSourcePath.Text))
                fbd.SelectedPath = _txtSourcePath.Text;

            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                _txtSourcePath.Text = fbd.SelectedPath;
                _treeView.NavigateAndExpand(fbd.SelectedPath);
            }
        }

        private void OnTreeCheckStateChanged(object? sender, EventArgs e)
        {
            UpdateStatusLabel();
        }

        private void OnFilterPatternsChanged(object? sender, EventArgs e)
        {
            if (_suppressFilterUpdate) return;
            ApplyPatternsToTree();
        }

        private void OnSourcePathKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true;
            string path = _txtSourcePath.Text.Trim();
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                _treeView.NavigateAndExpand(path);
        }

        #endregion

        #region Helpers

        private void ApplyPatternsToTree()
        {
            List<string> includes = ParsePatterns(_txtIncludePatterns.Text);
            List<string> excludes = ParsePatterns(_txtExcludePatterns.Text);

            _treeView.SetIncludePatterns(includes);
            _treeView.SetExcludePatterns(excludes);
        }

        private static List<string> ParsePatterns(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            return text.Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
        }

        private void UpdateStatusLabel()
        {
            (int folders, int files) = _treeView.GetCheckedCounts();
            if (folders == 0 && files == 0)
            {
                _lblStatus.Text = "Dosya se\u00e7mek i\u00e7in klas\u00f6rlere g\u00f6z at\u0131n ve onay kutular\u0131n\u0131 i\u015faretleyin";
                _lblStatus.ForeColor = Theme.ModernTheme.TextSecondary;
            }
            else
            {
                _lblStatus.Text = $"\u2705 {folders} klas\u00f6r, {files} dosya se\u00e7ili";
                _lblStatus.ForeColor = Theme.ModernTheme.AccentPrimary;
            }
        }

        #endregion
    }
}
