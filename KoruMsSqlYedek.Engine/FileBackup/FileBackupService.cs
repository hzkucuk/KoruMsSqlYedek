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
    /// Her zaman tam (Full) yedekleme yapar.
    /// </summary>
    public partial class FileBackupService : IFileBackupService
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

                var result = await BackupSourceInternalAsync(source, basePath, null, cancellationToken,
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
            return await BackupSourceInternalAsync(source, destinationBasePath, progress,
                cancellationToken, verifyAfterCopy);
        }

        private async Task<FileBackupResult> BackupSourceInternalAsync(
            FileBackupSource source,
            string destinationBasePath,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            bool verifyAfterCopy)
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
    }
}
