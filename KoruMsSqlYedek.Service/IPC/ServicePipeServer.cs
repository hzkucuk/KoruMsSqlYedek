using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.IPC;

namespace KoruMsSqlYedek.Service.IPC
{
    /// <summary>
    /// Named Pipe sunucusu — BackupWindowsService içinde barındırılır.
    /// Tray uygulamasından gelen komutları dinler, BackupActivityHub olaylarını tray'e iletir.
    /// Pipe adı: KoruMsSqlYedekPipe
    /// Protokol: JSON newline-delimited (her mesaj \n ile biter)
    /// </summary>
    public class ServicePipeServer : IDisposable
    {
        private const string PipeName = "KoruMsSqlYedekPipe";
        private static readonly ILogger Log = Serilog.Log.ForContext<ServicePipeServer>();

        private readonly ISchedulerService _schedulerService;
        private readonly IBackupCancellationRegistry _cancellationRegistry;
        private readonly IPlanManager _planManager;

        private readonly ConcurrentDictionary<Guid, NamedPipeServerStream> _clients
            = new ConcurrentDictionary<Guid, NamedPipeServerStream>();

        // Her istemci için ayrı yazma kilidi — eş zamanlı yazmaların JSON'u bozmasını önler
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _writeLocks
            = new ConcurrentDictionary<Guid, SemaphoreSlim>();

        private CancellationTokenSource _cts;
        private bool _disposed;

        public ServicePipeServer(
            ISchedulerService schedulerService,
            IBackupCancellationRegistry cancellationRegistry,
            IPlanManager planManager)
        {
            if (schedulerService == null) throw new ArgumentNullException(nameof(schedulerService));
            if (cancellationRegistry == null) throw new ArgumentNullException(nameof(cancellationRegistry));
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));

