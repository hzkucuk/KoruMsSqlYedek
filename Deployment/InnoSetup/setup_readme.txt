MikroSqlDbYedek — SQL Server Yedekleme & Bulut Senkronizasyon Sistemi
=====================================================================

Bu uygulama iki bileşenden oluşur:

1. TRAY UYGULAMASI (MikroSqlDbYedek.Win.exe)
   - System Tray'de çalışır
   - Yedekleme planları yönetimi
   - Dashboard, log görüntüleyici, ayarlar
   - Manuel yedekleme başlatma

2. WINDOWS SERVICE (MikroSqlDbYedek.Service.exe)
   - Arka planda zamanlı yedekleme motoru
   - Quartz.NET cron zamanlama
   - Otomatik başlama olarak kaydedilir

MINIMUM GEREKSİNİMLER
- Windows 10 (64-bit)
- .NET Framework 4.8
- SQL Server 2016+

VERİ KONUMLARI
- Planlar: %APPDATA%\MikroSqlDbYedek\Plans\
- Loglar:  %APPDATA%\MikroSqlDbYedek\Logs\
- Ayarlar: %APPDATA%\MikroSqlDbYedek\Config\

NOT: Güncelleme sırasında mevcut planlarınız ve loglarınız korunur.

Daha fazla bilgi: https://github.com/hzkucuk/MikroSqlDbYedek
