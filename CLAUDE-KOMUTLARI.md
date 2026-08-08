# Claude Code Komut Rehberi

> Claude Code'da `/` yazınca çıkan listenin tamamı — Türkçe açıklamaları ve bu projeden gerçek örneklerle.
> Liste bu makinede kurulu olan becerilerden çıkarıldı (`.claude/` ve `~/.claude/` altında özel komut yok).
> Güncel hali her zaman `/help` ile görülebilir.

---

## Nasıl çalışır

Komutlar iki gruba ayrılır — hangisini yazman gerektiğini bilmek zaman kazandırır:

| Tür | Anlamı |
|-----|--------|
| 🟢 **sen yazarsın** | Listeden seçip bilerek çağırdığın komutlar. Bir iş akışını baştan sona yürütürler. |
| 🟡 **Claude yükler** | Sen yazmasan da konu geldiğinde otomatik devreye girer. Ayrıca yazman gerekmez. |

Bir komutun üstüne mouse ile geldiğinde çıkan tooltip, o becerinin İngilizce tek satırlık tanımıdır.
Aşağıdaki açıklamalar aynı tanımların Türkçesi, örnekler ise bu projeye özel.

---

## Kod inceleme ve kalite

En çok işe yarayan grup. Üçü birbirinin yerine geçmez: biri hata arar, biri sadeleştirir, biri güvenlik açığı arar.

### 🟢 `/code-review [seviye] [PR# | branch] [--comment] [--fix]`

Değişiklikleri **hata avı** için inceler.

| Seviye | Davranış |
|--------|----------|
| `low` / `medium` | Az sayıda, yüksek güvenli bulgu |
| `high` / `max` | Geniş kapsam, belirsiz bulgular da dahil |
| `ultra` | Bulutta çok ajanlı derin inceleme (faturalandırılır, **yalnızca sen başlatabilirsin**) |

- `--fix` → bulguları doğrudan çalışma dizinine uygular
- `--comment` → GitHub PR'ına satır içi yorum bırakır
- Seviye yazmazsan en son kullandığın seviye tekrar kullanılır

```
/code-review high
```
> Retention temizliğinin iki kez çalıştığı hata tam bunun yakalayacağı türdendi —
> CI'da 14 test patlayana kadar fark edilmemişti. develop'a commit atmadan önce çalıştır.

```
/code-review ultra 5
```
> Açık duran dependabot PR #5 (`actions/checkout` 4→7) için derin inceleme.

---

### 🟢 `/simplify`

Değişen kodu **sadeleştirir, tekrarı temizler** ve düzeltmeleri uygular.
Hata aramaz — o iş `/code-review`'un.

```
/simplify
```
> SMTP bağlantı bloğu beş ayrı dosyada birebir kopyalanmıştı; sonunda `SmtpConnectionHelper`'a
> çıkarıldı. Bu komut o tür tekrarı kendi bulur.

---

### 🟢 `/security-review`

Mevcut dalda bekleyen değişiklikler için **güvenlik incelemesi** yapar.

```
/security-review
```
> Bu projede tam yeri var: DPAPI ile korunan SMTP/FTP şifreleri, Google Drive token'ları,
> SQL bağlantı dizeleri ve e-postaya giden hata metinlerindeki yol sızıntısı.

---

## Çalıştırma ve proje kurulumu

### 🟢 `/run`

Uygulamayı **gerçekten başlatır** — değişikliğin çalıştığını testle değil uygulamanın kendisiyle doğrular.

```
/run
```
> Tray uygulamasını açıp SMTP profil dialogundaki "Test" butonunu denemek için.
> Birim testlerin yakalayamadığı UI/dialog hataları ancak böyle çıkar.

---

### 🟢 `/init`

Projeyi tarayıp **CLAUDE.md dosyasını oluşturur** — mimari, build komutları, kritik kurallar.
Sende zaten var; yapı ciddi değiştiğinde işe yarar.

```
/init
```
> CLAUDE.md'deki "versiyon 3 yerde güncellenir" kuralı 4'e çıkmıştı (Service csproj eklendi).
> Bu tür kaymaları toparlamak için.

---

## Otomasyon ve zamanlama

İkisi karıştırılır: **loop** bu oturumda tekrar eder, **schedule** sen kapattıktan sonra bulutta çalışır.

### 🟢 `/loop [süre] [komut veya istek]`

Bir işi **belirli aralıklarla tekrar** çalıştırır. Süre yazmazsan Claude uygun aralığı kendi seçer.
Tek seferlik işler için kullanma.

```
/loop 10m release workflow durumunu kontrol et
```
> v0.99.88 release'ini beklerken tam bu işe yarardı.

---

### 🟢 `/schedule`

**Cron ile çalışan bulut ajanları** kurar, listeler, siler. Tek seferlik ileri tarihli çalıştırma da yapar.

```
/schedule her pazartesi 09:00'da açık dependabot PR'larını incele ve güvenliyse merge et
```
> #4 ve #5 aylardır açık bekliyor. Bu tür bakım işleri için biçilmiş kaftan.

