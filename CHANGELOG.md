## [0.25.2] - 2025-07-12 — 7z.dll Entegrasyonu: Sıkıştırma Çalışır Hale Getirildi

### Düzeltmeler
- **7z.dll eksikliği düzeltildi**: `Squid-Box.SevenZipSharp` paketi native `7z.dll`'yi içermiyordu — sıkıştırma uyarı verip çalışmıyordu
- **Native DLL entegrasyonu**: `Engine\Native\x64\7z.dll` projeye eklendi, build'de output'a kopyalanır
- **Geliştirilmiş DLL arama**: 3 aşamalı fallback: `x64/7z.dll` → `7z.dll` → `Program Files\7-Zip`

### Etkilenen Dosyalar
- MikroSqlDbYedek.Engine/Native/x64/7z.dll (yeni — native binary)
- MikroSqlDbYedek.Engine/MikroSqlDbYedek.Engine.csproj (Content copy to output)
- MikroSqlDbYedek.Engine/Compression/SevenZipCompressionService.cs (Initialize fallback güncellendi)
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.25.2)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (versiyon 0.25.2)

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
- MikroSqlDbYedek.Engine/IoC/EngineModule.cs (UsingConstructor eklendi)
- MikroSqlDbYedek.Win/MainWindow.cs (IFileBackupService eklendi, dosya yedekleme pipeline)
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.25.1)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (versiyon 0.25.1)

---

## [0.25.0] - 2025-07-12 — Faz 25: Manuel Yedekleme Pipeline Tamamlama

### Düzeltmeler
- **Manuel yedekleme tam pipeline**: Sıkıştırma, doğrulama, bulut upload ve geçmiş kayıt artık manuel yedeklemede de çalışıyor
  - SQL Backup → Verify (RESTORE VERIFYONLY) → Compress (.7z LZMA2) → Cloud Upload → History
- **Yeni bağımlılıklar**: `ICompressionService` ve `ICloudUploadOrchestrator` MainWindow'a Autofac ile enjekte edildi
- **`SaveBackupHistory()`**: Yedek sonuçlarını correlationId ile geçmişe kaydeden helper metod
- **Detaylı log çıktısı**: Her pipeline adımı ↳/✓/✗ göstergeleriyle raporlanır

### Etkilenen Dosyalar
- MikroSqlDbYedek.Win/MainWindow.cs (constructor genişletildi, OnStartBackupClick tam pipeline, SaveBackupHistory eklendi)
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.25.0)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (versiyon 0.25.0)

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
- MikroSqlDbYedek.Core/Models/Enums.cs (BackupMode enum)
- MikroSqlDbYedek.Core/Models/BackupPlan.cs (Mode özelliği)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.Designer.cs (RadioButton kontrolleri, _activeSteps)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.cs (dinamik navigasyon, RebuildActiveSteps, RebuildStepIndicator)
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.24.0)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (versiyon 0.24.0)

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
- MikroSqlDbYedek.Win/Forms/PlanEditForm.Designer.cs (tamamen yeniden yazıldı — 6 adım)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.cs (6 adım mantığı, ikon düzeltmeleri)
- MikroSqlDbYedek.Win/Theme/PhosphorIcons.cs (ArrowLeft/ArrowRight sabitleri)
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.23.0)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (versiyon 0.23.0)

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
- MikroSqlDbYedek.Win/Controls/CronBuilderPanel.cs (yeni)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.Designer.cs (tamamen yeniden yazıldı)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.cs (CronBuilderPanel + raporlama entegrasyonu)
- MikroSqlDbYedek.Core/Models/Enums.cs (ReportFrequency enum)
- MikroSqlDbYedek.Core/Models/ConfigModels.cs (ReportingConfig sınıfı)
- MikroSqlDbYedek.Core/Models/BackupPlan.cs (Reporting özelliği)
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.22.0)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (versiyon 0.22.0)

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
- MikroSqlDbYedek.Win/Forms/PlanEditForm.Designer.cs (tamamen yeniden yazıldı)
- MikroSqlDbYedek.Win/Forms/PlanEditForm.cs (wizard navigasyon + düzeltmeler)
- MikroSqlDbYedek.Core/Models/ConfigModels.cs (TrustServerCertificate eklendi)
- MikroSqlDbYedek.Engine/Backup/SqlBackupService.cs (BuildConnectionString + CreateServerConnection)
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs (versiyon 0.21.0)
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj (versiyon 0.21.0)

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
- MikroSqlDbYedek.Win/Program.cs
- MikroSqlDbYedek.Win/Theme/ModernFormBase.cs
- MikroSqlDbYedek.Win/Theme/ModernTheme.cs
- MikroSqlDbYedek.Win/Theme/ModernButton.cs
- MikroSqlDbYedek.Win/Theme/ModernCardPanel.cs
- MikroSqlDbYedek.Win/Theme/ModernNumericUpDown.cs
- MikroSqlDbYedek.Win/Theme/ModernLoadingOverlay.cs
- MikroSqlDbYedek.Win/Theme/ModernToggleSwitch.cs
- MikroSqlDbYedek.Win/Theme/ModernToolStripRenderer.cs
- MikroSqlDbYedek.Win/NativeMethods.cs
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj
- FEATURES.md

