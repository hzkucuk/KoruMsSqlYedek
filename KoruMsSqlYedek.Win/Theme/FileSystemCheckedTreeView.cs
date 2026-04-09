using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>Boyut hesaplama sonucu — gerçek boyut ve tahmini 7z sıkıştırılmış boyut.</summary>
    internal sealed record SizeCalculationResult(long TotalBytes, long Estimated7zBytes);

    /// <summary>
    /// Dosya sistemi TreeView kontrolü — checkbox desteği, tri-state propagation,
    /// lazy-load ve include/exclude filtre görselleştirmesi.
    /// Dosya yedekleme kaynağı seçimi için tasarlandı.
    /// </summary>
    internal sealed partial class FileSystemCheckedTreeView : UserControl
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
        private readonly ImageList _stateImageList;

        // ── State ──
        private bool _suppressCheckEvent;
        private List<string> _includePatterns = new();
        private List<string> _excludePatterns = new();

        /// <summary>Tri-state: indeterminate (kısmi seçim) durumundaki node'ları izler.</summary>
        private readonly HashSet<TreeNode> _mixedNodes = new();

        // ── Size Cache ──
        /// <summary>Dosya boyutu önbelleği: path → byte cinsinden boyut. LoadChildren sırasında doldurulur.</summary>
        private readonly ConcurrentDictionary<string, long> _fileSizeCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Klasör boyutu önbelleği: path → byte cinsinden toplam boyut (recursive).</summary>
        private readonly ConcurrentDictionary<string, long> _folderSizeCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Arka plan boyut hesaplamasını iptal etmek için.</summary>
        private CancellationTokenSource _sizeCts;

        /// <summary>Checkpoint durumu değiştiğinde tetiklenir.</summary>
        internal event EventHandler CheckStateChanged;

        /// <summary>Boyut hesaplaması tamamlandığında tetiklenir. Gerçek ve tahmini 7z boyutunu taşır.</summary>
        internal event EventHandler<SizeCalculationResult> SizeCalculated;

        // ── StateImageList checkbox indices ──
        private const int StateNone = 0;
        private const int StateUnchecked = 1;
        private const int StateChecked = 2;
        private const int StateIndeterminate = 3;

        internal FileSystemCheckedTreeView()
        {
            _imageList = CreateImageList();
            _stateImageList = CreateCheckboxImageList();

            _tree = new TreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = false,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                ImageList = _imageList,
                StateImageList = _stateImageList,
                Font = ModernTheme.FontBody,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Indent = 20,
                ItemHeight = 22,
                FullRowSelect = true
            };

            _tree.BeforeExpand += OnBeforeExpand;
            _tree.NodeMouseClick += OnNodeMouseClick;
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

        /// <summary>
        /// Tema renklerine uygun özel checkbox ikonları oluşturur.
        /// CheckBoxes=false ile kullanılır — Visual Styles override olmaz.
        /// Index 0: boş (yer tutucu), 1: unchecked, 2: checked, 3: indeterminate.
        /// </summary>
        private static ImageList CreateCheckboxImageList()
        {
            const int size = 16;
            ImageList list = new()
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(size, size)
            };

            // 0: Boş — dummy node'lar için (checkbox gösterilmez)
            list.Images.Add(new Bitmap(size, size));

            // 1: Unchecked
            list.Images.Add(RenderCheckboxBitmap(size, CheckboxState.Unchecked));

            // 2: Checked
            list.Images.Add(RenderCheckboxBitmap(size, CheckboxState.Checked));

            // 3: Indeterminate
            list.Images.Add(RenderCheckboxBitmap(size, CheckboxState.Indeterminate));

            return list;
        }

        private enum CheckboxState { Unchecked, Checked, Indeterminate }

        private static Bitmap RenderCheckboxBitmap(int size, CheckboxState state)
        {
            Bitmap bmp = new(size, size);
            bmp.SetResolution(96, 96);

            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            int boxSize = size - 3;
            int x = (size - boxSize) / 2;
            int y = (size - boxSize) / 2;
            Rectangle boxRect = new(x, y, boxSize - 1, boxSize - 1);

            switch (state)
            {
                case CheckboxState.Unchecked:
                    using (Pen borderPen = new(ModernTheme.TextSecondary, 1.5f))
                        g.DrawRectangle(borderPen, boxRect);
                    break;

                case CheckboxState.Checked:
                    using (SolidBrush fill = new(ModernTheme.AccentPrimary))
                        g.FillRectangle(fill, boxRect);
                    using (Pen checkPen = new(Color.White, 1.6f))
                    {
                        checkPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                        checkPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                        g.DrawLines(checkPen,
                        [
                            new PointF(x + boxSize * 0.22f, y + boxSize * 0.52f),
                            new PointF(x + boxSize * 0.42f, y + boxSize * 0.72f),
                            new PointF(x + boxSize * 0.78f, y + boxSize * 0.28f)
                        ]);
                    }
                    break;

                case CheckboxState.Indeterminate:
                    using (Pen borderPen = new(ModernTheme.StatusWarning, 1.5f))
                        g.DrawRectangle(borderPen, boxRect);
                    int pad = boxSize / 4;
                    Rectangle inner = new(x + pad, y + pad, boxSize - 2 * pad - 1, boxSize - 2 * pad - 1);
                    using (SolidBrush fill = new(ModernTheme.StatusWarning))
                        g.FillRectangle(fill, inner);
                    break;
            }

            return bmp;
        }

        /// <summary>Node'un checked durumda olup olmadığını döndürür (StateImageIndex == 2).</summary>
        private static bool IsNodeChecked(TreeNode node) => node.StateImageIndex == StateChecked;

        /// <summary>Node'un indeterminate (mixed) durumda olup olmadığını döndürür.</summary>
        internal bool IsNodeMixed(TreeNode node) => _mixedNodes.Contains(node);

        /// <summary>Node'un checked veya indeterminate durumda olup olmadığını döndürür.</summary>
        private static bool IsNodeCheckedOrMixed(TreeNode node)
            => node.StateImageIndex == StateChecked || node.StateImageIndex == StateIndeterminate;

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
                        node.StateImageIndex = StateChecked;
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

        /// <summary>
        /// Seçili dosya ve klasörlerin toplam boyutunu döndürür (byte).
        /// Önbellekte mevcut değerleri kullanır; eksik klasörler için -1 tahmini döner.
        /// Tam sonuç için SizeCalculated event'ini dinleyin.
        /// </summary>
        internal long GetCheckedTotalSize()
        {
            long total = 0;
            CalculateCheckedSize(_tree.Nodes, ref total);
            return total;
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
                _sizeCts?.Cancel();
                _sizeCts?.Dispose();
                _imageList?.Dispose();
                _stateImageList?.Dispose();
                _mixedNodes.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
