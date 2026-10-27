using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Backup
{
    /// <summary>
    /// Yedekleme zincir bütünlüğünü kontrol eder.
    /// Differential/Incremental almadan önce geçerli Full yedek varlığını doğrular.
    /// Incremental (Transaction Log) için de ayrı zincir kontrolü yapar.
    /// Yedeğin zamanı dosya adındaki damgadan okunur (retention ile aynı ölçü; kopyalanan
    /// dosyanın oluşturma tarihi yanıltmaz). Aynı gövdeli .bak ve .7z tek yedektir.
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

            int count = GetBackupTimes(localPath, databaseName, BackupFileName.Full).Count;
            bool hasValid = count > 0;
            Log.Debug(
                "Zincir kontrolü: {Database} Full yedek {Result} ({Count} adet)",
                databaseName,
                hasValid ? "bulundu" : "bulunamadı",
                count);

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
            DateTime? lastFullDate = GetLastFullBackupDate(localPath, databaseName);
            if (lastFullDate == null)
                return true;

            int diffCountSinceLastFull = CountSince(localPath, databaseName, BackupFileName.Differential, lastFullDate.Value);

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

            DateTime? lastFullDate = GetLastFullBackupDate(localPath, databaseName);
            if (lastFullDate == null)
                return 0;

            return CountSince(localPath, databaseName, BackupFileName.Differential, lastFullDate.Value);
        }

        /// <summary>
        /// Son Full yedekten bu yana geçen Incremental (Log) yedek sayısını döndürür.
        /// </summary>
        public int GetIncrementalCountSinceLastFull(string localPath, string databaseName)
        {
            if (string.IsNullOrEmpty(localPath) || !Directory.Exists(localPath))
                return 0;

            DateTime? lastFullDate = GetLastFullBackupDate(localPath, databaseName);
            if (lastFullDate == null)
                return 0;

            return CountSince(localPath, databaseName, BackupFileName.Incremental, lastFullDate.Value);
        }

        /// <summary>
        /// Son Full yedeğin tarihini döndürür. Yoksa null.
        /// </summary>
        public DateTime? GetLastFullBackupDate(string localPath, string databaseName)
        {
            if (string.IsNullOrEmpty(localPath) || !Directory.Exists(localPath))
                return null;

            var times = GetBackupTimes(localPath, databaseName, BackupFileName.Full);
            return times.Count > 0 ? times.Max() : (DateTime?)null;
        }

        private static int CountSince(string localPath, string databaseName, string backupType, DateTime since)
            => GetBackupTimes(localPath, databaseName, backupType).Count(t => t > since);

        /// <summary>
        /// "{db}_{Tür}_yyyyMMdd_HHmmss" yedeklerinin zamanları: .bak VE .7z (sıkıştırma açıkken
        /// ara .bak silinir, yalnızca .7z kalır). Aynı gövdeli .bak + .7z bir kez sayılır.
        /// </summary>
        private static List<DateTime> GetBackupTimes(string localPath, string databaseName, string backupType)
        {
            if (string.IsNullOrEmpty(databaseName))
                return new List<DateTime>();

            var matcher = BackupFileName.For(databaseName, backupType);
            var byStem = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            foreach (string path in Directory.GetFiles(localPath))
            {
                string name = Path.GetFileName(path);
                if (matcher.TryMatch(name, out DateTime ts))
                    byStem[Path.GetFileNameWithoutExtension(name)] = ts;
            }

            return byStem.Values.ToList();
        }
    }
}
