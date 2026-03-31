## [0.43.0] - 2026-05-09 — Dosya Yedekleme Global Lock Çakışması Düzeltmesi

### Hata Düzeltmesi
- **Dosya yedekleme hiç çalışmıyordu (ROOT CAUSE)**: `TriggerPlanNowAsync` hem SQL hem FileBackup job'unu aynı anda tetikliyordu. FileBackup job'u `_globalBackupLock` semaforu yüzünden "Başka bir yedekleme zaten çalışıyor" uyarısıyla atlanıyordu. SQL job bittiğinde ise `plan.FileBackup.Schedule` dolu olduğu için dosya yedeklemeyi çalıştırmıyordu. **Sonuç: FileBackup hiçbir zaman çalışamıyordu.** (etkilenen: `QuartzSchedulerService.cs`, `BackupJobExecutor.cs`)
- **Çözüm**: Manuel tetiklemede SQL job'a `manualTrigger` flag'i geçiliyor. SQL job bitince, FileBackup'ın ayrı schedule'ı olsa bile dosya yedeklemeyi çalıştırıyor. FileBackup job'u artık ayrıca tetiklenmiyor (lock çakışması önlendi). Sadece SQL job yoksa FileBackup doğrudan tetikleniyor.

---

## [0.42.12] - 2026-05-09 — Dosya Yedekleme Root Cause Düzeltmesi

### Hata Düzeltmesi
- **FileBackupService null sessiz return**: `ExecuteFileBackupAsync` ilk guard'ı 3 ayrı koşula bölündü; her biri kendi log seviyesiyle (Error/Warning/Information) raporlanıyor. Eğer Autofac inject başarısız olursa log'da “FileBackupService null” görünür. (etkilenen: `BackupJobExecutor.cs`)
- **Status=Success bug (0 dosya)**: `BackupSourceAsync` içinde `CollectFiles` boş döndüğünde (kaynak dizin yok veya dosya eşleşmiyor) sonuç yanlışlıkla `Success` olarak işaretleniyordu. Artık 0 dosya bulununca `Status=Failed` + `ErrorMessage` set ediliyor. Bu, `ExecuteFileBackupAsync`’teki results kontrolünün doğru çalışmasını sağlıyor. (etkilenen: `FileBackupService.cs`)
- **Kaynak sayısı logu**: Başlangıç log’una aktif kaynak sayısı eklendi — 0 kaynaklı planı hemen teşhis eder.

---

## [0.42.11] - 2026-05-09 — UAC Manifest + Dosya Yedekleme Tanısal Loglama

### Hata Düzeltmesi
- **Dosya yedekleme bulut modu sessiz başarısızlıkları**: `ExecuteFileBackupAsync` içindeki tüm erken dönüş noktaları artık `Log.Warning` ile nedeni bildiriyor. Hangi adımda takıldığı (dosya kopyalanamadı / dizin yok / dizin boş / arşiv oluşturulamadı / CloudOrchestrator null / aktif hedef yok) logdan okunabilir. (etkilenen: `BackupJobExecutor.cs`)
- **Boş dizin → SevenZip çökmesi**: Hedef `Files/` dizini oluşturulmuş ama içinde hiç dosya olmadığında `CompressDirectoryAsync` hata fırlatıyordu. Artık sıkıştırma öncesi `Directory.EnumerateFiles` guard'ı ile boş dizin erkenden yakalanır.
- **Bulut yükleme başlangıç/bitiş logu**: Upload öncesi kaç hedefe gönderileceği, bitiş sonrası tamamlandı logu eklendi.

### Yeni Özellik
- **requireAdministrator manifest**: `KoruMsSqlYedek.Win` projesi artık `app.manifest` ile `requireAdministrator` UAC seviyesinde çalışır. Tray menüsündeki Servis Başlat / Durdur / Yeniden Başlat işlemleri yönetici yetkisi gerektirdiğinden bu zorunluydu. (etkilenen: `app.manifest`, `KoruMsSqlYedek.Win.csproj`)

---

## [0.42.10] - 2026-05-09

### Hata Düzeltmesi
- **Dosya yedekleme buluta gönderilmiyordu**: `BackupJobExecutor.ExecuteFileBackupAsync` içinde `UploadToAllAsync` çağrısına eksik olan `plan.PlanName` parametresi eklendi. Bu parametre olmadan bulut hedefinde yanlış/eksik klasör yolu kullanılıyordu. (etkilenen: `BackupJobExecutor.cs`)
- **Manuel tetiklemede dosya yedekleme çalışmıyordu**: `QuartzSchedulerService.TriggerPlanNowAsync` yalnızca `{planId}_Full` job'unu arıyordu; FileBackup job'u hiç tetiklenmiyordu. Artık `Full / Differential / Incremental` öncelik sırasıyla SQL backup + ayrı schedule'a sahip `{planId}_FileBackup` job'u ayrıca tetikleniyor. (etkilenen: `QuartzSchedulerService.cs`)

### Yeni Özellik
- **Tray servis kontrol menüsü**: Sistem tepsisi menüsüne Servis Durumu göstergesi ve **Servisi Başlat / Servisi Durdur / Yeniden Başlat** düğmeleri eklendi. Menü her açılışta `ServiceController` üzerinden gerçek servis durumunu sorgular ve buton etkin/pasif durumunu otomatik günceller. (etkilenen: `TrayApplicationContext.cs`, `Resources.resx`, `Resources.tr-TR.resx`)

---

## [0.42.9] - 2026-04-06 — Uzak Klasör Yolu Tooltip + Pipe Bağlantı Kesilme Düzeltmesi

### Yeni Özellik
- **Uzak Klasör Yolu tooltip**: "Bulut Hedef Düzenle" formundaki `Uzak Klasör Yolu` alanına provider türüne göre dinamik tooltip eklendi.
  - **Google Drive / OneDrive**: başına `/` konmaz, alt klasörler için `/` ayırıcısı kullanılır. Örnek: `Yedekler/Plan1`
  - **FTP / FTPS / SFTP**: Unix yolu, başında `/` olmalıdır. Örnek: `/yedekler/plan1`
  - Tooltip hem label hem de text box üzerinde gösterilir. (etkilenen: `CloudTargetEditDialog.cs`, `CloudTargetEditDialog.Designer.cs`)

### Hata Düzeltmesi
- **Yedek sonrası servis bağlantısı kesiliyor**: `ServicePipeClient.ReadLoopAsync` içinde pipe tarafından kapatılan bağlantı (`IOException`) artık yakalanarak `SetConnected(false)` tetiklenmemesi sağlandı. Yeniden bağlanma döngüsü sessizce devam eder; "Servis bağlantısı kesildi" balonu gereksiz yere gösterilmez. (etkilenen: `ServicePipeClient.cs`)

---

## [0.42.8] - 2026-04-05 — Express VSS Backup Robustlaştırma

### Hata Düzeltmesi
- **VSS başarısız → COPY_ONLY fallback**: VSS snapshot admin yetkisi gerektirdiğinden başarısız olduğunda artık `COPY_ONLY` SQL backup ile fallback yapılır. SQL Server MDF/LDF dosyaları kilitli olduğundan direct file copy kaldırıldı; snapshot yoksa direkt kopyalama yerine hemen COPY_ONLY'e geçilir.
- **SMO lazy-load düzeltmesi**: `FileGroups.Refresh()` ve `LogFiles.Refresh()` çağrıları eklendi; dosya listesi artık doğru doldurulur.
- **Metot ayrımı**: `TryExpressVssBackupAsync` → `TryVssFileCopyAsync` (bool) + `TrySqlCopyOnlyFallbackAsync` olarak ikiye bölündü. `Server` parametresi fallback için eklendi. (etkilenen: `SqlBackupService.cs`)

---

## [0.42.7] - 2026-04-05 — Express Edition VSS Dosya Kopyası

### Yeni Özellik
- **Express Edition ek güvenlik yedeği**: `SqlBackupService` artık SQL Server Express tespit edildiğinde, başarılı SMO backup'ın ardından ek olarak VSS (Volume Shadow Copy) üzerinden MDF/LDF/NDF dosyalarını kopyalar ve `.7z` arşivine sıkıştırır. Sonuç `BackupResult.VssFileCopyPath` / `VssFileCopySizeBytes` alanlarında raporlanır. VSS hatası ana yedeği etkilemez; fallback olarak doğrudan kopyalama denenir. (etkilenen: `SqlBackupService.cs`, `BackupResult.cs`)
- **Constructor injection**: `SqlBackupService`'e `IVssService` ve `SevenZipCompressionService` Autofac üzerinden enjekte edilir (opsiyonel; null ise VSS adımı atlanır).

---

## [0.42.6] - 2026-04-05 — SQL Server Edition & Recovery Model Uyumluluğu

### Yeni Özellik / Hata Düzeltmesi
- **Edition tespiti**: `SqlBackupService` artık her backup operasyonunda SQL Server edition'ını tespit eder ve Debug log'a yazar. Express/Standard/Enterprise ayrımı yapılır. `ISqlBackupService.GetServerEditionAsync()` metodu eklendi — UI ve plan doğrulamasında kullanılabilir. (yeni model: `SqlServerEditionInfo`)
- **Recovery Model kontrolü**: `BackupDatabaseAsync` içinde, transaction log (Incremental) yedeği talep edildiğinde veritabanının recovery model'i kontrol edilir. `Simple recovery model` → log yedeği yerine **Full yedeğe otomatik yükseltilir**, kullanıcıya Warning loglanır. Express instance ise note olarak eklenir. Düzeltme komutu da loga yazılır: `ALTER DATABASE [x] SET RECOVERY FULL`. (etkilenen: `SqlBackupService.cs`, `ISqlBackupService.cs`, `SqlServerEditionInfo.cs`)

---

## [0.42.5] - 2026-04-05 — CopyOnly Backup Zinciri Düzeltmesi

### Hata Düzeltmesi
- **Full backup CopyOnly=true hatası**: `SqlBackupService` satır 86 — `CopyOnly = backupType != SqlBackupType.Incremental` yanlış bir ifadeydi; Full ve Differential yedekler `CopyOnly=true` ile alınıyordu. Full backup `CopyOnly=true` olduğunda SQL Server'ın differential baseline'ı güncellenmez → Differential yedekler son Full yedeği base alamaz, boyutları gereksiz büyür veya hata verir. `CopyOnly = false` olarak düzeltildi. (etkilenen: `SqlBackupService.cs`)

---

## [0.42.4] - 2026-04-05 — Global Yedekleme Kilidi

### Yeni Özellik
- **Eşzamanlı yedekleme engeli**: Bir plan yedeklenirken başka bir plan (otomatik veya manuel) artık başlatılamaz. `BackupJobExecutor`'a `static SemaphoreSlim(1,1)` eklendi; kilit alınamazsa job atlanır ve uyarı loglanır. `ServicePipeServer` `ManualBackup` handler'ında da `IsAnyRunning()` kontrolü ile manuel tetikleme önceden reddedilir. (etkilenen: `BackupJobExecutor.cs`, `ServicePipeServer.cs`, `BackupCancellationRegistry.cs`)

