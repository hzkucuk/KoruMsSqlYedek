## [0.92.2] - 2026-06-28 — Progress Bar %100 Hatası Kesin Düzeltme + Diagnostik Loglama

### Düzeltme
- **Progress bar Completed öncesi asla %100 göstermez** — Tüm ara hesaplamalar (DatabaseProgress, StepChanged, CloudUploadProgress) artık Math.Min(pct, 99) ile sınırlanıyor. Sadece Completed eventi %100 ayarlıyor.
- **StartConsolidatedCloudPhase artık expectedBase kullanıyor** — CloudPhaseBase = expectedBase (ağırlık modelinin deterministik değeri). MaxPercent şişmesine karşı bağışık.
- **Progress bar bulut yükleme metni düzeltildi** — Bar metni artık ham batch yüzdesini (e.ProgressPercent) gösteriyor, kümülatif değil.

### Diagnostik
- PlanProgressTracker: 6 metoda Serilog debug loglama eklendi (StartConsolidatedCloudPhase, CalculateDatabaseProgress, CalculateLocalStepProgress, CalculateCloudUploadProgress, CalculateFileBackupPhaseStart, CalculateFileSourceProgress)
- MainWindow.BackupActivity: Tüm _progressBar.Value atamalarına debug loglama eklendi
- Sonraki çalıştırmada kök neden kesin olarak tespit edilecek

### Test
- 2 test güncellendi (CloudPhaseBase artık expectedBase kullanıyor): StartConsolidatedCloudPhase_RecordsBasePercent, CloudUpload_Consolidated_WeightDistribution_SqlOnly
- 56 test geçiyor

### Etkilenen Dosyalar
- `PlanProgressTracker.cs` — StartConsolidatedCloudPhase fix + 6 metoda diagnostik loglama
- `MainWindow.BackupActivity.cs` — Safety cap (99) + diagnostik loglama + bar metin düzeltmesi
- `PlanProgressTrackerTests.cs` — 2 test güncellendi

---

## [0.92.1] - 2026-06-27 — Progress Bar Bulut Yükleme Sırasında %100 Gösterme Hatası Düzeltildi

### Düzeltme
- **Progress bar bulut yükleme sırasında %100 gösteriyordu** — Bulut yükleme %11 iken progress bar %100 gösteriyordu. Kök neden: `CalculateCloudUploadProgress` global `MaxPercent` değerini taban olarak kullanıyordu (`Math.Max(cumPct, MaxPercent)`). MaxPercent herhangi bir nedenle şiştiğinde bulut ilerlemesi hep 100 dönerdi.
- **3 katmanlı savunma düzeltmesi:**
  1. `CloudPhaseBase` artık ağırlık modelinin beklenen tavanı ile sınırlanıyor (`Math.Min(MaxPercent, expectedBase)`)
  2. Bulut fazı kendi monoton artış izleyicisini (`_maxCloudPercent`) kullanıyor — global `MaxPercent` şişmesinden etkilenmiyor
  3. `CalculateLocalStepProgress` bulut fazı sırasında devre dışı (`IsConsolidatedCloudPhase` guard)

### Test
- 4 yeni test eklendi (56 toplam):
  - `CloudUpload_InflatedMaxPercent_DoesNotCorruptCloudProgress`
  - `CloudUpload_InflatedMaxPercent_SqlOnlyPlan_CappedTo40`
  - `CloudUpload_InflatedMaxPercent_FileOnlyPlan_CappedTo25`
  - `LocalStepProgress_InConsolidatedCloudPhase_ReturnsMinusOne`

### Etkilenen Dosyalar
- `PlanProgressTracker.cs` — 3 katmanlı savunma: CloudPhaseBase cap, _maxCloudPercent, IsConsolidatedCloudPhase guard
- `PlanProgressTrackerTests.cs` — 4 yeni test

---

## [0.92.0] - 2026-06-27 — Konsolide Bulut Yükleme: Tek Seferde Toplu Upload

### Yeni Özellik
- **SQL ve dosya yedeklerinin bulut yüklemeleri tek bir toplu fazda birleştirildi** — Daha önce her SQL veritabanı yedeklemesinden sonra ayrı ayrı buluta yükleniyor ve dosya yedekleri de ayrı bir fazda yükleniyordu. Artık tüm yedek dosyaları (SQL + dosya) yerel işlemler tamamlandıktan sonra tek bir toplu fazda buluta gönderiliyor.
- **UploadBatchToAllAsync**: CloudUploadOrchestrator'a yeni toplu yükleme metodu eklendi — tüm dosyaları tüm hedeflere sırayla yükler, kümülatif byte-bazlı ilerleme raporlar
- **UploadAllPendingAsync**: BackupJobExecutor'a konsolide yükleme yardımcı metodu eklendi — SQL pending uploads + dosya arşivini birleştirip toplu yüklemeye gönderir
- **PlanProgressTracker konsolide model**: Yeni ağırlık modeli — SQL lokal → Dosya lokal → Konsolide bulut fazı. Progress bar artık %100'e ulaşır
- **CloudFileName / CloudFileIndex / CloudFileTotal**: BackupActivityEventArgs'a yeni özellikler — hangi dosyanın yüklendiği ve toplam dosya sayısı UI'da gösterilir
- **ICloudUploadOrchestrator**: UploadBatchToAllAsync interface metodu eklendi

### Düzeltme
- **Progress bar %100'e ulaşmıyordu** — Ayrı SQL ve dosya bulut fazları arasındaki ağırlık model karmaşıklığı nedeniyle progress bar %100'e ulaşamıyordu. Konsolide model ile düzeltildi.

### Etkilenen Dosyalar
- `BackupActivityEvent.cs` — CloudFileName, CloudFileIndex, CloudFileTotal eklendi
- `ICloudUploadOrchestrator.cs` — UploadBatchToAllAsync eklendi
- `CloudUploadOrchestrator.cs` — UploadBatchToAllAsync implementasyonu
- `BackupJobExecutor.SqlPipeline.cs` — Bulut yükleme kaldırıldı, pending uploads döndürüyor
- `BackupJobExecutor.FilePipeline.cs` — Bulut yükleme kaldırıldı, arşiv yolu döndürüyor
- `BackupJobExecutor.Helpers.cs` — UploadAllPendingAsync eklendi
- `BackupJobExecutor.cs` — Execute() konsolide pipeline'a yeniden bağlandı
- `PlanProgressTracker.cs` — Konsolide bulut ağırlık modeli
- `MainWindow.BackupActivity.cs` — Konsolide bulut fazı UI desteği
- `PlanProgressTrackerTests.cs` — Konsolide model testleri

## [0.91.0] - 2026-06-26 — Büyük Dosya Refactoring: Partial Class Ayrımı

### Yeni Özellik
- **12 büyük dosya partial class'lara ayrıldı** — Kod okunabilirliği ve bakım kolaylığı için tüm büyük dosyalar mantıksal birimlere bölündü:
  1. `MainWindow.cs` (2197→350 satır) + 6 partial: Dashboard, Plans, BackupExecution, BackupActivity, LogViewer, Settings
  2. `BackupJobExecutor.cs` (1043→278 satır) + 3 partial: SqlPipeline, FilePipeline, Helpers
  3. `EmailNotificationService.cs` (956→126 satır) + 4 partial: SqlNotification, FileNotification, CloudNotification, JobNotification
  4. `PlanEditForm.cs` (959→129 satır) + 4 partial: WizardNavigation, PlanBinding, CloudAndFileSources, Visibility
  5. `TrayApplicationContext.cs` (788→307 satır) + 3 partial: ServiceControl, BackupActivity, UpdateCheck
  6. `SqlBackupService.cs` (877→186 satır) + 3 partial: Operations, VssBackup, Helpers
  7. `FileSystemCheckedTreeView.cs` (928→321 satır) + 3 partial: NodeLoading, Filtering, SizeCalculation
  8. `CloudUploadOrchestrator.cs` (638→234 satır) + 2 partial: CloudOperations, RetryAndRecovery
  9. `MegaProvider.cs` (629→261 satır) + 1 partial: Operations (4 kritik pattern korundu)
  10. `GoogleDriveProvider.cs` (608→238 satır) + 1 partial: Operations
  11. `FtpSftpProvider.cs` (594→226 satır) + 2 partial: Ftp, Sftp
  12. `FileBackupService.cs` (523→288 satır) + 2 partial: CopyAndVerify, FileCollection
- Toplam **30+ yeni partial dosya** oluşturuldu
- Tüm mevcut davranış ve public API'ler korundu, breaking change yok

## [0.90.3] - 2026-06-25 — IsSuccess Hesaplama Düzeltmesi

### Düzeltme
- **E-posta bildirimi SQL/dosya başarısızlığında "Başarılı" gösteriyordu** — `IsSuccess` hesaplaması yalnızca bulut upload sonucunu kontrol ediyordu. SQL yedekleme veya dosya yedekleme başarısız olduğunda bile bildirim "Başarılı" olarak gönderiliyordu. `overallSuccess = allCloudOk && !anySqlFailed && !anyFileFailed` formülü ile düzeltildi. Her iki yol da (SQL+Dosya ve sadece Dosya) güncellenip test edildi.

## [0.90.2] - 2026-06-24 — ListView Grup Expand/Collapse P/Invoke Düzeltmesi

### Düzeltme
- **ListView grup expand/collapse çalışmıyordu** — .NET'in `ShowGroups` setter'ı `value == current` ise `LVM_ENABLEGROUPVIEW` mesajını göndermiyordu. `Groups.Clear()` sonrası grup görünümü kayboluyordu. `ListViewHeaderPainter.EnableGroupView()` ile doğrudan P/Invoke üzerinden `LVM_ENABLEGROUPVIEW(TRUE)` gönderilerek düzeltildi. ShowGroups force-toggle + P/Invoke belt-and-suspenders yaklaşımı ile her çağrıda garanti edildi.

## [0.90.1] - 2026-06-24 — ListView Grup Görünümü Kök Neden Düzeltmesi

### Düzeltme
- **ListView grupları görünmüyordu (kök neden)** — `ShowGroups = true` atandığında `Groups.Count == 0` olduğu için .NET dahili olarak `LVM_ENABLEGROUPVIEW(FALSE)` gönderiyordu. `ShowGroups` ataması, gruplar eklendikten sonraya taşınarak düzeltildi. Collapsible grup başlıkları artık doğru çalışıyor.

## [0.90.0] - 2026-06-23 — ListView Grup Başlıkları + SMTP Profil Ekleme

### Düzeltme
- **ListView grup başlıkları görünmüyor** — `SetWindowTheme("DarkMode_Explorer")` uygulanarak native grup başlıkları dark modda düzgün render edilir hale getirildi. Collapsible grup başlıkları artık görünür.
- **LVS_EX_DOUBLEBUFFER** eklendi — ListView grup ve item geçişlerinde flicker önlendi.

### İyileştirme
- **SMTP profil linki çalışır hale getirildi** — PlanEditForm'da "Profil ekle / düzenle" linki artık MessageBox yerine SmtpProfileEditDialog açar. Yeni profil oluşturulunca ComboBox otomatik güncellenir ve yeni profil seçilir.

## [0.89.0] - 2026-06-22 — UI Düzeltmeleri: Ayarlar ComboBox, Yedek Türü Genişlik, Navigasyon İkonları

### Düzeltme
- **Ayarlar > Genel sekmesinde görünmeyen ComboBox'lar** — Dil, Tema ve Log Konsol Teması ComboBox'ları 1px genişlikteydi (Size=1,34). `Dock=Fill` eklenerek TLP sütununa tam genişletildi.
- **"Yedek Türü" ComboBox'u çok dar** — "Incremental (Artırımlı)" metni sığmıyordu. `Dock=Fill` eklenerek genişletildi.

### İyileştirme
- **İleri/Geri butonlarına DevExpress ikonları** — PlanEditForm navigasyon butonlarına Import_16x16.png (Geri) ve Export_16x16.png (İleri) ikonları eklendi. Unicode ok karakterleri kaldırıldı.

## [0.88.0] - 2026-06-22 — Konsolide Bildirim: Tek E-posta

### Yeni Özellik
- **Konsolide e-posta bildirimi** — Görev tamamlandığında SQL yedekleme, dosya yedekleme ve bulut yükleme sonuçları tek bir e-posta olarak gönderilir (eskiden 2+ ayrı e-posta gidiyordu).
- E-postada **görev logu** bölümü — Log ekranındaki tüm adım bilgileri e-postaya dahil edilir.
- `JobNotificationData` modeli — Tüm görev sonuçlarını (SQL + dosya + bulut) tek nesnede toplar.
- `INotificationService.NotifyJobCompletedAsync` — Yeni konsolide bildirim metodu.
- E-posta şablonu: SQL veritabanı tablosu, dosya yedek özeti, bulut yükleme detayları ve görev logu bölümleri.

