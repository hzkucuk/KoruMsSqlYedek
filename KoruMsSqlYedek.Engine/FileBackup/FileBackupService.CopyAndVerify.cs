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
                const int bufferSize = 1_048_576; // 1 MB — büyük dosyalarda I/O verimliliği
                long fileSize = 0;
                try { fileSize = new FileInfo(sourceFile).Length; } catch { }

                // Büyük dosyalar (100 MB+) için ilerleme loglaması
                bool logProgress = fileSize > 100 * 1024 * 1024;
                if (logProgress)
                {
                    Log.Information(
                        "Büyük dosya kopyalanıyor: {File} [{SizeMb:F1} MB]",
                        Path.GetFileName(sourceFile), fileSize / BytesPerMb);
                }

                await Task.Run(() =>
                {
                    using (var sourceStream = new FileStream(
                        sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize))
                    using (var destStream = new FileStream(
                        destFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
                    {
                        if (!logProgress)
                        {
                            sourceStream.CopyTo(destStream, bufferSize);
                        }
                        else
                        {
                            // Buffered kopyalama ile periyodik log
                            byte[] buffer = new byte[bufferSize];
                            long copied = 0;
                            int lastLoggedPct = 0;
                            int bytesRead;
                            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ct.ThrowIfCancellationRequested();
                                destStream.Write(buffer, 0, bytesRead);
                                copied += bytesRead;
                                if (fileSize > 0)
                                {
                                    int pct = (int)(copied * 100 / fileSize);
                                    if (pct >= lastLoggedPct + 25) // %25 aralıklarla logla
                                    {
                                        lastLoggedPct = pct;
                                        Log.Information(
                                            "  Kopyalanıyor: {File} — %{Pct} ({CopiedMb:F0}/{TotalMb:F0} MB)",
                                            Path.GetFileName(sourceFile), pct,
                                            copied / BytesPerMb, fileSize / BytesPerMb);
                                    }
                                }
                            }
                        }
                    }
                }, ct);

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
                FileShare.ReadWrite, bufferSize: 1_048_576, useAsync: true);

            byte[] hash = await sha256.ComputeHashAsync(stream, ct);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