---

## [0.42.3] - 2026-04-05 — Hata Düzeltmeleri: Buton Sırası, Dosya Yedek Upload, NextRun Yarış Koşulu

### Hata Düzeltmeleri
- **PlanEditForm buton sırası**: `Controls.Add` sırası düzeltildi; sol→sağ görsel sıra artık `[İptal]` `[◄ Geri]` `[İleri ►]` `[💾 Kaydet & Çık]` (etkilenen: `PlanEditForm.Designer.cs`)
- **Dosya yedekleme bulut upload atlanıyordu**: `BackupJobExecutor.ExecuteFileBackupAsync` satır 326 — sadece `Success` kontrol ediliyordu; `PartialSuccess` (kilitli dosya gibi) durumunda sıkıştırma ve bulut upload atlanıyordu; artık `PartialSuccess` ve `FilesCopied > 0` da kabul edilir (etkilenen: `BackupJobExecutor.cs`)
- **Sonraki çalışma zamanı "—" gösteriyordu**: `ServicePipeServer` — yeni bağlantıda iki eş zamanlı `SendStatusToClientAsync` çağrısı (ilk broadcast + `RequestStatus` yanıtı) aynı pipe'a aynı anda yazarak JSON satırlarını bozuyordu; bağlantı anındaki redundant broadcast kaldırıldı; her istemci için `SemaphoreSlim(1,1)` yazma kilidi eklendi; `BroadcastAsync` ve `SendStatusToClientAsync` artık kilit kullanıyor (etkilenen: `ServicePipeServer.cs`)

---

## [0.42.2] - 2026-04-05 — Uygulama Adı Düzeltmesi

### Hata Düzeltmeleri
- **Uygulama adı** balon bildiriminde "KoruMsSqlYedek" yerine "Koru MsSql Yedek" gösteriliyordu; `Resources.resx` + `Resources.tr-TR.resx` `AppName` key düzeltildi

---

## [0.42.1] - 2026-04-05 — SMO Hata Düzeltmesi + Dosya Yedek Upload

### Hata Düzeltmeleri
- **`PropertyCannotBeRetrievedException`**: `SqlBackupService.ListDatabasesAsync` — `db.Size`, `Status`, `RecoveryModel`, `LastBackupDate`, `IsSystemObject` her biri ayrı `try/catch` ile sarıldı; offline/restoring/snapshot DB'lerde çökme engellendi
- **Dosya yedekleme bulut upload**: `ExecuteFileBackupAsync` — cloud upload yalnızca `Compression != null` bloğu içinde çalışıyordu; sıkıştırma ve upload bağımsız `try/catch` bloklarına ayrıldı; `as` cast → `is` pattern değiştirildi

---

## [0.42.0] - 2026-04-05 — UI Geliştirmeleri: Log Görev Filtresi, Dashboard Sıralama, Tray Animasyonu, Upload ETA

### Yeni Özellikler
- **Log ekranı görev adı filtresi** (`_cmbLogPlan`): Log toolbar'a "Görev:" etiketi ve ComboBox eklendi; seçili plan adına göre log satırları filtrelenir; `PopulateLogPlanFilter()` plan listesinden otomatik doldurulur; Temizle butonu sıfırlar; `LogViewer_AllPlans` resource key eklendi
- **Dashboard ListView sıralama**: Kolon başlığına tıklayarak sıralama (`OnLastBackupsColumnClick`); tarih/boyut/durum kolonları tip-bilinçli karşılaştırma (`BackupResult.Tag` üzerinden); `_lvSortColumn/_lvSortAscending` alanları; sort ok çizimi `OnListViewDrawColumnHeader`'da `AccentPrimary` renginde üçgen
- **Dashboard ListView AutoSize**: `AutoResizeListViewColumns()` yardımcısı `TextRenderer.MeasureText` ile header + içerik genişliklerini ölçer; her yükleme sonrası otomatik çağrılır; sağda boşluk kalmaz
- **TrayIcon animasyonu**: Yedekleme başladığında 8-frame (45° adımlarla dönen) animasyon; `SymbolIconHelper.CreateAnimationFrames()` + `System.Windows.Forms.Timer` (150ms); `StartTrayAnimation/StopTrayAnimation` lifecycle
- **Upload ilerleme ETA**: `CloudUploadProgress` log satırında "Kalan: X MB | Süre: X dk" gösterimi; `FormatEta(bytesRemaining, speedBps)` yardımcısı
- **Grid NextRun sütunu düzeltmesi**: ISO 8601 → yerel saat `dd.MM.yyyy HH:mm`; `_nextFireTimes` sözlüğü filtre geçişlerinde kaybolmaz
- **Başarısız görev satır rengi**: `ModernTheme.GridErrorRow` (Dark: `58,20,20` / Light: `255,232,232`); `PlanRowData.LastBackupFailed` alanı
- **copilot-instructions.md güncellendi**: Proje-spesifik derin içerik; bileşenler arası haberleşme kanalları tablosu; UI mimarisi; log toolbar layout

### Değişen Dosyalar
- `KoruMsSqlYedek.Win\MainWindow.Designer.cs` — `_lblLogPlan`, `_cmbLogPlan`, `ColumnClick` olayı
- `KoruMsSqlYedek.Win\MainWindow.cs` — `PopulateLogPlanFilter`, `ApplyLogFilter` plan filtresi, `OnLastBackupsColumnClick`, `AutoResizeListViewColumns`, `LastBackupsItemComparer`, `FormatEta`, `_lvSortColumn/_lvSortAscending`, `_nextFireTimes`, `GridErrorRow` kullanımı
- `KoruMsSqlYedek.Win\TrayApplicationContext.cs` — `_animTimer`, `StartTrayAnimation`, `StopTrayAnimation`, `OnAnimTimerTick`
- `KoruMsSqlYedek.Win\Helpers\SymbolIconHelper.cs` — `RenderRotatedIcon`, `CreateAnimationFrames`
- `KoruMsSqlYedek.Win\Theme\ModernTheme.cs` — `GridErrorRow` (Dark + Light)
- `KoruMsSqlYedek.Win\Properties\Resources.resx` + `Resources.tr-TR.resx` — `LogViewer_AllPlans`
- `.github\copilot-instructions.md` — Tam proje-spesifik yeniden yazım

---

## [0.41.0] - 2026-04-04 — Görev Listesi Sıralama + Arama

### Yeni Özellikler
- **Kolon başlığı tıklama ile sıralama** (`_dgvPlans`): Tüm kolon başlıklarına tıklanarak artan/azalan sıralama yapılabilir; İlerleme kolonu (Progress) sıralamadan çıkarıldı; aktif sıralama kolonu `SortGlyphDirection` ile görselleştirilir
- **ToolStrip arama kutusu** (`_tstSearch`): Görev adı, veritabanı, strateji ve depolama alanı üzerinde büyk/küçük harf duyarsız canlı filtreleme; `_tslSearchLabel` ("Ara:") + 200 px `ToolStripTextBox`
- **`PlanRowData` iç sınıf**: Plan görüntüleme verileri (history dahil) `RefreshPlanList()`'te tek seferlik hesaplanır, `ApplyPlanFilter()` filtreleme/sıralama/grid doldurmaı yönetir — her filtre değişikliğinde history yeniden sorgulanmaz
- **Durum çubuğu**: Filtreleme aktifken "X / Y görev", değilse mevcut format korunur

### Değişen Dosyalar
- `KoruMsSqlYedek.Win\MainWindow.Designer.cs` — 3 yeni ToolStrip öğesi + `ColumnHeaderMouseClick` olayı
- `KoruMsSqlYedek.Win\MainWindow.cs` — `PlanRowData`, `ApplyPlanFilter()`, `OnPlanGridColumnHeaderClick()`, `OnPlanSearchTextChanged()`, `RefreshPlanList()` refactor

---

## [0.40.0] - 2026-04-04 — Periyodik Raporlama Motoru

### Yeni Özellikler
- **`IReportingService`** (`Core\Interfaces`): Periyodik rapor gönderimi için yeni arayüz
- **`ReportingService`** (`Engine\Notification`): Günlük/Haftalık/Aylık özet rapor oluşturur ve SMTP ile gönderir
  - `GetReportingPeriod()`: Daily (dün), Weekly (geçen hafta Pzt–Pzt), Monthly (geçen ay 1'i–bu ay 1'i)
  - Plan'ın `BackupHistory`'sinden dönem kayıtlarını sorgular; başarı oranı, toplam boyut, sıkıştırma kazancı hesaplar
  - HTML tablo: özet + en fazla 50 kayıtlık detay tablosu; başarı/başarısız renklendirme
  - SMTP profil çözümü: `SmtpProfileId` → eski per-plan alanlar (geriye uyumluluk)
- **`ReportingJob`** (`Engine\Scheduling`): Quartz.NET `IJob` — `planId` JobData üzerinden plan yükler, `IReportingService` çağırır; `DisallowConcurrentExecution`
- **`QuartzSchedulerService`**: `SchedulePlanAsync`'ta `plan.Reporting.IsEnabled` aktifse reporting job zamanlanır; `UnschedulePlanAsync` + `GetNextFireTimeAsync` "Reporting" tipini kapsar
  - `BuildReportingCron()`: Daily `0 0 H * * ?`, Weekly `0 0 H ? * MON`, Monthly `0 0 H 1 * ?`; `SendHour` 0–23 sınırlanır
- **`EngineModule`**: `IReportingService`/`ReportingService` + `ReportingJob` kaydı eklendi

