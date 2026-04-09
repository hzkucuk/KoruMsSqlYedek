# KoruMsSqlYedek

![Version](https://img.shields.io/badge/version-0.99.31-blue)

**SQL Server Yedekleme & Bulut Senkronizasyon Sistemi**

KoruMsSqlYedek

---

## Özellikler

### Yedekleme
- **SQL Server Yedekleme:** Full, Differential ve Incremental (Transaction Log) stratejileri
- **Zincir Bütünlüğü:** Differential/Incremental öncesi otomatik Full yedek kontrolü
- **Otomatik Full Yükseltme:** Differential zincir limitini aşınca otomatik Full tetikleme
- **RESTORE VERIFYONLY:** Her yedek sonrası isteğe bağlı doğrulama
- **Restore Güvenliği:** Restore öncesi hedef DB otomatik yedekleme
- **Dosya Yedekleme:** VSS (Volume Shadow Copy) ile açık/kilitli dosya desteği (Outlook PST/OST, SQL MDF/LDF vb.)

### Bulut Senkronizasyon
- **Google Drive:** Bireysel hesap, OAuth kimlik doğrulama, çöp kutusu temizleme
- **FTP/SFTP:** FluentFTP ve SSH.NET ile güvenli transfer
- **UNC Ağ Paylaşımı:** Ağ paylaşımı desteği

### Zamanlama & Otomasyon
- **Quartz.NET:** Cron tabanlı esnek zamanlama
- **Windows Service:** Topshelf ile arka plan çalışma
- **Tray Uygulaması:** System Tray'den kolay yönetim

### Güvenlik
- **DPAPI + Base64:** Şifreler güvenli şekilde saklanır
- **SQL + Windows Authentication:** Her iki kimlik doğrulama desteklenir
- **7z Şifreleme:** LZMA2 algoritması ile şifreli arşivler

### Diğer
- **Retention Politikası:** Plan bazında KeepLastN veya DeleteOlderThanDays
- **E-posta Bildirimi:** SMTP ile başarı/hata bildirimleri
- **Yapısal Loglama:** Serilog ile rolling file, 30 gün saklama
- **Çoklu Dil:** Türkçe (tr-TR) ve İngilizce (en-US)

---

## Mimari

```
KoruMsSqlYedek.sln
├── KoruMsSqlYedek.Core       # Paylaşılan modeller, arayüzler, yardımcılar
├── KoruMsSqlYedek.Engine     # İş mantığı: yedekleme, sıkıştırma, bulut, zamanlama
├── KoruMsSqlYedek.Win        # System Tray WinForms UI uygulaması
├── KoruMsSqlYedek.Service    # Windows Service (arka plan yedekleme motoru)
└── KoruMsSqlYedek.Tests      # Unit testler (MSTest)
```

**Veri yolu:** Tüm paylaşılan veriler (planlar, ayarlar, upload state, loglar) `%ProgramData%\KoruMsSqlYedek\` altında saklanır. Bu sayede hem Tray uygulaması (kullanıcı bağlamı) hem Windows Service (LocalSystem) aynı verilere erişir.

---

## Gereksinimler

| Bileşen | Minimum Versiyon |
|---------|-----------------|
| İşletim Sistemi | Windows 10+ |
| .NET | 10.0 |
| SQL Server | 2016+ |
| Visual Studio | 2022+ (geliştirme için) |

---

## Kurulum

### Geliştirici Kurulumu

```bash
git clone https://github.com/hzkucuk/KoruMsSqlYedek.git
cd KoruMsSqlYedek
dotnet restore KoruMsSqlYedek.slnx
dotnet build KoruMsSqlYedek.slnx
```

### Üretim Kurulumu
Inno Setup ile oluşturulan kurulum paketi kullanılır. Detaylar için [INSTALL.md](INSTALL.md) dosyasına bakınız.

---

## Kullanım

### Tray Uygulaması
1. `KoruMsSqlYedek.Win.exe` çalıştırın — System Tray'de ikon belirir
2. Sağ tık menüsünden **Planlar** ile yedekleme planları oluşturun
3. Cron ifadesi ile zamanlama belirleyin
4. Bulut hedeflerini yapılandırın

### Windows Service
```bash
KoruMsSqlYedek.Service.exe install
KoruMsSqlYedek.Service.exe start
```

---

## Teknoloji Stack

| Teknoloji | Kullanım |
|-----------|----------|
| Quartz.NET 3.8.1 | Cron zamanlama |
| Serilog 3.1.1 | Yapısal loglama |
| SMO 171.30.0 | SQL Server yedekleme/restore |
| SevenZipSharp 1.6.2.24 | LZMA2 sıkıştırma |
| AlphaVSS 1.4.0 | Volume Shadow Copy |
| Google.Apis.Drive.v3 | Google Drive API |
| FluentFTP 51.0.0 | FTP/FTPS |
| SSH.NET 2024.1.0 | SFTP |
| MailKit 4.3.0 | SMTP e-posta |
| Autofac 8.1.1 | IoC/DI container |
| Topshelf 4.3.0 | Windows Service hosting |

---

## Lisans

Bu proje özel lisans altındadır. Detaylar için proje sahibiyle iletişime geçiniz.

---

## Ekran Görüntüleri

> Ekran görüntüleri `docs/images/` klasörüne eklenecektir.
