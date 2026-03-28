# Copilot Direktifi — .NET 10

**Rol:** .NET WinForms ve MSSQL uzmanı. Windows Forms uygulamaları, ADO.NET, Entity Framework, SQL Server sorguları, stored procedure'ler ve WinForms UI tasarımı konularında derin bilgiye sahiptir.

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
- `CancellationToken` varsa tüm alt çağrılara ilet.
- Gereksiz `ToList()` / `ToArray()` kullanma.
- Magic number yasak; sabit veya enum kullan.
- Nullable Reference Types: her public method girişinde `ArgumentNullException.ThrowIfNull()` ekle.

## Veritabanı
Açık talimat olmadan: EF Migration oluşturma, kolon silme/rename/tip değiştirme.

## Güvenlik & Hata Yönetimi
- Log'larda şifre/token/PII maskele.
- Kullanıcıya stack trace gösterme; correlation ID döndür.
- Exception yutma; handle et veya `throw` ile ilet.

## Otodökümantasyon (otomatik — hatırlatma bekleme)
Her değişiklik sonrası:
- **CHANGELOG.md:** `[vX.Y.Z] — YYYY-MM-DD — [Özet] — [Etkilenen dosya]`
- **FEATURES.md:** ⚠️ **MUTLAKA** güncellenmeli — aşağıdaki "Plan Dosyası" bölümüne bak.
- **INSTALL.md:** NuGet / config / env değişikliğinde senkronize et.
- Semantic versioning: breaking=MAJOR, yeni özellik=MINOR, düzeltme=PATCH.

## Plan Dosyası — FEATURES.md (zorunlu — her faz/adım sonunda güncelle)
`FEATURES.md` projenin yol haritası ve ilerleme takip dosyasıdır. **Her kod değişikliği sonrası mutlaka güncellenmelidir.**
- **Faz tamamlandığında:** "Faz Durumu" tablosunda ilgili fazın durumunu `✅ Tamamlandı` olarak işaretle.
- **Adım tamamlandığında:** İlgili faz bölümündeki checkbox'ı `[x]` olarak işaretle.
- **Yeni özellik/yetenek eklendiyse:** İlgili faz altına yeni satır ekle.
- **Faz devam ediyorsa:** Durum `🔄 Devam Ediyor` olarak güncelle.
- **Hiçbir faz/adım tamamlanmadan** pull request veya commit yapılmamalıdır — FEATURES.md güncel değilse uyar.
- Güncelleme sırası: Kod değişikliği → Build doğrulama → **FEATURES.md güncelle** → CHANGELOG.md → Commit.

## Versiyon Yönetimi (kritik — her release'de uygulanmalı)
Versiyon **3 dosyada** senkron tutulmalı:
1. **`Properties\AssemblyInfo.cs`** → `AssemblyVersion` + `AssemblyFileVersion` (tek kaynak)
2. **`.csproj`** → `<ApplicationVersion>` (ClickOnce)
3. **`CHANGELOG.md`** → `## [X.Y.Z] - YYYY-MM-DD` girdisi
- Versiyon değişikliğinde **üçü birlikte** güncellenmelidir.
- Release için `Deployment\Build-Release.ps1` scripti kullanılır.
- ZIP arşivleri `releases/` klasörüne oluşturulur (Git dışı).

## Git İş Akışı & Commit Kuralları
- **Commit mesajı formatı:** `[tip]: kısa açıklama` (örn: `fix: statik alan sırası düzeltildi`)
  - Tipler: `feat`, `fix`, `refactor`, `docs`, `chore`, `style`, `test`
- Her değişiklik sonrası **commit öncesi kontrol listesi:**
  1. Proje hatasız derleniyor mu?
  2. CHANGELOG.md güncellendi mi?
  3. Versiyon numarası senkron mu (AssemblyInfo, .csproj, CHANGELOG)?