---

## [0.19.1] - 2025-07-09 — Faz 19: Dashboard & İkon Düzeltmeleri

### Düzeltmeler
- **Dashboard ListView başlık beyaz**: `_lvLastBackups` için `OwnerDraw = true` etkinleştirildi; `DrawColumnHeader` handler'ı tema renkleriyle (GridHeaderBack/GridHeaderText) sütun başlıkları çiziyor
- **KPI kart ikonları görünmüyor**: `_lblStatusIcon`, `_lblNextIcon`, `_lblPlansIcon` — `Label` + `Segoe MDL2 Assets` font → `PictureBox` + Phosphor Render ile değiştirildi
- **PhosphorIcons sessiz hata**: `catch { return null; }` → `Serilog.Log.Error()` ile hata loglanıyor

### Etkilenen Dosyalar
- MikroSqlDbYedek.Win/MainWindow.Designer.cs
- MikroSqlDbYedek.Win/MainWindow.cs
- MikroSqlDbYedek.Win/Theme/PhosphorIcons.cs
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
- MikroSqlDbYedek.Win/Theme/ModernFormBase.cs
- MikroSqlDbYedek.Win/NativeMethods.cs
- MikroSqlDbYedek.Win/MainWindow.cs
- MikroSqlDbYedek.Win/MainWindow.Designer.cs
- MikroSqlDbYedek.Win/Forms/PlanEditForm.cs
- MikroSqlDbYedek.Win/Forms/PlanEditForm.Designer.cs
- MikroSqlDbYedek.Win/Forms/CloudTargetEditDialog.cs
- MikroSqlDbYedek.Win/Forms/CloudTargetEditDialog.Designer.cs
- MikroSqlDbYedek.Win/Forms/FileBackupSourceEditDialog.cs
- MikroSqlDbYedek.Win/Forms/FileBackupSourceEditDialog.Designer.cs
- MikroSqlDbYedek.Win/MikroSqlDbYedek.Win.csproj
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs

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
- MikroSqlDbYedek.Win/Properties/Resources.resx
- MikroSqlDbYedek.Win/Properties/Resources.tr-TR.resx
- MikroSqlDbYedek.Win/Theme/ModernComboBox.cs
- MikroSqlDbYedek.Win/Theme/ModernProgressBar.cs
- MikroSqlDbYedek.Core/Models/AppSettings.cs
- MikroSqlDbYedek.Win/MainWindow.Designer.cs
- MikroSqlDbYedek.Win/MainWindow.cs
- MikroSqlDbYedek.Win/Program.cs

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
- MikroSqlDbYedek.Win/MainWindow.cs (yeni)
- MikroSqlDbYedek.Win/MainWindow.Designer.cs (yeni)
- MikroSqlDbYedek.Win/TrayApplicationContext.cs
- MikroSqlDbYedek.Win/IoC/WinContainerBootstrap.cs
- MikroSqlDbYedek.Win/Properties/AssemblyInfo.cs

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
