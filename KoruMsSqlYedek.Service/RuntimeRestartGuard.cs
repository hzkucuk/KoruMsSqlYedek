using System;
using System.Collections.Generic;
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
    /// bekle → kısa ek bekleme (IPC/bildirim kuyruğu boşalsın) → işaret dosyasına zaman yaz →
    /// FailFast. Etkilenen planlar hata anında işaret dosyasına yazılır; yeni süreç sağlıklı
    /// kalkarsa onları hemen yeniden çalıştırır, o gecenin yedeği kaybolmaz.
    ///
    /// Döngü koruması: iki yeniden başlatma arasında en az <see cref="MinInterval"/> olur;
    /// istek atlanmaz, ertelenir. Bir "olay" (aralarında <see cref="IncidentReset"/>'ten uzun
    /// sessizlik olmayan yeniden başlatmalar) boyunca planlar en fazla <see cref="MaxReruns"/>
    /// kez yeniden çalıştırılır — kalıcı hatada (ör. karantinadaki DLL) saatlik Full yedek döngüsü olmaz.
    /// </summary>
    internal sealed class RuntimeRestartGuard
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<RuntimeRestartGuard>();
        private static readonly TimeSpan MinInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DrainDelay = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan PendingMaxAge = TimeSpan.FromHours(12);
        private static readonly TimeSpan IncidentReset = TimeSpan.FromHours(24);
        private const int MaxReruns = 2;

        private static readonly object MarkerLock = new();

        private readonly IBackupCancellationRegistry _registry;
        private int _restartRequested;

        public RuntimeRestartGuard(IBackupCancellationRegistry registry)
        {
            _registry = registry;
        }

        private static string MarkerPath => Path.Combine(PathHelper.LogsDirectory, "runtime-restart.json");

        private sealed class Marker
        {
            /// <summary>Son yeniden başlatma (döngü koruması). Henüz yeniden başlatılmadıysa MinValue.</summary>
            public DateTime Utc { get; set; }

            /// <summary>Bu olayın ilk yeniden başlatması (yeniden çalıştırma yaş sınırı).</summary>
            public DateTime FirstUtc { get; set; }

            /// <summary>Bu olayda kaç kez bekleyen plan yeniden çalıştırıldı.</summary>
            public int Reruns { get; set; }

            public List<string> PendingPlanIds { get; set; } = new();
        }

        /// <summary>
        /// Açılışta kritik derlemeleri yükler; yeniden başlatmanın çözebileceği bir hata varsa
        /// yeniden başlatma ister. Ayrıca çalışma sırasında raporlanan yükleme hatalarına abone olur.
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

            foreach (var (assembly, error, _) in failures)
                Log.Fatal("Kritik .NET derlemesi yüklenemedi: {Assembly} — {Error}", assembly, error);

            if (failures.Any(f => f.Restartable))
                RequestRestart($"Açılışta {failures.Count} kritik derleme yüklenemedi", ct);
            else
                Log.Fatal("Derleme hatası yeniden başlatmayla düzelmez (sürüm uyuşmazlığı / bozuk dosya): " +
                          "kurulumu yeniden çalıştırarak onarın.");
            return false;
        }

        /// <summary>
        /// Önceki süreç derleme hatası yüzünden yeniden başlatıldıysa, o süreçte başarısız olan
        /// planları yeniden tetikler. Yalnızca sağlıklı süreçte ve bir kez çalışır.
        /// </summary>
        public async Task TriggerPendingPlansAsync(ISchedulerService scheduler, CancellationToken ct)
        {
            List<string> pending;
            lock (MarkerLock)
            {
                Marker marker = ReadMarker();
                if (marker == null || marker.PendingPlanIds.Count == 0)
                    return;

                pending = marker.PendingPlanIds.ToList();
                marker.PendingPlanIds.Clear();

                TimeSpan age = DateTime.UtcNow - marker.FirstUtc;
                if (marker.Utc == DateTime.MinValue || age < TimeSpan.Zero || age > PendingMaxAge)
                {
                    WriteMarker(marker);
                    Log.Warning("Yeniden başlatmadan kalan {Count} plan çok eski/geçersiz zamanlı, tetiklenmiyor.", pending.Count);
                    return;
                }

                if (marker.Reruns >= MaxReruns)
                {
                    WriteMarker(marker);
                    Log.Error(
                        "Planlar bu olayda zaten {Reruns} kez yeniden çalıştırıldı, sorun kalıcı görünüyor; " +
                        "tekrar tetiklenmiyor: {Plans}. Kurulumu onarın ve antivirüs kayıtlarını kontrol edin.",
                        marker.Reruns, string.Join(", ", pending));
                    return;
                }

                marker.Reruns++;
                WriteMarker(marker);
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
            // Her raporda: o an çalışan (hatayı alan) planı kalıcı olarak kaydet —
            // erteleme sırasında düşen planlar da yeni süreçte yeniden çalıştırılır.
            PersistRunningPlans();

            if (Interlocked.Exchange(ref _restartRequested, 1) != 0)
                return;

            // Döngü koruması: son yeniden başlatmadan bu yana MinInterval geçmediyse ERTELE.
            // Gelecek tarihli işaret (saat düzeltmesi) geçersiz sayılır.
            TimeSpan delay = TimeSpan.Zero;
            Marker last;
            lock (MarkerLock) last = ReadMarker();
            if (last != null && last.Utc != DateTime.MinValue)
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
                            await Task.Delay(PollInterval, ct);
                        await Task.Delay(DrainDelay, ct);
                    }
                    while (_registry.IsAnyRunning());

                    List<string> pending;
                    lock (MarkerLock)
                    {
                        Marker marker = ReadMarker() ?? new Marker();
                        DateTime now = DateTime.UtcNow;
                        bool newIncident = marker.Utc == DateTime.MinValue || now - marker.Utc > IncidentReset
                                           || marker.Utc > now;
                        if (newIncident)
                        {
                            marker.FirstUtc = now;
                            marker.Reruns = 0;
                        }
                        marker.Utc = now;
                        WriteMarker(marker);
                        pending = marker.PendingPlanIds.ToList();
                    }

                    Log.Warning(
                        "Derleme yükleme hatası nedeniyle servis süreci yeniden başlatma için sonlandırılıyor. " +
                        "Yeni süreçte yeniden çalıştırılacak planlar: {Plans}",
                        pending.Count > 0 ? string.Join(", ", pending) : "-");
                    await Serilog.Log.CloseAndFlushAsync();

                    // Environment.Exit host'u düzgün kapatıp SCM'ye SERVICE_STOPPED bildirebilir;
                    // o zaman kurtarma eylemi tetiklenmez. FailFast "beklenmedik sonlanma" üretir
                    // ve sc failure ... restart kuralı servisi kesin olarak yeniden başlatır.
                    Environment.FailFast("KoruMsSqlYedek: .NET derleme yükleme hatası — servis yeniden başlatılıyor.");
                }
                catch (OperationCanceledException) { /* servis duruyor; etkilenen planlar işarette kalır */ }
                catch (Exception ex)
                {
                    Log.Error(ex, "Servis yeniden başlatma isteği işlenemedi.");
                }
            });
        }

        /// <summary>O an çalışan planları işaret dosyasının bekleyen listesine hemen ekler.</summary>
        private void PersistRunningPlans()
        {
            try
            {
                var running = _registry.GetRunningPlanIds();
                if (running.Count == 0) return;

                lock (MarkerLock)
                {
                    Marker marker = ReadMarker() ?? new Marker { Utc = DateTime.MinValue, FirstUtc = DateTime.MinValue };
                    int before = marker.PendingPlanIds.Count;
                    marker.PendingPlanIds = marker.PendingPlanIds
                        .Union(running, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (marker.PendingPlanIds.Count != before)
                        WriteMarker(marker);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Etkilenen planlar kaydedilemedi.");
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
                marker.FirstUtc = DateTime.SpecifyKind(marker.FirstUtc, DateTimeKind.Utc);
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