### Test
- **`ReportingServiceTests`**: 17 yeni test
  - Constructor null guard'ları
  - `SendReportAsync`: plan null, Reporting null/disabled, SMTP profil yok, profil ID bulunamadı
  - `GetReportingPeriod`: Daily/Weekly/Monthly dönem hesabı
  - `BuildReportingCron`: null/disabled, Daily/Weekly/Monthly cron, SendHour sınırlama

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core\Interfaces\IReportingService.cs` (YENİ)
- `KoruMsSqlYedek.Engine\Notification\ReportingService.cs` (YENİ)
- `KoruMsSqlYedek.Engine\Scheduling\ReportingJob.cs` (YENİ)
- `KoruMsSqlYedek.Engine\Scheduling\QuartzSchedulerService.cs`
- `KoruMsSqlYedek.Engine\IoC\EngineModule.cs`
- `KoruMsSqlYedek.Tests\ReportingServiceTests.cs` (YENİ)
- `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.40.0.0
- `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.40.0.0

---

## [0.39.2] - 2026-04-04 — BackupJobExecutor Kapsamlı Test Genişletmesi

### Test İyileştirmeleri
- **BackupJobExecutorTests**: 19 → 31 test (+12 yeni senaryo)
  - ChainValidator: Full yedek yok → Differential→Full yükseltme; AutoPromote eşiği; eşik altında Differential korunur
  - VerifyAfterBackup: SQL verify başarısız → pipeline devam; arşiv verify başarısız → CompressionVerified=false
  - Sıkıştırma: `plan.Compression null` → CompressAsync çağrılmaz
  - Cloud upload: throws → Retention/History/Notify yine çalışır; sıkıştırma yok → ham .bak dosyası upload edilir
  - Hata dayanıklılığı: Retention throws → History+Notify çalışır; `plan.Notifications null` → exception fırlatılmaz
  - Çoklu DB: ilk DB başarısız → döngü devam, ikinci DB çalışır
  - İptal: CancellationToken önceden iptal → OperationCanceledException yutulur

---

## [0.39.1] - 2026-04-04 — UI Görünen Ad Düzeltmesi

### İyileştirmeler
- **UI Yeniden Adlandırma**: Tüm kullanıcıya görünen alanlarda "KoruMsSqlYedek" → "Koru MsSql Yedek" düzeltildi (pencere başlıkları, e-posta konu/gövde/footer, gönderici adı, SMTP diyaloğu, bulut hedef varsayılan klasör, Assembly metadata)
- Etkilenen dosyalar: `MainWindow.cs`, `MainWindow.Designer.cs`, `SmtpProfileEditDialog.cs/Designer`, `CloudTargetEditDialog.cs`, `EmailNotificationService.cs`, `AppSettings.cs`, `AppSettingsManager.cs`, `AssemblyInfo.cs`

---

## [0.39.0] - 2026-04-04 — Merkezi SMTP Profil Yönetimi

### Yeni Özellikler
- **Çoklu SMTP Profili**: Ayarlar ekranına "E-posta Profilleri" tablosu eklendi; birden fazla SMTP profili tanımlanabilir (Gmail, Office 365, vb.)
- **Profil Seçimi (PlanEditForm)**: Görev düzenleme wizard Adım 6, per-plan SMTP alanlar yerine tek combo-box (SmtpProfileId) kullanıyor
- **Eski Plan Uyumluluğu**: `SmtpProfileId` boşsa `SmtpServer` / `SmtpPort` vb. eski alanlar otomatik fallback olarak kullanılıyor
- **Legacy Migrasyon**: Yükleme sırasında eski `smtp` JSON alanı varsa otomatik olarak `smtpProfiles[0]` ("Varsayılan") profiline taşınıyor
- **SmtpProfileEditDialog**: Profil ekle / düzenle / sil diyaloğu (`MainWindow` üzerinden)

### Teknik Detaylar
- `AppSettings.SmtpProfiles: List<SmtpProfile>` — yeni merkezi profil listesi
- `NotificationConfig.SmtpProfileId` — plan başvurusu; `SmtpPort?`, `SmtpUseSsl?` nullable
- `EmailNotificationService(IAppSettingsManager)` — profil çözümleme
- `AppSettingsManager.MigrateSmtpLegacy()` — tek seferlik v1 migrasyon
- 4 yeni `EmailNotificationServiceTests` + CloudUploadOrchestrator / AppSettingsManager test güncellemeleri
- 211/211 test geçti

### Değiştirilen Dosyalar
- `KoruMsSqlYedek.Core\Models\AppSettings.cs`, `ConfigModels.cs`
- `KoruMsSqlYedek.Engine\AppSettingsManager.cs`, `EmailNotificationService.cs`
- `KoruMsSqlYedek.Win\Forms\SmtpProfileEditDialog.cs` (yeni), `PlanEditForm.cs`, `MainWindow.cs`
- `KoruMsSqlYedek.Tests\EmailNotificationServiceTests.cs`, `TestDataFactory.cs`, `AppSettingsManagerTests.cs`, `CloudUploadOrchestratorTests.cs`

---
## [0.38.0] - 2026-04-03 — Dosya Yedekleme Tetikleme Hatası Düzeltildi

### Hata Düzeltmeleri
- **Dosya Yedekleme Tetiklenmiyor**: `FileBackup.IsEnabled = true` ve ayrı zamanlama (`FileBackup.Schedule`) boş olduğunda dosya yedekleme hiç çalışmıyordu. SQL backup job'u tamamlandıktan sonra `ExecuteFileBackupAsync` çağrılmıyordu.
- `BackupJobExecutor.Execute()`: SQL yedekleme pipeline'ı bittikten sonra, plana ait dosya yedekleme etkin ve ayrı schedule'ı yoksa `ExecuteFileBackupAsync` artık otomatik tetiklenir.

### Değiştirilen Dosyalar
- `KoruMsSqlYedek.Engine\Scheduling\BackupJobExecutor.cs`

---

## [0.37.0] - 2026-04-03

### Yeni Özellikler
- **Grid İlerleme Sütunu**: `DataGridViewProgressBarColumn` ile görev listesinde çalışan yedek için satır bazlı progress bar gösterimi
- **Per-Plan Log Yalıtımı**: Görevler arası geçiş yapıldığında `_txtBackupLog` seçili plana ait log buffer'ını gösterir; iki görev aynı anda çalışsa dahi loglar karışmaz
- **Upload Byte/Hız Bilgisi**: Bulut yükleme sırasında `%XX | Gönderilen: YMB/ZMB | Hız: WMB/s` formatında log + progress bar `CustomText` modunda gösterim
- **Varsayılan Uzak Klasör Yolu**: Yeni bulut hedef oluştururken `RemoteFolderPath` boşsa OAuth için `KoruMsSqlYedek`, FTP/SFTP için `/KoruMsSqlYedek` varsayılan değeri atanır

### Teknik Detaylar
- `BackupActivityEventArgs`: `BytesSent`, `BytesTotal`, `SpeedBytesPerSecond` alanları eklendi
- `BackupActivityMessage` (PipeProtocol): yeni alanlar JSON üzerinden serileştirilir
- `CloudUploadOrchestrator`: `hubProgress` callback'i upload başlangıç zamanını kaydedip byte/hız hesaplar
- `DataGridViewProgressBarCell/Column`: `Theme` namespace'inde yeni özel sütun tipi
- `ModernProgressBar.DisplayMode = CustomText` aktif upload sırasında kullanılır

### Değiştirilen Dosyalar
- `KoruMsSqlYedek.Core\Events\BackupActivityEvent.cs`
- `KoruMsSqlYedek.Core\IPC\PipeProtocol.cs`
- `KoruMsSqlYedek.Engine\Cloud\CloudUploadOrchestrator.cs`
- `KoruMsSqlYedek.Win\Forms\CloudTargetEditDialog.cs`
- `KoruMsSqlYedek.Win\Theme\DataGridViewProgressBarCell.cs` (YENİ)
- `KoruMsSqlYedek.Win\MainWindow.Designer.cs`
- `KoruMsSqlYedek.Win\MainWindow.cs`
- `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.37.0
- `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.37.0.0

---

## [0.36.0] - 2026-04-02 — Inno Setup & Build Script (.NET 10 Uyumlu Dağıtım)

### Güncelleme
- **KoruMsSqlYedek.iss**: Versiyon 0.36.0; `.NET Framework 4.8` kontrolü kaldırıldı, `.NET 10 Desktop Runtime` varlık kontrolü eklendi (`IsDotNet10DesktopInstalled` — `FindFirst` ile `Microsoft.WindowsDesktop.App\10.*`)
- **Inno Setup CustomMessages**: `DotNetRequired` mesajı .NET 10 indirme linki ile güncellendi (TR + EN)
- **Build-Release.ps1**: Her iki `dotnet publish` komutuna `-r win-x64 --self-contained false` eklendi (framework-dependent, RID-specific publish)
- **Build-Release.ps1**: Adım sayacı `[1/6]–[6/6]` → `[1/7]–[7/7]`; yeni 7. adım olarak isteğe bağlı `ISCC.exe` Inno Setup installer derleme eklendi

### Değiştirilen Dosyalar
- `Deployment\InnoSetup\KoruMsSqlYedek.iss`
- `Deployment\Build-Release.ps1`
- `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.36.0
- `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.36.0.0

---

## [0.35.0] - 2026-04-02 — Restore UI (Yedekten Geri Yükleme Diyaloğu)

### Yeni Özellikler
- **RestoreDialog**: Plan sağ tık menüsünde "Geri Yükle..." ile açılan modal diyalog
- **Yedek geçmişi listesi**: Seçilen plana ait başarılı yedekler tarih/DB/tür/dosya/boyut/durum sütunlarıyla görüntülenir
- **RESTORE VERIFYONLY**: "Doğrula" butonu ile seçili yedek dosyasının bütünlüğü kontrol edilir
- **RESTORE DATABASE**: "Geri Yükle" butonu — hedef DB adı düzenlenebilir; güvenlik yedeği seçeneği (varsayılan açık)
- **İlerleme çubuğu + canlı log**: Restore işlemi sırasında yüzde ve adım mesajları anlık gösterilir
- **Context menu entegrasyonu**: Plan grid sağ tık menüsüne ayraç + "Geri Yükle..." öğesi eklendi

### Teknik Detaylar
- `RestoreDialog` → `ModernFormBase`'den türer; `ISqlBackupService` + `IBackupHistoryManager` inject
- Restore öncesi güvenlik yedeği: `SqlBackupService.RestoreDatabaseAsync(createPreRestoreBackup: true)`
- 2 saatlik timeout ile `CancellationTokenSource`; form kapatılınca iptal
- Tüm UI string'leri lokalize edildi (20 kaynak anahtarı, EN + TR)

### Yeni Dosyalar
- `KoruMsSqlYedek.Win/Forms/RestoreDialog.cs`
- `KoruMsSqlYedek.Win/Forms/RestoreDialog.Designer.cs`

### Değiştirilen Dosyalar
- `KoruMsSqlYedek.Win/MainWindow.Designer.cs` — `_ctxSep4` + `_ctxRestore` context menu öğesi
- `KoruMsSqlYedek.Win/MainWindow.cs` — `OnCtxRestoreClick` handler
- `KoruMsSqlYedek.Win/Properties/Resources.resx` + `Resources.tr-TR.resx` — 20 yeni anahtar

---

## [0.34.0] - 2026-04-02 — Bildirim Sistemi (MailKit + ToastEnabled)

### Yeni Özellikler
- **ToastEnabled plan ayarı**: Her plan için bağımsız tray balloon tip kontrolü — `false` yapıldığında hiçbir balloon gösterilmez
- **ToastEnabled pipe akışı**: Servis, plan konfigürasyonundan `ToastEnabled` değerini okuyup `BackupActivityMessage` içinde Win uygulamasına iletir
- **EmailNotificationService**: MailKit tabanlı SMTP bildirimi — yedek sonucu (başarılı/başarısız) HTML e-posta olarak gönderilir
- **Bildirim başarısızlığı yedek sonucunu etkilemez**: `BackupJobExecutor.NotifyIfConfigured` hataları sadece log'a yazar

### Değişiklikler
- `BackupActivityEventArgs`: `ToastEnabled` özelliği eklendi (varsayılan `true`)
- `BackupActivityMessage`: `toastEnabled` JSON alanı eklendi; `FromArgs` + `ToArgs` güncellendi
- `ServicePipeServer.OnActivityChanged`: `_planManager`'dan plan okunup `msg.ToastEnabled` doldurulur
- `TrayApplicationContext.OnBackupActivityChanged`: tüm `case`'lerde `if (e.ToastEnabled)` koruması eklendi

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Events/BackupActivityEvent.cs`
- `KoruMsSqlYedek.Core/IPC/PipeProtocol.cs`
- `KoruMsSqlYedek.Service/IPC/ServicePipeServer.cs`
- `KoruMsSqlYedek.Win/TrayApplicationContext.cs`

