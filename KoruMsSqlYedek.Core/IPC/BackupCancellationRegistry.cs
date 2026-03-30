using System;
using System.Collections.Concurrent;
using System.Threading;

namespace KoruMsSqlYedek.Core.IPC
{
    /// <summary>
    /// Çalışan yedekleme job'larının CancellationTokenSource'larını takip eden singleton kayıt.
    /// BackupJobExecutor iş başında kaydeder, bitişte siler.
    /// ServicePipeServer, Cancel komutu geldiğinde ilgili CTS'i iptal eder.
    /// </summary>
    public interface IBackupCancellationRegistry
    {
        /// <summary>Çalışan bir job için CTS kaydeder.</summary>
        void Register(string planId, CancellationTokenSource cts);

        /// <summary>Planın çalışan job'ını iptal eder. Plan kaydı yoksa sessizce geçer.</summary>
        void Cancel(string planId);

        /// <summary>Job tamamlanınca kaydı temizler.</summary>
        void Unregister(string planId);

        /// <summary>Belirtilen plan şu an çalışıyor mu?</summary>
        bool IsRunning(string planId);

        /// <summary>Herhangi bir plan şu an çalışıyor mu?</summary>
        bool IsAnyRunning();
    }

    /// <summary>Thread-safe, ConcurrentDictionary tabanlı implementasyon.</summary>
    public class BackupCancellationRegistry : IBackupCancellationRegistry
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _running
            = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

        public void Register(string planId, CancellationTokenSource cts)
        {
            if (string.IsNullOrWhiteSpace(planId))
                return;

            // Varsa eski kaydın yerini al (önceki zaten iptal/tamamlanmış olmalı)
            _running[planId] = cts;
        }

        public void Cancel(string planId)
        {
            if (string.IsNullOrWhiteSpace(planId))
                return;

            if (_running.TryGetValue(planId, out CancellationTokenSource cts))
            {
                try { cts.Cancel(); }
                catch (ObjectDisposedException) { /* zaten tamamlanmış */ }
            }
        }

        public void Unregister(string planId)
        {
            if (string.IsNullOrWhiteSpace(planId))
                return;

            _running.TryRemove(planId, out _);
        }

        public bool IsRunning(string planId)
        {
            if (string.IsNullOrWhiteSpace(planId))
                return false;

            return _running.ContainsKey(planId);
        }

        public bool IsAnyRunning() => !_running.IsEmpty;
    }
}
