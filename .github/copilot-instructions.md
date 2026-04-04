# Copilot Direktifi — .NET 10

**Rol:** Sen deneyimli bir Windows Forms (.NET 10) geliştiricisisin. WinForms uygulama mimarisi, kontrol yaşam döngüsü, UI thread yönetimi (Control.Invoke / BeginInvoke / async-await), custom kontrol geliştirme, GDI+ çizim, ClickOnce dağıtımı ve Windows API entegrasyonu konularında derin uzmanlığa sahipsin. Kullanıcı deneyimini ön planda tutarak temiz, sürdürülebilir ve performanslı WinForms kodu yazarsın.

**Öncelik:** Güvenlik > Mimari bütünlük > Stabilite > Performans

## Temel Kurallar
- Sadece istenen bloğu değiştir; tüm dosyayı yeniden yazma.
- Public API / method imzalarını açık talimat olmadan değiştirme.
- Talep dışı refactor yapma.
- Belirsizlikte işlemi başlatma, soru sor.
- Büyük değişiklikleri parçala, her adımda onay iste.

## Mimari
- Mevcut mimariyi (MVC / Razor Pages / Clean Architecture) koru.
- Katman ihlali yasak. Yeni pattern eklemeden önce gerekçe sun.

## .NET 10 Standartları
- `Task.Result` ve `.Wait()` kesinlikle yasak; her zaman `await` kullan.
- WinForms'ta UI güncellemelerini her zaman UI thread'inde yap; `Control.InvokeRequired` kontrolünü ihmal etme.
- `CancellationToken` varsa tüm alt çağrılara ilet.
- Gereksiz `ToList()` / `ToArray()` kullanma.
- Magic number yasak; sabit veya enum kullan.
- Nullable Reference Types: her public method girişinde `ArgumentNullException.ThrowIfNull()` ekle.
- **CS8602 / Nullable uyarıları:** Kod yazarken veya değiştirirken `CS8602` (olası null başvuru) ve diğer nullable uyarılarını (`CS8600`, `CS8601`, `CS8603`, `CS8604`) bırakma. Null olabilecek değişkenlere erişimden önce `is not null` kontrolü, `!` (null-forgiving) yerine `??` / `?.` operatörü veya early-return pattern kullan. Her değişiklik sonrası build uyarılarını kontrol et.

## Veritabanı
Açık talimat olmadan: EF Migration oluşturma, kolon silme/rename/tip değiştirme.

## Güvenlik & Hata Yönetimi
- Log'larda şifre/token/PII maskele.
- Kullanıcıya stack trace gösterme; correlation ID döndür.
- Exception yutma; handle et veya `throw` ile ilet.

## Otodökümantasyon (otomatik — hatırlatma bekleme)
Her değişiklik sonrası:
- **CHANGELOG.md:** `[vX.Y.Z] — YYYY-MM-DD — [Özet] — [Etkilenen dosya]`
- **README.md:** Yeni özellik, kurulum değişikliği, yapı değişikliği veya kullanım farkı olduğunda ilgili bölümü güncelle. Sürüm badge'ini güncel tut.
- **FEATURES.md:** Yeni yetenek veya mantık değişikliğinde güncelle.
- **INSTALL.md:** NuGet / config / env değişikliğinde senkronize et.
- Semantic versioning: breaking=MAJOR, yeni özellik=MINOR, düzeltme=PATCH.

## Versiyon Yönetimi (kritik — her release'de uygulanmalı)
Versiyon proje başına **senkron** tutulmalı:

1. **Ana `.csproj`** → `<Version>`, `<AssemblyVersion>`, `<FileVersion>`, `<ApplicationVersion>`
2. **`CHANGELOG.md`** → `## [X.Y.Z] - YYYY-MM-DD` girdisi
3. **`README.md`** → Version badge

- Versiyon değişikliğinde **üçü birlikte** güncellenmelidir.
- Semantic versioning: breaking=MAJOR, yeni özellik=MINOR, düzeltme=PATCH.

## Release Süreci (kullanıcı "release derle" dediğinde)

Kullanıcı "release derle", "release yap", "release oluştur" veya benzeri dediğinde aşağıdaki adımları **sırayla** uygula:

1. **Versiyon güncelle** — Yukarıdaki 3 nokteyi yeni versiyon numarasıyla senkronize et
2. **Dokümantasyon güncelle** — CHANGELOG.md, README.md, FEATURES.md, INSTALL.md gerekli bölümlerini güncelle
3. **Build doğrula** — `dotnet build -c Release` çalıştır, hata olmadığından emin ol
4. **Installer derle** — Projeye ait build/setup script'ini çalıştır, installer çıktısını doğrula
5. **Git commit (develop)** — Tüm değişiklikleri commit et: `git add -A && git commit -m "release: vX.Y.Z"`
6. **Merge to master** — `git checkout master && git merge develop --no-ff -m "release: vX.Y.Z"`
7. **Git tag** — Versiyon tag'i oluştur: `git tag vX.Y.Z`
8. **Git push** — Her iki branch'i push et: `git push origin master --tags && git push origin develop`
9. **Develop'a dön** — `git checkout develop`
10. **Bilgilendir** — GitHub Actions otomatik tetiklenecek, installer oluşturulup GitHub Release'e eklenecek

## Yanıt Formatı
1. Değişiklik özeti (1-2 cümle)
2. Sadece değişen kod bloğu
3. Dokümantasyon güncellemeleri
4. Onay noktası

## ═══════════════ REGRESYON ÖNLEME DİREKTİFLERİ ═══════════════
> Bu bölüm, tekrarlayan hataların kök nedenlerinden çıkarılmış **zorunlu** kurallardır.
> Her kod değişikliğinde bu kurallar kontrol edilmelidir.

### 1. PlanId Propagasyonu (Kritik — Çoklu Plan Desteği)
- **Kural:** Servis katmanından (Engine) UI katmanına (Win) ulaşan **her event/callback** mutlaka `PlanId` taşımalıdır.
- **Kontrol listesi:**
  - Yeni bir event args sınıfı oluşturulursa → `string PlanId` property'si zorunlu.
  - Yeni bir `ICloudUploadOrchestrator`, `IBackupService` veya benzeri interface metodu eklenirse → `string planId` parametresi düşünülmeli.
  - `BackupJobExecutor` içindeki her çağrıda `plan.PlanId` geçirilmeli.
  - UI tarafında `AppendBackupLog` her zaman `planId` almalı; PlanId olmadan log eklenmez.
- **Neden:** v0.48.0'da `CloudUploadOrchestrator` eventleri PlanId taşımadığı için farklı planların logları karıştı.

### 2. Dictionary/Collection Güvenliği
- **Kural:** `_nextFireTimes`, `_planLogs`, `_planProgress` gibi durum sözlüklerini **toptan temizleme (`Clear()`) yapma**.
- Yalnızca tek anahtar güncelle (`dict[key] = value`) veya tek anahtar sil (`dict.Remove(key)`).
- Yeni plan başladığında sadece o planın buffer'ını temizle, diğerlerine dokunma.
- **Neden:** v0.48.0'da `_nextFireTimes` toptan temizlenince tüm planların sonraki çalışma zamanları kayboldu.

### 3. İlerleme Satırı Yönetimi (Tek Satır Güncelleme)
- **Kural:** Tekrarlayan ilerleme bilgisi (upload %, veritabanı ilerleme vb.) log paneline **yeni satır olarak eklenmez**, son ilerleme satırı **yerinde güncellenir**.
- `isProgressLine` flag'i ile `AppendBackupLog` çağrılmalı.
- `ReplaceLastProgressLine` metodu `ProgressLineMarker` sabiti ile son satırı tanır ve üzerine yazar.
- Buffer'da da aynı mantık uygulanır (son tuple güncellenir, yeni eklenmez).
- **Neden:** v0.48.0'da her upload progress eventi yeni satır ekliyordu, log paneli aşağı kayıyordu.

### 4. Interface/Method İmza Değişikliği Propagasyonu
- **Kural:** Bir interface veya method imzası değiştirildiğinde:
  1. **Tüm implementasyonlar** güncellenmeli (Find All References / Go To Implementation kullan).
  2. **Tüm çağrı noktaları** (callers) güncellenmeli.
  3. Build ile doğrula — compile error kalmadığından emin ol.
- Eksik parametre geçirme → runtime'da null/default davranış → sessiz hata.
- **Kontrol:** `find_symbol` (navigationType=2 veya 3) ile tüm referansları ve implementasyonları kontrol et.

### 5. Event Handler Zinciri Bütünlüğü
- **Kural:** `OnBackupActivityChanged` gibi merkezi event handler'lara yeni bir `BackupActivityType` case eklendiğinde:
  1. `switch` bloğuna case ekle (UI davranış).
  2. `BuildActivityLogLine` → log metni.
  3. `GetLogColor` → renk eşlemesi.
  4. `UpdatePlanRowStatus` → grid ikonu/rengi.
  5. Gerekirse `_progressBar` güncelleme mantığı.
