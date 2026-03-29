using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SevenZip;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Compression
{
    /// <summary>
    /// SevenZipSharp tabanlı LZMA2 sıkıştırma servisi.
    /// Plan'daki CompressionConfig ayarlarına göre algoritma ve seviye eşlemesi yapar.
    /// </summary>
    public class SevenZipCompressionService : ICompressionService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<SevenZipCompressionService>();
        private static bool _initialized;
        private const double BytesPerMb = 1048576.0;

        /// <summary>
        /// 7z.dll yolunu ayarlar. Uygulama başlangıcında bir kez çağrılmalıdır.
        /// </summary>
        public static void Initialize(string sevenZipDllPath = null)
        {
            if (_initialized)
                return;

            if (string.IsNullOrEmpty(sevenZipDllPath))
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;

                // 1. Uygulama dizini/x64/7z.dll (build output)
                string candidate = Environment.Is64BitProcess
                    ? Path.Combine(appDir, "x64", "7z.dll")
                    : Path.Combine(appDir, "x86", "7z.dll");

                if (!File.Exists(candidate))
                {
                    // 2. Uygulama dizininde düz 7z.dll
                    candidate = Path.Combine(appDir, "7z.dll");
                }

                if (!File.Exists(candidate))
                {
                    // 3. Program Files — 7-Zip kurulumu
                    string programFiles = Environment.Is64BitProcess
                        ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                        : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    candidate = Path.Combine(programFiles, "7-Zip", "7z.dll");
                }

                if (!File.Exists(candidate))
                {
                    throw new FileNotFoundException(
                        "7z.dll bulunamadı. x64/7z.dll (build output) veya 7-Zip kurulumu gereklidir.",
                        candidate);
                }

                sevenZipDllPath = candidate;
            }

            SevenZipBase.SetLibraryPath(sevenZipDllPath);
            _initialized = true;
            Log.Information("SevenZip başlatıldı: {DllPath}", sevenZipDllPath);
        }

        public async Task<long> CompressAsync(
            string sourceFilePath,
            string destinationArchivePath,
            string password,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("Sıkıştırılacak dosya bulunamadı.", sourceFilePath);

            EnsureInitialized();

            Log.Information("Sıkıştırma başlıyor: {Source} → {Dest}", sourceFilePath, destinationArchivePath);

            await Task.Run(() =>
            {
                var compressor = CreateCompressor(Core.Models.CompressionLevel.Ultra);

                if (!string.IsNullOrEmpty(password))
                {
                    compressor.EncryptHeaders = true;
                }

                compressor.Compressing += (sender, e) =>
                {
                    progress?.Report(e.PercentDone);
                };

                string destDir = Path.GetDirectoryName(destinationArchivePath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                if (!string.IsNullOrEmpty(password))
                {
                    compressor.CompressFilesEncrypted(destinationArchivePath, password, sourceFilePath);
                }
                else
                {
                    compressor.CompressFiles(destinationArchivePath, sourceFilePath);
                }
            }, cancellationToken);

            var archiveInfo = new FileInfo(destinationArchivePath);
            var sourceInfo = new FileInfo(sourceFilePath);
            double ratio = sourceInfo.Length > 0
                ? (1.0 - (double)archiveInfo.Length / sourceInfo.Length) * 100
                : 0;

            Log.Information(
                "Sıkıştırma tamamlandı: {Archive} [{SizeMb:F1} MB, oran: %{Ratio:F1}]",
                destinationArchivePath,
                archiveInfo.Length / BytesPerMb,
                ratio);

            return archiveInfo.Length;
        }

        /// <summary>
        /// Birden fazla dosyayı tek arşive sıkıştırır.
        /// Dosya yedekleri için kullanılır (çoklu dosya → tek .7z).
        /// </summary>
        public async Task<long> CompressMultipleAsync(
            string[] sourceFilePaths,
            string destinationArchivePath,
            string password,
            Core.Models.CompressionLevel level,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            if (sourceFilePaths == null || sourceFilePaths.Length == 0)
                throw new ArgumentException("En az bir kaynak dosya gerekli.", nameof(sourceFilePaths));

            EnsureInitialized();

            string fileList = string.Join(", ", sourceFilePaths.Select(Path.GetFileName));
            Log.Information("Çoklu sıkıştırma başlıyor: {FileCount} dosya → {Dest}", sourceFilePaths.Length, destinationArchivePath);

            await Task.Run(() =>
            {
                var compressor = CreateCompressor(level);

                if (!string.IsNullOrEmpty(password))
                {
                    compressor.EncryptHeaders = true;
                }

                compressor.Compressing += (sender, e) =>
                {
                    progress?.Report(e.PercentDone);
                };

                string destDir = Path.GetDirectoryName(destinationArchivePath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                if (!string.IsNullOrEmpty(password))
                {
                    compressor.CompressFilesEncrypted(destinationArchivePath, password, sourceFilePaths);
                }
                else
                {
                    compressor.CompressFiles(destinationArchivePath, sourceFilePaths);
                }
            }, cancellationToken);

            var archiveInfo = new FileInfo(destinationArchivePath);

            Log.Information(
                "Çoklu sıkıştırma tamamlandı: {Archive} [{SizeMb:F1} MB]",
                destinationArchivePath,
                archiveInfo.Length / BytesPerMb);

            return archiveInfo.Length;
        }

        /// <summary>
        /// Dizini arşive sıkıştırır.
        /// Dosya yedekleri dizinini sıkıştırmak için kullanılır.
        /// </summary>
        public async Task<long> CompressDirectoryAsync(
            string sourceDirectory,
            string destinationArchivePath,
            string password,
            Core.Models.CompressionLevel level,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Kaynak dizin bulunamadı: {sourceDirectory}");

            EnsureInitialized();

            Log.Information("Dizin sıkıştırma başlıyor: {Source} → {Dest}", sourceDirectory, destinationArchivePath);

            await Task.Run(() =>
            {
                var compressor = CreateCompressor(level);
                compressor.DirectoryStructure = true;
                compressor.PreserveDirectoryRoot = false;

                if (!string.IsNullOrEmpty(password))
                {
                    compressor.EncryptHeaders = true;
                }

                compressor.Compressing += (sender, e) =>
                {
                    progress?.Report(e.PercentDone);
                };

                string destDir = Path.GetDirectoryName(destinationArchivePath);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                if (!string.IsNullOrEmpty(password))
                {
                    compressor.CompressDirectory(sourceDirectory, destinationArchivePath, password);
                }
                else
                {
                    compressor.CompressDirectory(sourceDirectory, destinationArchivePath);
                }
            }, cancellationToken);

            var archiveInfo = new FileInfo(destinationArchivePath);

            Log.Information(
                "Dizin sıkıştırma tamamlandı: {Archive} [{SizeMb:F1} MB]",
                destinationArchivePath,
                archiveInfo.Length / BytesPerMb);

            return archiveInfo.Length;
        }

        public async Task ExtractAsync(
            string archivePath,
            string destinationDirectory,
            string password,
            IProgress<int> progress,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException("Arşiv dosyası bulunamadı.", archivePath);

            EnsureInitialized();

            Log.Information("Açma başlıyor: {Archive} → {Dest}", archivePath, destinationDirectory);

            await Task.Run(() =>
            {
                Directory.CreateDirectory(destinationDirectory);

                using (var extractor = string.IsNullOrEmpty(password)
                    ? new SevenZipExtractor(archivePath)
                    : new SevenZipExtractor(archivePath, password))
                {
                    extractor.Extracting += (sender, e) =>
                    {
                        progress?.Report(e.PercentDone);
                    };

                    extractor.ExtractArchive(destinationDirectory);
                }
            }, cancellationToken);

            Log.Information("Açma tamamlandı: {Archive}", archivePath);
        }

        #region Helpers

        private SevenZipCompressor CreateCompressor(Core.Models.CompressionLevel level)
        {
            return new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                CompressionMethod = CompressionMethod.Lzma2,
                CompressionLevel = MapCompressionLevel(level),
                DirectoryStructure = false
            };
        }

        /// <summary>
        /// Core CompressionLevel enum → SevenZip CompressionLevel eşlemesi.
        /// </summary>
        private static SevenZip.CompressionLevel MapCompressionLevel(Core.Models.CompressionLevel level)
        {
            switch (level)
            {
                case Core.Models.CompressionLevel.None:
                    return SevenZip.CompressionLevel.None;
                case Core.Models.CompressionLevel.Fast:
                    return SevenZip.CompressionLevel.Fast;
                case Core.Models.CompressionLevel.Normal:
                    return SevenZip.CompressionLevel.Normal;
                case Core.Models.CompressionLevel.Maximum:
                    return SevenZip.CompressionLevel.High;
                case Core.Models.CompressionLevel.Ultra:
                    return SevenZip.CompressionLevel.Ultra;
                default:
                    return SevenZip.CompressionLevel.Normal;
            }
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                Initialize();
        }

        #endregion
    }
}
