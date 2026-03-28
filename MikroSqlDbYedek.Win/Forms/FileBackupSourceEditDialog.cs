using System;
using System.Windows.Forms;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Win.Helpers;

namespace MikroSqlDbYedek.Win.Forms
{
    /// <summary>
    /// Dosya yedekleme kaynağı ekleme/düzenleme dialogu.
    /// Kaynak adı, dizin yolu, include/exclude pattern, recursive ve VSS ayarları.
    /// </summary>
    public partial class FileBackupSourceEditDialog : Form
    {
        private readonly FileBackupSource _source;

        /// <summary>Düzenlenen/oluşturulan dosya yedekleme kaynağı.</summary>
        public FileBackupSource Source => _source;

        /// <summary>Yeni dosya yedekleme kaynağı oluşturma.</summary>
        public FileBackupSourceEditDialog() : this(null) { }

        /// <summary>Mevcut kaynağı düzenleme. null ise yeni kaynak.</summary>
        public FileBackupSourceEditDialog(FileBackupSource existing)
        {
            InitializeComponent();

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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadSourceToUi();
        }

        #region Load / Save

        private void LoadSourceToUi()
        {
            _txtSourceName.Text = _source.SourceName ?? "";
            _txtSourcePath.Text = _source.SourcePath ?? "";
            _chkRecursive.Checked = _source.Recursive;
            _chkUseVss.Checked = _source.UseVss;
            _chkEnabled.Checked = _source.IsEnabled;

            _txtIncludePatterns.Text = _source.IncludePatterns != null
                ? string.Join("; ", _source.IncludePatterns)
                : "";

            _txtExcludePatterns.Text = _source.ExcludePatterns != null
                ? string.Join("; ", _source.ExcludePatterns)
                : "";
        }

        private bool SaveUiToSource()
        {
            if (string.IsNullOrWhiteSpace(_txtSourceName.Text))
            {
                MessageBox.Show(Res.Get("FileSource_NameRequired"), Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSourceName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_txtSourcePath.Text))
            {
                MessageBox.Show(Res.Get("FileSource_PathRequired"), Res.Get("ValidationError"),
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
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Res.Get("FileSource_BrowsePath");
                if (!string.IsNullOrEmpty(_txtSourcePath.Text))
                    fbd.SelectedPath = _txtSourcePath.Text;

                if (fbd.ShowDialog(this) == DialogResult.OK)
                    _txtSourcePath.Text = fbd.SelectedPath;
            }
        }

        #endregion
    }
}
