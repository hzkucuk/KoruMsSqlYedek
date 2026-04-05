using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Dosya sistemi TreeView kontrolü — checkbox desteği, tri-state propagation,
    /// lazy-load ve include/exclude filtre görselleştirmesi.
    /// Dosya yedekleme kaynağı seçimi için tasarlandı.
    /// </summary>
    internal sealed class FileSystemCheckedTreeView : UserControl
    {
        // ── Constants ──
        private const string DummyNodeKey = "__dummy__";
        private const int IconDrive = 0;
        private const int IconFolderClosed = 1;
        private const int IconFolderOpen = 2;
        private const int IconFile = 3;
        private const int IconFileExcluded = 4;

        // ── Controls ──
        private readonly TreeView _tree;
        private readonly ImageList _imageList;

        // ── State ──
        private bool _suppressCheckEvent;
        private List<string> _includePatterns = new();
        private List<string> _excludePatterns = new();

        /// <summary>Checkbox durumu değiştiğinde tetiklenir.</summary>
        internal event EventHandler CheckStateChanged;

        internal FileSystemCheckedTreeView()
        {
            _imageList = CreateImageList();

            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                ImageList = _imageList,
                Font = ModernTheme.FontBody,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Indent = 20,
                ItemHeight = 22,
                FullRowSelect = true
            };

            _tree.BeforeExpand += OnBeforeExpand;
            _tree.AfterCheck += OnAfterCheck;
            _tree.AfterExpand += OnAfterExpand;

            Controls.Add(_tree);
            BackColor = ModernTheme.SurfaceColor;
            BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_tree.Nodes.Count == 0)
                LoadDrives();
        }

        // ═══════════════ PUBLIC API ═══════════════

        /// <summary>Dahil kalıplarını ayarlar ve ağacı günceller.</summary>
        internal void SetIncludePatterns(List<string> patterns)
        {
            _includePatterns = patterns ?? new List<string>();
            ApplyFilterVisualsToAllNodes();
        }

        /// <summary>Hariç kalıplarını ayarlar ve ağacı günceller.</summary>
        internal void SetExcludePatterns(List<string> patterns)
        {
            _excludePatterns = patterns ?? new List<string>();
            ApplyFilterVisualsToAllNodes();
        }

        /// <summary>
        /// Tüm işaretlenmiş (checked) node'ların tam dosya/klasör yollarını döndürür.
        /// Sadece "yaprak" seçimleri döner — bir klasör tamamen seçiliyse
        /// alt öğeleri yerine sadece klasör yolunu döndürür.
        /// </summary>
        internal List<string> GetCheckedPaths()
        {
            List<string> paths = new();
            CollectCheckedPaths(_tree.Nodes, paths);
            return paths;
        }

        /// <summary>
        /// Verilen yolları ağaçta işaretler.
        /// Lazy-load nedeniyle henüz yüklenmemiş düğümler genişletilir.
        /// </summary>
        internal void SetCheckedPaths(IEnumerable<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            _suppressCheckEvent = true;
            try
            {
                UncheckAll(_tree.Nodes);
                foreach (string path in paths)
                {
                    TreeNode node = NavigateToPath(path);
                    if (node is not null)
                    {
                        node.Checked = true;
                        PropagateCheckDown(node, true);
                        UpdateParentCheckState(node);
                    }
                }
            }
            finally
            {
                _suppressCheckEvent = false;
            }
        }

        /// <summary>
        /// İşaretli klasör ve dosya sayısını döndürür.
        /// </summary>
        internal (int Folders, int Files) GetCheckedCounts()
        {
            int folders = 0, files = 0;
            CountChecked(_tree.Nodes, ref folders, ref files);
            return (folders, files);
        }

        /// <summary>TreeView'ı tamamen yeniden yükler.</summary>
        internal void RefreshTree()
        {
            _tree.BeginUpdate();
            _tree.Nodes.Clear();
            LoadDrives();
            _tree.EndUpdate();
        }

        /// <summary>Belirtilen klasör yolunu ağaçta açar ve seçer.</summary>
        internal void NavigateAndExpand(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) return;

            TreeNode node = NavigateToPath(folderPath);
            if (node is not null)
            {
                _tree.SelectedNode = node;
                node.EnsureVisible();
            }
        }

        // ═══════════════ DRIVE & NODE LOADING ═══════════════

        private void LoadDrives()
        {
            _tree.BeginUpdate();
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;

                    string label = string.IsNullOrEmpty(drive.VolumeLabel)
                        ? drive.Name
                        : $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})";

                    TreeNode driveNode = new(label)
                    {
                        Tag = drive.RootDirectory.FullName,
                        ImageIndex = IconDrive,
                        SelectedImageIndex = IconDrive
                    };
                    driveNode.Nodes.Add(DummyNodeKey, "");
                    _tree.Nodes.Add(driveNode);
                }
            }
            finally
            {
                _tree.EndUpdate();
            }
        }

        private void LoadChildren(TreeNode parentNode)
        {
            string path = parentNode.Tag as string;
            if (string.IsNullOrEmpty(path)) return;

            parentNode.Nodes.Clear();

            try
            {
                DirectoryInfo dir = new(path);

                // Klasörler
                foreach (DirectoryInfo subDir in dir.GetDirectories().OrderBy(d => d.Name))
                {
                    if (IsSystemOrHiddenDir(subDir)) continue;

                    TreeNode folderNode = new(subDir.Name)
                    {
                        Tag = subDir.FullName,
                        ImageIndex = IconFolderClosed,
                        SelectedImageIndex = IconFolderClosed
                    };

                    // Alt klasör veya dosya varsa dummy ekle
                    try
                    {
                        if (subDir.GetDirectories().Length > 0 || subDir.GetFiles().Length > 0)
                            folderNode.Nodes.Add(DummyNodeKey, "");
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }

                    ApplyFilterVisualToNode(folderNode);
                    parentNode.Nodes.Add(folderNode);
                }

                // Dosyalar
                foreach (FileInfo file in dir.GetFiles().OrderBy(f => f.Name))
                {
                    bool excluded = IsExcludedByPattern(file.Name);
                    bool included = IsIncludedByPattern(file.Name);

                    TreeNode fileNode = new(file.Name)
                    {
                        Tag = file.FullName,
                        ImageIndex = excluded ? IconFileExcluded : IconFile,
                        SelectedImageIndex = excluded ? IconFileExcluded : IconFile
                    };

                    ApplyFilterVisualToNode(fileNode);
                    parentNode.Nodes.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            // Ebeveynin check state'ini çocuklara propagate et
            if (parentNode.Checked)
            {
                _suppressCheckEvent = true;
                PropagateCheckDown(parentNode, true);
                _suppressCheckEvent = false;
            }
        }

        private static bool IsSystemOrHiddenDir(DirectoryInfo dir)
        {
            // $Recycle.Bin, System Volume Information gibi sistem klasörlerini gizle
            if (dir.Name.StartsWith('$') || dir.Name.StartsWith('.'))
                return true;

            FileAttributes attrs = dir.Attributes;
            return attrs.HasFlag(FileAttributes.System) && attrs.HasFlag(FileAttributes.Hidden);
        }

        // ═══════════════ EVENT HANDLERS ═══════════════

        private void OnBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            if (node.Nodes.Count == 1 && node.Nodes[0].Name == DummyNodeKey)
            {
                LoadChildren(node);
            }
        }

        private void OnAfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is string)
            {
                e.Node.ImageIndex = IconFolderOpen;
                e.Node.SelectedImageIndex = IconFolderOpen;
            }
        }

        private void OnAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_suppressCheckEvent) return;
            if (e.Action == TreeViewAction.Unknown) return;

            _suppressCheckEvent = true;
            try
            {
                // Alt düğümlere propagate
                PropagateCheckDown(e.Node, e.Node.Checked);
                // Üst düğümleri güncelle
                UpdateParentCheckState(e.Node);
            }
            finally
            {
                _suppressCheckEvent = false;
            }

            CheckStateChanged?.Invoke(this, EventArgs.Empty);
        }

        // ═══════════════ TRI-STATE CHECK PROPAGATION ═══════════════

        private void PropagateCheckDown(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Name == DummyNodeKey) continue;
                child.Checked = isChecked;
                PropagateCheckDown(child, isChecked);
            }
        }

        private static void UpdateParentCheckState(TreeNode node)
        {
            TreeNode parent = node.Parent;
            if (parent is null) return;

            bool allChecked = true;
            bool noneChecked = true;

            foreach (TreeNode sibling in parent.Nodes)
            {
                if (sibling.Name == DummyNodeKey) continue;
                if (sibling.Checked)
                    noneChecked = false;
                else
                    allChecked = false;
            }

            // WinForms TreeView doesn't natively support indeterminate,
            // but we can set the parent to checked if all children are checked
            parent.Checked = allChecked && !noneChecked;

            // Recurse up
            UpdateParentCheckState(parent);
        }

        // ═══════════════ FILTER VISUALS ═══════════════

        private void ApplyFilterVisualsToAllNodes()
        {
            _tree.BeginUpdate();
            ApplyFilterVisualsRecursive(_tree.Nodes);
            _tree.EndUpdate();
        }

        private void ApplyFilterVisualsRecursive(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                ApplyFilterVisualToNode(node);
                if (node.Nodes.Count > 0)
                    ApplyFilterVisualsRecursive(node.Nodes);
            }
        }

        private void ApplyFilterVisualToNode(TreeNode node)
        {
            string path = node.Tag as string;
            if (string.IsNullOrEmpty(path)) return;

            bool isFile = File.Exists(path);
            if (!isFile) return; // Klasörlere filtre uygulanmaz

            string fileName = Path.GetFileName(path);
            bool excluded = IsExcludedByPattern(fileName);
            bool included = IsIncludedByPattern(fileName);

            if (excluded)
            {
                node.ForeColor = ModernTheme.TextDisabled;
                node.ImageIndex = IconFileExcluded;
                node.SelectedImageIndex = IconFileExcluded;
            }
            else if (_includePatterns.Count > 0 && !included)
            {
                node.ForeColor = ModernTheme.TextDisabled;
                node.ImageIndex = IconFileExcluded;
                node.SelectedImageIndex = IconFileExcluded;
            }
            else
            {
                node.ForeColor = ModernTheme.TextPrimary;
                node.ImageIndex = IconFile;
                node.SelectedImageIndex = IconFile;
            }
        }

        private bool IsExcludedByPattern(string fileName)
        {
            return _excludePatterns.Any(p => MatchesWildcard(fileName, p));
        }

        private bool IsIncludedByPattern(string fileName)
        {
            if (_includePatterns.Count == 0) return true;
            return _includePatterns.Any(p => MatchesWildcard(fileName, p));
        }

        /// <summary>Basit wildcard eşleştirme (*, ?).</summary>
        private static bool MatchesWildcard(string fileName, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return false;

            string trimmed = pattern.Trim();
            // Basit dosya kalıpları: *.ext, dosya.*, *.*, prefix*, *suffix
            try
            {
                // FileSystemName.MatchesSimpleExpression ile güvenli eşleştirme
                return fileName.Length > 0
                    && (trimmed == "*" || trimmed == "*.*"
                        || SimpleWildcardMatch(fileName, trimmed));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Basit * ve ? wildcard eşleştirmesi — case-insensitive.
        /// </summary>
        private static bool SimpleWildcardMatch(string input, string pattern)
        {
            int inputIdx = 0, patternIdx = 0;
            int inputStar = -1, patternStar = -1;

            while (inputIdx < input.Length)
            {
                if (patternIdx < pattern.Length &&
                    (char.ToLowerInvariant(pattern[patternIdx]) == char.ToLowerInvariant(input[inputIdx])
                     || pattern[patternIdx] == '?'))
                {
                    inputIdx++;
                    patternIdx++;
                }
                else if (patternIdx < pattern.Length && pattern[patternIdx] == '*')
                {
                    patternStar = patternIdx;
                    inputStar = inputIdx;
                    patternIdx++;
                }
                else if (patternStar >= 0)
                {
                    patternIdx = patternStar + 1;
                    inputStar++;
                    inputIdx = inputStar;
                }
                else
                {
                    return false;
                }
            }

            while (patternIdx < pattern.Length && pattern[patternIdx] == '*')
                patternIdx++;

            return patternIdx == pattern.Length;
        }

        // ═══════════════ HELPER: COLLECT CHECKED PATHS ═══════════════

        private static void CollectCheckedPaths(TreeNodeCollection nodes, List<string> paths)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name == DummyNodeKey) continue;

                if (node.Checked)
                {
                    // Eğer tüm çocuklar da checked ise sadece bu klasörü ekle
                    bool allChildrenChecked = AllChildrenChecked(node);
                    if (allChildrenChecked || node.Nodes.Count == 0 ||
                        (node.Nodes.Count == 1 && node.Nodes[0].Name == DummyNodeKey))
                    {
                        string path = node.Tag as string;
                        if (!string.IsNullOrEmpty(path))
                            paths.Add(path);
                    }
                    else
                    {
                        // Kısmi seçim — çocuklara in
                        CollectCheckedPaths(node.Nodes, paths);
                    }
                }
                else if (HasAnyCheckedChild(node))
                {
                    CollectCheckedPaths(node.Nodes, paths);
                }
            }
        }

        private static bool AllChildrenChecked(TreeNode node)
        {
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Name == DummyNodeKey) continue;
                if (!child.Checked) return false;
                if (!AllChildrenChecked(child)) return false;
            }
            return true;
        }

        private static bool HasAnyCheckedChild(TreeNode node)
        {
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Name == DummyNodeKey) continue;
                if (child.Checked) return true;
                if (HasAnyCheckedChild(child)) return true;
            }
            return false;
        }

        private static void UncheckAll(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.Checked = false;
                if (node.Nodes.Count > 0)
                    UncheckAll(node.Nodes);
            }
        }

        private static void CountChecked(TreeNodeCollection nodes, ref int folders, ref int files)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name == DummyNodeKey) continue;
                if (node.Checked)
                {
                    string path = node.Tag as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (Directory.Exists(path))
                            folders++;
                        else
                            files++;
                    }
                }

                if (node.Nodes.Count > 0)
                    CountChecked(node.Nodes, ref folders, ref files);
            }
        }

        // ═══════════════ NAVIGATION ═══════════════

        private TreeNode NavigateToPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;

            string normalizedPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar);
            string[] parts = normalizedPath.Split(Path.DirectorySeparatorChar);

            // Sürücü düğümünü bul
            string drivePart = parts[0] + Path.DirectorySeparatorChar;
            TreeNode current = null;
            foreach (TreeNode driveNode in _tree.Nodes)
            {
                string driveTag = (driveNode.Tag as string ?? "").TrimEnd(Path.DirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                if (string.Equals(driveTag, drivePart, StringComparison.OrdinalIgnoreCase))
                {
                    current = driveNode;
                    break;
                }
            }

            if (current is null) return null;

            // Alt yolları takip et
            for (int i = 1; i < parts.Length; i++)
            {
                // Lazy-load tetikle
                if (current.Nodes.Count == 1 && current.Nodes[0].Name == DummyNodeKey)
                    LoadChildren(current);

                current.Expand();

                TreeNode found = null;
                foreach (TreeNode child in current.Nodes)
                {
                    if (string.Equals(child.Text, parts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        found = child;
                        break;
                    }
                }

                if (found is null) return current;
                current = found;
            }

            return current;
        }

        // ═══════════════ IMAGE LIST ═══════════════

        private static ImageList CreateImageList()
        {
            ImageList imgList = new()
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(16, 16)
            };

            // 0: Drive
            imgList.Images.Add(RenderIconFromSegoeSymbol('\uE7F8', ModernTheme.StatusInfo, 16));
            // 1: Folder closed
            imgList.Images.Add(RenderIconFromSegoeSymbol('\uE8B7', ModernTheme.StatusWarning, 16));
            // 2: Folder open
            imgList.Images.Add(RenderIconFromSegoeSymbol('\uE838', ModernTheme.StatusWarning, 16));
            // 3: File
            imgList.Images.Add(RenderIconFromSegoeSymbol('\uE7C3', ModernTheme.TextSecondary, 16));
            // 4: File excluded (dimmed)
            imgList.Images.Add(RenderIconFromSegoeSymbol('\uE7C3', ModernTheme.TextDisabled, 16));

            return imgList;
        }

        /// <summary>"Segoe UI Symbol" / "Segoe MDL2 Assets" fontundan ikon render eder.</summary>
        private static Bitmap RenderIconFromSegoeSymbol(char symbol, Color color, int size)
        {
            Bitmap bmp = new(size, size);
            bmp.SetResolution(96, 96);

            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // Segoe MDL2 Assets veya Segoe UI Symbol fontunu dene
            string[] fontNames = { "Segoe MDL2 Assets", "Segoe UI Symbol", "Segoe UI" };
            Font font = null;
            foreach (string fn in fontNames)
            {
                try
                {
                    font = new Font(fn, size * 0.7f, FontStyle.Regular, GraphicsUnit.Pixel);
                    break;
                }
                catch { }
            }

            if (font is null) return bmp;

            using (font)
            using (SolidBrush brush = new(color))
            {
                StringFormat sf = new()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                g.DrawString(symbol.ToString(), font, brush, new RectangleF(0, 0, size, size), sf);
            }

            return bmp;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _imageList?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
