# KoruMsSqlYedek — Ürün Gereksinim Dokümanı (PRD)

**Versiyon:** 0.99.35  
**Tarih:** 2025-07-17  
**Yazar:** Hüzeyin Küçük / Zafer Bilgisayar  
**Durum:** Pre-Release (Beta)

---

## 1. Ürün Özeti

**KoruMsSqlYedek**, Windows ortamında çalışan, SQL Server veritabanlarını ve dosya sistemini otomatik olarak yedekleyen, sıkıştıran ve bulut hedeflerine senkronize eden bir masaüstü + servis uygulamasıdır.

**Hedef Kitle:** Küçük ve orta ölçekli işletmelerdeki sistem yöneticileri, muhasebe/ERP yazılımı kullanan son kullanıcılar.

**Temel Değer Önerisi:** Tek bir kurulumla SQL Server yedekleme, dosya yedekleme, şifreli sıkıştırma ve çoklu bulut hedefine otomatik senkronizasyon — teknik bilgi gerektirmeden.

---

## 2. Hedefler ve Başarı Kriterleri

| Hedef | Başarı Kriteri |
|-------|----------------|
| Sıfır veri kaybı | Zamanlanmış yedekler %99.9 başarı oranıyla tamamlanır |
| Otomasyon | Kullanıcı müdahalesi olmadan günlük yedekleme + bulut senkronizasyon |
| Felaket kurtarma | Bulutta en az 1 geçerli kopya her zaman mevcut |
| Bütünlük doğrulama | Her yedek RESTORE VERIFYONLY + SHA-256 boyut eşleşmesi ile doğrulanır |
| Kullanım kolaylığı | Wizard tabanlı plan oluşturma, 5 dakikada kurulum |

---

## 3. Mimari Genel Bakış

```
┌─────────────────────────────────────────────────────────────────┐
│                    KoruMsSqlYedek.sln                            │
├─────────────────┬───────────────┬───────────────┬───────────────┤
│   Core          │   Engine      │   Win         │   Service     │
│   (Paylaşılan)  │   (İş Mantığı)│   (WinForms)  │   (Windows    │
│                 │               │   System Tray │    Service)   │
├─────────────────┼───────────────┼───────────────┼───────────────┤
│ • Modeller      │ • SQL Backup  │ • Dashboard   │ • Quartz Host │
│ • Arayüzler     │ • Dosya Backup│ • Plan Wizard │ • Pipe Server │
│ • Event Hub     │ • Sıkıştırma  │ • Log Viewer  │ • Auto-Start  │
│ • IPC Protokolü │ • Bulut Upload│ • Ayarlar     │               │
│ • Yardımcılar   │ • Zamanlama   │ • Toast/Tray  │               │
│                 │ • Retention   │ • Pipe Client  │               │
│                 │ • E-posta     │               │               │
└─────────────────┴───────────────┴───────────────┴───────────────┘
                              │
                    ┌─────────┴─────────┐
                    │  KoruMsSqlYedek.   │
                    │      Tests         │
                    │  (MSTest, 56+)     │
                    └───────────────────┘

Veri Dizini: %ProgramData%\KoruMsSqlYedek\
(Planlar, ayarlar, upload state, loglar — Tray & Service ortak erişim)
```

### 3.1 İletişim Modeli

```
┌──────────┐    Named Pipe (JSON, newline-delimited)    ┌──────────┐
│  Service  │ ◄════════════════════════════════════════► │   Win    │
│  (Engine) │   BackupActivityMessage ←→ Commands       │  (Tray)  │
└──────────┘                                            └──────────┘
      │                                                       │
      │  BackupActivityHub (static event)                     │
      │  ───────────────────────────────►                     │
      │  PipeProtocol.FromArgs() → JSON → ToArgs()            │
      └───────────────────────────────────────────────────────┘
```

### 3.2 Yedekleme İş Akışı (Pipeline)

