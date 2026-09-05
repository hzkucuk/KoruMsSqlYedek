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
using KoruMsSqlYedek.Engine.Update;
using KoruMsSqlYedek.Service.Security;
using KoruMsSqlYedek.Service.SelfUpdate;

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
                    // GÜVENLİK: Pipe'a yalnızca SYSTEM ve BUILTIN\Administrators bağlanabilir.
                    // Tray uygulaması requireAdministrator ile yükseltilmiş çalışır; sıradan
                    // kullanıcılar ManualBackup/CancelBackup/InstallSelfUpdate gönderemez.
                    var pipeSecurity = new PipeSecurity();
                    pipeSecurity.AddAccessRule(new PipeAccessRule(
                        new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                        PipeAccessRights.FullControl,
                        AccessControlType.Allow));
                    pipeSecurity.AddAccessRule(new PipeAccessRule(
                        new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                        PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize,
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

                    // Derinlemesine savunma: ACL'den bağımsız olarak istemci kimliğini doğrula.
                    // Hiçbir komut bu kontrolden önce işlenmez.
                    if (!IsClientAuthorized(pipe))
                    {
                        pipe.Dispose();
                        pipe = null;
                        continue;
                    }

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

        // ── İstemci kimlik doğrulama ─────────────────────────────────────────

        /// <summary>
        /// Bağlanan istemcinin SYSTEM veya BUILTIN\Administrators üyesi olduğunu doğrular.
        /// RunAsClient (ImpersonateNamedPipeClient) istemcinin Identification seviyesinde
        /// bağlanmasıyla da çalışır — WindowsIdentity.GetCurrent() için yeterlidir.
        /// Kimlik alınamazsa bağlantı reddedilir (fail-closed).
        /// </summary>
        private static bool IsClientAuthorized(NamedPipeServerStream pipe)
        {
            string userName = "(bilinmiyor)";
            bool authorized = false;

            try
            {
                pipe.RunAsClient(() =>
                {
                    using var identity = WindowsIdentity.GetCurrent();
                    userName = identity.Name;

                    if (identity.IsSystem)
                    {
                        authorized = true;
                        return;
                    }

                    var principal = new WindowsPrincipal(identity);
                    authorized = principal.IsInRole(WindowsBuiltInRole.Administrator);
                });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Pipe istemci kimliği alınamadı — bağlantı reddedildi.");
                return false;
            }

            if (!authorized)
            {
                Log.Warning(
                    "Yetkisiz pipe istemcisi reddedildi: {User} (SYSTEM veya Administrators değil)",
                    userName);
                return false;
            }

            Log.Debug("Pipe istemcisi yetkili: {User}", userName);
            return true;
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

                case PipeMessageType.InstallSelfUpdate:
                {
                    var cmd = (InstallSelfUpdateCommand)message;
                    Log.Information(
                        "Self-update komutu alındı: Sürüm={Version}, URL={Url}",
                        cmd.Version, cmd.DownloadUrl);
                    // Arka planda çalıştır — pipe yanıtı HandleInstallSelfUpdateAsync içinde gönderilir
                    _ = Task.Run(() => HandleInstallSelfUpdateAsync(clientId, pipe, cmd, ct), ct);
                    break;
                }

                default:
                    Log.Debug("Bilinmeyen pipe mesaj tipi: {Type}", message.Type);
                    break;
            }
        }

        // ── Self-Update ─────────────────────────────────────────────────────

        /// <summary>
        /// Self-update installer'ını SYSTEM yetkileriyle (UAC'sız) indirir, doğrular ve çalıştırır.
        /// GÜVENLİK: Tray'den gelen dosya yoluna asla güvenilmez — servis installer'ı kendisi,
        /// yalnızca SYSTEM/Administrators erişimli Updates dizinine indirir ve SHA-256'sını doğrular.
        /// 1. Komutu doğrula (sürüm, https+GitHub URL, 64 hex SHA-256)
        /// 2. Updates dizinini kısıtlı ACL ile hazırla, eski *.exe'leri sil
        /// 3. İndir, SHA-256 (+ boyut) doğrula — uyuşmazsa dosyayı sil ve reddet
        /// 4. Restart flag yaz (installer sonrası tray app yeniden başlatılacak)
        /// 5. Pipe üzerinden yanıt gönder (tray app kapanacak)
        /// 6. Installer'ı SYSTEM olarak sessiz modda çalıştır
        /// 7. Tray app'i kullanıcı oturumunda yeniden başlat
        /// </summary>
        private async Task HandleInstallSelfUpdateAsync(
            Guid clientId, NamedPipeServerStream pipe,
            InstallSelfUpdateCommand cmd, CancellationToken ct)
        {
            var selfUpdate = new SelfUpdateHandler();
            string installerPath = null;

            async Task RejectAsync(string reason)
            {
                Log.Warning("Self-update reddedildi: {Reason}", reason);
                TryDeleteFile(installerPath);
                await SendResponseToClientAsync(clientId, pipe,
                    new InstallSelfUpdateResponseMessage { Success = false, Message = reason }, ct);
            }

            try
            {
                // 1. Komut alanlarını doğrula
                if (!Version.TryParse(cmd?.Version, out var targetVersion))
                {
                    await RejectAsync($"Geçersiz sürüm bilgisi: '{cmd?.Version}'");
                    return;
                }

                if (!InstallerVerifier.IsAllowedDownloadUrl(cmd.DownloadUrl))
                {
                    await RejectAsync("İndirme URL'i izin verilen listede değil (yalnızca https ve GitHub hostları kabul edilir).");
                    return;
                }

                if (!InstallerVerifier.IsValidSha256Hex(cmd.ExpectedSha256))
                {
                    await RejectAsync("Beklenen SHA-256 özeti eksik veya geçersiz (64 hex karakter olmalı).");
                    return;
                }

                // 2. Kısıtlı Updates dizinini hazırla (varsa ACL yeniden uygulanır) ve eski installer'ları sil
                string updatesDir = DirectoryAcl.UpdatesDirectory;
                DirectoryAcl.EnsureRestrictedDirectory(updatesDir);

                foreach (string stale in Directory.GetFiles(updatesDir, "*.exe"))
                    TryDeleteFile(stale);

                installerPath = Path.Combine(
                    updatesDir, $"KoruMsSqlYedek_v{targetVersion.ToString(3)}_Setup.exe");

                // 3. İndir ve doğrula
                Log.Information("Self-update installer indiriliyor: {Url} → {Path}", cmd.DownloadUrl, installerPath);
                await InstallerVerifier.DownloadToFileAsync(cmd.DownloadUrl, installerPath, ct);

                var fileInfo = new FileInfo(installerPath);
                if (cmd.ExpectedSizeBytes > 0 && fileInfo.Length != cmd.ExpectedSizeBytes)
                {
                    await RejectAsync(
                        $"Installer boyutu beklenenden farklı (beklenen {cmd.ExpectedSizeBytes} B, indirilen {fileInfo.Length} B).");
                    return;
                }

                string actualSha256 = await InstallerVerifier.ComputeSha256HexAsync(installerPath, ct);
                if (!InstallerVerifier.Sha256Matches(actualSha256, cmd.ExpectedSha256))
                {
                    Log.Warning(
                        "Self-update SHA-256 uyuşmazlığı: beklenen={Expected}, hesaplanan={Actual}",
                        cmd.ExpectedSha256, actualSha256);
                    await RejectAsync("Installer SHA-256 özeti beklenen değerle uyuşmuyor — dosya silindi.");
                    return;
                }

                Log.Information(
                    "Self-update installer doğrulandı (SHA-256 eşleşti, {Size} B): {Path}",
                    fileInfo.Length, installerPath);

                // 4. Tray app yolunu belirle
                string serviceDir = AppContext.BaseDirectory;
                string trayAppPath = Path.GetFullPath(
                    Path.Combine(serviceDir, "..", "KoruMsSqlYedek.Win.exe"));

                // 5. Restart flag dosyasını yaz
                await selfUpdate.WriteFlagAsync(trayAppPath, ct);

                // 6. Başarı yanıtını gönder — tray app kapanacak
                await SendResponseToClientAsync(clientId, pipe,
                    new InstallSelfUpdateResponseMessage
                    {
                        Success = true,
                        Message = "Self-update başlatılıyor, uygulama yeniden başlatılacak."
                    }, ct);

                // 7. Tray app'in kapanmasını bekle (file lock'lar serbest kalsın)
                await Task.Delay(3000, ct).ConfigureAwait(false);

                // 8. Doğrulanmış installer'ı başlat — SYSTEM olarak UAC gerekmez
                //    /VERYSILENT: kullanıcıya hiçbir diyalog gösterme
                //    /SUPPRESSMSGBOXES: hata mesajlarını da gösterme
                //    /SP-: "Bu programı kurmak istiyor musunuz?" sorusunu atla
                //    /CLOSEAPPLICATIONS: açık dosyaları kapat
                //    /NOPOSTLAUNCH=1: [Run] bölümünde tray app başlatmasını engelle (biz başlatacağız)
                using var process = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /SP- /CLOSEAPPLICATIONS /NOPOSTLAUNCH=1",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                if (process is null)
                {
                    Log.Error("Self-update installer process başlatılamadı.");
                    selfUpdate.TryDeleteRestartFlag();
                    return;
                }

                Log.Information("Installer PID: {PID}, bekleniyor...", process.Id);
                await process.WaitForExitAsync(ct).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    Log.Error("Self-update installer hata ile sonlandı. ExitCode: {ExitCode}",
                        process.ExitCode);
                    selfUpdate.TryDeleteRestartFlag();
                    return;
                }

                Log.Information("Self-update installer başarıyla tamamlandı.");

                // 9. Tray app'i kullanıcı oturumunda yeniden başlat
                selfUpdate.LaunchTrayAppInUserSession(trayAppPath);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error(ex, "Self-update hatası.");
                selfUpdate.TryDeleteRestartFlag();
                TryDeleteFile(installerPath);

                try
                {
                    if (pipe.IsConnected)
                    {
                        await SendResponseToClientAsync(clientId, pipe,
                            new InstallSelfUpdateResponseMessage
                            {
                                Success = false,
                                Message = $"Self-update hatası: {ex.Message}"
                            }, ct);
                    }
                }
                catch { /* Pipe zaten kopmuş olabilir */ }
            }
        }

        /// <summary>Dosyayı siler; yoksa veya silinemezse sessizce loglar.</summary>
        private static void TryDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Log.Debug("Dosya silindi: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dosya silinemedi: {Path}", path);
            }
        }

        /// <summary>Belirli bir istemciye yanıt mesajı gönderir.</summary>
        private async Task SendResponseToClientAsync(
            Guid clientId, NamedPipeServerStream pipe,
            PipeMessage response, CancellationToken ct)
        {
            string json = PipeSerializer.Serialize(response) + "\n";
            byte[] data = Encoding.UTF8.GetBytes(json);

            if (!_writeLocks.TryGetValue(clientId, out var writeLock)) return;
            bool acquired = await writeLock.WaitAsync(2000, ct);
            if (!acquired) return;
            try
            {
                if (pipe.IsConnected)
                    await pipe.WriteAsync(data, 0, data.Length, ct);
            }
            finally
            {
                writeLock.Release();
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
