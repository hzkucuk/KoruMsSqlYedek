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

            // Cloud orphan temizliği:
            // Yerel dosyası zaten silinmiş ama cloud'da hâlâ duran kayıtları da temizle.
            // History'deki tüm başarılı cloud upload'ları retention'a tabi tut;
            // local dosyası artık mevcut değilse (veya zaten sil listesindeyse) cloud'dan da sil.
            if (plan.HasCloudTargets && _cloudOrchestrator != null && cloudFileMap != null)
            {
                await CleanupOrphanCloudEntriesAsync(plan, cloudFileMap, filesToDelete, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Cloud folder sweep:
            // History'de hiç kaydı olmayan ama bulut klasöründe duran ESKİ yedek dosyalarını
            // (önceki cihazdan, yeniden kurulumdan vs.) retention politikasına göre temizler.
            // Yalnızca plan'ın bilinen dosya isim desenine uyan dosyalara dokunur.
            if (plan.HasCloudTargets && _cloudOrchestrator != null)
            {
                await SweepCloudFoldersAsync(plan, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Yerel dosyası silinmiş ama cloud'da hâlâ duran kayıtları retention'a göre temizler.
        /// cloudFileMap'teki her local yol için: dosya diskte yoksa VE bu run'da sil listesinde de
        /// yoksa (yani önceki bir run'da silinmiş demek), cloud kaydını da siler.
        /// </summary>
        private async Task CleanupOrphanCloudEntriesAsync(
            BackupPlan plan,
            Dictionary<string, List<(string FileId, CloudTargetConfig Target)>> cloudFileMap,
            List<FileInfo> alreadyDeletedThisRun,
            CancellationToken ct)
        {
            // Bu run'da local olarak silinen dosyalar — bunlar yukarıdaki döngüde zaten cloud'dan silindi
            var alreadyHandled = new HashSet<string>(
                alreadyDeletedThisRun.Select(f => f.FullName),
                StringComparer.OrdinalIgnoreCase);

            int orphansDeleted = 0;

            foreach (var kvp in cloudFileMap)
            {
                ct.ThrowIfCancellationRequested();

                string localPath = kvp.Key;

                // Dosya bu run'da zaten işlendi — atla
                if (alreadyHandled.Contains(localPath))
                    continue;

                // Dosya hâlâ diskte var — retention henüz silmemişse dokunma
                if (File.Exists(localPath))
                    continue;

                // Local dosya yok ama cloud'da kaydı var → orphan, sil
                Log.Information(
                    "Cloud orphan temizliği: Local dosya yok, cloud'dan siliniyor: {FileName}",
                    Path.GetFileName(localPath));

                foreach (var (fileId, target) in kvp.Value)
                {
                    try
                    {
                        var results = await _cloudOrchestrator.DeleteFromAllAsync(
                            fileId,
                            new List<CloudTargetConfig> { target },
                            ct).ConfigureAwait(false);

                        if (results?.Count > 0 && results[0].IsSuccess)
                        {
                            orphansDeleted++;
                            Log.Information(
                                "Cloud orphan silindi: {Provider} — {FileId} ({FileName})",
                                target.DisplayName, fileId, Path.GetFileName(localPath));
                        }
                        else
                        {
                            Log.Warning(
                                "Cloud orphan silinemedi: {Provider} — {FileId} ({FileName})",
                                target.DisplayName, fileId, Path.GetFileName(localPath));
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        Log.Warning(ex,
                            "Cloud orphan silme hatası: {Provider} — {FileId} ({FileName})",
                            target.DisplayName, fileId, Path.GetFileName(localPath));
                    }
                }
            }

            if (orphansDeleted > 0)
                Log.Information(
                    "Cloud orphan temizliği tamamlandı: Plan={PlanName} — {Count} cloud kaydı silindi",
                    plan.PlanName, orphansDeleted);
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

        // ── Cloud Folder Sweep ────────────────────────────────────────────
        // History'de hiç kaydı olmayan ama bulut klasöründe duran ESKİ yedek dosyalarını
        // (önceki cihazdan, yeniden kurulumdan vs. kalmış) retention politikasına göre temizler.
        //
        // GÜVENLİK:
        // - Yalnızca plan'ın bilinen dosya isim desenine uyan dosyalara dokunur
        //   ({db}_Full_*, {db}_Differential_*, {db}_Log_*, {db}_VSS_*, Files_*).
        // - Yalnızca .bak veya .7z uzantılı dosyalar.
        // - Yalnızca config.RemoteFolderPath altındaki dosyalar (provider tarafından zorlanır).
        // - "Sadece dosya yaşına göre" karar verir; yerel disk varlığına bakmaz (zaten history yok).
        // - GFS modunda sadece "DeleteOlderThanDays" (varsa) veya 90 gün varsayılanı uygulanır;
        //   GFS karmaşık periyot seçimi orphan/uzak dosyalar için güvenli değil.

        private async Task SweepCloudFoldersAsync(BackupPlan plan, CancellationToken ct)
        {
            if (plan.CloudTargets is null || plan.CloudTargets.Count == 0)
                return;

            foreach (var target in plan.CloudTargets)
            {
                if (target is null || !target.IsEnabled)
                    continue;

                ct.ThrowIfCancellationRequested();

                List<CloudFileEntry> remoteFiles;
                try
                {
                    remoteFiles = await _cloudOrchestrator.ListFolderAsync(target, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Cloud folder sweep: Listeleme başarısız ({Provider})", target.DisplayName);
                    continue;
                }

                if (remoteFiles is null)
                {
                    // Provider listeleme desteklemiyor (FTP/SFTP/Local) — atla
                    Log.Debug("Cloud folder sweep: {Provider} listeleme desteklemiyor, atlanıyor.",
                        target.DisplayName);
                    continue;
                }

                if (remoteFiles.Count == 0)
                    continue;

                int deleted = await SweepFilesForTargetAsync(plan, target, remoteFiles, ct)
                    .ConfigureAwait(false);

                if (deleted > 0)
                {
                    Log.Information(
                        "Cloud folder sweep tamamlandı: {Provider} — {Count} eski dosya silindi (Plan={PlanName})",
                        target.DisplayName, deleted, plan.PlanName);
                }
            }
        }

        private async Task<int> SweepFilesForTargetAsync(
            BackupPlan plan,
            CloudTargetConfig target,
            List<CloudFileEntry> remoteFiles,
            CancellationToken ct)
        {
            int totalDeleted = 0;

            // Her veritabanı + dosya türü için ayrı kümele
            foreach (string dbName in plan.Databases ?? new List<string>())
            {
                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, $"{dbName}_Full_"),
                    plan.GetEffectiveRetention(BackupFileType.SqlFull), ct).ConfigureAwait(false);

                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, $"{dbName}_Differential_"),
                    plan.GetEffectiveRetention(BackupFileType.SqlDifferential), ct).ConfigureAwait(false);

                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, $"{dbName}_Log_"),
                    plan.GetEffectiveRetention(BackupFileType.SqlLog), ct).ConfigureAwait(false);

                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, $"{dbName}_VSS_"),
                    plan.GetEffectiveRetention(BackupFileType.SqlVss), ct).ConfigureAwait(false);
            }

            if (plan.FileBackup?.IsEnabled == true)
            {
                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, "Files_"),
                    plan.GetEffectiveRetention(BackupFileType.FileBackup), ct).ConfigureAwait(false);
            }

            return totalDeleted;
        }

        private static List<CloudFileEntry> MatchByPattern(
            List<CloudFileEntry> all, string namePrefix)
        {
            // Plan dosya isim deseni: "{db}_{Type}_yyyyMMdd_HHmmss.bak" veya ".7z"
            return all.Where(e =>
                    !string.IsNullOrEmpty(e.Name) &&
                    e.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase) &&
                    (e.Name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                     e.Name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private async Task<int> SweepGroupAsync(
            CloudTargetConfig target,
            List<CloudFileEntry> allRemoteFiles,
            List<CloudFileEntry> group,
            RetentionPolicy retention,
            CancellationToken ct)
        {
            if (retention is null || group.Count == 0)
                return 0;

            // En yeni dosya başta
            var ordered = group.OrderByDescending(e => e.CreatedAtUtc).ToList();

            var toDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // GFS uzak dosyalar için güvenli değil (boyut/hash bilgisi sınırlı, periyot seçimi yanılabilir)
            // → GFS de "DeleteOlderThanDays varsa onu, yoksa KeepLastN'i" uygula.
            switch (retention.Type)
            {
                case RetentionPolicyType.KeepLastN:
                    foreach (var e in ordered.Skip(Math.Max(1, retention.KeepLastN)))
                        toDelete.Add(e.FileId);
                    break;

                case RetentionPolicyType.DeleteOlderThanDays:
                    {
                        DateTime cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, retention.DeleteOlderThanDays));
                        foreach (var e in ordered.Where(x => x.CreatedAtUtc < cutoff))
                            toDelete.Add(e.FileId);
                        break;
                    }

                case RetentionPolicyType.Both:
                    {
                        foreach (var e in ordered.Skip(Math.Max(1, retention.KeepLastN)))
                            toDelete.Add(e.FileId);

                        DateTime cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, retention.DeleteOlderThanDays));
                        foreach (var e in ordered.Where(x => x.CreatedAtUtc < cutoff))
                            toDelete.Add(e.FileId);
                        break;
                    }

                case RetentionPolicyType.GFS:
                    {
                        // Yıl bazlı en uzak periyodu kullan; en azından son 1 günlük dosyayı koru
                        int days = retention.GfsKeepDaily > 0 ? retention.GfsKeepDaily : 7;
                        // Aylık varsa aylık günü baz al (12 ay ≈ 365 gün)
                        if (retention.GfsKeepMonthly > 0)
                            days = Math.Max(days, retention.GfsKeepMonthly * 31);
                        if (retention.GfsKeepYearly > 0)
                            days = Math.Max(days, retention.GfsKeepYearly * 366);

                        DateTime cutoff = DateTime.UtcNow.AddDays(-days);
                        foreach (var e in ordered.Where(x => x.CreatedAtUtc < cutoff))
                            toDelete.Add(e.FileId);
                        break;
                    }
            }

            int deleted = 0;
            foreach (string fileId in toDelete)
            {
                ct.ThrowIfCancellationRequested();

                var entry = allRemoteFiles.FirstOrDefault(e => e.FileId == fileId);
                string fileName = entry?.Name ?? fileId;

                try
                {
                    bool ok = await _cloudOrchestrator.DeleteFromTargetAsync(fileId, target, ct)
                        .ConfigureAwait(false);

                    if (ok)
                    {
                        deleted++;
                        Log.Information(
                            "Cloud folder sweep: {Provider} — eski dosya silindi: {FileName} (oluşturma: {Created:yyyy-MM-dd})",
                            target.DisplayName, fileName, entry?.CreatedAtUtc ?? DateTime.MinValue);
                    }
                    else
                    {
                        Log.Warning(
                            "Cloud folder sweep: {Provider} — silme başarısız: {FileName}",
                            target.DisplayName, fileName);
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Log.Warning(ex,
                        "Cloud folder sweep hatası: {Provider} — {FileName}",
                        target.DisplayName, fileName);
                }
            }

            return deleted;
        }
    }
}