### Değişiklik
- `BackupJobExecutor.Execute()` — Per-DB ve per-component bildirimler kaldırıldı; tüm sonuçlar toplanıp görev sonunda tek bildirim gönderilir.
- `BackupJobExecutor.ExecuteSqlBackupAsync` — `(bool, List<BackupResult>)` tuple döndürür.
- `BackupJobExecutor.ExecuteFileBackupAsync` — `(bool, List<FileBackupResult>, List<CloudUploadResult>, string)` tuple döndürür.
- `NotifyIfConfigured` ve `NotifyFileBackupIfConfiguredAsync` metotları kaldırıldı.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/JobNotificationData.cs` — Yeni model
- `KoruMsSqlYedek.Core/Interfaces/INotificationService.cs` — NotifyJobCompletedAsync eklendi
- `KoruMsSqlYedek.Engine/Notification/EmailNotificationService.cs` — NotifyJobCompletedAsync + BuildJobCompletedEmailBody
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs` — Konsolide bildirim akışı

## [0.87.0] - 2026-06-22 — DevExpress PNG İkonları Tüm Formlara + Animasyonlu Tray İkonları + ListView Grup Collapse Düzeltmesi

### Yeni Özellik
- **DevExpress PNG ikonları tüm formlara** — MainWindow, PlanEditForm, CloudTargetEditDialog, FileBackupSourceEditDialog'daki tüm PhosphorIcons kullanımları DevExpress Images kütüphanesi PNG'lerine değiştirildi (23 ikon).
- **Animasyonlu tray ikonları** — Yedekleme sırasında Icons8 CloudSync animasyonlu GIF, tamamlanınca CheckMark animasyonlu GIF tray'de gösterilir.
- `SymbolIconHelper.ExtractGifFrames` — Gömülü GIF dosyasından tray ikonu boyutunda kareler çıkarır.
- Tamamlanma animasyonu bitince otomatik idle ikona döner.

