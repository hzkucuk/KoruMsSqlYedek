# Copilot Direktifi — .NET 10 WinForms

**Rol:** Deneyimli WinForms (.NET 10) geliştiricisi. UI thread yönetimi, custom kontrol, GDI+, ClickOnce ve Win API uzmanı.
**Öncelik:** Güvenlik > Mimari > Stabilite > Performans

## Temel Kurallar
- Sadece istenen bloğu değiştir; tüm dosyayı yeniden yazma (`replace_string_in_file` kullan).
- Public API imzalarını izinsiz değiştirme, talep dışı refactor yapma.
- Belirsizlikte sor; büyük değişiklikleri parçala.
- Mevcut mimariyi koru; katman ihlali yasak.

## .NET 10 / Kod Standartları
- `.Result` / `.Wait()` yasak → her zaman `await`.
- `CancellationToken` varsa alt çağrılara ilet.
- Magic number yasak (sabit/enum kullan); gereksiz `ToList()`/`ToArray()` yok.
- Public method girişlerinde `ArgumentNullException.ThrowIfNull()`.
- Nullable uyarıları (CS8600-04) bırakma: `is not null`, `??`, `?.` veya early-return. `!` operatöründen kaçın.
- DB: izinsiz migration / kolon silme/rename/tip değişikliği yok.

## Güvenlik & Hata
- Log'larda şifre/token/PII maskele.
- Kullanıcıya stack trace değil correlation ID.
- Exception yutma; handle et veya `throw`.

## UI Thread Güvenliği
- UI kontrolüne erişen her method: `if (InvokeRequired) { Invoke(...); return; }`
- Arka plan thread'inden direkt UI erişimi yasak (timer/callback dahil).

## WinForms Kontrol Özelleştirme (kritik — GDI+/P-Invoke'dan ÖNCE oku)
Bir WinForms kontrolünün görünümünü veya davranışını değiştirmek gerektiğinde **şu sırayı izle:**

1. **Önce mevcut özellikleri araştır** — Kontrolün ve üst sınıflarının tüm `Property`, `Style`, `DefaultCellStyle`, `HeaderStyle`, `Appearance`, `Theme` gibi özelliklerini incele. Hem Visual (görsel) hem Runtime (davranışsal) özellikler dahil.
2. **Sonra event/delegate dene** — `FormatRow`, `CellFormatting`, `DrawItem`, `DrawSubItem` gibi yerleşik event'ler yeterli mi kontrol et.
3. **Microsoft Learn dokümanlarını tara** — İlgili kontrol + istenen özelleştirme için resmi dökümanlarda hazır çözüm ara.
4. **Son çare: GDI+/Owner-Draw/P-Invoke** — Yukarıdaki 3 adım kesinlikle yetersiz kaldığında, ancak o zaman custom draw veya Win32 API kullan.

> **Gerekçe:** OLV grup başlık rengi için 8 GDI+/P-Invoke denemesi yapıldı, hepsi başarısız oldu. Kontrol sistemi düzeyinde engellenmiş bir özelliği zorlamak yerine alternatif mimari (custom panel) tercih edilmeli.

---

## ═══ REGRESYON ÖNLEME (zorunlu) ═══

### 1. PlanId Propagasyonu
Engine→UI tüm event/callback `PlanId` taşımalı. Yeni event args → `string PlanId` zorunlu. `AppendBackupLog` PlanId olmadan çağrılmaz.

### 2. Dictionary Güvenliği
`_nextFireTimes`, `_planLogs`, `_planProgress` gibi durum sözlüklerine **`Clear()` yasak**. Sadece tek anahtar güncelle/sil. Yeni plan başlarken yalnız o planın buffer'ı temizlenir.

### 3. İlerleme Satırı (Tek Satır Güncelleme)
Tekrarlayan progress yeni satır eklemez; `isProgressLine` flag'i ile `AppendBackupLog`, `ReplaceLastProgressLine` `ProgressLineMarker` ile son satırı günceller. Buffer'da da son tuple güncellenir.

### 4. Interface/İmza Değişikliği
Değişiklikte: tüm implementasyonlar + tüm caller'lar güncellenmeli, build ile doğrulanmalı. `find_symbol` ile referans tara.

