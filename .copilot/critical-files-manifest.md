# Kritik Dosya Manifesti — Koruma Kuralları

> **Bu dosya Copilot tarafından her kod değişikliğinde okunmalı ve doğrulanmalıdır.**
> Son güncelleme: 2026-04-07 — v0.94.0

## Amaç

Bu dosya, geçmişte regresyon yaşanan kritik dosyaları ve korunması **zorunlu** olan kod kalıplarını tanımlar.
Herhangi bir değişiklik öncesinde snapshot dosyaları ile karşılaştırma yapılmalıdır.

## Snapshot Dizini

```
.copilot/snapshots/v0.72.0/
├── CloudUploadOrchestrator.cs
├── GoogleDriveProvider.cs
├── BackupJobExecutor.cs
├── ICloudProvider.cs
└── ICloudUploadOrchestrator.cs
```

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

---

## Geçmiş Regresyon Kaydı

| Versiyon | Dosya | Kayıp | Neden |
|----------|-------|-------|-------|
| v0.71.0 | GoogleDriveProvider.cs | — | Files.EmptyTrash() kullanıldı (tüm çöpü siliyordu) |
| v0.48.0 | MainWindow.cs | PlanId propagasyonu | CloudUploadOrchestrator eventleri PlanId taşımıyordu |
| v0.48.0 | MainWindow.cs | _nextFireTimes | Dictionary toptan Clear() ile temizlendi |
