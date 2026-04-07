using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.IPC;

namespace KoruMsSqlYedek.Win.IPC
{
    /// <summary>
    /// Named Pipe istemcisi — Tray (Win) uygulamasında çalışır.
    /// Servisten gelen BackupActivityMessage'ları yerel BackupActivityHub'da tekrar yayınlar.
    /// Tray'den gelen manuel yedek / iptal komutlarını servise iletir.
    /// </summary>
    public class ServicePipeClient : IDisposable
    {
        private const string PipeName      = "KoruMsSqlYedekPipe";
        private const int    ReconnectDelay = 5000; // ms
        private static readonly ILogger Log = Serilog.Log.ForContext<ServicePipeClient>();

        private NamedPipeClientStream _pipe;
        private StreamWriter          _writer;
        private CancellationTokenSource _cts;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private bool _disposed;
        private volatile bool _connected;

        /// <summary>Bağlantı durumu değiştiğinde ateşlenir (UI thread'de değil).</summary>
        public event EventHandler<bool> ConnectionChanged;

        /// <summary>Servis pipe'ına bağlı mı?</summary>
        public bool IsConnected => _connected;

        // ── Başlatma / Durdurma ──────────────────────────────────────────────

        /// <summary>Arka planda bağlantı döngüsünü başlatır.</summary>
        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ConnectLoopAsync(_cts.Token));
        }

        /// <summary>Bağlantıyı ve döngüyü durdurur.</summary>
        public void Stop()
        {
            _cts?.Cancel();
            DisposeConnection();
        }

        // ── Komut göndericileri ──────────────────────────────────────────────

        /// <summary>Servise manuel yedek komutu gönderir.</summary>
        public async Task SendManualBackupCommandAsync(string planId, string backupType)
        {
            var cmd = new ManualBackupCommand { PlanId = planId, BackupType = backupType };
            await SendAsync(cmd);
        }

        /// <summary>Servise iptal komutu gönderir.</summary>
        public async Task SendCancelCommandAsync(string planId)
        {
            var cmd = new CancelBackupCommand { PlanId = planId };
            await SendAsync(cmd);
        }

        /// <summary>Servisten güncel durum bilgisi ister.</summary>
        public async Task RequestStatusAsync()
        {
            await SendAsync(new RequestStatusCommand());
        }

        // ── Bağlantı döngüsü ────────────────────────────────────────────────

        private async Task ConnectLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var pipe = new NamedPipeClientStream(
                        ".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                    await pipe.ConnectAsync(3000, ct);

                    _pipe   = pipe;
                    _writer = new StreamWriter(pipe, Encoding.UTF8, bufferSize: 4096, leaveOpen: true)
                    {
                        AutoFlush = true,
                        NewLine   = "\n"
                    };

                    SetConnected(true);
                    Log.Information("Servis pipe bağlantısı kuruldu.");

                    await RequestStatusAsync();
                    await ReadLoopAsync(pipe, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (TimeoutException)
                {
                    // Servis henüz başlamamış — bekle ve tekrar dene
                    SetConnected(false);
                }
                catch (Exception ex)
                {
                    SetConnected(false);
                    Log.Debug(ex, "Pipe bağlantısı kesildi, yeniden deneniyor...");
                }
                finally
                {
                    DisposeConnection();
                }

                if (!ct.IsCancellationRequested)
                    await Task.Delay(ReconnectDelay, ct).ConfigureAwait(false);
            }
        }

        private async Task ReadLoopAsync(NamedPipeClientStream pipe, CancellationToken ct)
        {
            using var reader = new StreamReader(pipe, Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

            try
            {
                while (!ct.IsCancellationRequested && pipe.IsConnected)
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null) break; // sunucu bağlantıyı kapattı (EOF)

                    var message = PipeSerializer.Deserialize(line);
                    if (message == null) continue;

                    HandleServerMessage(message);
                }
            }
            catch (IOException)
            {
                // Sunucu tarafından bağlantı kapatıldı (pipe broken) veya yedek sonrası yeniden
                // bağlanma döngüsü. SetConnected(false) çağrılmaz; yeniden bağlanma döngüsü
                // sessizce devralır ve zaten bağlı olduğunuzda connected→true atlaması sayesinde
                // "bağlantı kesildi" bildirimi gösterilmez.
                Log.Debug("Pipe okuma döngüsü IOException ile sonlandı, yeniden bağlanılacak.");
            }
        }

        // ── Gelen mesaj işleyici ─────────────────────────────────────────────

        private void HandleServerMessage(PipeMessage message)
        {
            switch (message.Type)
            {
                case PipeMessageType.BackupActivity:
                {
                    var msg  = (BackupActivityMessage)message;
                    var args = msg.ToArgs();

                    // UI thread'e aktararak yayınla
                    FireOnUiThread(() => BackupActivityHub.Raise(args));
                    break;
                }

                case PipeMessageType.ServiceStatus:
                {
                    var msg = (ServiceStatusMessage)message;
                    FireOnUiThread(() => ServiceStatusHub.Raise(msg));
                    Log.Debug("Servis durumu alındı: IsRunning={IsRunning}, PlanCount={Count}",
                        msg.IsRunning, msg.NextFireTimes?.Count ?? 0);
                    break;
                }
            }
        }

        // ── Yardımcı metotlar ────────────────────────────────────────────────

        private async Task SendAsync(PipeMessage message)
        {
            if (!_connected || _writer == null) return;

            await _writeLock.WaitAsync();
            try
            {
                // Bekleme sırasında bağlantı kesilebilir — tekrar kontrol et
                if (!_connected || _writer == null) return;

                string json = PipeSerializer.Serialize(message);
                await _writer.WriteLineAsync(json);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Pipe mesajı gönderilemedi: {Type}", message.Type);
                SetConnected(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private void SetConnected(bool value)
        {
            if (_connected == value) return;
            _connected = value;
            try { ConnectionChanged?.Invoke(this, value); }
            catch { /* UI event handler hatası */ }
        }

        private void DisposeConnection()
        {
            try { _writer?.Dispose(); }   catch { /* ignore */ }
            try { _pipe?.Dispose(); }     catch { /* ignore */ }
            _writer = null;
            _pipe   = null;
        }

        /// <summary>
        /// UI mesaj döngüsünde çalışma garantisi verir.
        /// WinForms context'i yoksa doğrudan çalıştırır (servis/test ortamı).
        /// </summary>
        private static void FireOnUiThread(Action action)
        {
            var ctx = SynchronizationContext.Current;
            if (ctx != null)
                ctx.Post(_ => action(), null);
            else
                action();
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
