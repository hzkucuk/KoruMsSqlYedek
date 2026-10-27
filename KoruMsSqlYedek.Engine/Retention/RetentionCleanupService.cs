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
    /// SAHİPLİK:
    /// - Planın yerel dizinindeki ve her bulut hedefinin klasöründeki, planın veritabanı adlarıyla
    ///   tam ad desenine uyan dosyalar ({db}_{Tür}_yyyyMMdd_HHmmss.bak|.7z, Files_yyyyMMdd_HHmmss.7z;
    ///   bkz. <see cref="BackupFileName"/>) bu plana ait sayılır. Geçmiş kaydı aranmaz: veri taşımada
    ///   ya da eski sürümlerde geçmişi kaybolmuş yedekler de plan kuralına göre temizlenir.
    /// - Bu cihazdaki BAŞKA bir planın geçmişinde geçen dosyalar asla silinmez.
    /// - Bulut silme yalnızca hedefin kendi klasör listesinden yapılır; yerel dosyanın yokluğu
    ///   tek başına bir bulut kopyasını sildirmez.
    ///
    /// GÜVENLİK:
    /// - Her grubun en yeni yedeği her durumda kalır.
    /// - Fark/log yedekleri zincire bağlıdır: en yeni tam yedekten sonrakiler hiç silinmez,
    ///   tam yedek yoksa hiçbiri silinmez.
    /// - Bulut hedefi olan planda buluta gönderilemeyen yerel dosya silinmez.
    /// - Geçmiş okunamazsa hiçbir şey silinmez (fail-safe).
    /// </summary>
    public class RetentionCleanupService : IRetentionService
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<RetentionCleanupService>();

        private const string ProtectAllMarker = "*PROTECT_ALL*";

        private const string FileBackupDatabaseName = "__FileBackup__";

        // ── Toplu temizlik emniyeti ──
        // v0.99.100 ile biriken eski yedekler ilk çalıştırmada temizlenir ve sürüm otomatik
        // güncellemeyle tüm kurulumlara aynı anda gider. Beklenmeyen bir durumda zarar sınırlı
        // ve geri alınabilir kalsın diye:
        // - Bir çalıştırmada yerelde ve her bulut hedefinde en fazla MaxDeletionsPerRun dosya
        //   silinir (en eskiler önce); birikmiş yedekler birkaç çalıştırmada erir. Günlük
        //   işleyişte çalıştırma başına 1-2 silme olduğundan sınır devreye girmez.
        // - Güvenli dönemde (ilk SafeModeDuration) Google Drive silmeleri, hedefte çöp kutusu
        //   kapalı olsa bile çöpe gönderilir (Drive çöpü 30 günde kendiliğinden boşaltır).

        /// <summary>Bir çalıştırmada yerelde ve her bulut hedefinde silinebilecek en fazla dosya.</summary>
        internal const int MaxDeletionsPerRun = 20;

        /// <summary>Drive silmelerinin çöpe gönderildiği güvenli dönem.</summary>
        internal static readonly TimeSpan SafeModeDuration = TimeSpan.FromDays(14);

        /// <summary>
        /// Güvenli dönemin başladığı anı (ilk çalıştırma, UTC) tutan dosya. Testler geçici bir
        /// yol verir.
        /// </summary>
        internal string SafeModeMarkerPath { get; set; } =
            Path.Combine(PathHelper.ConfigDirectory, "retention-guvenli-donem.txt");

        private readonly IBackupHistoryManager _historyManager;
        private readonly ICloudUploadOrchestrator _cloudOrchestrator;
        private readonly IPlanManager _planManager;

        /// <param name="planManager">
        /// Varsa yalnızca hâlâ var olan planların geçmişi "başka plan" sayılır; silinmiş ya da
        /// yeniden oluşturulmuş planın eski yedekleri temizlenebilir. Yoksa her başka PlanId korunur.
        /// </param>
        public RetentionCleanupService(
            IBackupHistoryManager historyManager,
            ICloudUploadOrchestrator cloudOrchestrator = null,
            IPlanManager planManager = null)
        {
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _cloudOrchestrator = cloudOrchestrator;
            _planManager = planManager;
        }

        /// <summary>Bir veritabanının bir yedek türü ya da dosya yedeği arşivleri.</summary>
        private sealed class RetentionGroup
        {
            private readonly BackupFileName _matcher;

            public RetentionGroup(string database, string backupType, BackupFileType fileType)
            {
                Database = database;
                BackupType = backupType;
                FileType = fileType;
                _matcher = database == null
                    ? BackupFileName.ForFileArchives()
                    : BackupFileName.For(database, backupType);
            }

            /// <summary>null: dosya yedeği arşivleri (Files_*).</summary>
            public string Database { get; }
            public string BackupType { get; }
            public BackupFileType FileType { get; }

            public bool IsChained =>
                BackupType == BackupFileName.Differential || BackupType == BackupFileName.Incremental;

            public bool TryMatch(string fileName, out DateTime timestamp) =>
                _matcher.TryMatch(fileName, out timestamp);

            public override string ToString() => Database == null ? "Files" : $"{Database}/{BackupType}";
        }

        /// <summary>
        /// Silme kararının verildiği birim. Bulutta aynı adı taşıyan birden çok dosya tek öğedir:
        /// ya hepsi kalır ya hepsi silinir (aynı adlı kopyalar KeepLastN sayımını şişirmez).
        /// </summary>
        private sealed class RetentionItem
        {
            public string Name { get; set; }
            public DateTime Time { get; set; }
            public long Size { get; set; }

            /// <summary>Yerelde tam yol; bulutta sağlayıcının dosya tanımlayıcısı.</summary>
            public List<string> Keys { get; } = new List<string>();
        }

        public async Task CleanupAsync(BackupPlan plan, CancellationToken cancellationToken)
        {
            if (plan == null || string.IsNullOrEmpty(plan.LocalPath))
                return;

            Log.Information("Retention temizliği başlıyor: Plan={PlanName}, BulutHedef={HasCloud}",
                plan.PlanName, plan.HasCloudTargets);

            List<BackupResult> history;
            List<BackupResult> foreignHistory;
            try
            {
                // Geçmiş tek seferde okunur ve plana göre ayrılır
                var all = (_historyManager.GetRecentHistory(int.MaxValue) ?? new List<BackupResult>())
                    .Where(r => r != null)
                    .ToList();
                history = all.Where(r => r.PlanId == plan.PlanId).ToList();

                var livePlanIds = GetLivePlanIds();
                foreignHistory = all
                    .Where(r => r.PlanId != plan.PlanId && (livePlanIds == null || livePlanIds.Contains(r.PlanId ?? string.Empty)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Retention: yedek geçmişi okunamadı — güvenlik gereği hiçbir dosya silinmeyecek (Plan={PlanName})",
                    plan.PlanName);
                return;
            }

            var groups = BuildGroups(plan);
            var foreign = BuildForeignSets(foreignHistory);
            var invalid = BuildInvalidFileSet(history);
            var cloudProtected = plan.HasCloudTargets ? BuildCloudProtectedFileSet(plan, history) : null;

            await CleanupLocalAsync(plan, groups, foreign, invalid, cloudProtected, cancellationToken)
                .ConfigureAwait(false);

            if (plan.HasCloudTargets && _cloudOrchestrator != null)
            {
                await SweepCloudFoldersAsync(plan, groups, foreign, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Hâlâ var olan planların kimlikleri. Okunamazsa null: o zaman her başka PlanId korunur.
        /// </summary>
        private HashSet<string> GetLivePlanIds()
        {
            if (_planManager == null)
                return null;

            try
            {
                return new HashSet<string>(
                    (_planManager.GetAllPlans() ?? new List<BackupPlan>())
                        .Where(p => p != null && !string.IsNullOrEmpty(p.PlanId))
                        .Select(p => p.PlanId),
                    StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Retention: plan listesi okunamadı, tüm başka planların geçmişi korunacak");
                return null;
            }
        }

        private static List<RetentionGroup> BuildGroups(BackupPlan plan)
        {
            var groups = new List<RetentionGroup>();

            foreach (string db in plan.Databases ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(db)) continue;

                groups.Add(new RetentionGroup(db, BackupFileName.Full, BackupFileType.SqlFull));
                groups.Add(new RetentionGroup(db, BackupFileName.Differential, BackupFileType.SqlDifferential));
                groups.Add(new RetentionGroup(db, BackupFileName.Incremental, BackupFileType.SqlLog));
                groups.Add(new RetentionGroup(db, BackupFileName.Vss, BackupFileType.SqlVss));
            }

            if (plan.FileBackup?.IsEnabled == true)
                groups.Add(new RetentionGroup(null, null, BackupFileType.FileBackup));

            return groups;
        }

        // ── Yerel ─────────────────────────────────────────────────────────

        private async Task CleanupLocalAsync(
            BackupPlan plan,
            List<RetentionGroup> groups,
            (HashSet<string> Paths, HashSet<string> Names, HashSet<string> RemoteIds) foreign,
            HashSet<string> invalidFiles,
            HashSet<string> cloudProtected,
            CancellationToken ct)
        {
            if (!Directory.Exists(plan.LocalPath))
            {
                Log.Warning("Retention: yerel dizin yok veya erişilemiyor, yerel temizlik atlandı: {Path}",
                    plan.LocalPath);
                return;
            }

            List<FileInfo> files = await Task.Run(
                () => Directory.GetFiles(plan.LocalPath).Select(f => new FileInfo(f)).ToList(), ct)
                .ConfigureAwait(false);

            var skippedForeign = new List<string>();
            var itemsByGroup = new Dictionary<RetentionGroup, List<RetentionItem>>();
            var invalidByGroup = new Dictionary<RetentionGroup, List<RetentionItem>>();

            foreach (var group in groups)
            {
                var items = new List<RetentionItem>();
                var invalidItems = new List<RetentionItem>();
                foreach (var file in files)
                {
                    if (!group.TryMatch(file.Name, out DateTime ts))
                        continue;

                    // Aynı klasöre farklı yol yazımıyla (Z:\ ↔ \\srv\, 8.3, junction) ulaşan
                    // başka bir planın dosyası tam yolla yakalanmaz; ad da karşılaştırılır.
                    string fullPath = NormalizePath(file.FullName);
                    if (foreign.Paths.Contains(fullPath) || foreign.Names.Contains(file.Name))
                    {
                        skippedForeign.Add(file.Name);
                        continue;
                    }

                    var item = new RetentionItem { Name = file.Name, Time = ts, Size = file.Length };
                    item.Keys.Add(file.FullName);
                    (invalidFiles.Contains(fullPath) ? invalidItems : items).Add(item);
                }
                itemsByGroup[group] = items;
                invalidByGroup[group] = invalidItems;
            }

            LogSkippedForeign("Yerel", skippedForeign);

            var toDelete = SelectForDeletion(plan, itemsByGroup);

            // Başarısız yedeğin dosyası (doğrulaması geçmemiş .bak vb.) politika sayımına girmez,
            // zincire taban olmaz; grubunda daha yeni geçerli bir yedek varsa silinir.
            foreach (var kvp in invalidByGroup)
            {
                var valid = itemsByGroup[kvp.Key];
                if (valid.Count == 0) continue;
                DateTime newestValid = valid.Max(i => i.Time);
                toDelete.AddRange(kvp.Value.Where(i => i.Time < newestValid).Select(i => (kvp.Key, i)));
            }

            int deleted = 0, kept = 0;
            foreach (var (group, item) in ApplyRunLimit(toDelete, "Yerel"))
            {
                ct.ThrowIfCancellationRequested();
                string path = item.Keys[0];

                if (cloudProtected != null &&
                    (cloudProtected.Contains(ProtectAllMarker) || cloudProtected.Contains(NormalizePath(path))))
                {
                    kept++;
                    Log.Warning("Retention atlandı (buluta gönderilememiş): {FileName}", item.Name);
                    continue;
                }

                try
                {
                    File.Delete(path);
                    deleted++;
                    Log.Information("Eski yedek silindi: {FileName} ({Group})", item.Name, group);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Dosya silinemedi: {FileName}", item.Name);
                }
            }

            if (deleted > 0 || kept > 0)
            {
                Log.Information(
                    "Yerel retention tamamlandı: Plan={PlanName} — {Deleted} silindi, {Kept} buluta gönderilemediği için korundu",
                    plan.PlanName, deleted, kept);
            }
        }

        // ── Bulut ─────────────────────────────────────────────────────────

        private async Task SweepCloudFoldersAsync(
            BackupPlan plan,
            List<RetentionGroup> groups,
            (HashSet<string> Paths, HashSet<string> Names, HashSet<string> RemoteIds) foreign,
            CancellationToken ct)
        {
            foreach (var target in plan.CloudTargets)
            {
                if (target is null || !target.IsEnabled)
                    continue;

                ct.ThrowIfCancellationRequested();

                // Boş RemoteFolderPath'te yükleme varsayılan klasöre gider; liste ve silme de oradan
                var listTarget = Cloud.CloudUploadOrchestrator.WithEffectiveFolder(target, plan.PlanName);

                List<CloudFileEntry> remoteFiles;
                try
                {
                    remoteFiles = await _cloudOrchestrator.ListFolderAsync(listTarget, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Cloud folder sweep: Listeleme başarısız ({Provider})", target.DisplayName);
                    continue;
                }

                if (remoteFiles is null)
                {
                    Log.Warning("Cloud folder sweep: {Provider} listelenemedi, bulut temizliği atlandı.",
                        target.DisplayName);
                    continue;
                }

                var skippedForeign = new List<string>();
                var itemsByGroup = new Dictionary<RetentionGroup, List<RetentionItem>>();

                foreach (var group in groups)
                {
                    var byName = new Dictionary<string, RetentionItem>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in remoteFiles)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.FileId) ||
                            !group.TryMatch(entry.Name, out DateTime ts))
                            continue;

                        if (foreign.Names.Contains(entry.Name) || foreign.RemoteIds.Contains(entry.FileId))
                        {
                            skippedForeign.Add(entry.Name);
                            continue;
                        }

                        if (!byName.TryGetValue(entry.Name, out var item))
                        {
                            item = new RetentionItem { Name = entry.Name, Time = ts };
                            byName[entry.Name] = item;
                        }
                        item.Size = Math.Max(item.Size, entry.SizeBytes);
                        item.Keys.Add(entry.FileId);
                    }
                    itemsByGroup[group] = byName.Values.ToList();
                }

                LogSkippedForeign(target.DisplayName, skippedForeign);

                var selected = ApplyRunLimit(SelectForDeletion(plan, itemsByGroup), target.DisplayName);
                var deleteTarget = selected.Count > 0 ? WithSafeModeTrash(listTarget) : listTarget;

                int deleted = 0;
                foreach (var (group, item) in selected)
                {
                    foreach (string fileId in item.Keys)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            bool ok = await _cloudOrchestrator.DeleteFromTargetAsync(fileId, deleteTarget, ct)
                                .ConfigureAwait(false);
                            if (ok)
                            {
                                deleted++;
                                Log.Information(
                                    "Cloud folder sweep: {Provider} — eski dosya silindi: {FileName} ({Group})",
                                    target.DisplayName, item.Name, group);
                            }
                            else
                            {
                                Log.Warning("Cloud folder sweep: {Provider} — silme başarısız: {FileName}",
                                    target.DisplayName, item.Name);
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Cloud folder sweep hatası: {Provider} — {FileName}",
                                target.DisplayName, item.Name);
                        }
                    }
                }

                Log.Information(
                    "Cloud folder sweep tamamlandı: {Provider} — {Count} eski dosya silindi (Plan={PlanName}, klasör={Folder})",
                    target.DisplayName, deleted, plan.PlanName,
                    listTarget.Type == CloudProviderType.UncPath ? listTarget.LocalOrUncPath : listTarget.RemoteFolderPath);
            }
        }

        /// <summary>
        /// Bir çalıştırmadaki silmeleri <see cref="MaxDeletionsPerRun"/> ile sınırlar (en eskiler önce);
        /// kalanlar sonraki çalıştırmalara bırakılır.
        /// </summary>
        private static List<(RetentionGroup Group, RetentionItem Item)> ApplyRunLimit(
            List<(RetentionGroup Group, RetentionItem Item)> toDelete, string where)
        {
            if (toDelete.Count <= MaxDeletionsPerRun)
                return toDelete;

            Log.Information(
                "Retention ({Where}): {Total} eski dosyadan en eski {Limit} tanesi bu çalıştırmada silinecek, " +
                "kalan {Rest} sonraki çalıştırmalara bırakıldı",
                where, toDelete.Count, MaxDeletionsPerRun, toDelete.Count - MaxDeletionsPerRun);

            return toDelete.OrderBy(d => d.Item.Time).Take(MaxDeletionsPerRun).ToList();
        }

        /// <summary>
        /// Güvenli dönemde, çöp kutusu kapalı Google Drive hedefinin silmeleri çöpe gitsin diye
        /// TrashRetentionDays verilmiş bir kopya döndürür; aksi halde hedefin kendisi.
        /// </summary>
        private CloudTargetConfig WithSafeModeTrash(CloudTargetConfig target)
        {
            if (target.Type != CloudProviderType.GoogleDrivePersonal || target.UsesTrash || !IsInSafeMode())
                return target;

            var copy = Cloud.CloudUploadOrchestrator.ShallowCopyWithFolder(target, target.RemoteFolderPath);
            copy.TrashRetentionDays = (int)SafeModeDuration.TotalDays;
            Log.Information(
                "Retention güvenli dönem: {Provider} silmeleri kalıcı değil, çöp kutusuna gönderiliyor",
                target.DisplayName);
            return copy;
        }

        /// <summary>
        /// Güvenli dönemde miyiz? Başlangıç ilk çağrıda kaydedilir. Dosya okunamaz/yazılamazsa
        /// güvenli tarafta kalınır (true).
        /// </summary>
        private bool IsInSafeMode()
        {
            try
            {
                if (File.Exists(SafeModeMarkerPath) &&
                    DateTime.TryParse(File.ReadAllText(SafeModeMarkerPath).Trim(),
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind, out DateTime start))
                {
                    return DateTime.UtcNow < start.ToUniversalTime() + SafeModeDuration;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(SafeModeMarkerPath));
                File.WriteAllText(SafeModeMarkerPath, DateTime.UtcNow.ToString("o"));
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Retention güvenli dönem bilgisi okunamadı/yazılamadı, silmeler çöpe gönderilecek");
                return true;
            }
        }

        // ── Politika ──────────────────────────────────────────────────────

        /// <summary>
        /// Her grup için politikanın sileceği öğeleri seçer. Fark/log grupları zincire bağlıdır:
        /// aynı veritabanının en yeni tam yedeğinden sonraki fark ve log yedekleri silinmez;
        /// veritabanının hiç tam yedeği yoksa fark/log yedeklerinin hiçbiri silinmez.
        /// </summary>
        private static List<(RetentionGroup Group, RetentionItem Item)> SelectForDeletion(
            BackupPlan plan, Dictionary<RetentionGroup, List<RetentionItem>> itemsByGroup)
        {
            // Damgası şu andan bir günden fazla ileride olan dosya (sunucu saati bir ara yanlış
            // ayarlanmış ya da elle bırakılmış) sayıma girmez ve silinmez: aksi halde sonsuza dek
            // "en yeni" sayılıp gerçek yedeklerin silinmesine yol açardı.
            DateTime futureLimit = DateTime.Now.AddDays(1);
            foreach (var group in itemsByGroup.Keys.ToList())
            {
                var future = itemsByGroup[group].Where(i => i.Time > futureLimit).ToList();
                if (future.Count == 0) continue;

                Log.Warning(
                    "Retention: {Count} dosyanın tarihi ileride, sayılmadı ve silinmeyecek ({Group}): {Files}",
                    future.Count, group, string.Join(", ", future.Take(10).Select(i => i.Name)));
                itemsByGroup[group] = itemsByGroup[group].Except(future).ToList();
            }

            var newestFull = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in itemsByGroup)
            {
                if (kvp.Key.BackupType == BackupFileName.Full && kvp.Value.Count > 0)
                    newestFull[kvp.Key.Database] = kvp.Value.Max(i => i.Time);
            }

            var result = new List<(RetentionGroup, RetentionItem)>();
            foreach (var kvp in itemsByGroup)
            {
                var group = kvp.Key;
                var items = kvp.Value;
                if (items.Count == 0)
                    continue;

                var policy = plan.GetEffectiveRetention(group.FileType);
                if (policy == null)
                    continue;

                DateTime? chainStart = null;
                if (group.IsChained)
                {
                    if (!newestFull.TryGetValue(group.Database, out DateTime t))
                        continue;
                    chainStart = t;
                }

                foreach (var item in SelectByPolicy(items, policy))
                {
                    if (chainStart.HasValue && item.Time >= chainStart.Value)
                        continue;
                    result.Add((group, item));
                }
            }

            return result;
        }

        /// <summary>
        /// Politikaya göre silinecek öğeler. En yeni öğe her durumda kalır: yedekleme uzun süre
        /// durduysa "N günden eski" kuralı grubun tamamını silmez.
        /// </summary>
        private static List<RetentionItem> SelectByPolicy(List<RetentionItem> items, RetentionPolicy retention)
        {
            var ordered = items.OrderByDescending(i => i.Time).ToList();
            var candidates = new HashSet<RetentionItem>();

            if (retention.Type == RetentionPolicyType.GFS)
            {
                var keep = GfsProtected(ordered, retention, i => i.Time, i => i.Size);
                candidates.UnionWith(ordered.Where(i => !keep.Contains(i)));
            }
            else
            {
                if (retention.Type == RetentionPolicyType.KeepLastN ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    // KeepLastN=0 "hepsini sil" anlamına gelmez
                    int keep = retention.KeepLastN;
                    if (keep < 1)
                    {
                        Log.Warning("Retention: KeepLastN={Keep} geçersiz, 1 olarak uygulanıyor", keep);
                        keep = 1;
                    }
                    candidates.UnionWith(ordered.Skip(keep));
                }

                if (retention.Type == RetentionPolicyType.DeleteOlderThanDays ||
                    retention.Type == RetentionPolicyType.Both)
                {
                    if (retention.DeleteOlderThanDays <= 0)
                    {
                        Log.Warning(
                            "Retention: DeleteOlderThanDays={Days} geçersiz, yaş bazlı silme atlandı",
                            retention.DeleteOlderThanDays);
                    }
                    else
                    {
                        DateTime cutoff = DateTime.Now.AddDays(-retention.DeleteOlderThanDays);
                        candidates.UnionWith(ordered.Where(i => i.Time < cutoff));
                    }
                }
            }

            candidates.Remove(ordered[0]);
            return ordered.Where(candidates.Contains).ToList();
        }

        /// <summary>
        /// GFS (Grandfather-Father-Son) politikasına göre korunacak dosyaları belirler.
        /// Her periyot (gün/hafta/ay/yıl) için en yeni (en büyük) yedek seçilir.
        /// </summary>
        /// <remarks>Public for unit testing.</remarks>
        public static HashSet<string> BuildGfsProtectedSet(List<FileInfo> files, RetentionPolicy retention)
        {
            return new HashSet<string>(
                GfsProtected(files, retention, f => f.CreationTime, f => f.Length).Select(f => f.FullName),
                StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<T> GfsProtected<T>(
            List<T> items, RetentionPolicy retention, Func<T, DateTime> time, Func<T, long> size)
        {
            var protectedItems = new HashSet<T>();
            DateTime now = DateTime.Now;

            // Günlük: Son N günün her biri için en iyi yedeği koru
            if (retention.GfsKeepDaily > 0)
            {
                SelectBestPerPeriod(items, retention.GfsKeepDaily, time, size,
                    t => t.Date,
                    now.Date.AddDays(-retention.GfsKeepDaily + 1),
                    protectedItems);
            }

            // Haftalık: Son N haftanın her biri için en iyi yedeği koru (ISO hafta başı: Pazartesi)
            if (retention.GfsKeepWeekly > 0)
            {
                SelectBestPerPeriod(items, retention.GfsKeepWeekly, time, size,
                    GetWeekStart,
                    GetWeekStart(now).AddDays(-7 * (retention.GfsKeepWeekly - 1)),
                    protectedItems);
            }

            // Aylık: Son N ayın her biri için en iyi yedeği koru
            if (retention.GfsKeepMonthly > 0)
            {
                SelectBestPerPeriod(items, retention.GfsKeepMonthly, time, size,
                    t => new DateTime(t.Year, t.Month, 1),
                    new DateTime(now.Year, now.Month, 1).AddMonths(-retention.GfsKeepMonthly + 1),
                    protectedItems);
            }

            // Yıllık: Son N yılın her biri için en iyi yedeği koru
            if (retention.GfsKeepYearly > 0)
            {
                SelectBestPerPeriod(items, retention.GfsKeepYearly, time, size,
                    t => new DateTime(t.Year, 1, 1),
                    new DateTime(now.Year - retention.GfsKeepYearly + 1, 1, 1),
                    protectedItems);
            }

            return protectedItems;
        }

        /// <summary>
        /// Belirtilen periyot fonksiyonuna göre her dilimden en büyük dosyayı seçer.
        /// </summary>
        private static void SelectBestPerPeriod<T>(
            List<T> items,
            int keepCount,
            Func<T, DateTime> time,
            Func<T, long> size,
            Func<DateTime, DateTime> periodKeySelector,
            DateTime cutoff,
            HashSet<T> protectedItems)
        {
            var bestPerPeriod = items
                .Where(i => time(i) >= cutoff)
                .GroupBy(i => periodKeySelector(time(i)))
                .OrderByDescending(g => g.Key)
                .Take(keepCount)
                .Select(g => g.OrderByDescending(size).ThenByDescending(time).First());

            foreach (var item in bestPerPeriod)
                protectedItems.Add(item);
        }

        /// <summary>
        /// ISO 8601 hafta başlangıcını (Pazartesi) döndürür.
        /// </summary>
        private static DateTime GetWeekStart(DateTime date)
        {
            int diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

            return date.Date.AddDays(-diff);
        }

        // ── Sahiplik ve koruma setleri ───────────────────────────────────

        /// <summary>
        /// Bu cihazdaki başka planların geçmişinde geçen yerel yollar, dosya adları ve uzak
        /// tanımlayıcılar. Bu dosyalar ad desenine uysa da bu plana ait sayılmaz.
        /// </summary>
        private static (HashSet<string> Paths, HashSet<string> Names, HashSet<string> RemoteIds) BuildForeignSets(
            List<BackupResult> foreignHistory)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var remoteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in foreignHistory)
            {
                foreach (string path in new[] { result.BackupFilePath, result.CompressedFilePath, result.VssFileCopyPath })
                {
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    paths.Add(NormalizePath(path));
                    names.Add(Path.GetFileName(path));
                }

                var uploads = (result.CloudUploadResults ?? new List<CloudUploadResult>())
                    .Concat(result.VssCloudUploadResults ?? new List<CloudUploadResult>());
                foreach (var upload in uploads)
                {
                    if (upload != null && !string.IsNullOrEmpty(upload.RemoteFilePath))
                        remoteIds.Add(upload.RemoteFilePath);
                }
            }

            return (paths, names, remoteIds);
        }

        /// <summary>
        /// Başarısız SQL yedeklerinin (yedek ya da doğrulama hatası) yerel dosyaları (normalize).
        /// Geçerli yedek sayılmazlar. Dosya yedeği kaydı bir kaynak bile kilitliyse "Failed" olur
        /// ama arşivi geçerlidir; bu yüzden dosya yedeği kayıtları dahil edilmez.
        /// </summary>
        private static HashSet<string> BuildInvalidFileSet(List<BackupResult> history)
        {
            var invalid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var result in history)
            {
                if (result.Status == BackupResultStatus.Success || result.DatabaseName == FileBackupDatabaseName)
                    continue;

                AddNormalized(invalid, result.BackupFilePath);
                AddNormalized(invalid, result.CompressedFilePath);
            }
            return invalid;
        }

        /// <summary>
        /// Buluta tam gönderilmemiş yedeklerin yerel yollarını (normalize) toplar; bu dosyalar
        /// yerelden silinmez. Başarısız SQL yedeğinin .bak'ı korunmaz (geçerli yedek değildir,
        /// korunması diski sonsuza dek doldururdu). Arşivler (.7z, Files_*) durumdan bağımsız korunur:
        /// kısmi başarılı dosya yedeği "Failed" kaydedilir ama arşivi tek kopyadır.
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
                        if (result.Status == BackupResultStatus.Success)
                            AddNormalized(protectedFiles, result.BackupFilePath);
                        AddNormalized(protectedFiles, result.CompressedFilePath);
                    }

                    // VSS arşivi kendi yükleme sonucuna göre korunur (null: eski sürüm kaydı, bilinmez)
                    if (result.VssCloudUploadResults != null &&
                        (result.VssCloudUploadResults.Count == 0 || !result.VssCloudUploadResults.All(r => r.IsSuccess)))
                    {
                        AddNormalized(protectedFiles, result.VssFileCopyPath);
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
                protectedFiles.Add(ProtectAllMarker);
            }

            return protectedFiles;
        }

        private static void LogSkippedForeign(string where, List<string> skipped)
        {
            if (skipped.Count == 0) return;

            Log.Information(
                "Retention ({Where}): {Count} dosya başka bir planın geçmişinde kayıtlı olduğu için atlandı: {Files}",
                where, skipped.Count,
                string.Join(", ", skipped.Take(20)) + (skipped.Count > 20 ? ", …" : string.Empty));
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
    }
}
