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
        private readonly ICloudUploadOrchestrator _cloudOrchestrator;

        public RetentionCleanupService(
            IBackupHistoryManager historyManager,
            ICloudUploadOrchestrator cloudOrchestrator = null)
        {
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _cloudOrchestrator = cloudOrchestrator;
        }

        public async Task CleanupAsync(BackupPlan plan, CancellationToken cancellationToken)
        {
            if (plan == null || string.IsNullOrEmpty(plan.LocalPath))
                return;

            Log.Information("Retention temizliği başlıyor: Plan={PlanName}, BulutHedef={HasCloud}",
                plan.PlanName, plan.HasCloudTargets);

            // Geçmiş kayıtlarını yükle — bulut koruma + cloud fileId haritası için
            HashSet<string> cloudProtectedFiles = null;
            Dictionary<string, List<(string FileId, CloudTargetConfig Target)>> cloudFileMap = null;

            if (plan.HasCloudTargets)
            {
                cloudProtectedFiles = BuildCloudProtectedFileSet(plan);
                if (_cloudOrchestrator != null)
                    cloudFileMap = BuildCloudFileMap(plan);
            }

            // Silinecek dosyaları belirle (sync dosya taraması)
            var filesToDelete = new List<FileInfo>();

            await Task.Run(() =>
            {
                foreach (string dbName in plan.Databases)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    CollectFilesToDelete(plan.LocalPath, dbName, BackupFileType.SqlFull,
                        plan.GetEffectiveRetention(BackupFileType.SqlFull), cloudProtectedFiles, filesToDelete);

                    CollectFilesToDelete(plan.LocalPath, dbName, BackupFileType.SqlDifferential,
                        plan.GetEffectiveRetention(BackupFileType.SqlDifferential), cloudProtectedFiles, filesToDelete);

                    CollectFilesToDelete(plan.LocalPath, dbName, BackupFileType.SqlLog,
                        plan.GetEffectiveRetention(BackupFileType.SqlLog), cloudProtectedFiles, filesToDelete);

                    CollectFilesToDelete(plan.LocalPath, dbName, BackupFileType.SqlVss,
                        plan.GetEffectiveRetention(BackupFileType.SqlVss), cloudProtectedFiles, filesToDelete);
                }

                if (plan.FileBackup?.IsEnabled == true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CollectFileBackupArchivesToDelete(plan.LocalPath,
                        plan.GetEffectiveRetention(BackupFileType.FileBackup), cloudProtectedFiles, filesToDelete);
                }
            }, cancellationToken);

            // Silme işlemini yürüt: önce cloud, sonra local
            int deletedLocal = 0;
            int deletedCloud = 0;
            int skipped = 0;

            foreach (var file in filesToDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Cloud silme
                if (cloudFileMap != null &&
                    cloudFileMap.TryGetValue(file.FullName, out var cloudEntries))
                {
                    foreach (var (fileId, target) in cloudEntries)
                    {
                        try
                        {
                            var results = await _cloudOrchestrator.DeleteFromAllAsync(
                                fileId,
                                new List<CloudTargetConfig> { target },
                                cancellationToken).ConfigureAwait(false);

                            if (results?.Count > 0 && results[0].IsSuccess)
                            {
                                deletedCloud++;
                                Log.Information(
                                    "Cloud retention: {Provider} — silindi: {FileId} ({FileName})",
                                    target.DisplayName, fileId, file.Name);
                            }
                            else
                            {
                                Log.Warning(
                                    "Cloud retention: {Provider} — silinemedi: {FileId} ({FileName})",
                                    target.DisplayName, fileId, file.Name);
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            Log.Warning(ex,
                                "Cloud retention hatası: {Provider} — {FileId} ({FileName})",
                                target.DisplayName, fileId, file.Name);
                        }
                    }
                }
                else if (plan.HasCloudTargets && cloudFileMap != null)
                {
                    Log.Debug(
                        "Cloud retention: Geçmişte eşleşme bulunamadı, local silinecek: {FileName}",
                        file.Name);
                }

                // Local silme
                TryDeleteFile(file, ref deletedLocal);
            }

            if (deletedLocal > 0 || deletedCloud > 0 || skipped > 0)
            {
                Log.Information(
                    "Retention tamamlandı: Plan={PlanName} — Local={DeletedLocal} silindi, Cloud={DeletedCloud} silindi, {Skipped} korundu",
                    plan.PlanName, deletedLocal, deletedCloud, skipped);
            }
        }

        /// <summary>
        /// History'den local dosya yolu → cloud (fileId, target) listesi haritası oluşturur.
        /// Bir local dosyanın birden fazla cloud hedefine yüklenmiş olabileceği gözetilir.
        /// </summary>
        private Dictionary<string, List<(string FileId, CloudTargetConfig Target)>> BuildCloudFileMap(BackupPlan plan)
        {
            var map = new Dictionary<string, List<(string, CloudTargetConfig)>>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
                var history = _historyManager.GetHistoryByPlan(plan.PlanId, 500);

                foreach (var result in history)
                {
                    if (result.CloudUploadResults == null) continue;

                    // Bu backup'ın local dosya yolu (.7z varsa onu, yoksa .bak)
                    string localPath = result.CompressedFilePath ?? result.BackupFilePath;
                    if (string.IsNullOrEmpty(localPath)) continue;

                    foreach (var upload in result.CloudUploadResults)
                    {
                        if (!upload.IsSuccess || string.IsNullOrEmpty(upload.RemoteFilePath))
                            continue;

                        // Hangi CloudTargetConfig bu upload'a karşılık geliyor?
                        var matchedTarget = plan.CloudTargets?.FirstOrDefault(t =>
                            t.IsEnabled && t.Type == upload.ProviderType &&
                            t.DisplayName == upload.DisplayName);

                        if (matchedTarget == null) continue;

                        if (!map.TryGetValue(localPath, out var list))
                        {
                            list = new List<(string, CloudTargetConfig)>();
                            map[localPath] = list;
                        }

                        list.Add((upload.RemoteFilePath, matchedTarget));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Cloud dosya haritası oluşturulamadı — cloud retention atlanacak");
            }

            return map;
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

        private void CollectFilesToDelete(
            string localPath,
            string databaseName,
            BackupFileType fileType,
            RetentionPolicy retention,
            HashSet<string> cloudProtectedFiles,
            List<FileInfo> result)
        {
            if (retention == null || !Directory.Exists(localPath))
                return;

            string typeToken = fileType switch
            {
                BackupFileType.SqlDifferential => "Differential_",
                BackupFileType.SqlLog => "Log_",
                BackupFileType.SqlVss => "VSS_",
                _ => "Full_"
            };

            var allFiles = Directory.GetFiles(localPath, $"{databaseName}_{typeToken}*")
                .Where(f => f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            Log.Debug(
                "Retention tarama: {Database}/{FileType} — {Count} dosya bulundu (Pattern: {Pattern})",
                databaseName, fileType, allFiles.Count, $"{databaseName}_{typeToken}*");

            if (allFiles.Count == 0)
                return;

            CollectCandidates(allFiles, retention, cloudProtectedFiles, result);
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

        private void CollectFileBackupArchivesToDelete(
            string localPath,
            RetentionPolicy retention,
            HashSet<string> cloudProtectedFiles,
            List<FileInfo> result)
        {
            if (retention == null || !Directory.Exists(localPath))
                return;

            var allFiles = Directory.GetFiles(localPath, "Files_*.7z")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (allFiles.Count == 0)
                return;

            CollectCandidates(allFiles, retention, cloudProtectedFiles, result);
        }

        /// <summary>
        /// Retention politikasına göre silinmesi gereken dosyaları result listesine ekler.
        /// Bulut koruma kontrolü burada yapılır.
        /// </summary>
        private void CollectCandidates(
            List<FileInfo> allFiles,
            RetentionPolicy retention,
            HashSet<string> cloudProtectedFiles,
            List<FileInfo> result)
        {
            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (retention.Type == RetentionPolicyType.GFS)
            {
                var protectedByGfs = BuildGfsProtectedSet(allFiles, retention);
                foreach (var file in allFiles)
                {
                    if (!protectedByGfs.Contains(file.FullName))
                        candidates.Add(file.FullName);
                }
            }
            else
            {
                if (retention.Type == RetentionPolicyType.KeepLastN ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    foreach (var file in allFiles.Skip(retention.KeepLastN))
                        candidates.Add(file.FullName);
                }

                if (retention.Type == RetentionPolicyType.DeleteOlderThanDays ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    DateTime cutoff = DateTime.Now.AddDays(-retention.DeleteOlderThanDays);
                    foreach (var file in allFiles.Where(f => f.CreationTime < cutoff))
                        candidates.Add(file.FullName);
                }
            }

            foreach (var fullPath in candidates)
            {
                // Bulut koruma kontrolü
                if (cloudProtectedFiles != null)
                {
                    if (cloudProtectedFiles.Contains("*PROTECT_ALL*"))
                    {
                        Log.Warning("Retention atlandı (geçmiş okunamadı, güvenlik modu): {FileName}",
                            Path.GetFileName(fullPath));
                        continue;
                    }

                    if (cloudProtectedFiles.Contains(fullPath))
                    {
                        Log.Warning("Retention atlandı (buluta gönderilememiş): {FileName}",
                            Path.GetFileName(fullPath));
                        continue;
                    }
                }

                var fi = allFiles.First(f => f.FullName.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
                result.Add(fi);
            }
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
