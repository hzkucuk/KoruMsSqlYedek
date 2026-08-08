using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    /// <summary>
    /// wimlib-imagex aracılığıyla WIM tabanlı disk imajı yedekleme servisi.
    /// wbadmin'in yerini alır: wbadmin sıkıştırma yapmadan VHDX üretirken,
    /// wimlib LZMS solid sıkıştırma ile tipik olarak %60-70 daha küçük imaj üretir.
    /// SYSTEM yetkisiyle çalışan Windows Service ortamında kullanılmak üzere tasarlanmıştır.
    /// </summary>
    public class WimlibDiskImageService : IDiskImageService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<WimlibDiskImageService>();

        /// <summary>"(45%) done" gibi ilerleme çıktılarını yakalar.</summary>
        private static readonly Regex ProgressRegex = new Regex(
            @"(\d{1,3})\s*%",
            RegexOptions.Compiled);

        /// <inheritdoc/>
        public async Task<List<DiskImageResult>> BackupDiskImagesAsync(
            BackupPlan plan,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(plan);

            var results = new List<DiskImageResult>();

            var config = plan.DiskImageBackup;
            if (config is null || !config.IsEnabled || config.Sources is null || config.Sources.Count == 0)
            {
                Log.Information("Disk imajı yedekleme devre dışı veya kaynak tanımlanmamış. Plan: {PlanId}", plan.PlanId);
                return results;
            }

            string destDir = Path.Combine(plan.LocalPath, config.SubDirectory ?? "DiskImages");
            Directory.CreateDirectory(destDir);

            // Etkin kaynakları önce süz — ilerleme dilimleri atlanan kaynaklar yüzünden kaymasın
            var activeSources = config.Sources.FindAll(s => s.IsEnabled);
            if (activeSources.Count == 0)
            {
                Log.Warning("Disk imajı yedekleme: Aktif kaynak yok. Plan: {PlanId}", plan.PlanId);
                return results;
            }

            int total = activeSources.Count;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int index = i;
                var slicedProgress = new Progress<int>(p =>
                {
                    int overall = (index * 100 + p) / total;
                    progress?.Report(Math.Clamp(overall, 0, 100));
                });

                var result = await BackupVolumeAsync(
                        activeSources[i], destDir, config.Format,
                        config.Compression, config.WriteIntegrityTable,
                        slicedProgress, cancellationToken)
                    .ConfigureAwait(false);

                result.PlanId = plan.PlanId;
                results.Add(result);
            }

            progress?.Report(100);
            return results;
        }

        /// <inheritdoc/>
        public Task<DiskImageResult> BackupVolumeAsync(
            DiskImageSource source,
            string destinationPath,
            DiskImageFormat format,
            IProgress<int> progress,
            CancellationToken cancellationToken)
            => BackupVolumeAsync(source, destinationPath, format,
                DiskImageCompression.Solid, writeIntegrityTable: true,
                progress, cancellationToken);

        /// <summary>
        /// Tek bir sürücünün WIM imajını oluşturur.
        /// </summary>
        public async Task<DiskImageResult> BackupVolumeAsync(
            DiskImageSource source,
            string destinationPath,
            DiskImageFormat format,
            DiskImageCompression compression,
            bool writeIntegrityTable,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destinationPath);
            if (string.IsNullOrWhiteSpace(source.VolumePath))
                throw new ArgumentException("VolumePath boş olamaz.", nameof(source));

            var result = new DiskImageResult
            {
                VolumePath = source.VolumePath,
                Format = format,
                StartedAt = DateTime.UtcNow
            };

            string exePath = WimlibCommandBuilder.ResolveExecutablePath();
            if (exePath is null)
            {
                result.Status = BackupResultStatus.Failed;
                result.ErrorMessage =
                    $"{WimlibCommandBuilder.ExecutableName} bulunamadı. " +
                    $"Uygulama dizini altındaki {WimlibCommandBuilder.NativeSubDirectory} klasörüne " +
                    "yerleştirin veya PATH'e ekleyin.";
                result.CompletedAt = DateTime.UtcNow;
                Log.Error("Disk imajı yedekleme başarısız: {Message}", result.ErrorMessage);
                return result;
            }

            // Çıktı yolunu biz belirliyoruz — konsol çıktısından yol parse etmeye gerek yok
            string outputPath = Path.Combine(
                destinationPath,
                WimlibCommandBuilder.BuildImageFileName(source.VolumePath, DateTime.Now));
            result.OutputPath = outputPath;

            string args = WimlibCommandBuilder.BuildCaptureArguments(
                source.VolumePath, outputPath, compression, writeIntegrityTable);

            Log.Information(
                "Disk imajı yedekleme başlıyor. Kaynak: {Volume}, Hedef: {Output}, Sıkıştırma: {Compression}",
                source.VolumePath, outputPath, compression);
            Log.Debug("wimlib komutu: {Exe} {Args}", exePath, args);

            var stderrBuffer = new StringBuilder();

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                process.Start();

                // İptal edilirse süreci ağacıyla birlikte sonlandır
                using var registration = cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(entireProcessTree: true);
                            Log.Warning("wimlib süreci iptal nedeniyle sonlandırıldı. Kaynak: {Volume}", source.VolumePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "wimlib süreci sonlandırılırken hata oluştu.");
                    }
                });

                var stdoutTask = PumpProgressAsync(process.StandardOutput, progress);
                var stderrTask = PumpErrorAsync(process.StandardError, stderrBuffer);

                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (process.ExitCode != 0)
                {
                    result.Status = BackupResultStatus.Failed;
                    result.ErrorMessage = BuildErrorMessage(process.ExitCode, stderrBuffer);

                    // Yarım kalan imaj dosyası geri yüklemede tuzağa dönüşür — temizle
                    TryDeleteIncompleteImage(outputPath);
                    result.OutputPath = null;

                    Log.Error("wimlib başarısız oldu. ExitCode: {Code}, Kaynak: {Volume}, Hata: {Error}",
                        process.ExitCode, source.VolumePath, result.ErrorMessage);
                }
                else
                {
                    result.Status = BackupResultStatus.Success;
                    result.ImageSizeBytes = GetFileSize(outputPath);
                    progress?.Report(100);

                    Log.Information(
                        "Disk imajı başarıyla oluşturuldu. Kaynak: {Volume}, Dosya: {Path}, Boyut: {SizeMB} MB",
                        source.VolumePath, outputPath, result.ImageSizeBytes / 1024 / 1024);
                }
            }
            catch (OperationCanceledException)
            {
                result.Status = BackupResultStatus.Cancelled;
                result.ErrorMessage = "Kullanıcı tarafından iptal edildi.";
                TryDeleteIncompleteImage(outputPath);
                result.OutputPath = null;
                Log.Information("Disk imajı yedekleme iptal edildi. Kaynak: {Volume}", source.VolumePath);
                throw;
            }
            catch (Exception ex)
            {
                result.Status = BackupResultStatus.Failed;
                result.ErrorMessage = ex.Message;
                TryDeleteIncompleteImage(outputPath);
                result.OutputPath = null;
                Log.Error(ex, "Disk imajı yedekleme hatası. Kaynak: {Volume}", source.VolumePath);
                throw;
            }
            finally
            {
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

        /// <summary>
        /// stdout'u karakter tamponuyla okur ve ilerleme yüzdesini bildirir.
        /// wimlib ilerlemeyi satır sonu yerine satır başı (\r) ile güncellediğinden
        /// ReadLineAsync kullanılamaz — aksi halde işlem bitene kadar hiç ilerleme görünmez.
        /// </summary>
        private static async Task PumpProgressAsync(StreamReader reader, IProgress<int> progress)
        {
            var buffer = new char[1024];
            var current = new StringBuilder();
            int lastReported = -1;

            int read;
            while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    char c = buffer[i];
                    if (c == '\r' || c == '\n')
                    {
                        if (current.Length > 0)
                        {
                            ReportLine(current.ToString(), progress, ref lastReported);
                            current.Clear();
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            if (current.Length > 0)
                ReportLine(current.ToString(), progress, ref lastReported);
        }

        private static void ReportLine(string line, IProgress<int> progress, ref int lastReported)
        {
            Log.Verbose("[wimlib] {Line}", line);

            var match = ProgressRegex.Match(line);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out int pct))
                return;

            pct = Math.Clamp(pct, 0, 100);

            // Aynı yüzdeyi tekrar tekrar bildirip UI'yı olay yağmuruna tutma
            if (pct == lastReported)
                return;

            lastReported = pct;
            progress?.Report(pct);
        }

        /// <summary>
        /// stderr'ı okur, loglar ve hata mesajı üretmek üzere son satırları saklar.
        /// </summary>
        private static async Task PumpErrorAsync(StreamReader reader, StringBuilder buffer)
        {
            string line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Log.Warning("[wimlib stderr] {Line}", line);

                // Hata mesajı e-postaya da gidiyor; sınırsız büyümesin
                if (buffer.Length < 2000)
                    buffer.AppendLine(line.Trim());
            }
        }

        private static string BuildErrorMessage(int exitCode, StringBuilder stderr)
        {
            string detail = stderr.ToString().Trim();
            return string.IsNullOrEmpty(detail)
                ? $"wimlib çıkış kodu: {exitCode}"
                : $"wimlib çıkış kodu: {exitCode} — {detail}";
        }

        /// <summary>
        /// Başarısız veya iptal edilmiş işlemden kalan yarım imajı siler.
        /// Yarım WIM dosyası geçerli görünüp geri yüklemede başarısız olabileceği için bırakılmaz.
        /// </summary>
        private static void TryDeleteIncompleteImage(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Log.Information("Yarım kalan disk imajı silindi: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Yarım kalan disk imajı silinemedi: {Path}", path);
            }
        }

        private static long GetFileSize(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
            try { return new FileInfo(path).Length; } catch { return 0; }
        }
    }
}