```
[Quartz Trigger]
      │
      ▼
┌─ BackupJobExecutor.Execute() ─────────────────────────────────┐
│                                                                │
│  1. SQL Pipeline (her DB için)                                 │
│     ├─ Full / Differential / Incremental yedek                 │
│     ├─ RESTORE VERIFYONLY doğrulama                             │
│     ├─ 7z LZMA2 sıkıştırma (şifreli, isteğe bağlı)           │
│     └─ Arşiv bütünlük doğrulaması                              │
│                                                                │
│  2. Dosya Pipeline                                             │
│     ├─ VSS Snapshot (açık/kilitli dosyalar)                    │
│     ├─ Dosya kopyalama (1 MB buffer)                           │
│     └─ 7z LZMA2 sıkıştırma                                    │
│                                                                │
│  3. Konsolide Bulut Yükleme (tüm dosyalar tek fazda)           │
│     ├─ UploadBatchToAllAsync (dosya × hedef matrisi)           │
│     ├─ Resume desteği (UploadStateManager)                     │
│     ├─ Retry (üstel geri çekilme)                              │
│     └─ SHA-256 boyut doğrulama                                 │
│                                                                │
│  4. Retention Temizliği                                        │
│     ├─ Per-type politikalar (Full/Diff/Log/Files)              │
│     └─ Yerel + Uzak temizlik                                   │
│                                                                │
│  5. Konsolide E-posta Bildirimi (tek e-posta)                  │
│                                                                │
│  6. BackupActivityHub → UI Progress + Log                      │
└────────────────────────────────────────────────────────────────┘
```

---

## 4. Fonksiyonel Gereksinimler

### 4.1 SQL Server Yedekleme

| ID | Gereksinim | Durum |
|----|-----------|-------|
| SQL-01 | Full, Differential, Incremental (Transaction Log) yedekleme stratejileri | ✅ |
| SQL-02 | Zincir bütünlüğü kontrolü (Diff/Inc öncesi otomatik Full kontrol) | ✅ |
| SQL-03 | Otomatik Full yükseltme (Diff zincir limiti aşılınca) | ✅ |
| SQL-04 | RESTORE VERIFYONLY ile her yedek sonrası doğrulama | ✅ |
| SQL-05 | SQL Server Express native sıkıştırma atlanması (WITH COMPRESSION sadece Std/Ent) | ✅ |
| SQL-06 | Windows Authentication + SQL Authentication desteği | ✅ |
| SQL-07 | Otomatik sysadmin yetki kontrolü ve atama (Service modunda) | ✅ |

### 4.2 Dosya Yedekleme

| ID | Gereksinim | Durum |
|----|-----------|-------|
| FILE-01 | VSS (Volume Shadow Copy) ile açık/kilitli dosya yedekleme | ✅ |
| FILE-02 | Outlook PST/OST, SQL MDF/LDF gibi kilitli dosya desteği | ✅ |
| FILE-03 | TreeView tabanlı dosya/klasör seçimi (tri-state checkbox) | ✅ |
| FILE-04 | 1 MB buffer ile büyük dosya kopyalama + ilerleme raporlama | ✅ |
| FILE-05 | Her zaman Full yedek (artırımlı/fark kaldırıldı — v0.93.0) | ✅ |

### 4.3 Sıkıştırma

| ID | Gereksinim | Durum |
|----|-----------|-------|
| CMP-01 | 7z LZMA2 algoritması ile sıkıştırma | ✅ |
| CMP-02 | İsteğe bağlı şifreleme (AES-256) | ✅ |
| CMP-03 | Sıkıştırma ilerleme raporlama (yüzde bazlı) | ✅ |
| CMP-04 | Arşiv bütünlük doğrulaması | ✅ |

### 4.4 Bulut Senkronizasyon

| ID | Gereksinim | Durum |
|----|-----------|-------|
| CLD-01 | Google Drive desteği (OAuth, bireysel hesap) | ✅ |
| CLD-02 | FTP/FTPS desteği (FluentFTP) | ✅ |
| CLD-03 | SFTP desteği (SSH.NET, 256 KB buffer) | ✅ |
| CLD-04 | UNC ağ paylaşımı desteği | ✅ |
| CLD-05 | Resume özelliği (kesilen yüklemeler kaldığı yerden devam) | ✅ |
| CLD-06 | Üstel geri çekilme ile retry mekanizması | ✅ |
| CLD-07 | Toplu yükleme (UploadBatchToAllAsync — tüm dosyalar × tüm hedefler) | ✅ |
| CLD-08 | SHA-256 boyut eşleşmesi ile bütünlük doğrulama | ✅ |
| CLD-09 | Per-file ilerleme takibi (dosya adı + yüzde + hız + ETA) | ✅ |
| CLD-10 | İlerleme eventi throttle (250ms, maks 4 event/sn) | ✅ |
| CLD-11 | Google Drive çöp kutusu temizleme (klasör kapsamlı) | ✅ |
| CLD-12 | ~~Mega.io desteği~~ (kaldırıldı — v0.94.0) | ❌ |