---

## [0.33.0] - 2026-04-02 — Scheduler Durum Sorgusu (Next Fire Times)

### Yeni Özellikler
- **Plan grid `_colNextRun`**: Her planın bir sonraki çalışma zamanı servis üzerinden canlı alınarak grid'de gösterilir
- **`ServiceStatusHub`**: Statik event hub — pipe'dan gelen `ServiceStatusMessage` mesajlarını UI bileşenlerine iletir
- **`BroadcastStatusAsync()`**: Servis, yedek tamamlandığında/başarısız/iptal sonrası tüm istemcilere status yayınlar
- **Dashboard zamanlayıcısı**: Her 30 saniyede `RequestStatusAsync()` ile güncel next fire times istenir

### Değişiklikler
- `ServicePipeServer`: `IPlanManager` inject; `GetNextFireTimeAsync` ile `NextFireTimes` doldurma; `BroadcastStatusAsync()` yeni metod
- `ServicePipeClient`: `ServiceStatus` mesajı → `ServiceStatusHub.Raise()` ile UI'ye iletilir
- `MainWindow`: `ServiceStatusHub.StatusReceived` subscribe; `OnServiceStatusReceived` handler

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/IPC/ServiceStatusHub.cs` (YENİ)
- `KoruMsSqlYedek.Service/IPC/ServicePipeServer.cs`
- `KoruMsSqlYedek.Win/IPC/ServicePipeClient.cs`
- `KoruMsSqlYedek.Win/MainWindow.cs`

---

## [0.32.0] - 2026-04-02 — Pipe Bağlantı Durum Göstergesi

### Yeni Özellikler
- **Tray icon**: Servis bağlı değilken gri uyarı ikonu; bağlandığında yeşil kalkan ikonuna döner
- **Tray tooltip**: Bağlantı durumuna göre dinamik — "Servis bağlı değil ⚠" veya "KoruMsSqlYedek — Hazır"
- **Tray balloon**: Bağlantı kurulduğunda ve kesildiğinde bildirim
- **MainWindow status bar** (`_tslStatus`): Bağlı → yeşil "● Servis bağlı", Bağlı değil → kırmızı "⚠ Servis bağlı değil"
- **Düğme durumu**: `_btnStart` servis bağlı değilken otomatik devre dışı

### Değişiklikler
- `TrayIconStatus` enum: `Disconnected` değeri eklendi
- `SymbolIconHelper.CreateStatusIcon()`: Disconnected → gri uyarı ikonu
- `TrayApplicationContext`: `OnPipeConnectionChanged` handler; başlangıçta Disconnected ikonu
- `MainWindow`: `OnPipeConnectionChanged` + `UpdateStatusBarConnection`; unsubscribe OnFormClosing

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/Helpers/SymbolIconHelper.cs`
- `KoruMsSqlYedek.Win/TrayApplicationContext.cs`
- `KoruMsSqlYedek.Win/MainWindow.cs`
- `KoruMsSqlYedek.Win/Properties/Resources.resx` + `Resources.tr-TR.resx`

---

## [0.31.0] - 2026-04-02 — Named Pipe IPC: Servis ↔ Tray Ayrışması

### Mimari
- Tray uygulaması artık yedekleme motorunu doğrudan çalıştırmıyor; Windows Service ile Named Pipe (`KoruMsSqlYedekPipe`) üzerinden JSON protokolüyle haberleşiyor
- `BackupCancellationRegistry`: planId → CTS eşlemesi; pipe üzerinden anlık iptal desteği

### Yeni Dosyalar
- `Core\IPC\PipeProtocol.cs` — paylaşımlı mesaj protokolü (ManualBackup, CancelBackup, BackupActivity, ServiceStatus)
- `Core\IPC\BackupCancellationRegistry.cs` — `IBackupCancellationRegistry` + impl
- `Service\IPC\ServicePipeServer.cs` — çok-istemci pipe sunucusu; BackupActivityHub yayını
- `Win\IPC\ServicePipeClient.cs` — otomatik yeniden bağlantı; UI thread'e marshal; SendManualBackup/Cancel
- `Win\IoC\WinModule.cs` — Tray için Quartz/sıkıştırma/bulut içermeyen hafif IoC modülü

### Değişiklikler
- `BackupJobExecutor`: `IBackupCancellationRegistry` property injection; CTS kayıt/sil
- `BackupWindowsService`: `ServicePipeServer` inject; start/stop entegrasyonu
- `ServiceContainerBootstrap`: pipe bileşenleri singleton olarak kayıtlı
- `WinContainerBootstrap`: `EngineModule` → `WinModule`
- `TrayApplicationContext`: scheduler bağımlılıkları kaldırıldı; `ServicePipeClient` ile çalışır
- `MainWindow`: pipe-tabanlı manuel yedekleme UI; `OnBackupActivityChanged`; `UpdatePlanRowStatus`; `AppendBackupLog`

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/IPC/PipeProtocol.cs` (yeni)
- `KoruMsSqlYedek.Core/IPC/BackupCancellationRegistry.cs` (yeni)
- `KoruMsSqlYedek.Service/IPC/ServicePipeServer.cs` (yeni)
- `KoruMsSqlYedek.Win/IPC/ServicePipeClient.cs` (yeni)
- `KoruMsSqlYedek.Win/IoC/WinModule.cs` (yeni)
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs`
- `KoruMsSqlYedek.Service/BackupWindowsService.cs`
- `KoruMsSqlYedek.Service/IoC/ServiceContainerBootstrap.cs`
- `KoruMsSqlYedek.Win/IoC/WinContainerBootstrap.cs`
- `KoruMsSqlYedek.Win/TrayApplicationContext.cs`
- `KoruMsSqlYedek.Win/MainWindow.cs`

---

## [0.30.0] - 2026-03-31 — Yerel Yedek Bütünlük Sistemi

### Yeni Özellikler
- **SQL RESTORE VERIFYONLY**: `plan.VerifyAfterBackup = true` ise yedek dosyası SMO `SqlVerify` ile doğrulanır; sonuç `BackupResult.VerifyResult`'ta saklanır (zaten mevcuttu, pipeline entegrasyonu tamamlandı)
- **7z arşiv doğrulama**: Sıkıştırma sonrası `SevenZipExtractor.Check()` ile tüm girdiler CRC kontrolünden geçer; `BackupResult.CompressionVerified: bool?` eklendi
- **Dosya kopyası SHA-256**: Her dosya kopyalandıktan sonra boyut + SHA-256 karşılaştırması; kaynak kilitli ise boyut eşleşmesi yeterli; `FileBackupResult.FilesVerified` / `FilesVerificationFailed` takibi

### Değişiklikler
- `ICompressionService`: `VerifyArchiveAsync(archivePath, password, ct)` eklendi
- `SevenZipCompressionService`: `VerifyArchiveAsync` implementasyonu (şifreli arşiv desteği dahil)
- `BackupResult`: `CompressionVerified: bool?` alanı eklendi
- `BackupJobExecutor`: Step 3b — compress sonrası `VerifyArchiveAsync` çağrısı
- `FileBackupModels.FileBackupResult`: `FilesVerified` + `FilesVerificationFailed` eklendi
- `IFileBackupService.BackupSourceAsync`: `verifyAfterCopy = false` opsiyonel parametre
- `FileBackupService`: `VerifyFileCopyIntegrityAsync` + `ComputeFileSha256Async` yardımcıları

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Interfaces/ICompressionService.cs`
- `KoruMsSqlYedek.Core/Interfaces/IFileBackupService.cs`
- `KoruMsSqlYedek.Core/Models/BackupResult.cs`
- `KoruMsSqlYedek.Core/Models/FileBackupModels.cs`
- `KoruMsSqlYedek.Engine/Compression/SevenZipCompressionService.cs`
- `KoruMsSqlYedek.Engine/FileBackup/FileBackupService.cs`
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs`

---

## [0.29.0] - 2026-03-31 — Resumable Upload + Bulut SHA-256 Bütünlük Kontrolü