---

## Ortam ve ayarlar

### 🟢 `/fewer-permission-prompts`

Geçmiş oturumları tarar, **sürekli izin sorulan güvenli komutları** bulur ve
`.claude/settings.json` dosyasına izin listesi ekler.

```
/fewer-permission-prompts
```
> `git stash drop` izin engeline takılıp iş yavaşlamıştı. `dotnet build`, `gh run list`,
> `git status` gibi sürekli tekrarlananları bir kez izinli yaparsan akış kesilmez.

---

### 🟢 `/update-config`

`settings.json` ayarlarını düzenler: izinler, ortam değişkenleri ve **hook'lar**.

> ⚠️ "Şu andan itibaren her X'te Y yap" türü otomatik davranışlar **ancak hook ile** olur.
> Hafızaya not düşmekle olmaz — çünkü hook'u Claude değil, programın kendisi çalıştırır.

```
/update-config her commit sonrası dotnet build çalıştır
```
> CLAUDE.md'deki "görev sonrası build doğrula" kuralını temenni olmaktan çıkarıp
> gerçekten zorunlu hale getirir.

---

### 🟢 `/keybindings-help`

Klavye kısayollarını özelleştirir (`~/.claude/keybindings.json`).

```
/keybindings-help
```
> Gönderme tuşunu değiştirmek ya da kendi kısayol zincirini tanımlamak için.

---

## Claude'un kendi yüklediği beceriler

Bunları yazman gerekmez — konu geçtiğinde otomatik açılır. Listede göründükleri için burada da yer alıyorlar.

### 🟡 `/dataviz`

Grafik, tablo, gösterge paneli üretirken **renk paleti ve grafik kurallarını** belirler.

```
"History klasöründeki JSON'lardan aylık yedek başarı grafiği çıkar"
```
> `C:\ProgramData\KoruMsSqlYedek\History\` altında günlük kayıtlar var — bunlardan panel
> istediğinde otomatik yüklenir.

---

### 🟡 `/artifact-design` · `/artifact-diagramming` · `/artifact-capabilities`

Paylaşılabilir web sayfası (**Artifact**) hazırlarken tasarım, diyagram ve canlı veri
yeteneklerini yöneten üç rehber.

```
"Bunu bir sayfa haline getir, ekibe göndereyim"
```
> Sayfalar varsayılan olarak **gizli** oluşturulur; paylaşma kararı sende kalır.

---

### 🟡 `/claude-api`

Claude API / Anthropic SDK referansı — model kimlikleri, fiyatlar, token sayımı, araç kullanımı.
Model konusunda ezberden cevap verilmemesi için var.

```
"Yedek raporlarını özetleyen bir yapay zekâ modülü ekleyelim mi?"
```
> Projeye LLM entegrasyonu konuşulduğu anda otomatik açılır.

---

## Yerleşik CLI komutları

Bunlar beceri değil, Claude Code'un kendi komutları. **Claude bunları çalıştıramaz** — senin yazman gerekir.
(`/model` yazıldığında Claude'a düz metin olarak gelmesinin sebebi budur.)

| Komut | İşlevi |
|-------|--------|
| `/model` | Modeli değiştirir (Opus 5, Sonnet 5, Haiku 4.5) |
| `/fast` | Hızlı modu açar/kapatır — küçük modele **düşmez**, aynı Opus daha hızlı üretir |
| `/clear` | Sohbeti sıfırlar |
| `/config` | Ayar panelini açar |
| `/help` | Kurulu komutların canlı listesini gösterir |

> `/clear`'ı konu tamamen değiştiğinde kullan — SMTP hatasından disk imajı tasarımına geçerken
> eski bağlamı taşımak işe yaramaz.

---

## Bu projede günlük akış

CLAUDE.md'deki "görev sonrası" kuralıyla uyumlu, komutlar sıraya koyulmuş hali:

1. Değişikliği yap, sonra `/simplify` ile tekrarı temizle
2. `/code-review high` — commit'ten **önce**. Retention hatası gibi sessiz regresyonlar burada yakalanır
3. Şifre, token veya bağlantı dizesine dokunduysan `/security-review`
4. `/run` ile uygulamayı aç, UI tarafını gerçekten dene
5. Versiyonu 4 yerde güncelle, CHANGELOG'a yaz, develop'a commit'le
6. `git tag vX.Y.Z && git push origin vX.Y.Z` ile release'i tetikle
7. Uzun süren CI'ı beklerken `/loop`; tekrar eden bakım işleri için `/schedule`

---

## İlgili dosyalar

- [CLAUDE.md](CLAUDE.md) — projenin Claude Code talimatları (mimari, kritik kurallar, versiyon yönetimi)
- `.claude/settings.local.json` — bu makineye özel izinler ve ayarlar

---

*Son güncelleme: 2026-08-09 · Web hali: [Komut Rehberi](https://claude.ai/code/artifact/df638194-6c5b-4eec-93be-8cfddbdb94d5)*
