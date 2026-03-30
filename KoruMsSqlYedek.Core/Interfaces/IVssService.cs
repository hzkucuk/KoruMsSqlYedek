using System;
using System.Collections.Generic;
using System.Threading;

namespace KoruMsSqlYedek.Core.Interfaces
{
    /// <summary>
    /// Volume Shadow Copy Service (VSS) soyutlaması.
    /// Açık/kilitli dosyaları (Outlook PST, OST, Office dosyaları vb.)
    /// shadow copy üzerinden kopyalamayı sağlar.
    /// </summary>
    public interface IVssService : IDisposable
    {
        /// <summary>
        /// Belirtilen volume için VSS snapshot oluşturur.
        /// </summary>
        /// <param name="volumePath">Volume yolu (ör. "C:\").</param>
        /// <param name="ct">İptal belirteci. Her VSS adımı arasında kontrol edilir.</param>
        /// <returns>Snapshot ID.</returns>
        Guid CreateSnapshot(string volumePath, CancellationToken ct = default);

        /// <summary>
        /// Snapshot üzerinden dosyanın shadow copy yolunu döndürür.
        /// Bu yol üzerinden kilitli dosya okunabilir.
        /// </summary>
        /// <param name="snapshotId">CreateSnapshot'tan dönen ID.</param>
        /// <param name="originalFilePath">Orijinal dosya yolu.</param>
        /// <returns>Shadow copy dosya yolu.</returns>
        string GetSnapshotFilePath(Guid snapshotId, string originalFilePath);

        /// <summary>
        /// Tüm aktif snapshot'ları temizler ve serbest bırakır.
        /// </summary>
        void DeleteAllSnapshots();

        /// <summary>
        /// Belirtilen snapshot'ı siler.
        /// </summary>
        void DeleteSnapshot(Guid snapshotId);

        /// <summary>
        /// VSS servisi kullanılabilir durumda mı kontrol eder.
        /// </summary>
        bool IsAvailable();
    }
}