### Yeni Özellikler
- **Resumable upload**: Kesilen bulut yüklemeleri kaldığı yerden devam eder (tüm provider'lar)
- **Upload state persistence**: `UploadStateRecord` JSON ile `%APPDATA%\KoruMsSqlYedek\UploadState\` altında saklanır
- **Startup recovery**: Uygulama başlangıcında (`TrayApplicationContext` + `BackupWindowsService`) bekleyen upload'lar otomatik yeniden denenir
- **Remote boyut doğrulama**: Upload sonrası provider'dan dosya boyutu alınır ve yerel boyutla karşılaştırılır

### Provider Detayları
- Google Drive: `InitiateSessionAsync()` → `Task<Uri>` (cast via `ResumableUpload` base) → `ResumeAsync(uri)`
- OneDrive: `UploadSession.UploadUrl` persist; `LargeFileUploadTask` ile resume
- FTP/FTPS: `FtpRemoteExists.Resume` — native sunucu-taraflı resume
- SFTP: `GetAttributes.Size` offset + `Open(ReadWrite)` + seek ile append

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/UploadStateRecord.cs` (yeni)
- `KoruMsSqlYedek.Engine/Cloud/UploadStateManager.cs` (yeni)
- `KoruMsSqlYedek.Core/Helpers/PathHelper.cs`
- `KoruMsSqlYedek.Core/Interfaces/ICloudProvider.cs`
- `KoruMsSqlYedek.Core/Interfaces/ICloudUploadOrchestrator.cs`
- `KoruMsSqlYedek.Core/Models/BackupResult.cs`
- `KoruMsSqlYedek.Engine/Cloud/GoogleDriveProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/OneDriveProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/FtpSftpProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/LocalNetworkProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/CloudUploadOrchestrator.cs`
- `KoruMsSqlYedek.Win/TrayApplicationContext.cs`
- `KoruMsSqlYedek.Service/BackupWindowsService.cs`

---

## [0.28.0] - 2026-03-30 — Bulut Upload İlerleme + Otomatik Klasör Yapısı

### Yeni Özellikler
- **Bulut upload ilerleme**: Her bulut hedefi için durum etiketi ve progress bar anlık güncellenir (`CloudUploadStarted / Progress / Completed`)
- **Otomatik klasör yapısı**: `RemoteFolderPath` boş bırakılırsa dosyalar otomatik olarak `KoruMsSqlYedek/{PlanAdı}/` klasörüne yüklenir; klasör yoksa provider otomatik oluşturur
- **Per-target log satırları**: Her hedefin yükleme sonucu (✓/✗) ayrı satır olarak log'a yazılır

### Değişiklikler
- `BackupActivityType`: `CloudUploadStarted`, `CloudUploadProgress`, `CloudUploadCompleted`, `StepChanged` eklendi
- `BackupActivityEventArgs`: `CloudTargetName`, `CloudTargetIndex`, `CloudTargetTotal`, `ProgressPercent`, `StepName`, `IsSuccess` alanları eklendi
- `ICloudUploadOrchestrator.UploadToAllAsync`: `planName` opsiyonel parametresi eklendi
- `CloudUploadOrchestrator`: `SanitizeFolderName` + `ShallowCopyWithFolder` yardımcıları eklendi
- `MainWindow.cs`: Progress bar SQL (%60) + Bulut (%40) olarak bölündü

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Events/BackupActivityEvent.cs`
- `KoruMsSqlYedek.Core/Interfaces/ICloudUploadOrchestrator.cs`
- `KoruMsSqlYedek.Engine/Cloud/CloudUploadOrchestrator.cs`
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs`
- `KoruMsSqlYedek.Win/MainWindow.cs`

---

## [0.27.2] - 2026-03-30 — AppData Otomatik Migrasyon

### Düzeltme
- **AppData migrasyon**: Uygulama başlangıcında `%APPDATA%\MikroSqlDbYedek\` klasörü tespit edilirse tüm veriler otomatik olarak `%APPDATA%\KoruMsSqlYedek\` altına taşınır ve eski klasör silinir

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Helpers/PathHelper.cs` (MigrateLegacyAppData eklendi)
- `KoruMsSqlYedek.Win/Program.cs` (migrasyon çağrısı eklendi)

---

## [0.27.1] - 2026-03-30 — İsim Değişikliği Sonrası Temizlik

### Düzeltmeler
- **`KoruMsSqlYedek.slnLaunch.user`**: Proje yolları `MikroSqlDbYedek.*` → `KoruMsSqlYedek.*` olarak güncellendi
- **Eski klasörler silindi**: `MikroSqlDbYedek.Engine\` ve `MikroSqlDbYedek.Win\` (yalnızca `obj` içeriyordu) kaldırıldı
- **`KoruMsSqlYedek.iss` versiyon senkronu**: `MyAppVersion` `0.14.0` → `0.27.1` olarak AssemblyInfo.cs ile eşleştirildi

### Etkilenen Dosyalar
- `KoruMsSqlYedek.slnLaunch.user`
- `Deployment\InnoSetup\KoruMsSqlYedek.iss`

---

## [0.27.0] - 2026-03-30 — Proje Yeniden Adlandırma + Depolama Sütunu

### Değişiklikler
- **Proje yeniden adlandırma**: `MikroSqlDbYedek` → `KoruMsSqlYedek` — tüm namespace, assembly, klasör, solution, proje ve setup dosyalarında uygulandı (124 kaynak dosya)
- **Inno Setup**: `AppName = "Koru MsSql Yedek"`, service adı `KoruMsSqlYedekService`
- **Git remote**: `https://github.com/hzkucuk/KoruMsSqlYedek` olarak güncellendi

### Yeni Özellik
- **Görev listesi Depolama sütunu**: Planın depolama hedefi artık `☁ Bulut (N)` veya `💾 Yerel` olarak gösteriliyor

### Etkilenen Dosyalar
- Tüm `.cs`, `.csproj`, `.slnx`, `.iss` dosyaları (124 içerik güncellemesi)
- KoruMsSqlYedek.Win/MainWindow.cs (storageLabel)
- KoruMsSqlYedek.Win/MainWindow.Designer.cs (_colCloudTargets.HeaderText = "Depolama")
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (0.27.0.0)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (ApplicationVersion 0.27.0.0)

---

## [0.26.2] - 2026-03-29 — Güvenlik Sertleştirme: TLS/SSH Doğrulama, Hata Mesajı Sanitizasyonu

### Güvenlik
- **FTPS sertifika doğrulaması**: Artık varsayılan olarak sertifika doğrulaması aktif; `FtpsSkipCertificateValidation` ayarı ile yalnızca bilinçli olarak devre dışı bırakılabilir (MITM koruması)
- **SFTP host key doğrulaması**: Trust-on-first-use (TOFU) mekanizması eklendi — ilk bağlantıda parmak izi kaydedilir, sonraki bağlantılarda doğrulanır; uyuşmazlıkta bağlantı reddedilir
- **Hata mesajı sanitizasyonu**: Kullanıcıya gösterilen hata mesajlarından dosya yolları ve stack trace bilgileri temizleniyor (MainWindow + e-posta bildirimi)
- **HTML XSS önlemi**: E-posta bildirimlerindeki hata mesajlarına `HtmlEncode` uygulandı
- **Plaintext şifre uyarısı**: FTP/SFTP ve UNC bağlantılarında DPAPI korumasız şifre tespit edildiğinde log uyarısı verilir

### Yeni Ayarlar (ConfigModels)
- `CloudTargetConfig.FtpsSkipCertificateValidation` — FTPS sertifika doğrulamasını atlar (varsayılan: false)
- `CloudTargetConfig.SftpHostFingerprint` — SFTP sunucu parmak izi (TOFU ile otomatik kaydedilir)

### Etkilenen Dosyalar
- KoruMsSqlYedek.Core/Models/ConfigModels.cs
- KoruMsSqlYedek.Engine/Cloud/FtpSftpProvider.cs
- KoruMsSqlYedek.Engine/Cloud/UncNetworkConnection.cs
- KoruMsSqlYedek.Engine/Notification/EmailNotificationService.cs
- KoruMsSqlYedek.Win/MainWindow.cs
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj

---

## [0.26.1] - 2026-03-29 — Güvenlik & Kod Kalitesi Düzeltmeleri

### Güvenlik
- **FtpSftpProvider**: `BuildRemotePath` — `Path.GetFileName()` ile path traversal (`../`) engellendi
- **FtpSftpProvider**: Constructor'a enum doğrulaması eklendi (Ftp/Ftps/Sftp dışı tip reddedilir)

### Düzeltmeler
- **SqlBackupService**: `BackupDatabaseAsync`, `VerifyBackupAsync`, `RestoreDatabaseAsync`, `ListDatabasesAsync` — `SqlConnection` artık `using var` ile dispose ediliyor; kullanılmayan `CreateServerConnection` metodu silindi
- **FileBackupService**: Constructor — `ArgumentNullException.ThrowIfNull(vssService)` eklendi
- **PasswordProtector**: `IsProtected` boş catch → `Log.Debug` ile hata loglanıyor
- **OneDriveAuthHelper**: `DecryptIfNeeded` boş catch → `Log.Warning` ile hata loglanıyor
- **LocalNetworkProvider**: Bağlantı testi sırasında geçici dosya silinmesi `try/finally` ile garanti altına alındı
- **Magic number**: `1048576.0` sabiti `BytesPerMb` olarak tanımlandı — `SqlBackupService`, `FileBackupService`, `SevenZipCompressionService`, `EmailNotificationService`, `MainWindow`

### Etkilenen Dosyalar
- KoruMsSqlYedek.Engine/Cloud/FtpSftpProvider.cs
- KoruMsSqlYedek.Engine/Backup/SqlBackupService.cs
- KoruMsSqlYedek.Engine/FileBackup/FileBackupService.cs
- KoruMsSqlYedek.Core/Helpers/PasswordProtector.cs
- KoruMsSqlYedek.Engine/Cloud/OneDriveAuthHelper.cs
- KoruMsSqlYedek.Engine/Cloud/LocalNetworkProvider.cs
- KoruMsSqlYedek.Engine/Compression/SevenZipCompressionService.cs
- KoruMsSqlYedek.Engine/Notification/EmailNotificationService.cs
- KoruMsSqlYedek.Win/MainWindow.cs

---

## [0.26.0] - 2025-07-12 — Faz 26: Bulut Yedek Koruma — Gönderilmemiş Dosya Silme Engeli

### Yeni Özellikler
- **Bulut koruma**: Retention temizliği artık buluta başarıyla gönderilmemiş dosyaları silmiyor
  - Plan `BackupMode.Cloud` ise geçmiş kayıtları sorgulanır
  - Tüm cloud upload'ları başarılı olmayan dosyalar retention'dan korunur
  - Geçmiş okunamazsa güvenlik modu: hiçbir dosya silinmez
- **Detaylı retention logları**: Silinen, korunan ve atlanan dosyalar ayrı ayrı raporlanır

### Etkilenen Dosyalar
- KoruMsSqlYedek.Engine/Retention/RetentionCleanupService.cs (IBackupHistoryManager enjeksiyonu, bulut koruma mantığı)
- KoruMsSqlYedek.Tests/RetentionCleanupServiceTests.cs (Moq mock güncellemesi)
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.26.0)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (versiyon 0.26.0)

---

## [0.25.2] - 2025-07-12 — 7z.dll Entegrasyonu: Sıkıştırma Çalışır Hale Getirildi

### Düzeltmeler
- **7z.dll eksikliği düzeltildi**: `Squid-Box.SevenZipSharp` paketi native `7z.dll`'yi içermiyordu — sıkıştırma uyarı verip çalışmıyordu
- **Native DLL entegrasyonu**: `Engine\Native\x64\7z.dll` projeye eklendi, build'de output'a kopyalanır
- **Geliştirilmiş DLL arama**: 3 aşamalı fallback: `x64/7z.dll` → `7z.dll` → `Program Files\7-Zip`

### Etkilenen Dosyalar
- KoruMsSqlYedek.Engine/Native/x64/7z.dll (yeni — native binary)
- KoruMsSqlYedek.Engine/KoruMsSqlYedek.Engine.csproj (Content copy to output)
- KoruMsSqlYedek.Engine/Compression/SevenZipCompressionService.cs (Initialize fallback güncellendi)
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.25.2)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (versiyon 0.25.2)

---

## [0.25.1] - 2025-07-12 — Faz 25b: Autofac Constructor Hatası + Dosya Yedekleme Pipeline

### Düzeltmeler
- **Autofac DependencyResolutionException düzeltildi**: `CloudUploadOrchestrator` iki aynı uzunlukta constructor içeriyordu — `UsingConstructor(typeof(ICloudProviderFactory))` ile belirlendi
- **Dosya yedekleme manuel pipeline'a eklendi**: Plan'da `FileBackup.IsEnabled` aktifse dosya kaynakları yedeklenir
  - VSS ile açık/kilitli dosyalar desteklenir (Outlook PST/OST vb.)
  - Dosya yedekleri sıkıştırılır (dizin → .7z arşiv)
  - Bulut modda dosya arşivi hedeflere yüklenir
- **`IFileBackupService`** MainWindow constructor'ına enjekte edildi

### Etkilenen Dosyalar
- KoruMsSqlYedek.Engine/IoC/EngineModule.cs (UsingConstructor eklendi)
- KoruMsSqlYedek.Win/MainWindow.cs (IFileBackupService eklendi, dosya yedekleme pipeline)
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.25.1)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (versiyon 0.25.1)

---

## [0.25.0] - 2025-07-12 — Faz 25: Manuel Yedekleme Pipeline Tamamlama

