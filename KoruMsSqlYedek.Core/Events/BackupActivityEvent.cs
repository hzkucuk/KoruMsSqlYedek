using System;
using System.Collections.Generic;

namespace KoruMsSqlYedek.Core.Events
{
    public enum BackupActivityType
    {
        Started,
        DatabaseProgress,
        StepChanged,          // Adım adı değişti (SQL, Verify, Compress, Cloud...)
        CloudUploadStarted,   // Bir bulut hedefine upload başladı
        CloudUploadProgress,  // Upload yüzdesi güncellendi
        CloudUploadCompleted, // Bir bulut hedefine upload bitti
        CloudUploadAbandoned, // Maks deneme aşıldı, dosya terk edildi
        Completed,
        Failed,
        Cancelled
    }

    public class BackupActivityEventArgs : EventArgs
    {
        public string PlanId { get; set; }
        public string PlanName { get; set; }
        public string DatabaseName { get; set; }
        public BackupActivityType ActivityType { get; set; }
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public string Message { get; set; }

        // Bulut upload detayları
        public string StepName { get; set; }          // "SQL Backup", "Sıkıştırma", "Bulut Upload" vb.
        public string CloudTargetName { get; set; }   // Provider görünen adı
        public int CloudTargetIndex { get; set; }     // Kaçıncı hedef (1 tabanlı)
        public int CloudTargetTotal { get; set; }     // Toplam hedef sayısı
        public int ProgressPercent { get; set; }      // Upload yüzdesi (0-100)
        public bool IsSuccess { get; set; }           // Upload sonucu

        // Upload hız/boyut bilgisi (CloudUploadProgress olaylarında doldurulur)
        public long BytesSent { get; set; }
        public long BytesTotal { get; set; }
        public long SpeedBytesPerSecond { get; set; }

        /// <summary>
        /// Batch modunda şu an yüklenen dosyanın gönderilen byte miktarı.
        /// Log satırlarında dosya bazlı ilerleme gösterimi için kullanılır.
        /// BytesSent/BytesTotal batch toplam değerlerini taşırken bu alan dosya bazlıdır.
        /// </summary>
        public long FileBytesSent { get; set; }

        /// <summary>
        /// Toplu bulut yüklemede şu an yüklenen dosyanın adı.
        /// </summary>
        public string CloudFileName { get; set; }

        /// <summary>Toplu bulut yüklemede kaçıncı dosya (1 tabanlı).</summary>
        public int CloudFileIndex { get; set; }

        /// <summary>Toplu bulut yüklemede toplam dosya sayısı.</summary>
        public int CloudFileTotal { get; set; }

        /// <summary>
        /// Plan bu çalışmada dosya yedekleme fazı içeriyorsa true.
        /// İlerleme çubuğu hesabında SQL ve dosya arasında ağırlık dağılımı yapar.
        /// </summary>
        public bool HasFileBackup { get; set; }

        /// <summary>
        /// Plan en az bir etkin bulut hedefi içeriyorsa true.
        /// false ise bulut yükleme adımı atlanır ve ilerleme çubuğu
        /// yerel adımlara (SQL, doğrulama, sıkıştırma, temizlik) göre ilerler.
        /// </summary>
        public bool HasCloudTargets { get; set; }

        /// <summary>
        /// Plan konfigürasyonundan gelen ToastEnabled değeri.
        /// Tray uygulaması bu değere göre balloon tip gösterir.
        /// Varsayılan true — plan bilgisi yoksa her zaman göster.
        /// </summary>
        public bool ToastEnabled { get; set; } = true;

        /// <summary>
        /// Upload sonrası uzak dosya boyutu (byte). Sağlık kontrolü için.
        /// CloudUploadCompleted olaylarında doldurulur.
        /// </summary>
        public long RemoteFileSizeBytes { get; set; }

        /// <summary>
        /// Yerel dosya boyutu (byte). Sağlık kontrolü karşılaştırması için.
        /// CloudUploadCompleted olaylarında doldurulur.
        /// </summary>
        public long LocalFileSizeBytes { get; set; }

        /// <summary>
        /// Dosya bütünlüğü doğrulaması yapılıp başarılı oldu mu.
        /// true: boyut eşleşmesi doğrulandı, false: doğrulanamadı veya eşleşmiyor.
        /// CloudUploadCompleted olaylarında doldurulur.
        /// </summary>
        public bool? IsIntegrityVerified { get; set; }

        /// <summary>
        /// Maks deneme aşılarak terk edilen dosya bilgileri.
        /// CloudUploadAbandoned olaylarında doldurulur.
        /// </summary>
        public List<string> AbandonedFiles { get; set; }
    }

    /// <summary>
    /// Uygulama genelinde yedekleme aktivitelerini yayınlayan statik event hub.
    /// Tüm UI bileşenleri (MainWindow, Tray, Toast) bu hub'ı dinleyerek güncel kalır.
    /// </summary>
    public static class BackupActivityHub
    {
        public static event EventHandler<BackupActivityEventArgs> ActivityChanged;

        public static void Raise(BackupActivityEventArgs args)
        {
            ActivityChanged?.Invoke(null, args);
        }
    }
}