- **Eksik case = sessiz hata.** Her yeni ActivityType bu 5 noktada kontrol edilmeli.

### 6. Buffer ↔ UI Senkronizasyonu
- **Kural:** `_planLogs` buffer'ına yazılan her veri, UI'ya da aynı formatta yansımalı.
- Buffer tuple tipi `(string Text, Color Color)` — değiştirilirse `OnPlanGridSelectionChanged` rebuild mantığı da güncellenmeli.
- Plan geçişlerinde `_txtBackupLog.Clear()` + buffer'dan renkli rebuild yapılır.
- **Asla** buffer'a yazıp UI'ya yazmayı veya tam tersini yapma.

### 7. UI Thread Güvenliği
- **Kural:** UI kontrollerine erişen **her method** `InvokeRequired` kontrolü yapmalı.
- Pattern: `if (InvokeRequired) { Invoke(new Action(() => MethodName(args))); return; }`
- `async void` event handler'lar hariç, arka plan thread'inden direkt UI erişimi **kesinlikle yasak**.
- Timer/Callback'lerden gelen çağrılarda da aynı kontrol uygulanır.

### 8. Yeni Özellik Ekleme Kontrol Listesi
Her yeni özellik veya değişiklik sonrası şu soruları cevapla:
- [ ] PlanId tüm event zincirine propagate ediliyor mu?
- [ ] Dictionary/collection toptan temizleniyor mu? (Yasak)
- [ ] İlerleme satırları tek satırda mı güncelleniyor?
- [ ] Interface değişikliği varsa tüm implementasyon + caller güncel mi?
- [ ] Event handler switch'ine yeni case ekleniyorsa 5 nokta kontrol edildi mi?
- [ ] Buffer ve UI senkron mu?
- [ ] UI thread safety sağlandı mı?
- [ ] Build başarılı mı? (uyarı dahil kontrol)
- [ ] RichTextBox pozisyon hesaplaması `Text` yerine `Lines[]`/`GetFirstCharIndexFromLine()` kullanıyor mu?

### 9. RichTextBox Text vs Select İndeks Uyumsuzluğu (Kritik)
- **Kural:** `RichTextBox.Text` `\r\n` döndürür ama `Select(start, length)` dahili indeks kullanır (`\n` tek karakter).
- `Text` üzerinden `IndexOf`/`LastIndexOf` ile hesaplanan pozisyonları **asla** `Select()` ile kullanma.
- Doğru yol: `Lines[]` + `GetFirstCharIndexFromLine(lineIndex)` veya `TextLength` (dahili uzunluk).
- **Neden:** v0.49.0'da `ReplaceLastProgressLine` `Text.LastIndexOf` pozisyonu ile `Select()` çağrınca satır sayısı arttıkça offset kayıyordu ve tüm metin siliniyordu.

### 10. "Bozulma Riski Yüksek" Dosyalar
Bu dosyalarda değişiklik yaparken **ekstra dikkatli** ol:

| Dosya | Risk | Neden |
|-------|------|-------|
| `MegaProvider.cs` | 🔴 Çok Yüksek | **2 kez regresyon yaşandı!** Session caching + semaphore + trash filtresi; snapshot doğrulaması zorunlu |
| `MainWindow.cs` → `OnBackupActivityChanged` | 🔴 Yüksek | Tüm backup eventlerinin merkezi; eksik case = sessiz hata |
| `MainWindow.cs` → `AppendBackupLog` | 🔴 Yüksek | Buffer + UI + renk + ilerleme; 4 sorumluluğu var |
| `GoogleDriveProvider.cs` → `EmptyTrashAsync` | 🔴 Yüksek | Klasör kapsamlı sorgu zorunlu; `Files.EmptyTrash()` TÜM çöpü siler — YASAK |
| `MainWindow.cs` → `OnPlanGridSelectionChanged` | 🟡 Orta | Buffer rebuild; format değişirse kırılır |
| `CloudUploadOrchestrator.cs` | 🟡 Orta | Event firing; PlanId eksikliği log karışmasına yol açar |
| `BackupJobExecutor.cs` | 🟡 Orta | Plan lifecycle; PlanId geçirilmezse izolasyon bozulur |
| `ModernTheme.cs` | 🟢 Düşük | Renk ekleme güvenli; ama Dark+Light **ikisi birden** güncellenmeli |