### Düzeltmeler
- **Manuel yedekleme tam pipeline**: Sıkıştırma, doğrulama, bulut upload ve geçmiş kayıt artık manuel yedeklemede de çalışıyor
  - SQL Backup → Verify (RESTORE VERIFYONLY) → Compress (.7z LZMA2) → Cloud Upload → History
- **Yeni bağımlılıklar**: `ICompressionService` ve `ICloudUploadOrchestrator` MainWindow'a Autofac ile enjekte edildi
- **`SaveBackupHistory()`**: Yedek sonuçlarını correlationId ile geçmişe kaydeden helper metod
- **Detaylı log çıktısı**: Her pipeline adımı ↳/✓/✗ göstergeleriyle raporlanır

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/MainWindow.cs (constructor genişletildi, OnStartBackupClick tam pipeline, SaveBackupHistory eklendi)
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.25.0)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (versiyon 0.25.0)

---

## [0.24.0] - 2025-07-11 — Faz 24: Wizard Yedekleme Modu Seçimi (Yerel/Bulut)

### Yeni Özellikler
- **Yedekleme Modu Seçimi**: Plan oluştururken ilk adımda yerel veya bulut yedekleme seçimi
  - Yerel: Disk, UNC, ağ paylaşımı, harici disk — Hedefler adımı atlanır (5 adım)
  - Bulut: Google Drive, OneDrive, FTP/SFTP — tüm adımlar gösterilir (6 adım)
- **`BackupMode` enum**: `Local` / `Cloud` seçenekleri (Enums.cs)
- **`BackupPlan.Mode`**: Plan modeli mod bilgisini JSON'da saklar
- **Dinamik wizard navigasyonu**: Mod seçimine göre adımlar otomatik yapılandırılır
- **Dinamik adım göstergesi**: 5 veya 6 nokta, mod değişiminde anında güncellenir

### Etkilenen Dosyalar
- KoruMsSqlYedek.Core/Models/Enums.cs (BackupMode enum)
- KoruMsSqlYedek.Core/Models/BackupPlan.cs (Mode özelliği)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs (RadioButton kontrolleri, _activeSteps)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.cs (dinamik navigasyon, RebuildActiveSteps, RebuildStepIndicator)
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.24.0)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (versiyon 0.24.0)

---

## [0.23.0] - 2025-07-11 — Faz 23: Wizard Adım Yeniden Yapılandırma + İkon Düzeltmeleri

### Yeni Özellikler
- **6 Adımlı Wizard**: 5 adım → 6 adıma yeniden yapılandırıldı
  - Adım 1: Bağlantı (Plan bilgileri + SQL sunucu)
  - Adım 2: Kaynaklar (Veritabanları + Dosya/Klasör kaynakları birlikte)
  - Adım 3: Zamanlama (SQL strateji + Dosya zamanlama — koşullu görünürlük)
  - Adım 4: Sıkıştırma & Saklama (değişmedi)
  - Adım 5: Hedefler (Bulut/Uzak — ayrı adım, açıklayıcı ipucu metni)
  - Adım 6: Bildirim & Rapor (E-posta + Periyodik rapor — son adım)
- **Phosphor Arrow İkonları**: ArrowLeft/ArrowRight sabitleri eklendi, navigasyon butonlarında kullanılıyor

### Düzeltmeler
- **Çift ikon sorunu**: Tüm butonlardan `TextImageRelation` kaldırıldı — ModernButton özel OnPaint ile çakışma giderildi
- **Unicode ok karakterleri**: ▶/◀ buton metinlerinden kaldırıldı, yerine Phosphor ikonları
- **Secondary buton ikon rengi**: Beyaz → TextPrimary (daha iyi kontrast)

### İyileştirmeler
- Form boyutu: 640x640 → 660x680
- Alan genişliği: tw=340 → tw=420
- Hedefler ListView: 430x100 → 478x380 (tam sayfa)
- `UpdateFileScheduleVisibility()`: Dosya zamanlama koşullu görünürlük

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs (tamamen yeniden yazıldı — 6 adım)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.cs (6 adım mantığı, ikon düzeltmeleri)
- KoruMsSqlYedek.Win/Theme/PhosphorIcons.cs (ArrowLeft/ArrowRight sabitleri)
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.23.0)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (versiyon 0.23.0)

---

## [0.22.0] - 2025-07-11 — Faz 22: Cron UI Builder + Tooltips + Raporlama + Layout

### Yeni Özellikler
- **CronBuilderPanel**: Kullanıcı dostu cron zamanlama oluşturucu UserControl
  - Sıklık seçimi: Günlük / Haftalık / Aylık / Özel (Cron)
  - Haftalık: gün seçimi checkbox'ları (Pzt-Paz)
  - Aylık: ayın günü spinner (1-28)
  - Saat/dakika seçimi
  - Canlı önizleme: insan okunabilir açıklama + ham cron ifadesi
  - Mevcut cron ifadelerini geri ayrıştırma (günlük/haftalık/aylık/özel algılama)
- **ToolTip Sistemi**: Tüm form alanlarında detaylı Türkçe açıklamalar ve örnekler (15s görüntüleme)
- **Raporlama Yapılandırması**: Plan bazında yedek rapor ayarları
  - Rapor sıklığı: Günlük / Haftalık / Aylık
  - Alıcı e-posta ve gönderim saati
  - ReportingConfig modeli + ReportFrequency enum

### İyileştirmeler
- **Türkçe etiketler düzeltildi**: "Bağlantıyı Sına", "Yerel Yedek Klasörü", "Sunucu Adı / IP", "Sıkıştırma Algoritması"
- **Form boyutu optimize edildi**: 580x560 → 640x640, etiket sütunu genişletildi (tx=150, tw=340)
- **Bölüm numaralandırma**: ① ② ③ ④ ⑤ ⑥ ile görsel bölüm ayrımı
- **Varsayılan değerler**: Tüm alanlar için anlamlı varsayılanlar (saat 02:00, LZMA2, Ultra sıkıştırma vb.)

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/Controls/CronBuilderPanel.cs (yeni)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs (tamamen yeniden yazıldı)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.cs (CronBuilderPanel + raporlama entegrasyonu)
- KoruMsSqlYedek.Core/Models/Enums.cs (ReportFrequency enum)
- KoruMsSqlYedek.Core/Models/ConfigModels.cs (ReportingConfig sınıfı)
- KoruMsSqlYedek.Core/Models/BackupPlan.cs (Reporting özelliği)
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.22.0)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (versiyon 0.22.0)

---

## [0.21.0] - 2025-07-10 — Faz 21: Plan Düzenleme Wizard + SSL & DB Adı Düzeltmeleri

### Yeni Özellikler
- **Wizard Tabanlı Plan Düzenleme**: 8-tab TabControl → 5 adımlı wizard panele dönüştürüldü
  - Adım 1: Plan Bilgileri + SQL Bağlantı
  - Adım 2: Veritabanı Seçimi (Tümünü Seç + Yenile butonları)
  - Adım 3: Yedekleme Stratejisi & Zamanlama
  - Adım 4: Sıkıştırma & Saklama Politikası
  - Adım 5: Bulut Hedefler + Bildirim + Dosya Yedekleme
- **Adım göstergesi**: Üst barda 5 adım noktası (tamamlanan=✓ yeşil, aktif=beyaz, gelecek=devre dışı)
- **Otomatik veritabanı yükleme**: Adım 1→2 geçişinde bağlantı testi + DB listesi otomatik yüklenir
- **TrustServerCertificate**: SQL bağlantısında SSL sertifika doğrulama kontrolü (varsayılan: güven)

### Hata Düzeltmeleri
- **SSL sertifika hatası düzeltildi**: `Microsoft.Data.SqlClient` v4+ `Encrypt=Mandatory` varsayılanı → `TrustServerCertificate=true` ve `Encrypt=Optional` desteği eklendi
- **Veritabanı adı bozulması düzeltildi**: `CheckedListBox` görüntü metni `"MikroDB_V16 (356 MB)"` yedek adı olarak kaydediliyordu → `DatabaseListItem` sınıfı ile ad ve görüntü metni ayrıştırıldı

### İyileştirmeler
- **BuildCurrentConnInfo()**: SQL bağlantı bilgisi oluşturma tek noktada toplandı (kod tekrarı giderildi)
- **CreateServerConnection()**: `BuildConnectionString()` kullanarak yeniden yazıldı (çift kaynak sorunu giderildi)
- **ApplyIcons()**: Yeni wizard butonları için ikon ataması eklendi (_btnRefreshDatabases)

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs (tamamen yeniden yazıldı)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.cs (wizard navigasyon + düzeltmeler)
- KoruMsSqlYedek.Core/Models/ConfigModels.cs (TrustServerCertificate eklendi)
- KoruMsSqlYedek.Engine/Backup/SqlBackupService.cs (BuildConnectionString + CreateServerConnection)
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.21.0)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (versiyon 0.21.0)

---

## [0.20.0] - 2025-07-09 — Faz 20: .NET 10 Native Dark Mode + Tema Yenileme

### Yeni Özellikler
- **.NET 10 Native Dark Mode**: `Application.SetColorMode(SystemColorMode.Dark)` ile tüm standart WinForms kontrolleri otomatik koyu temada
- **Program.cs**: `ApplyNativeColorMode()` metodu — ModernTheme ayarından native SystemColorMode eşlemesi

### İyileştirmeler
- **Tema Paleti**: Emerald accent(16,185,129), geniş elevation farkı, Tailwind durum renkleri (amber/red/blue)
- **ModernButton**: Ghost stili beyaz alpha overlay (koyu arka planda görünür)
- **ModernCardPanel**: Gölge alpha 15→50 (koyu temada görünür)
- **ModernNumericUpDown**: Yuvarlak köşe (radius=4)
- **ModernLoadingOverlay**: Tema-duyarlı koyu arka plan
- **ModernToggleSwitch**: Off rengi (70,70,78) — koyu tema uyumlu
- **ModernToolStripRenderer**: Dinamik accent rengi, geliştirilmiş hover/press kontrastı

### Temizlik
- **ModernFormBase**: 217→100 satır — DWM hack ve 12+ standart kontrol manual theming kaldırıldı
- **NativeMethods**: `DwmSetWindowAttribute`, `SetWindowTheme` P/Invoke kaldırıldı (native API ile gereksiz)
- **MaterialSkin.2** NuGet paketi kaldırıldı

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/Program.cs
- KoruMsSqlYedek.Win/Theme/ModernFormBase.cs
- KoruMsSqlYedek.Win/Theme/ModernTheme.cs
- KoruMsSqlYedek.Win/Theme/ModernButton.cs
- KoruMsSqlYedek.Win/Theme/ModernCardPanel.cs
- KoruMsSqlYedek.Win/Theme/ModernNumericUpDown.cs
- KoruMsSqlYedek.Win/Theme/ModernLoadingOverlay.cs
- KoruMsSqlYedek.Win/Theme/ModernToggleSwitch.cs
- KoruMsSqlYedek.Win/Theme/ModernToolStripRenderer.cs
- KoruMsSqlYedek.Win/NativeMethods.cs
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj
- FEATURES.md

