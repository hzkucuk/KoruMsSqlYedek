using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    // ── File Collection + Utility Helpers ─────────────────────────────
    public partial class FileBackupService
    {
        private List<string> CollectFiles(FileBackupSource source)
        {
            var files = new List<string>();

            var searchOption = source.Recursive
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            // ── Yeni davranış: TreeView seçili yollar ──
            if (source.SelectedPaths?.Count > 0)
            {
                foreach (string selectedPath in source.SelectedPaths)
                {
                    if (File.Exists(selectedPath))
                    {
                        // Doğrudan seçili dosya
                        files.Add(selectedPath);
                    }
                    else if (Directory.Exists(selectedPath))
                    {
                        // Seçili klasör — içindeki dosyaları topla
                        CollectFilesFromDirectory(selectedPath, source.IncludePatterns, searchOption, files);
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

                CollectFilesFromDirectory(source.SourcePath, source.IncludePatterns, searchOption, files);
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

        /// <summary>Bir dizindeki dosyaları include pattern'lara göre toplar.</summary>
        private void CollectFilesFromDirectory(
            string directoryPath, List<string> includePatterns, SearchOption searchOption, List<string> files)
        {
            if (includePatterns?.Count > 0)
            {
                foreach (string pattern in includePatterns)
                {
                    try
                    {
                        files.AddRange(Directory.GetFiles(directoryPath, pattern, searchOption));
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.Warning(ex, "Erişim engellendi: {Path} ({Pattern})", directoryPath, pattern);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Alt dizin silinmiş olabilir, atla
                    }
                }
            }
            else
            {
                try
                {
                    files.AddRange(Directory.GetFiles(directoryPath, "*.*", searchOption));
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Warning(ex, "Erişim engellendi: {Path}", directoryPath);
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
