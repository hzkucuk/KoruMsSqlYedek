# Kritik Dosya Manifesti — Koruma Kuralları

> **Bu dosya Copilot tarafından her kod değişikliğinde okunmalı ve doğrulanmalıdır.**
> Son güncelleme: 2025-07-10 — v0.72.0-safe

## Amaç

Bu dosya, geçmişte regresyon yaşanan kritik dosyaları ve korunması **zorunlu** olan kod kalıplarını tanımlar.
Herhangi bir değişiklik öncesinde snapshot dosyaları ile karşılaştırma yapılmalıdır.

## Snapshot Dizini

```
.copilot/snapshots/v0.72.0/
├── MegaProvider.cs
├── CloudUploadOrchestrator.cs
├── GoogleDriveProvider.cs
├── BackupJobExecutor.cs
├── ICloudProvider.cs
└── ICloudUploadOrchestrator.cs
```

---

## 🔴 MegaProvider.cs — EN KRİTİK

**Dosya:** `KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs`
**Risk:** 🔴 Çok Yüksek — 2 kez regresyon yaşandı (v0.71.x → session caching kaybı)

### Korunması Zorunlu Kalıplar

#### 1. Session Caching Alanları (SİLİNMEMELİ)
```csharp
private static MegaApiClient _cachedClient;
private static string _cachedEmail;
private static DateTime _sessionLastUsedUtc;
private static readonly SemaphoreSlim _sessionSemaphore = new(1, 1);
private const int SessionExpiryMinutes = 15;
```

#### 2. GetOrCreateSessionAsync (SİLİNMEMELİ / DEĞİŞTİRİLMEMELİ)
- Email eşleşmesi kontrolü
- 15 dakika süre aşımı kontrolü
- Eski oturum kapatma + yeni oturum açma
- `_sessionLastUsedUtc` güncelleme

#### 3. InvalidateSessionInternalAsync (SİLİNMEMELİ)
- `LogoutSafeAsync` + `_cachedClient = null` + `_cachedEmail = null`

#### 4. Semaphore Pattern (TÜM PUBLIC METOTLARDA OLMALI)
Her public metot (`UploadAsync`, `DeleteAsync`, `EmptyTrashAsync`) şu kalıba uymalı:
```
await _sessionSemaphore.WaitAsync(ct);
try {
    var client = await GetOrCreateSessionAsync(config, ct);
    // ... iş mantığı ...
    _sessionLastUsedUtc = DateTime.UtcNow;
} catch (Exception) {
    await InvalidateSessionInternalAsync();
} finally {
    _sessionSemaphore.Release();
}
```

#### 5. TestConnectionAsync (FARKLI — ÖNBELLEKSİZ)
- Her zaman yeni `MegaApiClient()` kullanır (kimlik doğrulama testi)
- `_cachedClient` kullanmaz

#### 6. EmptyTrashAsync — IsOurBackupFile Filtresi (SİLİNMEMELİ)
- Yalnızca `.bak`/`.7z` uzantılı dosyalar
- `_Full_`, `_Differential_`, `_Incremental_`, `Files_` desenleri
- Kullanıcının kişisel dosyalarına DOKUNMAZ

### Doğrulama Kontrol Listesi
Herhangi bir MegaProvider değişikliğinden SONRA:
- [ ] `_cachedClient` statik alanı var mı?
- [ ] `_sessionSemaphore` statik alanı var mı?
- [ ] `GetOrCreateSessionAsync` metodu var mı?
- [ ] `InvalidateSessionInternalAsync` metodu var mı?
- [ ] `UploadAsync` semaphore + cache kullanıyor mu?
- [ ] `DeleteAsync` semaphore + cache kullanıyor mu?
- [ ] `EmptyTrashAsync` semaphore + cache + `IsOurBackupFile` filtresi var mı?
- [ ] `TestConnectionAsync` yeni client oluşturuyor mu? (önbellek KULLANMAMALI)

---

## 🟡 GoogleDriveProvider.cs

**Dosya:** `KoruMsSqlYedek.Engine/Cloud/GoogleDriveProvider.cs`
**Risk:** 🟡 Orta

### Korunması Zorunlu Kalıplar

#### EmptyTrashAsync — Klasör Kapsamlı Sorgu (SİLİNMEMELİ)
- `Files.EmptyTrash()` API **kullanılmaz** (tüm çöpü siler — tehlikeli!)
- `trashed=true and '{folderId}' in parents` sorgusu ile yalnızca kendi klasörümüzdeki dosyalar
- `FindFolderIdAsync` yardımcı metodu
- Her dosya tek tek `Files.Delete(fileId)` ile silinir

---

## 🟡 BackupJobExecutor.cs

**Dosya:** `KoruMsSqlYedek.Engine/Scheduling/BackupJobExecutor.cs`
**Risk:** 🟡 Orta

### Korunması Zorunlu Kalıplar

#### EmptyTrashIfNeededAsync Hook
- FileBackup-only yolunda çağrılmalı
- SQL+File combined yolunda çağrılmalı
- try/catch sarmalı — çöp boşaltma hatası backup'ı başarısız yapmamalı
- `CloudOrchestrator` null kontrolü

---

## 🟡 CloudUploadOrchestrator.cs

**Dosya:** `KoruMsSqlYedek.Engine/Cloud/CloudUploadOrchestrator.cs`
**Risk:** 🟡 Orta

### Korunması Zorunlu Kalıplar

#### EmptyTrashForAllAsync Filtreleme
- `t.IsEnabled && !t.PermanentDeleteFromTrash` filtresi
- `provider.SupportsTrash` kontrolü
- Her hedef için ayrı try/catch — bir hata diğerlerini durdurmamalı

---

## 🟢 Interface Dosyaları

### ICloudProvider.cs
- `bool SupportsTrash { get; }` — zorunlu
- `Task<int> EmptyTrashAsync(...)` — zorunlu
- Yeni metot eklenirse tüm implementasyonlar güncellenmeli

### ICloudUploadOrchestrator.cs
- `Task<int> EmptyTrashForAllAsync(...)` — zorunlu

---

## Snapshot Karşılaştırma Prosedürü

Herhangi bir kritik dosyada değişiklik yapıldığında:

1. **Değişiklik öncesi** — Snapshot dosyasını oku: `.copilot/snapshots/v0.72.0/{Dosya}.cs`
2. **Değişiklik sonrası** — Yukarıdaki kontrol listelerini doğrula
3. **Build** — Derleme hatası olmadığından emin ol
4. **Test** — `run_tests` ile regresyon testi yap
5. **Yeni snapshot** — Büyük değişiklik sonrası snapshot güncelle:
   ```
   Copy-Item "KoruMsSqlYedek.Engine/Cloud/MegaProvider.cs" ".copilot/snapshots/vX.Y.Z/MegaProvider.cs"
   ```

---

## Geçmiş Regresyon Kaydı

| Versiyon | Dosya | Kayıp | Neden |
|----------|-------|-------|-------|
| v0.71.x | MegaProvider.cs | Session caching (tüm static alanlar + metotlar) | Trash özelliği eklenirken dosya üzerine yazıldı |
| v0.71.0 | GoogleDriveProvider.cs | — | Files.EmptyTrash() kullanıldı (tüm çöpü siliyordu) |
| v0.48.0 | MainWindow.cs | PlanId propagasyonu | CloudUploadOrchestrator eventleri PlanId taşımıyordu |
| v0.48.0 | MainWindow.cs | _nextFireTimes | Dictionary toptan Clear() ile temizlendi |
