# 🗺️ Yol Haritası (Roadmap)

> Son güncelleme: 2025-07-20 — v0.57.0  
> Öncelik: 🔴 Kritik | 🟠 Yüksek | 🟡 Orta | 🟢 Düşük  
> Durum: ⬜ Planlandı | 🔄 Devam Ediyor | ✅ Tamamlandı

---

## Kısa Vade (Sonraki 1-2 Sprint)

### 🔴 Kritik

| # | İş Kalemi | Modül | Neden |
|---|-----------|-------|-------|
| K1 | ✅ Named Pipe IPC protokol testleri (18+20 birim test) | Service + Win + Tests | v0.57.0'da tamamlandı — PipeProtocol + CancellationRegistry testleri |
| K2 | ✅ Cancel/Failure cleanup testleri (8 birim test) | Engine + Tests | v0.57.0'da tamamlandı — SQL/sıkıştırma/bulut iptal propagasyonu |
| K3 | ✅ RestoreDialog tamamlama (.7z, lokalizasyon, iptal UX) | Win | v0.57.0'da tamamlandı — .7z desteği, 10 kaynak anahtarı, iptal UX |

### 🟠 Yüksek

| # | İş Kalemi | Modül | Neden |
|---|-----------|-------|-------|
| Y1 | Local-mode SQL ilerleme takibi (bulut olmadan) | Win + Engine | Bulut yükleme yokken SQL adımları ara ilerleme göstermiyor |
| Y2 | VSS test kapsamı genişletme | Tests | VssSnapshotService için unit test yok |
| Y3 | ✅ Named Pipe IPC test kapsamı (38 test) | Tests | v0.57.0'da tamamlandı — PipeProtocolTests + CancellationRegistryTests |
| Y4 | ✅ BackupJobExecutor cancel path test (8 test) | Tests | v0.57.0'da tamamlandı — iptal/hata/başarı akış testleri |

---

## Orta Vade (Sonraki 3-5 Sprint)

### 🟡 Orta

| # | İş Kalemi | Modül | Neden |
|---|-----------|-------|-------|
| O1 | E-posta bildirim şablonları (HTML) | Engine | Şu an düz metin, profesyonel şablon gerekli |
| O2 | Raporlama detaylandırma (grafik/özet) | Engine | Temel rapor var, detaylı istatistik eksik |
| O3 | Retention politika detayları (grandfather-father-son) | Engine | Şu an basit sayısal, gelişmiş politika eklenebilir |
| O4 | MainWindow sorumluluk ayrıştırma | Win | 4 sorumluluk (buffer+UI+renk+ilerleme) tek metotta, kırılganlık riski |
| O5 | Stres testi (büyük DB, çok plan, eşzamanlı) | Tests | Eşzamanlı yedekleme (v0.46.0) stres altında test edilmeli |
| O6 | Progress Tracker unit testleri | Tests | PlanProgressTracker weight model'i için dedicated test eksik |
| O7 | Otomatik güncelleme mekanizması | Win | Kullanıcıya yeni sürüm bildirimi / otomatik güncelleme |

---

## Uzun Vade (Gelecek Çeyrek)

### 🟢 Düşük

| # | İş Kalemi | Modül | Neden |
|---|-----------|-------|-------|
| U1 | Azure Blob Storage provider | Engine | Yeni bulut hedefi |
| U2 | AWS S3 provider | Engine | Yeni bulut hedefi |
| U3 | Web dashboard (opsiyonel) | Yeni Proje | Uzaktan izleme arayüzü |
| U4 | Çoklu dil desteği (i18n) tamamlama | Win | Resources.resx altyapısı var, tüm string'ler taşınmalı |
| U5 | PowerShell script desteği (pre/post backup) | Engine | Kullanıcı tanımlı script'ler |
| U6 | Yedekleme doğrulama (otomatik restore + checksum) | Engine | Yedek bütünlüğü otomatik doğrulama |

---

## Teknik Borç

| # | Açıklama | Etki | Öncelik |
|---|----------|------|---------|
| TB1 | MainWindow.OnBackupActivityChanged — merkezi switch, her yeni event tipi 5 noktada güncellenmeli | Sessiz hata riski | 🟠 |
| TB2 | AppendBackupLog — Buffer + UI + Renk + İlerleme tek metotta | Kırılganlık | 🟡 |
| TB3 | RichTextBox indeks uyumsuzluğu (Text \r\n vs Select \n) | Bilinen pitfall, dikkat gerekli | 🟡 |
| TB4 | Test coverage: VSS, IPC, Cancel Cleanup, Progress Tracker, RestoreDialog | Güvenlik açığı | 🟠 |
| TB5 | Serilog yapılandırılmış loglama standardizasyonu | Tutarlılık | 🟢 |

---

## Yarın (Sonraki Oturum) Planı

### Önerilen Sıralama

1. **Y1 — Local-mode SQL ilerleme**
   - Bulut hedefi olmayan planlarda progress bar davranışını iyileştir

2. **Y2 — VSS test kapsamı genişletme**
   - VssSnapshotService için unit test yaz

3. **O1-O3 — Orta vade iş kalemleri**
   - E-posta şablonları, raporlama detayı, gelişmiş retention politikası

> **Not:** K1/K2/K3 ve Y3/Y4 v0.57.0'da tamamlandı. Öncelik sırası, kullanıcı geri bildirimine göre güncellenecektir.
