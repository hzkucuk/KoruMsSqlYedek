# Özellikler & Geliştirme Yol Haritası

Bu dosya, MikroSqlDbYedek projesinin mevcut ve planlanan özelliklerini fazlar halinde listeler.

---

## Faz Durumu

| Faz | Açıklama | Durum |
|-----|----------|-------|
| **Faz 1** | Altyapı — Solution, NuGet, Referanslar, Build | ✅ Tamamlandı |
| **Faz 2** | Proje Dokümantasyonu & Versiyon Yönetimi | ✅ Tamamlandı |
| **Faz 3** | Core/Engine Servis Detay İmplementasyonu | ✅ Tamamlandı |
| **Faz 3.5** | Test Altyapısı & Unit Test'ler (Moq + FluentAssertions) | ✅ Tamamlandı |
| **Faz 4** | Cloud Provider — FTP/SFTP | ✅ Tamamlandı |
| **Faz 5** | Cloud Provider — Local/UNC Ağ Paylaşımı | ✅ Tamamlandı |
| **Faz 6** | Cloud Provider — Google Drive | ✅ Tamamlandı |
| **Faz 7** | Cloud Provider — OneDrive | ✅ Tamamlandı |
| **Faz 8** | Cloud Upload Orchestrator Entegrasyonu | ✅ Tamamlandı |
| **Faz 9** | Win UI — Tray App Altyapısı | ✅ Tamamlandı |
| **Faz 10** | Win UI — Plan Yönetim Formları | ✅ Tamamlandı |
| **Faz 11** | Win UI — Dashboard, Log, Ayarlar | ✅ Tamamlandı |
| **Faz 12** | Autofac IoC Container Entegrasyonu | ✅ Tamamlandı |
| **Faz 13** | Lokalizasyon (Resx: tr-TR, en-US) | ✅ Tamamlandı |
| **Faz 14** | Unit Test Genişletme | ✅ Tamamlandı |
| **Faz 15** | Inno Setup Dağıtım Paketi | ✅ Tamamlandı |
| **Faz 16** | WinForms UI Modernizasyon | ✅ Tamamlandı |
| **Faz 17** | .NET 10 Migrasyonu | ✅ Tamamlandı |

---

## Faz 1 — Altyapı ✅

- [x] 5 proje oluşturuldu (Core, Engine, Win, Service, Tests)
- [x] Solution dosyası (.slnx) yapılandırıldı
- [x] Tüm NuGet paketleri eklendi (14+ paket)
- [x] Proje referansları yapılandırıldı
- [x] Core modelleri ve interface'ler oluşturuldu
- [x] Engine servisleri iskelet olarak oluşturuldu
- [x] Service Topshelf host yapılandırıldı
- [x] Tests projesi temel testler ile oluşturuldu
- [x] Derleme hataları düzeltildi — 0 hata ile build

## Faz 2 — Proje Dokümantasyonu & Versiyon Yönetimi ✅

- [x] README.md oluşturuldu
- [x] CHANGELOG.md oluşturuldu (v0.1.0)
- [x] FEATURES.md oluşturuldu (bu dosya)
- [x] INSTALL.md oluşturuldu
- [x] AssemblyInfo.cs versiyon senkronu (0.1.0.0)
- [x] Win.csproj ApplicationVersion güncellemesi

## Faz 3 — Core/Engine Servis Detay İmplementasyonu ✅

- [x] ICloudUploadOrchestrator arayüzü oluşturuldu
- [x] IBackupHistoryManager arayüzü oluşturuldu
- [x] PlanManager: JSON şema migration (v1→v2), ValidatePlan, DeserializeAndMigrate
- [x] BackupChainValidator: HasValidLogChain, GetDifferentialCount, GetIncrementalCount, GetLastFullBackupDate
- [x] SevenZipCompressionService: CompressionLevel mapping, CompressMultipleAsync, CompressDirectoryAsync, 7z.dll fallback
- [x] BackupJobExecutor: Tam pipeline (SQL→Verify→Compress→Cloud→Retention→History→Notify), per-step error isolation
- [x] BackupHistoryManager: Günlük JSON dosyaları, thread-safe, plan/tarih sorgu, 90 gün temizlik
- [x] EmailNotificationService: Zengin HTML e-posta (boyut/oran/doğrulama/bulut), NotifyFileBackupAsync
- [x] CloudUploadOrchestrator: ICloudUploadOrchestrator implementasyonu

## Faz 3.5 — Test Altyapısı & Unit Test'ler ✅

