# Özellikler & Geliştirme Yol Haritası

Bu dosya, KoruMsSqlYedek projesinin mevcut ve planlanan özelliklerini fazlar halinde listeler.

### v0.99.37 — Dosya Filtre Kalıpları Boyut Hesaplama
- Dahil/Hariç kalıp değişikliğinde dosya boyutu otomatik yeniden hesaplanıyor
- Boyut hesaplaması include/exclude filtrelerini uygulayarak sadece geçen dosyaları sayıyor
- Dosya kaynağı düzenleme formunda ilk açılışta mevcut seçimler için boyut hesaplama

### v0.99.5 — Per-Type Retention Şablonları
- `RetentionScheme`: SQL Full / Diff / Log ve Files arşivi için bağımsız retention politikaları
- Hazır şablonlar: Minimal, Standard (★ öneri), Extended, GFS — `RetentionTemplates.FromType()` factory
- `BackupPlan.GetEffectiveRetention(BackupFileType)` — per-type; eski planlarda `Retention` fallback
- `RetentionCleanupService` refactor: `_Full_`, `_Differential_`, `_Log_` dosya adı parse ile tip tespiti
- PlanEditForm Step 4: şablon dropdown + info etiketi + Özel mod için mevcut kontroller korundu

### v0.99.4 — Güncelleme Kontrol Düzeltmesi
- `InstallerPrefix` `"KoruMsSqlYedek_v"` olarak düzeltildi; artık GitHub asset eşleşiyor ve güncelleme bildirimi çalışıyor

### v0.99.3
- Bağlantı ve servis başlatma bildirimleri `ModernToast.Success` (yeşil ✓) ile gösteriliyor
- Servis durdurma bildirimi nötr `ModernToast.Info` olarak güncellendi

### v0.99.2 — GitHub Actions Kesin Düzeltme
- choco install innosetup kaldırıldı (windows-latest’ta pre-installed)
- ISCC.exe tam mutlak yol kullanımı
- Exit code kontrolü eklendi
- v0.94.0’dan beri release oluşmama sorunu çözüldü

### v0.99.1 — CI/CD Release Workflow Düzeltmeleri
- Inno Setup mutlak yol (github.workspace) düzeltmesi
- releases/ klasörü sıralama düzeltmesi
- Çok satırlı GITHUB_OUTPUT heredoc syntax
- Compress-Archive dizi syntax düzeltmesi

### v0.99.0 — Telif Hakkı & Açık Kaynak Atıfları
- Telif hakkı: © 2026 Zafer Bilgisayar, Geliştirici: Hüzeyin Küçük
- Hakkında formunda 19 açık kaynak kütüphanenin adı ve lisans bilgisi
- Tüm .csproj dosyalarında Copyright/Authors/Company metadata

### v0.98.0 — Hakkında Formu
- Hakkında diyalogu: uygulama adı, versiyon, GDI+ kalkan logo, telif hakkı, GitHub linki, .NET runtime bilgisi
- Tray menü + status bar versiyon etiketi ile erişim

### v0.97.0 — Yeşil Grup Başlıkları (ObjectListView)
- ThemedObjectListView alt sınıfı: NM_CUSTOMDRAW ile grup başlık rengini özelleştirme
- Dashboard grup başlıkları emerald yeşil (AccentPrimaryHover)
- UseExplorerTheme=false ile explorer tema baskılama

### v0.96.0 — Dashboard ObjectListView Geçişi
- Dashboard Son Yedeklemeler gridi `ObjectListView.Repack.NET6Plus` v2.9.5 ile değiştirildi
- Yerleşik sıralama, plan bazlı gruplama, AspectName veri bağlama, FormatRow satır renklendirme
- Dark theme tam uyum (header, alternatif satır, seçim renkleri)

### v0.92.0 — Konsolide Bulut Yükleme: Tek Seferde Toplu Upload
- [x] SQL + dosya yedeklerinin bulut yüklemeleri tek toplu fazda birleştirildi
- [x] `UploadBatchToAllAsync` — CloudUploadOrchestrator'a toplu yükleme metodu
- [x] `UploadAllPendingAsync` — BackupJobExecutor'a konsolide yükleme yardımcısı
- [x] PlanProgressTracker konsolide ağırlık modeli (SQL lokal → Dosya lokal → Toplu bulut)
- [x] Progress bar %100'e ulaşma sorunu düzeltildi
- [x] CloudFileName / CloudFileIndex / CloudFileTotal — UI'da yüklenen dosya bilgisi
- [x] 52 birim testi geçti (konsolide model testleri dahil)

### v0.91.0 — Büyük Dosya Refactoring: Partial Class Ayrımı
- [x] 12 büyük dosya partial class'lara ayrıldı (toplam 30+ yeni dosya)
- [x] MainWindow.cs → 6 partial (Dashboard, Plans, BackupExecution, BackupActivity, LogViewer, Settings)
- [x] BackupJobExecutor.cs → 3 partial (SqlPipeline, FilePipeline, Helpers)
- [x] EmailNotificationService.cs → 4 partial (SqlNotification, FileNotification, CloudNotification, JobNotification)
- [x] PlanEditForm.cs → 4 partial (WizardNavigation, PlanBinding, CloudAndFileSources, Visibility)
- [x] TrayApplicationContext.cs → 3 partial (ServiceControl, BackupActivity, UpdateCheck)
- [x] SqlBackupService.cs → 3 partial (Operations, VssBackup, Helpers)
- [x] FileSystemCheckedTreeView.cs → 3 partial (NodeLoading, Filtering, SizeCalculation)
- [x] CloudUploadOrchestrator.cs → 2 partial (CloudOperations, RetryAndRecovery)
- [x] GoogleDriveProvider.cs → 1 partial (Operations)
- [x] FtpSftpProvider.cs → 2 partial (Ftp, Sftp)
- [x] FileBackupService.cs → 2 partial (CopyAndVerify, FileCollection)
- [x] Tüm public API'ler ve davranışlar korundu, breaking change yok

### v0.90.3 — IsSuccess Hesaplama Düzeltmesi
- `IsSuccess` artık SQL/dosya yedekleme başarısızlıklarını da dahil ediyor
- `overallSuccess = allCloudOk && !anySqlFailed && !anyFileFailed` formülü
- Dosya-yalnızca planlarda da `anyFileSourceFailed` kontrolü eklendi

### v0.90.2 — ListView Grup Expand/Collapse P/Invoke Düzeltmesi
- `ListViewHeaderPainter.EnableGroupView()` — doğrudan `LVM_ENABLEGROUPVIEW(TRUE)` P/Invoke
- Force-toggle + P/Invoke belt-and-suspenders yaklaşımı ile grup görünümü garanti

### v0.90.1 — ListView Grup Görünümü Kök Neden Düzeltmesi
- ShowGroups zamanlama hatası düzeltildi — Groups.Count > 0 olduktan sonra set ediliyor
- Collapsible grup başlıkları artık doğru çalışıyor

### v0.90.0 — ListView Grup Başlıkları + SMTP Profil Ekleme
- ListView DarkMode_Explorer teması — native grup başlıkları dark modda görünür, collapsible
- LVS_EX_DOUBLEBUFFER — ListView flicker önleme
- PlanEditForm SMTP profil linki — SmtpProfileEditDialog açar, profil ekleyince ComboBox güncellenir

### v0.89.0 — UI Düzeltmeleri: Ayarlar ComboBox, Navigasyon İkonları
- Ayarlar > Genel: Dil, Tema, Log Konsol Teması ComboBox'ları görünür hale getirildi (Dock=Fill)
- Yedek Türü ComboBox'u genişletildi (Dock=Fill)
- PlanEditForm İleri/Geri butonlarına DevExpress PNG ikonları eklendi (Import/Export)

### v0.88.0 — Konsolide Bildirim: Tek E-posta

- [x] **Konsolide e-posta bildirimi**: Görev tamamlandığında SQL + dosya + bulut sonuçları tek e-postada
- [x] **Görev logu e-postada**: BackupActivityHub eventleri toplanıp e-postaya ekleniyor
- [x] **JobNotificationData modeli**: Tüm sonuçları tek nesnede birleştiren veri sınıfı
- [x] **NotifyJobCompletedAsync**: INotificationService'e yeni konsolide bildirim metodu
- [x] Per-DB ve per-component bildirimler kaldırıldı

### Değiştirilen Dosyalar
- [x] `KoruMsSqlYedek.Core/Models/JobNotificationData.cs` — Yeni
- [x] `KoruMsSqlYedek.Core/Interfaces/INotificationService.cs` — NotifyJobCompletedAsync
- [x] `KoruMsSqlYedek.Engine/Notification/EmailNotificationService.cs` — Uygulama
- [x] `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs` — Konsolide akış

### v0.87.0 — DevExpress PNG İkonları Tüm Formlara + Animasyonlu Tray İkonları
- [x] Tüm formlardaki PhosphorIcons → DevExpress PNG ikonlarına geçiş (23 ikon)
- [x] MainWindow, PlanEditForm, CloudTargetEditDialog, FileBackupSourceEditDialog güncel
- [x] Animasyonlu GIF tray ikonları (CloudSync yedeklerken, CheckMark tamamlanınca)
- [x] GIF frame extraction (SymbolIconHelper.ExtractGifFrames)
- [x] ListView grup collapse/expand (+/−) düzeltmesi (ListViewHeaderPainter NativeWindow)

### v0.86.0 — Toolbar: DevExpress PNG İkonları
- [x] DevExpress Images kütüphanesinden renkli 16x16 PNG ikonlar (7 adet)
- [x] Toolbar’dan şifre butonları kaldırıldı
- [x] EmbeddedResource tabanlı ikon yükleme (LoadToolStripIcon)

### v0.85.0 — Dashboard: Plan Bazlı Gruplandırma
- [x] Son Yedeklemeler listesi plan adına göre gruplandırıldı (ListViewGroup)
- [x] Açılır/kapanır gruplar (+/−) — CollapsedState.Expanded
- [x] Grup başlığında plan adı ve yedekleme sayısı
- [x] BeginUpdate/EndUpdate performans optimizasyonu