### 11. Snapshot Doğrulama (Kritik — Kritik Dosya Değişikliklerinde Zorunlu)
- **Kural:** `.copilot/critical-files-manifest.md` dosyasında listelenen **kritik dosyalarda** herhangi bir değişiklik yapılmadan ÖNCE:
  1. `.copilot/snapshots/` dizinindeki ilgili snapshot dosyasını oku.
  2. Korunması gereken kalıpları (session caching, semaphore, trash filtresi vb.) belirle.
  3. Değişiklik SONRASI manifest'teki kontrol listesini tek tek doğrula.
  4. Eksik kalıp tespit edilirse değişikliği **tamamla, kaybetme**.
- **Özellikle MegaProvider.cs:** Her değişiklikte şu 4 kalıp MUTLAKA mevcut olmalı:
  1. `_cachedClient` + `_sessionSemaphore` statik alanları
  2. `GetOrCreateSessionAsync` metodu
  3. `InvalidateSessionInternalAsync` metodu
  4. Tüm public metotlarda semaphore + cache pattern
- **Neden:** v0.71.x'te trash özelliği eklenirken MegaProvider.cs session caching tamamen kaybedildi. Bu kural tekrarı önler.
- **Snapshot güncelleme:** Büyük değişiklik sonrası snapshot'ı da güncelle: `Copy-Item "dosya" ".copilot/snapshots/vX.Y.Z/dosya"`

### 12. Yeni Özellik → Mevcut Kalıp Koruması
- **Kural:** Bir dosyaya yeni özellik eklerken, mevcut kalıplar **olduğu gibi korunmalıdır**.
- Dosyanın tamamını yeniden yazmak yerine **sadece ilgili bloğu** ekle/değiştir.
- `replace_string_in_file` ile minimal değişiklik yap; tüm dosyayı `create_file` ile üzerine yazma.
- **Neden:** Tüm dosya yeniden yazma işlemleri sırasında mevcut kalıplar (session caching, filtreler vb.) kayboluyor.

## Git Branch Stratejisi (3 Katmanlı)

| Branch | Amaç | Kural |
|--------|------|-------|
| `master` | Sadece release'ler | Doğrudan commit **yasak**; sadece develop'tan merge |
| `develop` | Günlük geliştirme | Varsayılan çalışma branch'i |
| `feature/*` | Yeni özellik | develop'tan dallan, develop'a merge et |
| `fix/*` | Bug düzeltme | develop'tan dallan, develop'a merge et |
| `hotfix/*` | Acil düzeltme | master'dan dallan, master + develop'a merge et |

### Günlük Git Akışı
Her görev/özellik/düzeltme tamamlandıktan ve build doğrulandıktan sonra:
1. `git add -A`
2. `git commit -m "<tip>: <kısa açıklama>"`
3. `git push origin develop`

**Commit tipleri:** `feat`, `fix`, `refactor`, `docs`, `chore`, `style`, `perf`
**Kural:** Release commit'leri hariç tag oluşturma. Tag sadece "release derle" sürecinde atılır.
**Kural:** `master` branch'ine doğrudan commit atma. Her zaman `develop` üzerinde çalış.

## Görev Sonrası Otomasyon (kritik — her görev tamamlandığında otomatik uygula)

Her özellik/güncelleme/düzeltme tamamlandıktan sonra aşağıdaki adımlar **hatırlatma beklemeden otomatik** uygulanır:

1. **Versiyon güncelle** — Semantic versioning'e göre (MAJOR/MINOR/PATCH) versiyon noktalarını senkronize et
2. **Dökümanları güncelle** — CHANGELOG.md, FEATURES.md, INSTALL.md, README.md (gerekli olanlar)
3. **Build doğrula** — `dotnet build` ile derleme hatası olmadığından emin ol
4. **Git gönder** — `git add -A` → `git commit` → `git push origin develop`

> **Not:** Bu adımlar kullanıcı hatırlatmadan otomatik yapılır. Versiyon bump seviyesi:
> - Yeni özellik → MINOR (1.8.0 → 1.9.0)
> - Bug fix / küçük düzeltme → PATCH (1.8.0 → 1.8.1)
> - Breaking change → MAJOR (1.8.0 → 2.0.0)

## Yanıt Formatı
1. Değişiklik özeti (1-2 cümle)
2. Sadece değişen kod bloğu
3. Dokümantasyon güncellemeleri
4. Onay noktası