- [x] Moq 4.20.72 ve FluentAssertions 6.12.2 NuGet paketleri eklendi
- [x] TestDataFactory yardımcı sınıfı (CreateValidPlan, CreateSuccessResult, CreateFailedResult, CreatePlanWithCloudTargets, CreatePlanWithFileBackup)
- [x] BackupJobExecutorTests — 8 test (pipeline, mock tüm bağımlılıklar, hata izolasyonu)
- [x] CloudUploadOrchestratorTests — 7 test (retry, provider not found, cancellation, mixed results)
- [x] PlanManagerTests genişletildi — 10 test (CRUD, schema migration v1→v2, export/import)
- [x] BackupChainValidatorTests — 16 test (chain integrity, promote to full, diff/incremental count)
- [x] BackupHistoryManagerTests — 8 test (save/query, date range, maxRecords, failed persistence)
- [x] RetentionCleanupServiceTests — 8 test (KeepLastN, DeleteOlderThanDays, Both, .7z, multi-DB)
- [x] Toplam: 65 test, %100 başarılı

## Faz 4 — Cloud Provider: FTP/SFTP ✅

- [x] FTP/FTPS upload (FluentFTP AsyncFtpClient, AutoConnect, IProgress<FtpProgress>)
- [x] SFTP upload (SSH.NET SftpClient, progress callback, EnsureDirectoryExists)
- [x] DeleteAsync — FTP ve SFTP uzak dosya silme
- [x] TestConnectionAsync — bağlantı testi (10s timeout)
- [x] Upload sonrası checksum doğrulama (FTP: MD5, SFTP: boyut karşılaştırma)
- [x] DPAPI şifre çözme entegrasyonu (PasswordProtector)
- [x] FTPS encryption desteği (Explicit TLS)
- [x] Uzak klasör otomatik oluşturma (FTP + SFTP)
- [x] Retry politikası: CloudUploadOrchestrator üzerinden (3 deneme, exponential backoff)
- [x] FtpSftpProviderTests — 13 test (validation, unreachable host, provider info)
- [x] Toplam: 78 test, %100 başarılı

## Faz 5 — Cloud Provider: Local/UNC ✅

- [x] LocalNetworkProvider tam implementasyonu (buffered async kopyalama)
- [x] UNC path kimlik bilgisi ile erişim (WNetAddConnection2 P/Invoke)
- [x] UncNetworkConnection helper sınıfı (IDisposable, otomatik disconnect)
- [x] DPAPI şifre çözme entegrasyonu (PasswordProtector)
- [x] Dosya kopyalama ilerleme raporlama (IProgress<int>, %0-100)
- [x] Kopyalama sonrası dosya boyutu doğrulama
- [x] TestConnectionAsync yazma izni kontrolü (temp dosya oluştur/sil)
- [x] CancellationToken desteği tüm operasyonlarda
- [x] Hedef dizin otomatik oluşturma (derin dizin desteği)
- [x] BackupHistoryManager: Test izolasyonu için custom directory desteği
- [x] LocalNetworkProviderTests — 18 test (constructor, upload, delete, testConnection, progress, UNC)
- [x] BackupHistoryManagerTests düzeltildi (temp dizin izolasyonu)
- [x] Toplam: 96 test, %100 başarılı

## Faz 6 — Cloud Provider: Google Drive ✅

- [x] OAuth2 kimlik doğrulama akışı (GoogleDriveAuthHelper — GoogleWebAuthorizationBroker, token refresh, NullDataStore)
- [x] Bireysel hesap desteği (GoogleDrivePersonal)
- [x] Google Workspace desteği (GoogleDriveWorkspace)
- [x] Dosya upload (resumable upload — 1 MB chunk, IProgress<int> raporlama)
- [x] Klasör yönetimi (ID veya yol ile, iç içe klasör oluşturma)
- [x] Eski dosya silme + çöp kutusu kalıcı temizleme (PermanentDeleteFromTrash)
- [x] Quota ve bağlantı kontrolü (About.Get — kullanıcı bilgisi, depolama kotası)
- [x] DPAPI entegrasyonu (OAuthClientSecret, OAuthTokenJson şifreli saklama)
- [x] CloudTargetConfig: OAuthClientId, OAuthClientSecret alanları eklendi
- [x] GoogleDriveProviderTests — 17 test (constructor, upload validation, delete, testConnection, AuthHelper)
- [x] Toplam: 113 test, %100 başarılı

## Faz 7 — Cloud Provider: OneDrive ✅

