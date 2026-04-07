using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    internal sealed partial class FileSystemCheckedTreeView
    {
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

                    // Boyutu önbelleğe al — FileInfo zaten elde edildiği için ek I/O maliyeti yok
                    try { _fileSizeCache[file.FullName] = file.Length; }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }

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
            RequestSizeCalculationAsync();
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
    }
}