### v0.84.0 — E-posta Şablonları: Bulut Yükleme Detayları & Hata Kodları
- [x] SQL yedek bildirimi: Bulut yükleme detayında uzak dosya yolu, boyut, detaylı hata + deneme sayısı
- [x] Dosya yedek bildirimi: Arşiv bilgisi, kaynak başına süre/boyut, başarısız dosya listesi, bulut yükleme sonuçları
- [x] Bulut başarısızlık bildirimi: Provider türü, başarısız hedef sayısı, genişletilmiş hata mesajı
- [x] Periyodik rapor: Bulut kolonu, hata detayları bölümü, başarısız bulut yüklemeleri bölümü
- [x] `INotificationService.NotifyFileBackupAsync` ve `BackupJobExecutor` entegrasyonu

### v0.83.0 — Dosya Yedekleme Fark/Artırımlı Strateji & İlerleme Düzeltmesi
- [x] Dosya yedekleme stratejisi: Tam (Full), Fark (Differential), Artırımlı (Incremental)
- [x] JSON manifest sistemi: `file_full.json` + `file_last.json` ile değişen dosya tespiti (LastWriteTimeUtc + Size)
- [x] Fark yedek: Son tam yedekten bu yana değişen dosyalar; Artırımlı: Son herhangi yedekten bu yana değişenler
- [x] PlanEditForm Adım 3'te strateji ComboBox eklendi (Tam Yedek / Fark Yedek / Artırımlı Yedek)
- [x] İlerleme çubuğu dosya kaynak bazlı güncelleme: `CalculateFileSourceProgress` ile kaynak başına ağırlıklı ilerleme

### v0.77.3 — Kurtarma Şifresi (Recovery Password)
- [x] Plan bazlı kurtarma şifresi desteği
- [x] Plan şifresi unutulduğunda kurtarma şifresi ile erişim
- [x] Güvenlik sorusu sıfırlamasında kurtarma şifresi de temizlenir

### v0.77.2 — Plan Şifre UX Sadeleştirme
- [x] Checkbox tabanlı plan şifre koruması ("🔒 Bu görevi şifre ile koru")
- [x] Tikle → şifre gir → kaydet — bitti

### v0.77.1 — Plan Bazlı Şifre İzolasyonu
- [x] Plan şifresi tanımlı planlarda yalnızca plan şifresi kabul edilir (global override kaldırıldı)
- [x] Güvenlik sorusu kurtarma → plan şifresi de otomatik sıfırlanır
- [x] Şifremi Unuttum config yolu ProgramData ile uyumlu

### v0.77.0 — Post-Install Düzeltmeleri & Modern Tray İkonları
- ✅ Error 740 admin hatası çözüldü (asInvoker + sc.exe UAC)
- ✅ Tray ikonu görünmüyor: native icon handle sızıntısı düzeltildi
- ✅ Görev zamanlama layout sorunu düzeltildi (CronBuilderPanel boyut + form yükseklik)
- ✅ Modern Windows 11 flat tray ikonları (gradient + spinner animasyon)
- ✅ System.ServiceProcess.ServiceController bağımlılığı kaldırıldı

### v0.76.0 — Servis Veri Yolu Düzeltmesi & DPAPI Migrasyon
- ✅ %APPDATA% → %ProgramData% geçişi (Tray + Service aynı veri yolunu kullanır)
- ✅ DPAPI CurrentUser → LocalMachine scope geçişi (Service LocalSystem şifre çözebilir)
- ✅ Otomatik veri migrasyonu (DataMigrationHelper — dosya kopyalama + şifre re-encryption)
- ✅ Installer ProgramData dizin izinleri (Users: Modify)
- ✅ PasswordProtector geriye uyumluluk (LocalMachine → CurrentUser fallback)