---

## [0.19.1] - 2025-07-09 — Faz 19: Dashboard & İkon Düzeltmeleri

### Düzeltmeler
- **Dashboard ListView başlık beyaz**: `_lvLastBackups` için `OwnerDraw = true` etkinleştirildi; `DrawColumnHeader` handler'ı tema renkleriyle (GridHeaderBack/GridHeaderText) sütun başlıkları çiziyor
- **KPI kart ikonları görünmüyor**: `_lblStatusIcon`, `_lblNextIcon`, `_lblPlansIcon` — `Label` + `Segoe MDL2 Assets` font → `PictureBox` + Phosphor Render ile değiştirildi
- **PhosphorIcons sessiz hata**: `catch { return null; }` → `Serilog.Log.Error()` ile hata loglanıyor

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/MainWindow.Designer.cs
- KoruMsSqlYedek.Win/MainWindow.cs
- KoruMsSqlYedek.Win/Theme/PhosphorIcons.cs
- FEATURES.md

---

## [0.19.0] - 2026-03-28 — Faz 21: Tam Dark Mode + Phosphor Icons Entegrasyonu

### Değiştirilenler
- **ModernFormBase**: `ApplyControlTheme()` 6 kontrol tipinden 16'ya genişletildi — Button, TextBox, ComboBox, CheckBox, RadioButton, NumericUpDown, CheckedListBox, Label, Panel, TabPage artık otomatik karanlık temada
- **ModernFormBase**: `public` yapıldı (tüm formlar türeyebilsin)
- **DWM Dark Title Bar**: `OnHandleCreated` override'ında `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` — Windows 10/11'de pencere başlık çubuğu artık koyu
- **NativeMethods**: `DwmSetWindowAttribute` P/Invoke ve `DWMWA_USE_IMMERSIVE_DARK_MODE` sabiti eklendi
- **MainWindow**: Base class `Form` → `ModernFormBase`, 9 Button→ModernButton, 4 CheckBox→ModernCheckBox, 1 TabControl→ModernTabControl
- **PlanEditForm**: Base class `Form` → `ModernFormBase`, 10 Button→ModernButton, 1 TabControl→ModernTabControl
- **CloudTargetEditDialog**: Base class `Form` → `ModernFormBase`, 3 Button→ModernButton
- **FileBackupSourceEditDialog**: Base class `Form` → `ModernFormBase`, 3 Button→ModernButton
- **ButtonStyle atamaları**: Primary (Kaydet, Test, Başlat), Secondary (İptal, Gözat, Yenile), Danger (Kaldır), Ghost (Temizle, Dışa Aktar)

### Eklenenler
- **Phosphor Icons (MIT)**: `Phosphor-Fill.ttf` ve `Phosphor-Bold.ttf` gömülü resource olarak eklendi
- **`PhosphorIcons` sınıfı**: TTF font'tan cache'li Bitmap üretir; Get/GetAccent/GetDanger/GetWarning/GetInfo yardımcıları, 32 ikon sabiti
- Tüm formlarda `ApplyIcons()`: butonlar emoji yerine gerçek ikonlar kullanıyor (Play, Stop, Save, Cancel, Folder, Plug, Envelope, Refresh, Export, Eraser, Plus, Pencil, Trash)

### Düzeltmeler
- Beyaz arka planlı butonlar (metin okunmuyor) — ModernButton ile tamamen koyu tema
- Beyaz TextBox/NumericUpDown/ComboBox input alanları — ApplyControlTheme ile otomatik renklendirildi
- Ayarlar iç TabControl beyaz sekme başlıkları — ModernTabControl ile yeşil accent underline
- CheckBox/CheckedListBox OS varsayılan renkleri — tema motoru ile koyu renklendirildi
- Label renk koruma: Yalnızca varsayılan renkli (SystemColors.ControlText / Color.Black) label'lar değiştirilir — dashboard özel renkli label'lar korunur
- **ComboBox çift ok**: `ModernComboBox.DrawBorder()` native dropdown butonunu arka plan rengiyle örterek tek özel ok çiziyor

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/Theme/ModernFormBase.cs
- KoruMsSqlYedek.Win/NativeMethods.cs
- KoruMsSqlYedek.Win/MainWindow.cs
- KoruMsSqlYedek.Win/MainWindow.Designer.cs
- KoruMsSqlYedek.Win/Forms/PlanEditForm.cs
- KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs
- KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs
- KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.Designer.cs
- KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.cs
- KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.Designer.cs
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs

---

## [0.18.1] - 2026-03-28 — Faz 20: Görsel Düzeltmeler & Dark/Light Tema Seçimi

### Düzeltmeler
- **Sekme adları**: `Tab_Dashboard` / `Tab_Plans` vb. kaynak anahtarları her iki .resx dosyasına eklendi — artık "Tab_Dashboard" yerine "Dashboard" / "Planlar" görünüyor
- **ComboBox**: Tüm `System.Windows.Forms.ComboBox` → `Theme.ModernComboBox` olarak değiştirildi; owner-draw ile tam karanlık tema desteği sağlandı
- **ModernComboBox.OnDrawItem**: `Items[e.Index]?.ToString()` → `GetItemText(Items[e.Index])` — `DisplayMember` özelliğine artık saygı gösteriyor (ör. BackupPlan.PlanName)
- **ModernProgressBar**: `_trackColor` varsayılanı `Color.FromArgb(230,232,236)` (açık gri) → `ModernTheme.BorderColor` (koyu temada RGB 70,70,70) olarak güncellendi
- **Yedekleme Butonu**: `_btnStart` sabit `Size(150,32)` → `AutoSize = true + Padding(10,4)` — "▶ Yedeklemeyi Başlat" metni artık kırpılmıyor

### Eklenenler
- **Dark / Light Tema Seçimi**: Ayarlar sekmesine "Tema" açılır menüsü eklendi (Koyu / Açık)
- **AppSettings.Theme**: `"dark"` veya `"light"` değeri JSON ayarlarına kaydediliyor
- **Program.cs ApplyThemeSetting()**: Uygulama başlangıcında tema ayarı okunup uygulanıyor
- **ModernTheme.ApplyTheme()**: Kaydet sonrasında anlık olarak çağrılıyor; tam etki için yeniden başlatma gerekebilir

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/Properties/Resources.resx
- KoruMsSqlYedek.Win/Properties/Resources.tr-TR.resx
- KoruMsSqlYedek.Win/Theme/ModernComboBox.cs
- KoruMsSqlYedek.Win/Theme/ModernProgressBar.cs
- KoruMsSqlYedek.Core/Models/AppSettings.cs
- KoruMsSqlYedek.Win/MainWindow.Designer.cs
- KoruMsSqlYedek.Win/MainWindow.cs
- KoruMsSqlYedek.Win/Program.cs

---

## [0.18.0] - 2026-03-28 — Faz 19: Tek Pencere — Sekmeli Ana Form

### Değiştirilenler
- **UI Mimarisi**: 5 ayrı pencere (Dashboard, Planlar, Loglar, Ayarlar, Manuel Yedekleme) → tek `MainWindow` içinde 5 sekme olarak birleştirildi
- **TrayApplicationContext.cs**: Çoklu form yönetimi kaldırıldı; tek `MainWindow` referansı + `SelectTab(int)` ile sekme yönlendirmesi
- **WinContainerBootstrap.cs**: PlanListForm, LogViewerForm, SettingsForm, ManualBackupDialog kayıtları kaldırıldı; `MainWindow` SingleInstance olarak kaydedildi

### Eklenenler
- **MainWindow.cs** + **MainWindow.Designer.cs**: 5 sekmeli ana form
  - Tab 0 — Dashboard: 3 KPI kart + yedekleme geçmişi ListView + 30s otomatik yenileme
  - Tab 1 — Planlar: ToolStrip (Yeni/Düzenle/Sil/Dışa/İçe Aktar) + DataGridView + CRUD
  - Tab 2 — Yedekleme: Plan seç, DB seç, tür seç, ProgressBar, gerçek zamanlı log + async iptal
  - Tab 3 — Loglar: Dosya seç, seviye/arama filtresi, DataGridView, 5s otomatik takip
  - Tab 4 — Ayarlar: Genel + SMTP sekmeleri, Kaydet/İptal

### Silinenler
- MainDashboardForm.cs / Designer.cs
- Forms/PlanListForm.cs / Designer.cs
- Forms/LogViewerForm.cs / Designer.cs
- Forms/SettingsForm.cs / Designer.cs
- Forms/ManualBackupDialog.cs / Designer.cs

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/MainWindow.cs (yeni)
- KoruMsSqlYedek.Win/MainWindow.Designer.cs (yeni)
- KoruMsSqlYedek.Win/TrayApplicationContext.cs
- KoruMsSqlYedek.Win/IoC/WinContainerBootstrap.cs
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs

---

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
- KoruMsSqlYedek.Win/Theme/ModernTheme.cs
- KoruMsSqlYedek.Win/Theme/ModernToolStripRenderer.cs
- KoruMsSqlYedek.Win/Theme/VersionSidebarRenderer.cs (yeni)
- KoruMsSqlYedek.Win/TrayApplicationContext.cs
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs

---

## [0.16.0] - 2026-03-28 — Faz 17: .NET 10 Migrasyonu

### Değiştirilenler
- Tüm projeler `net48` → `net10.0-windows` hedef çerçevesine yükseltildi (Core, Engine, Win, Service, Tests)
- **Windows Service host:** Topshelf 4.3.0 kaldırıldı → `Microsoft.Extensions.Hosting.WindowsServices 10.0.0` ile değiştirildi
- `BackupWindowsService`: `Start()/Stop()` → `IHostedService.StartAsync/StopAsync` implementasyonu
- `Program.cs` (Service): `HostFactory.Run(...)` → `Host.CreateDefaultBuilder(...).UseWindowsService(...)`
- `ServiceContainerBootstrap`: `Build()` → `Configure(ContainerBuilder)` (Autofac.Extensions.DependencyInjection entegrasyonu)
- `SqlBackupService`: `System.Data.SqlClient` → `Microsoft.Data.SqlClient`
- `KoruMsSqlYedek.Win.csproj`: `ImportWindowsDesktopTargets` kaldırıldı, `App.config` exclude edildi
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
- KoruMsSqlYedek.Core/KoruMsSqlYedek.Core.csproj
- KoruMsSqlYedek.Engine/KoruMsSqlYedek.Engine.csproj
- KoruMsSqlYedek.Service/KoruMsSqlYedek.Service.csproj
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj
- KoruMsSqlYedek.Tests/KoruMsSqlYedek.Tests.csproj
- KoruMsSqlYedek.Service/Program.cs
- KoruMsSqlYedek.Service/BackupWindowsService.cs
- KoruMsSqlYedek.Service/IoC/ServiceContainerBootstrap.cs
- KoruMsSqlYedek.Engine/Backup/SqlBackupService.cs
- KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs

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
- **KoruMsSqlYedek.iss** (Deployment/InnoSetup): Inno Setup kurulum scripti — .NET 4.8 ve Windows 10+ ön koşul kontrolü, bileşen seçimi (Tray+Service/ayrı), Topshelf entegrasyonu, Türkçe+İngilizce dil desteği, LZMA2 sıkıştırma
- **Build-Release.ps1** (Deployment): Otomasyon scripti — NuGet restore, build, test, publish, ZIP paketleme
- **install-service.cmd** (Deployment): Windows Service kurulum helper scripti (yönetici yetkisi kontrolü)
- **uninstall-service.cmd** (Deployment): Windows Service kaldırma helper scripti
- **setup_readme.txt** (Deployment/InnoSetup): Kurulum bilgi dosyası