- **Branch stratejisi:** `main` → kararlı, `dev` → geliştirme, `feature/*` → yeni özellikler, `fix/*` → hata düzeltmeleri.
- **Push öncesi:** `git pull --rebase` ile güncel kalınmalı.
- **Tag:** Her release'de `vX.Y.Z` formatında tag oluşturulmalı: `git tag -a vX.Y.Z -m "Release X.Y.Z"`.

## README.md Güncelleme Kuralları
- **Her önemli değişiklikte** README.md güncellenmeli:
  - Yeni özellik eklendi → "Özellikler" bölümüne ekle.
  - Bağımlılık değişti (NuGet, runtime) → "Gereksinimler" bölümünü güncelle.
  - Kurulum/yapılandırma değişti → "Kurulum" bölümünü güncelle.
  - API/kullanım değişti → "Kullanım" bölümünü güncelle.
- README.md yapısı: `Proje Adı` → `Açıklama` → `Özellikler` → `Gereksinimler` → `Kurulum` → `Kullanım` → `Lisans`.
- Ekran görüntüleri `docs/images/` klasöründe tutulmalı.

## Yanıt Formatı
1. Değişiklik özeti (1-2 cümle)
2. Sadece değişen kod bloğu
3. Dokümantasyon güncellemeleri
4. Onay noktası

---

# Proje: MikroSqlDbYedek — SQL Server Yedekleme & Bulut Senkronizasyon Sistemi

## Proje Mimarisi
- **MikroSqlDbYedek.Core** — Paylaşılan modeller, arayüzler, yardımcı sınıflar (Class Library)
- **MikroSqlDbYedek.Engine** — İş mantığı motoru: yedekleme, sıkıştırma, bulut upload, zamanlama, bildirim (Class Library)
- **MikroSqlDbYedek.Win** — System Tray WinForms UI uygulaması
- **MikroSqlDbYedek.Service** — Windows Service (Topshelf ile host edilen arka plan motoru)
- **MikroSqlDbYedek.Tests** — Unit test projesi
- TFM: .NET Framework 4.8 (tüm projeler)

## Teknoloji Stack Kuralları
- **Zamanlama:** Quartz.NET 3.x — cron ifadeleri ile job scheduling
- **Logging:** Serilog — yapısal loglama, rolling file sink. Log'larda şifre/connection string ASLA yazılmaz.
- **JSON:** Newtonsoft.Json — plan şablonları ve konfigürasyon
- **SQL Backup:** Microsoft.SqlServer.SqlManagementObjects (SMO) — BACKUP DATABASE / RESTORE komutları
- **Sıkıştırma:** SevenZipSharp (Squid-Box.SevenZipSharp) — LZMA2 algoritması, şifreli arşiv desteği
- **Bulut:** Provider pattern ile soyutlanmış:
  - Google Drive (bireysel + Workspace): Google.Apis.Drive.v3 — silinen dosyalar çöp kutusundan temizlenmeli
  - OneDrive (bireysel + kurumsal): Microsoft.Graph — silinen dosyalar çöp kutusundan temizlenmeli
  - FTP/SFTP: FluentFTP + SSH.NET
  - Yerel/UNC: System.IO (ağ paylaşımı)
- **Windows Service:** Topshelf — kolay install/uninstall/debug
- **E-posta:** MailKit — SMTP bildirim
- **IoC:** Autofac — dependency injection (Core ve Engine katmanlarında)
- **Dosya Yedekleme:** AlphaVSS — Volume Shadow Copy ile açık/kilitli dosya desteği (Outlook PST/OST vb.)
- **Çoklu Dil:** Resx tabanlı lokalizasyon (tr-TR, en-US varsayılan)

## Yedekleme Kuralları
- **Strateji türleri:** Full, Full+Differential, Full+Differential+Incremental
- **Zincir bütünlüğü:** Differential/Incremental yedek almadan önce geçerli Full yedek varlığı kontrol edilmeli.
- **Otomatik Full yükseltme:** Differential zincir `autoPromoteToFullAfter` değerini aşarsa otomatik Full tetiklenmeli.
- **RESTORE VERIFYONLY:** Her yedek sonrası isteğe bağlı doğrulama. Başarısızlıkta bildirim gönderilmeli.
- **Restore öncesi güvenlik:** Restore işlemi öncesi hedef DB'nin mevcut hali otomatik yedeklenmeli.

