# 📐 Mimari Kararlar Günlüğü (ADR)

> Format: Her karar numaralı, tarihli ve gerekçeli tutulur.  
> Durum: ✅ Kabul Edildi | 🔄 Tartışılıyor | ❌ İptal Edildi

---

## ADR-001: Clean Architecture Katman Yapısı
- **Tarih:** 2024 (proje başlangıcı)
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** SQL yedekleme uygulaması; Windows Service + WinForms tray UI gereksinimi.
- **Karar:** 5 proje yapısı: `Core` (kontratlar) → `Engine` (iş mantığı) → `Win` (UI) / `Service` (host) / `Tests`
- **Gerekçe:** Katman bağımsızlığı, test edilebilirlik, UI ile servis arasında paylaşılan iş mantığı.
- **Sonuç:** Engine, hem Win hem Service tarafından kullanılabiliyor. Core, dış bağımlılık taşımıyor.

---

## ADR-002: Named Pipe IPC (Service ↔ Tray)
- **Tarih:** v0.46.0
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Windows Service (SYSTEM) ile kullanıcı oturumundaki tray uygulaması arasında iletişim gerekli.
- **Alternatifler:** TCP/HTTP loopback, WCF, Memory-mapped files, Named Pipes
- **Karar:** Named Pipe (`KoruMsSqlYedekPipe`) + JSON newline-delimited protokol
- **Gerekçe:**
  - Kernel seviyesi IPC, network stack gerektirmez
  - ACL ile güvenlik kontrolü (v0.54.0'da PipeSecurity eklendi)
  - Düşük latency, firewall sorunu yok
  - .NET native destek (`System.IO.Pipes`)
- **Risk:** SYSTEM ↔ user session arası ACL yapılandırması gerekli (v0.54.0'da çözüldü).

---

## ADR-003: Quartz.NET Zamanlama
- **Tarih:** v0.46.0
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Planlanmış yedekleme görevleri için cron-tabanlı zamanlama gerekli.
- **Alternatifler:** System.Timers.Timer, Hangfire, Windows Task Scheduler
- **Karar:** Quartz.NET (in-memory scheduler)
- **Gerekçe:**
  - Cron expression desteği (esnek zamanlama)
  - In-process çalışır, ek altyapı gerektirmez
  - Misfire handling (kaçırılmış görev yönetimi)
  - Eşzamanlı görev desteği (`DisallowConcurrentExecution` opsiyonel)
- **Sonuç:** v0.46.0'da eşzamanlı yedekleme desteği sorunsuz eklendi.

---

## ADR-004: Plan Bazlı Eşzamanlı Yedekleme
- **Tarih:** v0.46.0
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Birden fazla yedekleme planı aynı anda çalışabilmeli.
- **Karar:** Her plan kendi `BackupJobExecutor` instance'ı ile çalışır; PlanId ile izolasyon sağlanır.
- **Gerekçe:** Farklı sunuculardaki veritabanları paralel yedeklenebilmeli.
- **Kısıtlar:**
  - Tüm event'ler PlanId taşımalı (regresyon direktifi #1)
  - Dictionary'ler toptan temizlenmemeli (regresyon direktifi #2)
  - Log buffer'ları plan bazlı izole edilmeli
- **Sonuç:** v0.48.0'da PlanId izolasyon sorunu düzeltildi; regresyon direktifleri eklendi.

---

## ADR-005: PlanProgressTracker Ağırlık Modeli
- **Tarih:** v0.50.0 (genişletme: v0.53.0)
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Yedekleme ilerlemesini tek bir progress bar'da gösterme ihtiyacı.
- **Karar:** Ağırlıklı ilerleme modeli:
  - SQL-only: SQL %100
  - SQL + File: SQL %80, File %20
  - File-only: File %100
  - VSS varsa: Snapshot %20, Copy %50, Cleanup %30
- **Gerekçe:** SQL yedekleme genellikle daha uzun sürer; dosya yedekleme tamamlayıcı.
- **Risk:** Ağırlıklar tahmini; gerçek süre dağılımına göre ayarlanabilir.

---

## ADR-006: AlphaVSS ile Açık Dosya Yedekleme
- **Tarih:** v0.47.0
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Kullanıcı dosya yedeklemesi sırasında kilitli/açık dosyalar kopyalanamıyor.
- **Alternatifler:** Robocopy /B, dosya kilidi bekleme, VSS
- **Karar:** AlphaVSS 1.4.0 ile Volume Shadow Copy Service entegrasyonu
- **Gerekçe:** Windows VSS, uygulamayı durdurmadan tutarlı dosya anlık görüntüsü sağlar.
- **Risk:** SYSTEM yetkisi gerekli; bazı uygulamalar VSS writer sağlamayabilir.

---

## ADR-007: İptal/Başarısızlık Ara Dosya Temizliği
- **Tarih:** v0.55.0
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Yedekleme iptal edildiğinde veya başarısız olduğunda .bak, .7z ve geçici klasörler diskte kalıyordu.
- **Karar:** `cleanupPaths` listesi + per-DB `cleanupSnapshot` pattern + `CleanupOnFailure` helper
- **Gerekçe:**
  - Disk alanı israfını önle
  - Kullanıcı müdahalesi gerektirme
  - Başarılı adımların dosyaları listeden çıkarılır → sadece başarısız/iptal edilen temizlenir
- **Kısıt:** `OperationCanceledException` inner catch'lerde yeniden fırlatılmalı (6 nokta düzeltildi).

---

## ADR-008: Autofac IoC Container
- **Tarih:** Proje başlangıcı
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Bağımlılık enjeksiyonu altyapısı gerekli.
- **Alternatifler:** Microsoft.Extensions.DependencyInjection, Autofac, Ninject
- **Karar:** Autofac (hem Win hem Service projelerinde ayrı IoC modülleri)
- **Gerekçe:** Zengin özellik seti (module, decorator, interception), WinForms uyumu.

---

## ADR-009: Serilog Yapılandırılmış Loglama
- **Tarih:** Proje başlangıcı
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Hem dosya hem konsol loglaması gerekli.
- **Karar:** Serilog + File sink
- **Gerekçe:** Yapılandırılmış loglama, seviye filtreleme, kolay sink ekleme.

---

## ADR-010: 3 Katmanlı Branch Stratejisi
- **Tarih:** 2025-07-19 (v0.56.0)
- **Durum:** ✅ Kabul Edildi
- **Bağlam:** Proje karmaşıklığı arttı; master üzerinde doğrudan geliştirme riski yüksek.
- **Karar:**
  - `master` → Sadece release'ler (tag'li)
  - `develop` → Günlük geliştirme (varsayılan çalışma branch'i)
  - `feature/*`, `fix/*`, `hotfix/*` → Kısa ömürlü dallar
- **Gerekçe:**
  - master her zaman kararlı ve dağıtılabilir
  - develop üzerinde entegrasyon testi
  - Feature branch'leri izole geliştirme sağlar
- **Kural:** master'a sadece develop'tan merge yapılır (release sırasında).

---

## Karar Ekleme Şablonu

```markdown
## ADR-XXX: [Başlık]
- **Tarih:** YYYY-MM-DD
- **Durum:** ✅ / 🔄 / ❌
- **Bağlam:** [Problem/ihtiyaç]
- **Alternatifler:** [Değerlendirilen seçenekler]
- **Karar:** [Seçilen çözüm]
- **Gerekçe:** [Neden bu seçenek]
- **Risk/Kısıt:** [Bilinen riskler]
- **Sonuç:** [Gözlemlenen etki]
```