- [x] Microsoft Graph API entegrasyonu (Microsoft.Graph 5.62.0 + Microsoft.Identity.Client 4.67.2)
- [x] Bireysel (MSA) hesap desteği (OneDrivePersonal — consumers authority)
- [x] Kurumsal (Entra ID) hesap desteği (OneDriveBusiness — common authority)
- [x] MSAL token yönetimi (OneDriveAuthHelper — PublicClientApplication, AcquireTokenSilent/Interactive, cache serialization)
- [x] Large file upload (LargeFileUploadTask — 3.125 MiB chunk, 320 KiB katı, IProgress<long> raporlama)
- [x] Klasör yönetimi (yol ile iç içe klasör oluşturma, ItemWithPath API)
- [x] Eski dosya silme + PermanentDelete (ODataError 404 fallback ile)
- [x] Bağlantı ve quota kontrolü (/me/drive — sahip, kullanılan/toplam alan)
- [x] DPAPI entegrasyonu (MSAL cache Base64 + PasswordProtector)
- [x] OneDriveProviderTests — 21 test (constructor 4, upload validation 5, delete 1, testConnection 3, AuthHelper 4, ValidateConfig 4)
- [x] Toplam: 134 test, %100 başarılı

## Faz 8 — Cloud Upload Orchestrator Entegrasyonu ✅

