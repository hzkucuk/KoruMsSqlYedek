using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.FileBackup
{
    // ── SQL Server veritabanı dosyalarının korunması ──────────────────
    public partial class FileBackupService
    {
        /// <summary>
        /// Uzantısından tanınan SQL Server dosyaları (sys.master_files okunamazsa da korunur).
        /// "._ms": Mikro birincil veri dosyası (sahada görüldü) — dosya-yalnızca planlarda ve
        /// SQL bağlantısı kurulamadığında da korunması için.
        /// </summary>
        private static readonly HashSet<string> SqlFileExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mdf", ".ndf", ".ldf", "._ms" };

        internal const string SqlFileSkippedMessage =
            "SQL Server veritabanı dosyası: anlık görüntü (VSS) alınamadığı için güvenli kopyalanamadı ve " +
            "atlandı. Açık veritabanı dosyası doğrudan kopyalanmaz; veritabanını SQL yedeği ile yedekleyin.";

        /// <summary>
        /// Dosya SQL Server'a ait mi? Uzantı (.mdf/.ndf/.ldf) veya sys.master_files listesi.
        /// Liste uzantıdan bağımsızdır: Mikro birincil veri dosyasını "._MS" uzantısıyla açar.
        /// </summary>
        internal static bool IsSqlDatabaseFile(string filePath, ISet<string> sqlDatabaseFiles)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (SqlFileExtensions.Contains(Path.GetExtension(filePath)))
                return true;

            return sqlDatabaseFiles != null
                && sqlDatabaseFiles.Count > 0
                && sqlDatabaseFiles.Contains(NormalizePath(filePath));
        }

        /// <summary>
        /// Plan SQL bağlantısı içeriyorsa SQL Server'ın dosya listesini okur ve —
        /// DisableAutoClose açıksa — dosyası yedek kaynaklarının altında kalan veritabanlarında
        /// AUTO_CLOSE'u kapatır. Hata fırlatmaz (iptal hariç); başarısızlıkta uzantı kontrolü kalır.
        /// </summary>
        private async Task<ISet<string>> PrepareSqlDatabaseFilesAsync(
            BackupPlan plan, List<FileBackupSource> sources, CancellationToken ct)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_sqlBackupService == null || string.IsNullOrWhiteSpace(plan.SqlConnection?.Server))
                return set;

            try
            {
                var files = await _sqlBackupService.GetDatabaseFilesAsync(plan.SqlConnection, ct)
                            ?? new List<SqlDatabaseFileInfo>();

                foreach (var f in files.Where(f => !string.IsNullOrWhiteSpace(f.PhysicalPath)))
                    set.Add(NormalizePath(f.PhysicalPath));

                if (plan.DisableAutoClose)
                {
                    var roots = sources.SelectMany(GetSourceRoots).ToList();
                    var databases = files
                        .Where(f => f.IsAutoCloseOn && IsUnderAnyRoot(f.PhysicalPath, roots))
                        .Select(f => f.DatabaseName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (databases.Count > 0)
                    {
                        var changed = await _sqlBackupService.DisableAutoCloseAsync(
                            plan.SqlConnection, databases, ct);
                        if (changed?.Count > 0)
                            Log.Information(
                                "Dosya yedeğine giren veritabanlarında AUTO_CLOSE kapatıldı: {Databases}",
                                string.Join(", ", changed));
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "SQL veritabanı dosyaları hazırlanamadı; yalnızca uzantı kontrolü uygulanacak.");
            }

            return set;
        }

        private static IEnumerable<string> GetSourceRoots(FileBackupSource source)
        {
            if (source.SelectedPaths?.Count > 0)
                return source.SelectedPaths;
            return string.IsNullOrWhiteSpace(source.SourcePath)
                ? Enumerable.Empty<string>()
                : new[] { source.SourcePath };
        }

        internal static bool IsUnderAnyRoot(string path, IEnumerable<string> roots)
        {
            string normalized = NormalizePath(path);
            foreach (string root in roots)
            {
                if (string.IsNullOrWhiteSpace(root))
                    continue;

                string r = NormalizePath(root).TrimEnd('\\');
                if (normalized.Equals(r, StringComparison.OrdinalIgnoreCase)
                    || normalized.StartsWith(r + "\\", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string NormalizePath(string path)
        {
            try { return Path.GetFullPath(path); }
            catch { return path; }
        }
    }
}
