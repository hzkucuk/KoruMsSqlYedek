using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.IPC;

namespace KoruMsSqlYedek.Service
{
    /// <summary>
    /// Derleme yükleme hatasında servisi yeniden başlatır. CLR yüklenemeyen derlemeyi süreç
    /// ömrü boyunca hatalı olarak önbelleğe aldığından (bkz. <see cref="RuntimeHealth"/>)
    /// tek çare yeni süreçtir. Süreç FailFast ile sonlandırılır; SCM kurtarma eylemi
    /// (installer: sc failure ... restart) servisi yeniden başlatır.
    ///
    /// Akış: istek → (gerekirse döngü koruması için ertele) → çalışan yedekler bitene kadar
    /// bekle → kısa ek bekleme (IPC/bildirim kuyruğu boşalsın) → işaret dosyasına zaman ve
    /// etkilenen planları yaz → FailFast. Yeni süreç sağlıklı kalkarsa etkilenen planları
    /// hemen yeniden çalıştırır; o gecenin yedeği kaybolmaz.
    ///
    /// Döngü koruması: iki yeniden başlatma arasında en az <see cref="MinInterval"/> olur.
    /// İstek ATLANMAZ, ertelenir — aksi halde hatalı süreç günlerce yaşamaya devam ederdi.
    /// </summary>
    internal sealed class RuntimeRestartGuard
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<RuntimeRestartGuard>();
        private static readonly TimeSpan MinInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DrainDelay = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan PendingMaxAge = TimeSpan.FromHours(12);

        private readonly IBackupCancellationRegistry _registry;
        private readonly HashSet<string> _affectedPlans = new(StringComparer.OrdinalIgnoreCase);
        private int _restartRequested;

        public RuntimeRestartGuard(IBackupCancellationRegistry registry)
        {
            _registry = registry;
        }

        private static string MarkerPath => Path.Combine(PathHelper.LogsDirectory, "runtime-restart.json");

        private sealed class Marker
        {
            public DateTime Utc { get; set; }
            public List<string> PendingPlanIds { get; set; } = new();
        }

        /// <summary>
        /// Açılışta kritik derlemeleri yükler; yüklenemeyen varsa yeniden başlatma ister.
        /// Ayrıca çalışma sırasında raporlanan yükleme hatalarına abone olur.
        /// </summary>
        /// <returns>Süreç sağlıklıysa true (önceki yeniden başlatmadan kalan planlar tetiklenebilir).</returns>
        public bool Start(CancellationToken ct)
        {
            RuntimeHealth.AssemblyLoadFailed += assembly =>
                RequestRestart($"Derleme yüklenemedi: {assembly}", ct);

            var failures = RuntimeHealth.PreloadCriticalAssemblies();
            if (failures.Count == 0)
            {
                Log.Debug("Kritik .NET derlemeleri yüklendi.");
                return true;
            }

            foreach (var (assembly, error) in failures)
                Log.Fatal("Kritik .NET derlemesi yüklenemedi: {Assembly} — {Error}", assembly, error);

            RequestRestart($"Açılışta {failures.Count} kritik derleme yüklenemedi", ct);
            return false;
        }

        /// <summary>
        /// Önceki süreç derleme hatası yüzünden yeniden başlatıldıysa, o süreçte başarısız olan
        /// planları yeniden tetikler. Yalnızca sağlıklı süreçte ve bir kez çalışır.
        /// </summary>
        public async Task TriggerPendingPlansAsync(ISchedulerService scheduler, CancellationToken ct)
        {
            Marker marker = ReadMarker();
            if (marker == null || marker.PendingPlanIds.Count == 0)
                return;

            // Bir kez tetiklenir: liste temizlenir, zaman damgası (döngü koruması için) korunur
            var pending = marker.PendingPlanIds.ToList();
            marker.PendingPlanIds.Clear();
            WriteMarker(marker);

            TimeSpan age = DateTime.UtcNow - marker.Utc;
            if (age < TimeSpan.Zero || age > PendingMaxAge)
            {
                Log.Warning("Yeniden başlatmadan kalan {Count} plan çok eski/geçersiz zamanlı, tetiklenmiyor.", pending.Count);
                return;
            }

            foreach (string planId in pending)
            {
                try
                {
                    Log.Warning("Derleme hatası yüzünden yarıda kalan plan yeniden çalıştırılıyor: {PlanId}", planId);
                    await scheduler.TriggerPlanNowAsync(planId, ct);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Bekleyen plan tetiklenemedi: {PlanId}", planId);
                }
            }
        }

