using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Retention
{
    /// <summary>
    /// Eski yedeklerin retention politikasına göre temizlenmesini yönetir.
    /// Bulut modda: buluta başarıyla gönderilmemiş dosyalar SİLİNMEZ.
    /// </summary>
    public class RetentionCleanupService : IRetentionService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<RetentionCleanupService>();

        private readonly IBackupHistoryManager _historyManager;

        public RetentionCleanupService(IBackupHistoryManager historyManager)
        {
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        }

        public async Task CleanupAsync(BackupPlan plan, CancellationToken cancellationToken)
        {
            if (plan == null || string.IsNullOrEmpty(plan.LocalPath))
                return;

            Log.Information("Retention temizliği başlıyor: Plan={PlanName}, BulutHedef={HasCloud}",
                plan.PlanName, plan.HasCloudTargets);

            // Aktif bulut hedefi varsa geçmiş kayıtlarını yükle — upload durumunu kontrol etmek için
            HashSet<string> cloudProtectedFiles = null;
            if (plan.HasCloudTargets)
            {
                cloudProtectedFiles = BuildCloudProtectedFileSet(plan);
            }

            await Task.Run(() =>
            {
                foreach (string dbName in plan.Databases)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    CleanupForDatabaseByType(plan.LocalPath, dbName, BackupFileType.SqlFull,
                        plan.GetEffectiveRetention(BackupFileType.SqlFull), cloudProtectedFiles);

                    CleanupForDatabaseByType(plan.LocalPath, dbName, BackupFileType.SqlDifferential,
                        plan.GetEffectiveRetention(BackupFileType.SqlDifferential), cloudProtectedFiles);

                    CleanupForDatabaseByType(plan.LocalPath, dbName, BackupFileType.SqlLog,
                        plan.GetEffectiveRetention(BackupFileType.SqlLog), cloudProtectedFiles);
                }

                // Dosya yedekleme arşivlerini de temizle (Files_*.7z)
                if (plan.FileBackup?.IsEnabled == true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CleanupFileBackupArchives(plan.LocalPath,
                        plan.GetEffectiveRetention(BackupFileType.FileBackup), cloudProtectedFiles);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Buluta gönderilmemiş dosyaların tam yol setini oluşturur.
        /// Bu dosyalar retention tarafından silinmeyecektir.
        /// </summary>
        private HashSet<string> BuildCloudProtectedFileSet(BackupPlan plan)
        {
            var protectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var history = _historyManager.GetHistoryByPlan(plan.PlanId, 500);

                foreach (var result in history)
                {
                    bool allUploadsSuccessful = result.CloudUploadResults != null
                        && result.CloudUploadResults.Count > 0
                        && result.CloudUploadResults.All(r => r.IsSuccess);

                    if (!allUploadsSuccessful)
                    {
                        // Bu dosya buluta tam gönderilmemiş — koru
                        if (!string.IsNullOrEmpty(result.BackupFilePath))
                            protectedFiles.Add(result.BackupFilePath);

                        if (!string.IsNullOrEmpty(result.CompressedFilePath))
                            protectedFiles.Add(result.CompressedFilePath);
                    }
                }

                if (protectedFiles.Count > 0)
                {
                    Log.Warning(
                        "Bulut koruma: {Count} dosya buluta gönderilemediği için silinmeyecek (Plan={PlanName})",
                        protectedFiles.Count, plan.PlanName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Bulut koruma kontrolü yapılamadı — güvenlik gereği hiçbir dosya silinmeyecek");
                // Güvenlik: geçmiş okunamazsa tüm dosyalar korunur
                protectedFiles.Add("*PROTECT_ALL*");
            }

            return protectedFiles;
        }

        private void CleanupForDatabaseByType(
            string localPath,
            string databaseName,
            BackupFileType fileType,
            RetentionPolicy retention,
            HashSet<string> cloudProtectedFiles)
        {
            if (retention == null || !Directory.Exists(localPath))
                return;

            // Dosya adındaki tipe göre filtre — her tip kendi havuzunda sayılır
            string typeFilter = fileType switch
            {
                BackupFileType.SqlDifferential => "_Differential_",
                BackupFileType.SqlLog => "_Log_",
                _ => "_Full_"
            };

            // .bak ve .7z dosyalarını topla, sadece ilgili tip
            var allFiles = Directory.GetFiles(localPath, $"{databaseName}_*.*")
                .Where(f => (f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                             f.EndsWith(".7z", StringComparison.OrdinalIgnoreCase)) &&
                            Path.GetFileName(f).Contains(typeFilter, StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            int deletedCount = 0;
            int skippedCount = 0;

            if (retention.Type == RetentionPolicyType.GFS)
            {
                var protectedByGfs = BuildGfsProtectedSet(allFiles, retention);

                foreach (var file in allFiles)
                {
                    if (!protectedByGfs.Contains(file.FullName))
                        TryDeleteFileWithCloudCheck(file, cloudProtectedFiles, ref deletedCount, ref skippedCount);
                }
            }
            else
            {
                if (retention.Type == RetentionPolicyType.KeepLastN ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    var toDeleteByCount = allFiles.Skip(retention.KeepLastN).ToList();
                    foreach (var file in toDeleteByCount)
                    {
                        TryDeleteFileWithCloudCheck(file, cloudProtectedFiles, ref deletedCount, ref skippedCount);
                    }
                }

                if (retention.Type == RetentionPolicyType.DeleteOlderThanDays ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    DateTime cutoff = DateTime.Now.AddDays(-retention.DeleteOlderThanDays);
                    var toDeleteByAge = allFiles
                        .Where(f => f.CreationTime < cutoff)
                        .ToList();

                    foreach (var file in toDeleteByAge)
                    {
                        TryDeleteFileWithCloudCheck(file, cloudProtectedFiles, ref deletedCount, ref skippedCount);
                    }
                }
            }

            if (deletedCount > 0 || skippedCount > 0)
            {
                Log.Information(
                    "Retention tamamlandı: {Database}/{FileType} — {Deleted} silindi, {Skipped} korundu (bulut bekliyor)",
                    databaseName, fileType, deletedCount, skippedCount);
            }
        }

        /// <summary>
        /// GFS (Grandfather-Father-Son) politikasına göre korunacak dosyaları belirler.
        /// Her periyot (gün/hafta/ay/yıl) için en yeni (en büyük) yedek seçilir.
        /// </summary>
        /// <remarks>Public for unit testing.</remarks>
        public static HashSet<string> BuildGfsProtectedSet(List<FileInfo> files, RetentionPolicy retention)
        {
            var protectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DateTime now = DateTime.Now;

            // Günlük: Son N günün her biri için en iyi yedeği koru
            if (retention.GfsKeepDaily > 0)
            {
                SelectBestPerPeriod(files, retention.GfsKeepDaily,
                    f => f.CreationTime.Date,
                    now.Date.AddDays(-retention.GfsKeepDaily + 1),
                    protectedFiles);
            }

            // Haftalık: Son N haftanın her biri için en iyi yedeği koru (ISO hafta başı: Pazartesi)
            if (retention.GfsKeepWeekly > 0)
            {
                SelectBestPerPeriod(files, retention.GfsKeepWeekly,
                    f => GetWeekStart(f.CreationTime),
                    GetWeekStart(now).AddDays(-7 * (retention.GfsKeepWeekly - 1)),
                    protectedFiles);
            }

            // Aylık: Son N ayın her biri için en iyi yedeği koru
            if (retention.GfsKeepMonthly > 0)
            {
                SelectBestPerPeriod(files, retention.GfsKeepMonthly,
                    f => new DateTime(f.CreationTime.Year, f.CreationTime.Month, 1),
                    new DateTime(now.Year, now.Month, 1).AddMonths(-retention.GfsKeepMonthly + 1),
                    protectedFiles);
            }

            // Yıllık: Son N yılın her biri için en iyi yedeği koru
            if (retention.GfsKeepYearly > 0)
            {
                SelectBestPerPeriod(files, retention.GfsKeepYearly,
                    f => new DateTime(f.CreationTime.Year, 1, 1),
                    new DateTime(now.Year - retention.GfsKeepYearly + 1, 1, 1),
                    protectedFiles);
            }

            return protectedFiles;
        }

        /// <summary>
        /// Belirtilen periyot fonksiyonuna göre her dilimden en büyük dosyayı seçer.
        /// </summary>
        private static void SelectBestPerPeriod(
            List<FileInfo> files,
            int keepCount,
            Func<FileInfo, DateTime> periodKeySelector,
            DateTime cutoff,
            HashSet<string> protectedFiles)
        {
            var eligible = files.Where(f => f.CreationTime >= cutoff);

            var bestPerPeriod = eligible
                .GroupBy(periodKeySelector)
                .OrderByDescending(g => g.Key)
                .Take(keepCount)
                .Select(g => g.OrderByDescending(f => f.Length).ThenByDescending(f => f.CreationTime).First());

            foreach (var file in bestPerPeriod)
            {
                protectedFiles.Add(file.FullName);
            }
        }

        /// <summary>
        /// ISO 8601 hafta başlangıcını (Pazartesi) döndürür.
        /// </summary>
        private static DateTime GetWeekStart(DateTime date)
        {
            int diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

            return date.Date.AddDays(-diff);
        }

        /// <summary>
        /// Dosya yedekleme arşivlerini (Files_*.7z) retention politikasına göre temizler.
        /// </summary>
        private void CleanupFileBackupArchives(
            string localPath,
            RetentionPolicy retention,
            HashSet<string> cloudProtectedFiles)
        {
            if (retention == null || !Directory.Exists(localPath))
                return;

            var allFiles = Directory.GetFiles(localPath, "Files_*.7z")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (allFiles.Count == 0)
                return;

            int deletedCount = 0;
            int skippedCount = 0;

            if (retention.Type == RetentionPolicyType.GFS)
            {
                var protectedByGfs = BuildGfsProtectedSet(allFiles, retention);

                foreach (var file in allFiles)
                {
                    if (!protectedByGfs.Contains(file.FullName))
                        TryDeleteFileWithCloudCheck(file, cloudProtectedFiles, ref deletedCount, ref skippedCount);
                }
            }
            else
            {
                if (retention.Type == RetentionPolicyType.KeepLastN ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    var toDeleteByCount = allFiles.Skip(retention.KeepLastN).ToList();
                    foreach (var file in toDeleteByCount)
                    {
                        TryDeleteFileWithCloudCheck(file, cloudProtectedFiles, ref deletedCount, ref skippedCount);
                    }
                }

                if (retention.Type == RetentionPolicyType.DeleteOlderThanDays ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    DateTime cutoff = DateTime.Now.AddDays(-retention.DeleteOlderThanDays);
                    var toDeleteByAge = allFiles
                        .Where(f => f.CreationTime < cutoff)
                        .ToList();

                    foreach (var file in toDeleteByAge)
                    {
                        TryDeleteFileWithCloudCheck(file, cloudProtectedFiles, ref deletedCount, ref skippedCount);
                    }
                }
            }

            if (deletedCount > 0 || skippedCount > 0)
            {
                Log.Information(
                    "Dosya arşiv retention tamamlandı: {Deleted} silindi, {Skipped} korundu (bulut bekliyor)",
                    deletedCount, skippedCount);
            }
        }

        private void TryDeleteFileWithCloudCheck(
            FileInfo file,
            HashSet<string> cloudProtectedFiles,
            ref int deletedCount,
            ref int skippedCount)
        {
            // Bulut koruma kontrolü
            if (cloudProtectedFiles != null)
            {
                // Geçmiş okunamazsa tüm dosyalar korunur
                if (cloudProtectedFiles.Contains("*PROTECT_ALL*"))
                {
                    skippedCount++;
                    Log.Warning(
                        "Retention atlandı (geçmiş okunamadı, güvenlik modu): {FileName}",
                        file.Name);
                    return;
                }

                if (cloudProtectedFiles.Contains(file.FullName))
                {
                    skippedCount++;
                    Log.Warning(
                        "Retention atlandı (buluta gönderilememiş): {FileName}",
                        file.Name);
                    return;
                }
            }

            TryDeleteFile(file, ref deletedCount);
        }

        private void TryDeleteFile(FileInfo file, ref int deletedCount)
        {
            try
            {
                if (file.Exists)
                {
                    file.Delete();
                    deletedCount++;
                    Log.Information("Eski yedek silindi: {FileName}", file.Name);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dosya silinemedi: {FileName}", file.Name);
            }
        }
    }
}
