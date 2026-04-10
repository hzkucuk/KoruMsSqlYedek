#nullable enable
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
    /// TreeView seçimleri kaynak gerçeğidir — SourcePath otomatik türetilir.
    /// Include/Exclude kalıpları TreeView görselini dinamik filtreler.
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
        public FileBackupSourceEditDialog(FileBackupSource? existing)
        {
            InitializeComponent();
            ApplyIcons();
            ApplyLocalization();
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

        private void ApplyLocalization()
        {
            _lblSourceName.Text = Res.Get("FileSource_SourceName");
            _btnNavigate.Text = Res.Get("FileSource_Navigate");
            _lblStatus.Text = Res.Get("FileSource_StatusHint");
            _grpPatterns.Text = Res.Get("FileSource_PatternsGroup");
            _lblInclude.Text = Res.Get("FileSource_Include");
            _lblExclude.Text = Res.Get("FileSource_Exclude");
            _lblHint.Text = Res.Get("FileSource_Hint");
            _grpOptions.Text = Res.Get("FileSource_OptionsGroup");
            _chkRecursive.Text = Res.Get("FileSource_Recursive");
            _chkUseVss.Text = Res.Get("FileSource_UseVss");
            _chkEnabled.Text = Res.Get("FileSource_Enabled");
            _btnSave.Text = Res.Get("FileSource_Save");
            _btnCancel.Text = Res.Get("FileSource_Cancel");

            // ── Rich Tooltips ────────────────────────────────────────────
            _toolTip.SetToolTip(_txtSourceName, Res.Get("Tip_FileSource_SourceName"));
            _toolTip.SetToolTip(_treeView, Res.Get("Tip_FileSource_TreeView"));
            _toolTip.SetToolTip(_txtIncludePatterns, Res.Get("Tip_FileSource_Include"));
            _toolTip.SetToolTip(_txtExcludePatterns, Res.Get("Tip_FileSource_Exclude"));
            _toolTip.SetToolTip(_chkRecursive, Res.Get("Tip_FileSource_Recursive"));
            _toolTip.SetToolTip(_chkUseVss, Res.Get("Tip_FileSource_UseVss"));
            _toolTip.SetToolTip(_chkEnabled, Res.Get("Tip_FileSource_Enabled"));
            _toolTip.SetToolTip(_btnNavigate, Res.Get("Tip_FileSource_Navigate"));
        }

        private void ApplyIcons()
        {
            _btnSave.Image = LoadIcon("Save_16x16.png");
            _btnSave.Text = Res.Get("FileSource_Save");
            _btnSave.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnCancel.Image = LoadIcon("Cancel_16x16.png");
            _btnCancel.Text = Res.Get("FileSource_Cancel");
            _btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;

            _btnNavigate.Image = LoadIcon("Open_16x16.png");
        }

        private static System.Drawing.Image? LoadIcon(string name)
        {
            var asm = typeof(FileBackupSourceEditDialog).Assembly;
            string resourceName = $"KoruMsSqlYedek.Win.Resources.Icons.{name}";
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null) return null;
            return System.Drawing.Image.FromStream(stream);
        }

        private void WireEvents()
        {
            _treeView.CheckStateChanged += OnTreeCheckStateChanged;
            _treeView.SizeCalculated += OnSizeCalculated;
            _txtIncludePatterns.TextChanged += OnFilterPatternsChanged;
            _txtExcludePatterns.TextChanged += OnFilterPatternsChanged;
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

            // Kalıpları TreeView'a uygula
            ApplyPatternsToTree();

            // Seçili yolları geri yükle (yeni format) veya SourcePath'e git (eski format)
            if (_source.SelectedPaths?.Count > 0)
            {
                _treeView.SetCheckedPaths(_source.SelectedPaths);
            }
            else if (!string.IsNullOrWhiteSpace(_source.SourcePath))
            {
                // Eski format uyumu: SourcePath'e navigasyonla klasörü aç, kök klasörü seçili yap
                _treeView.NavigateAndExpand(_source.SourcePath);
            }

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

            // TreeView seçimlerini al
            List<string> checkedPaths = _treeView.GetCheckedPaths();
            if (checkedPaths.Count == 0)
            {
                Theme.ModernMessageBox.Show(
                    "Yedeklenecek en az bir klasör veya dosya seçmelisiniz.",
                    Res.Get("ValidationError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _source.SourceName = _txtSourceName.Text.Trim();
            _source.SelectedPaths = checkedPaths;
            _source.SourcePath = DeriveCommonRoot(checkedPaths);
            _source.Recursive = _chkRecursive.Checked;
            _source.UseVss = _chkUseVss.Checked;
            _source.IsEnabled = _chkEnabled.Checked;

            _source.IncludePatterns.Clear();
            if (!string.IsNullOrWhiteSpace(_txtIncludePatterns.Text))
            {
                foreach (string pattern in _txtIncludePatterns.Text.Split([';', ','], StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = pattern.Trim();
                    if (trimmed.Length > 0)
                        _source.IncludePatterns.Add(trimmed);
                }
            }

            _source.ExcludePatterns.Clear();
            if (!string.IsNullOrWhiteSpace(_txtExcludePatterns.Text))
            {
                foreach (string pattern in _txtExcludePatterns.Text.Split([';', ','], StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = pattern.Trim();
                    if (trimmed.Length > 0)
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

        private void OnNavigateToFolder(object sender, EventArgs e)
        {
            using FolderBrowserDialog fbd = new();
            fbd.Description = "TreeView'da görüntülenecek klasörü seçin";

            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
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

        private void OnSizeCalculated(object? sender, Theme.SizeCalculationResult result)
        {
            UpdateStatusLabel(result.TotalBytes, result.Estimated7zBytes);
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

        private void UpdateStatusLabel(long totalBytes = -1, long estimated7zBytes = -1)
        {
            (int folders, int files) = _treeView.GetCheckedCounts();
            if (folders == 0 && files == 0)
            {
                _lblStatus.Text = Res.Get("FileSource_StatusHint");
                _lblStatus.ForeColor = Theme.ModernTheme.TextSecondary;
            }
            else
            {
                string sizeText;
                if (totalBytes >= 0 && estimated7zBytes >= 0)
                    sizeText = $" — {FormatFileSize(totalBytes)} (~{FormatFileSize(estimated7zBytes)} 7z)";
                else if (totalBytes >= 0)
                    sizeText = $" — {FormatFileSize(totalBytes)}";
                else
                    sizeText = " — " + Res.Get("FileSource_Calculating");

                _lblStatus.Text = Res.Format("FileSource_StatusSelected", folders, files, sizeText);
                _lblStatus.ForeColor = Theme.ModernTheme.AccentPrimary;
            }
        }

        /// <summary>
        /// Seçili yolların ortak kök dizinini belirler.
        /// VSS volume tespiti ve eski format uyumu için kullanılır.
        /// </summary>
        private static string DeriveCommonRoot(List<string> paths)
        {
            if (paths.Count == 0) return "";
            if (paths.Count == 1)
            {
                string single = paths[0];
                return Directory.Exists(single) ? single : Path.GetDirectoryName(single) ?? single;
            }

            // Tüm yolları normalize et ve parçala
            string[][] allParts = paths
                .Select(p => Path.GetFullPath(p)
                    .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();

            // Ortak parçaları bul
            int commonLength = allParts[0].Length;
            for (int i = 1; i < allParts.Length; i++)
            {
                commonLength = Math.Min(commonLength, allParts[i].Length);
                for (int j = 0; j < commonLength; j++)
                {
                    if (!string.Equals(allParts[0][j], allParts[i][j], StringComparison.OrdinalIgnoreCase))
                    {
                        commonLength = j;
                        break;
                    }
                }
            }

            if (commonLength == 0) return paths[0][..3]; // Sürücü kökü (ör. "C:\")

            string root = string.Join(Path.DirectorySeparatorChar.ToString(),
                allParts[0].Take(commonLength));

            // Sürücü harfi için ters slash ekle (C: → C:\)
            if (root.Length == 2 && root[1] == ':')
                root += Path.DirectorySeparatorChar;

            return root;
        }

        private static string FormatFileSize(long bytes)
        {
            return bytes switch
            {
                < 1024L => $"{bytes} B",
                < 1024L * 1024 => $"{bytes / 1024.0:F1} KB",
                < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
                _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
            };
        }

        #endregion
    }
}