### 4.5 Zamanlama ve Otomasyon

| ID | Gereksinim | Durum |
|----|-----------|-------|
| SCH-01 | Quartz.NET ile Cron tabanlı zamanlama | ✅ |
| SCH-02 | Çoklu plan desteği (paralel görev izleme) | ✅ |
| SCH-03 | Manuel tetikleme (tek tıkla yedekleme) | ✅ |
| SCH-04 | İptal desteği (CancellationToken tabanlı) | ✅ |

### 4.6 Retention (Saklama Politikası)

| ID | Gereksinim | Durum |
|----|-----------|-------|
| RET-01 | KeepLastN (son N yedeği sakla) | ✅ |
| RET-02 | DeleteOlderThanDays (N günden eski sil) | ✅ |
| RET-03 | Per-type retention (Full/Diff/Log/Files ayrı politikalar) | ✅ |
| RET-04 | Hazır şablonlar (Minimal, Standard, Extended, GFS) | ✅ |
| RET-05 | Yerel + Uzak (bulut) temizlik | ✅ |

### 4.7 Bildirim

| ID | Gereksinim | Durum |
|----|-----------|-------|
| NTF-01 | SMTP e-posta bildirimi (MailKit) | ✅ |
| NTF-02 | Konsolide bildirim (SQL + dosya + bulut tek e-posta) | ✅ |
| NTF-03 | Görev logu e-postaya dahil | ✅ |
| NTF-04 | ModernToast tray bildirimleri | ✅ |
| NTF-05 | Toast: başlatma, tamamlanma, hata, iptal, bağlantı durumu | ✅ |

### 4.8 Kullanıcı Arayüzü (WinForms)

| ID | Gereksinim | Durum |
|----|-----------|-------|
| UI-01 | System Tray uygulaması (arka planda çalışma) | ✅ |
| UI-02 | Dashboard: son yedeklemeler (CollapsibleGroupPanel, plan bazlı) | ✅ |
| UI-03 | Plan düzenleme wizard'ı (5 adım, sekme navigasyonu) | ✅ |
| UI-04 | RichTextBox log paneli (renkli, per-plan buffer) | ✅ |
| UI-05 | İlerleme çubuğu (ağırlıklı model: SQL + Dosya + Bulut fazları) | ✅ |
| UI-06 | Dark / Light tema desteği (ModernTheme) | ✅ |
| UI-07 | Dark mode scrollbar (DarkMode_Explorer Win32 theme) | ✅ |
| UI-08 | Animasyonlu tray ikonu (yedekleme sırasında + tamamlanma) | ✅ |
| UI-09 | Çoklu dil: Türkçe (tr-TR) + İngilizce (en-US) | ✅ |
| UI-10 | Hakkında formu (versiyon, lisans, açık kaynak atıfları) | ✅ |

### 4.9 Windows Service

| ID | Gereksinim | Durum |
|----|-----------|-------|
| SVC-01 | Windows Service olarak arka plan çalışma (Topshelf) | ✅ |
| SVC-02 | Named Pipe IPC (JSON, newline-delimited) | ✅ |
| SVC-03 | Service ↔ Tray tam event senkronizasyonu | ✅ |
| SVC-04 | Tray'den servis başlat/durdur/yeniden başlat | ✅ |

### 4.10 Güvenlik

| ID | Gereksinim | Durum |
|----|-----------|-------|
| SEC-01 | DPAPI + Base64 ile şifre saklama | ✅ |
| SEC-02 | SQL + Windows Authentication | ✅ |
| SEC-03 | 7z AES-256 şifreli arşivler | ✅ |
| SEC-04 | Log'larda PII/şifre/token maskeleme | ✅ |
| SEC-05 | Plan bazlı şifre koruması | ✅ |

### 4.11 Güncelleme ve Dağıtım

| ID | Gereksinim | Durum |
|----|-----------|-------|
| UPD-01 | GitHub Releases üzerinden otomatik güncelleme kontrolü | ✅ |
| UPD-02 | InnoSetup installer (Tray + Service bileşen seçimi) | ✅ |
| UPD-03 | .NET 10 Desktop Runtime otomatik kurulumu (gömülü) | ✅ |
| UPD-04 | GitHub Actions CI/CD (tag push → build → release) | ✅ |
| UPD-05 | Lisans sözleşmesi + sorumluluk reddi sayfaları | ✅ |

---

## 5. Fonksiyonel Olmayan Gereksinimler

