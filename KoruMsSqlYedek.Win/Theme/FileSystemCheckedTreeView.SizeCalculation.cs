using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    internal sealed partial class FileSystemCheckedTreeView
    {
        // ═══════════════ SIZE CALCULATION ═══════════════

        /// <summary>
        /// Seçili öğelerin boyutunu arka planda hesaplar ve SizeCalculated event'ini tetikler.
        /// Her çağrıda önceki hesaplama iptal edilir (debounce).
        /// Dosya uzantılarına göre tahmini 7z sıkıştırılmış boyut da hesaplanır.
        /// </summary>
        private void RequestSizeCalculationAsync()
        {
            _sizeCts?.Cancel();
            _sizeCts?.Dispose();
            _sizeCts = new CancellationTokenSource();
            CancellationToken ct = _sizeCts.Token;

            // Seçili yolları UI thread'inde topla
            List<string> checkedPaths = GetCheckedPaths();

            // Filtre kalıplarını kopyala — arka plan thread'inde güvenli erişim
            bool hasFilters = _includePatterns.Count > 0 || _excludePatterns.Count > 0;
            List<string> includeSnap = new(_includePatterns);
            List<string> excludeSnap = new(_excludePatterns);

            Task.Run(() =>
            {
                long total = 0;
                double estimated7z = 0;

                foreach (string path in checkedPaths)
                {
                    if (ct.IsCancellationRequested) return;

                    if (File.Exists(path))
                    {
                        // Tekil dosya — filtre kontrolü
                        if (hasFilters && !PassesFilter(Path.GetFileName(path), includeSnap, excludeSnap))
                            continue;

                        long size = GetFileSizeCached(path);
                        total += size;
                        estimated7z += size * Estimate7zRatio(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        if (hasFilters)
                        {
                            // Filtre varsa klasör içini tek tek tara
                            var (folderTotal, folder7z) = GetFilteredFolderSize(path, includeSnap, excludeSnap, ct);
                            total += folderTotal;
                            estimated7z += folder7z;
                        }
                        else
                        {
                            total += GetFolderSizeCached(path, ct);
                            estimated7z += GetFolderEstimated7zSize(path, ct);
                        }
                    }
                }

                if (!ct.IsCancellationRequested)
                {
                    try
                    {
                        SizeCalculationResult result = new(total, (long)estimated7z);
                        BeginInvoke(new Action(() => SizeCalculated?.Invoke(this, result)));
                    }
                    catch (InvalidOperationException) { }
                }
            }, ct);
        }

        /// <summary>
        /// Klasör içindeki dosyaları filtre kalıplarına göre filtreleyerek toplam boyut hesaplar.
        /// </summary>
        private (long Total, double Estimated7z) GetFilteredFolderSize(
            string folderPath, List<string> includes, List<string> excludes, CancellationToken ct)
        {
            long total = 0;
            double estimated7z = 0;

            try
            {
                foreach (string file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    if (ct.IsCancellationRequested) return (0, 0);

                    string fileName = Path.GetFileName(file);
                    if (!PassesFilter(fileName, includes, excludes))
                        continue;

                    try
                    {
                        long fileSize = GetFileSizeCached(file);
                        total += fileSize;
                        estimated7z += fileSize * Estimate7zRatio(file);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return (total, estimated7z);
        }

        /// <summary>
        /// Dosya adının dahil/hariç kalıplarına göre filtreyi geçip geçmediğini kontrol eder.
        /// Thread-safe: kendi kalıp listelerini kullanır.
        /// </summary>
        private static bool PassesFilter(string fileName, List<string> includes, List<string> excludes)
        {
            if (excludes.Any(p => MatchesWildcard(fileName, p)))
                return false;

            if (includes.Count > 0 && !includes.Any(p => MatchesWildcard(fileName, p)))
                return false;

            return true;
        }

        /// <summary>Dosya boyutunu cache'den döndürür, yoksa hesaplar ve cache'ler.</summary>
        private long GetFileSizeCached(string filePath)
        {
            if (_fileSizeCache.TryGetValue(filePath, out long cached))
                return cached;

            try
            {
                long size = new FileInfo(filePath).Length;
                _fileSizeCache[filePath] = size;
                return size;
            }
            catch (IOException) { return 0; }
            catch (UnauthorizedAccessException) { return 0; }
        }

        /// <summary>
        /// Klasör boyutunu cache'den döndürür, yoksa recursive hesaplar ve cache'ler.
        /// EnumerateFiles ile tek seferde tüm dosyaları tarar — verimli I/O.
        /// </summary>
        private long GetFolderSizeCached(string folderPath, CancellationToken ct)
        {
            if (_folderSizeCache.TryGetValue(folderPath, out long cached))
                return cached;

            long total = 0;
            try
            {
                foreach (string file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    if (ct.IsCancellationRequested) return 0;

                    try
                    {
                        long fileSize = GetFileSizeCached(file);
                        total += fileSize;
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            if (!ct.IsCancellationRequested)
                _folderSizeCache[folderPath] = total;

            return total;
        }

        /// <summary>
        /// Klasör içindeki dosyaların uzantılarına göre tahmini 7z sıkıştırılmış boyutunu hesaplar.
        /// </summary>
        private long GetFolderEstimated7zSize(string folderPath, CancellationToken ct)
        {
            double estimated = 0;
            try
            {
                foreach (string file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    if (ct.IsCancellationRequested) return 0;

                    try
                    {
                        long fileSize = GetFileSizeCached(file);
                        estimated += fileSize * Estimate7zRatio(file);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return (long)estimated;
        }

        /// <summary>
        /// Dosya uzantısına göre tahmini 7z sıkıştırma oranını döndürür.
        /// 0.0 = tam sıkıştırma, 1.0 = sıkıştırma yok.
        /// </summary>
        private static double Estimate7zRatio(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                // Zaten sıkıştırılmış formatlar — neredeyse küçülmez
                ".7z" or ".zip" or ".rar" or ".gz" or ".bz2" or ".xz" or ".zst" or ".cab" => 0.99,
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".heic" or ".avif" => 0.98,
                ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" => 0.98,
                ".mp3" or ".aac" or ".ogg" or ".flac" or ".wma" or ".opus" => 0.98,

                // Modern Office (dahili zip) — az küçülür
                ".docx" or ".xlsx" or ".pptx" or ".odt" or ".ods" or ".odp" => 0.92,
                ".pdf" => 0.87,

                // Eski Office & binary belgeler — orta sıkıştırma
                ".doc" or ".xls" or ".ppt" or ".rtf" => 0.40,

                // Veritabanı / yedek dosyaları — iyi sıkıştırır
                ".bak" or ".mdf" or ".ldf" or ".ndf" or ".trn" => 0.20,
                ".mdb" or ".accdb" or ".sqlite" or ".db" => 0.25,

                // Çalıştırılabilir / ikili — orta
                ".exe" or ".dll" or ".sys" or ".ocx" => 0.45,
                ".iso" or ".img" or ".vhd" or ".vhdx" => 0.55,
                ".pst" or ".ost" => 0.35,

                // Metin / kaynak kodu — çok iyi sıkıştırır
                ".txt" or ".log" or ".csv" or ".tsv" or ".md" => 0.12,
                ".cs" or ".vb" or ".java" or ".py" or ".js" or ".ts" or ".cpp" or ".h" or ".c" => 0.12,
                ".xml" or ".json" or ".yaml" or ".yml" or ".toml" or ".ini" or ".cfg" or ".config" => 0.12,
                ".html" or ".htm" or ".css" or ".scss" or ".less" => 0.12,
                ".sql" or ".ps1" or ".sh" or ".bat" or ".cmd" => 0.12,
                ".sln" or ".csproj" or ".vbproj" or ".fsproj" or ".props" or ".targets" => 0.12,

                // Bitmap görsel — iyi sıkıştırır
                ".bmp" or ".tif" or ".tiff" or ".raw" => 0.15,

                // Bilinmeyen — varsayılan orta tahmin
                _ => 0.50
            };
        }

        /// <summary>Checked node'ların boyutunu hesaplar (sync, cache'den).</summary>
        private void CalculateCheckedSize(TreeNodeCollection nodes, ref long total)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name == DummyNodeKey) continue;

                if (IsNodeChecked(node))
                {
                    string path = node.Tag as string;
                    if (string.IsNullOrEmpty(path)) continue;

                    bool allChildrenSelected = AllChildrenChecked(node);
                    if (allChildrenSelected || node.Nodes.Count == 0 ||
                        (node.Nodes.Count == 1 && node.Nodes[0].Name == DummyNodeKey))
                    {
                        if (_fileSizeCache.TryGetValue(path, out long fileSize))
                            total += fileSize;
                        else if (_folderSizeCache.TryGetValue(path, out long folderSize))
                            total += folderSize;
                    }
                    else
                    {
                        CalculateCheckedSize(node.Nodes, ref total);
                    }
                }
                else if (HasAnyCheckedChild(node))
                {
                    CalculateCheckedSize(node.Nodes, ref total);
                }
            }
        }
    }
}