            _schedulerService = schedulerService;
            _cancellationRegistry = cancellationRegistry;
            _planManager = planManager;
        }

        // ── Başlatma / Durdurma ──────────────────────────────────────────────

        public void Start()
        {
            _cts = new CancellationTokenSource();
            BackupActivityHub.ActivityChanged += OnActivityChanged;
            Task.Run(() => AcceptLoopAsync(_cts.Token));
            Log.Information("Pipe sunucusu başlatıldı: {PipeName}", PipeName);
        }

        public void Stop()
        {
            BackupActivityHub.ActivityChanged -= OnActivityChanged;
            _cts?.Cancel();

            foreach (var pair in _clients)
            {
                try { pair.Value.Dispose(); }
                catch { /* bağlantı zaten kopmuş olabilir */ }
            }

            _clients.Clear();
            Log.Information("Pipe sunucusu durduruldu.");
        }

        // ── Bağlantı kabul döngüsü ───────────────────────────────────────────

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                NamedPipeServerStream pipe = null;
                try
                {
                    var pipeSecurity = new PipeSecurity();
                    pipeSecurity.AddAccessRule(new PipeAccessRule(
                        new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow));
                    pipeSecurity.AddAccessRule(new PipeAccessRule(
                        new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                        PipeAccessRights.FullControl,
                        AccessControlType.Allow));
                    pipeSecurity.AddAccessRule(new PipeAccessRule(
                        new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                        PipeAccessRights.FullControl,
                        AccessControlType.Allow));
                    pipeSecurity.AddAccessRule(new PipeAccessRule(
                        WindowsIdentity.GetCurrent().User!,
                        PipeAccessRights.FullControl,
                        AccessControlType.Allow));

                    pipe = NamedPipeServerStreamAcl.Create(
                        PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        0, 0,
                        pipeSecurity);

                    await pipe.WaitForConnectionAsync(ct);

                    var clientId = Guid.NewGuid();
                    _clients[clientId] = pipe;
                    _writeLocks[clientId] = new SemaphoreSlim(1, 1);

                    Log.Debug("Yeni pipe istemcisi bağlandı: {ClientId}", clientId);

                    // İstemci okuma döngüsünü arka planda başlat
                    _ = Task.Run(() => ClientReadLoopAsync(clientId, pipe, ct), ct);
                }
                catch (OperationCanceledException)
                {
                    pipe?.Dispose();
                    break;
                }
                catch (UnauthorizedAccessException ex)
                {
                    pipe?.Dispose();
                    Log.Warning(
                        "Pipe erişim hatası — muhtemelen başka bir servis instance'ı zaten çalışıyor. " +
                        "10 saniye sonra tekrar denenecek. Detay: {Message}", ex.Message);
                    await Task.Delay(10_000, ct).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    pipe?.Dispose();
                    Log.Warning(ex, "Pipe I/O hatası, 3 saniye sonra yeniden deneniyor...");
                    await Task.Delay(3000, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    pipe?.Dispose();
                    Log.Warning(ex, "Pipe kabul döngüsü hatası, yeniden deneniyor...");
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                }
            }
        }

        // ── İstemci okuma döngüsü ────────────────────────────────────────────

        private async Task ClientReadLoopAsync(
            Guid clientId, NamedPipeServerStream pipe, CancellationToken ct)
        {
            try
            {
                using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
                    bufferSize: 4096, leaveOpen: true);

                while (!ct.IsCancellationRequested && pipe.IsConnected)
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null) break; // bağlantı kapandı

                    var message = PipeSerializer.Deserialize(line);
                    if (message == null) continue;

                    await HandleCommandAsync(clientId, message, pipe, ct);
                }
            }
            catch (IOException) { /* normal bağlantı kopuşu */ }
            catch (Exception ex)
            {
                Log.Warning(ex, "Pipe istemci okuma hatası: {ClientId}", clientId);
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                if (_writeLocks.TryRemove(clientId, out var wl)) wl.Dispose();
                try { pipe.Dispose(); }
                catch { /* ignore */ }
                Log.Debug("Pipe istemcisi bağlantısı kesildi: {ClientId}", clientId);
            }
        }

        // ── Komut işleyici ───────────────────────────────────────────────────

        private async Task HandleCommandAsync(
            Guid clientId, PipeMessage message, NamedPipeServerStream pipe, CancellationToken ct)
        {
            switch (message.Type)
            {
                case PipeMessageType.ManualBackup:
                {
                    var cmd = (ManualBackupCommand)message;
                    Log.Information(
                        "Manuel yedek komutu alındı: PlanId={PlanId}, Tür={BackupType}",
                        cmd.PlanId, cmd.BackupType);

                    if (_cancellationRegistry.IsRunning(cmd.PlanId))
                    {
                        Log.Warning(
                            "Manuel yedek reddedildi — bu plan zaten çalışıyor: PlanId={PlanId}",
                            cmd.PlanId);
                        break;
                    }

                    try
                    {
                        await _schedulerService.TriggerPlanNowAsync(cmd.PlanId, ct, cmd.BackupType);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Manuel yedek tetiklenirken hata: {PlanId}", cmd.PlanId);
                    }
                    break;
                }

                case PipeMessageType.CancelBackup:
                {
                    var cmd = (CancelBackupCommand)message;
                    Log.Information("İptal komutu alındı: PlanId={PlanId}", cmd.PlanId);
                    _cancellationRegistry.Cancel(cmd.PlanId);
                    break;
                }

                case PipeMessageType.RequestStatus:
                    await SendStatusToClientAsync(clientId, pipe, ct);
                    break;

                default:
                    Log.Debug("Bilinmeyen pipe mesaj tipi: {Type}", message.Type);
                    break;
            }
        }

        // ── Olayları tüm istemcilere yayınla ────────────────────────────────

        private void OnActivityChanged(object sender, BackupActivityEventArgs e)
        {
            var msg = BackupActivityMessage.FromArgs(e);

            // Plan konfigürasyonundan ToastEnabled değerini aktar
            var plan = _planManager.GetAllPlans()
                .FirstOrDefault(p => p.PlanId == e.PlanId);
            msg.ToastEnabled = plan?.Notifications?.ToastEnabled ?? true;

            BroadcastAsync(msg).ConfigureAwait(false);

            // Yedekleme bitince tüm istemcilere güncel zamanlama bilgisi gönder
            if (e.ActivityType == BackupActivityType.Completed ||
                e.ActivityType == BackupActivityType.Failed ||
                e.ActivityType == BackupActivityType.Cancelled)
            {
                BroadcastStatusAsync().ConfigureAwait(false);
            }
        }

        private async Task BroadcastAsync(PipeMessage message)
        {
            if (_clients.IsEmpty) return;

            string json = PipeSerializer.Serialize(message) + "\n";
            byte[] data = Encoding.UTF8.GetBytes(json);

            foreach (var pair in _clients.ToArray())
            {
                if (!_writeLocks.TryGetValue(pair.Key, out var writeLock)) continue;
                bool acquired = await writeLock.WaitAsync(2000);
                if (!acquired) continue;
                try
                {
                    if (pair.Value.IsConnected)
                        await pair.Value.WriteAsync(data, 0, data.Length);
                }
                catch
                {
                    _clients.TryRemove(pair.Key, out _);
                    try { pair.Value.Dispose(); } catch { }
                }
                finally
                {
                    writeLock.Release();
                }
            }
        }

        /// <summary>Tüm bağlı istemcilere güncel zamanlama bilgisini yayınlar.</summary>
        private async Task BroadcastStatusAsync()
        {
            if (_clients.IsEmpty) return;
            try
            {
                var status = new ServiceStatusMessage { IsRunning = _schedulerService.IsRunning };
                var plans = _planManager.GetAllPlans();
                var nextFire = new Dictionary<string, string>();
                var cts = new CancellationTokenSource(5000);

                foreach (var plan in plans.Where(p => p.IsEnabled))
                {
                    var t = await _schedulerService.GetNextFireTimeAsync(plan.PlanId, cts.Token);
                    if (t.HasValue)
                        nextFire[plan.PlanId] = t.Value.LocalDateTime.ToString("dd.MM.yyyy HH:mm");
                }

                status.NextFireTimes = nextFire;
                await BroadcastAsync(status);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "BroadcastStatusAsync hatası.");
            }
        }

        private async Task SendStatusToClientAsync(Guid clientId, NamedPipeServerStream pipe, CancellationToken ct)
        {
            var status = new ServiceStatusMessage { IsRunning = _schedulerService.IsRunning };

            try
            {
                var plans = _planManager.GetAllPlans();
                var nextFire = new Dictionary<string, string>();

                foreach (var plan in plans.Where(p => p.IsEnabled))
                {
                    var t = await _schedulerService.GetNextFireTimeAsync(plan.PlanId, ct);
                    if (t.HasValue)
                        nextFire[plan.PlanId] = t.Value.LocalDateTime.ToString("dd.MM.yyyy HH:mm");
                }

                status.NextFireTimes = nextFire;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Sonraki ateşleme zamanları alınamadı.");
            }

            string json = PipeSerializer.Serialize(status) + "\n";
            byte[] data = Encoding.UTF8.GetBytes(json);

            if (!_writeLocks.TryGetValue(clientId, out var writeLock)) return;
            bool acquired = await writeLock.WaitAsync(2000, ct);
            if (!acquired) return;
            try
            {
                if (pipe.IsConnected)
                    await pipe.WriteAsync(data, 0, data.Length, ct);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Durum mesajı gönderilemedi.");
            }
            finally
            {
                writeLock.Release();
            }
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _cts?.Dispose();
        }
    }
}
