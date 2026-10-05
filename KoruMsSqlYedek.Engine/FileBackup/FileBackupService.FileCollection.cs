using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Serilog;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    // ── File Collection + Utility Helpers ─────────────────────────────
    public partial class FileBackupService
    {
        private List<string> CollectFiles(FileBackupSource source, CancellationToken ct = default)
        {
            var files = new List<string>();
            bool recursive = source.Recursive;

            // ── Yeni davranış: TreeView seçili yollar ──
            if (source.SelectedPaths?.Count > 0)
            {
                foreach (string selectedPath in source.SelectedPaths)
                {
                    ct.ThrowIfCancellationRequested();

                    if (File.Exists(selectedPath))
                    {
                        // Doğrudan seçili dosya
                        files.Add(selectedPath);
                    }
                    else if (Directory.Exists(selectedPath))
                    {
                        // Seçili klasör — içindeki dosyaları topla
                        CollectFilesFromDirectory(selectedPath, source.IncludePatterns, recursive, files, ct);
                    }
                    else
                    {
                        Log.Warning("Seçili yol bulunamadı: {Path}", selectedPath);
                    }
                }
            }
            // ── Eski davranış uyumu: SourcePath tabanlı ──
            else if (!string.IsNullOrWhiteSpace(source.SourcePath))
            {
                if (!Directory.Exists(source.SourcePath))
                {
                    Log.Warning("Kaynak dizin bulunamadı: {Path}", source.SourcePath);
                    return files;
                }

                CollectFilesFromDirectory(source.SourcePath, source.IncludePatterns, recursive, files, ct);
            }

            // Exclude pattern uygula
            if (source.ExcludePatterns.Count > 0)
            {
                files = files.Where(f => !MatchesAnyPattern(f, source.ExcludePatterns)).ToList();
            }

            // Tekrar eden yolları kaldır
            files = files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return files;
        }

        /// <summary>
        /// Bir dizindeki dosyaları include pattern'lara göre toplar.
        /// Erişilemeyen alt klasörler atlanır (Directory.GetFiles tek bir erişilemez alt klasörde
        /// tüm listeyi kaybediyordu); büyük ağaçlarda iptal her dosyada kontrol edilir.
        /// </summary>
        private void CollectFilesFromDirectory(
            string directoryPath, List<string> includePatterns, bool recursive, List<string> files,
            CancellationToken ct)
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = recursive,
                IgnoreInaccessible = true,
                AttributesToSkip = 0,          // GetFiles gibi gizli/sistem dosyaları da dahil
                MatchType = MatchType.Win32,
                MatchCasing = MatchCasing.CaseInsensitive
            };

            var patterns = includePatterns?.Count > 0 ? includePatterns : new List<string> { "*" };
            foreach (string pattern in patterns)
            {
                try
                {
                    foreach (string file in Directory.EnumerateFiles(directoryPath, pattern, options))
                    {
                        ct.ThrowIfCancellationRequested();
                        files.Add(file);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Warning(ex, "Erişim engellendi: {Path} ({Pattern})", directoryPath, pattern);
                }
                catch (DirectoryNotFoundException)
                {
                    // Dizin silinmiş olabilir, atla
                }
            }
        }

        private bool MatchesAnyPattern(string filePath, List<string> patterns)
        {
            string fileName = Path.GetFileName(filePath);
            foreach (string pattern in patterns)
            {
                string regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                    return true;
            }
            return false;
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()
                .Replace('/', '\\'));
        }

        private string SanitizeFolderName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