        private void RequestRestart(string reason, CancellationToken ct)
        {
            CaptureRunningPlans();

            if (Interlocked.Exchange(ref _restartRequested, 1) != 0)
                return;

            // Döngü koruması: son yeniden başlatmadan bu yana MinInterval geçmediyse ERTELE.
            // Gelecek tarihli işaret (saat düzeltmesi) geçersiz sayılır.
            TimeSpan delay = TimeSpan.Zero;
            Marker last = ReadMarker();
            if (last != null)
            {
                TimeSpan age = DateTime.UtcNow - last.Utc;
                if (age < TimeSpan.Zero)
                    Log.Warning("Yeniden başlatma işareti gelecek tarihli ({Utc:u}), yok sayılıyor.", last.Utc);
                else if (age < MinInterval)
                    delay = MinInterval - age;
            }

            if (delay > TimeSpan.Zero)
                Log.Error(
                    "Servis yeniden başlatılması gerekiyor ({Reason}); döngüyü önlemek için {Minutes:F0} dk ertelendi. " +
                    "Sorun sürerse kurulumu onarın ve antivirüs kayıtlarını kontrol edin.",
                    reason, delay.TotalMinutes);
            else
                Log.Warning("Servis yeniden başlatılacak: {Reason}. Çalışan yedekler bitince süreç sonlandırılacak.", reason);

            _ = Task.Run(async () =>
            {
                try
                {
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, ct);

                    // Çalışan yedeği yarıda kesme. Boşaldıktan sonra kısa ek bekleme: Failed/Completed
                    // olayının IPC yazımı ve bildirim kuyruğu tamamlansın; bu arada yeni iş başladıysa tekrar bekle.
                    do
                    {
                        while (_registry.IsAnyRunning())
                        {
                            CaptureRunningPlans();
                            await Task.Delay(PollInterval, ct);
                        }
                        await Task.Delay(DrainDelay, ct);
                    }
                    while (_registry.IsAnyRunning());

                    // Önceki süreçten kalıp henüz tetiklenemeyen planlar (ör. açılışta yine hata) korunur
                    List<string> affected;
                    lock (_affectedPlans) affected = _affectedPlans.ToList();
                    var previous = ReadMarker()?.PendingPlanIds ?? new List<string>();
                    affected = affected.Union(previous, StringComparer.OrdinalIgnoreCase).ToList();
                    WriteMarker(new Marker { Utc = DateTime.UtcNow, PendingPlanIds = affected });

                    Log.Warning(
                        "Derleme yükleme hatası nedeniyle servis süreci yeniden başlatma için sonlandırılıyor. " +
                        "Yeni süreçte yeniden çalıştırılacak planlar: {Plans}",
                        affected.Count > 0 ? string.Join(", ", affected) : "-");
                    await Serilog.Log.CloseAndFlushAsync();

                    // Environment.Exit host'u düzgün kapatıp SCM'ye SERVICE_STOPPED bildirebilir;
                    // o zaman kurtarma eylemi tetiklenmez. FailFast "beklenmedik sonlanma" üretir
                    // ve sc failure ... restart kuralı servisi kesin olarak yeniden başlatır.
                    Environment.FailFast("KoruMsSqlYedek: .NET derleme yükleme hatası — servis yeniden başlatılıyor.");
                }
                catch (OperationCanceledException) { /* servis zaten duruyor */ }
                catch (Exception ex)
                {
                    Log.Error(ex, "Servis yeniden başlatma isteği işlenemedi.");
                }
            });
        }

        /// <summary>O an çalışan planları "bu süreçte etkilenen" olarak kaydeder.</summary>
        private void CaptureRunningPlans()
        {
            try
            {
                var running = _registry.GetRunningPlanIds();
                lock (_affectedPlans)
                {
                    foreach (string id in running)
                        _affectedPlans.Add(id);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Çalışan planlar alınamadı.");
            }
        }

        private static Marker ReadMarker()
        {
            try
            {
                if (!File.Exists(MarkerPath)) return null;
                var marker = JsonSerializer.Deserialize<Marker>(File.ReadAllText(MarkerPath));
                if (marker == null) return null;
                marker.Utc = DateTime.SpecifyKind(marker.Utc, DateTimeKind.Utc);
                marker.PendingPlanIds ??= new List<string>();
                return marker;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Yeniden başlatma işareti okunamadı, yok sayılıyor: {Path}", MarkerPath);
                return null;
            }
        }

        private static void WriteMarker(Marker marker)
        {
            try { File.WriteAllText(MarkerPath, JsonSerializer.Serialize(marker)); }
            catch (Exception ex) { Log.Warning(ex, "Yeniden başlatma işareti yazılamadı: {Path}", MarkerPath); }
        }
    }
}
