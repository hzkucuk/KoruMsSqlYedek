---
layout: default
title: Kullanım Koşulları — Koru MsSql Yedek
---

# Kullanım Koşulları

**Son güncelleme:** 2 Nisan 2026

Bu kullanım koşulları, **Koru MsSql Yedek** ("Uygulama") uygulamasının kullanımını düzenler.

---

## 1. Kabul

Uygulamayı indirerek, yükleyerek veya kullanarak bu koşulları kabul etmiş olursunuz. Kabul etmiyorsanız uygulamayı kullanmayınız.

---

## 2. Lisans

Koru MsSql Yedek, [MIT Lisansı](https://github.com/hzkucuk/KoruMsSqlYedek/blob/master/LICENSE.txt) altında dağıtılan açık kaynaklı bir yazılımdır. Kaynak koda erişebilir, değiştirebilir ve dağıtabilirsiniz.

---

## 3. Kullanım Amacı

Uygulama aşağıdaki amaçlarla tasarlanmıştır:

- Microsoft SQL Server veritabanlarının yedeklenmesi
- Dosya ve klasörlerin yedeklenmesi (VSS desteği ile)
- Yedeklerin bulut depolama hizmetlerine yüklenmesi
- Yedeklerin zamanlanmış otomatik çalıştırılması
- Yedeklerin geri yüklenmesi

---

## 4. Sorumluluk Sınırları

### 4.1 Garanti Reddi

Uygulama **"OLDUĞU GİBİ"** (AS IS) sunulmaktadır. Geliştirici:

- Uygulamanın kesintisiz veya hatasız çalışacağını garanti etmez.
- Veri kaybı, yedek bozulması veya geri yükleme başarısızlığından sorumlu değildir.
- Uygulamanın belirli bir amaca uygunluğunu garanti etmez.

### 4.2 Kullanıcı Sorumluluğu

Kullanıcı aşağıdakilerden sorumludur:

- **Yedeklerin doğruluğunu düzenli olarak kontrol etmek** (test restore yapma)
- Bulut hesap kimlik bilgilerinin güvenliğini sağlamak
- Yedekleme planlarını ihtiyaçlarına göre yapılandırmak
- Uygulamanın çalıştığı sunucunun güvenliğini sağlamak
- Yedekleme stratejisinin iş gereksinimlerine uygunluğunu değerlendirmek

### 4.3 Veri Kaybı

**ÖNEMLİ:** Hiçbir yedekleme çözümü %100 güvenilir değildir. Kritik verileriniz için:

- Birden fazla yedekleme hedefi kullanın (yerel + bulut)
- Düzenli olarak geri yükleme testleri yapın
- 3-2-1 yedekleme kuralını uygulayın (3 kopya, 2 farklı ortam, 1 uzak)

---

## 5. Üçüncü Taraf Hizmetleri

Uygulama, kullanıcının yapılandırdığı üçüncü taraf hizmetlere bağlanır:

- **Google Drive** — [Google Hizmet Şartları](https://policies.google.com/terms) geçerlidir
- **Microsoft OneDrive** — [Microsoft Hizmet Sözleşmesi](https://www.microsoft.com/servicesagreement) geçerlidir
- **FTP/SFTP sunucuları** — Sunucu sahibinin koşulları geçerlidir

Bu hizmetlerin kullanımı ilgili sağlayıcıların kendi koşullarına tabidir.

---

## 6. Güncellemeler

- Uygulama günde bir kez yeni sürüm kontrolü yapar (GitHub Releases API).
- Güncelleme yüklemesi kullanıcının onayı ile gerçekleşir; otomatik kurulum yapılmaz.
- Güncellemeler yeni özellikler, hata düzeltmeleri ve güvenlik yamaları içerebilir.

---

## 7. Gizlilik

Kullanıcı verilerinin işlenmesi hakkında detaylı bilgi için [Gizlilik Politikası](privacy-policy.md) sayfasına bakınız.

---

## 8. Değişiklikler

Bu koşullar zaman zaman güncellenebilir. Önemli değişiklikler uygulama güncellemeleri ile birlikte duyurulacaktır.

---

## 9. İletişim

Sorularınız için:

- **GitHub Issues:** [github.com/hzkucuk/KoruMsSqlYedek/issues](https://github.com/hzkucuk/KoruMsSqlYedek/issues)
- **Geliştirici:** HZK

---

*Bu koşullar en son 24 Temmuz 2025 tarihinde güncellenmiştir.*