### Değiştirilenler
- **INSTALL.md**: Üretim kurulumu bölümü güncellendi — Inno Setup ve Build-Release.ps1 kullanım talimatları eklendi

### Etkilenen Dosyalar
- Deployment/Build-Release.ps1 (yeni)
- Deployment/InnoSetup/KoruMsSqlYedek.iss (yeni)
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
- KoruMsSqlYedek.Tests/FileBackupServiceTests.cs (yeni)
- KoruMsSqlYedek.Tests/EmailNotificationServiceTests.cs (yeni)
- KoruMsSqlYedek.Tests/AppSettingsManagerTests.cs (yeni)
- KoruMsSqlYedek.Tests/Helpers/TestDataFactory.cs (güncelleme)

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
- KoruMsSqlYedek.Win/Properties/Resources.resx (yeni)
- KoruMsSqlYedek.Win/Properties/Resources.tr-TR.resx (yeni)
- KoruMsSqlYedek.Win/Helpers/Res.cs (yeni)
- KoruMsSqlYedek.Win/Program.cs (güncelleme)
- KoruMsSqlYedek.Win/TrayApplicationContext.cs (güncelleme)
- KoruMsSqlYedek.Win/MainDashboardForm.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/PlanListForm.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/ManualBackupDialog.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/SettingsForm.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/LogViewerForm.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.cs (güncelleme)

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
- KoruMsSqlYedek.Engine/IoC/EngineModule.cs (yeni)
- KoruMsSqlYedek.Engine/Scheduling/AutofacJobFactory.cs (yeni)
- KoruMsSqlYedek.Engine/Scheduling/QuartzSchedulerService.cs (güncelleme)
- KoruMsSqlYedek.Win/IoC/WinContainerBootstrap.cs (yeni)
- KoruMsSqlYedek.Win/Program.cs (güncelleme)
- KoruMsSqlYedek.Win/TrayApplicationContext.cs (güncelleme)
- KoruMsSqlYedek.Win/MainDashboardForm.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/PlanListForm.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/ManualBackupDialog.cs (güncelleme)
- KoruMsSqlYedek.Win/Forms/SettingsForm.cs (güncelleme)
- KoruMsSqlYedek.Service/IoC/ServiceContainerBootstrap.cs (yeni)
- KoruMsSqlYedek.Service/Program.cs (güncelleme)

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
- KoruMsSqlYedek.Core/Models/AppSettings.cs (yeni)
- KoruMsSqlYedek.Core/Interfaces/IAppSettingsManager.cs (yeni)
- KoruMsSqlYedek.Engine/AppSettingsManager.cs (yeni)
- KoruMsSqlYedek.Win/MainDashboardForm.cs (güncelleme — canlı veri)
- KoruMsSqlYedek.Win/Forms/LogViewerForm.cs + .Designer.cs (yeni)
- KoruMsSqlYedek.Win/Forms/SettingsForm.cs + .Designer.cs (yeni)
- KoruMsSqlYedek.Win/Forms/ManualBackupDialog.cs + .Designer.cs (yeni)
- KoruMsSqlYedek.Win/TrayApplicationContext.cs (menü handler'ları güncelleme)

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
- KoruMsSqlYedek.Win/Forms/PlanListForm.cs + .Designer.cs (yeni)
- KoruMsSqlYedek.Win/Forms/PlanEditForm.cs + .Designer.cs (yeni)
- KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs + .Designer.cs (yeni)
- KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.cs + .Designer.cs (yeni)
- KoruMsSqlYedek.Win/TrayApplicationContext.cs (Planlar menüsü entegrasyonu)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (SDK-style dönüşümü)

---

## [0.8.0] - 2025-07-18 — Faz 9: Win UI — Tray App Altyapısı

### Eklenenler
- **TrayApplicationContext:** System Tray tabanlı ApplicationContext — NotifyIcon ile tray'de çalışma, ContextMenuStrip menü (Dashboard Aç, Planlar, Manuel Yedekleme, Log Görüntüle, Ayarlar, Çıkış), çift tıkla Dashboard açma, balloon tip bildirimleri, durum bazlı ikon güncelleme
- **Program.cs yeniden yapılandırma:** Global Mutex ile tek instance (WM_SHOWFIRSTINSTANCE broadcast ile mevcut instance aktivasyonu), Serilog file sink (rolling daily, 30 gün retention, %APPDATA%\KoruMsSqlYedek\Logs\), Application.ThreadException + AppDomain.UnhandledException global exception handler'ları
- **MainDashboardForm:** Dashboard iskeleti — TLP layout (başlık, 3x2 durum özeti paneli, "Son Yedeklemeler" GroupBox + ListView 6 kolon), StatusStrip (durum + versiyon), UserClosing → Hide (tray'de kalır)
- **SymbolIconHelper:** Segoe MDL2 Assets / Segoe UI Symbol fontundan Icon ve Bitmap render etme, TrayIconStatus enum (Idle/Running/Success/Warning/Error) ile durum bazlı tray ikonu
- **NativeMethods:** Win32 P/Invoke tanımları — DestroyIcon, SetForegroundWindow, ShowWindow, RegisterWindowMessage, SendMessage
- **Win.csproj güncellemeleri:** Serilog 3.1.1 + Serilog.Sinks.File 5.0.0 PackageReference, RestoreProjectStyle, RuntimeIdentifier=win

### Etkilenen Dosyalar
- KoruMsSqlYedek.Win/TrayApplicationContext.cs (yeni)
- KoruMsSqlYedek.Win/MainDashboardForm.cs + MainDashboardForm.Designer.cs (yeni)
- KoruMsSqlYedek.Win/Helpers/SymbolIconHelper.cs (yeni)
- KoruMsSqlYedek.Win/NativeMethods.cs (yeni)
- KoruMsSqlYedek.Win/Program.cs (yeniden yazıldı — Mutex, Serilog, TrayApplicationContext)
- KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj (NuGet, RuntimeIdentifier)

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
- KoruMsSqlYedek.Core/Interfaces/ICloudProviderFactory.cs (yeni)
- KoruMsSqlYedek.Core/Interfaces/ICloudUploadOrchestrator.cs (DeleteFromAllAsync, TestAllConnectionsAsync, CloudDeleteResult, CloudConnectionTestResult eklendi)
- KoruMsSqlYedek.Engine/Cloud/CloudProviderFactory.cs (yeni)
- KoruMsSqlYedek.Engine/Cloud/CloudUploadOrchestrator.cs (factory constructor, GetProvider, DeleteFromAllAsync, TestAllConnectionsAsync)
- KoruMsSqlYedek.Tests/CloudProviderFactoryTests.cs (yeni — 13 test)
- KoruMsSqlYedek.Tests/CloudUploadOrchestratorTests.cs (14 yeni test)

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
- KoruMsSqlYedek.Engine/Cloud/OneDriveProvider.cs (tam implementasyon)
- KoruMsSqlYedek.Engine/Cloud/OneDriveAuthHelper.cs (yeni)
- KoruMsSqlYedek.Engine/KoruMsSqlYedek.Engine.csproj (Microsoft.Identity.Client eklendi)
- KoruMsSqlYedek.Tests/OneDriveProviderTests.cs (yeni — 21 test)

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
- KoruMsSqlYedek.Engine/Cloud/GoogleDriveProvider.cs (tam implementasyon)
- KoruMsSqlYedek.Engine/Cloud/GoogleDriveAuthHelper.cs (yeni)
- KoruMsSqlYedek.Core/Models/ConfigModels.cs (OAuthClientId, OAuthClientSecret eklendi)
- KoruMsSqlYedek.Tests/GoogleDriveProviderTests.cs (yeni — 17 test)

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
- KoruMsSqlYedek.Engine/Cloud/LocalNetworkProvider.cs (tam implementasyon)
- KoruMsSqlYedek.Engine/Cloud/UncNetworkConnection.cs (yeni)
- KoruMsSqlYedek.Engine/BackupHistoryManager.cs (custom directory desteği)
- KoruMsSqlYedek.Tests/LocalNetworkProviderTests.cs (yeni)
- KoruMsSqlYedek.Tests/BackupHistoryManagerTests.cs (temp dizin izolasyonu)

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
- KoruMsSqlYedek.Engine/Cloud/FtpSftpProvider.cs (tam implementasyon — önceki placeholder yerine)
- KoruMsSqlYedek.Tests/FtpSftpProviderTests.cs (yeni)
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
- KoruMsSqlYedek.Tests/KoruMsSqlYedek.Tests.csproj (Moq, FluentAssertions eklendi)
- KoruMsSqlYedek.Tests/Helpers/TestDataFactory.cs (yeni)
- KoruMsSqlYedek.Tests/BackupJobExecutorTests.cs (yeni)
- KoruMsSqlYedek.Tests/CloudUploadOrchestratorTests.cs (yeni)
- KoruMsSqlYedek.Tests/PlanManagerTests.cs (genişletildi)
- KoruMsSqlYedek.Tests/BackupChainValidatorTests.cs (yeni)
- KoruMsSqlYedek.Tests/BackupHistoryManagerTests.cs (yeni)
- KoruMsSqlYedek.Tests/RetentionCleanupServiceTests.cs (yeni)
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
- KoruMsSqlYedek.Core/Interfaces/ICloudUploadOrchestrator.cs (yeni)
- KoruMsSqlYedek.Core/Interfaces/IBackupHistoryManager.cs (yeni)
- KoruMsSqlYedek.Engine/PlanManager.cs (yeniden yazıldı)
- KoruMsSqlYedek.Engine/Backup/BackupChainValidator.cs (genişletildi)
- KoruMsSqlYedek.Engine/Compression/SevenZipCompressionService.cs (genişletildi)
- KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs (yeniden yazıldı)
- KoruMsSqlYedek.Engine/BackupHistoryManager.cs (yeni)
- KoruMsSqlYedek.Engine/Notification/EmailNotificationService.cs (genişletildi)
- KoruMsSqlYedek.Engine/Cloud/CloudUploadOrchestrator.cs (değiştirildi)
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
- KoruMsSqlYedek.Core/* (tüm dosyalar)
- KoruMsSqlYedek.Engine/* (tüm dosyalar)
- KoruMsSqlYedek.Service/* (tüm dosyalar)
- KoruMsSqlYedek.Tests/* (tüm dosyalar)