## Dosya Yedekleme Kuralları
- SQL yedeklemeye ek olarak, dosya/klasör yedekleme desteklenir.
- **VSS (Volume Shadow Copy):** Açık/kilitli dosyalar (Outlook PST, OST, SQL MDF/LDF, Excel vb.) VSS snapshot üzerinden yedeklenir.
- **AlphaVSS** kütüphanesi ile VSS snapshot oluşturulur, dosya kopyalandıktan sonra snapshot serbest bırakılır.
- VSS başarısız olursa (izin, servis kapalı), normal kopyalama denenecek ve uyarı log'a yazılacak.
- Kaynak tanımları: dizin yolu, include pattern (`*.pst;*.ost;*.docx`), exclude pattern (`*.tmp;~*`), recursive flag.
- Hedef: Plan'daki `localPath` altında `Files\{SourceName}\` dizinine kopyalanır.
- Dosya yedekleri de sıkıştırma ve bulut upload pipeline'ından geçer.
- Desteklenen özel senaryolar: Outlook PST/OST, Thunderbird profilleri, açık Office dosyaları.
- Dosya yedekleri retention politikasına tabidir (SQL yedekler gibi).
- Her plan hem SQL hem dosya yedekleme veya sadece birini içerebilir.

## JSON Plan Şablonu Kuralları
- Planlar `%APPDATA%\MikroSqlDbYedek\Plans\` altında `{planId}.json` olarak saklanır.
- `planId` → GUID, otomatik atanır.
- `compression.archivePassword` → DPAPI + Base64 ile geri dönüşümlü encode. Düz metin ASLA saklanmaz.
- Plan şeması değişikliğinde geriye uyumluluk sağlanmalı (yeni alan ekleme → varsayılan değer ile).
- Cron ifadeleri Quartz.NET formatında: `0 0 2 ? * SUN` (Pazar 02:00)

## Sıkıştırma Kuralları
- Varsayılan algoritma: LZMA2 (en iyi oran)
- Şifre her plan için bağımsız belirlenir (`compression.archivePassword`)
- Sıkıştırma sırasında ilerleme yüzdesi UI'ye raporlanmalı (IProgress<int>)
- Arşiv formatı: .7z

## Bulut Upload Kuralları
- Provider pattern: `ICloudProvider` arayüzü → GoogleDrive, OneDrive, FTP/SFTP, UNC Path
- **Google Drive:** OAuth2 ile kimlik doğrulama. Bireysel ve Google Workspace desteklenir. Upload sonrası eski dosyalar silindiğinde çöp kutusundan da kalıcı temizleme yapılmalı (`files.emptyTrash` veya `files.delete`).
- **OneDrive:** Microsoft Graph API ile erişim. Bireysel (MSA) ve kurumsal (Entra ID) hesaplar desteklenir. Silinen dosyalar çöp kutusundan da kalıcı temizlenmeli (`permanentDelete`).
- **FTP/SFTP:** FluentFTP (FTP/FTPS) + SSH.NET (SFTP) ile bağlantı.
- **Yerel/UNC:** Ağ paylaşımı desteği (kimlik bilgisi ile erişim isteğe bağlı).
- Retry politikası: 3 deneme, exponential backoff (2s → 4s → 8s)
- Bandwidth throttling desteği (isteğe bağlı, plan bazında)
- Upload sonrası checksum doğrulama

## Retention (Saklama) Kuralları
- Her plan için bağımsız retention politikası: `keepLastN` veya `deleteOlderThanDays`
- Retention temizliği yedek işlemi sonrası tetiklenir
- Silinen dosyalar log'a kaydedilir

## UI Kuralları (Tray Uygulaması)
- Uygulama System Tray'de çalışır (NotifyIcon)
- Tek instance (Mutex ile)
- Tray sağ tık menüsü: Dashboard Aç, Planlar, Log Görüntüle, Ayarlar, Çıkış
- Balloon tip ile anlık bildirim (yedek başarılı/başarısız)
- Plan ekleme/düzenleme/silme UI'den yapılır, JSON dosyasına serialize edilir

## Windows Service Kuralları
- Service adı: `MikroSqlDbYedekService`
- Topshelf ile host edilir (debug modda konsol, production'da service)
- Quartz.NET scheduler service start'ta başlatılır, stop'ta graceful shutdown
- Service ↔ Tray UI iletişimi: Named Pipes veya dosya sistemi sinyalleri

## Log Kuralları
- Log dosyaları: `%APPDATA%\MikroSqlDbYedek\Logs\`
- Rolling file: günlük dosya, 30 gün retention
- Log seviyeleri: Information (normal), Warning (yeniden deneme), Error (başarısızlık)
- Her yedek operasyonu bir `correlationId` ile izlenir
- Log'da ASLA: connection string, şifre, DPAPI verisi

## Bildirim Kuralları
- Bildirim türleri: E-posta (SMTP), Toast notification (Windows), Balloon tip (tray)
- Her plan için bağımsız bildirim ayarı: onSuccess, onFailure
- Bildirim başarısızlığı backup başarısını etkilemez (fire-and-forget değil ama hata yutulmaz, log'a yazılır)

## Inno Setup Dağıtım Kuralları
- Setup dosyası: `Deployment\InnoSetup\MikroSqlDbYedek.iss`
- Kurulum sırasında: Tray uygulaması + Windows Service birlikte kurulur
- Service otomatik start olarak kaydedilir
- Güncelleme sırasında mevcut planlar ve loglar korunur
- Minimum gereksinim: Windows 10+, .NET Framework 4.8, SQL Server 2016+

## Dosya & Klasör Konvansiyonları
- Uygulama verileri: `%APPDATA%\MikroSqlDbYedek\`
  - `Plans\` — JSON plan dosyaları
  - `Logs\` — Serilog log dosyaları
  - `Config\` — Genel ayarlar (appsettings.json)
- Yedek dosyaları: Plan'daki `localPath` ile belirlenir (varsayılan: `D:\Backups\MikroSqlDbYedek\`)

## Namespace Konvansiyonu
- `MikroSqlDbYedek.Core.Models` — Veri modelleri
- `MikroSqlDbYedek.Core.Interfaces` — Servis arayüzleri
- `MikroSqlDbYedek.Core.Helpers` — Yardımcı sınıflar
- `MikroSqlDbYedek.Engine.Backup` — Yedekleme servisleri
- `MikroSqlDbYedek.Engine.Cloud` — Bulut provider'ları
- `MikroSqlDbYedek.Engine.Compression` — Sıkıştırma servisleri
- `MikroSqlDbYedek.Engine.Scheduling` — Zamanlama servisleri
- `MikroSqlDbYedek.Engine.Notification` — Bildirim servisleri
- `MikroSqlDbYedek.Engine.Retention` — Saklama politikası servisleri
- `MikroSqlDbYedek.Engine.FileBackup` — Dosya yedekleme ve VSS servisleri

## SQL Server Kimlik Doğrulama
- Hem Windows Authentication hem SQL Authentication desteklenir.
- Her plan kendi bağlantı bilgilerini saklar (`sqlConnection` nesnesi).
- SQL Authentication şifreleri DPAPI + Base64 ile geri dönüşümlü encode edilir.
- Tek plan birden fazla SQL Server instance'ından DB seçebilir.

## Çoklu Dil Desteği
- Resx tabanlı lokalizasyon kullanılır.
- Varsayılan diller: Türkçe (tr-TR) ve İngilizce (en-US).
- Tüm UI string'leri `Resources.resx` / `Resources.tr-TR.resx` dosyalarından okunur.
- Dil değişikliği uygulama ayarlarından yapılır, restart gerektirir.
