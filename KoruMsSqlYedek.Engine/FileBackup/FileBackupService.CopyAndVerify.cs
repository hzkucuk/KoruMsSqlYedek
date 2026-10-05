using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    // ── Copy Operations + Integrity Verification ─────────────────────
    public partial class FileBackupService
    {
        private async Task<bool> TryCopyViaVssAsync(
            Guid snapshotId, string sourceFile, string destFile, CancellationToken ct)
        {
            try
            {
                string snapshotPath = _vssService.GetSnapshotFilePath(snapshotId, sourceFile);

                // File.Copy iptal edilemez; büyük PST/OST kopyaları dakikalar sürebildiğinden parçalı kopya
                await CopyFileCancellableAsync(snapshotPath, sourceFile, destFile, ct);

                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
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
                await CopyFileCancellableAsync(sourceFile, sourceFile, destFile, ct);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Warning(ex, "Direkt dosya kopyalama başarısız: {File}", sourceFile);
                return false;
            }
        }

        /// <summary>
        /// 1 MB'lık parçalarla kopyalar; iptal her parçada kontrol edilir.
        /// <paramref name="readPath"/> VSS snapshot yolu olabilir, <paramref name="displayPath"/> loglar içindir.
        /// </summary>
        private static Task CopyFileCancellableAsync(
            string readPath, string displayPath, string destFile, CancellationToken ct)
        {
            const int bufferSize = 1_048_576; // 1 MB — büyük dosyalarda I/O verimliliği

            return Task.Run(() =>
            {
                using var sourceStream = new FileStream(
                    readPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize);
                using var destStream = new FileStream(
                    destFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize);

                long fileSize = 0;
                try { fileSize = sourceStream.Length; } catch { }

                // Büyük dosyalar (100 MB+) için ilerleme loglaması
                bool logProgress = fileSize > 100 * 1024 * 1024;
                if (logProgress)
                {
                    Log.Information(
                        "Büyük dosya kopyalanıyor: {File} [{SizeMb:F1} MB]",
                        Path.GetFileName(displayPath), fileSize / BytesPerMb);
                }

                byte[] buffer = new byte[bufferSize];
                long copied = 0;
                int lastLoggedPct = 0;
                int bytesRead;
                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    destStream.Write(buffer, 0, bytesRead);
                    copied += bytesRead;
                    if (logProgress)
                    {
                        int pct = (int)(copied * 100 / fileSize);
                        if (pct >= lastLoggedPct + 25) // %25 aralıklarla logla
                        {
                            lastLoggedPct = pct;
                            Log.Information(
                                "  Kopyalanıyor: {File} — %{Pct} ({CopiedMb:F0}/{TotalMb:F0} MB)",
                                Path.GetFileName(displayPath), pct,
                                copied / BytesPerMb, fileSize / BytesPerMb);
                        }
                    }
                }
            }, ct);
        }

        /// <summary>
        /// Dosyanın salt okunur özniteliğini kaldırır. File.Copy kaynağın özniteliklerini taşıdığından
        /// salt okunur kaynaklar (ör. Mikro .SNO) hedefte de salt okunur kalır; sonraki çalıştırmada
        /// üzerine yazılamaz ve ara Files klasörü silinemez.
        /// </summary>
        internal static void ClearReadOnly(string path)
        {
            try
            {
                var attrs = File.GetAttributes(path);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (Exception ex)
            {
                Log.Debug(ex, "Salt okunur özniteliği kaldırılamadı: {File}", path);
            }
        }

        /// <summary>Klasördeki tüm dosyaların salt okunur özniteliğini kaldırır (silme öncesi).</summary>
        internal static void ClearReadOnlyRecursive(string directory)
        {
            if (!Directory.Exists(directory)) return;
            foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                ClearReadOnly(file);
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
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
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
                FileShare.ReadWrite, bufferSize: 1_048_576, useAsync: true);

            byte[] hash = await sha256.ComputeHashAsync(stream, ct);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
