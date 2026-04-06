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

        // ── State ──
        private bool _suppressCheckEvent;
        private List<string> _includePatterns = new();
        private List<string> _excludePatterns = new();

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
            }
            base.Dispose(disposing);
        }
    }
}