### Düzeltme
- **ListView grup collapse/expand (+/−) butonları** — `OwnerDraw=true` native grup header renderını engelliyordu. `OwnerDraw` kaldırıldı, kolon başlık boyama `ListViewHeaderPainter` NativeWindow subclass'ına taşındı.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/MainWindow.cs` — ApplyIcons (PhosphorIcons→DevExpress PNG)
- `KoruMsSqlYedek.Win/MainWindow.Designer.cs` — OwnerDraw, DrawItem/DrawSubItem kaldırıldı
- `KoruMsSqlYedek.Win/Theme/ListViewHeaderPainter.cs` — Yeni: NativeWindow header custom draw
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.cs` — ApplyIcons (DevExpress PNG)
- `KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs` — ApplyIcons (DevExpress PNG)
- `KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.cs` — ApplyIcons (DevExpress PNG)
- `KoruMsSqlYedek.Win/TrayApplicationContext.cs` — GIF animasyon, completion animation
- `KoruMsSqlYedek.Win/Helpers/SymbolIconHelper.cs` — ExtractGifFrames
- `KoruMsSqlYedek.Win/Resources/Icons/` — 16 yeni PNG ikon
- `KoruMsSqlYedek.Win/Resources/TrayIcons/` — CloudSync.gif, CheckMark.gif

## [0.86.0] - 2026-06-21 — Toolbar: DevExpress PNG İkonları + Şifre Butonları Kaldırıldı

### Yeni Özellik
- **DevExpress PNG ikonları** — Toolbar butonları DevExpress Images kütüphanesinden renkli 16x16 PNG ikonlar kullanıyor (New, Edit, Delete, Export, Import, Refresh, Find).
- `LoadToolStripIcon` yardımcı metodu — EmbeddedResource’tan ikon yükleme.

### Kaldırılan
- Toolbar’dan şifre butonları kaldırıldı (`_tsbPassword`, `_tsmiPasswordToggle`, `_tsmiPasswordSetup`).
- `UpdatePasswordButtonIcon` metodu ve `OnPasswordSetupClick`, `OnPasswordToggleClick` handler’ları kaldırıldı.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/MainWindow.cs` — ApplyToolStripIcons, LoadToolStripIcon
- `KoruMsSqlYedek.Win/MainWindow.Designer.cs` — Şifre butonları kaldırıldı
- `KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj` — EmbeddedResource Icons/*.png
- `KoruMsSqlYedek.Win/Resources/Icons/` — 7 adet PNG ikon dosyası

## [0.85.1] - 2026-06-21 — Fix: Zamanlanmış görev manualTrigger KeyNotFoundException

### Düzeltme
- `BackupJobExecutor.Execute` — `context.MergedJobDataMap.GetString("manualTrigger")` zamanlanmış tetiklemede `KeyNotFoundException` fırlatıyordu. `ContainsKey` kontrolü eklendi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs`

## [0.85.0] - 2026-06-21 — Dashboard: Plan Bazlı Gruplandırma

### Yeni Özellik
- **ListView grup görünümü** — "Son Yedeklemeler" listesi plan adına göre gruplandırıldı. Her grup başlığında plan adı ve yedekleme sayısı gösterilir.
- **Açılır/kapanır gruplar** — `ListViewGroupCollapsedState.Expanded` ile +/− butonlarıyla gruplar daraltılıp genişletilebilir.
- `BeginUpdate/EndUpdate` ile performans optimizasyonu.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/MainWindow.cs` — `LoadRecentBackups()` grup mantığı
- `KoruMsSqlYedek.Win/Properties/Resources.resx` — `Dashboard_GroupBackupCount` eklendi
- `KoruMsSqlYedek.Win/Properties/Resources.tr-TR.resx` — `Dashboard_GroupBackupCount` eklendi

## [0.84.0] - 2026-06-21 — E-posta Şablonları: Bulut Yükleme Detayları & Hata Kodları

### Yeni Özellik
- **SQL yedekleme bildirimi — bulut yükleme detayları** — Başarılı yüklemelerde uzak dosya yolu (`RemoteFilePath`) ve boyutu, başarısız yüklemelerde detaylı hata mesajı ve deneme sayısı gösterilir.
- **Dosya yedekleme bildirimi — zengin içerik** — Arşiv dosyası adı/boyutu, kaynak başına süre/boyut, başarısız dosya listesi (`FailedFiles`), bulut yükleme sonuçları (uzak yol, boyut, hata). Daha önce gönderilmeyen dosya yedek bildirimi artık `BackupJobExecutor`'dan çağrılıyor.
- **Bulut başarısızlık bildirimi — detaylı hata** — Provider türü, başarısız hedef sayısı, hata mesajı limiti 300→500 karakter.
- **Periyodik rapor — bulut & hata bölümleri** — Detay tablosuna “Bulut” kolonu (başarılı/toplam), “Hata Detayları” bölümü (başarısız yedeklemelerin hata mesajları), “Başarısız Bulut Yüklemeleri” bölümü (hedef, deneme, hata).

### Değişiklik
- `INotificationService` — `NotifyFileBackupAsync` metodu eklendi (bulut sonuçları + arşiv bilgisi parametreleri).
- `EmailNotificationService` — `FormatErrorDetail`, `FormatBytes` yardımcı metotları.
- `ReportingService` — `SanitizeForReport` yardımcı metodu.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Interfaces/INotificationService.cs` — NotifyFileBackupAsync eklendi
- `KoruMsSqlYedek.Engine/Notification/EmailNotificationService.cs` — Tüm e-posta şablonları zenginleştirildi
- `KoruMsSqlYedek.Engine/Notification/ReportingService.cs` — Rapor şablonu güncellendi
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs` — NotifyFileBackupIfConfiguredAsync eklendi

## [0.83.1] - 2026-06-21 — İlerleme Çubuğu Ağırlık Modeli Düzeltmesi

### Düzeltme
- **İlerleme çubuğu dosya yedeklemede %100'e zıplıyor** — Bulut hedefi olmayan planlarda dosya yedekleme aşamaları (kopyalama, sıkıştırma, temizlik) için ağırlık dağılımı düzeltildi.
  - Bulut yokken: Dosya-only planlarda copy=%50, compress=%35, cleanup=%5 (toplam %90).
  - Bulut yokken: SQL+Dosya planlarda copy=%10, compress=%7, cleanup=%2 (toplam %19, SQL üstüne eklenir).
  - `PlanProgressTracker`: `GetFileCopyWeight`/`GetFileCompressWeight` helper metotları, `CalculateFileCleanupProgress` yeni metot.
  - `MainWindow`: StepChanged handler'a dosya temizlik case'i eklendi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/PlanProgressTracker.cs` — Cloud-aware ağırlık modeli
- `KoruMsSqlYedek.Win/MainWindow.cs` — Dosya temizlik ilerleme handler

## [0.83.0] - 2026-06-21 — Dosya Yedekleme Fark/Artırımlı Strateji & İlerleme Çubuğu Düzeltmesi

### Yeni Özellik
- **Dosya yedekleme stratejisi (Tam/Fark/Artırımlı)** — Dosya yedekleme görevleri artık üç strateji destekliyor:
  - **Tam Yedek:** Her seferinde tüm dosyalar yedeklenir (varsayılan).
  - **Fark Yedek:** Son tam yedekten bu yana değişen dosyalar yedeklenir.
  - **Artırımlı Yedek:** Son yedekten (tam veya artırımlı) bu yana değişen dosyalar yedeklenir.
  - JSON manifest sistemi: `{LocalPath}/Manifests/file_full.json` (son tam yedek) + `file_last.json` (son herhangi yedek).
  - Dosya karşılaştırma: `LastWriteTimeUtc` + `Size` üzerinden (hash yok, performans için).
  - Fark/artırımlı yedekte değişen dosya yoksa manifest güncellenir, boş arşiv oluşturulmaz.
- **Plan düzenleme formunda strateji seçimi** — Adım 3'te dosya yedekleme bölümüne "Strateji" ComboBox eklendi.

### Düzeltme
- **İlerleme çubuğu dosya yedeklemeyi yansıtmıyor** — Dosya kaynaklarının kopyalanma ilerlemesi artık ilerleme çubuğuna yansıtılıyor.
  - `PlanProgressTracker.CalculateFileSourceProgress`: Dosya-only planlarda %0-25, SQL+dosya planlarda %80-85 aralığında kaynak bazlı ilerleme.
  - `BackupJobExecutor`: Her kaynak için `CurrentIndex`/`TotalCount` event parametreleri eklendi.
  - `MainWindow`: StepChanged handler'a dosya kaynak ilerleme hesaplaması eklendi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/FileBackupModels.cs` — FileBackupStrategy enum, Strategy property, BackedUpFilePaths
- `KoruMsSqlYedek.Core/PlanProgressTracker.cs` — CalculateFileSourceProgress metodu
- `KoruMsSqlYedek.Engine/FileBackup/FileBackupManifestManager.cs` — YENİ DOSYA: Manifest model ve yönetici
- `KoruMsSqlYedek.Engine/FileBackup/FileBackupService.cs` — Strateji tabanlı filtreleme, manifest kaydetme
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs` — Strateji etiketi, kaynak bazlı event'ler
- `KoruMsSqlYedek.Win/MainWindow.cs` — Dosya kaynak ilerleme handler
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs` — Strateji ComboBox kontrolleri
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.cs` — Strateji yükleme/kaydetme
- `KoruMsSqlYedek.Win/Properties/Resources.resx` — FileStrat resource key'leri
- `KoruMsSqlYedek.Win/Properties/Resources.tr-TR.resx` — FileStrat Türkçe çeviriler

## [0.82.0] - 2026-04-05 — Bulut Yükleme Hata Yönetimi & Bildirim

### Yeni Özellik
- **Maksimum deneme ile vazgeçme** — Bulut yükleme 10 kurtarma denemesinden sonra otomatik terk edilir (`MaxRecoveryAttempts=10`).
  - `RecoverPendingUploadsAsync` artık `AttemptCount >= 10` olan dosyaları siler ve `CloudUploadAbandoned` event'i fırlatır.
  - Terk edilen dosya isimleri `AbandonedFiles` listesiyle UI'ya iletilir.
- **Bulut yükleme başarısızlık e-posta bildirimi** — Tüm bulut hedeflerine yükleme başarısız olduğunda detaylı HTML e-posta gönderilir.
  - Plan adı, dosya adı, tarih, sağlayıcı detayları tablosu (deneme sayısı, hata mesajları), aksiyon önerileri.
  - Hem SQL yedekleme (ana + VSS başarısız) hem dosya yedekleme senaryolarında aktif.
- **Dosya yedek arşiv adı log'da görünür** — Bulut yükleme tamamlanma mesajında arşiv dosya adı (`archiveFileName`) eklendi.

### Düzeltme
- **Servis sonsuz yeniden deneme** — Önceden başarısız yükleme kayıtları sonsuza kadar tekrar deneniyor, artık 10 denemeden sonra terk ediliyor.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/CloudUploadOrchestrator.cs` — MaxRecoveryAttempts, abandon logic, CloudUploadAbandoned event
- `KoruMsSqlYedek.Core/Events/BackupActivityEvent.cs` — CloudUploadAbandoned enum, AbandonedFiles property
- `KoruMsSqlYedek.Core/Interfaces/INotificationService.cs` — NotifyCloudUploadFailureAsync method
- `KoruMsSqlYedek.Engine/Notification/EmailNotificationService.cs` — NotifyCloudUploadFailureAsync implementasyonu
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs` — Dosya+SQL yedek cloud failure notification, archiveFileName log
- `KoruMsSqlYedek.Win/MainWindow.cs` — CloudUploadAbandoned UI handler (OnBackupActivityChanged, BuildActivityLogLine, GetLogColor)

## [0.81.0] - 2026-06-18 — Tahmini 7z Sıkıştırılmış Boyut Gösterimi

### Yeni Özellik
- **Tahmini 7z boyut hesaplaması** — Seçili dosyaların gerçek boyutunun yanına tahmini 7z sıkıştırılmış boyutu eklendi.
  - Dosya uzantısına göre 30+ kategori: metin (~%12), veritabanı/bak (~%20), sıkıştırılmış (~%99), görsel (~%98), exe (~%45).
  - `SizeCalculationResult` record ile hem gerçek hem tahmini boyut iletilir.
  - Durum çubuğu formatı: "✅ 3 klasör, 12 dosya seçili — 1.99 GB (~890 MB 7z)".
  - Arka plan `Task.Run` ile hesaplama, `CancellationToken` debounce.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/Theme/FileSystemCheckedTreeView.cs` — SizeCalculationResult, Estimate7zRatio, GetFolderEstimated7zSize
- `KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.cs` — UpdateStatusLabel tahmini 7z gösterimi

## [0.80.0] - 2026-06-18 — TreeView Kaynak Seçimi Mimarisi

### Yeni Özellik
- **TreeView kaynak gerçeği (source of truth)** — Dizin Yolu textbox'ı kaldırıldı, TreeView seçimleri artık dosya yedekleme kaynağının tek belirleyicisi.
  - `SelectedPaths` modele eklendi — TreeView'da seçilen klasör/dosya yolları JSON'da saklanır.
  - `SourcePath` otomatik türetilir (`DeriveCommonRoot`) — VSS volume tespiti ve geriye uyumluluk için.
  - "📁 Konuma Git..." butonu ile TreeView'da navigasyon (FolderBrowserDialog → NavigateAndExpand).
  - Kaydet validasyonu: en az bir seçim zorunlu (path textbox yerine TreeView kontrol edilir).
- **Engine SelectedPaths desteği** — `CollectFiles` artık `SelectedPaths` listesini doğrudan kullanır.
  - Her seçili yol: dosya ise doğrudan eklenir, klasör ise include pattern'larla taranır.
  - Eski format uyumu: `SelectedPaths` boşsa `SourcePath` ile çalışır.
  - `CollectFilesFromDirectory` helper metodu çıkarıldı.
  - VSS volume tespiti `SelectedPaths[0]` fallback'i eklendi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/FileBackupModels.cs` — `SelectedPaths` property eklendi
- `KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.cs` — Tamamen yeniden yazıldı (TreeView = kaynak gerçeği)
- `KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.Designer.cs` — Path row kaldırıldı, navigate butonu eklendi
- `KoruMsSqlYedek.Engine/FileBackup/FileBackupService.cs` — CollectFiles SelectedPaths desteği, CollectFilesFromDirectory helper

## [0.79.0] - 2026-06-18 — Mega Login Düzeltmesi & TreeView Boyut Hesaplaması

### Düzeltme
- **Mega login timeout** — Login zaman aşımı 30s→90s'ye çıkarıldı (hashcash/PBKDF2 hesaplaması için yeterli süre).
  - `LoginAsync` çağrısı `Task.Run` ile sarımlandı — .NET 10'da kütüphanenin dahili sync-over-async çağrıları güvenle çalışır.
  - `SynchronizeApiRequests=false` ile oluşturma — semafor ile zaten kontrol edildiği için gereksiz iç kilitleme kaldırıldı.
  - Şifre çözüleme kontrolü eklendi (null/empty ise açıklayıcı hata mesajı).
  - Giriş öncesi teşhis log'u eklendi (email + şifre uzunluğu).

### Yeni Özellik
- **TreeView disk boyut hesaplaması** — Seçilen dosya ve klasörlerin toplam boyutu arka planda hesaplanır ve status bar'da gösterilir.
  - `ConcurrentDictionary` önbelleği: dosya boyutları `LoadChildren` sırasında, klasör boyutları ilk erişimde cache'lenir.
  - Arka plan `Task.Run` ile hesaplama, `CancellationToken` ile debounce.
  - `SizeCalculated` event'i ile UI güncelleme ("3 klasör, 12 dosya seçili — 1.45 GB").
  - `GetCheckedTotalSize()` API metodu.
  - Okunabilir boyut formatı: B, KB, MB, GB.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs` — Login timeout artırımı, Task.Run sarmalama, Options, teşhis log
- `KoruMsSqlYedek.Core/Constants/TimeoutConstants.cs` — `MegaLoginTimeoutSeconds = 90` eklendi
- `KoruMsSqlYedek.Win/Theme/FileSystemCheckedTreeView.cs` — Boyut cache, arka plan hesaplama, SizeCalculated event
- `KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.cs` — Boyut gösterimi, FormatFileSize, SizeCalculated handler

## [0.78.0] - 2026-06-18 — TreeView Checkbox Dosya Seçimi

### Yeni Özellik
- **FileSystemCheckedTreeView kontrolü** — Dosya yedekleme kaynak seçimi için checkbox destekli TreeView kontrolü eklendi.
  - Lazy-load dizin yapısı (BeforeExpand ile talep üzerine yükleme)
  - Tri-state checkbox desteği (ebeveyn↔çocuk propagasyonu)
  - Include/Exclude filtre kalıpları ile görsel filtreleme (hariç tutulan dosyalar soluk görünür)
  - Wildcard eşleme (*, ?) desteği
  - Sürücü/Klasör/Dosya simgeleri (Segoe MDL2 Assets)
  - `GetCheckedPaths()`, `SetCheckedPaths()`, `GetCheckedCounts()`, `NavigateAndExpand()` API'leri
- **FileBackupSourceEditDialog yeniden tasarlandı** — Yeni TreeView kontrolü ile dosya sistemi gezgini, gerçek zamanlı filtre önizlemesi ve durum çubuğu eklendi.
  - Form boyutlandırılabilir (800×700, min 680×560)
  - Include/Exclude kalıpları değiştiğinde TreeView anlık güncellenir
  - Dizin yolu Enter ile veya Browse butonu ile TreeView'da navigasyon
  - Seçili klasör/dosya sayısı durum etiketinde gösterilir
  - Seçenekler yatay düzende (tek satırda) gösterilir

### Düzeltme
- `MainWindow.BackupLog.resx` çift kaynak çıktısı hatası düzeltildi (MSB3577)

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/Theme/FileSystemCheckedTreeView.cs` — Yeni custom TreeView kontrolü
- `KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.Designer.cs` — Yeniden tasarlanmış layout
- `KoruMsSqlYedek.Win/Forms/FileBackupSourceEditDialog.cs` — TreeView entegrasyonu, yeni event handler'lar
- `MainWindow.BackupLog.resx` — Kaldırıldı (çift kaynak çakışması)

---

## [0.77.3] - 2026-04-06 — Kurtarma Şifresi (Recovery Password)

### Yeni Özellik
- **Plan bazlı kurtarma şifresi** — Plan şifresini unutan kullanıcılar, önceden tanımladıkları kurtarma şifresiyle erişim sağlayabilir.
- `BackupPlan.RecoveryPasswordHash` modele eklendi (JSON: `recoveryPasswordHash`)
- Plan düzenleme formunda checkbox aktifken ikinci bir "Kurtarma şifresi" alanı gösterilir
- Şifre doğrulama dialogu önce plan şifresini, eşleşmezse kurtarma şifresini dener
- Güvenlik sorusu ile sıfırlamada kurtarma şifresi de temizlenir

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/BackupPlan.cs` — RecoveryPasswordHash + HasRecoveryPassword
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs` — _txtRecoveryPassword kontrolü
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.cs` — Load/Save/Toggle mantığı
- `KoruMsSqlYedek.Win/Forms/PasswordDialog.cs` — Kurtarma şifresi doğrulama
- `KoruMsSqlYedek.Win/MainWindow.cs` — RecoveryHash aktarımı

---

## [0.77.2] - 2026-04-06 — Plan Şifre UX Sadeleştirme

### İyileştirme
- **Plan şifre koruması basitleştirildi** — Karmaşık durum etiketi + kaldır butonu yerine tek bir **"🔒 Bu görevi şifre ile koru"** checkbox'u eklendi. Tikle, şifre gir, kaydet — bu kadar.
- 5 kontrol (header, status, label, textbox, button) yerine 2 kontrol (checkbox + textbox)

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs` — Checkbox tabanlı layout
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.cs` — Checkbox olay mantığı

---

## [0.77.1] - 2026-04-06 — Plan Bazlı Şifre İzolasyonu

### Düzeltme
- **Plan bazlı şifre artık gerçekten izole çalışıyor** — Plan şifresi tanımlı planlarda artık yalnızca plan şifresi kabul edilir; global (master) şifre plan bazlı korumayı geçersiz kılamaz.
- **Güvenlik sorusu kurtarma akışı iyileştirildi** — Plan şifresi olan bir görevde güvenlik sorusu ile kurtarma yapıldığında, plan şifresi de otomatik sıfırlanır.
- **Şifremi Unuttum config yolu düzeltildi** — `%AppData%` → `%ProgramData%` (v0.76.0 migrasyonu ile uyumlu hale getirildi).

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/Forms/PasswordDialog.cs` — İzole doğrulama + PlanPasswordReset flag
- `KoruMsSqlYedek.Win/MainWindow.cs` — CheckPlanPassword kurtarma akışı

---

## [0.77.0] - 2026-04-06 — Post-Install Düzeltmeleri & Modern Tray İkonları

### Düzeltme
- **Error 740 (admin yetki hatası) çözüldü** — app.manifest `requireAdministrator` → `asInvoker` olarak değiştirildi. Tray uygulaması artık normal kullanıcı olarak çalışır, servis kontrolü (başlat/durdur) sc.exe + UAC ile yükseltilir.
- **Tray ikonu görünmüyor sorunu çözüldü** — `Icon.FromHandle(hIcon)` native handle sızıntısı düzeltildi. Tüm ikon üretim metotlarında clone + DestroyIcon pattern'i uygulandı.
- **Görev zamanlama (Step 3) layout sorunu çözüldü** — CronBuilderPanel boyutu (100→80px) ve y-offset çakışması düzeltildi. Form yüksekliği artırıldı (680→740px), Sizable yapıldı.

### İyileştirme
- **Modern tray ikonları** — Windows 11 flat stil: dikey gradient, ince kenar, gölgesiz sembol. Animasyon: kısa yay (120°) spinner stili.
- **ServiceController bağımlılığı kaldırıldı** — Servis durumu sc.exe query ile sorgulanır (admin gerektirmez), Start/Stop/Restart için sc.exe + `runas` verb (UAC) kullanılır.
- `System.ServiceProcess.ServiceController` NuGet paketi kaldırıldı.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/app.manifest` — asInvoker
- `KoruMsSqlYedek.Win/TrayApplicationContext.cs` — sc.exe service kontrol
- `KoruMsSqlYedek.Win/Helpers/SymbolIconHelper.cs` — icon handle fix + modern stil
- `KoruMsSqlYedek.Win/Controls/CronBuilderPanel.cs` — Height 80px
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs` — layout + form boyutu
- `KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj` — ServiceController paketi kaldırıldı
- `Deployment/InnoSetup/KoruMsSqlYedek.iss` — shellexec flag

## [0.76.0] - 2026-04-06 — Servis Veri Yolu Düzeltmesi & DPAPI Migrasyon

### Düzeltme (Kritik)
- **Servis %APPDATA% yol uyumsuzluğu çözüldü** — Windows Service (LocalSystem) ile Tray uygulaması (kullanıcı) farklı %APPDATA% kullandığı için plan dosyaları servis tarafından bulunamıyordu. Tüm paylaşılan veriler artık %ProgramData%\KoruMsSqlYedek altında saklanır.
- **DPAPI CurrentUser → LocalMachine scope geçişi** — Şifreler artık LocalMachine scope ile korunur, böylece hem kullanıcı hem LocalSystem servisi şifreleri çözebilir. Eski CurrentUser şifreleri otomatik dönüştürülür.

### Yeni
- **DataMigrationHelper:** Eski %APPDATA% konumundaki verileri otomatik olarak %ProgramData%'ya taşır ve DPAPI şifrelerini LocalMachine scope'a dönüştürür
- **Installer dizin izinleri:** ProgramData dizinlerine Users grubuna Modify izni otomatik verilir
- PasswordProtector artık çözme sırasında önce LocalMachine, başarısız olursa CurrentUser scope dener (geriye uyumluluk)

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Helpers/PathHelper.cs` — CommonApplicationData (%ProgramData%)
- `KoruMsSqlYedek.Core/Helpers/PasswordProtector.cs` — LocalMachine scope + fallback
- `KoruMsSqlYedek.Core/Helpers/DataMigrationHelper.cs` — YENİ: migrasyon yardımcısı
- `KoruMsSqlYedek.Win/Program.cs` — Migrasyon entegrasyonu
- `Deployment/InnoSetup/KoruMsSqlYedek.iss` — [Dirs] section + versiyon

## [0.75.1] - 2026-04-06 — Anti-Regresyon & Installer Düzeltmesi

### Düzeltme
- **3 pre-existing test failure düzeltildi** — 1174/1174 test artık geçiyor (0 failure)
  - GoogleDrive: Gömülü credential fallback nedeniyle MissingClientId/MissingClientSecret testleri güncellendi
  - FileBackup: SourceDirNotExists test beklentisi düzeltildi (Failed status doğru davranış)
- **Installer takılma sorunu çözüldü** — `sc.exe` ile servis yönetimi; exe'nin CLI komut desteklememesi nedeniyle sonsuz bekleme engellendi
- **Build-Release.ps1 RID uyumsuzluğu düzeltildi** — `--no-build` publish'ten kaldırıldı, RID-specific self-restore aktif

### Yeni
- **TimeoutConstants.cs:** Tüm timeout değerleri merkezi sabit dosyasında tanımlı (FTP, SFTP, Mega, genel)
- FtpSftpProvider ve MegaProvider timeout'ları TimeoutConstants'tan okunuyor
- Installer güncelleme öncesi servisi otomatik durduruyor (dosya kilidi önleme)
- Installer servis açıklaması ve auto-start yapılandırması eklendi

## [0.75.0] - 2026-04-06 — Dialog Düzeni + Zamanlama UX + Dosya Retention Düzeltmesi

### Yeni
- **FileBackupSourceEditDialog:** Daha geniş pencere (510px), GroupBox ile bölümlendirilmiş alan, tüm kontrollere ToolTip eklendi
- **PlanEditForm Step 3:** "Görev" terminolojisi uygulandı (Tam Yedek Görevi, Fark Yedek Görevi vb.), iyileştirilmiş tooltip'ler (öneriler + kurtarma bilgileri), SQL/Dosya bölümleri arasına ayırıcı çizgi eklendi

### Düzeltme
- **RetentionCleanupService:** `Files_*.7z` dosya yedekleme arşivleri artık retention politikasına göre temizleniyor (KeepLastN, DeleteOlderThanDays, GFS dahil)
  - Kök neden: Eski kod sadece `{databaseName}_*.*` pattern'i ile arama yapıyordu, `Files_*.7z` hiçbir zaman eşleşmiyordu

### Test
- Dosya arşiv retention için 6 yeni test: KeepLastN, DeleteOlderThanDays, FileBackupDisabled, FileBackupNull, MixedDbAndFile, GfsPolicy

## [0.74.1] - 2026-04-06 — Kapsamlı Çapraz Özellik Kombinasyon Testleri

### Yeni
- **CrossFeatureCombinationTests.cs:** 644 yeni test — tüm BackupPlan özelliklerinin çapraz kombinasyonlarını doğrular.
  - MegaMatrix: Strategy(3) × Retention(4) × Cloud(2) × Password(2) × FileBackup(2) × ArchivePw(2) × Verify(2) = 384 JSON round-trip testi
  - SqlAuth × Compression: AuthMode(2) × Strategy(3) × Algorithm(4) × Level(5) = 120 kombinasyon
  - CloudTarget: 6 provider tipi için tüm provider-specific alanlar (FtpsSkipCertValidation, SftpFingerprint, BandwidthLimit, PermanentDeleteFromTrash)
  - Notification: SmtpProfile vs Legacy × OnSuccess × OnFailure × Email × Toast = 32 kombinasyon
  - Reporting: Frequency(3) × Enabled(2) × EmailTo(2) = 12 kombinasyon
  - FileBackup: Recursive × VSS × Enabled = 8 kombinasyon + çoklu kaynak + edge case
  - GFS Retention: 5 parametre kombinasyonu
  - Triple Password System: PlanPw × ArchivePw × SqlPw = 8 kombinasyon
  - Edge Cases: Maximum config (tüm özellikler açık) + Minimum config (tüm özellikler kapalı)
  - Backward Compatibility: v0.72 öncesi JSON format doğrulaması

### Düzeltme
- **FTP Null Config Test Discovery:** MSTest DataRow enum serileştirme sorunu düzeltildi — `CloudProviderType` enum yerine `int` değerler kullanılarak 3 test [None] durumundan çıkarıldı.

### Test İstatistikleri
- Toplam test: 521 → 1168 (+647 yeni test)
- Başarılı: 1165/1168 (3 pre-existing failure — GoogleDrive, FileBackup)
- Yeni regresyon: **0**

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Tests/CrossFeatureCombinationTests.cs` (yeni)
- `KoruMsSqlYedek.Tests/PlanPasswordIntegrationTests.cs` (FTP DataRow fix)

## [0.74.0] - 2026-04-05 — Plan Bazlı Şifre Koruması

### Yeni Özellik
- **Plan Şifresi:** Her plan için ayrı şifre belirlenebilir. Plan düzenleme ve silme işlemlerinde hem master şifre hem plan şifresi kabul edilir.
- **İki Katmanlı Doğrulama:** Master şifre tüm planlara evrensel erişim sağlarken, plan şifresi sadece o plana özeldir.
- **Sihirbaz Entegrasyonu:** Plan düzenleme sihirbazının Sıkıştırma adımına (Step 4) plan şifre yönetim bölümü eklendi.

### Teknik
- `BackupPlan.cs`: `PasswordHash` ve `HasPlanPassword` property eklendi.
- `PasswordDialog.cs`: Opsiyonel `planPasswordHash` parametresi — master VEYA plan şifresi kabul ediyor.
- `MainWindow.cs`: `CheckPlanPassword(BackupPlan plan = null)` — düzenleme/silme plan şifresini kontrol ediyor, yeni plan oluşturma kontrolsüz.
- `PlanEditForm.cs` / `PlanEditForm.Designer.cs`: Plan şifre durum göstergesi, yeni şifre alanı ve şifre kaldırma butonu eklendi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/BackupPlan.cs`
- `KoruMsSqlYedek.Win/Forms/PasswordDialog.cs`
- `KoruMsSqlYedek.Win/MainWindow.cs`
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.cs`
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs`

## [0.73.0] - 2026-04-05 — Yerel/Bulut Mod Ayrımı Kaldırıldı

### İyileştirme
- **Yedekleme Modu Kaldırıldı:** Plan oluşturma sihirbazından "Yerel" / "Bulut" mod seçimi kaldırıldı. Artık tüm planlar her zaman 6 adımı (Bağlantı → Kaynaklar → Zamanlama → Sıkıştırma → Hedefler → Bildirim) gösteriyor.
- **Otomatik Algılama:** Bulut hedef varlığı `BackupPlan.HasCloudTargets` computed property ile otomatik belirleniyor.
- **Geriye Dönük Uyumluluk:** `BackupMode` enum ve `Mode` property JSON uyumluluğu için `[Obsolete]` olarak korunuyor.

### Teknik
- `Enums.cs`: `BackupMode` enum `[Obsolete]` işaretlendi.
- `BackupPlan.cs`: `Mode` property `[Obsolete]`, `HasCloudTargets` computed property eklendi.
- `RetentionCleanupService.cs`: `plan.Mode == BackupMode.Cloud` → `plan.HasCloudTargets` olarak değiştirildi.
- `PlanEditForm.cs`: Radio button mantığı, `OnBackupModeChanged` handler kaldırıldı, `RebuildActiveSteps` her zaman 6 adım döndürüyor.
- `PlanEditForm.Designer.cs`: `_lblBackupMode`, `_rbModeLocal`, `_rbModeCloud` kontrolleri kaldırıldı.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/Enums.cs`
- `KoruMsSqlYedek.Core/Models/BackupPlan.cs`
- `KoruMsSqlYedek.Engine/Retention/RetentionCleanupService.cs`
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.cs`
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs`

## [0.72.0] - 2026-04-05 — Mega Oturum Önbellekleme Yeniden Eklendi

### Düzeltme
- **Mega Session Caching Kaybı Giderildi:** v0.71.x çöp kutusu özelliği eklenirken v0.70.0'da eklenen oturum önbellekleme kaybolmuştu. Her upload/delete/trash işleminde ayrı login/logout yapılıyordu → Mega rate limiting tetikleniyordu.
- **Oturum Yeniden Kullanımı:** Aynı email ile 15 dakika içindeki tüm Mega API çağrıları mevcut oturumu yeniden kullanıyor.
- **SemaphoreSlim Serializasyon:** Tüm Mega API çağrıları (Upload, Delete, EmptyTrash) sıralı işleniyor — eşzamanlı erişim ve rate limiting önleniyor.
- **Hata Sonrası Oturum Geçersizleştirme:** Timeout veya API hatasında oturum önbelleği temizleniyor, sonraki çağrı yeni oturum açıyor.

### Teknik
- `MegaProvider.cs`: `_cachedClient`, `_cachedEmail`, `_sessionLastUsedUtc`, `_sessionSemaphore` statik alanları eklendi.
- `GetOrCreateSessionAsync()`: 15dk önbellek + email değişikliği kontrolü + otomatik geçersizleştirme.
- `InvalidateSessionInternalAsync()`: Güvenli logout + önbellek temizleme.
- `UploadAsync`, `DeleteAsync`, `EmptyTrashAsync`: Semaphore + session caching entegrasyonu.
- `TestConnectionAsync`: Taze bağlantı (kimlik doğrulama testi için önbellek kullanmıyor).

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs`

---

## [0.71.1] - 2026-04-05 — Çöp Kutusu Güvenlik Düzeltmesi: Sadece Bizim Dosyalarımız

### Düzeltme
- **Google Drive:** `Files.EmptyTrash()` (tüm çöpü boşaltan) API yerine, sadece bizim klasörümüzde (`RemoteFolderPath`) bulunan çöp dosyaları tek tek kalıcı siliniyor. Kullanıcının kişisel çöp dosyalarına dokunulmuyor.
- **Mega:** Tüm trash children yerine, yalnızca yedek dosya adı desenine uyan (`*_Full_*.bak/7z`, `*_Differential_*`, `*_Incremental_*`, `Files_*.7z`) dosyalar siliniyor.
- **ICloudProvider.EmptyTrashAsync:** Dokümantasyon güncellendi — "tüm dosyaları" değil, "sadece bizim dosyalarımızı" sildiği belirtildi.

### Teknik
- `GoogleDriveProvider.cs`: Klasör bazlı sorgu (`trashed=true and '{folderId}' in parents`) + pagination + tek tek `Files.Delete(fileId)`. Yeni `FindFolderIdAsync` helper (klasörü bul ama oluşturma).
- `MegaProvider.cs`: `IsOurBackupFile(fileName)` helper — `.bak`/`.7z` uzantısı + yedek isim deseni kontrolü.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Interfaces/ICloudProvider.cs` (dokümantasyon)
- `KoruMsSqlYedek.Engine/Cloud/GoogleDriveProvider.cs` (EmptyTrashAsync + FindFolderIdAsync)
- `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs` (EmptyTrashAsync + IsOurBackupFile)

---

## [0.71.0] - 2026-04-05 — Bulut Çöp Kutusu Otomatik Temizleme (Mega + Google Drive)

### Yeni Özellik
- **Çöp Kutusu Otomatik Temizleme:** `PermanentDeleteFromTrash=false` (çöp kutusuna taşı) ayarındaki Mega ve Google Drive hedeflerinde, yedekleme tamamlandıktan sonra birikmiş çöp öğeleri otomatik olarak kalıcı siliniyor.
- **Mega:** Çöp düğümü altındaki tüm öğeler tek tek kalıcı siliniyor (`NodeType.Trash` → children → `DeleteAsync(node, false)`).
- **Google Drive:** Yerel `Files.EmptyTrash()` API ile tek çağrıda tüm çöp kutusu temizleniyor.
- **FTP/SFTP/UNC:** Çöp kutusu konsepti yok — `SupportsTrash=false`, no-op stub.
- **Orkestrasyon:** `CloudUploadOrchestrator.EmptyTrashForAllAsync` — yalnızca `PermanentDeleteFromTrash=false` VE `SupportsTrash=true` hedefleri filtreler.
- **Pipeline Entegrasyonu:** Her yedekleme görevi tamamlandığında (SQL ve/veya Dosya), `EmptyTrashIfNeededAsync` çağrılıyor. Hata durumunda yedekleme başarısını etkilemez.

### Teknik
- `ICloudProvider`: `bool SupportsTrash` property + `Task<int> EmptyTrashAsync(config, ct)` metodu eklendi.
- `ICloudUploadOrchestrator`: `Task<int> EmptyTrashForAllAsync(targets, ct)` metodu eklendi.
- `MegaProvider.cs`: `SupportsTrash => true`, `EmptyTrashAsync` implementasyonu.
- `GoogleDriveProvider.cs`: `SupportsTrash => true`, `EmptyTrashAsync` implementasyonu.
- `FtpSftpProvider.cs`, `LocalNetworkProvider.cs`: `SupportsTrash => false`, no-op.
- `CloudUploadOrchestrator.cs`: `EmptyTrashForAllAsync` implementasyonu.
- `BackupJobExecutor.cs`: `EmptyTrashIfNeededAsync` helper metodu + her iki yedekleme akışına hook.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Interfaces/ICloudProvider.cs`
- `KoruMsSqlYedek.Core/Interfaces/ICloudUploadOrchestrator.cs`
- `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/GoogleDriveProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/FtpSftpProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/LocalNetworkProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/CloudUploadOrchestrator.cs`
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs`

---

## [0.70.0] - 2026-04-05 — Mega Oturum Önbellekleme + Diagnostik İyileştirmeler

### İyileştirme
- **Mega Oturum Önbellekleme:** Her dosya yüklemesinde yeni login/logout yerine, mevcut oturum 15 dakikaya kadar yeniden kullanılıyor. VSS ile dosya sayısı ikiye katlandığında bile Mega rate limiting tetiklenmiyor.
- **SemaphoreSlim Serializasyon:** Eşzamanlı upload istekleri sıralı işleniyor, oturum yarış koşulları önleniyor.
- **Diagnostik Log Seviyesi:** MegaProvider'ın tüm kritik logları (login denemesi, bağlantı kontrolü, oturum yeniden kullanımı/süresi dolması) `Log.Information` seviyesine yükseltildi — Service loglarında artık görünür.
- **DPAPI Null Kontrolü:** Şifre çözme başarısız olursa açık hata mesajı fırlatılıyor.
- **Rate Limiting İpucu:** Timeout hatalarında Mega hesap bazlı rate limiting olasılığı kullanıcıya bildiriliyor.
- **MainWindow Switch:** `CloudUploadStarted` ve `CloudUploadCompleted` event'leri artık açık case olarak işleniyor — "Unhandled BackupActivityType" uyarı logları kaldırıldı.

### Teknik
- `MegaProvider.cs`: Tamamen yeniden yazıldı — statik `_cachedClient`, `_cachedEmail`, `_sessionLastUsedUtc`, `GetOrCreateSessionAsync()`, `InvalidateSession()`. Upload/Delete sonrası logout yok.
- `MainWindow.cs`: `OnBackupActivityChanged` switch'ine `CloudUploadStarted` ve `CloudUploadCompleted` case'leri eklendi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs` (oturum önbellekleme, diagnostik loglar)
- `KoruMsSqlYedek.Win/MainWindow.cs` (switch case düzeltmesi)

---

## [0.69.0] - 2026-04-04 — Mega Upload Retry Düzeltmesi + Hata Mesajı Görünürlüğü

### Düzeltme
- **Mega Retry Gecikmesi:** `CloudUploadOrchestrator.UploadWithRetryAsync` içinde, provider exception fırlatmadan `IsSuccess=false` döndüğünde (örn. Mega login timeout) retry döngüsü gecikme olmadan devam ediyordu. Artık non-exception başarısızlıklarda da exponential backoff (2s/4s/8s) uygulanıyor.
- **Hata Mesajı Görünürlüğü:** Bulut yükleme başarısız olduğunda log panelinde sadece "Başarısız ✕" yerine "Başarısız ✕ — {hata detayı}" gösteriliyor. Kullanıcı başarısızlığın nedenini (timeout, bağlantı hatası vb.) doğrudan görebilir.
- **Hızlı Retry Engelleme:** VSS eklenmesiyle dosya sayısı ikiye katlandığında, gecikmesiz retry'lar Mega rate limiting'i tetikliyordu. Exponential backoff bu sorunu çözüyor.

### Teknik
- `UploadWithRetryAsync`: `if (result.IsSuccess)` bloğundan sonra `else` bloğu eklendi — hata mesajı loglanıp retry delay uygulanıyor, son başarısız sonuç korunuyor.
- `BuildActivityLogLine` (`CloudUploadCompleted`): Başarısız durumda `e.Message` hata detayı gösteriliyor.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/CloudUploadOrchestrator.cs` (retry delay + hata korunması)
- `KoruMsSqlYedek.Win/MainWindow.cs` (hata mesajı görünürlüğü)

---

## [0.68.5] - 2026-04-04 — Log Çelişkileri Düzeltildi + VSS Etiket Güncellemesi

### Düzeltme
- **"Express VSS" → "VSS":** Tüm log mesajlarında "Express VSS" etiketi "VSS" olarak güncellendi. VSS artık tüm SQL Server sürümlerinde çalıştığı için "Express" ifadesi kaldırıldı.
- **Yanlış Başarı Durumu:** Bulut yükleme tamamen başarısız olduğunda bile "Yedekleme tamamlandı. ✓" gösteriliyordu. Artık bulut başarısızlığında "⚠ Yedekleme tamamlandı (bulut yükleme başarısız)" mesajı gösteriliyor.
- **Grid Durum İkonu:** Bulut başarısızlığında grid’de ✓ yerine ⚠ uyarı ikonu gösteriliyor.
- **Log Renkleri:** Bulut başarısız tamamlanma durumunda yeşil yerine uyarı rengi kullanılıyor.

### Teknik
- `ExecuteSqlBackupAsync` ve `ExecuteFileBackupAsync` artık `Task<bool>` dönüyor (bulut yükleme başarı durumu).
- `BackupActivityType.Completed` event’inde `IsSuccess` ve `Message` alanları bulut sonucuna göre dolduruluyor.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Backup/SqlBackupService.cs` (~9 log mesajı güncellendi)
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs` (Task<bool> dönüş + bulut izleme)
- `KoruMsSqlYedek.Win/MainWindow.cs` (UI: log mesajı, renk, grid ikonu)

---

## [0.68.4] - 2026-04-04 — Mega Bağlantı Ön Kontrolü + Login Diagnostik

### İyileştirme
- **Mega Bağlantı Ön Kontrolü:** Login denemesinden önce Mega API sunucusuna hızlı HTTP isteği (10 saniye) gönderiliyor. Sunucu erişilemezse 30 saniye login timeout beklemek yerine anında hata verilir.
- **Detaylı Diagnostik Loglama:** Email, şifre uzunluğu, HTTP status kodu loglanıyor. Bağlantı sorunlarının kök nedeni hızlıca teşhis edilebilir.
- **İyileştirilmiş Hata Mesajları:** DNS/firewall/internet hataları için ayrı ayrı açıklayıcı Türkçe mesajlar. Endpoint bilgisi dahil.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs`

---

## [0.68.3] - 2026-04-04 — VSS Tüm Edition’larda Aktif + Bulut Upload Bağımsızlığı

### İyileştirme
- **VSS Dosya Kopyası Tüm Edition’larda:** Express kısıtı kaldırıldı. MDF/LDF VSS kopyası artık tüm SQL Server sürümlerinde (Express, Developer, Standard, Enterprise) ek güvenlik olarak alınıyor. Başarısız olursa sessizce atlanır.
- **Bağımsız Bulut Yükleme:** Ana dosya (.bak/.7z) ve VSS dosyası ayrı try bloklarda yükleniyor. Biri başarısız olsa bile diğeri denenir — en az bir yedeğin buluta ulaşması garanti edilir.
- **Hata Loglama:** Her iki upload da başarısızsa Error seviyesinde log yazılır.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Backup/SqlBackupService.cs` (Express kısıtı kaldırıldı)
- `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs` (bulut upload bağımsızlığı)

---

## [0.68.2] - 2026-04-04 — Şifre Koruması Null Fix

### Düzeltme
- **PasswordSetupDialog ArgumentNullException:** `_settings` null olduğunda şifre ayarları dialogu açılırken `ArgumentNullException` hatası oluşuyordu. `_settings ??= _settingsManager.Load()` ile lazy-load eklendi.
- **OnPasswordToggleClick:** Aynı null koruma tutarlılık için eklendi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/MainWindow.cs`

---

## [0.68.1] - 2026-04-04 — Mega Upload Timeout Koruması + Pipe Güvenlik İyileştirmesi

### Düzeltme
- **Mega Upload Timeout Koruması:** LoginAsync CancellationToken desteklemediği için Task.WhenAny ile 30 saniyelik timeout eklendi
- **Upload İptal Garantisi:** UploadAsync iptal sinyaline yanıt vermediğinde Task.WhenAny wrapper ile anında iptal
- **Logout Güvenliği:** LogoutAsync 10 saniyelik timeout ile fire-and-forget, cleanup'ı asla bloklamaz
- **TimeoutException Yakalama:** Kullanıcıya açıklayıcı Türkçe hata mesajı
- **Adım Adım Debug Loglama:** Login, klasör kontrol, upload başlangıcı aşamalarında detaylı log

### İyileştirme
- **Pipe Güvenliği:** ServicePipeServer'a BuiltinAdministratorsSid ve mevcut kullanıcı SID'i eklendi
- **Pipe Hata Yönetimi:** UnauthorizedAccessException (10s) ve IOException (3s) için özel retry mantığı

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs` (tam yeniden yazım)
- `KoruMsSqlYedek.Service/IPC/ServicePipeServer.cs`

---

## [0.68.0] - 2026-04-04 — Mega.io Bulut Desteği + OneDrive/Workspace/LocalPath Kaldırma

### Yeni Özellik
- **Mega.io Bulut Desteği:** Email/şifre ile kimlik doğrulama, dosya yükleme (ilerleme bilgisi), silme (çöp kutusu/kalıcı), klasör yönetimi, kota bilgisi
- **MegaApiClient v1.10.5:** NuGet paketi eklendi (CG.Web.MegaApiClient)
- **CloudTargetEditDialog:** Mega.io combobox'a eklendi, FTP grubu Mega için yeniden kullanılır (host/port gizli, "Email" etiketi)

### Kaldırılan
- **OneDrive Desteği:** Tüm OneDrive provider kodu, NuGet paketleri (Microsoft.Graph, Azure.Identity, MSAL) ve testler kaldırıldı
- **GoogleDriveWorkspace:** Enum değeri ve ilgili branching kaldırıldı, Google Drive artık tek tip
- **CloudProviderType.LocalPath:** Bulut hedeflerinden kaldırıldı (yerel yedekleme klasörü BackupPlanConfig.LocalPath etkilenmedi)

### İyileştirme
- GoogleDriveProvider: DisplayName sadeleştirildi → "Google Drive"
- LocalNetworkProvider: Sadece UNC paylaşımı, DisplayName → "Ağ Paylaşımı (UNC)"
- PlanEditForm: Bulut modu radio/hint metinleri güncellendi (Mega.io eklendi)
- Tüm test dosyaları güncellendi (yeni Mega testleri, LocalPath→UncPath geçişleri)

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs` (yeni)
- `KoruMsSqlYedek.Core/Models/Enums.cs`
- `KoruMsSqlYedek.Engine/Cloud/CloudProviderFactory.cs`
- `KoruMsSqlYedek.Engine/Cloud/GoogleDriveProvider.cs`
- `KoruMsSqlYedek.Engine/Cloud/LocalNetworkProvider.cs`
- `KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs`
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.Designer.cs`
- `KoruMsSqlYedek.Core/Interfaces/ICloudProvider.cs`
- `KoruMsSqlYedek.Core/Models/ConfigModels.cs`
- `KoruMsSqlYedek.Engine/KoruMsSqlYedek.Engine.csproj`
- Silinen: `OneDriveProvider.cs`, `OneDriveAuthHelper.cs`, `OneDriveProviderTests.cs`
- Test: `CloudProviderFactoryTests.cs`, `GoogleDriveProviderTests.cs`, `CloudUploadOrchestratorTests.cs`, `LocalNetworkProviderTests.cs`, `TestDataFactory.cs`

---

## [0.67.0] - 2026-04-03 — Google OAuth Özel Credential Yönetimi

### Yeni Özellik
- **Google OAuth Ayarları Dialogu:** Kullanıcılar Google Cloud Console'dan aldıkları kendi Client ID/Secret değerlerini girebilir.
- **⚙ Diyalog erişimi:** Bulut hedef düzenleme ekranında OAuth grubuna ⚙ (dişli) butonu eklendi.
- **DPAPI şifreleme:** Client Secret değeri DPAPI ile şifrelenerek AppSettings'e kaydedilir.
- **Statik credential override:** `GoogleDriveAuthHelper.SetCustomCredentials` ile uygulama genelinde özel credential kullanımı.
- **Otomatik yükleme:** Uygulama başlangıcında AppSettings'teki özel credential otomatik yüklenir.

### İyileştirme
- `AppSettings`: `GoogleOAuthClientId`, `GoogleOAuthClientSecret`, `HasCustomGoogleOAuth` eklendi.
- `GoogleDriveAuthHelper`: `ResolveCredentials()` ile özel > gömülü credential önceliklendirme.
- Eski per-target `OAuthClientId`/`OAuthClientSecret` alanları kayıt sırasında temizlenir.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/AppSettings.cs`
- `KoruMsSqlYedek.Engine/Cloud/GoogleDriveAuthHelper.cs`
- `KoruMsSqlYedek.Win/Forms/GoogleOAuthSettingsDialog.cs` (yeni)
- `KoruMsSqlYedek.Win/Forms/GoogleOAuthSettingsDialog.Designer.cs` (yeni)
- `KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs`
- `KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.Designer.cs`
- `KoruMsSqlYedek.Win/Forms/PlanEditForm.cs`
- `KoruMsSqlYedek.Win/Program.cs`

---

## [0.66.1] - 2026-04-03 — Google OAuth "invalid_client" Düzeltmesi

### Düzeltme
- **Google OAuth Hatası Çözüldü:** Eski plan yapılandırmalarında saklanan özel (custom) OAuthClientId/OAuthClientSecret değerleri, gömülü credential'lar yerine kullanılıyordu ve Google'dan "invalid_client" hatası alınıyordu.
- `CloudTargetEditDialog`: Kimlik doğrulama artık her zaman gömülü (embedded) credential kullanır.
- `GoogleDriveAuthHelper`: Token yenileme işlemi artık her zaman gömülü credential kullanır.
- Kayıt sırasında eski özel OAuth değerleri otomatik temizlenir (backward compat cleanup).

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs` — OnGoogleAuthClick sadeleştirildi, SaveUiToTarget eski değerleri temizler
- `KoruMsSqlYedek.Engine/Cloud/GoogleDriveAuthHelper.cs` — GetCredentialAsync sadeleştirildi

---

## [0.66.0] - 2026-04-03 — Şifre Koruması Aktif/Pasif Toggle

### Yeni Özellik
- **Şifre Aktif/Pasif Toggle:** Şifre koruması tanımlandıktan sonra kaldırmaya gerek kalmadan aktif/pasif yapılabilir.
- **ToolStripSplitButton:** Şifre butonu dropdown menü ile genişletildi (Aktif/Pasif Yap + Şifre Ayarları).
- **3 Durumlu İkon:** Kalkan ikonu — yeşil (aktif), turuncu/çizgili (pasif), gri (tanımsız).

### İyileştirme
- `AppSettings`: `PasswordEnabled` (JSON persisted) ve `HasPassword` (computed) property eklendi.
- `IsPasswordProtected` artık `HasPassword && PasswordEnabled` kontrolü yapar.
- `PasswordSetupDialog`: Şifre kaldırma artık `PasswordEnabled` değerini de sıfırlar.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Core/Models/AppSettings.cs` — PasswordEnabled, HasPassword
- `KoruMsSqlYedek.Win/MainWindow.cs` — OnPasswordToggleClick, UpdatePasswordButtonIcon (3 durum)
- `KoruMsSqlYedek.Win/MainWindow.Designer.cs` — ToolStripSplitButton, dropdown items
- `KoruMsSqlYedek.Win/Forms/PasswordSetupDialog.cs` — PasswordEnabled set/reset
- `KoruMsSqlYedek.Win/Theme/PhosphorIcons.cs` — ShieldSlash ikonu

---

## [0.65.0] - 2026-04-03 — Log Performansı, Dark Dialog, Şifre Koruması, Tray Sadeleştirme

### Yeni Özellik
- **Şifre Koruması:** Görev ekleme/düzenleme/silme işlemleri için şifre koruması. SHA256+DPAPI hash, güvenlik sorusu ile kurtarma.
- **ModernMessageBox:** Standart MessageBox yerine dark temalı özel dialog. Windows 10’da da tutarlı dark görünüm.
- **Şifre Dialogları:** PasswordDialog (doğrulama + güvenlik sorusu kurtarma), PasswordSetupDialog (oluşturma/değiştirme/kaldırma).

### İyileştirme
- **Log VirtualMode:** DataGridView VirtualMode ile büyük log dosyaları artık UI’yı dondurmaz. CellValueNeeded/CellFormatting pattern, AutoSizeRowsMode=DisplayedCells.
- **Tray Menü Sadeleştirme:** Planlar, Manuel Yedekleme, Log Görüntüle kaldırıldı (Dashboard’dan erişilebilir).
- **Plan toolbar:** Şifre koruması butonu eklendi (kalkan ikonu, durum rengi).

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/Theme/ModernMessageBox.cs` — yeni: dark temalı MessageBox
- `KoruMsSqlYedek.Win/Forms/PasswordDialog.cs/.Designer.cs` — yeni: şifre doğrulama
- `KoruMsSqlYedek.Win/Forms/PasswordSetupDialog.cs/.Designer.cs` — yeni: şifre kurulum
- `KoruMsSqlYedek.Core/Helpers/PlanPasswordHelper.cs` — yeni: SHA256+DPAPI hash/verify
- `KoruMsSqlYedek.Core/Models/AppSettings.cs` — PasswordHash, SecurityQuestion, SecurityAnswerHash
- `KoruMsSqlYedek.Win/MainWindow.cs` — VirtualMode, CheckPlanPassword, OnPasswordSetupClick
- `KoruMsSqlYedek.Win/MainWindow.Designer.cs` — VirtualMode, şifre butonu
- `KoruMsSqlYedek.Win/TrayApplicationContext.cs` — menü sadeleştirme
- Tüm form dosyaları — MessageBox → ModernMessageBox

---

## [0.64.1] - 2026-04-02 — Provider Listesi Sadeleştirme

### İyileştirme
- **Provider listesi sadeleştirildi:** Google Drive (Bireysel/Workspace) → tek "Google Drive" satırı, OneDrive (Bireysel/Kurumsal) → tek "OneDrive" satırı.
- **✓ işareti:** Test edilmiş/çalışan provider'ların yanına ✓ eklendi (Google Drive ✓).
- **Mapping sistemi:** Combo box index → CloudProviderType enum eşlemesi (ProviderMap array + GetSelectedProviderType/GetComboIndexForType helper'ları). Mevcut config'lerdeki Workspace/Business türleri otomatik birleşik satıra yönlendirilir.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs` — ProviderMap, PopulateProviderTypes, mapping helpers
- `KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs` — versiyon
- `KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj` — versiyon

---

## [0.64.0] - 2025-07-25 — Google Drive OAuth Sadeleştirme (Gömülü Credential)

### Yeni Özellik
- **Gömülü OAuth Credential:** Google Drive OAuth2 Client ID/Secret uygulamaya Base64-obfuscated olarak gömüldü. Kullanıcıların Google Cloud Console'dan credential almasına gerek kalmadı.
- **GoogleOAuthCredentials.cs:** Statik sınıf — Base64 encode/decode, `IsConfigured` property, try/catch ile hata koruması.
- **Parametresiz AuthorizeInteractiveAsync:** Gömülü credential ile tek tıkla Google hesabı bağlama.
- **Credential öncelik sırası:** Config'deki özel credential > gömülü credential (backward compat).
- **UI sadeleştirme:** Client ID/Secret alanları kaldırıldı, OAuth grubu "Hesap Bağlama" olarak yeniden adlandırıldı, tek buton + durum etiketi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Engine/Cloud/GoogleOAuthCredentials.cs` — yeni: gömülü credential sınıfı
- `KoruMsSqlYedek.Engine/Cloud/GoogleDriveAuthHelper.cs` — parametresiz overload + fallback mantığı
- `KoruMsSqlYedek.Engine/Cloud/GoogleDriveProvider.cs` — ValidateConfig gömülü/config dual destek
- `KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.cs` — UI mantığı sadeleştirme
- `KoruMsSqlYedek.Win/Forms/CloudTargetEditDialog.Designer.cs` — ClientId/Secret kontrolleri kaldırıldı
- `KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs` — versiyon
- `KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj` — versiyon

---

## [0.63.0] - 2025-07-24 — O7: Inno Setup Installer + GitHub Actions CI/CD + Otomatik Güncelleme

### Yeni Özellik (O7 — Otomatik Güncelleme Mekanizması)
- **Inno Setup 6 installer:** Program Files kurulumu, Windows Service (sc.exe) kaydı, masaüstü kısayolu, başlangıçta çalıştır seçeneği, AppData korunur (kaldırma sırasında). (`installer/setup.iss`)
- **PowerShell build script:** AssemblyInfo.cs'den otomatik versiyon algılama, dotnet publish (Win + Service), ISCC.exe ile installer derleme. (`installer/build.ps1`)
- **GitHub Actions CI/CD:** `v*` tag push tetiklemesi, .NET 10 SDK, build/test/publish, Inno Setup via Chocolatey, GitHub Release oluşturma (installer asset). (`.github/workflows/release.yml`)
- **IUpdateService:** GitHub Releases API üzerinden güncelleme kontrolü, `UpdateInfo` modeli (Version, Title, ReleaseNotes, DownloadUrl, FileSizeBytes, PublishedAt, HtmlUrl). (`IUpdateService.cs`)
- **UpdateChecker:** `/releases/latest` endpoint, `System.Version` karşılaştırma, installer asset algılama (`KoruMsSqlYedek_Setup_` prefix), akışlı indirme + ilerleme raporlama. (`UpdateChecker.cs`)
- **Tray güncelleme entegrasyonu:** Günlük otomatik kontrol (60s gecikme → 24 saat aralık), balon bildirim, manuel kontrol menü öğesi, temp klasöre indirme → `runas` ile installer başlatma. (`TrayApplicationContext.cs`)
- **13 kaynak anahtarı:** Güncelleme UI metinleri (menü, kontrol, indirme, hata mesajları). (`Resources.resx`)
- **Autofac kaydı:** `UpdateChecker` → `IUpdateService` (SingleInstance). (`WinModule.cs`)

### Etkilenen Dosyalar
- `installer/setup.iss` — yeni: Inno Setup 6 installer script
- `installer/build.ps1` — yeni: PowerShell build + publish + installer derleme
- `.github/workflows/release.yml` — yeni: GitHub Actions release workflow
- `KoruMsSqlYedek.Core/Interfaces/IUpdateService.cs` — yeni: güncelleme servisi arayüzü + UpdateInfo modeli
- `KoruMsSqlYedek.Engine/Update/UpdateChecker.cs` — yeni: GitHub API implementasyonu
- `KoruMsSqlYedek.Win/TrayApplicationContext.cs` — güncelleme timer, menü, indirme/başlatma
- `KoruMsSqlYedek.Win/IoC/WinModule.cs` — UpdateChecker DI kaydı
- `KoruMsSqlYedek.Win/Properties/Resources.resx` — 13 Update_* kaynak anahtarı
- `KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs` — versiyon
- `KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj` — versiyon

---

## [0.62.0] - 2025-07-23 — TB1/TB4: Switch Refactor + RestoreDialog & Exhaustiveness Testleri

### İyileştirme (TB1 — OnBackupActivityChanged Switch Refactor)
- **BuildActivityLogLine:** switch statement → switch expression + `throw ArgumentOutOfRangeException` (fail-fast). Karmaşık CloudUploadProgress case'i `BuildCloudUploadLogLine` helper'a çıkarıldı. (`MainWindow.cs`)
- **GetLogColor:** default case `ModernTheme.LogDefault` → `throw ArgumentOutOfRangeException` (sessiz hata yerine fail-fast). (`MainWindow.cs`)
- **UpdatePlanRowStatus:** İnline switch → `GetStatusDisplay` switch expression helper'a çıkarıldı, `(string Icon, Color Color)` tuple döner. (`MainWindow.cs`)
- **OnBackupActivityChanged:** `default:` case eklendi — `Log.Warning("Unhandled BackupActivityType...")` ile bilinmeyen türleri loglar. (`MainWindow.cs`)
- **XML doc comments:** Tüm 5 sorumluluk noktasına ⚠️ uyarılı dokümantasyon eklendi.

### Yeni Test Kapsamı (TB4 — RestoreDialog + Exhaustiveness Testleri)
- **RestoreDialogTests:** 15 birim testi — 4 constructor null guard, 5 CleanupTempDirectory (method existence + null/empty/nonexistent/existing dir), 3 LoadHistory filtreleme mantığı (success-only, all-failed, empty), 3 grid row data doğrulama (boyut formatı, compressed path önceliği, fallback). (`RestoreDialogTests.cs`)
- **BackupActivityExhaustivenessTests:** 20 parameterized test — enum değer sayısı kontrolü (9), KnownActivityTypes coverage, DynamicData ile her enum değeri için BuildActivityLogLine ve GetLogColor kapsam doğrulaması. (`BackupActivityExhaustivenessTests.cs`)
- **InternalsVisibleTo:** Win projesinden Tests projesine internal erişim (AssemblyInfo.cs attribute). (`AssemblyInfo.cs`, `Tests.csproj`)

### Altyapı
- **InternalsVisibleTo düzeltme:** `GenerateAssemblyInfo=false` olan projede MSBuild `<InternalsVisibleTo>` item yerine AssemblyInfo.cs'e manuel attribute eklendi.

### Etkilenen Dosyalar
- `KoruMsSqlYedek.Win/MainWindow.cs` — TB1 switch refactor (4 değişiklik)
- `KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs` — InternalsVisibleTo + versiyon
- `KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj` — versiyon
- `KoruMsSqlYedek.Tests/RestoreDialogTests.cs` — yeni test dosyası
- `KoruMsSqlYedek.Tests/BackupActivityExhaustivenessTests.cs` — yeni test dosyası
- `KoruMsSqlYedek.Tests/KoruMsSqlYedek.Tests.csproj` — Win project reference

### Test İstatistikleri
- Yeni: 35 test (15 RestoreDialog + 20 BackupActivityExhaustiveness)
- Toplam: 447 test | Geçen: 446 | Başarısız: 1 (ilgisiz, önceden var olan FileBackupServiceTests hatası)

---

## [0.61.0] - 2025-07-22 — O5/O6: Stres Testleri + PlanProgressTracker Testleri

### Yeni Test Kapsamı
- **Stres testleri (O5):** 8 yeni stres testi — eşzamanlı farklı planlar, aynı plan SemaphoreSlim kilit testi, büyük DB listesi (20 DB), karma başarı/hata/iptal senaryoları, ardışık hızlı çalıştırma (deadlock kontrolü), paralel bulut upload, monoton ilerleme event doğrulaması, çoklu DB iptal propagasyonu. (`StressTests.cs`)
- **PlanProgressTracker ağırlık modeli testleri (O6):** 22 yeni birim testi — sınır değerler (negatif totalCount, 100 DB, MaxPercent=100, büyük index), VSS ağırlık dağılımı (20/50/30) hassas doğrulama, NoVSS (30/70), dosya-only tam pipeline, SQL+dosya+bulut karma pipeline, çoklu bulut hedef yüzde dağılımı (2 ve 3 hedef), rastgele bulut yüzde monoton garanti, LocalStepProgress ascending doğrulama, sabit kontrolü. (`PlanProgressTrackerTests.cs`)
- **Düzeltme:** PlanProgressTracker.cs'de eksik `using System;` ve `using System.Collections.Generic;` eklendi (pre-existing build issue).

### Test İstatistikleri
- Yeni: 30 test (8 Stres + 22 PlanProgressTracker)
- Toplam: 412 test | Geçen: 411 | Başarısız: 1 (ilgisiz, önceden var olan FileBackupServiceTests hatası)

---

## [0.60.0] - 2025-07-21 — O2/O3/O4: Raporlama İstatistik + GFS Retention + MainWindow Ayrıştırma

### Yeni Özellik
- **Raporlama istatistikleri (O2):** `ReportingService.BuildReportHtml` artık `EmailTemplateBuilder` kullanıyor. Eklenen istatistikler: ortalama süre, en büyük yedek, sıkıştırma oranı, veritabanı bazlı özet tablosu. (`ReportingService.cs`)
- **GFS Retention politikası (O3):** Grandfather-Father-Son saklama desteği — günlük/haftalık/aylık/yıllık periyot bazlı en iyi yedek koruması. `RetentionPolicyType.GFS` enum, `RetentionPolicy` modeline GFS alanları, `BuildGfsProtectedSet` algoritması. (`Enums.cs`, `ConfigModels.cs`, `RetentionCleanupService.cs`)
- **MainWindow log ayrıştırma (O4):** `AppendBackupLog`, `ReplaceLastProgressLine`, `AppendColoredLine`, `ProgressLineMarker`, `_planLogs` buffer'ı `MainWindow.BackupLog.cs` partial class'ına taşındı. (`MainWindow.BackupLog.cs`, `MainWindow.cs`)

### Test İstatistikleri
- Yeni: 9 GFS Retention testi (GfsRetentionTests)
- Toplam: 349 test | Geçen: 348 | Başarısız: 1 (ilgisiz, önceden var olan FileBackupServiceTests hatası)

---

## [0.59.0] - 2025-07-20 — O1: Profesyonel E-posta Bildirim Şablonları

### Yeni Özellik
- **EmailTemplateBuilder:** Tüm e-posta bildirimleri için ortak HTML şablon sınıfı — koyu branded header, durum rozeti, özet/detay tabloları, hata bloğu, footer. (`EmailTemplateBuilder.cs`)
- **SQL yedek bildirimi yenilendi:** Profesyonel şablon ile tutarlı marka görünümü, yeni alanlar: CompressionVerified (arşiv doğrulama), VssFileCopySizeBytes. (`EmailNotificationService.cs`)
- **Dosya yedek bildirimi yenilendi:** Aynı şablon altyapısı, kaynak detay tablosu. (`EmailNotificationService.cs`)
- **NotifyFileBackupAsync SMTP profil desteği:** Eski per-plan SMTP alanları yerine merkezi SmtpProfile çözümleme, çoklu alıcı desteği. (`EmailNotificationService.cs`)
- **Bulut yükleme detay tablosu:** Bildirim e-postalarında hedef/durum/detay sütunlu tablo.

### Test İstatistikleri
- Yeni: 27 EmailTemplateBuilderTests
- Toplam: 340 test | Geçen: 339 | Başarısız: 1 (ilgisiz, önceden var olan FileBackupServiceTests hatası)

---

## [0.58.0] - 2025-07-20 — Y1/Y2: Local-mode SQL İlerleme + VSS Test Kapsamı

### Yeni Özellik
- **Local-mode SQL ilerleme çubuğu (Y1):** Bulut hedefi olmayan planlarda ilerleme çubuğu artık her SQL adımında güncelleniyor. Ağırlıklar: SQL=%50, Doğrulama=%65, Sıkıştırma=%80, Arşiv Doğrulama=%88, Temizlik=%95. (`MainWindow.cs`, `BackupActivityEvent.cs`, `BackupJobExecutor.cs`)
- **HasCloudTargets flag:** `BackupActivityEventArgs` ve `PlanProgressTracker`'a eklendi. Started event'inde plan bulut hedefi durumunu bildiriyor.
- **VSS test kapsamı (Y2):** 19 birim testi — Dispose güvenliği, CreateSnapshot argüman doğrulama, GetSnapshotFilePath path mapping (farklı volume'lar, iç içe yollar), DeleteSnapshot/DeleteAllSnapshots, IsAvailable, IVssService kontrat. (`VssSnapshotServiceTests.cs`)

### Test İstatistikleri
- Toplam: 313 test | Geçen: 312 | Başarısız: 1 (ilgisiz, önceden var olan FileBackupServiceTests hatası)

---

## [0.57.0] - 2025-07-20 — K1/K2/K3: IPC Testleri, İptal/Temizlik Testleri, RestoreDialog Tamamlama

### Yeni Özellik
- **Named Pipe IPC testleri (K1):** 18 birim testi — tüm PipeProtocol mesaj türleri (FromArgs/ToArgs roundtrip, kenar durumları, büyük payload, özel karakter). (`PipeProtocolTests.cs`)
- **BackupCancellationRegistry testleri (K1):** 20 birim testi — Register/Cancel/Unregister/IsRunning/IsAnyRunning, thread safety, büyük/küçük harf duyarsız PlanId. (`BackupCancellationRegistryTests.cs`)
- **Cancel/Cleanup testleri (K2):** 8 yeni birim testi — SQL/sıkıştırma/bulut yükleme iptal propagasyonu, CancellationRegistry yaşam döngüsü, hata ve başarılı akış ActivityType doğrulaması. (`BackupJobExecutorTests.cs`)
- **RestoreDialog .7z desteği (K3):** `.7z` arşivi algılama → `ExtractAsync` ile geçici klasöre açma → `.bak` bulma → geri yükleme → geçici klasör temizliği. (`RestoreDialog.cs`)
- **RestoreDialog lokalizasyon (K3):** 10 yeni kaynak anahtarı (Restore_*), tüm sabit Türkçe stringler `Res.Get()`/`Res.Format()` ile değiştirildi. (`Resources.resx`)
- **RestoreDialog iptal UX (K3):** `_isBusy` flag ile işlem sırasında iptal butonu aktif, iptal onay diyaloğu, buton metni dinamik (İptal/Kapat).

### Düzeltme
- **BackupJobExecutorTests SetJobData:** 2 parametreli `SetJobData` overload'una eksik `manualTrigger` anahtarı eklendi (27 test düzeltmesi).
- **UploadToAllAsync mock düzeltmesi:** Tüm mock Setup/Verify çağrıları 6 parametreden 7 parametreye güncellendi (planId optional param).

### Altyapı
- **DI kaydı:** `SevenZipCompressionService` → `ICompressionService` olarak `WinModule.cs`'e eklendi.
- **MainWindow:** `ICompressionService` constructor parametresi ve `RestoreDialog` çağrı güncellemesi.

### Test İstatistikleri
- Toplam: 294 test | Geçen: 293 | Başarısız: 1 (ilgisiz, önceden var olan FileBackupServiceTests hatası)

---

## [0.56.0] - 2025-07-19 — Proje Yönetişim & Branch Stratejisi

### Yeni Özellik
- **3 katmanlı branch stratejisi**: `master` (release) → `develop` (günlük geliştirme) → `feature/*/fix/*/hotfix/*` dallanma modeli oluşturuldu.
- **Modül stabilite haritası** (`docs/STATUS.md`): 5 proje, 38 modülün 🟢/🟡/🔴 derecelendirmesi.
- **Yol haritası** (`docs/ROADMAP.md`): Kısa/orta/uzun vade iş kalemleri + teknik borç takibi.
- **Mimari kararlar günlüğü** (`docs/DECISIONS.md`): 10 ADR kaydı (Clean Architecture, IPC, Quartz, VSS, Cancel Cleanup vb.).
- **Copilot direktifi güncelleme**: Git workflow bölümleri yeni branch stratejisine uyarlandı.

---

## [0.55.0] - 2026-05-12 — İptal/Hata Durumunda Ara Dosya Temizliği

### Yeni Özellik
- **İptal/hata temizliği**: Yedekleme görevi iptal edildiğinde veya başarısız olduğunda tamamlanmamış ara dosyalar (`.bak`, `.7z`, `Files/` staging klasörü) otomatik olarak siliniyor. (etkilenen: `BackupJobExecutor.cs`)
- **Per-DB snapshot takibi**: Her veritabanı için başarıyla tamamlanan dosyalar temizlik listesinden çıkarılıyor; yalnızca yarım kalan işlemin dosyaları temizleniyor.
- **OperationCanceledException yayılımı**: Sıkıştırma, bulut yükleme ve retention adımlarındaki iç `catch` blokları artık iptal istisnalarını yutmak yerine yeniden fırlatıyor.

---

## [0.54.0] - 2026-05-12 — Named Pipe Güvenlik ACL Düzeltmesi

### Düzeltme
- **Servis–Tray pipe bağlantı hatası giderildi**: `ServicePipeServer` artık `PipeSecurity` ile pipe oluşturuyor. SYSTEM hesabıyla çalışan servise normal kullanıcı olarak çalışan Tray uygulaması bağlanabiliyor. (`AuthenticatedUserSid` → ReadWrite, `LocalSystemSid` → FullControl) (etkilenen: `ServicePipeServer.cs`)
- `NamedPipeServerStreamAcl.Create()` kullanılarak pipe ACL'si açıkça tanımlandı.

---

## [0.53.1] - 2026-05-12 — Uyarı Temizliği & Paket Güncellemesi

### Düzeltme
- **NU1608 çözümü**: SMO 171.30.0 → 181.15.0, SqlClient 6.0.2 → 6.1.4, MSAL 4.67.2 → 4.83.3 — bağımlılık kısıtlaması uyarıları giderildi.
- **CS8632 çözümü**: `#nullable enable` bağlamı olmayan 8 event handler’dan `object?` → `object` dönüştürüldü. (etkilenen: `MainWindow.cs`, `RestoreDialog.cs`, `PlanEditForm.cs`)
- **CS0414 çözümü**: `SmtpProfileEditDialog._isNew` kullanılmayan alan kaldırıldı.

---

## [0.53.0] - 2026-05-12 — Dosya Yedekleme İlerleme Çubuğu Entegrasyonu

### Yeni Özellik
- **Dosya yedekleme progress bar desteği**: Dosya yedekleme fazı artık ilerleme çubuğuna dahil ediliyor. SQL+Dosya planlarında SQL %80, dosya yedekleme %20 ağırlıklı; sadece dosya yedekleme planlarında dosya %100 ağırlıklı. (etkilenen: `MainWindow.cs`, `BackupJobExecutor.cs`, `BackupActivityEvent.cs`)
- **Dinamik ağırlık modeli**: Dosya yedekleme içinde kopyalama+sıkıştırma %25, bulut yükleme %75 oranında dağılıyor.
- **HasFileBackup event bilgisi**: `BackupActivityEventArgs`'a `HasFileBackup` property eklendi; Started event artık dosya yedekleme koşulunu önceden bildiriyor.
- **StepChanged faz algılama**: "Dosya Yedekleme" ve "Dosya Sıkıştırma" adımları ilerleme çubuğu geçişlerini tetikliyor.

---

## [0.52.0] - 2026-05-12 — Ara Dosya Otomatik Temizliği

### Yeni Özellik
- **Sıkıştırma sonrası .bak temizliği**: SQL yedekleme sonrası sıkıştırma başarılıysa (ve arşiv doğrulama geçtiyse) ara `.bak` dosyası otomatik olarak siliniyor. Disk alanı tasarrufu sağlanıyor. (etkilenen: `BackupJobExecutor.cs`)
- **Files klasörü temizliği**: Dosya yedekleme arşivi (`.7z`) oluşturulduktan sonra geçici `Files` staging klasörü otomatik olarak siliniyor. (etkilenen: `BackupJobExecutor.cs`)
- Her iki temizlik adımı log konsoluna `StepChanged "Temizlik"` eventi olarak yansıyor.

---

## [0.51.1] - 2026-05-12 — VSS Bulut Yükleme İlerleme Çubuğu Senkronizasyonu

### Hata Düzeltmesi
- **İlerleme çubuğu VSS yüklemesi ile senkron değildi**: Ana bulut yüklemesi tamamlandığında progress bar %100'e ulaşıyordu ancak VSS dosyası hâlâ yükleniyordu. Yeni ağırlık modeli: VSS dosyası varsa SQL %20 + Ana bulut %50 + VSS bulut %30; yoksa SQL %30 + Bulut %70 (değişmez). `PlanProgressTracker`'a `HasVssUpload` ve `IsVssPhase` alanları eklendi. `StepChanged` eventleri ile "Express VSS" ve "VSS Bulut Yükleme" fazları algılanarak dinamik ağırlık geçişi yapılıyor. (etkilenen: `MainWindow.cs`)

---

## [0.51.0] - 2026-05-12 — Tray Sidebar Program Adı + Servis Debug Modu + Log Renk Şeması Ayarları

### Yeni Özellik
- **Tray sidebar'da program adı**: Tray menü kenar çubuğu artık versiyon yanında "Koru MsSql Yedek" uygulama adını da gösteriyor. (etkilenen: `VersionSidebarRenderer.cs`, `TrayApplicationContext.cs`)
- **Servis debug modu algılama**: Windows Service yüklü olmadığında ancak pipe bağlantısı varsa menüde "Servis: Bağlı (Debug) ✓" gösteriyor. VS2026 multi-project debugging desteği iyileştirildi. (etkilenen: `TrayApplicationContext.cs`)
- **Log renk şeması ayarları**: Ayarlar panelinden log konsolu renk şeması seçilebiliyor. 12 yerleşik şablon (Koru, Solarized Dark/Light, Monokai, Dracula, Nord, Gruvbox, One Dark, Tokyo Night, Catppuccin, Ubuntu, Matrix Green). Seçilen şema JSON ayarlarda saklanıyor ve uygulama başlangıcında otomatik uygulanıyor. (etkilenen: `MainWindow.cs`, `Program.cs`, `Resources.resx`, `Resources.tr-TR.resx`)

---

## [0.50.0] - 2026-05-12 — Kümülatif İlerleme Çubuğu

### Yeni Özellik
- **Kümülatif progress bar**: Yedekleme ilerleme çubuğu artık her adımda sıfırlanmak yerine toplam işlem yüzdesini gösteriyor. Her veritabanı toplam ilerlemenin eşit bir dilimini alır (100/N%), veritabanı yedekleme %30'unu, bulut yükleme %70'ini kapsar. Çoklu bulut hedefleri de ağırlıklı olarak dahil edilir. `Math.Max` ile monoton artış garantisi sağlanır — ilerleme asla geriye gitmez. (etkilenen: `MainWindow.cs`)
- **Tamamlanma göstergesi**: Başarılı yedekleme sonunda ilerleme çubuğu %100'ü kısa süre gösterdikten sonra sıfırlanır. (etkilenen: `MainWindow.cs`)
- **PlanProgressTracker**: Yeni iç sınıf ile plan başına veritabanı sırası, toplam ve maksimum ilerleme izlenir. (etkilenen: `MainWindow.cs`)

---

## [0.49.1] - 2026-05-11 — RichTextBox İlerleme Satırı Silme Hatası Düzeltmesi

### Hata Düzeltmesi
- **İlerleme satırı tüm metni siliyordu**: `RichTextBox.Text` (`\r\n`) ile `Select()` (dahili `\n`) arasındaki indeks uyumsuzluğu satır sayısı arttıkça yanlış bölge seçimine yol açıyordu. `Lines[]` + `GetFirstCharIndexFromLine()` ile doğru pozisyon hesabına geçildi. (etkilenen: `MainWindow.cs`)
- **Regresyon direktifi eklendi**: RichTextBox Text vs Select indeks uyumsuzluğu kuralı `copilot-instructions.md`'ye eklendi. (etkilenen: `.github/copilot-instructions.md`)

---

## [0.49.0] - 2026-05-11 — Renkli Log Konsolu ("Koru" Temalı)

### Yeni Özellik
- **Renkli log konsolu**: Yedekleme log paneli artık RichTextBox tabanlı, her olay türüne özel renk ile gösteriliyor. "Koru" (Koruma) temasına uygun güven veren renk paleti: yeşil=başarılı/başlangıç, kırmızı=hata, sarı=iptal/uyarı, mavi=bilgi/veritabanı, turkuaz=ilerleme, mor=bulut işlemi. (etkilenen: `MainWindow.cs`, `MainWindow.Designer.cs`, `ModernTheme.cs`)
- **Tema renk genişletmesi**: ModernTheme'e 10 yeni log konsol rengi eklendi (LogSuccess, LogError, LogWarning, LogInfo, LogProgress, LogCloud, LogStarted, LogTimestamp, LogDefault, LogConsoleBg) — hem Dark hem Light tema için ayrı paletler. (etkilenen: `ModernTheme.cs`)
- **Renkli buffer desteği**: Plan bazlı log buffer'ı artık `(Text, Color)` tuple'ları saklıyor, plan geçişlerinde renkli rebuild yapılıyor. (etkilenen: `MainWindow.cs`)

---

## [0.48.0] - 2026-05-11 — Plan Bazlı Log İzolasyonu + Tek Satır İlerleme + Sonraki Çalışma Zamanı Düzeltmesi

### Hata Düzeltmesi
- **Log paneli plan karışması**: Bulut upload olayları (CloudUploadStarted/Progress/Completed) artık `PlanId` içeriyor. Farklı bir plan seçiliyken çalışan planın logları artık karışmıyor. `AppendBackupLog` null-planId catch-all kaldırıldı, `_viewingPlanId` fallback kullanılıyor. (etkilenen: `CloudUploadOrchestrator.cs`, `ICloudUploadOrchestrator.cs`, `BackupJobExecutor.cs`, `MainWindow.cs`)
- **Sonraki çalışma zamanları boş gelme**: Plans sekmesine her geçişte servisden güncel zamanlama bilgisi isteniyor (`RequestStatusAsync`). `_nextFireTimes` sözlüğü hiçbir zaman temizlenmiyor, yalnızca güncelleniyor. (etkilenen: `MainWindow.cs`)

### Yeni Özellik
- **Tek satır ilerleme güncellemesi**: Bulut yükleme ilerleme satırları artık log panelinde tek satırda güncelleniyor, aşağı doğru kayma sorunu giderildi. Buffer ve UI aynı anda güncelleniyor. (etkilenen: `MainWindow.cs`)

---

## [0.47.0] - 2026-05-10 — VSS Bulut Yükleme + Modern Tray İkon + Servis Kontrol İyileştirmesi

### Hata Düzeltmesi
- **VSS dosyası buluta yüklenmiyor**: Express VSS ile oluşturulan `*_VSS_*.7z` dosyaları artık SQL yedek dosyasından sonra otomatik olarak bulut hedeflerine yükleniyor. (etkilenen: `BackupJobExecutor.cs`)

### Yeni Özellik
- **Modern circular badge tray ikonu**: Tray ikonları artık gradient arka planlı, yuvarlak badge stili ile render ediliyor. Her durum için farklı renk gradient'i (yeşil idle, mavi running, kırmızı error vb.) ve beyaz sembol. (etkilenen: `SymbolIconHelper.cs`)
- **Spinning arc animasyon**: Yedekleme sırasında tray ikonu artık mavi daire üzerinde dönen beyaz ark (progress spinner) gösteriyor — 12 kare, daha akıcı animasyon. (etkilenen: `SymbolIconHelper.cs`)
- **Servis kontrol iyileştirmesi**: Win32Exception (yetki hatası) ayrı yakalanıyor ve "Yönetici olarak çalıştırın" mesajı gösteriliyor. Gerçek SCM durumu artık "Bilinmiyor" yerine doğrudan gösteriliyor. (etkilenen: `TrayApplicationContext.cs`)

---

## [0.46.0] - 2026-05-09 — Eşzamanlı Yedekleme Desteği

### Yeni Özellik
- **Farklı planlar paralel çalışabilir**: Bir yedekleme planı çalışırken artık diğer planların "Yedekle" butonu/sağ-tık menüsü pasif olmuyor. Her plan bağımsız olarak başlatılabilir ve iptal edilebilir. (etkilenen: `MainWindow.cs`, `BackupJobExecutor.cs`)
- **Plan bazlı kilit mekanizması**: Service tarafında global `SemaphoreSlim(1,1)` yerine plan başına `ConcurrentDictionary<string, SemaphoreSlim>` kullanılıyor — aynı plan iki kez çalışamaz, farklı planlar paralel çalışabilir. (etkilenen: `BackupJobExecutor.cs`)
- **Per-plan UI durum takibi**: `_isBackupRunning` (bool) → `_runningPlanIds` (HashSet), `_activePlanId` → `_viewingPlanId` — progress bar ve log paneli seçili plana göre güncelleniyor. (etkilenen: `MainWindow.cs`)

### Hata Düzeltmesi
- **Tray animasyon renk taşması**: `CreateAnimationFrames` içinde `brightness - 120` negatif olabiliyordu → `Math.Clamp(0, 255)` ile düzeltildi. (etkilenen: `SymbolIconHelper.cs`)

---

## [0.45.0] - 2026-05-09 — Tray İkon & Menü İyileştirmeleri

### Yeni Özellik
- **DPI uyumlu tray ikonu**: Tüm tray ikonları artık `SystemInformation.SmallIconSize` ile doğru boyutta render ediliyor; yüksek DPI ekranlarda net ve parlak görünüyor. (etkilenen: `SymbolIconHelper.cs`)
- **Daha parlak ikon renkleri**: Tüm durum ikonları (idle, running, success, warning, error, disconnected) daha yüksek parlaklıkla yeniden tasarlandı; koyu/açık taskbar'larda görünürlük artırıldı.
- **Gölge efekti**: Her ikon ve animasyon karesine yarı-şeffaf koyu gölge eklendi — hem açık hem koyu taskbar'larda yüksek kontrast.
- **Pulse animasyon**: Yedekleme animasyonu artık hem dönüyor hem parlaklık pulse ediyor (sin dalga) — çok daha belirgin.
- **Uygulama adı menü başlığı**: Tray sağ-tık menüsünün en üstüne "Koru MsSql Yedek" başlığı eklendi (yeşil accent rengi). (etkilenen: `TrayApplicationContext.cs`, `VersionSidebarRenderer.cs`)

### Hata Düzeltmesi
- **Servis kontrol hataları**: `UpdateServiceMenuItems` artık sessiz catch yapmıyor. Servis yüklü değilse "Servis: Yüklü Değil ⚠" gösteriyor; diğer hatalarda butonlar etkin bırakılıp log yazılıyor. (etkilenen: `TrayApplicationContext.cs`, `Resources.resx`, `Resources.tr-TR.resx`)

---

## [0.44.1] - 2026-05-09 — Upload Progress %100→%99 Glitch Düzeltmesi

### Hata Düzeltmesi
- **Bulut yükleme ilerlemesi %100→%99 sapması**: `Progress<T>` callback'leri ThreadPool üzerinden sırasız çalışabiliyordu. `CloudUploadOrchestrator`'a `lastReportedPct` koruması eklendi — geriye giden yüzde değerleri artık atlanıyor. (etkilenen: `CloudUploadOrchestrator.cs`)
- **GoogleDriveProvider `Math.Min(percent, 99)` kaldırıldı**: İlerleme doğal değerini raporluyor; CloudUploadOrchestrator'daki koruma duplikasyonu önlüyor. (etkilenen: `GoogleDriveProvider.cs`)
- **%100 ilerleme log satırı bastırıldı**: `CloudUploadCompleted` zaten "Başarılı ✓" yazdığı için %100 satırı gereksizdi. Boş satır koruması da `AppendBackupLog`'a eklendi. (etkilenen: `MainWindow.cs`)

---

## [0.44.0] - 2026-05-09 — Dashboard Log Zenginleştirme

### Yeni Özellik
- **Detaylı yedekleme adım bilgisi UI'da**: Servis logundaki zengin adım detayları (SQL yedek boyutu, Express VSS bilgisi, sıkıştırma oranı, arşiv doğrulama, bulut yükleme sonucu, retention temizliği) artık Win uygulamasının yedek log panelinde de görünüyor. (etkilenen: `BackupJobExecutor.cs`, `MainWindow.cs`)
- **Dosya yedekleme adım bilgileri**: Dosya yedekleme başlangıcı, kaynak bazlı kopya sonuçları, arşiv boyutu ve bulut yükleme detayları UI'a yansıtılıyor.
- Her pipeline adımında `BackupActivityHub.Raise(StepChanged)` çağrısı eklendi; `BuildActivityLogLine` formatı `Message` alanını doğrudan gösteriyor.

---

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
