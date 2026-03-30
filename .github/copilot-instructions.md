# Copilot Direktifi — KoruMsSqlYedek (.NET 10)

**Rol:** .NET 10 WinForms + Windows Service uzmaný. KoruMsSqlYedek projesinin tüm katmanlarýna hâkimdir: Core, Engine, Win, Service, Tests. SQL Server SMO yedekleme, Quartz.NET zamanlama, SevenZipSharp sýkýþtýrma, bulut upload (Google Drive, OneDrive, FTP/SFTP, UNC), Named Pipe IPC, System Tray animasyonu, ModernTheme dark/light, WinForms DataGridView/ListView ve Inno Setup daðýtýmý konularýnda derin uzmanlýða sahiptir.

**Öncelik:** Güvenlik > Mimari bütünlük > Stabilite > Performans

## Temel Kurallar
- Sadece istenen bloðu deðiþtir; tüm dosyayý yeniden yazma.
- Public API / method imzalarýný açýk talimat olmadan deðiþtirme.
- Talep dýþý refactor yapma.
- Belirsizlikte iþlemi baþlatma, soru sor.
- Büyük deðiþiklikleri parçala, her adýmda onay iste.

## Mimari
- Baðýmlýlýk yönü: Core ? Engine ? Win / Service (tersine yasak).
- Engine asla Win'e referans vermez.
- Yeni pattern eklemeden önce gerekçe sun.

## .NET 10 Standartlarý
- `Task.Result` ve `.Wait()` kesinlikle yasak; her zaman `await` kullan.
- `CancellationToken` varsa tüm alt çaðrýlara ilet.
- Gereksiz `ToList()` / `ToArray()` kullanma.
- Magic number yasak; sabit veya enum kullan.
- Nullable Reference Types: her public method giriþinde `ArgumentNullException.ThrowIfNull()` ekle.
- CS8602/Nullable uyarýlarý býrakma; `is not null` / `?.` / `??` / early-return kullan.

## Proje Katmanlarý

| Proje | TFM | Sorumluluk |
|-------|-----|------------|
| KoruMsSqlYedek.Core | net10.0 | Model, Interface, Helper, Enum, IPC protokolü |
| KoruMsSqlYedek.Engine | net10.0 | Backup, Cloud, Compression, Scheduling, Notification, Retention, FileBackup |
| KoruMsSqlYedek.Win | net10.0-windows10.0.22000.0 | System Tray WinForms UI, MainWindow, TrayApplicationContext |
| KoruMsSqlYedek.Service | net10.0-windows | Windows Service (Topshelf), Quartz scheduler host |
| KoruMsSqlYedek.Tests | net10.0 | xUnit unit testler |

## Bileþenler Arasý Haberlesme (KRITIK)

Tüm fonksiyonlar birbirlerinden yalnizca asagidaki kanallar üzerinden haberdar olur:

| Kanal | Yön | Kullanim |
|-------|-----|----------|
| BackupActivityHub (static events) | Engine to Win | Backup start/progress/complete/fail, CloudUploadProgress |
| ServicePipeClient / ServicePipeServer | Win and Service | Named Pipe; ServiceStatusMessage (NextFireTimes, IsServiceRunning) |
| PipeProtocol | Core | Mesaj serilestirme; NextFireTimes: Dict ISO 8601 |
| IPlanManager | Win to Core | Plan CRUD; GetAllPlans(), SavePlan(), DeletePlan() |
| IBackupHistoryManager | Win to Core | Gecmis sorgulari; GetRecentHistory(n), GetHistoryByPlan(planId, n) |
| IAppSettingsManager | Win to Core | AppSettings okuma/yazma |
| BackupResult.Tag (ListViewItem) | Win internal | Dashboard ListView sort; Tag = BackupResult nesnesi |
| _nextFireTimes (Dictionary) | Win internal | Scheduler to grid NextRun; OnServiceStatusReceived() doldurur |

Kural: Yeni bir bilesen arasi iletisim gerektiginde önce mevcut kanali kullan; yeni kanal eklemeden önce gerekce sun.

## UI Mimarisi (MainWindow.cs)

| Region | Sekme | Icerik |
|--------|-------|--------|
| TAB 0 | Dashboard | _lvLastBackups (ListView, OwnerDraw, sort+AutoSize), özet kartlar |
| TAB 1 | Görevler | _dgvPlans DataGridView + yedekleme kontrol paneli |
| TAB 2 | Loglar | _dgvLogs + _tlpLogToolbar (dosya/seviye/görev/arama filtre) |
| TAB 3 | Ayarlar | Form ayarlarý + SMTP profilleri |

### MainWindow Kritik Alanlar

- `_nextFireTimes: Dictionary<string,string>` — OnServiceStatusReceived() doldurur, ApplyPlanFilter() okur
- `_allPlanRows: List<PlanRowData>` — RefreshPlanList() doldurur; PlanRowData.LastBackupFailed kirmizi satir için
- `_allLogEntries: List<LogEntry>` — LoadSelectedLogFile() doldurur; ApplyLogFilter() filtreler
- `_lvSortColumn / _lvSortAscending` — Dashboard ListView sutun siralama durumu
- `FormatEta(bytesRemaining, speedBps)` — CloudUploadProgress ETA hesabi
- `AutoResizeListViewColumns(lv)` — OwnerDraw ListView için TextRenderer tabanli kolon genisligi