- [x] ICloudProviderFactory arayüzü (Core — CreateProvider, IsSupported)
- [x] CloudProviderFactory implementasyonu (Engine — 9 CloudProviderType → 4 concrete provider mapping)
- [x] CloudUploadOrchestrator genişletildi: Factory constructor + provider listesi constructor (geriye uyumluluk)
- [x] GetProvider: Lazy caching — factory ile oluşturulan provider'lar dictionary'de cache'lenir
- [x] DeleteFromAllAsync: Tüm aktif hedeflerden dosya silme (retention temizliği için)
- [x] TestAllConnectionsAsync: Tüm aktif hedeflerin bağlantı testi (UI'den kullanım için)
- [x] CloudDeleteResult ve CloudConnectionTestResult modelleri eklendi
- [x] CloudProviderFactoryTests — 13 test (9 tür mapping, IsSupported, invalid type, instance uniqueness)
- [x] CloudUploadOrchestratorTests genişletildi — 21 test (7 mevcut + 14 yeni: factory constructor, cache, DeleteFromAllAsync, TestAllConnectionsAsync)
- [x] Toplam: 159 test, %100 başarılı

## Faz 9 — Win UI: Tray App Altyapısı ✅

- [x] TrayApplicationContext: ApplicationContext tabanlı tray uygulaması (NotifyIcon + ContextMenuStrip)
- [x] Mutex ile tek instance (Global Mutex, WM_SHOWFIRSTINSTANCE broadcast ile mevcut instance aktivasyonu)
- [x] Balloon tip bildirimleri (ShowBalloonTip helper, durum bazlı ikon güncelleme)
- [x] Tray menüsü: Dashboard Aç (bold), Planlar, Manuel Yedekleme, Log Görüntüle, Ayarlar, Çıkış (onay dialogu)
- [x] Program.cs: Serilog file sink (rolling daily, 30 gün), Application.ThreadException + AppDomain.UnhandledException global handler
- [x] MainDashboardForm: TLP layout — durum özeti (3x2), son yedeklemeler ListView (6 kolon), StatusStrip (durum + versiyon)
- [x] Dashboard kapatma davranışı: UserClosing → Hide (tray'de kal), Application kapanma → Close
- [x] SymbolIconHelper: Segoe MDL2 Assets / Segoe UI Symbol fontundan Icon ve Bitmap render (TrayIconStatus enum ile durum bazlı)
- [x] NativeMethods: DestroyIcon, SetForegroundWindow, ShowWindow, RegisterWindowMessage P/Invoke
- [x] Win.csproj: Serilog PackageReference, RestoreProjectStyle, RuntimeIdentifier eklendi
- [x] Toplam: 159 test, %100 başarılı (mevcut testler korundu)

## Faz 10 — Win UI: Plan Yönetim Formları ✅

- [x] PlanListForm: DataGridView ile plan listesi (7 kolon: Aktif, Plan Adı, Strateji, Veritabanları, Zamanlama, Bulut, Oluşturulma)
- [x] PlanListForm: ToolStrip — Yeni Plan, Düzenle, Sil (onay dialogu), Dışa Aktar, İçe Aktar, Yenile
- [x] PlanEditForm: 8 sekmeli TabControl ile tam BackupPlan düzenleme
  - Genel (plan adı, aktif, yerel yol)
  - SQL Bağlantı (sunucu, auth mode, credentials, timeout, bağlantı testi, veritabanı listeleme)
  - Strateji (tür, cron zamanlamaları, auto promote, verify)
  - Sıkıştırma (algoritma, seviye, arşiv şifresi)
  - Bulut Hedefler (ListView ile add/edit/remove)
  - Saklama (retention türü, KeepLastN, DeleteOlderThanDays)
  - Bildirimler (e-posta SMTP ayarları, toast toggle)
  - Dosya Yedekleme (toggle, zamanlama, kaynak ListView ile add/edit/remove)
- [x] CloudTargetEditDialog: Provider türüne göre dinamik alan görünürlüğü (FTP/SFTP, OAuth, Yerel/UNC grupları)
- [x] FileBackupSourceEditDialog: Kaynak adı, dizin yolu, include/exclude pattern, recursive, VSS toggle
- [x] SQL bağlantı test butonu (async TestConnectionAsync + otomatik veritabanı listeleme)
- [x] DPAPI şifre koruması (PasswordProtector.Protect) — SQL, arşiv, SMTP, bulut şifreleri
- [x] TrayApplicationContext: Planlar menüsü → PlanListForm entegrasyonu (tek instance)
- [x] Win.csproj: SDK-style'a dönüştürüldü (PackageReference uyumluluğu için)
- [x] Toplam: 159 test, %100 başarılı (mevcut testler korundu)

## Faz 11 — Win UI: Dashboard, Log, Ayarlar ✅

- [x] MainDashboardForm: Canlı veri yükleme (PlanManager + BackupHistoryManager)
  - Son 50 yedekleme geçmişi, renk kodlu ListView (başarılı=yeşil, kısmi=turuncu, hata=kırmızı)
  - 30 saniyelik otomatik yenileme Timer
  - Durum özeti: Aktif plan sayısı, son yedek durumu, format yardımcıları
- [x] LogViewerForm: Serilog dosya okuyucu
  - Regex tabanlı çok satırlı giriş ayrıştırma
  - Seviye ve metin filtreleme (VRB/DBG/INF/WRN/ERR/FTL)
  - 5 saniyelik otomatik takip (auto-tail) Timer
  - Dışa aktarma (tab-separated txt), renk kodlu DataGridView (Consolas 9pt)
- [x] SettingsForm: 2 sekmeli (Genel + SMTP) ayar formu
  - Genel: Dil, başlangıç, tray, varsayılan yedek dizini, log/geçmiş saklama
  - SMTP: Sunucu, port, SSL, kimlik, gönderici, alıcı, test e-postası
  - AppSettings modeli (Core), IAppSettingsManager arayüzü (Core), AppSettingsManager (Engine)
  - JSON dosya kalıcılığı: %APPDATA%\MikroSqlDbYedek\Config\appsettings.json
- [x] ManualBackupDialog: Anlık yedekleme diyalogu
  - Plan ve veritabanı seçimi (CheckedListBox)
  - Yedek türü (Full/Differential/Incremental)
  - İlerleme çubuğu, durum etiketi, konsol tarzı log çıktısı
  - Async yürütme, iptal desteği (CancellationToken)
- [x] TrayApplicationContext: Tüm menü handler'ları bağlandı
  - Log Görüntüle → LogViewerForm (tek instance)
  - Ayarlar → SettingsForm (modal dialog)
  - Manuel Yedekleme → ManualBackupDialog (tek instance)
  - Cleanup/Dispose: Tüm form'lar düzgün kapatılıyor
- [x] Toplam: 159 test, %100 başarılı (mevcut testler korundu)

## Faz 12 — Autofac IoC Container ✅

- [x] Engine: EngineModule — tüm servis kayıtları (SingleInstance/InstancePerDependency)
- [x] Engine: AutofacJobFactory — Quartz.NET IJobFactory implementasyonu
- [x] Engine: QuartzSchedulerService — IJobFactory ctor desteği
- [x] Win: WinContainerBootstrap — EngineModule + tüm formlar
- [x] Win: TrayApplicationContext — ILifetimeScope ile form resolve
- [x] Win: MainDashboardForm, PlanListForm, PlanEditForm, ManualBackupDialog, SettingsForm — constructor injection
- [x] Win: Program.cs — container bootstrap entegrasyonu
- [x] Service: ServiceContainerBootstrap — EngineModule + BackupWindowsService
- [x] Service: Program.cs — container bootstrap entegrasyonu
- [x] Tüm `new XxxService()` doğrudan örnekleme kaldırıldı
- [x] 159 test başarılı — 0 hata

## Faz 13 — Lokalizasyon ✅

- [x] Resources.resx (en-US varsayılan) — 130+ string key tanımlandı
- [x] Resources.tr-TR.resx — Türkçe çeviriler tamamlandı
- [x] Res.cs yardımcı sınıf (ResourceManager wrapper: Get/Format)
- [x] Tüm UI string'lerin resource'a taşınması (10 form dosyası, 100+ replacement)
- [x] Dil değişikliği ayarı (Program.cs ApplyLanguageSetting — AppSettings.Language)
- [x] Kaynak anahtar doğrulaması — kod ↔ resx 0 uyumsuzluk
- [x] 159 test başarılı — 0 hata

## Faz 14 — Unit Test Genişletme ✅

- [x] BackupChainValidator testleri (16 test — Faz 3.5'te tamamlandı)
- [x] PlanManager CRUD testleri genişletme (10 test — Faz 3.5'te tamamlandı)
- [x] RetentionCleanupService testleri (8 test — Faz 3.5'te tamamlandı)
- [x] FileBackupService testleri — 21 test (mock VSS, fallback, pattern, recursive, progress, timestamps, sanitize)
- [x] CloudUploadOrchestrator testleri (7+14 test — Faz 3.5 + Faz 8'de tamamlandı)
- [x] EmailNotificationService testleri — 7 test (config null/disabled, filter logic, SMTP hata yutma)
- [x] AppSettingsManager testleri — 10 test (load defaults, roundtrip, corrupted JSON, SMTP, null guard)
- [x] TestDataFactory genişletildi — CreatePlanWithMultipleFileBackupSources, CreateFileBackupSource, CreateSuccessFileBackupResult
- [x] BackupJobExecutorTests (8 test — Faz 3.5'te tamamlandı)
- [x] BackupHistoryManagerTests (8 test — Faz 3.5'te tamamlandı)
- [x] Toplam: 197 test, %100 başarılı

## Faz 15 — Inno Setup Dağıtım ✅

- [x] MikroSqlDbYedek.iss script
- [x] Tray + Service birlikte kurulum
- [x] Service otomatik start kaydı
- [x] Güncelleme sırasında plan/log koruma
- [x] Build-Release.ps1 otomasyon scripti
- [x] Minimum gereksinim kontrolü (.NET 4.8, Windows 10+)


## Faz 17 — .NET 10 Migrasyonu (v0.16.0) ✅

- [x] Tüm projeler `net48` → `net10.0-windows` hedef çerçevesine yükseltildi
- [x] Topshelf → `Microsoft.Extensions.Hosting.WindowsServices` ile değiştirildi
- [x] `BackupWindowsService` → `IHostedService` implementasyonu
- [x] `ServiceContainerBootstrap` → `AutofacServiceProviderFactory` entegrasyonu
- [x] `System.Data.SqlClient` → `Microsoft.Data.SqlClient`
- [x] DPAPI için `System.Security.Cryptography.ProtectedData` paketi eklendi
- [x] `ImportWindowsDesktopTargets` ve `App.config` kaldırıldı
- [x] WFO1000 / NU1701 uyarıları bastırıldı
- [x] 0 hata ile build doğrulandı

---

## Faz 16 — WinForms UI Modernizasyon (v0.15.0) ✅

### 19 Theme Bilesen
- [x] ModernTheme, ModernCardPanel, ModernButton, StatusBadge, ModernToolStripRenderer, ModernFormBase
- [x] ModernTextBox, ModernToggleSwitch, ModernProgressBar, ModernTabControl
- [x] ModernToast, ModernSearchBox, ModernDivider
- [x] ModernGroupBox, ModernComboBox, ModernCheckBox, ModernNumericUpDown
- [x] ModernLoadingOverlay, ModernHeaderPanel

### Modernize Edilen Formlar
- [x] MainDashboardForm -- kart tabanli modern layout
- [x] PlanListForm -- DataGridView, ToolStrip, StatusStrip
- [x] PlanEditForm -- BackColor, Font, TabControl, buton paneli
- [x] LogViewerForm -- DataGridView + StatusStrip + Form tema
- [x] SettingsForm -- TabControl, tab sayfalar, buton paneli, Form tema
- [x] ManualBackupDialog -- BackColor, Font, buton paneli, ProgressBar
- [x] CloudTargetEditDialog -- BackColor, Font, Form tema
- [x] FileBackupSourceEditDialog -- BackColor, Font, ipucu rengi
