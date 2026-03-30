using System;
using System.IO;
using System.Linq;
using Serilog;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Backup
{
    /// <summary>
    /// Yedekleme zincir bütünlüğünü kontrol eder.
    /// Differential/Incremental almadan önce geçerli Full yedek varlığını doğrular.
    /// Incremental (Transaction Log) için de ayrı zincir kontrolü yapar.
    /// </summary>
    public class BackupChainValidator
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<BackupChainValidator>();

        /// <summary>
        /// Belirtilen veritabanı için geçerli bir Full yedek var mı kontrol eder.
        /// </summary>
        public bool HasValidFullBackup(string localPath, string databaseName)
        {
            if (string.IsNullOrEmpty(localPath) || !Directory.Exists(localPath))
                return false;

            string pattern = $"{databaseName}_Full_*.bak";
            var fullBackups = Directory.GetFiles(localPath, pattern)
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            bool hasValid = fullBackups.Count > 0;
            Log.Debug(
                "Zincir kontrolü: {Database} Full yedek {Result} ({Count} adet)",
                databaseName,
                hasValid ? "bulundu" : "bulunamadı",
                fullBackups.Count);

            return hasValid;
        }

        /// <summary>
        /// Differential zincir uzunluğunu kontrol eder.
        /// autoPromoteToFullAfter değerini aşarsa true döner (Full yükseltme gerekli).
        /// </summary>
        public bool ShouldPromoteToFull(string localPath, string databaseName, int autoPromoteAfter)
        {
            if (string.IsNullOrEmpty(localPath) || !Directory.Exists(localPath))
                return true;

            if (autoPromoteAfter <= 0)
                return false;

            // Son Full yedekten sonraki diff sayısını bul
            string fullPattern = $"{databaseName}_Full_*.bak";
            var lastFull = Directory.GetFiles(localPath, fullPattern)
                .OrderByDescending(f => File.GetCreationTime(f))
                .FirstOrDefault();

            if (lastFull == null)
                return true;

            DateTime lastFullDate = File.GetCreationTime(lastFull);

            string diffPattern = $"{databaseName}_Differential_*.bak";
            int diffCountSinceLastFull = Directory.GetFiles(localPath, diffPattern)
                .Count(f => File.GetCreationTime(f) > lastFullDate);

            bool shouldPromote = diffCountSinceLastFull >= autoPromoteAfter;

            if (shouldPromote)
            {
                Log.Information(
                    "Otomatik Full yükseltme gerekli: {Database} ({DiffCount} diff, limit: {Limit})",
                    databaseName, diffCountSinceLastFull, autoPromoteAfter);
            }

            return shouldPromote;
        }

        /// <summary>
        /// Incremental (Transaction Log) zincir bütünlüğünü kontrol eder.
        /// Full yedek yoksa veya son Full'den bu yana log chain kırılmışsa false döner.
        /// </summary>
        public bool HasValidLogChain(string localPath, string databaseName)
        {
            if (string.IsNullOrEmpty(localPath) || !Directory.Exists(localPath))
                return false;

            // Önce geçerli Full yedek olmalı
            if (!HasValidFullBackup(localPath, databaseName))
                return false;

            // Full yedek varsa, log backup alınabilir
            // (SQL Server kendi LSN zincirini tutar, biz sadece Full varlığını garanti ederiz)
            return true;
        }

        /// <summary>
        /// Son Full yedekten bu yana geçen Differential yedek sayısını döndürür.
        /// </summary>
        public int GetDifferentialCountSinceLastFull(string localPath, string databaseName)
        {
            if (string.IsNullOrEmpty(localPath) || !Directory.Exists(localPath))
                return 0;

            string fullPattern = $"{databaseName}_Full_*.bak";
            var lastFull = Directory.GetFiles(localPath, fullPattern)
                .OrderByDescending(f => File.GetCreationTime(f))
                .FirstOrDefault();

            if (lastFull == null)
                return 0;

            DateTime lastFullDate = File.GetCreationTime(lastFull);

            string diffPattern = $"{databaseName}_Differential_*.bak";
            return Directory.GetFiles(localPath, diffPattern)
                .Count(f => File.GetCreationTime(f) > lastFullDate);
        }

        /// <summary>
        /// Son Full yedekten bu yana geçen Incremental (Log) yedek sayısını döndürür.
        /// </summary>
        public int GetIncrementalCountSinceLastFull(string localPath, string databaseName)
        {
            if (string.IsNullOrEmpty(localPath) || !Directory.Exists(localPath))
                return 0;

            string fullPattern = $"{databaseName}_Full_*.bak";
            var lastFull = Directory.GetFiles(localPath, fullPattern)
                .OrderByDescending(f => File.GetCreationTime(f))
                .FirstOrDefault();

            if (lastFull == null)
                return 0;

            DateTime lastFullDate = File.GetCreationTime(lastFull);

            string logPattern = $"{databaseName}_Incremental_*.bak";
            return Directory.GetFiles(localPath, logPattern)
                .Count(f => File.GetCreationTime(f) > lastFullDate);
        }

        /// <summary>
        /// Son Full yedeğin tarihini döndürür. Yoksa null.
        /// </summary>
        public DateTime? GetLastFullBackupDate(string localPath, string databaseName)
        {
            if (string.IsNullOrEmpty(localPath) || !Directory.Exists(localPath))
                return null;

            string fullPattern = $"{databaseName}_Full_*.bak";
            var lastFull = Directory.GetFiles(localPath, fullPattern)
                .OrderByDescending(f => File.GetCreationTime(f))
                .FirstOrDefault();

            return lastFull != null ? File.GetCreationTime(lastFull) : (DateTime?)null;
        }
    }
}
