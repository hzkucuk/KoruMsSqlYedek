# 🗺️ Yol Haritası (Roadmap)

> Son güncelleme: 2025-07-19 — v0.55.0  
> Öncelik: 🔴 Kritik | 🟠 Yüksek | 🟡 Orta | 🟢 Düşük  
> Durum: ⬜ Planlandı | 🔄 Devam Ediyor | ✅ Tamamlandı

---

## Kısa Vade (Sonraki 1-2 Sprint)

### 🔴 Kritik

| # | İş Kalemi | Modül | Neden |
|---|-----------|-------|-------|
| K1 | Named Pipe üretim testi (SYSTEM service ↔ user tray) | Service + Win | v0.54.0 ACL fix'i henüz gerçek ortamda test edilmedi |
| K2 | Cancel/Failure cleanup üretim testi | Engine | v0.55.0'da eklendi, edge case'ler (disk dolu, erişim engeli) test edilmeli |
| K3 | RestoreDialog tamamlama ve test | Win | Temel yapı var ama eksik, kullanıcı senaryoları tamamlanmalı |

### 🟠 Yüksek

| # | İş Kalemi | Modül | Neden |
|---|-----------|-------|-------|
| Y1 | Local-mode SQL ilerleme takibi (bulut olmadan) | Win + Engine | Bulut yükleme yokken SQL adımları ara ilerleme göstermiyor |
| Y2 | VSS test kapsamı genişletme | Tests | VssSnapshotService için unit test yok |
| Y3 | Named Pipe IPC test kapsamı | Tests | ServicePipeServer/Client için unit test yok |
| Y4 | BackupJobExecutor cancel path test | Tests | v0.55.0 cleanup logic'i için dedicated testler eksik |

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

1. **K1 — Named Pipe üretim testi**
   - Windows Service'i SYSTEM olarak kur (`sc create`)
   - Tray uygulamasını kullanıcı oturumundan başlat
   - Bağlantı, komut gönderimi, event akışı doğrula

2. **K3 — RestoreDialog tamamlama**
   - Mevcut durumu incele, eksikleri belirle
   - Restore akışını uçtan uca test et

3. **Y1 — Local-mode SQL ilerleme**
   - Bulut hedefi olmayan planlarda progress bar davranışını iyileştir

4. **Y2-Y4 — Test kapsamı genişletme**
   - VSS, IPC, Cancel path testleri yaz

> **Not:** Öncelik sırası, kullanıcı geri bildirimine göre güncellenecektir.