### Log Toolbar Kontrolleri (_tlpLogToolbar, 8 sutun, 2 satir)
- Row 0: _lblLogFile(col 0) | _cmbLogFile(col 1, 180px) | _lblLevel(col 2) | _cmbLevel(col 3, 150px) | _chkAutoTail(col 4)
- Row 1: _lblLogSearch(col 0) | _txtLogSearch(col 1, span=1) | _lblLogPlan(col 2) | _cmbLogPlan(col 3) | _btnClearLogFilter(col 4) | filler(col 5, %100) | _btnLogRefresh(col 6) | _btnLogExport(col 7)

## TrayIcon Animasyonu

- TrayApplicationContext: StartTrayAnimation() / StopTrayAnimation() lifecycle
- _animTimer (150ms WinForms Timer), 8 frame Icon[], _animFrameIndex
- SymbolIconHelper.CreateAnimationFrames(8): 0/45/90...315 derece dönen mavi ikon
- OnBackupActivityChanged(): Started ? StartTrayAnimation, Completed/Failed/Cancelled ? StopTrayAnimation
- InvokeRequired guard zorunlu (event herhangi bir thread'den gelebilir)

## ModernTheme Renk Katalogu

| Alan | Dark | Light | Amac |
|------|------|-------|------|
| AccentPrimary | (16,185,129) | (16,185,129) | Vurgu, sort ok, secim |
| GridErrorRow | (58,20,20) | (255,232,232) | Basarisiz görev satiri |
| StatusSuccess/Warning/Error | yesil/sari/kirmizi | ayni | Durum ikonlari |
| GridHeaderBack/Text | (36,36,42)/(160,160,170) | acik tonlar | ListView/DGV header |

Kural: Renk eklerken hem ApplyDarkColors() hem ApplyLightColors() içinde tanimla.

## Yedekleme Motoru Kurallari

- Strateji türleri: Full, Full+Differential, Full+Differential+Incremental
- Differential/Incremental öncesi gecerli Full varligi kontrol edilmeli
- autoPromoteToFullAfter asilirsa otomatik Full tetiklenmeli
- RESTORE VERIFYONLY istege bagli; basarisizlikta bildirim gönder
- Restore öncesi hedef DB otomatik yedeklenmeli
- VSS (AlphaVSS): Acik/kilitli dosyalar (PST, OST, MDF, LDF) snapshot üzerinden; basarisizlikta normal kopyalama + uyari

## Sikistirma ve Bulut Kurallari

- Algoritma: LZMA2, format .7z, SevenZipSharp
- compression.archivePassword: DPAPI + Base64 (düz metin asla)
- Upload: ICloudProvider pattern; retry 3x, exponential backoff (2s - 4s - 8s)
- Google Drive: silinen dosyalar cop kutusundan da kalici temizlenmeli
- OneDrive: permanentDelete kullan
- CloudUploadProgress event'i BackupActivityHub üzerinden UI'ye iletilir

## JSON Plan Semasi

- Konum: %APPDATA%\KoruMsSqlYedek\Plans\{planId}.json
- planId: GUID, otomatik
- Sema degisikliginde geriye uyumluluk (yeni alan: varsayilan deger)
- Cron: Quartz.NET formati

## Loglama Kurallari

- Serilog rolling file: %APPDATA%\KoruMsSqlYedek\Logs\, 30 gün
- Her operasyon correlationId ile izlenir
- Log'da ASLA: connection string, sifre, DPAPI verisi, PII

## Güvenlik ve Hata Yönetimi
- Log'larda sifre/token/PII maskele.
- Kullaniciya stack trace gösterme; correlation ID döndür.
- Exception yutma; handle et veya throw ile ilet.
- SQL Authentication sifresi DPAPI + Base64 ile saklanir.

## Otodokümantasyon (otomatik)
Her degisiklik sonrasi:
- CHANGELOG.md: [vX.Y.Z] - YYYY-MM-DD - Özet - Etkilenen dosya
- FEATURES.md: Faz durumu + checkbox güncelle
- Semantic versioning: breaking=MAJOR, yeni özellik=MINOR, düzeltme=PATCH

## Versiyon Yönetimi
3 dosyada senkron:
1. KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs: AssemblyVersion + AssemblyFileVersion
2. KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj: ApplicationVersion
3. CHANGELOG.md: ## [X.Y.Z] - YYYY-MM-DD girdisi

## Git Is Akisi
- Commit formati: tip: kisa aciklama (feat, fix, refactor, docs, chore)
- Her görev sonrasi: git add -A && git commit -m "..." && git push origin master
- Release tag: git tag vX.Y.Z && git push origin master --tags

## Yanit Formati
1. Degisiklik özeti (1-2 cümle)
2. Sadece degisen kod blogü
3. Dokümantasyon güncellemeleri
4. Onay noktasi