| Kategori | Gereksinim | Detay |
|----------|-----------|-------|
| **Platform** | Windows 10+ (x64) | .NET 10 Desktop Runtime |
| **SQL Uyumu** | SQL Server 2016+ | SMO 171.30.0 |
| **Performans** | Büyük dosya transfer | 1 MB buffer, Google Drive 10 MB chunk |
| **Performans** | UI thread yükü | İlerleme eventi throttle (250ms, maks 4/sn) |
| **Performans** | SFTP throughput | 256 KB buffer, 1 MB FileStream |
| **Güvenilirlik** | Upload resume | UploadStateManager ile kesinti kurtarma |
| **Güvenilirlik** | Retry | Üstel geri çekilme (max 3 deneme) |
| **Gözlemlenebilirlik** | Yapısal loglama | Serilog rolling file, 30 gün saklama |
| **Gözlemlenebilirlik** | Per-plan log buffer | Plan seçiminde ilgili logları gösterme |
| **Bakım** | Partial class | 12 büyük dosya 30+ partial'a ayrılmış |
| **Test** | Unit test | MSTest, 56+ test |

---

## 6. Teknoloji Stack

| Katman | Teknoloji | Versiyon | Kullanım |
|--------|-----------|----------|----------|
| Runtime | .NET | 10.0 | Hedef framework |
| UI | WinForms | .NET 10 | System Tray + Dashboard |
| Zamanlama | Quartz.NET | 3.8.1 | Cron tabanlı job scheduling |
| SQL | SMO | 171.30.0 | SQL Server backup/restore |
| Sıkıştırma | SevenZipSharp | 1.6.2.24 | LZMA2 sıkıştırma + AES-256 |
| VSS | AlphaVSS | 1.4.0 | Volume Shadow Copy |
| Bulut | Google.Apis.Drive.v3 | — | Google Drive API |
| Bulut | FluentFTP | 51.0.0 | FTP/FTPS transfer |
| Bulut | SSH.NET | 2024.1.0 | SFTP transfer |
| E-posta | MailKit | 4.3.0 | SMTP bildirimi |
| DI | Autofac | 8.1.1 | IoC container |
| Service | Topshelf | 4.3.0 | Windows Service hosting |
| Loglama | Serilog | 3.1.1 | Yapısal loglama |
| Serileştirme | Newtonsoft.Json | — | Pipe IPC + config dosyaları |
| Installer | InnoSetup | 6.2+ | Windows installer |
| CI/CD | GitHub Actions | — | Otomatik build + release |

---

## 7. Veri Modeli

### 7.1 Temel Modeller

```
BackupPlan
├── PlanId (string, GUID)
├── PlanName (string)
├── SqlConnection (SqlConnectionConfig)
│   ├── ServerName, Authentication, Username, Password (DPAPI)
│   └── Databases[] (seçili DB listesi)
├── BackupType (Full / Differential / Incremental)
├── Schedule (CronExpression)
├── CompressionSettings
│   ├── IsEnabled, Level, Password (DPAPI)
│   └── Algorithm (LZMA2)
├── CloudTargets[] (CloudTargetConfig)
│   ├── Type (GoogleDrive / Ftp / Sftp / LocalNetwork)
│   ├── DisplayName, IsEnabled
│   └── Provider-specific settings
├── Retention (RetentionPolicy)
│   ├── Mode (KeepLastN / DeleteOlderThanDays)
│   └── Value (int)
├── RetentionScheme (RetentionScheme, nullable)
│   ├── SqlFull, SqlDifferential, SqlLog, FileBackup
│   └── Per-type KeepLastN/DeleteOlderThan
├── FileBackup (FileBackupConfig)
│   └── Sources[] (dosya/klasör yolları)
└── Notifications (NotificationConfig)
    ├── SmtpProfile, Recipients
    └── ToastEnabled
```

### 7.2 Event Modeli (BackupActivityEventArgs)

```
BackupActivityEventArgs
├── PlanId, PlanName
├── ActivityType (Started|DatabaseProgress|StepChanged|CloudUploadStarted|
│                 CloudUploadProgress|CloudUploadCompleted|CloudUploadAbandoned|
│                 Completed|Failed|Cancelled)
├── DatabaseName, CurrentIndex, TotalCount
├── CloudTargetName, CloudTargetIndex, CloudTargetTotal
├── CloudFileName, CloudFileIndex, CloudFileTotal
├── ProgressPercent, BytesSent, BytesTotal, SpeedBytesPerSecond
├── IsSuccess, Message
├── HasFileBackup, HasCloudTargets
├── RemoteFileSizeBytes, LocalFileSizeBytes, IsIntegrityVerified
├── AbandonedFiles[], ToastEnabled
```