### v0.75.1 — Anti-Regresyon & Installer Düzeltmesi
- ✅ 3 pre-existing test failure düzeltildi (1174/1174 → 0 failure)
- ✅ Installer takılma sorunu çözüldü (sc.exe ile servis yönetimi)
- ✅ Build-Release.ps1 RID uyumsuzluğu düzeltildi
- ✅ Merkezi TimeoutConstants sistemi (tüm provider'lar)
- ✅ Güncelleme öncesi servis otomatik durdurma

### v0.75.0 — Dialog Düzeni + Zamanlama UX + Dosya Retention Düzeltmesi
- Dosya kaynak düzenleme diyaloğu yeniden tasarlandı: 510px genişlik, GroupBox bölümleri, kapsamlı ToolTip'ler
- Zamanlama adımında "Görev" terminolojisi: "Tam Yedek Görevi", "Fark Yedek Görevi" vb. ile anlaşılır etiketler
- Tooltip'lere geri yükleme bilgileri ve öneriler eklendi
- SQL ve Dosya zamanlama bölümleri arasına görsel ayırıcı eklendi
- **Bug fix:** `Files_*.7z` dosya arşivleri artık retention politikasıyla temizleniyor (daha önce hiç temizlenmiyordu)

### v0.74.0 — Plan Bazlı Şifre Koruması
- Her plan için ayrı şifre (SHA256+DPAPI) belirlenebilir
- Plan düzenleme/silme sırasında master VEYA plan şifresi kabul edilir
- Sihirbaz Step 4'te plan şifre yönetim paneli: durum göstergesi + şifre belirleme + şifre kaldırma
- Yeni plan oluşturmada şifre sorulmaz, tetikleme (trigger) sırasında da sorulmaz

### v0.73.0 — Yerel/Bulut Mod Ayrımı Kaldırıldı
- Plan sihirbazından "Yerel" / "Bulut" mod seçimi kaldırıldı — tüm planlar 6 adımı her zaman gösteriyor
- Bulut hedef varlığı `BackupPlan.HasCloudTargets` computed property ile otomatik algılanıyor
- `BackupMode` enum ve `Mode` property geriye dönük JSON uyumluluğu için `[Obsolete]` korunuyor
- Wizard UX sadeleştirildi: Kullanıcı doğrudan hedef ekleyerek bulut depolama kullanabilir

### v0.71.1 — Çöp Kutusu Güvenlik Düzeltmesi
- Google Drive: Sadece bizim klasörümüzdeki çöp dosyaları temizlenir, kullanıcının kişisel çöpüne dokunulmaz

### v0.71.0 — Bulut Çöp Kutusu Otomatik Temizleme (Google Drive)
- Google Drive çöp kutusu: Files.EmptyTrash() API ile tek çağrıda temizleniyor
- ICloudProvider: SupportsTrash + EmptyTrashAsync arayüzü
- Orkestrasyon: EmptyTrashForAllAsync — sadece uygun hedefleri filtreler
- Pipeline: Her yedekleme sonrası otomatik çöp temizliği (hata güvenli)

### v0.68.5 — Log Çelişkileri Düzeltildi + VSS Etiket Güncellemesi
- "Express VSS" → "VSS" tüm log mesajlarında güncellendi
- Bulut yükleme başarısız olduğunda tamamlanma durumu doğru gösteriliyor (⚠ uyarı ikonu + mesaj)
- Grid ve log panelinde bulut başarısızlığı renk ve ikon ile ayrışıyor

### v0.68.0 — OneDrive/Workspace/LocalPath Kaldırma
- OneDrive desteği tamamen kaldırıldı (Microsoft.Graph, Azure.Identity, MSAL)
- GoogleDriveWorkspace enum değeri kaldırıldı, Google Drive tek tip
- CloudProviderType.LocalPath bulut hedeflerinden kaldırıldı (yerel yedekleme etkilenmedi)

### v0.66.0 — Şifre Koruması Aktif/Pasif Toggle
- Şifre aktif/pasif toggle: Şifreyi kaldırmadan korumayı geçici olarak devre dışı bırakma
- ToolStripSplitButton: Dropdown menü ile hızlı aktif/pasif geçişi
- 3 durumlu kalkan ikonu: yeşil (aktif), turuncu (pasif), gri (tanımsız)

### v0.65.0 — Log Performansı, Dark Dialog, Şifre Koruması
- Log viewer VirtualMode: Büyük log dosyaları artık UI’yı dondurmaz
- ModernMessageBox: Tüm dialog’lar dark tema ile tutarlı
- Şifre koruması: Görev işlemleri şifre ile korunur, güvenlik sorusu ile kurtarma
- Tray menü sadeleştirme: Gereksiz öğeler kaldırıldı

### v0.64.1 — Provider Listesi Sadeleştirme
- Google Drive (Bireysel/Workspace) → tek "Google Drive ✓" satırı
- ProviderMap mapping sistemi (enum uyumluluğu korunur)
- Test edilen provider'lara ✓ işareti

### v0.64.0 — Google Drive OAuth Sadeleştirme (Gömülü Credential)
- Gömülü OAuth Credential: Client ID/Secret Base64-obfuscated olarak uygulamaya gömüldü
- Parametresiz AuthorizeInteractiveAsync: tek tıkla Google hesabı bağlama
- Credential öncelik sırası: config özel > gömülü (backward compat)
- UI sadeleştirme: Client ID/Secret alanları kaldırıldı, "Hesap Bağlama" grubu

### v0.63.0 — O7: Inno Setup Installer + GitHub Actions CI/CD + Otomatik Güncelleme
- Inno Setup 6 installer: Program Files kurulumu, Windows Service sc.exe kaydı, masaüstü kısayolu, başlangıçta çalıştır, AppData korunur
- PowerShell build script: otomatik versiyon algılama, dotnet publish (Win+Service), ISCC derleme
- GitHub Actions CI/CD: v* tag tetikleme, .NET 10, build/test/publish, Inno Setup via Chocolatey, GitHub Release + installer asset
- IUpdateService + UpdateChecker: GitHub Releases API, /releases/latest, System.Version karşılaştırma, akışlı indirme + ilerleme
- Tray güncelleme entegrasyonu: günlük otomatik kontrol (24h), balon bildirim, manuel menü öğesi, temp indirme → runas installer
- 13 güncelleme kaynak anahtarı (Resources.resx)
- Autofac DI: UpdateChecker → IUpdateService (SingleInstance)

### v0.62.0 — TB1/TB4: Switch Refactor + RestoreDialog & Exhaustiveness Testleri
- TB1 — OnBackupActivityChanged switch refactor: BuildActivityLogLine/GetLogColor → switch expression + throw (fail-fast), BuildCloudUploadLogLine helper, GetStatusDisplay tuple helper, default case Log.Warning, XML doc ⚠️ uyarıları
- TB4 — RestoreDialogTests: 15 test (4 constructor null guard, 5 CleanupTempDirectory, 3 LoadHistory filtre, 3 grid row data)
- TB4 — BackupActivityExhaustivenessTests: 20 parameterized test (enum count, coverage, DynamicData BuildActivityLogLine/GetLogColor)
- InternalsVisibleTo: Win → Tests (AssemblyInfo.cs manual attribute, GenerateAssemblyInfo=false workaround)

### v0.61.0 — O5/O6: Stres Testleri + PlanProgressTracker Testleri
- Stres testleri (O5): 8 test — eşzamanlı plan çalıştırma, SemaphoreSlim kilit, büyük DB listesi, karma senaryolar, deadlock kontrolü, bulut paralel, monoton ilerleme, iptal propagasyonu
- PlanProgressTracker ağırlık modeli testleri (O6): 22 test — sınır değerler, VSS (20/50/30), NoVSS (30/70), dosya-only pipeline, karma plan, çoklu hedef dağılımı, monoton garanti
- PlanProgressTracker.cs eksik using düzeltmesi

### v0.60.0 — O2/O3/O4: Raporlama İstatistik + GFS Retention + MainWindow Ayrıştırma
- Raporlama detaylandırma (O2): EmailTemplateBuilder ile tutarlı HTML, ek istatistikler (ort. süre, en büyük yedek, sıkıştırma), veritabanı bazlı özet tablosu
- GFS Retention politikası (O3): Grandfather-Father-Son — günlük (7), haftalık (4), aylık (12), yıllık (2) periyot bazlı koruma
- MainWindow sorumluluk ayrıştırma (O4): Log buffer + UI rendering MainWindow.BackupLog.cs partial class'a taşındı
- 9 GFS Retention birim testi

### v0.59.0 — O1: Profesyonel E-posta Bildirim Şablonları
- EmailTemplateBuilder: ortak HTML şablon sınıfı (header, statusBadge, summaryTable, detailTable, errorBlock, footer)
- SQL yedek bildirimi: profesyonel şablon + CompressionVerified + VssFileCopy alanları
- Dosya yedek bildirimi: aynı şablon + kaynak detay tablosu
- NotifyFileBackupAsync: SmtpProfile desteği (eski per-plan SMTP yerine)
- 27 EmailTemplateBuilderTests birim testi

### v0.58.0 — Y1/Y2: Local-mode SQL İlerleme + VSS Test Kapsamı
- Local-mode SQL ilerleme çubuğu: bulut hedefsiz planlarda her adımda (SQL→Doğrulama→Sıkıştırma→Temizlik) ilerleme çubuğu güncelleniyor
- HasCloudTargets flag: BackupActivityEventArgs + PlanProgressTracker, Started event ile bildirim
- VssSnapshotService testleri: 19 birim testi (Dispose, path mapping, argüman doğrulama, kontrat)

### v0.57.0 — K1/K2/K3: IPC Testleri, İptal/Temizlik Testleri, RestoreDialog Tamamlama
- Named Pipe IPC protokol testleri: 18 birim testi (tüm mesaj türleri, roundtrip, kenar durumları)
- BackupCancellationRegistry testleri: 20 birim testi (Register/Cancel/Unregister, thread safety)
- Cancel/Cleanup pipeline testleri: 8 yeni test (SQL/sıkıştırma/bulut iptal propagasyonu)
- RestoreDialog `.7z` desteği: arşiv algılama → geçici klasöre açma → .bak bulma → geri yükleme → temizlik
- RestoreDialog lokalizasyon: 10 kaynak anahtarı, tüm sabit stringler `Res.Get()`/`Res.Format()`
- RestoreDialog iptal UX: işlem sırasında iptal butonu aktif, onay diyaloğu, dinamik buton metni
- DI: `ICompressionService` (SevenZipCompressionService) WinModule'e kaydedildi
- Test düzeltmeleri: SetJobData manualTrigger, UploadToAllAsync 7-param mock uyumu

### v0.56.0 — Proje Yönetişim & Branch Stratejisi
- 3 katmanlı branch stratejisi: `master` → `develop` → `feature/*/fix/*/hotfix/*`
- Modül stabilite haritası (`docs/STATUS.md`): 38 modül derecelendirmesi
- Yol haritası (`docs/ROADMAP.md`): Kısa/orta/uzun vade planlama
- Mimari kararlar günlüğü (`docs/DECISIONS.md`): 10 ADR kaydı
- Copilot direktifi Git workflow güncellemesi

### v0.55.0 — İptal/Hata Durumunda Ara Dosya Temizliği
- Yedekleme iptal veya hata durumunda tamamlanmamış `.bak`, `.7z`, `Files/` staging dosyaları otomatik siliniyor
- Per-DB snapshot takibi: Başarıyla tamamlanan DB dosyaları korunuyor, yalnızca yarım kalan dosyalar temizleniyor
- İç `catch` blokları `OperationCanceledException` yeniden fırlatıyor (iptal sinyali doğru yayılıyor)

### v0.54.0 — Named Pipe Güvenlik ACL Düzeltmesi
- Servis (SYSTEM) → Tray (kullanıcı) pipe bağlantı hatası giderildi
- `NamedPipeServerStreamAcl.Create()` ile `PipeSecurity` ACL eklendi (AuthenticatedUsers ReadWrite, SYSTEM FullControl)
- Ek NuGet paketi gerekmedi (`net10.0-windows` TFM yeterli)

### v0.53.1 — Uyarı Temizliği & Paket Güncellemesi
- SMO 181.15.0, SqlClient 6.1.4, MSAL 4.83.3’e yükseltildi (NU1608 çözüldü)
- 8 event handler’da CS8632 nullable uyarısı düzeltildi
- `SmtpProfileEditDialog._isNew` kullanılmayan alan kaldırıldı (CS0414)

### v0.53.0 — Dosya Yedekleme İlerleme Çubuğu Entegrasyonu
- Dosya yedekleme fazı ilerleme çubuğuna entegre edildi (SQL+Dosya: %80/%20 ağırlık, Dosya-only: %100)
- Dosya sıkıştırma ve bulut yükleme ayrı alt fazlar olarak izleniyor (%25 sıkıştırma, %75 bulut)
- `HasFileBackup` event property ile Started event dosya yedekleme koşulunu önceden bildiriyor
- `PlanProgressTracker` genişletildi: `HasFileBackup`, `IsFileBackupPhase`, `SqlDbCount`

### v0.52.0 — Ara Dosya Otomatik Temizliği
- Sıkıştırma başarılıysa ara `.bak` dosyası otomatik siliniyor (doğrulama başarısızsa korunur)
- Dosya yedekleme arşivi oluşturulduktan sonra geçici `Files` klasörü otomatik temizleniyor
- Temizlik adımları log konsoluna bildirim olarak yansıyor

### v0.51.1 — VSS Bulut Yükleme İlerleme Çubuğu Senkronizasyonu
- VSS dosyası bulut yüklemesi artık ilerleme çubuğuna dahil — %100'e yalnızca tüm yüklemeler bitince ulaşılıyor
- Dinamik ağırlık modeli: VSS varsa 20/50/30, yoksa 30/70 — StepChanged sinyalleri ile otomatik faz geçişi

### v0.51.0 — Tray Sidebar Program Adı + Servis Debug Modu + Log Renk Şeması Ayarları
- Tray menü kenar çubuğu artık "Koru MsSql Yedek" program adı + versiyon gösteriyor
- Windows Service yüklü değilken pipe bağlıysa "Servis: Bağlı (Debug) ✓" durumu gösteriliyor
- Ayarlar panelinden 12 terminal renk şeması arasında geçiş yapılabiliyor (Koru, Solarized, Monokai, Dracula, Nord vb.)
- Seçilen renk şeması uygulama başlangıcında otomatik uygulanıyor

### v0.50.0 — Kümülatif İlerleme Çubuğu
- İlerleme çubuğu artık her adımda (DB → Cloud) sıfırlanmak yerine toplam işlem yüzdesini kümülatif olarak gösteriyor
- Her veritabanı toplam ilerlemenin eşit dilimini alır; SQL yedekleme %30, bulut yükleme %70 ağırlıkla eşlenir
- `PlanProgressTracker` ile monoton artış garantisi (Math.Max) — ilerleme asla geriye gitmez

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
| **Faz 18** | Koyu Tema — MikroUpdate.Win Senkronizasyonu | ✅ Tamamlandı |
| **Faz 19** | Dashboard & İkon Düzeltmeleri | ✅ Tamamlandı |
| **Faz 20** | .NET 10 Native Dark Mode + Tema Yenileme | ✅ Tamamlandı |
| **Faz 21** | Plan Düzenleme Wizard + SSL & DB Adı Düzeltmeleri | ✅ Tamamlandı |
| **Faz 22** | Cron UI Builder + Tooltips + Raporlama + Layout | ✅ Tamamlandı |
| **Faz 23** | Wizard Adım Yeniden Yapılandırma + İkon Düzeltmeleri | ✅ Tamamlandı |
| **Faz 24** | Wizard Yedekleme Modu Seçimi (Yerel/Bulut) | ✅ Tamamlandı |
| **Faz 25** | Manuel Yedekleme Pipeline Tamamlama | ✅ Tamamlandı |
| **Faz 26** | Bulut Yedek Koruma: Gönderilmemiş Dosya Silme Engeli | ✅ Tamamlandı |
| **Faz 26.2** | Güvenlik Sertleştirme: TLS/SSH Doğrulama, Hata Sanitizasyonu | ✅ Tamamlandı |
| **Faz 27** | Proje Yeniden Adlandırma + Depolama Sütunu | ✅ Tamamlandı |
| **Faz 27.1** | İsim Değişikliği Sonrası Temizlik | ✅ Tamamlandı |
| **Faz 27.2** | AppData Otomatik Migrasyon (MikroSqlDbYedek → KoruMsSqlYedek) | ✅ Tamamlandı |
| **Faz 28** | Bulut Upload İlerleme + Otomatik Klasör Yapısı | ✅ Tamamlandı |
| **Faz 29** | Resumable Upload + Bulut SHA-256 Bütünlük Kontrolü | ✅ Tamamlandı |
| **Faz 30** | Yerel Yedek Bütünlük Sistemi (SQL + 7z + Dosya Kopyası SHA-256) | ✅ Tamamlandı |
| **Faz 31** | Named Pipe IPC — Servis ↔ Tray Ayrışması (Option A) | ✅ Tamamlandı |
| **Faz 32** | Pipe Bağlantı Durum Göstergesi (Tray + MainWindow) | ✅ Tamamlandı |
| **Faz 33** | Scheduler Durum Sorgusu — Next Fire Times (Pipe üzerinden) | ✅ Tamamlandı |
| **Faz 34** | Bildirim Sistemi — E-posta (MailKit) + Tray Balloon (ToastEnabled) | ✅ Tamamlandı |
| **Faz 35** | Restore UI — Yedek geçmişinden veritabanı geri yükleme diyaloğu | ✅ Tamamlandı |
| **Faz 36** | Inno Setup & Build Script — .NET 10 uyumlu dağıtım paketi | ✅ Tamamlandı |
| **Faz 37** | Grid Progress Bar + Per-Plan Log + Upload Bytes + Varsayılan Klasör | ✅ Tamamlandı |
| **Faz 38** | Dosya Yedekleme Tetikleme Hatası Düzeltme | ✅ Tamamlandı |
| **Faz 39** | Merkezi SMTP Profil Yönetimi + Kapsam test | ✅ Tamamlandı |
| **Faz 39.1** | UI Görünen Ad Düzeltmesi ("Koru MsSql Yedek") | ✅ Tamamlandı |
| **Faz 39.2** | BackupJobExecutor Kapsamlı Test Genişletmesi (+12 test, 223 toplam) | ✅ Tamamlandı |
| **Faz 40** | Periyodik Raporlama Motoru (Günlük/Haftalık/Aylık HTML rapor) | ✅ Tamamlandı |
| **Faz 41** | Görev Listesi Kolon Sıralama + Arama (ToolStrip) | ✅ Tamamlandı |
| **Faz 42** | UI Geliştirmeleri: Log Görev Filtresi + Dashboard Sıralama + Tray Animasyonu | ✅ Tamamlandı |
| **Faz 42.1** | Bulut Hedef Tooltip + Pipe IOException Sessiz Yeniden Bağlanma | ✅ Tamamlandı |
| **Faz 42.2** | Plan Bazlı Log İzolasyonu + Tek Satır İlerleme + Sonraki Çalışma Zamanı Düzeltmesi | ✅ Tamamlandı |
| **Faz 42.3** | Renkli Log Konsolu — "Koru" Temalı Trust Palette | ✅ Tamamlandı |

---

## Faz 42.3 — Renkli Log Konsolu — "Koru" Temalı Trust Palette (v0.49.0) ✅

### Yeni Özellikler
- [x] **RichTextBox log konsolu**: `_txtBackupLog` TextBox → RichTextBox dönüşümü; Cascadia Mono 9F, kenarlıksız, koyu arka plan
- [x] **Olay türüne göre renk**: Her `BackupActivityType` için ayrı renk — Başlangıç=zümrüt, Başarılı=yeşil, Hata=kırmızı, İptal=amber, Veritabanı/Adım=mavi, İlerleme=turkuaz, Bulut=mor
- [x] **ModernTheme log renk paleti**: 10 yeni renk sabiti (LogSuccess, LogError, LogWarning, LogInfo, LogProgress, LogCloud, LogStarted, LogTimestamp, LogDefault, LogConsoleBg) — Dark ve Light tema destekli
- [x] **Renkli buffer**: `_planLogs` artık `List<(string Text, Color Color)>` tuple saklar; plan geçişlerinde renkli rebuild yapılır
- [x] **GetLogColor switch expression**: `BackupActivityType → Color` eşlemesi

### Etkilenen Dosyalar
- [x] `KoruMsSqlYedek.Win\MainWindow.cs` — `AppendBackupLog`, `AppendColoredLine`, `ReplaceLastProgressLine`, `OnPlanGridSelectionChanged`, `GetLogColor`
- [x] `KoruMsSqlYedek.Win\MainWindow.Designer.cs` — RichTextBox dönüşümü (instantiation, properties, field)
- [x] `KoruMsSqlYedek.Win\Theme\ModernTheme.cs` — Log color field + Dark/Light palette assignments

---

## Faz 40 — Periyodik Raporlama Motoru ✅

### Yeni Özellikler
- [x] **`IReportingService`** — `Core\Interfaces\IReportingService.cs`: `SendReportAsync(BackupPlan, CancellationToken)` arayüzü
- [x] **`ReportingService`** — `Engine\Notification\ReportingService.cs`
  - Günlük / Haftalık / Aylık dönem hesabı (`GetReportingPeriod`)
  - `BackupHistoryManager` üzerinden dönem kayıtları sorgulanır
  - Özet: toplam/başarılı/başarısız/oran, boyut, sıkıştırma kazancı
  - Detay: son 50 kayıt tablo (tarih, DB, tür, durum, boyut, süre)
  - SMTP profil çözümü: `SmtpProfileId` → eski per-plan alanlar (geriye uyumluluk)
  - Rapor hatası yedek işlemini etkilemez
- [x] **`ReportingJob`** — `Engine\Scheduling\ReportingJob.cs`: Quartz.NET `IJob`, `DisallowConcurrentExecution`, property injection
- [x] **`QuartzSchedulerService`** güncellemesi
  - `SchedulePlanAsync`: `plan.Reporting.IsEnabled` ise `ScheduleReportingJobAsync` çağrılır
  - `BuildReportingCron()`: Daily `0 0 H * * ?`, Weekly `0 0 H ? * MON`, Monthly `0 0 H 1 * ?`; `SendHour` 0–23 sınırlanır
  - `UnschedulePlanAsync`: "Reporting" job da silinir
  - `GetNextFireTimeAsync`: "Reporting" tipi de kontrol edilir
- [x] **`EngineModule`**: `IReportingService`/`ReportingService` + `ReportingJob` kayıtları

### Test
- [x] **`ReportingServiceTests`** — 17 test (240 toplam)
  - Constructor null guard'ları (historyManager, settingsManager)
  - `SendReportAsync`: plan null → ArgumentNullException; Reporting null/disabled → history sorgulanmaz; SMTP profil yok → exception fırlatılmaz; profil ID bulunamadı → exception fırlatılmaz; profil öncesi erken çıkış doğrulaması
  - `GetReportingPeriod`: Daily dün, Weekly 7 günlük Pzt başlangıç, Monthly tam önceki ay
  - `BuildReportingCron`: null/disabled → null; Daily/Weekly/Monthly cron doğrulaması; SendHour sınırlama

### Değiştirilen Dosyalar
- [x] `KoruMsSqlYedek.Core\Interfaces\IReportingService.cs` (YENİ)
- [x] `KoruMsSqlYedek.Engine\Notification\ReportingService.cs` (YENİ)
- [x] `KoruMsSqlYedek.Engine\Scheduling\ReportingJob.cs` (YENİ)
- [x] `KoruMsSqlYedek.Engine\Scheduling\QuartzSchedulerService.cs`
- [x] `KoruMsSqlYedek.Engine\IoC\EngineModule.cs`
- [x] `KoruMsSqlYedek.Tests\ReportingServiceTests.cs` (YENİ)
- [x] `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.40.0.0
- [x] `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.40.0.0

---

## Faz 41 — Görev Listesi Sıralama + Arama ✅

### Yeni Özellikler
- [x] **Kolon başlığı tıklama ile sıralama**: Her kolon başlığına tıklayınca artan/azalan sıralama; `SortGlyphDirection` (▲/▼) ile görsel geri bildirim; İlerleme kolonu sıralamadan çıkarıldı
- [x] **ToolStrip arama kutusu** (`_tstSearch`, 200 px): Görev adı, veritabanı listesi, strateji ve depolama alanı üzerinde büyük/küçük harf duyarsız canlı filtreleme
- [x] **Durum çubuğu**: Filtre aktifken "X / Y görev", değilken mevcut format
- [x] **`PlanRowData` iç sınıfı**: Plan görüntüleme verisi (history dahil) `RefreshPlanList()`'te tek seferinde hesaplanır; `ApplyPlanFilter()` her filtre/sıralama değişikliğinde history yeniden sorgulamaz
- [x] **`ApplyPlanFilter()`**: LINQ ile filtreleme + sıralama + grid doldurma tek metodda

### Değiştirilen Dosyalar
- [x] `KoruMsSqlYedek.Win\MainWindow.Designer.cs` — `_tsSep3`, `_tslSearchLabel`, `_tstSearch` + `ColumnHeaderMouseClick`
- [x] `KoruMsSqlYedek.Win\MainWindow.cs` — `PlanRowData`, `_allPlanRows`, `ApplyPlanFilter()`, `OnPlanGridColumnHeaderClick()`, `OnPlanSearchTextChanged()`, `RefreshPlanList()` refactor
- [x] `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.41.0.0
- [x] `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.41.0.0

---

## Faz 42 — UI Geliştirmeleri: Log Görev Filtresi + Dashboard Sıralama + Tray Animasyonu ✅

### Yeni Özellikler
- [x] **Log ekranı görev adı filtresi** (`_cmbLogPlan`): Log toolbar 8. kontrol; seçili plan adına göre mesaj filtreleme; `PopulateLogPlanFilter()` + `OnLogPlanFilterChanged`; Temizle butonu sıfırlar
- [x] **Dashboard ListView sıralama**: `OnLastBackupsColumnClick` + `LastBackupsItemComparer`; Tarih/boyut/durum için tip-bilinçli karşılaştırma (`BackupResult.Tag` üzerinden); sort ok `OnListViewDrawColumnHeader`'da `AccentPrimary` üçgen
- [x] **Dashboard ListView AutoSize**: `AutoResizeListViewColumns()` helper; `TextRenderer.MeasureText` ile header+içerik ölçümü; her yüklemede otomatik; sağda boşluk kalmaz
- [x] **TrayIcon animasyonu**: `SymbolIconHelper.CreateAnimationFrames(8)` + 150ms Timer; `StartTrayAnimation/StopTrayAnimation`; `InvokeRequired` guard; yedek başladığında döner, bittiğinde durur
- [x] **Upload ilerleme ETA**: `FormatEta(bytesRemaining, speedBps)` → "Kalan: X MB | Süre: X dk" log satırında
- [x] **Grid NextRun sütunu**: ISO 8601 → yerel `dd.MM.yyyy HH:mm`; `_nextFireTimes` dict filtre geçişlerinde kalıcı
- [x] **Başarısız görev satır rengi**: `ModernTheme.GridErrorRow`; `PlanRowData.LastBackupFailed`
- [x] **copilot-instructions.md**: Proje-spesifik yeniden yazım; bileşenler arası haberleşme tablosu

### Değiştirilen Dosyalar
- [x] `KoruMsSqlYedek.Win\MainWindow.Designer.cs` — `_lblLogPlan`, `_cmbLogPlan`, `ColumnClick`
- [x] `KoruMsSqlYedek.Win\MainWindow.cs` — `PopulateLogPlanFilter`, `ApplyLogFilter`, `LastBackupsItemComparer`, `AutoResizeListViewColumns`, `FormatEta`, sort alanları
- [x] `KoruMsSqlYedek.Win\TrayApplicationContext.cs` — animasyon lifecycle
- [x] `KoruMsSqlYedek.Win\Helpers\SymbolIconHelper.cs` — `RenderRotatedIcon`, `CreateAnimationFrames`
- [x] `KoruMsSqlYedek.Win\Theme\ModernTheme.cs` — `GridErrorRow`
- [x] `KoruMsSqlYedek.Win\Properties\Resources.resx` + `Resources.tr-TR.resx` — `LogViewer_AllPlans`
- [x] `.github\copilot-instructions.md` — Tam yeniden yazım
- [x] `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.42.0.0
- [x] `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.42.0.0

---

## Faz 42.1 — Bulut Hedef Tooltip + Pipe IOException Sessiz Yeniden Bağlanma ✅

### Yeni Özellik
- [x] **`_txtRemotePath` dinamik tooltip** (`CloudTargetEditDialog`): Provider türüne göre format rehberi
  - **Google Drive / OneDrive**: başında `/` veya `\` olmadan; alt klasör için `/` ayırıcı; Örnek: `Yedekler/Plan1`
  - **FTP / FTPS / SFTP**: Unix yolu, başında `/`; Örnek: `/yedekler/plan1`
  - `UpdateRemotePathTooltip(type)` metodu `UpdateFieldVisibility()` içinden provider değişiminde tetiklenir
  - Tooltip hem label (`_lblRemotePath`) hem de TextBox (`_txtRemotePath`) üzerinde gösterilir

### Hata Düzeltmesi
- [x] **Yedek sonrası "Servis bağlantısı kesildi" balonu** (`ServicePipeClient`): `ReadLoopAsync` içinde `IOException` yakalanarak `SetConnected(false)` tetiklenmemesi sağlandı; yeniden bağlanma döngüsü sessizce devam eder

### Değiştirilen Dosyalar
- [x] `KoruMsSqlYedek.Win\Forms\CloudTargetEditDialog.cs` — `UpdateRemotePathTooltip()`, `UpdateFieldVisibility()` çağrısı
- [x] `KoruMsSqlYedek.Win\Forms\CloudTargetEditDialog.Designer.cs` — `_toolTipRemotePath` alanı + `new ToolTip(components)`
- [x] `KoruMsSqlYedek.Win\IPC\ServicePipeClient.cs` — `ReadLoopAsync` IOException try/catch
- [x] `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.42.9.0
- [x] `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.42.9.0

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
  - JSON dosya kalıcılığı: %APPDATA%\KoruMsSqlYedek\Config\appsettings.json
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

- [x] KoruMsSqlYedek.iss script
- [x] Tray + Service birlikte kurulum
- [x] Service otomatik start kaydı
- [x] Güncelleme sırasında plan/log koruma
- [x] Build-Release.ps1 otomasyon scripti
- [x] Minimum gereksinim kontrolü (.NET 4.8, Windows 10+)


## Faz 19 — Dashboard & İkon Düzeltmeleri (v0.19.1) ✅

- [x] Dashboard ListView (`_lvLastBackups`) OwnerDraw etkinleştirildi — sütun başlıkları tema renkleriyle (GridHeaderBack/GridHeaderText) çiziliyor
- [x] DrawColumnHeader/DrawItem/DrawSubItem event handler'ları eklendi
- [x] KPI kart ikonları `Segoe MDL2 Assets` Label → `PictureBox` + Phosphor ikonlarına dönüştürüldü (CheckCircle, Clock, Database)
- [x] PhosphorIcons sessiz hata yakalama (`catch { return null; }`) → Serilog Error loglama ile değiştirildi
- [x] 0 hata ile build doğrulandı

---

## Faz 21 — Plan Düzenleme Wizard + SSL & DB Adı Düzeltmeleri (v0.21.0) ✅

### Wizard Tabanlı Plan Düzenleme
- [x] 8-tab TabControl → 5 adımlı wizard panel sistemi (PlanEditForm.Designer.cs tamamen yeniden yazıldı)
- [x] Adım 1: Plan Bilgileri + SQL Bağlantı (eski Tab 1+2 birleştirildi)
- [x] Adım 2: Veritabanı Seçimi (Tümünü Seç + Yenile butonları, otomatik DB yükleme)
- [x] Adım 3: Yedekleme Stratejisi & Zamanlama
- [x] Adım 4: Sıkıştırma & Saklama Politikası (eski Tab 4+6 birleştirildi)
- [x] Adım 5: Bulut Hedefler + Bildirim + Dosya Yedekleme (eski Tab 5+7+8 birleştirildi)
- [x] Adım göstergesi (üst bar): tamamlanan=✓ yeşil, aktif=beyaz, gelecek=devre dışı
- [x] Geri/İleri/Kaydet/İptal navigasyon çubuğu (alt bar)
- [x] Adım geçişlerinde doğrulama (plan adı + sunucu zorunlu)
- [x] Adım 1→2 geçişinde otomatik bağlantı testi + veritabanı listesi yükleme

### SSL Sertifika Düzeltmesi
- [x] `SqlConnectionInfo` modeline `TrustServerCertificate` özelliği eklendi (varsayılan: true)
- [x] `BuildConnectionString()`: TrustServerCertificate=true → Encrypt=Optional, aksi halde Mandatory
- [x] `CreateServerConnection()`: `BuildConnectionString()` kullanarak yeniden yazıldı (kod tekrarı giderildi)
- [x] PlanEditForm'a `_chkTrustCert` checkbox eklendi

### Veritabanı Adı Bozulması Düzeltmesi
- [x] `DatabaseListItem` sınıfı: `Name` (gerçek DB adı) + `DisplayText` (biçimlendirilmiş) ayrımı
- [x] `LoadDatabaseListAsync()`, `LoadPlanToUi()`, `SaveUiToPlan()` → DatabaseListItem kullanımı

### Kod Kalitesi
- [x] `BuildCurrentConnInfo()`: SQL bağlantı bilgisi tek noktada toplanarak kod tekrarı giderildi
- [x] `OnTestSqlConnectionClick` → `BuildCurrentConnInfo()` kullanacak şekilde sadeleştirildi
- [x] `ApplyIcons()`: `_btnRefreshDatabases` ikonu eklendi, kullanılmayan değişken temizlendi
- [x] 0 hata ile build doğrulandı

---

## Faz 22 — Cron UI Builder + Tooltips + Raporlama + Layout (v0.22.0) ✅

### CronBuilderPanel UserControl
- [x] Sıklık seçimi ComboBox: Günlük / Haftalık / Aylık / Özel (Cron)
- [x] Haftalık mod: 7 gün checkbox'ı (Pzt-Paz), çoklu gün seçimi
- [x] Aylık mod: ayın günü spinner (1-28)
- [x] Saat/dakika spinner'ları (varsayılan 02:00)
- [x] Özel mod: ham Quartz cron ifadesi girişi (PlaceholderText ile)
- [x] Canlı önizleme: insan okunabilir açıklama + ham cron ifadesi
- [x] `GetCronExpression()` / `SetCronExpression(string)` public API
- [x] Mevcut cron ifadesini geri ayrıştırma (günlük/haftalık/aylık/özel algılama)
- [x] Quartz.NET 6-alan formatı: saniye dakika saat ayGünü ay haftaGünü

### ToolTip Sistemi
- [x] `ToolTip` bileşeni: 15 saniyelik AutoPopDelay ile detaylı açıklamalar
- [x] Tüm form alanlarında Türkçe tooltip'ler (örnekli: "Örn: SQLSERVER01 veya 192.168.1.100")
- [x] Cron alanları, sıkıştırma, saklama politikası, bildirim ayarları dahil

### Raporlama Yapılandırması
- [x] `ReportFrequency` enum: Daily, Weekly, Monthly (Enums.cs)
- [x] `ReportingConfig` modeli: IsEnabled, Frequency, EmailTo, SendHour (ConfigModels.cs)
- [x] `BackupPlan.Reporting` özelliği (varsayılan: new ReportingConfig())
- [x] PlanEditForm UI: Rapor etkinleştirme, sıklık, e-posta, gönderim saati
- [x] `OnReportEnabledChanged` + `UpdateReportFieldsVisibility` görünürlük yönetimi

### Layout & Türkçe İyileştirmeleri
- [x] Form boyutu: 580x560 → 640x640 (CronBuilderPanel için geniş alan)
- [x] Etiket sütunu: tx=140→150, tw=320→340 (daha iyi hizalama)
- [x] Türkçe etiket düzeltmeleri: "Bağlantıyı Sına", "Yerel Yedek Klasörü", "Sunucu Adı / IP"
- [x] Bölüm numaralandırma: ① ② ③ ④ ⑤ ⑥ ile görsel ayrım
- [x] Tüm alanlar için anlamlı varsayılan değerler
- [x] PlanEditForm.cs: TextBox cron referansları → CronBuilderPanel API'sine güncellendi
- [x] 0 hata ile build doğrulandı

---

## Faz 23 — Wizard Adım Yeniden Yapılandırma + İkon Düzeltmeleri (v0.23.0) ✅

### 6 Adımlı Wizard Yapısı (5→6)
- [x] Kaynaklar ayrıldı: Adım 2 = Veritabanları + Dosya Kaynakları (SQL + VSS dosyalar birlikte)
- [x] Hedefler ayrıldı: Adım 5 = Bulut / Uzak Hedefler (Google Drive, OneDrive, FTP/SFTP, UNC)
- [x] Bildirim & Rapor son adıma taşındı: Adım 6 = E-posta Bildirimleri + Periyodik Rapor
- [x] Zamanlama birleştirildi: Adım 3 = SQL Strateji + Dosya Zamanlama (dosya bölümü koşullu görünürlük)
- [x] Adım göstergesi 5→6 noktaya genişletildi (stepW=103, font 8.25F/7.5F)
- [x] Adım başlıkları: Bağlantı → Kaynaklar → Zamanlama → Sıkıştırma → Hedefler → Bildirim

### Çift İkon Düzeltmesi
- [x] Tüm butonlardan `TextImageRelation` kaldırıldı — ModernButton'un özel `OnPaint` çizimiyle çakışma giderildi
- [x] Navigasyon butonlarına Phosphor ikonları eklendi: ArrowLeft (Geri), ArrowRight (İleri), FloppyDisk (Kaydet), XCircle (İptal)
- [x] `PhosphorIcons.cs`: ArrowLeft (\ue038) ve ArrowRight (\ue044) sabitleri eklendi
- [x] Secondary/Ghost buton ikonları TextPrimary rengiyle (beyaz yerine daha iyi kontrast)

### Layout İyileştirmeleri
- [x] Form boyutu: 640x640 → 660x680 (6 adım ve geniş içerik için)
- [x] Alan genişliği: tw=340 → tw=420 (daha geniş TextBox/ComboBox'lar)
- [x] Hedefler adımı: Açıklayıcı ipucu metni + büyük ListView (478x380)
- [x] Buton metinlerinden Unicode oklar (▶/◀) kaldırıldı — sadece Phosphor ikonları
- [x] `UpdateFileScheduleVisibility()`: Dosya zamanlama bölümü koşullu görünürlük
- [x] 0 hata ile build doğrulandı

---

## Faz 26 — Bulut Yedek Koruma: Gönderilmemiş Dosya Silme Engeli (v0.26.0) ✅

### Retention Bulut Koruma Mantığı
- [x] `RetentionCleanupService` constructor'ına `IBackupHistoryManager` enjekte edildi
- [x] Bulut modda (`BackupMode.Cloud`): geçmiş kayıtları sorgulanır, buluta gönderilmemiş dosyalar silinmez
- [x] `BuildCloudProtectedFileSet()`: Başarısız cloud upload'lu dosyaların tam yol seti oluşturulur
- [x] Geçmiş okunamadığında güvenlik modu: hiçbir dosya silinmez (`*PROTECT_ALL*`)
- [x] Yerel modda (`BackupMode.Local`): mevcut davranış korunur (bulut kontrolü yapılmaz)
- [x] Detaylı loglama: silinen, korunan ve atlanan dosyalar ayrı ayrı raporlanır
- [x] Mevcut 8 retention testi geçiyor (Moq mock ile güncellendi)
- [x] 0 hata ile build doğrulandı

---

## Faz 26.2 — Güvenlik Sertleştirme: TLS/SSH Doğrulama, Hata Sanitizasyonu (v0.26.2) ✅

### FTPS Sertifika Doğrulaması
- [x] FTPS bağlantılarında varsayılan olarak sertifika doğrulaması aktif (FluentFTP sistem CA deposu kullanır)
- [x] `FtpsSkipCertificateValidation` ayarı ile self-signed sertifikalar için bilinçli bypass

### SFTP Host Key Doğrulaması (TOFU)
- [x] İlk SFTP bağlantısında sunucu parmak izi otomatik kaydedilir
- [x] Sonraki bağlantılarda parmak izi eşleşmezse bağlantı reddedilir (MITM koruması)
- [x] `HostFingerprintUpdated` event ile plan dosyasına persist desteği

### Hata Mesajı Sanitizasyonu
- [x] `MainWindow.SanitizeErrorMessage()`: Dosya yolları, stack trace gizlenir
- [x] `EmailNotificationService.SanitizeForEmail()`: HTML encode + yol gizleme + uzunluk sınırı
- [x] Bulut upload hata mesajları e-postada sanitize edilir

### Plaintext Şifre Tespiti
- [x] FTP/SFTP ve UNC bağlantılarında DPAPI korumasız şifre loglanır
- [x] DPAPI çözme hatası `Log.Error` seviyesine yükseltildi

---

## Faz 25 — Manuel Yedekleme Pipeline Tamamlama (v0.25.1) ✅

### Manuel Yedekleme Tam Pipeline
- [x] `ICompressionService` ve `ICloudUploadOrchestrator` bağımlılıkları MainWindow'a eklendi
- [x] Manuel yedekleme artık tam pipeline çalıştırıyor: SQL Backup → Verify → Compress → Cloud Upload → History
- [x] Sıkıştırma: Plan'daki `Compression` ayarı varsa `.bak` → `.7z` arşivleme (LZMA2, DPAPI şifreli)
- [x] Doğrulama: Plan'da `VerifyAfterBackup` aktifse RESTORE VERIFYONLY çalıştırılır
- [x] Bulut Upload: Plan modu `Cloud` ve hedefler aktifse sıkıştırılmış/ham dosya bulut hedeflere yüklenir
- [x] Geçmiş Kayıt: Her yedek sonucu `correlationId` ile `IBackupHistoryManager`'a kaydedilir
- [x] `SaveBackupHistory()` helper metodu eklendi
- [x] Her pipeline adımı detaylı log çıktısı üretir (↳/✓/✗ göstergeleri)

### Autofac + Dosya Yedekleme Düzeltmeleri (v0.25.1)
- [x] `CloudUploadOrchestrator` Autofac `UsingConstructor(typeof(ICloudProviderFactory))` ile belirtildi
- [x] `IFileBackupService` MainWindow constructor'ına enjekte edildi
- [x] Dosya yedekleme pipeline eklendi: FileBackup → Compress (.7z dizin arşivi) → Cloud Upload
- [x] VSS desteği ile açık/kilitli dosyalar (Outlook PST/OST vb.) yedeklenir
- [x] 0 hata ile build doğrulandı

### 7z.dll Entegrasyonu (v0.25.2)
- [x] `7z.dll` (x64) native binary projeye eklendi (`Engine\Native\x64\7z.dll`)
- [x] Build'de output dizinine otomatik kopyalama (`PreserveNewest`)
- [x] `Initialize()` metodu 3 aşamalı fallback ile güncellendi
- [x] Tüm projeler (Win, Service, Tests) build sonrası `x64\7z.dll` içerir
- [x] 0 hata ile build doğrulandı

---

## Faz 24 — Wizard Yedekleme Modu Seçimi (Yerel/Bulut) (v0.24.0) ✅

### Yedekleme Modu Seçimi
- [x] `BackupMode` enum eklendi: `Local` (Disk/UNC/Ağ) ve `Cloud` (Google Drive/OneDrive/FTP/SFTP)
- [x] `BackupPlan.Mode` özelliği eklendi (varsayılan: Local, JSON serileştirme destekli)
- [x] Step 1'e RadioButton seçimi: "Yerel (Disk / UNC / Ağ Paylaşımı)" ve "Bulut (Google Drive / OneDrive / FTP / SFTP)"
- [x] Detaylı tooltip açıklamaları her iki mod için

### Dinamik Wizard Navigasyonu
- [x] `_activeSteps` listesi: Yedekleme moduna göre aktif adımları dinamik hesaplar
- [x] Yerel mod: 5 adım (Bağlantı → Kaynaklar → Zamanlama → Sıkıştırma → Bildirim) — Hedefler atlanır
- [x] Bulut mod: 6 adım (Bağlantı → Kaynaklar → Zamanlama → Sıkıştırma → Hedefler → Bildirim)
- [x] `RebuildActiveSteps()`: Mod değiştiğinde adım listesini yeniden oluşturur
- [x] `RebuildStepIndicator()`: Üst bar göstergesini aktif adımlara göre dinamik çizer (5 veya 6 nokta)
- [x] Adım genişliği otomatik ayarlanır: 5 adım=124px, 6 adım=103px
- [x] `ShowStep()`, `OnNextClick()`, `OnBackClick()` — `_activeSteps` indeksleri üzerinden navigasyon
- [x] `ValidateCurrentStep()` — panel indeksine göre doğrulama
- [x] `OnBackupModeChanged()` — mod değiştiğinde wizard'u anında yeniden yapılandırır
- [x] `LoadPlanToUi()` / `SaveUiToPlan()` — BackupMode okuma/yazma
- [x] 0 hata ile build doğrulandı

---

## Faz 20 — .NET 10 Native Dark Mode + Tema Yenileme (v0.20.0) ✅

### Native Dark Mode Entegrasyonu
- [x] `Application.SetColorMode(SystemColorMode.Dark)` — .NET 10 native dark mode etkinleştirildi
- [x] Program.cs: `ApplyNativeColorMode()` metodu eklendi (ModernTheme ayarı → SystemColorMode eşlemesi)
- [x] ModernFormBase: DWM dark title bar hack kaldırıldı (native API yönetiyor)
- [x] ModernFormBase: 12+ standart kontrol manual theming kaldırıldı (TextBox, ComboBox, CheckBox, RadioButton, NumericUpDown, CheckedListBox, Label, ListView, TabControl, GroupBox, Button — native auto-dark)
- [x] ModernFormBase: `ApplyThemeToAllChildren` → `ApplyThemeToCustomControls` yeniden adlandırıldı
- [x] NativeMethods: `DwmSetWindowAttribute`, `DWMWA_USE_IMMERSIVE_DARK_MODE`, `SetWindowTheme` P/Invoke kaldırıldı

### Tema Paleti Yenileme
- [x] Emerald accent renk: `(0,150,80)` → `(16,185,129)` (Tailwind emerald-500)
- [x] Daha geniş elevation farkı: Background(18,18,22) → Surface(30,30,36) → Hover(48,48,56)
- [x] Modern durum renkleri: Success=emerald, Warning=amber(245,158,11), Error=red(239,68,68), Info=blue(96,165,250)

### Kontrol Görsel Düzeltmeleri
- [x] ModernButton Ghost stili: siyah alpha → beyaz alpha overlay (koyu arka planda görünür)
- [x] ModernCardPanel gölge alpha: 15 → 50 (koyu temada görünür)
- [x] ModernNumericUpDown: dikdörtgen → yuvarlak köşe (radius=4)
- [x] ModernLoadingOverlay: sabit açık arka plan → tema-duyarlı koyu arka plan
- [x] ModernToggleSwitch off rengi: (190,190,195) → (70,70,78)
- [x] ModernToolStripRenderer: sabit accent → dinamik, hover alpha 20 → 35
- [x] MaterialSkin.2 NuGet paketi kaldırıldı
- [x] 0 hata ile build doğrulandı

---

## Faz 18 — Koyu Tema / MikroUpdate.Win Senkronizasyonu (v0.17.0) ✅

- [x] ModernTheme.cs renk paleti light → dark olarak güncellendi (16 renk sabiti)
- [x] Accent rengi mavi → yeşil RGB(0,150,80)
- [x] ModernToolStripRenderer hover overlay'leri koyu tema için düzeltildi
- [x] VersionSidebarRenderer portlandı — tray menüsünde yeşil gradient sidebar
- [x] TrayApplicationContext'e VersionSidebarRenderer uygulandı
- [x] 0 hata ile build doğrulandı

---

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

## Faz 27 — Proje Yeniden Adlandırma + Depolama Sütunu ✅

- [x] Tüm proje/namespace/assembly adları `MikroSqlDbYedek` → `KoruMsSqlYedek` olarak yeniden adlandırıldı (124 dosya)
- [x] 5 proje klasörü yeniden adlandırıldı (Core/Engine/Win/Service/Tests)
- [x] 5 `.csproj` dosyası yeniden adlandırıldı
- [x] `MikroSqlDbYedek.slnx` → `KoruMsSqlYedek.slnx` yeniden adlandırıldı
- [x] Inno Setup: `AppName = "Koru MsSql Yedek"`, `.iss` dosyası yeniden adlandırıldı
- [x] Git remote URL güncellendi: `https://github.com/hzkucuk/KoruMsSqlYedek`
- [x] Eski stub klasörler (`MikroSqlDbYedek.*`) temizlendi
- [x] `dotnet restore` + `dotnet build` — 0 hata ile tamamlandı
- [x] Görev listesi: "Depolama" sütunu eklendi — `☁ Bulut (N)` / `💾 Yerel` gösterimi

## Faz 28 — Bulut Upload İlerleme + Otomatik Klasör Yapısı ✅

- [x] Her bulut hedefi için anlık ilerleme etiketi + progress bar (`CloudUploadStarted / Progress / Completed`)
- [x] `RemoteFolderPath` boş bırakılırsa otomatik `KoruMsSqlYedek/{PlanAdı}/` klasörü oluşturulur
- [x] Per-target log satırları (✓/✗)

## Faz 29 — Resumable Upload + Bulut SHA-256 Bütünlük Kontrolü ✅

- [x] `UploadStateRecord` + `UploadStateManager`: kesilen upload'lar JSON ile persist edilir
- [x] `PathHelper.UploadStateDirectory`: state dosyaları `%APPDATA%\KoruMsSqlYedek\UploadState\`
- [x] **Google Drive**: `InitiateSessionAsync()` → `Task<Uri>` ile session URI yakalanır; `ResumeAsync(uri)` ile devam
- [x] **OneDrive**: `UploadSession.UploadUrl` persist; `LargeFileUploadTask` ile resume
- [x] **FTP/FTPS**: `FtpRemoteExists.Resume` — sunucu-taraflı kaldığı yerden devam
- [x] **SFTP**: `GetAttributes.Size` offset ile `client.Open(ReadWrite)` + seek ile append
- [x] `CloudUploadOrchestrator`: SHA-256 tek hesaplanır; per-target state oluştur/yükle; upload sonrası remote boyut karşılaştırması
- [x] `RecoverPendingUploadsAsync`: uygulama başlangıcında bekleyen upload'lar otomatik yeniden başlatılır
- [x] Hem `TrayApplicationContext` hem `BackupWindowsService`'te startup recovery

## Faz 30 — Yerel Yedek Bütünlük Sistemi (SQL + 7z + Dosya Kopyası SHA-256) ✅

### SQL Server Yedek Doğrulama
- [x] `SqlBackupService.VerifyBackupAsync()`: SMO `Restore.SqlVerify(server)` → `RESTORE VERIFYONLY`
- [x] `BackupJobExecutor` Step 2: `plan.VerifyAfterBackup = true` ise otomatik çalışır
- [x] `BackupResult.VerifyResult: bool?` — sonuç takibi

### 7z Arşiv Bütünlük Doğrulama
- [x] `ICompressionService.VerifyArchiveAsync(archivePath, password, ct) → Task<bool>`
- [x] `SevenZipCompressionService.VerifyArchiveAsync()`: `SevenZipExtractor.Check()` — tüm girdiler CRC kontrolü
- [x] Şifreli arşiv desteği: doğru parola ile açılır, CRC karşılaştırılır
- [x] `BackupJobExecutor` Step 3b: compress sonrası otomatik doğrulama
- [x] `BackupResult.CompressionVerified: bool?` — sonuç takibi

### Dosya Kopyası SHA-256 Doğrulama
- [x] `FileBackupService.VerifyFileCopyIntegrityAsync()`: boyut karşılaştırması + SHA-256
- [x] `ComputeFileSha256Async()`: async stream hash, `FileShare.ReadWrite` ile kilitli dosya desteği
- [x] Kaynak kilitli ise (IOException): boyut eşleşmesi yeterli kabul edilir
- [x] `FileBackupResult.FilesVerified` + `FilesVerificationFailed` alanları
- [x] `IFileBackupService.BackupSourceAsync()`: `verifyAfterCopy = false` opsiyonel parametre
- [x] `BackupFilesAsync()`: `plan.VerifyAfterBackup` ile doğrulama aktif edilir

---

## Faz 31 — Named Pipe IPC: Servis ↔ Tray Ayrışması (Option A) ✅

### Mimari Değişiklik
- [x] Tray uygulaması artık yedekleme motorunu **doğrudan çalıştırmıyor**; yalnızca Named Pipe üzerinden Windows Service ile iletişim kuruyor
- [x] `KoruMsSqlYedekPipe` — JSON + newline protokolü; çok-istemci desteği

### Yeni Dosyalar
- [x] `Core\IPC\PipeProtocol.cs` — `PipeMessage`, `ManualBackupCommand`, `CancelBackupCommand`, `BackupActivityMessage`, `ServiceStatusMessage`, `PipeSerializer`
- [x] `Core\IPC\BackupCancellationRegistry.cs` — `IBackupCancellationRegistry`; planId → CTS eşlemesi; pipe üzerinden iptal desteği
- [x] `Service\IPC\ServicePipeServer.cs` — çok-istemci AcceptLoop, komut yönetimi, `BackupActivityHub` yayını
- [x] `Win\IPC\ServicePipeClient.cs` — otomatik yeniden bağlantı (5 sn), `BackupActivityHub.Raise`, `SendManualBackupCommandAsync`, `SendCancelCommandAsync`
- [x] `Win\IoC\WinModule.cs` — Tray için hafif Autofac modülü (Quartz/sıkıştırma/bulut YOK)

### Değiştirilen Dosyalar
- [x] `BackupJobExecutor.cs` — `IBackupCancellationRegistry` property injection; CTS kayıt/sil
- [x] `BackupWindowsService.cs` — `ServicePipeServer` inject; Start/Stop entegrasyonu
- [x] `ServiceContainerBootstrap.cs` — `BackupCancellationRegistry` + `ServicePipeServer` singleton
- [x] `WinContainerBootstrap.cs` — `EngineModule` → `WinModule`
- [x] `TrayApplicationContext.cs` — Scheduler bağımlılıkları kaldırıldı; `ServicePipeClient` ile çalışır
- [x] `MainWindow.cs` — pipe-tabanlı manuel yedekleme; `OnBackupActivityChanged`; `UpdatePlanRowStatus`

---

## Faz 32 — Pipe Bağlantı Durum Göstergesi (Tray + MainWindow) ✅

### Yeni Özellikler
- [x] `TrayIconStatus.Disconnected` — gri uyarı ikonu; servis bağlantısı kesilince tray ikonu griye döner
- [x] Tray ballon tip bildirimleri: bağlandığında "Servis bağlandı", kesildiğinde "Servis bağlantısı kesildi"
- [x] MainWindow status bar: bağlı → yeşil "Servis bağlı", bağlı değil → kırmızı "Servis bağlı değil"
- [x] `ConnectionChanged` event'i `TrayApplicationContext` ve `MainWindow`'da subscribe edildi

### Değiştirilen Dosyalar
- [x] `Win\Helpers\SymbolIconHelper.cs` — `TrayIconStatus.Disconnected` + gri `SymbolWarning` ikonu
- [x] `Win\TrayApplicationContext.cs` — `OnPipeConnectionChanged` handler; başlangıçta Disconnected
- [x] `Win\MainWindow.cs` — `OnPipeConnectionChanged` + `UpdateStatusBarConnection`
- [x] `Win\Properties\Resources.resx` + `Resources.tr-TR.resx` — 7 yeni kaynak anahtarı

---

## Faz 33 — Scheduler Durum Sorgusu — Next Fire Times ✅

### Yeni Özellikler
- [x] `ServiceStatusMessage.NextFireTimes` — `Dictionary<string, string>` (planId → "dd.MM.yyyy HH:mm")
- [x] Servis her bağlantı isteğinde + yedek tamamlandığında/başarısız/iptal sonrası tüm istemcilere `ServiceStatusMessage` yayınlar
- [x] Dashboard zamanlayıcısı her 30 saniyede `RequestStatusAsync()` çağırır
- [x] Plan grid'indeki `_colNextRun` sütunu canlı güncellenir
- [x] `ServiceStatusHub` statik event hub — pipe'dan gelen status mesajlarını UI bileşenlerine iletir

### Yeni Dosyalar
- [x] `Core\IPC\ServiceStatusHub.cs` — `StatusReceived` event; `Raise(ServiceStatusMessage)` metodu

### Değiştirilen Dosyalar
- [x] `Service\IPC\ServicePipeServer.cs` — `IPlanManager` inject; `NextFireTimes` doldurma; `BroadcastStatusAsync()` yeni metod
- [x] `Win\IPC\ServicePipeClient.cs` — `ServiceStatus` mesajı → `ServiceStatusHub.Raise()`
- [x] `Win\MainWindow.cs` — `ServiceStatusHub.StatusReceived` subscribe; `OnServiceStatusReceived` handler; grid güncelleme

---

## Faz 34 — Bildirim Sistemi (MailKit + ToastEnabled) ✅

### Yeni Özellikler
- [x] **E-posta bildirimi**: MailKit SMTP — HTML formatlı yedek sonuç maili; her plan için bağımsız SMTP ayarı
- [x] **Tray balloon kontrolü**: `ToastEnabled` plan ayarı — `false` yapıldığında balloon tip gösterilmez
- [x] **Bildirim güvenliği**: Bildirim hatası yedek sonucunu etkilemez; hata log'a yazılır

### Değiştirilen Dosyalar
- [x] `Core\Events\BackupActivityEvent.cs` — `BackupActivityEventArgs.ToastEnabled` eklendi (varsayılan `true`)
- [x] `Core\IPC\PipeProtocol.cs` — `BackupActivityMessage.toastEnabled` JSON alanı; `FromArgs` + `ToArgs` güncellendi
- [x] `Service\IPC\ServicePipeServer.cs` — `OnActivityChanged`: plan config'den `ToastEnabled` okunup mesaja yazılır
- [x] `Win\TrayApplicationContext.cs` — tüm balloon case'lerine `if (e.ToastEnabled)` koruma eklendi

---

## Faz 35 — Restore UI (Yedekten Geri Yükleme Diyaloğu) ✅

### Yeni Özellikler
- [x] **RestoreDialog**: Plan sağ tık → "Geri Yükle..." — başarılı yedekler listelenir
- [x] **RESTORE VERIFYONLY**: "Doğrula" butonu ile yedek dosyası bütünlük kontrolü
- [x] **RESTORE DATABASE**: Hedef DB adı düzenlenebilir; restore öncesi güvenlik yedeği seçeneği (varsayılan açık)
- [x] **İlerleme + canlı log**: Yüzde progress bar + zaman damgalı log TextBox
- [x] **Güvenlik**: Onay diyaloğu; 2 saatlik timeout + form kapatılınca iptal

### Yeni Dosyalar
- [x] `Win\Forms\RestoreDialog.cs` — history yükleme, verify, restore, progress, log
- [x] `Win\Forms\RestoreDialog.Designer.cs` — DataGridView (6 sütun) + options TLP + progress + log + butonlar

### Değiştirilen Dosyalar
- [x] `Win\MainWindow.Designer.cs` — `_ctxSep4` + `_ctxRestore` context menu öğesi eklendi
- [x] `Win\MainWindow.cs` — `OnCtxRestoreClick` handler
- [x] `Win\Properties\Resources.resx` + `Resources.tr-TR.resx` — 20 yeni kaynak anahtarı (EN + TR)

---

## Faz 36 — Inno Setup & Build Script (.NET 10 Uyumlu Dağıtım) ✅

### Güncellemeler
- [x] **KoruMsSqlYedek.iss**: Versiyon 0.36.0, .NET 10 Desktop Runtime kontrolü (`IsDotNet10DesktopInstalled`)
- [x] **Inno Setup .NET kontrolü**: `FindFirst` ile `{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\10.*` varlığı kontrol edilir
- [x] **CustomMessages**: DotNetRequired mesajı .NET 10 Desktop Runtime için TR + EN güncellendi
- [x] **Build-Release.ps1**: Her iki `dotnet publish` komutuna `-r win-x64 --self-contained false` eklendi
- [x] **Build-Release.ps1**: Adım sayacı `[1/6]–[6/6]` → `[1/7]–[7/7]` güncellendi
- [x] **Build-Release.ps1**: Yeni 7. adım — `ISCC.exe` ile Inno Setup installer otomatik derleme (PATH veya varsayılan `%ProgramFiles(x86)%\Inno Setup 6\` konumunda aranır; bulunamazsa uyarı ile devam eder)

### Değiştirilen Dosyalar
- [x] `Deployment\InnoSetup\KoruMsSqlYedek.iss` — versiyon, .NET 10 kontrolü, mesajlar
- [x] `Deployment\Build-Release.ps1` — publish RID parametreleri + ISCC adımı

---

## Faz 37 — Grid Progress Bar + Per-Plan Log + Upload Bytes + Varsayılan Klasör ✅

### Yeni Özellikler
- [x] **Grid İlerleme Sütunu**: `DataGridViewProgressBarColumn` — aktif yedekleme sırasında satır bazlı % progress bar
- [x] **Per-Plan Log Yalıtımı**: Her plana ait log ayrı buffer'da saklanır; görevler arası geçişte `_txtBackupLog` seçili plana göre güncellenir
- [x] **Upload Byte/Hız Bilgisi**: Log satırı `%XX | Gönderilen: Y/Z | Hız: W/s` formatında; progress bar `CustomText` modunda göstergede aynı bilgiler
- [x] **Varsayılan Uzak Klasör**: Yeni bulut hedef oluştururken boş `RemoteFolderPath` → OAuth için `KoruMsSqlYedek`, FTP/SFTP için `/KoruMsSqlYedek`

### Teknik Değişiklikler
- [x] `BackupActivityEventArgs`: `BytesSent`, `BytesTotal`, `SpeedBytesPerSecond` eklendi
- [x] `BackupActivityMessage`: yeni alanlar JSON serialization ile pipe üzerinden taşınır
- [x] `CloudUploadOrchestrator.hubProgress`: `uploadStartTime` ile byte/hız hesabı
- [x] `DataGridViewProgressBarCell` + `DataGridViewProgressBarColumn`: custom WinForms hücre tipi
- [x] `MainWindow._planLogs` / `_planProgress`: plan bazlı log ve ilerleme sözlükleri
- [x] `RefreshPlanList`: `_colProgress` için `0` başlangıç değeri
- [x] `BuildActivityLogLine`: `static` → instance; `CloudUploadProgress` için bytes formatı

### Değiştirilen Dosyalar
- [x] `KoruMsSqlYedek.Core\Events\BackupActivityEvent.cs`
- [x] `KoruMsSqlYedek.Core\IPC\PipeProtocol.cs`
- [x] `KoruMsSqlYedek.Engine\Cloud\CloudUploadOrchestrator.cs`
- [x] `KoruMsSqlYedek.Win\Forms\CloudTargetEditDialog.cs`
- [x] `KoruMsSqlYedek.Win\Theme\DataGridViewProgressBarCell.cs` (YENİ)
- [x] `KoruMsSqlYedek.Win\MainWindow.Designer.cs`
- [x] `KoruMsSqlYedek.Win\MainWindow.cs`
- [x] `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.37.0.0
- [x] `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.37.0.0

---

## Faz 38 — Eşzamanlı Yedekleme Desteği (v0.46.0) ✅

### Yeni Özellikler
- [x] **Farklı planlar paralel yedeklenebilir**: Bir plan çalışırken diğer planların "Yedekle" butonu ve sağ-tık menüsü aktif kalır
- [x] **Per-plan kilit mekanizması**: Service tarafında global `SemaphoreSlim(1,1)` yerine plan bazlı `ConcurrentDictionary<string, SemaphoreSlim>` — aynı plan iki kez çalışamaz
- [x] **Per-plan UI durum takibi**: `_isBackupRunning` (bool) → `_runningPlanIds` (HashSet) — seçili plana göre buton/menü durumu

### Hata Düzeltmesi
- [x] **Tray animasyon `ArgumentException`**: `Color.FromArgb` negatif red değer alıyordu → `Math.Clamp(0, 255)` ile düzeltildi

### Değiştirilen Dosyalar
- [x] `KoruMsSqlYedek.Win\MainWindow.cs` — per-plan state tracking
- [x] `KoruMsSqlYedek.Engine\Scheduling\BackupJobExecutor.cs` — per-plan lock
- [x] `KoruMsSqlYedek.Win\Helpers\SymbolIconHelper.cs` — Math.Clamp fix
- [x] `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.46.0.0
- [x] `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.46.0.0

---

## Faz 39 — VSS Bulut Yükleme + Modern Tray İkon + Servis Kontrol (v0.47.0) ✅

### Hata Düzeltmesi
- [x] **VSS dosyası buluta yüklenmiyor**: Express VSS `*_VSS_*.7z` dosyaları artık SQL yedek dosyasından sonra otomatik bulut hedeflerine yükleniyor

### Yeni Özellikler
- [x] **Modern circular badge tray ikonu**: Gradient arka planlı yuvarlak badge stili, her durum için farklı renk, beyaz sembol
- [x] **Spinning arc animasyon**: 12 karelı dönen beyaz ark (progress spinner), mavi gradient arka plan
- [x] **Servis kontrol iyileştirmesi**: Win32Exception ayrı yakalama, "Yönetici olarak çalıştırın" uyarısı, gerçek SCM durumu gösterimi

### Değiştirilen Dosyalar
- [x] `KoruMsSqlYedek.Engine\Scheduling\BackupJobExecutor.cs` — VSS cloud upload
- [x] `KoruMsSqlYedek.Win\Helpers\SymbolIconHelper.cs` — modern circular badge icons + spinning arc animation
- [x] `KoruMsSqlYedek.Win\TrayApplicationContext.cs` — improved service control error handling
- [x] `KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs` — v0.47.0.0
- [x] `KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj` — ApplicationVersion 0.47.0.0
