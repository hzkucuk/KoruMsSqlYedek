## [0.17.0] - 2026-03-28 — Faz 18: Koyu Tema (MikroUpdate.Win ile senkronizasyon)

### Değiştirilenler
- **ModernTheme.cs**: Renk paleti light → dark tema olarak güncellendi
  - Arka plan: RGB(245,245,248) → RGB(30,30,30)
  - Surface: White → RGB(40,40,40)
  - Accent: Mavi RGB(0,120,212) → Yeşil RGB(0,150,80)
  - Metin: Koyu → Açık RGB(230,230,230)
  - Grid renkleri koyu tonlara güncellendi
- **ModernToolStripRenderer.cs**: Hover overlay renkleri koyu tema için düzeltildi (mavi alpha → beyaz/yeşil alpha)

### Eklenenler
- **Theme/VersionSidebarRenderer.cs**: Tray menüsüne yeşil gradient sidebar + dikey versiyon metni (MikroUpdate.Win'den port)
- **TrayApplicationContext.cs**: Tray context menüsüne `VersionSidebarRenderer` uygulandı

### Etkilenen Dosyalar
- MikroSqlDbYedek.Win/Theme/ModernTheme.cs
- MikroSqlDbYedek.Win/Theme/ModernToolStripRenderer.cs
- MikroSqlDbYedek.Win/Theme/VersionSidebarRenderer.cs (yeni)
- MikroSqlDbYedek.Win/TrayApplicationContext.cs
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs

---

## [0.16.0] - 2026-03-28 — Faz 17: .NET 10 Migrasyonu

### Değiştirilenler
- Tüm projeler `net48` → `net10.0-windows` hedef çerçevesine yükseltildi (Core, Engine, Win, Service, Tests)
- **Windows Service host:** Topshelf 4.3.0 kaldırıldı → `Microsoft.Extensions.Hosting.WindowsServices 10.0.0` ile değiştirildi
- `BackupWindowsService`: `Start()/Stop()` → `IHostedService.StartAsync/StopAsync` implementasyonu
- `Program.cs` (Service): `HostFactory.Run(...)` → `Host.CreateDefaultBuilder(...).UseWindowsService(...)`
- `ServiceContainerBootstrap`: `Build()` → `Configure(ContainerBuilder)` (Autofac.Extensions.DependencyInjection entegrasyonu)
- `SqlBackupService`: `System.Data.SqlClient` → `Microsoft.Data.SqlClient`
- `MikroSqlDbYedek.Win.csproj`: `ImportWindowsDesktopTargets` kaldırıldı, `App.config` exclude edildi
- WFO1000 (WinForms designer serileştirme) uyarısı bastırıldı

### Eklenenler
- `System.Security.Cryptography.ProtectedData 9.0.0` — DPAPI .NET 10 desteği (Core)
- `Microsoft.Data.SqlClient 6.0.2` — Modern SQL client (Engine)
- `System.ServiceProcess.ServiceController 9.0.0` — VSS servis kontrolü (Engine)
- `Autofac.Extensions.DependencyInjection 9.0.0` — Generic Host entegrasyonu (Service)
- `Microsoft.Extensions.Hosting 10.0.0` — Generic Host (Service)
- `Microsoft.Extensions.Hosting.WindowsServices 10.0.0` — Windows Service hosting (Service)
- `Serilog.Extensions.Hosting 8.0.0` — Serilog/Host entegrasyonu (Service)
- `AlphaVSS 1.4.0` NU1701 uyarısı bastırıldı (Engine — runtime uyumlu)

### Kaldırılanlar
- `Topshelf 4.3.0` — .NET Core/5+ desteklemiyor
- `Topshelf.Serilog 4.3.0`
- Core: gereksiz `<Reference Include="System.Security" />` ve `<Reference Include="System.ServiceProcess" />` tagları

### Etkilenen Dosyalar
- MikroSqlDbYedek.Core/MikroSqlDbYedek.Core.csproj
- MikroSqlDbYedek.Engine/MikroSqlDbYedek.Engine.csproj
- MikroSqlDbYedek.Service/MikroSqlDbYedek.Service.csproj
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj
- MikroSqlDbYedek.Tests/MikroSqlDbYedek.Tests.csproj
- MikroSqlDbYedek.Service/Program.cs
- MikroSqlDbYedek.Service/BackupWindowsService.cs
- MikroSqlDbYedek.Service/IoC/ServiceContainerBootstrap.cs
- MikroSqlDbYedek.Engine/Backup/SqlBackupService.cs
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs

---

## [0.15.0] - 2026-03-28
### Added
- Faz 16: 19 ozel tema bileşeni (ModernTheme, ModernCardPanel, ModernButton, StatusBadge, ModernToolStripRenderer, ModernFormBase, ModernTextBox, ModernToggleSwitch, ModernProgressBar, ModernTabControl, ModernToast, ModernSearchBox, ModernDivider, ModernGroupBox, ModernComboBox, ModernCheckBox, ModernNumericUpDown, ModernLoadingOverlay, ModernHeaderPanel)
- 8 form modernize edildi: MainDashboardForm, PlanListForm, PlanEditForm, LogViewerForm, SettingsForm, ManualBackupDialog, CloudTargetEditDialog, FileBackupSourceEditDialog
- Tum formlar ModernTheme ile tutarli renk paleti, font ve kenarlık stiline kavustu

# Changelog

Tüm önemli değişiklikler bu dosyada belgelenir.
Format: [Semantic Versioning](https://semver.org/lang/tr/) — breaking=MAJOR, yeni özellik=MINOR, düzeltme=PATCH.

---

## [0.14.0] - 2025-07-22 — Faz 15: Inno Setup Dağıtım Paketi

### Eklenenler
- **MikroSqlDbYedek.iss** (Deployment/InnoSetup): Inno Setup kurulum scripti — .NET 4.8 ve Windows 10+ ön koşul kontrolü, bileşen seçimi (Tray+Service/ayrı), Topshelf entegrasyonu, Türkçe+İngilizce dil desteği, LZMA2 sıkıştırma
- **Build-Release.ps1** (Deployment): Otomasyon scripti — NuGet restore, build, test, publish, ZIP paketleme
- **install-service.cmd** (Deployment): Windows Service kurulum helper scripti (yönetici yetkisi kontrolü)
- **uninstall-service.cmd** (Deployment): Windows Service kaldırma helper scripti
- **setup_readme.txt** (Deployment/InnoSetup): Kurulum bilgi dosyası

### Değiştirilenler
- **INSTALL.md**: Üretim kurulumu bölümü güncellendi — Inno Setup ve Build-Release.ps1 kullanım talimatları eklendi

### Etkilenen Dosyalar
- Deployment/Build-Release.ps1 (yeni)
- Deployment/InnoSetup/MikroSqlDbYedek.iss (yeni)
- Deployment/InnoSetup/setup_readme.txt (yeni)
- Deployment/install-service.cmd (yeni)
- Deployment/uninstall-service.cmd (yeni)
- INSTALL.md (güncelleme)
- FEATURES.md (güncelleme)

---

## [0.13.0] - 2025-07-22 — Faz 14: Unit Test Genişletme

### Eklenenler
- **FileBackupServiceTests** (Tests): 21 test — mock VSS flow, fallback to direct copy, include/exclude pattern, recursive, progress, timestamps, sanitize
- **EmailNotificationServiceTests** (Tests): 7 test — config null/disabled, notification filter logic, SMTP hata yutma davranışı
- **AppSettingsManagerTests** (Tests): 10 test — load defaults, save/load roundtrip, corrupted/empty/null JSON, SMTP settings, null guard, overwrite
- **TestDataFactory** genişletildi: CreatePlanWithMultipleFileBackupSources, CreateFileBackupSource, CreateSuccessFileBackupResult

### Etkilenen Dosyalar
- MikroSqlDbYedek.Tests/FileBackupServiceTests.cs (yeni)
- MikroSqlDbYedek.Tests/EmailNotificationServiceTests.cs (yeni)
- MikroSqlDbYedek.Tests/AppSettingsManagerTests.cs (yeni)
- MikroSqlDbYedek.Tests/Helpers/TestDataFactory.cs (güncelleme)

### Test İstatistikleri
- Önceki: 159 test
- Eklenen: 38 yeni test (21 + 7 + 10)
- Toplam: 197 test, %100 başarılı

---

## [0.12.0] - 2025-07-22 — Faz 13: Lokalizasyon (Resx: tr-TR, en-US)

### Eklenenler
- **Resources.resx** (Win/Properties): en-US varsayılan kaynak dosyası — 130+ UI string key
- **Resources.tr-TR.resx** (Win/Properties): Türkçe çeviriler (satellite assembly)
- **Res.cs** (Win/Helpers): ResourceManager wrapper — `Get(key)` ve `Format(key, args)` yardımcı metotları
- **ApplyLanguageSetting()** (Win/Program.cs): AppSettings.Language ayarına göre CurrentUICulture/CurrentCulture ayarlar

### Değişenler
- **TrayApplicationContext**: Menü öğeleri, balloon tip, tooltip, çıkış onayı → Res.Get()
- **MainDashboardForm**: ApplyLocalization() eklendi, durum/tür/zaman string'leri → Res.Get()/Format()
- **PlanListForm**: ApplyLocalization() eklendi, strateji/CRUD/dışa-içe aktarma string'leri → Res.Get()/Format()
- **PlanEditForm**: Combo öğeleri, doğrulama, bağlantı testi string'leri → Res.Get()/Format()
- **ManualBackupDialog**: İlerleme, durum, log string'leri → Res.Get()/Format()
- **SettingsForm**: Doğrulama, SMTP testi, kaydetme string'leri → Res.Get()/Format()
- **LogViewerForm**: Seviye adları, dışa aktarma, kayıt sayısı string'leri → Res.Get()/Format()
- **CloudTargetEditDialog**: Provider adları, doğrulama string'leri → Res.Get()
- **FileBackupSourceEditDialog**: Başlıklar, doğrulama string'leri → Res.Get()
- **Program.cs**: ApplyLanguageSetting() çağrısı + lokalize MessageBox string'leri

### Etkilenen Dosyalar
- MikroSqlDbYedek.Win/Properties/Resources.resx (yeni)
- MikroSqlDbYedek.Win/Properties/Resources.tr-TR.resx (yeni)
- MikroSqlDbYedek.Win/Helpers/Res.cs (yeni)
- MikroSqlDbYedek.Win/Program.cs (güncelleme)
- MikroSqlDbYedek.Win/TrayApplicationContext.cs (güncelleme)
- MikroSqlDbYedek.Win/MainDashboardForm.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/PlanListForm.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/ManualBackupDialog.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/SettingsForm.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/LogViewerForm.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/CloudTargetEditDialog.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/FileBackupSourceEditDialog.cs (güncelleme)

---

## [0.11.0] - 2025-07-21 — Faz 12: Autofac IoC Container Entegrasyonu

### Eklenenler
- **EngineModule** (Engine/IoC): Tüm Engine servislerini Autofac modülü olarak kaydeder (15+ servis — SingleInstance/InstancePerDependency)
- **AutofacJobFactory** (Engine/Scheduling): Quartz.NET IJobFactory implementasyonu — IJob'ları Autofac container'dan çözer
- **WinContainerBootstrap** (Win/IoC): Tray uygulaması için Autofac container yapılandırması — EngineModule + tüm formlar
- **ServiceContainerBootstrap** (Service/IoC): Windows Service için Autofac container yapılandırması — EngineModule + BackupWindowsService

### Değişenler
- **TrayApplicationContext**: ILifetimeScope ctor injection — tüm formlar container'dan çözümleniyor
- **MainDashboardForm**: IPlanManager + IBackupHistoryManager ctor injection
- **PlanListForm**: IPlanManager + ISqlBackupService ctor injection
- **PlanEditForm**: IPlanManager + ISqlBackupService ctor injection, inline new SqlBackupService() kaldırıldı
- **ManualBackupDialog**: IPlanManager + ISqlBackupService ctor injection
- **SettingsForm**: IAppSettingsManager ctor injection
- **QuartzSchedulerService**: Opsiyonel IJobFactory ctor parametresi eklendi
- **Win Program.cs**: WinContainerBootstrap.Build() + container.Resolve entegrasyonu
- **Service Program.cs**: ServiceContainerBootstrap.Build() + container.Resolve entegrasyonu (TODO kaldırıldı)

### Etkilenen Dosyalar
- MikroSqlDbYedek.Engine/IoC/EngineModule.cs (yeni)
- MikroSqlDbYedek.Engine/Scheduling/AutofacJobFactory.cs (yeni)
- MikroSqlDbYedek.Engine/Scheduling/QuartzSchedulerService.cs (güncelleme)
- MikroSqlDbYedek.Win/IoC/WinContainerBootstrap.cs (yeni)
- MikroSqlDbYedek.Win/Program.cs (güncelleme)
- MikroSqlDbYedek.Win/TrayApplicationContext.cs (güncelleme)
- MikroSqlDbYedek.Win/MainDashboardForm.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/PlanListForm.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/ManualBackupDialog.cs (güncelleme)
- MikroSqlDbYedek.Win/Forms/SettingsForm.cs (güncelleme)
- MikroSqlDbYedek.Service/IoC/ServiceContainerBootstrap.cs (yeni)
- MikroSqlDbYedek.Service/Program.cs (güncelleme)

---

## [0.10.0] - 2025-07-20 — Faz 11: Win UI — Dashboard, Log, Ayarlar

### Eklenenler
- **MainDashboardForm:** Canlı veri yükleme — son 50 yedek geçmişi (renk kodlu ListView), aktif plan sayısı, 30s otomatik yenileme Timer
- **LogViewerForm:** Serilog dosya okuyucu — regex ayrıştırma, seviye/metin filtreleme, auto-tail (5s), dışa aktarma, renk kodlu DataGridView (Consolas 9pt)
- **SettingsForm:** 2 sekmeli ayar formu (Genel + SMTP) — dil, başlangıç, tray, yedek dizini, log/geçmiş saklama, SMTP test e-postası
- **ManualBackupDialog:** Anlık yedekleme — plan/DB seçimi (CheckedListBox), yedek türü, ilerleme çubuğu, konsol log çıktısı, async yürütme, iptal desteği
- **AppSettings modeli** (Core): Dil, StartWithWindows, DefaultBackupPath, LogRetentionDays, HistoryRetentionDays, SmtpSettings
- **IAppSettingsManager** arayüzü (Core) + **AppSettingsManager** implementasyonu (Engine): JSON kalıcılık (%APPDATA%\Config\appsettings.json)
- **TrayApplicationContext:** Tüm menü handler'ları bağlandı (Log→LogViewerForm, Ayarlar→SettingsForm, Manuel Yedekleme→ManualBackupDialog)

### Etkilenen Dosyalar
- MikroSqlDbYedek.Core/Models/AppSettings.cs (yeni)
- MikroSqlDbYedek.Core/Interfaces/IAppSettingsManager.cs (yeni)
- MikroSqlDbYedek.Engine/AppSettingsManager.cs (yeni)
- MikroSqlDbYedek.Win/MainDashboardForm.cs (güncelleme — canlı veri)
- MikroSqlDbYedek.Win/Forms/LogViewerForm.cs + .Designer.cs (yeni)
- MikroSqlDbYedek.Win/Forms/SettingsForm.cs + .Designer.cs (yeni)
- MikroSqlDbYedek.Win/Forms/ManualBackupDialog.cs + .Designer.cs (yeni)
- MikroSqlDbYedek.Win/TrayApplicationContext.cs (menü handler'ları güncelleme)

---

## [0.9.0] - 2025-07-19 — Faz 10: Win UI — Plan Yönetim Formları

### Eklenenler
- **PlanListForm:** DataGridView ile plan listesi (7 kolon), ToolStrip CRUD (Yeni Plan, Düzenle, Sil, Dışa Aktar, İçe Aktar, Yenile), çift tıkla düzenleme, silme onay dialogu
- **PlanEditForm:** 8 sekmeli TabControl ile tam BackupPlan düzenleme — Genel, SQL Bağlantı, Strateji, Sıkıştırma, Bulut Hedefler, Saklama, Bildirimler, Dosya Yedekleme
- **CloudTargetEditDialog:** Provider türüne göre dinamik GroupBox görünürlüğü (FTP/SFTP, OAuth, Yerel/UNC), 9 CloudProviderType desteği, DPAPI şifre koruması
- **FileBackupSourceEditDialog:** Kaynak adı, dizin yolu, include/exclude pattern (noktalı virgül ayırıcı), recursive ve VSS toggle
- **TrayApplicationContext entegrasyonu:** Planlar menüsü → PlanListForm açma (tek instance pattern)

### Değişenler
- **Win.csproj:** Old-style csproj → SDK-style dönüşümü (NuGet PackageReference uyumluluğu için)
- PasswordProtector.Protect metod adı düzeltmesi (Encrypt → Protect)

### Etkilenen Dosyalar
- MikroSqlDbYedek.Win/Forms/PlanListForm.cs + .Designer.cs (yeni)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.cs + .Designer.cs (yeni)
- MikroSqlDbYedek.Win/Forms/CloudTargetEditDialog.cs + .Designer.cs (yeni)
- MikroSqlDbYedek.Win/Forms/FileBackupSourceEditDialog.cs + .Designer.cs (yeni)
- MikroSqlDbYedek.Win/TrayApplicationContext.cs (Planlar menüsü entegrasyonu)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (SDK-style dönüşümü)

---

## [0.8.0] - 2025-07-18 — Faz 9: Win UI — Tray App Altyapısı

### Eklenenler
- **TrayApplicationContext:** System Tray tabanlı ApplicationContext — NotifyIcon ile tray'de çalışma, ContextMenuStrip menü (Dashboard Aç, Planlar, Manuel Yedekleme, Log Görüntüle, Ayarlar, Çıkış), çift tıkla Dashboard açma, balloon tip bildirimleri, durum bazlı ikon güncelleme
- **Program.cs yeniden yapılandırma:** Global Mutex ile tek instance (WM_SHOWFIRSTINSTANCE broadcast ile mevcut instance aktivasyonu), Serilog file sink (rolling daily, 30 gün retention, %APPDATA%\MikroSqlDbYedek\Logs\), Application.ThreadException + AppDomain.UnhandledException global exception handler'ları
- **MainDashboardForm:** Dashboard iskeleti — TLP layout (başlık, 3x2 durum özeti paneli, "Son Yedeklemeler" GroupBox + ListView 6 kolon), StatusStrip (durum + versiyon), UserClosing → Hide (tray'de kalır)
- **SymbolIconHelper:** Segoe MDL2 Assets / Segoe UI Symbol fontundan Icon ve Bitmap render etme, TrayIconStatus enum (Idle/Running/Success/Warning/Error) ile durum bazlı tray ikonu
- **NativeMethods:** Win32 P/Invoke tanımları — DestroyIcon, SetForegroundWindow, ShowWindow, RegisterWindowMessage, SendMessage
- **Win.csproj güncellemeleri:** Serilog 3.1.1 + Serilog.Sinks.File 5.0.0 PackageReference, RestoreProjectStyle, RuntimeIdentifier=win

### Etkilenen Dosyalar
- MikroSqlDbYedek.Win/TrayApplicationContext.cs (yeni)
- MikroSqlDbYedek.Win/MainDashboardForm.cs + MainDashboardForm.Designer.cs (yeni)
- MikroSqlDbYedek.Win/Helpers/SymbolIconHelper.cs (yeni)
- MikroSqlDbYedek.Win/NativeMethods.cs (yeni)
- MikroSqlDbYedek.Win/Program.cs (yeniden yazıldı — Mutex, Serilog, TrayApplicationContext)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (NuGet, RuntimeIdentifier)

---

## [0.7.0] - 2025-07-18 — Faz 8: Cloud Upload Orchestrator Entegrasyonu

### Eklenenler
- **ICloudProviderFactory:** Bulut provider fabrika arayüzü — CloudProviderType'a göre ICloudProvider oluşturma ve desteklenirlik kontrolü
- **CloudProviderFactory:** Concrete factory — 9 CloudProviderType'ı 4 concrete provider'a (GoogleDrive, OneDrive, FtpSftp, LocalNetwork) eşleyen switch-based mapping
- **CloudUploadOrchestrator geliştirmeleri:**
  - Factory constructor: ICloudProviderFactory ile lazy provider oluşturma (ihtiyaç anında)
  - GetProvider caching: Factory ile oluşturulan provider'lar dictionary'de cache'lenir (aynı tür tekrar çağrıldığında yeniden oluşturulmaz)
  - DeleteFromAllAsync: Tüm aktif bulut hedeflerinden dosya silme — retention temizliği için
  - TestAllConnectionsAsync: Tüm aktif hedeflerin bağlantı testi — UI plan doğrulama için
  - Geriye uyumlu: Mevcut provider listesi constructor'ı korundu
- **CloudDeleteResult modeli:** ProviderType, DisplayName, IsSuccess, ErrorMessage
- **CloudConnectionTestResult modeli:** ProviderType, DisplayName, IsConnected, ErrorMessage
- **CloudProviderFactoryTests:** 13 test — 9 tür mapping, IsSupported (valid/invalid), invalid type exception, instance uniqueness
- **CloudUploadOrchestratorTests genişletildi:** 21 test (7 mevcut + 14 yeni — factory constructor, null factory, cache doğrulama, DeleteFromAllAsync success/error/disabled/not-found, TestAllConnectionsAsync success/error/disabled/not-found/multiple)

### Etkilenen Dosyalar
- MikroSqlDbYedek.Core/Interfaces/ICloudProviderFactory.cs (yeni)
- MikroSqlDbYedek.Core/Interfaces/ICloudUploadOrchestrator.cs (DeleteFromAllAsync, TestAllConnectionsAsync, CloudDeleteResult, CloudConnectionTestResult eklendi)
- MikroSqlDbYedek.Engine/Cloud/CloudProviderFactory.cs (yeni)
- MikroSqlDbYedek.Engine/Cloud/CloudUploadOrchestrator.cs (factory constructor, GetProvider, DeleteFromAllAsync, TestAllConnectionsAsync)
- MikroSqlDbYedek.Tests/CloudProviderFactoryTests.cs (yeni — 13 test)
- MikroSqlDbYedek.Tests/CloudUploadOrchestratorTests.cs (14 yeni test)

---

## [0.6.0] - 2026-03-27 — Faz 7: OneDrive Cloud Provider

### Eklenenler
- **OneDriveAuthHelper:** MSAL token yönetimi — PublicClientApplication, AcquireTokenSilent/Interactive, MSAL cache serialization (Base64), StaticAccessTokenProvider ile GraphServiceClient entegrasyonu
- **OneDriveProvider:** Tam ICloudProvider implementasyonu — resumable upload (LargeFileUploadTask, 3.125 MiB chunk, 320 KiB katları), iç içe klasör oluşturma (ItemWithPath API), PermanentDelete + ODataError 404 fallback, /me/drive ile bağlantı/quota testi
- **Bireysel + Kurumsal destek:** OneDrivePersonal (consumers authority) ve OneDriveBusiness (common authority)
- **MSAL entegrasyonu:** Microsoft.Identity.Client 4.67.2 NuGet paketi eklendi
- **DPAPI entegrasyonu:** MSAL cache verisi Base64 + PasswordProtector ile şifreli saklama
- **OneDriveProviderTests:** 21 test — constructor (4), upload validation (5), delete (1), testConnection (3), AuthHelper IsTokenValid (4), ValidateConfig (4)

### Etkilenen Dosyalar
- MikroSqlDbYedek.Engine/Cloud/OneDriveProvider.cs (tam implementasyon)
- MikroSqlDbYedek.Engine/Cloud/OneDriveAuthHelper.cs (yeni)
- MikroSqlDbYedek.Engine/MikroSqlDbYedek.Engine.csproj (Microsoft.Identity.Client eklendi)
- MikroSqlDbYedek.Tests/OneDriveProviderTests.cs (yeni — 21 test)

---

## [0.5.0] - 2026-03-27 — Faz 6: Google Drive Cloud Provider

### Eklenenler
- **GoogleDriveAuthHelper:** OAuth2 token yönetimi — GoogleWebAuthorizationBroker ile interaktif yetkilendirme, token refresh, NullDataStore (DPAPI tabanlı saklama), IsTokenValid kontrolü
- **GoogleDriveProvider:** Tam ICloudProvider implementasyonu — resumable upload (1 MB chunk), IProgress<int> ilerleme raporlama, klasör yönetimi (ID veya yol ile, iç içe oluşturma), kalıcı silme + çöp kutusu temizleme, About.Get ile bağlantı/quota testi
- **CloudTargetConfig genişletme:** OAuthClientId ve OAuthClientSecret alanları eklendi
- **Bireysel + Workspace desteği:** GoogleDrivePersonal ve GoogleDriveWorkspace provider türleri
- **DPAPI entegrasyonu:** OAuthClientSecret ve OAuthTokenJson şifreli saklama
- **GoogleDriveProviderTests:** 17 test — constructor (4), upload validation (5), delete (1), testConnection (3), AuthHelper IsTokenValid (4)

### Etkilenen Dosyalar
- MikroSqlDbYedek.Engine/Cloud/GoogleDriveProvider.cs (tam implementasyon)
- MikroSqlDbYedek.Engine/Cloud/GoogleDriveAuthHelper.cs (yeni)
- MikroSqlDbYedek.Core/Models/ConfigModels.cs (OAuthClientId, OAuthClientSecret eklendi)
- MikroSqlDbYedek.Tests/GoogleDriveProviderTests.cs (yeni — 17 test)

---

## [0.4.0] - 2026-03-27 — Faz 5: Local/UNC Ağ Paylaşımı Cloud Provider

### Eklenenler
- **UncNetworkConnection:** WNetAddConnection2/WNetCancelConnection2 P/Invoke ile UNC kimlik doğrulama (IDisposable)
- **Buffered async kopyalama:** 80KB buffer ile stream-based dosya kopyalama (File.Copy yerine)
- **İlerleme raporlama:** IProgress<int> ile %0-100 kopyalama ilerleme yüzdesi
- **Dosya boyutu doğrulama:** Kopyalama sonrası kaynak-hedef boyut karşılaştırma
- **Yazma izni kontrolü:** TestConnectionAsync — temp dosya oluştur/sil ile dizin erişim doğrulama
- **DPAPI şifre çözme:** UNC kimlik bilgileri için PasswordProtector entegrasyonu
- **Hedef dizin otomatik oluşturma:** Derin dizin yapısı desteği (Directory.CreateDirectory)
- **CancellationToken:** Tüm operasyonlarda iptal desteği
- **BackupHistoryManager:** Yapıcıya özel dizin parametresi (test izolasyonu)
- **LocalNetworkProviderTests:** 18 test — constructor, upload (7 senaryo), delete (2), testConnection (5), UNC
- **BackupHistoryManagerTests:** Temp dizin izolasyonu ile yeniden yapılandırıldı

### Düzeltmeler
- BackupHistoryManagerTests: Birikmiş kayıtlardan kaynaklanan GetRecentHistory test hatası düzeltildi
- TestConnectionAsync_InvalidPath: UNC timeout (13+ dk) sorunu düzeltildi (yerel geçersiz yol kullanıldı)

### Etkilenen Dosyalar
- MikroSqlDbYedek.Engine/Cloud/LocalNetworkProvider.cs (tam implementasyon)
- MikroSqlDbYedek.Engine/Cloud/UncNetworkConnection.cs (yeni)
- MikroSqlDbYedek.Engine/BackupHistoryManager.cs (custom directory desteği)
- MikroSqlDbYedek.Tests/LocalNetworkProviderTests.cs (yeni)
- MikroSqlDbYedek.Tests/BackupHistoryManagerTests.cs (temp dizin izolasyonu)

---

## [0.3.0] - 2025-07-14 — Faz 4: FTP/SFTP Cloud Provider

### Eklenenler
- **FTP/FTPS upload:** FluentFTP AsyncFtpClient — AutoConnect, UploadFile, IProgress<FtpProgress> ilerleme, FTPS Explicit TLS desteği
- **SFTP upload:** SSH.NET SftpClient — UploadFile with progress callback, timeout yapılandırması
- **DeleteAsync:** FTP ve SFTP uzak dosya silme
- **TestConnectionAsync:** Bağlantı testi (10s timeout) — FTP AutoConnect + IsConnected, SFTP Connect + IsConnected
- **Checksum doğrulama:** FTP — MD5 GetChecksum (sunucu destekliyorsa), SFTP — dosya boyutu karşılaştırma
- **DPAPI şifre çözme:** PasswordProtector.Unprotect entegrasyonu (düz metin fallback)
- **Uzak klasör oluşturma:** FTP CreateDirectory, SFTP recursive directory creation
- **FtpSftpProviderTests:** 13 test — validation, unreachable host (FTP/SFTP), delete, connect, provider info

### Düzeltmeler
- FluentFTP 51.0.0 IProgress<FtpProgress> uyumsuzluğu düzeltildi (Action → IProgress wrapper)

### Etkilenen Dosyalar
- MikroSqlDbYedek.Engine/Cloud/FtpSftpProvider.cs (tam implementasyon — önceki placeholder yerine)
- MikroSqlDbYedek.Tests/FtpSftpProviderTests.cs (yeni)
- FEATURES.md (Faz 4 tamamlandı)

---

## [0.2.1] - 2025-07-14 — Faz 3.5: Test Altyapısı & Unit Test'ler

### Eklenenler
- **Moq 4.20.72** — Interface mocking framework (Castle.Core 5.1.1 ile)
- **FluentAssertions 6.12.2** — Okunabilir test assertion'ları (.NET Framework 4.8 uyumlu son sürüm)
- **TestDataFactory** — Tekrar kullanılabilir test verisi fabrikası (BackupPlan, BackupResult, CloudTargets, FileBackup)
- **BackupJobExecutorTests** — 8 test: pipeline akışı, mock bağımlılıklar, hata izolasyonu, cloud upload
- **CloudUploadOrchestratorTests** — 7 test: retry mekanizması, provider bulunamadı, iptal, karışık sonuçlar
- **PlanManagerTests genişletildi** — 10 test: CRUD, schema migration v1→v2, export/import roundtrip, null safety
- **BackupChainValidatorTests** — 16 test: zincir bütünlüğü, Full yükseltme, diff/incremental sayım, tarih sorgusu
- **BackupHistoryManagerTests** — 8 test: kayıt/sorgu, tarih aralığı, maxRecords limiti, hata kayıt persistence
- **RetentionCleanupServiceTests** — 8 test: KeepLastN, DeleteOlderThanDays, Both politikası, .7z desteği, çoklu DB

### Test Özeti
- **Toplam: 65 test, %100 başarılı**
- Unit testler (Moq): BackupJobExecutor, CloudUploadOrchestrator
- Integration testler (temp dizin): PlanManager, BackupChainValidator, BackupHistoryManager, RetentionCleanupService, PasswordProtector

### Etkilenen Dosyalar
- MikroSqlDbYedek.Tests/MikroSqlDbYedek.Tests.csproj (Moq, FluentAssertions eklendi)
- MikroSqlDbYedek.Tests/Helpers/TestDataFactory.cs (yeni)
- MikroSqlDbYedek.Tests/BackupJobExecutorTests.cs (yeni)
- MikroSqlDbYedek.Tests/CloudUploadOrchestratorTests.cs (yeni)
- MikroSqlDbYedek.Tests/PlanManagerTests.cs (genişletildi)
- MikroSqlDbYedek.Tests/BackupChainValidatorTests.cs (yeni)
- MikroSqlDbYedek.Tests/BackupHistoryManagerTests.cs (yeni)
- MikroSqlDbYedek.Tests/RetentionCleanupServiceTests.cs (yeni)
- FEATURES.md (Faz 3.5 + Faz 14 güncellemesi)
- .github/copilot-instructions.md (FEATURES.md zorunlu güncelleme kuralı)

---

## [0.2.0] - 2025-07-14 — Faz 3: Core/Engine Servis Detay İmplementasyonu

### Eklenenler
- **ICloudUploadOrchestrator** arayüzü — bulut upload orkestrasyon soyutlaması
- **IBackupHistoryManager** arayüzü — yedekleme geçmişi yönetim soyutlaması
- **BackupHistoryManager** — günlük JSON dosyalarında yedek geçmişi saklama, thread-safe, plan/tarih bazlı sorgu, 90 gün otomatik temizlik

### İyileştirmeler
- **PlanManager:** JSON şema migration (v1→v2 otomatik yükseltme, JObject), ValidatePlan doğrulaması, DeserializeAndMigrate pattern'i, CurrentSchemaVersion=2
- **BackupChainValidator:** HasValidLogChain (incremental zincir), GetDifferentialCountSinceLastFull, GetIncrementalCountSinceLastFull, GetLastFullBackupDate
- **SevenZipCompressionService:** CompressionLevel mapping (Core→SevenZip enum), CompressMultipleAsync (çoklu dosya), CompressDirectoryAsync (dizin), 7z.dll fallback yolu, çift başlatma koruması
- **BackupJobExecutor:** Tam pipeline (SQL→Verify→Compress→Cloud→Retention→History→Notify), CloudOrchestrator/HistoryManager entegrasyonu, per-step try-catch hata izolasyonu, dosya yedekleme sıkıştırma+bulut akışı
- **EmailNotificationService:** Zengin HTML e-posta gövdesi (dosya boyutu, sıkıştırma oranı, doğrulama sonucu, bulut upload detayları), NotifyFileBackupAsync, BuildFileBackupEmailBody
- **CloudUploadOrchestrator:** ICloudUploadOrchestrator arayüz implementasyonu
- **Copilot direktifi:** FEATURES.md zorunlu güncelleme kuralı eklendi

### Etkilenen Dosyalar
- MikroSqlDbYedek.Core/Interfaces/ICloudUploadOrchestrator.cs (yeni)
- MikroSqlDbYedek.Core/Interfaces/IBackupHistoryManager.cs (yeni)
- MikroSqlDbYedek.Engine/PlanManager.cs (yeniden yazıldı)
- MikroSqlDbYedek.Engine/Backup/BackupChainValidator.cs (genişletildi)
- MikroSqlDbYedek.Engine/Compression/SevenZipCompressionService.cs (genişletildi)
- MikroSqlDbYedek.Engine/Scheduling/BackupJobExecutor.cs (yeniden yazıldı)
- MikroSqlDbYedek.Engine/BackupHistoryManager.cs (yeni)
- MikroSqlDbYedek.Engine/Notification/EmailNotificationService.cs (genişletildi)
- MikroSqlDbYedek.Engine/Cloud/CloudUploadOrchestrator.cs (değiştirildi)
- .github/copilot-instructions.md (FEATURES.md zorunlu güncelleme kuralı)
- FEATURES.md (Faz 2+3 tamamlandı olarak güncellendi)

---

## [0.1.0] - 2025-07-11 — Faz 1: Altyapı & İskelet

### Eklenenler
- **Core projesi:** Tüm modeller (BackupPlan, BackupResult, ConfigModels, DatabaseInfo, Enums, FileBackupModels), interface'ler (ISqlBackupService, ICloudProvider, ICompressionService, IPlanManager, ISchedulerService, INotificationService, IRetentionService, IFileBackupService, IVssService), yardımcılar (PasswordProtector, PathHelper)
- **Engine projesi:** SqlBackupService (SMO), BackupChainValidator, SevenZipCompressionService, PlanManager (JSON CRUD), QuartzSchedulerService, BackupJobExecutor (pipeline), EmailNotificationService (MailKit), RetentionCleanupService, FileBackupService (VSS fallback), VssSnapshotService (AlphaVSS), CloudUploadOrchestrator, GoogleDriveProvider (placeholder), OneDriveProvider (placeholder), FtpSftpProvider (placeholder), LocalNetworkProvider (tam)
- **Service projesi:** Topshelf host, BackupWindowsService lifecycle, otomatik kurtarma politikası
- **Tests projesi:** PasswordProtectorTests, PlanManagerTests (MSTest)
- **Win projesi:** Boş WinForms iskeleti (Core + Engine referansları)
- **Solution:** 5 proje .slnx formatında kayıtlı
- **NuGet:** 14+ paket (Quartz, SMO, SevenZipSharp, MailKit, Autofac, AlphaVSS, Google.Apis.Drive, Microsoft.Graph, FluentFTP, SSH.NET, Serilog, Topshelf, MSTest)
- **Proje referansları:** Engine→Core, Win→Core+Engine, Service→Core+Engine, Tests→Core+Engine

### Düzeltmeler
- SqlConnectionInfo isim çakışması (Core model vs. SMO) — using alias ile çözüldü
- IRetentionService.cs boş dosya — interface içeriği oluşturuldu
- SMO 171.x ServerConnection uyumsuzluğu — property-based oluşturma ile refactor
- FluentFTP 50.1.1→51.0.0, Google.Apis.Drive.v3 1.68.0.3567→1.68.0.3568 versiyon düzeltmeleri

### Etkilenen Dosyalar
- Tüm .csproj dosyaları (5 proje)
- MikroSqlDbYedek.Core/* (tüm dosyalar)
- MikroSqlDbYedek.Engine/* (tüm dosyalar)
- MikroSqlDbYedek.Service/* (tüm dosyalar)
- MikroSqlDbYedek.Tests/* (tüm dosyalar)
