using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    internal sealed partial class FileSystemCheckedTreeView
    {
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

        private void UncheckAll(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                _mixedNodes.Remove(node);
                node.Checked = false;
                if (node.Nodes.Count > 0)
                    UncheckAll(node.Nodes);
            }
        }

        private void CountChecked(TreeNodeCollection nodes, ref int folders, ref int files)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name == DummyNodeKey) continue;
                if (node.Checked && !_mixedNodes.Contains(node))
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
    }
}