---

## 8. Dağıtım Modeli

### 8.1 Installer Bileşenleri

| Bileşen | Açıklama | Seçim |
|---------|----------|-------|
| Tray Uygulaması | System Tray'de çalışır, UI sağlar | Varsayılan (zorunlu) |
| Windows Service | Arka plan yedekleme motoru | İsteğe bağlı |
| .NET 10 Runtime | Gömülü runtime installer (~57 MB) | Otomatik (eksikse) |

### 8.2 Kurulum Tipleri

- **Tam Kurulum:** Tray + Service
- **Sadece Tray:** UI uygulaması (kullanıcı oturumunda çalışır)
- **Sadece Service:** Arka plan motoru (oturum kapatılsa da çalışır)
- **Özel:** Kullanıcı seçimi

### 8.3 CI/CD Pipeline

```
[Git Tag Push: vX.Y.Z]
      │
      ▼
[GitHub Actions]
├── dotnet publish (Win + Service)
├── InnoSetup compile (ISCC.exe)
├── GitHub Release oluştur
└── Installer (.exe) + portable (.zip) yükle
```

---

## 9. Bilinen Kısıtlamalar

| Kısıt | Açıklama |
|-------|----------|
| Sadece Windows | .NET 10 WinForms, Windows Service, VSS — cross-platform değil |
| Sadece x64 | 64-bit Windows gerekli |
| SQL Server 2016+ | SMO uyumluluğu |
| Google Drive bireysel | Workspace/Enterprise hesaplar desteklenmiyor |
| Tek makine | Merkezi yönetim konsolu yok |

---

## 10. Gelecek Yol Haritası (Post v1.0)

| Öncelik | Özellik | Açıklama |
|---------|---------|----------|
| Yüksek | v1.0 Kararlı Sürüm | Beta'dan çıkış, tüm bilinen hatalar giderilmiş |
| Orta | Azure Blob Storage | Kurumsal bulut desteği |
| Orta | AWS S3 | Amazon bulut desteği |
| Orta | OneDrive | Microsoft kişisel/iş bulut desteği |
| Düşük | Merkezi Yönetim | Çoklu makine tek panelden yönetim |
| Düşük | PostgreSQL / MySQL | SQL Server dışı veritabanı desteği |
| Düşük | Linux Service | .NET Worker Service ile cross-platform |

---

## 11. Versiyon Geçmişi (Özet)

| Versiyon | Tarih | Milestone |
|----------|-------|-----------|
| 0.87.0 | 2026-06-22 | DevExpress ikonları, animasyonlu tray |
| 0.88.0 | 2026-06-22 | Konsolide e-posta bildirimi |
| 0.91.0 | 2026-06-26 | Partial class refactoring (30+ dosya) |
| 0.92.0 | 2026-06-27 | Konsolide bulut yükleme (tek fazda toplu upload) |
| 0.93.0 | 2026-06-27 | Paralel görev UI, Express sıkıştırma |
| 0.94.0 | 2026-04-07 | Mega.io kaldırma, CLAUDE.md |
| 0.95.0 | 2026-04-07 | SQL yetki kontrolü, grup başlıkları |
| 0.96.0 | 2026-04-08 | ObjectListView geçişi |
| 0.98.0 | 2026-04-08 | Hakkında formu |
| 0.99.0 | 2026-04-08 | Telif hakkı, açık kaynak atıfları |
| 0.99.5 | 2026-04-09 | Per-type retention şablonları |
| 0.99.14 | 2026-04-11 | Dosya yedekleme zamanlama düzeltmesi |
| 0.99.23 | 2025-07-14 | .NET Runtime otomatik kurulum |
| 0.99.27 | 2025-07-15 | Dark tema checkbox, özel uygulama ikonu |
| 0.99.30 | 2025-07-16 | Google Drive/SFTP hız optimizasyonu |
| 0.99.33 | 2025-07-17 | Pipe protokolü bulut dosya bilgisi düzeltmesi |
| 0.99.35 | 2025-07-17 | Installer simge düzeltmesi |

---

*Bu doküman proje kaynak kodundan, CHANGELOG.md ve README.md dosyalarından otomatik olarak derlenmiştir.*