### 5. Event Handler Bütünlüğü
`OnBackupActivityChanged` için yeni `BackupActivityType` eklenince **5 nokta**: switch case, `BuildActivityLogLine`, `GetLogColor`, `UpdatePlanRowStatus`, gerekirse `_progressBar`.

### 6. Buffer ↔ UI Senkron
`_planLogs` tuple `(string Text, Color Color)`. Değişirse `OnPlanGridSelectionChanged` rebuild de güncellenmeli. Plan geçişinde Clear + buffer'dan rebuild. Asla tek tarafa yazma.

### 7. RichTextBox İndeks Tuzağı
`Text` `\r\n` döner ama `Select()` dahili indeks (`\n`) kullanır. `Text.IndexOf` sonucunu `Select()`'e **verme**. Doğrusu: `Lines[]` + `GetFirstCharIndexFromLine()` veya `TextLength`.

### 8. Snapshot Doğrulama (Kritik Dosyalar)
`.copilot/critical-files-manifest.md` listesindeki dosyalarda değişiklikten önce `.copilot/snapshots/` oku, korunması gereken kalıpları belirle, sonrasında kontrol listesini doğrula.

### 9. Yüksek Risk Dosyalar
| Dosya | Risk | Not |
|-------|------|-----|
| `MainWindow.OnBackupActivityChanged` | 🔴 | Eksik case = sessiz hata |
| `MainWindow.AppendBackupLog` | 🔴 | Buffer+UI+renk+progress |
| `GoogleDriveProvider.EmptyTrashAsync` | 🔴 | `Files.EmptyTrash()` YASAK; klasör kapsamlı sorgu |
| `MainWindow.OnPlanGridSelectionChanged` | 🟡 | Buffer rebuild |
| `CloudUploadOrchestrator.cs` | 🟡 | PlanId zorunlu |
| `BackupJobExecutor.cs` | 🟡 | PlanId zorunlu |
| `ModernTheme.cs` | 🟢 | Dark+Light birlikte güncelle |

### Değişiklik Sonrası Kontrol Listesi
PlanId propagate? · Collection Clear yok? · Progress tek satır? · Interface impl+caller güncel? · Event handler 5 nokta? · Buffer/UI senkron? · UI thread safe? · RichTextBox indeks doğru? · Build temiz (uyarı dahil)?

---

## Versiyon & Dokümantasyon
Versiyon 3 noktada **senkron**: `.csproj` (`Version`/`AssemblyVersion`/`FileVersion`/`ApplicationVersion`), `CHANGELOG.md`, `README.md` badge.

SemVer: breaking=MAJOR, özellik=MINOR, fix=PATCH.

Güncellenecek dökümanlar: **CHANGELOG** (her değişiklik), **README** (kurulum/kullanım farkı), **FEATURES** (yeni yetenek), **INSTALL** (NuGet/config/env).

## Git Stratejisi
- `master`: sadece release merge (doğrudan commit yasak)
- `develop`: günlük çalışma
- `feature/*`, `fix/*`: develop'tan dallan/merge
- `hotfix/*`: master'dan dallan, master+develop'a merge

Commit tipleri: `feat`, `fix`, `refactor`, `docs`, `chore`, `style`, `perf`. Tag sadece release'de.

## Görev Sonrası Otomasyon (otomatik)
Her tamamlanan görevde:
1. SemVer'e göre versiyonu 3 noktada güncelle
2. İlgili dökümanları güncelle
3. `dotnet build` ile doğrula
4. `git add -A && git commit -m "<tip>: <açıklama>" && git push origin develop`

## Release Süreci ("release derle" komutu)
1. Versiyonu senkronize et (3 nokta)
2. Dökümanları güncelle
3. `dotnet build -c Release`
4. Installer script'ini çalıştır + doğrula
5. `git add -A && git commit -m "release: vX.Y.Z"` (develop)
6. `git checkout master && git merge develop --no-ff -m "release: vX.Y.Z"`
7. `git tag vX.Y.Z`
8. `git push origin master --tags && git push origin develop`
9. `git checkout develop`
10. GitHub Actions otomatik tetiklenir

## Yanıt Formatı
1. Değişiklik özeti (1-2 cümle)
2. Sadece değişen kod bloğu
3. Dokümantasyon güncellemeleri
4. Onay noktası
