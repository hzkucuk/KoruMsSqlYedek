using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    /// <summary>
    /// Dosya/klasör yedekleme servisi.
    /// VSS desteği ile açık/kilitli dosyaları (Outlook PST/OST vb.) yedekler.
    /// VSS başarısız olursa normal kopyalama denenir.
    /// </summary>
    public class FileBackupService : IFileBackupService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<FileBackupService>();
        private readonly IVssService _vssService;
        private const double BytesPerMb = 1048576.0;

        public FileBackupService(IVssService vssService)
        {
            ArgumentNullException.ThrowIfNull(vssService);
            _vssService = vssService;
        }

        public async Task<List<FileBackupResult>> BackupFilesAsync(
            BackupPlan plan,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            var results = new List<FileBackupResult>();

            if (plan?.FileBackup == null || !plan.FileBackup.IsEnabled)
                return results;

            string basePath = Path.Combine(plan.LocalPath, "Files");
            Directory.CreateDirectory(basePath);

            int totalSources = plan.FileBackup.Sources.Count(s => s.IsEnabled);
            int processed = 0;

            foreach (var source in plan.FileBackup.Sources.Where(s => s.IsEnabled))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await BackupSourceAsync(source, basePath, null, cancellationToken,
                    verifyAfterCopy: plan.VerifyAfterBackup);
                result.PlanId = plan.PlanId;
                results.Add(result);

                processed++;
                progress?.Report((int)((double)processed / totalSources * 100));
            }

            return results;
        }

        public async Task<FileBackupResult> BackupSourceAsync(
            FileBackupSource source,
            string destinationBasePath,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            bool verifyAfterCopy = false)
        {
            var result = new FileBackupResult
            {
                SourceName = source.SourceName,
                SourcePath = source.SourcePath,
                StartedAt = DateTime.UtcNow
            };

            string destDir = Path.Combine(destinationBasePath, SanitizeFolderName(source.SourceName));
            Directory.CreateDirectory(destDir);
            result.DestinationPath = destDir;

            try
            {
                var filesToBackup = CollectFiles(source);

                Log.Information(
                    "Dosya yedekleme başlıyor: {SourceName} — {FileCount} dosya bulundu",
                    source.SourceName, filesToBackup.Count);

                bool useVss = source.UseVss && _vssService != null && _vssService.IsAvailable();
                Guid? snapshotId = null;

                if (useVss)
                {
                    try
                    {
                        // Kaynak dizinin volume'unu al ve snapshot oluştur
                        // SelectedPaths varsa ortak kök, yoksa SourcePath kullanılır
                        string volumeRoot = Path.GetPathRoot(source.SourcePath);
                        if (string.IsNullOrEmpty(volumeRoot) && source.SelectedPaths?.Count > 0)
                            volumeRoot = Path.GetPathRoot(source.SelectedPaths[0]);

                        // CreateSnapshot bloke edici VSS çağrıları içerir — Task.Run ile offload et
                        snapshotId = await Task.Run(
                            () => _vssService.CreateSnapshot(volumeRoot, cancellationToken),
                            CancellationToken.None);
                        result.UsedVss = true;
                        Log.Information("VSS snapshot aktif: {Volume}", volumeRoot);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Log.Warning(ex,
                            "VSS snapshot oluşturulamadı, normal kopyalama denenecek: {SourceName}",
                            source.SourceName);
                        useVss = false;
                        result.UsedVss = false;
                    }
                }

                int processedFiles = 0;

                foreach (string sourceFile in filesToBackup)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        string relativePath = GetRelativePath(source.SourcePath, sourceFile);
                        string destFile = Path.Combine(destDir, relativePath);
                        string destFileDir = Path.GetDirectoryName(destFile);

                        if (!string.IsNullOrEmpty(destFileDir))
                            Directory.CreateDirectory(destFileDir);

                        bool copied = false;

                        // VSS ile kopyalama dene
                        if (useVss && snapshotId.HasValue)
                        {
                            copied = await TryCopyViaVssAsync(
                                snapshotId.Value, sourceFile, destFile, cancellationToken);
                        }

                        // VSS başarısız ise veya kapalı ise normal kopyalama
                        if (!copied)
                        {
                            copied = await TryCopyDirectAsync(sourceFile, destFile, cancellationToken);
                        }

                        if (copied)
                        {
                            result.FilesCopied++;
                            var fi = new FileInfo(destFile);
                            result.TotalSizeBytes += fi.Length;

                            // Bütünlük doğrulaması: boyut eşleşmesi + SHA-256
                            if (verifyAfterCopy)
                            {
                                bool verified = await VerifyFileCopyIntegrityAsync(
                                    sourceFile, destFile, cancellationToken);

                                if (verified)
                                    result.FilesVerified++;
                                else
                                    result.FilesVerificationFailed++;
                            }
                        }
                        else
                        {
                            result.FilesSkipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FilesSkipped++;
                        result.FailedFiles.Add(new FailedFileInfo
                        {
                            FilePath = sourceFile,
                            ErrorMessage = ex.Message
                        });
                        Log.Warning(ex, "Dosya kopyalanamadı: {File}", sourceFile);
                    }

                    processedFiles++;
                    if (filesToBackup.Count > 0)
                        progress?.Report((int)((double)processedFiles / filesToBackup.Count * 100));
                }

                // VSS snapshot'ı temizle
                if (snapshotId.HasValue)
                {
                    _vssService.DeleteSnapshot(snapshotId.Value);
                }

                result.Status = filesToBackup.Count == 0
                    ? BackupResultStatus.Failed
                    : result.FailedFiles.Count == 0
                        ? BackupResultStatus.Success
                        : result.FilesCopied > 0
                            ? BackupResultStatus.PartialSuccess
                            : BackupResultStatus.Failed;

                if (filesToBackup.Count == 0)
                    result.ErrorMessage = "Kaynak dizinde eşleşen dosya bulunamadı";

                result.CompletedAt = DateTime.UtcNow;

                Log.Information(
                    "Dosya yedekleme tamamlandı: {SourceName} — {Copied} kopyalandı, {Skipped} atlandı, {Verified} doğrulandı, {Status} [{SizeMb:F1} MB]",
                    source.SourceName, result.FilesCopied, result.FilesSkipped,
                    result.FilesVerified, result.Status, result.TotalSizeBytes / BytesPerMb);
            }
            catch (Exception ex)
            {
                result.Status = BackupResultStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
                Log.Error(ex, "Dosya yedekleme hatası: {SourceName}", source.SourceName);
            }

            return result;
        }

        private async Task<bool> TryCopyViaVssAsync(
            Guid snapshotId, string sourceFile, string destFile, CancellationToken ct)
        {
            try
            {
                string snapshotPath = _vssService.GetSnapshotFilePath(snapshotId, sourceFile);

                await Task.Run(() =>
                {
                    File.Copy(snapshotPath, destFile, overwrite: true);
                }, ct);

                return true;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "VSS kopyalama başarısız, direkt denenecek: {File}", sourceFile);
                return false;
            }
        }

        private async Task<bool> TryCopyDirectAsync(
            string sourceFile, string destFile, CancellationToken ct)
        {
            try
            {
                await Task.Run(() =>
                {
                    // FileShare.ReadWrite ile açık dosyaları okumaya çalış
                    using (var sourceStream = new FileStream(
                        sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var destStream = new FileStream(
                        destFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                }, ct);

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Direkt dosya kopyalama başarısız: {File}", sourceFile);
                return false;
            }
        }

        /// <summary>
        /// Kopyalanan dosyanın bütünlüğünü doğrular.
        /// 1. Boyut karşılaştırması (her durumda).
        /// 2. SHA-256 karşılaştırması (kaynak kilitli değilse).
        /// Kaynak kilitli ise boyut eşleşmesi yeterli kabul edilir.
        /// </summary>
        private async Task<bool> VerifyFileCopyIntegrityAsync(
            string sourceFile, string destFile, CancellationToken ct)
        {
            try
            {
                // 1. Boyut kontrolü — kilitli dosyalarda da FileInfo çalışır
                var srcInfo = new FileInfo(sourceFile);
                var dstInfo = new FileInfo(destFile);

                if (srcInfo.Length != dstInfo.Length)
                {
                    Log.Error(
                        "Dosya kopyası boyut uyuşmazlığı: {Source} ({SrcBytes} B) ≠ {Dest} ({DstBytes} B)",
                        Path.GetFileName(sourceFile), srcInfo.Length,
                        Path.GetFileName(destFile), dstInfo.Length);
                    return false;
                }

                // 2. SHA-256 karşılaştırması
                string srcHash = await ComputeFileSha256Async(sourceFile, ct);
                string dstHash = await ComputeFileSha256Async(destFile, ct);

                if (!string.Equals(srcHash, dstHash, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Error(
                        "Dosya kopyası SHA-256 uyuşmazlığı: {File} — src={SrcHash} dst={DstHash}",
                        Path.GetFileName(sourceFile), srcHash, dstHash);
                    return false;
                }

                Log.Debug("Dosya bütünlük doğrulaması ✓: {File}", Path.GetFileName(destFile));
                return true;
            }
            catch (IOException)
            {
                // Kaynak dosya kilitli → boyut eşleşmesi (zaten kontrol edildi) yeterli
                Log.Debug(
                    "Kaynak kilitli, SHA-256 atlandı — boyut doğrulaması ile onaylandı: {File}",
                    Path.GetFileName(sourceFile));
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dosya bütünlük doğrulaması başarısız: {File}", sourceFile);
                return false;
            }
        }

        /// <summary>
        /// Dosyanın SHA-256 hash değerini stream üzerinden hesaplar.
        /// </summary>
        private static async Task<string> ComputeFileSha256Async(string filePath, CancellationToken ct)
        {
            using var sha256 = SHA256.Create();
            using var stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite, bufferSize: 81920, useAsync: true);

            byte[] hash = await sha256.ComputeHashAsync(stream, ct);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

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
