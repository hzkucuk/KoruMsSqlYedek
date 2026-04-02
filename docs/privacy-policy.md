---
layout: default
title: Gizlilik Politikası — Koru MsSql Yedek
---

# Gizlilik Politikası

**Son güncelleme:** 24 Temmuz 2025

Bu gizlilik politikası, **Koru MsSql Yedek** ("Uygulama") uygulamasının kullanıcı verilerini nasıl topladığını, kullandığını ve koruduğunu açıklar.

---

## 1. Genel Bakış

Koru MsSql Yedek, SQL Server veritabanlarını ve dosyalarını yedekleyen, bulut depolama hizmetlerine (Google Drive, OneDrive, FTP/SFTP) yükleyen bir Windows masaüstü uygulamasıdır.

**Uygulama herhangi bir kişisel veriyi toplayıp bir sunucuya göndermez.** Tüm veriler kullanıcının kendi bilgisayarında ve kendi bulut hesaplarında saklanır.

---

## 2. Toplanan Veriler

### 2.1 Yerel Olarak Saklanan Veriler

Aşağıdaki veriler yalnızca kullanıcının bilgisayarında (`%APPDATA%\KoruMsSqlYedek`) saklanır:

| Veri Türü | Amaç | Konum |
|-----------|-------|-------|
| Yedekleme planları | Plan yapılandırması (veritabanı adları, zamanlama, hedefler) | `Plans\` |
| Uygulama ayarları | Genel tercihler, SMTP yapılandırması | `Config\` |
| Log dosyaları | Hata ayıklama ve sorun giderme | `Logs\` |
| Bulut upload durumu | Kesintiye uğrayan yüklemeleri sürdürme | `UploadState\` |

### 2.2 OAuth Kimlik Bilgileri

- Google Drive ve OneDrive entegrasyonu için OAuth 2.0 kimlik doğrulama kullanılır.
- OAuth token'ları kullanıcının bilgisayarında **Windows DPAPI** (Data Protection API) ile şifrelenerek saklanır.
- Token'lar yalnızca kullanıcının yedekleme dosyalarını yüklemek/silmek için kullanılır.
- Uygulama yalnızca `drive.file` scope'unu kullanır — bu, **yalnızca uygulamanın oluşturduğu dosyalara** erişim demektir.

### 2.3 Toplanmayan Veriler

Uygulama aşağıdaki verileri **toplamaz, paylaşmaz veya iletmez**:

- Kişisel bilgiler (ad, e-posta, telefon)
- Konum verileri
- Kullanım istatistikleri veya analitik
- Reklam veya pazarlama verileri
- Veritabanı içerikleri (yalnızca yedek dosyaları oluşturulur)

---

## 3. Üçüncü Taraf Hizmetleri

Uygulama, kullanıcının yapılandırdığı bulut hizmetlerine bağlanabilir:

| Hizmet | Erişim Türü | Kapsam |
|--------|-------------|--------|
| Google Drive | OAuth 2.0 (`drive.file`) | Yalnızca uygulama tarafından oluşturulan dosyalar |
| OneDrive | OAuth 2.0 (`Files.ReadWrite`) | Yalnızca belirtilen klasör |
| FTP/SFTP | Kullanıcı adı/şifre | Yalnızca belirtilen dizin |

Her hizmet bağlantısı kullanıcının açık onayı ile yapılır. Kullanıcı istediği zaman bağlantıyı kaldırabilir.

---

## 4. Güncelleme Kontrolü

Uygulama, günde bir kez GitHub Releases API'sini (`api.github.com`) kontrol ederek yeni sürüm olup olmadığını denetler. Bu istek:

- Anonim HTTP GET isteğidir (kimlik doğrulama gerekmez)
- Kişisel veri göndermez
- Yalnızca sürüm numarasını karşılaştırır

---

## 5. Veri Güvenliği

- OAuth token'ları ve şifreler **Windows DPAPI** ile şifrelenir (makine/kullanıcıya özel).
- Uygulama yönetici hakları ile çalışır (SQL Server erişimi için).
- Tüm iletişim HTTPS üzerinden gerçekleşir.
- Kaynak kod açık kaynaklıdır ve [GitHub'da](https://github.com/hzkucuk/KoruMsSqlYedek) incelenebilir.

---

## 6. Veri Saklama ve Silme

- Kullanıcı uygulamayı kaldırdığında uygulama dosyaları silinir.
- `%APPDATA%\KoruMsSqlYedek` klasörü korunur (kullanıcı tercihi).
- Kullanıcı bu klasörü elle silerek tüm yerel verileri tamamen kaldırabilir.
- Bulut hesaplarındaki yedek dosyaları kullanıcının sorumluluğundadır.

---

## 7. Çocukların Gizliliği

Bu uygulama 13 yaşın altındaki çocuklara yönelik değildir ve onlardan bilerek veri toplamaz.

---

## 8. Değişiklikler

Bu gizlilik politikası zaman zaman güncellenebilir. Güncellemeler bu sayfada yayınlanacaktır.

---

## 9. İletişim

Gizlilik ile ilgili sorularınız için:

- **GitHub Issues:** [github.com/hzkucuk/KoruMsSqlYedek/issues](https://github.com/hzkucuk/KoruMsSqlYedek/issues)
- **Geliştirici:** HZK

---

*Bu politika en son 24 Temmuz 2025 tarihinde güncellenmiştir.*
