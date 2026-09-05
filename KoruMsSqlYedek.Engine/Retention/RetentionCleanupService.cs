using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Engine.Retention
{
    /// <summary>
    /// Eski yedeklerin retention politikasına göre temizlenmesini yönetir.
    ///
    /// GÜVENLİK İLKELERİ:
    /// - Yalnızca BU PLANIN geçmiş kayıtlarında (history) yer alan dosyalar silinir.
    ///   Aynı dizini/prefix'i paylaşan başka bir planın, elle kopyalanmış veya bilinmeyen
    ///   dosyalar "sahipsiz" kabul edilir ve ASLA silinmez (Information log ile bildirilir).
    /// - Geçmiş okunamazsa hiçbir şey silinmez (fail-safe).
    /// - Bulut modda: buluta başarıyla gönderilmemiş dosyalar SİLİNMEZ.
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

            // Plan geçmişi TEK seferde ve ÜST SINIRSIZ okunur: sahiplik seti, bulut koruma seti
            // ve cloud fileId haritası hep bu listeden türetilir. Sınırlı (örn. 500) liste,
            // sık çalışan planlarda eski ama hâlâ diskte duran dosyaların korumasını düşürür.
            List<BackupResult> history;
            try
            {
                history = _historyManager.GetAllHistoryByPlan(plan.PlanId) ?? new List<BackupResult>();
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Retention: yedek geçmişi okunamadı — güvenlik gereği hiçbir dosya silinmeyecek (Plan={PlanName})",
                    plan.PlanName);
                return;
            }

            var ownedFiles = BuildOwnedFileSet(history);

            HashSet<string> cloudProtectedFiles = null;
            Dictionary<string, List<(string FileId, CloudTargetConfig Target)>> cloudFileMap = null;

            if (plan.HasCloudTargets)
            {
                cloudProtectedFiles = BuildCloudProtectedFileSet(plan, history);
                if (_cloudOrchestrator != null)
                    cloudFileMap = BuildCloudFileMap(plan, history);
            }

            // Silinecek dosyaları belirle (sync dosya taraması)
            var filesToDelete = new List<FileInfo>();
            var skippedUnknown = new List<string>();

            await Task.Run(() =>
            {
                foreach (string dbName in plan.Databases)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    CollectFilesToDelete(plan.LocalPath, dbName, BackupFileType.SqlFull,
                        plan.GetEffectiveRetention(BackupFileType.SqlFull), cloudProtectedFiles, ownedFiles, filesToDelete, skippedUnknown);

                    CollectFilesToDelete(plan.LocalPath, dbName, BackupFileType.SqlDifferential,
                        plan.GetEffectiveRetention(BackupFileType.SqlDifferential), cloudProtectedFiles, ownedFiles, filesToDelete, skippedUnknown);

                    CollectFilesToDelete(plan.LocalPath, dbName, BackupFileType.SqlLog,
                        plan.GetEffectiveRetention(BackupFileType.SqlLog), cloudProtectedFiles, ownedFiles, filesToDelete, skippedUnknown);

                    CollectFilesToDelete(plan.LocalPath, dbName, BackupFileType.SqlVss,
                        plan.GetEffectiveRetention(BackupFileType.SqlVss), cloudProtectedFiles, ownedFiles, filesToDelete, skippedUnknown);
                }

                if (plan.FileBackup?.IsEnabled == true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    CollectFileBackupArchivesToDelete(plan.LocalPath,
                        plan.GetEffectiveRetention(BackupFileType.FileBackup), cloudProtectedFiles, ownedFiles, filesToDelete, skippedUnknown);
                }
            }, cancellationToken);

            if (skippedUnknown.Count > 0)
            {
                // Çalıştırma başına tek satır: bu plana ait olmayan dosyalar
                Log.Information(
                    "Retention: {Count} dosya bu plana ait olmayan dosya olarak atlandı (geçmişte kaydı yok): {Files}",
                    skippedUnknown.Count, string.Join(", ", skippedUnknown.Take(20)) +
                    (skippedUnknown.Count > 20 ? ", …" : string.Empty));
            }

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
            // Bulut klasöründe duran ve BU PLANIN geçmişinde kaydı olan yedek dosyalarını
            // retention politikasına göre temizler. Geçmişte kaydı olmayan dosyalara dokunulmaz.
            if (plan.HasCloudTargets && _cloudOrchestrator != null)
            {
                await SweepCloudFoldersAsync(plan, history, cancellationToken).ConfigureAwait(false);
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
        /// Geçmişten bu planın ÜRETTİĞİ yerel dosyaların tam yol setini oluşturur.
        /// (.bak, .7z ve VSS arşivi — durum fark etmeksizin; başarısız kayıtların dosyası da bu plana aittir.)
        /// Retention yalnızca bu setteki dosyaları silebilir.
        /// </summary>
        private static HashSet<string> BuildOwnedFileSet(List<BackupResult> history)
        {
            var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in history)
            {
                AddNormalized(owned, result.BackupFilePath);
                AddNormalized(owned, result.CompressedFilePath);
                AddNormalized(owned, result.VssFileCopyPath);
            }

            return owned;
        }

        private static void AddNormalized(HashSet<string> set, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            set.Add(NormalizePath(path));
        }

        private static string NormalizePath(string path)
        {
            try { return Path.GetFullPath(path); }
            catch { return path; }
        }

        /// <summary>
        /// History'den local dosya yolu → cloud (fileId, target) listesi haritası oluşturur.
        /// Bir local dosyanın birden fazla cloud hedefine yüklenmiş olabileceği gözetilir.
        /// </summary>
        private Dictionary<string, List<(string FileId, CloudTargetConfig Target)>> BuildCloudFileMap(
            BackupPlan plan, List<BackupResult> history)
        {
            var map = new Dictionary<string, List<(string, CloudTargetConfig)>>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
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
        private HashSet<string> BuildCloudProtectedFileSet(BackupPlan plan, List<BackupResult> history)
        {
            var protectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
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
                // Güvenlik: geçmiş işlenemezse tüm dosyalar korunur
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
            HashSet<string> ownedFiles,
            List<FileInfo> result,
            List<string> skippedUnknown)
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

            // Dosya adı PathHelper.GenerateBackupFileName ile aynı temizlemeden geçer;
            // aksi halde üretilen dosya ile arama deseni eşleşmez.
            string pattern = $"{PathHelper.SanitizeFileNameComponent(databaseName)}_{typeToken}*";

            var allFiles = Directory.GetFiles(localPath, pattern)
                .Where(f => f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            Log.Debug(
                "Retention tarama: {Database}/{FileType} — {Count} dosya bulundu (Pattern: {Pattern})",
                databaseName, fileType, allFiles.Count, pattern);

            if (allFiles.Count == 0)
                return;

            CollectCandidates(allFiles, retention, cloudProtectedFiles, ownedFiles, result, skippedUnknown);
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
            HashSet<string> ownedFiles,
            List<FileInfo> result,
            List<string> skippedUnknown)
        {
            if (retention == null || !Directory.Exists(localPath))
                return;

            var allFiles = Directory.GetFiles(localPath, "Files_*.7z")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (allFiles.Count == 0)
                return;

            CollectCandidates(allFiles, retention, cloudProtectedFiles, ownedFiles, result, skippedUnknown);
        }

        /// <summary>
        /// Retention politikasına göre silinmesi gereken dosyaları result listesine ekler.
        /// Önce sahiplik filtresi (yalnızca bu planın geçmişindeki dosyalar), sonra politika,
        /// en son bulut koruma kontrolü uygulanır.
        /// </summary>
        private void CollectCandidates(
            List<FileInfo> allFiles,
            RetentionPolicy retention,
            HashSet<string> cloudProtectedFiles,
            HashSet<string> ownedFiles,
            List<FileInfo> result,
            List<string> skippedUnknown)
        {
            // ── Sahiplik filtresi ──
            // Bu plana ait olmayan dosyalar ne silinir ne de KeepLastN sayımına dahil edilir
            // (yabancı bir dosya, planın kendi yedeğinin yerini "işgal" etmemelidir).
            var owned = new List<FileInfo>(allFiles.Count);
            foreach (var file in allFiles)
            {
                if (ownedFiles != null && ownedFiles.Contains(NormalizePath(file.FullName)))
                    owned.Add(file);
                else
                    skippedUnknown.Add(file.Name);
            }

            if (owned.Count == 0)
                return;

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (retention.Type == RetentionPolicyType.GFS)
            {
                var protectedByGfs = BuildGfsProtectedSet(owned, retention);
                foreach (var file in owned)
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
                    // En az 1 yedek her zaman korunur (KeepLastN=0 "hepsini sil" anlamına gelmez)
                    int keep = retention.KeepLastN;
                    if (keep < 1)
                    {
                        Log.Warning("Retention: KeepLastN={Keep} geçersiz, 1 olarak uygulanıyor", keep);
                        keep = 1;
                    }

                    foreach (var file in owned.Skip(keep))
                        candidates.Add(file.FullName);
                }

                if (retention.Type == RetentionPolicyType.DeleteOlderThanDays ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    if (retention.DeleteOlderThanDays <= 0)
                    {
                        // "Şu an"dan eski = tüm dosyalar demek olurdu; yaş bazlı silme atlanır
                        Log.Warning(
                            "Retention: DeleteOlderThanDays={Days} geçersiz, yaş bazlı silme atlandı",
                            retention.DeleteOlderThanDays);
                    }
                    else
                    {
                        DateTime cutoff = DateTime.Now.AddDays(-retention.DeleteOlderThanDays);
                        foreach (var file in owned.Where(f => f.CreationTime < cutoff))
                            candidates.Add(file.FullName);
                    }
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

                var fi = owned.First(f => f.FullName.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
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
        // Bulut klasöründe duran ve BU PLANIN geçmişinde başarılı yükleme kaydı olan yedek
        // dosyalarını retention politikasına göre temizler.
        //
        // GÜVENLİK:
        // - YALNIZCA geçmişte (history) bu plana ait bir yükleme kaydı olan uzak dosyalar silinir.
        //   Eski davranış ("geçmişte kaydı yok → isim desenine uyuyorsa sil") KALDIRILDI:
        //   aynı bulut klasörünü ve aynı {db}_ prefix'ini paylaşan başka bir planın, başka bir
        //   cihazın veya elle yüklenmiş dosyaların, bu planın retention'ı tarafından yok edilmesine
        //   yol açıyordu. Sahipliği kanıtlanamayan bir dosyayı silmek veri kaybıdır; bu tür
        //   dosyalar kullanıcının elle temizlemesine bırakılır.
        // - Yalnızca plan'ın bilinen dosya isim desenine uyan dosyalara bakılır
        //   ({db}_Full_*, {db}_Differential_*, {db}_Log_*, {db}_VSS_*, Files_*).
        // - Yalnızca .bak veya .7z uzantılı dosyalar.
        // - Yalnızca config.RemoteFolderPath altındaki dosyalar (provider tarafından zorlanır).
        // - GFS modunda sadece yaş bazlı basit kural uygulanır; GFS karmaşık periyot seçimi
        //   uzak dosyalar için güvenli değil.

        private async Task SweepCloudFoldersAsync(BackupPlan plan, List<BackupResult> history, CancellationToken ct)
        {
            if (plan.CloudTargets is null || plan.CloudTargets.Count == 0)
            {
                Log.Information("Cloud folder sweep: Plan={PlanName} — bulut hedefi yok, atlanıyor.", plan.PlanName);
                return;
            }

            Log.Information("Cloud folder sweep başlıyor: Plan={PlanName} — {Count} hedef",
                plan.PlanName, plan.CloudTargets.Count);

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
                    Log.Information("Cloud folder sweep: {Provider} listeleme desteklemiyor (atlandı).",
                        target.DisplayName);
                    continue;
                }

                Log.Information(
                    "Cloud folder sweep: {Provider} klasöründe {Count} dosya bulundu (RemoteFolderPath={Path})",
                    target.DisplayName, remoteFiles.Count, string.IsNullOrEmpty(target.RemoteFolderPath) ? "(boş)" : target.RemoteFolderPath);

                if (remoteFiles.Count == 0)
                    continue;

                // Bu hedefe bu planın başarıyla yüklediği dosyalar (fileId + dosya adı)
                var known = BuildKnownRemoteSet(history, target);

                var unknownRemote = remoteFiles
                    .Where(e => !IsKnownRemote(e, known))
                    .Select(e => e.Name)
                    .ToList();

                if (unknownRemote.Count > 0)
                {
                    Log.Information(
                        "Cloud folder sweep: {Provider} — {Count} uzak dosya bu plana ait olmayan dosya olarak atlandı (geçmişte kaydı yok): {Files}",
                        target.DisplayName, unknownRemote.Count,
                        string.Join(", ", unknownRemote.Take(20)) + (unknownRemote.Count > 20 ? ", …" : string.Empty));
                }

                var ownedRemote = remoteFiles.Where(e => IsKnownRemote(e, known)).ToList();
                if (ownedRemote.Count == 0)
                    continue;

                int deleted = await SweepFilesForTargetAsync(plan, target, ownedRemote, ct)
                    .ConfigureAwait(false);

                Log.Information(
                    "Cloud folder sweep tamamlandı: {Provider} — {Count} eski dosya silindi (Plan={PlanName})",
                    target.DisplayName, deleted, plan.PlanName);
            }
        }

        /// <summary>
        /// Geçmişten, belirtilen hedefe bu planın başarıyla yüklediği dosyaların
        /// (RemoteFilePath/fileId ve yerel dosya adı) setini oluşturur.
        /// </summary>
        private static (HashSet<string> Ids, HashSet<string> Names) BuildKnownRemoteSet(
            List<BackupResult> history, CloudTargetConfig target)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in history)
            {
                if (result.CloudUploadResults == null) continue;

                foreach (var upload in result.CloudUploadResults)
                {
                    if (!upload.IsSuccess) continue;
                    if (upload.ProviderType != target.Type || upload.DisplayName != target.DisplayName) continue;

                    if (!string.IsNullOrEmpty(upload.RemoteFilePath))
                    {
                        ids.Add(upload.RemoteFilePath);
                        names.Add(Path.GetFileName(upload.RemoteFilePath));
                    }

                    // Yükleme, yerel dosya adıyla yapılır; uzak listede bu adla görünür
                    string localPath = result.CompressedFilePath ?? result.BackupFilePath;
                    if (!string.IsNullOrEmpty(localPath))
                        names.Add(Path.GetFileName(localPath));
                    if (!string.IsNullOrEmpty(result.VssFileCopyPath))
                        names.Add(Path.GetFileName(result.VssFileCopyPath));
                }
            }

            return (ids, names);
        }

        private static bool IsKnownRemote(CloudFileEntry entry, (HashSet<string> Ids, HashSet<string> Names) known)
        {
            if (entry == null) return false;
            if (!string.IsNullOrEmpty(entry.FileId) && known.Ids.Contains(entry.FileId)) return true;
            if (!string.IsNullOrEmpty(entry.Name) && known.Names.Contains(entry.Name)) return true;
            return false;
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
                string db = PathHelper.SanitizeFileNameComponent(dbName);

                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, $"{db}_Full_"),
                    plan.GetEffectiveRetention(BackupFileType.SqlFull), ct).ConfigureAwait(false);

                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, $"{db}_Differential_"),
                    plan.GetEffectiveRetention(BackupFileType.SqlDifferential), ct).ConfigureAwait(false);

                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, $"{db}_Log_"),
                    plan.GetEffectiveRetention(BackupFileType.SqlLog), ct).ConfigureAwait(false);

                totalDeleted += await SweepGroupAsync(target, remoteFiles,
                    MatchByPattern(remoteFiles, $"{db}_VSS_"),
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

            Log.Information(
                "Cloud folder sweep grubu: {Provider} — {Count} aday dosya, Policy={Policy} (KeepLastN={Keep}, OlderDays={Days})",
                target.DisplayName, group.Count, retention.Type, retention.KeepLastN, retention.DeleteOlderThanDays);

            // En yeni dosya başta
            var ordered = group.OrderByDescending(e => e.CreatedAtUtc).ToList();

            var toDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // GFS uzak dosyalar için güvenli değil (boyut/hash bilgisi sınırlı, periyot seçimi yanılabilir)
            // → GFS de yaş bazlı basit kural uygular.
            switch (retention.Type)
            {
                case RetentionPolicyType.KeepLastN:
                    foreach (var e in ordered.Skip(Math.Max(1, retention.KeepLastN)))
                        toDelete.Add(e.FileId);
                    break;

                case RetentionPolicyType.DeleteOlderThanDays:
                    AddOlderThan(ordered, retention.DeleteOlderThanDays, toDelete);
                    break;

                case RetentionPolicyType.Both:
                    foreach (var e in ordered.Skip(Math.Max(1, retention.KeepLastN)))
                        toDelete.Add(e.FileId);
                    AddOlderThan(ordered, retention.DeleteOlderThanDays, toDelete);
                    break;

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

        /// <summary>
        /// Yaş bazlı uzak silme adaylarını ekler. DeleteOlderThanDays &lt;= 0 ise "şu an"dan eski
        /// (= hepsi) anlamına geleceğinden atlanır.
        /// </summary>
        private static void AddOlderThan(List<CloudFileEntry> ordered, int days, HashSet<string> toDelete)
        {
            if (days <= 0)
            {
                Log.Warning("Cloud folder sweep: DeleteOlderThanDays={Days} geçersiz, yaş bazlı silme atlandı", days);
                return;
            }

            DateTime cutoff = DateTime.UtcNow.AddDays(-days);
            foreach (var e in ordered.Where(x => x.CreatedAtUtc < cutoff))
                toDelete.Add(e.FileId);
        }
    }
}
