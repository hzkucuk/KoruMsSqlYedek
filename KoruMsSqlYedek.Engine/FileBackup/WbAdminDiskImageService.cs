using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    /// <summary>
    /// wbadmin.exe aracılığıyla WIM tabanlı disk imajı yedekleme servisi.
    /// SYSTEM yetkisiyle çalışan Windows Service ortamında kullanılmak üzere tasarlanmıştır.
    /// </summary>
    public class WbAdminDiskImageService : IDiskImageService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<WbAdminDiskImageService>();

        // "Yüzde XX tamamlandı" veya "XX percent complete" gibi çıktıları yakalar
        private static readonly Regex ProgressRegex = new Regex(
            @"(\d{1,3})\s*(?:percent|yüzde|%)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            int total = config.Sources.Count;
            int done = 0;

            foreach (var source in config.Sources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!source.IsEnabled)
                {
                    Log.Debug("Disk imajı kaynağı devre dışı, atlanıyor: {Volume}", source.VolumePath);
                    continue;
                }

                // Her kaynak için ilerlemeyi orantılı olarak dağıt
                int basePercent = total > 0 ? done * 100 / total : 0;
                int sliceSize = total > 0 ? 100 / total : 100;

                var slicedProgress = new Progress<int>(p =>
                {
                    int overall = basePercent + p * sliceSize / 100;
                    progress?.Report(Math.Min(overall, 100));
                });

                var result = await BackupVolumeAsync(
                    source, destDir, config.Format, slicedProgress, cancellationToken)
                    .ConfigureAwait(false);

                results.Add(result);
                done++;
            }

            progress?.Report(100);
            return results;
        }

        /// <inheritdoc/>
        public async Task<DiskImageResult> BackupVolumeAsync(
            DiskImageSource source,
            string destinationPath,
            DiskImageFormat format,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (string.IsNullOrWhiteSpace(source.VolumePath))
                throw new ArgumentException("VolumePath boş olamaz.", nameof(source));
            ArgumentNullException.ThrowIfNull(destinationPath);

            var result = new DiskImageResult
            {
                VolumePath = source.VolumePath,
                Format = format,
                StartedAt = DateTime.UtcNow
            };

            Log.Information("Disk imajı yedekleme başlıyor. Kaynak: {Volume}, Hedef: {Dest}, Format: {Format}",
                source.VolumePath, destinationPath, format);

            try
            {
                string args = BuildWbAdminArguments(source.VolumePath, destinationPath, format);
                Log.Debug("wbadmin argümanları: {Args}", args);

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wbadmin.exe",
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // Stdout ve stderr'ı eşzamansız oku
                string outputPath = null;
                var readTask = Task.Run(async () =>
                {
                    string line;
                    while ((line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Log.Debug("[wbadmin] {Line}", line);

                        var match = ProgressRegex.Match(line);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int pct))
                            progress?.Report(Math.Clamp(pct, 0, 100));

                        // wbadmin çıktısında oluşturulan .wim yolunu yakala
                        if (line.Contains(".wim", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains(".vhd", StringComparison.OrdinalIgnoreCase))
                        {
                            outputPath ??= ExtractFilePath(line);
                        }
                    }
                }, cancellationToken);

                var errorTask = Task.Run(async () =>
                {
                    string errLine;
                    while ((errLine = await process.StandardError.ReadLineAsync().ConfigureAwait(false)) != null)
                        Log.Warning("[wbadmin stderr] {Line}", errLine);
                }, CancellationToken.None);

                // İptal edilirse process'i sonlandır
                await using var reg = cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(entireProcessTree: true);
                            Log.Warning("wbadmin process iptal nedeniyle sonlandırıldı. Kaynak: {Volume}", source.VolumePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "wbadmin process sonlandırılırken hata oluştu.");
                    }
                });

                await readTask.ConfigureAwait(false);
                await errorTask.ConfigureAwait(false);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (process.ExitCode != 0)
                {
                    result.Status = BackupResultStatus.Failed;
                    result.ErrorMessage = $"wbadmin çıkış kodu: {process.ExitCode}";
                    Log.Error("wbadmin başarısız oldu. ExitCode: {Code}, Kaynak: {Volume}", process.ExitCode, source.VolumePath);
                }
                else
                {
                    result.Status = BackupResultStatus.Success;
                    result.OutputPath = outputPath ?? destinationPath;

                    if (!string.IsNullOrEmpty(result.OutputPath) && File.Exists(result.OutputPath))
                        result.ImageSizeBytes = new FileInfo(result.OutputPath).Length;

                    progress?.Report(100);
                    Log.Information("Disk imajı başarıyla oluşturuldu. Kaynak: {Volume}, Dosya: {Path}, Boyut: {Size} MB",
                        source.VolumePath,
                        result.OutputPath,
                        result.ImageSizeBytes / 1024 / 1024);
                }
            }
            catch (OperationCanceledException)
            {
                result.Status = BackupResultStatus.Cancelled;
                result.ErrorMessage = "Kullanıcı tarafından iptal edildi.";
                Log.Information("Disk imajı yedekleme iptal edildi. Kaynak: {Volume}", source.VolumePath);
                throw;
            }
            catch (Exception ex)
            {
                result.Status = BackupResultStatus.Failed;
                result.ErrorMessage = ex.Message;
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
        /// wbadmin start backup argümanlarını oluşturur.
        /// </summary>
        private static string BuildWbAdminArguments(string volumePath, string destinationPath, DiskImageFormat format)
        {
            // Sürücü harfi normalize: "C:" veya "C:\" → "C:"
            string vol = volumePath.TrimEnd('\\', '/');

            // wbadmin için hedef UNC veya yerel disk olmalıdır.
            // Yerel yol: -backupTarget:D:\Backups\DiskImages
            // -include: yedeklenecek volume
            // -quiet: etkileşimsiz mod (SYSTEM servisi için zorunlu)
            return $"start backup -backupTarget:\"{destinationPath}\" -include:{vol} -quiet -vssFull";
        }

        /// <summary>
        /// wbadmin çıktı satırından dosya yolunu çıkarmaya çalışır.
        /// </summary>
        private static string ExtractFilePath(string line)
        {
            // Örnek: "  Yedekleme konumu: D:\Backups\DiskImages\WindowsImageBackup\..."
            int colonIdx = line.IndexOf(':', StringComparison.Ordinal);
            if (colonIdx < 0) return null;

            string candidate = line[(colonIdx + 1)..].Trim();
            // En az sürücü harfi + ':' + '\\' olmalı
            if (candidate.Length >= 3 && candidate[1] == ':')
                return candidate;

            return null;
        }
    }
}
