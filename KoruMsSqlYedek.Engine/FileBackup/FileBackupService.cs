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
using KoruMsSqlYedek.Core.Helpers;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    /// <summary>
    /// Dosya/klasör yedekleme servisi.
    /// VSS desteği ile açık/kilitli dosyaları (Outlook PST/OST vb.) yedekler.
    /// VSS başarısız olursa normal kopyalama denenir — SQL Server veritabanı dosyaları hariç:
    /// onlar yalnızca snapshot'tan okunur, orijinal yol hiçbir koşulda açılmaz.
    /// Her zaman tam (Full) yedekleme yapar.
    /// </summary>
    public partial class FileBackupService : IFileBackupService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<FileBackupService>();
        private readonly IVssService _vssService;
        private readonly ISqlBackupService _sqlBackupService;
        private const double BytesPerMb = 1048576.0;

        public FileBackupService(IVssService vssService, ISqlBackupService sqlBackupService = null)
        {
            ArgumentNullException.ThrowIfNull(vssService);
            _vssService = vssService;
            _sqlBackupService = sqlBackupService;
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

            var enabledSources = plan.FileBackup.Sources.Where(s => s.IsEnabled).ToList();
            int totalSources = enabledSources.Count;
            int processed = 0;

            // SQL Server'ın dosyaları (uzantıdan bağımsız — Mikro "._MS" gibi adlar kullanır)
            var sqlFiles = await PrepareSqlDatabaseFilesAsync(plan, enabledSources, cancellationToken);

            foreach (var source in enabledSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await BackupSourceInternalAsync(source, basePath, null, cancellationToken,
                    verifyAfterCopy: plan.VerifyAfterBackup, sqlDatabaseFiles: sqlFiles);
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
                cancellationToken, verifyAfterCopy, sqlDatabaseFiles: null);
        }

        private async Task<FileBackupResult> BackupSourceInternalAsync(
            FileBackupSource source,
            string destinationBasePath,
            IProgress<int> progress,
            CancellationToken cancellationToken,
            bool verifyAfterCopy,
            ISet<string> sqlDatabaseFiles)
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

            Guid? snapshotId = null;

            try
            {
                // Büyük ağaçlarda tarama dakikalar sürebilir — çağıran thread'i bloke etmesin, iptal edilebilsin
                var filesToBackup = await Task.Run(() => CollectFiles(source, cancellationToken), cancellationToken);

                Log.Information(
                    "Dosya yedekleme başlıyor: {SourceName} — {FileCount} dosya bulundu",
                    source.SourceName, filesToBackup.Count);

                bool useVss = source.UseVss && _vssService != null && _vssService.IsAvailable();
                string volumeRoot = null;

                if (useVss)
                {
                    try
                    {
                        // Kaynak dizinin volume'unu al ve snapshot oluştur
                        // SelectedPaths varsa ortak kök, yoksa SourcePath kullanılır
                        volumeRoot = Path.GetPathRoot(source.SourcePath);
                        if (string.IsNullOrEmpty(volumeRoot) && source.SelectedPaths?.Count > 0)
                            volumeRoot = Path.GetPathRoot(source.SelectedPaths[0]);

                        // CreateSnapshot bloke edici VSS çağrıları içerir — Task.Run ile offload et.
                        // İptal beklenmeden döner; geç tamamlanan snapshot arka planda silinir.
                        var snapshotTask = Task.Run(
                            () => _vssService.CreateSnapshot(volumeRoot, cancellationToken),
                            CancellationToken.None);
                        try
                        {
                            snapshotId = await snapshotTask.WaitAsync(cancellationToken);
                        }
                        catch (OperationCanceledException) when (!snapshotTask.IsCompleted)
                        {
                            _ = snapshotTask.ContinueWith(
                                t => _vssService.DeleteSnapshot(t.Result),
                                CancellationToken.None,
                                TaskContinuationOptions.OnlyOnRanToCompletion,
                                TaskScheduler.Default);
                            throw;
                        }
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

                        // Kopyanın okunduğu yol: VSS'te snapshot yolu, aksi halde orijinal.
                        // Doğrulama da AYNI yoldan yapılır — orijinali yeniden açmak hem
                        // snapshot sonrası değişmiş dosyada sahte uyuşmazlık verir hem de
                        // SQL Server dosyalarında veritabanını RECOVERY_PENDING'e düşürür.
                        string readPath = null;
                        bool isSqlFile = IsSqlDatabaseFile(sourceFile, sqlDatabaseFiles);

                        // Önceki çalıştırmadan kalan salt okunur hedef üzerine yazmayı engeller
                        ClearReadOnly(destFile);

                        // VSS ile kopyalama dene — yalnızca snapshot'ı alınan volume'deki dosyalar
                        // (başka diskteki bir SelectedPaths dosyası için snapshot yolu yanlış dosyayı gösterir)
                        if (useVss && snapshotId.HasValue
                            && string.Equals(Path.GetPathRoot(sourceFile), volumeRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            readPath = await TryCopyViaVssAsync(
                                snapshotId.Value, sourceFile, destFile, cancellationToken);
                        }

                        if (readPath == null && isSqlFile)
                        {
                            // SQL Server dosyası snapshot olmadan ASLA açılmaz: AUTO_CLOSE ile kapanmış
                            // veritabanının dosyasını tutmak SQL Server'ın yeniden açışını OS error 32
                            // ile düşürür ve veritabanı RECOVERY_PENDING'e geçer.
                            // Yarıda kalmış snapshot kopyası arşive girmesin
                            try { if (File.Exists(destFile)) File.Delete(destFile); } catch { }

                            result.FilesSkipped++;
                            result.FailedFiles.Add(new FailedFileInfo
                            {
                                FilePath = sourceFile,
                                ErrorMessage = SqlFileSkippedMessage
                            });
                            Log.Warning(
                                "SQL Server veritabanı dosyası snapshot olmadan açılmaz, atlandı: {File}",
                                sourceFile);
                        }
                        else
                        {
                            // VSS başarısız ise veya kapalı ise normal kopyalama
                            if (readPath == null
                                && await TryCopyDirectAsync(sourceFile, destFile, cancellationToken))
                            {
                                readPath = sourceFile;
                            }

                            if (readPath != null)
                            {
                                ClearReadOnly(destFile);
                                result.FilesCopied++;
                                var fi = new FileInfo(destFile);
                                result.TotalSizeBytes += fi.Length;

                                // Bütünlük doğrulaması: boyut eşleşmesi + SHA-256
                                if (verifyAfterCopy)
                                {
                                    bool verified = await VerifyFileCopyIntegrityAsync(
                                        readPath, destFile, cancellationToken);

                                    if (verified)
                                        result.FilesVerified++;
                                    else
                                        result.FilesVerificationFailed++;
                                }
                            }
                            else
                            {
                                // Yarım kalmış (veya önceki çalıştırmadan kalan) hedef arşive girmesin
                                try { if (File.Exists(destFile)) File.Delete(destFile); } catch { }
                                result.FilesSkipped++;
                            }
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.FilesSkipped++;
                        result.FailedFiles.Add(new FailedFileInfo
                        {
                            FilePath = sourceFile,
                            ErrorMessage = ExceptionMessageHelper.Describe(ex)
                        });
                        Log.Warning(ex, "Dosya kopyalanamadı: {File}", sourceFile);
                    }

                    processedFiles++;
                    if (filesToBackup.Count > 0)
                        progress?.Report((int)((double)processedFiles / filesToBackup.Count * 100));
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // İptal "başarısız kaynak" sonucuna dönüştürülmez; pipeline iptal yoluna düşmeli
                throw;
            }
            catch (Exception ex)
            {
                result.Status = BackupResultStatus.Failed;
                result.ErrorMessage = ExceptionMessageHelper.Describe(ex);
                result.CompletedAt = DateTime.UtcNow;
                Log.Error(ex, "Dosya yedekleme hatası: {SourceName}", source.SourceName);
            }
            finally
            {
                // İptal/hata dahil her durumda snapshot bırakılır (önceden yalnızca başarılı yolda siliniyordu)
                if (snapshotId.HasValue)
                    _vssService.DeleteSnapshot(snapshotId.Value);
            }

            return result;
        }
    }
}
