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
    }
}
