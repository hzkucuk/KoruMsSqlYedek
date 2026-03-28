# Kurulum Kılavuzu

## Sistem Gereksinimleri

### Çalışma Ortamı (Runtime)

| Bileşen | Minimum | Önerilen |
|---------|---------|----------|
| İşletim Sistemi | Windows 10 (64-bit) | Windows 11 |
| .NET Runtime | 10.0 | 10.0 (son yama) |
| SQL Server | 2016 | 2019+ |
| RAM | 2 GB | 4 GB+ |
| Disk | 500 MB (uygulama) | SSD önerilir |

### Geliştirme Ortamı

| Bileşen | Minimum |
|---------|---------|
| Visual Studio | 2022 Community+ |
| .NET SDK | 10.0 |
| Git | 2.x |

---

## Geliştirici Kurulumu

### 1. Kaynak Kodu İndirme

```bash
git clone https://github.com/hzkucuk/MikroSqlDbYedek.git
cd MikroSqlDbYedek
```

### 2. NuGet Paketlerini Geri Yükleme

```bash
dotnet restore MikroSqlDbYedek.slnx
```

### 3. Derleme

```bash
dotnet build MikroSqlDbYedek.slnx
```

### 4. Testleri Çalıştırma

```bash
dotnet test MikroSqlDbYedek.slnx
```

---

## Release Build Oluşturma

### Build-Release.ps1 ile Otomatik Paketleme

```powershell
# Tam build + test + ZIP paketi
.\Deployment\Build-Release.ps1

# Testleri atlayarak hızlı build
.\Deployment\Build-Release.ps1 -SkipTests
```

Script aşağıdaki adımları otomatik gerçekleştirir:
1. NuGet paketlerini geri yükler
2. Çözümü Release konfigürasyonunda derler
3. Testleri çalıştırır (opsiyonel)
4. Win ve Service projelerini publish eder
5. ZIP arşivi oluşturur: `releases\MikroSqlDbYedek_vX.Y.Z.zip`

> **Not:** 7z.dll dosyalarını publish çıktısındaki `x64/` ve `x86/` klasörlerine manuel olarak eklemeniz gerekir.
> Kaynak: [7-Zip Extra](https://www.7-zip.org/download.html)

### Inno Setup ile Kurulum Paketi Oluşturma

**Gereksinim:** [Inno Setup 6.2+](https://jrsoftware.org/isinfo.php)

1. Önce `Build-Release.ps1` ile publish edin (ZIP yerine publish çıktısı kullanılır)
2. `Deployment\InnoSetup\MikroSqlDbYedek.iss` dosyasını Inno Setup Compiler ile açın
3. ISS içindeki `WinPublishDir` ve `ServicePublishDir` yollarını ortamınıza göre doğrulayın
4. Compile edin → `releases\MikroSqlDbYedek_vX.Y.Z_Setup.exe` oluşur

```bash
# Komut satırından derleme (ISCC yolunuz PATH'te ise)
ISCC.exe Deployment\InnoSetup\MikroSqlDbYedek.iss
```

Kurulum paketi özellikleri:
- .NET Framework 4.8 ve Windows 10+ ön koşul kontrolü
- Bileşen seçimi: Tray Uygulaması + Windows Service (veya ayrı ayrı)
- Türkçe ve İngilizce dil desteği
- Windows Service otomatik kurulum ve başlatma (Topshelf)
- Güncelleme sırasında mevcut planlar ve loglar korunur
- Windows başlangıcında otomatik çalıştırma seçeneği (opsiyonel)

---

## Üretim Kurulumu

### Inno Setup ile Kurulum (Önerilen)

1. `MikroSqlDbYedek_vX.Y.Z_Setup.exe` dosyasını çalıştırın
2. Kurulum türünü seçin:
   - **Tam Kurulum:** Tray Uygulaması + Windows Service
   - **Sadece Tray:** Yalnızca tray uygulaması (manuel yedekleme)
   - **Sadece Service:** Yalnızca arka plan servisi
3. Kurulum sihirbazını takip edin
4. Windows Service otomatik olarak kurulur ve başlatılır

### Manuel Kurulum

#### Tray Uygulaması
1. Publish çıktısını (`publish\Win\`) hedef klasöre kopyalayın
2. `MikroSqlDbYedek.Win.exe` çalıştırın

#### Windows Service

Helper scriptler ile (yönetici yetkisi gerekli):
```bash
# Kurulum ve başlatma
install-service.cmd

# Durdurma ve kaldırma
uninstall-service.cmd
```

Veya doğrudan `sc.exe` ile (yönetici yetkisi gerekli):
```bash
# Kurulum
sc create MikroSqlDbYedekService binPath= "C:\...\MikroSqlDbYedek.Service.exe" start= auto

# Başlatma / Durdurma / Kaldırma
sc start MikroSqlDbYedekService
sc stop MikroSqlDbYedekService
sc delete MikroSqlDbYedekService
```

> **Service Bilgisi:**
> - Service adı: `MikroSqlDbYedekService`
> - Host: Microsoft.Extensions.Hosting.WindowsServices (.NET 10)
> - Otomatik başlama olarak kaydedilir

---

## Yapılandırma

### Uygulama Verileri Konumu

```
%APPDATA%\MikroSqlDbYedek\
├── Plans\          # JSON plan dosyaları
├── Logs\           # Serilog log dosyaları
└── Config\         # Genel ayarlar (appsettings.json)
```

### Yedek Dosyaları Varsayılan Konumu

```
D:\Backups\MikroSqlDbYedek\
```

> Konum, her plan içinde `localPath` alanı ile özelleştirilebilir.

---

## NuGet Paketleri

| Paket | Versiyon | Kullanım |
|-------|----------|----------|
| Quartz | 3.8.1 | Cron zamanlama |
| Serilog | 3.1.1 | Yapısal loglama |
| Serilog.Sinks.File | 5.0.0+ | Dosya log |
| Serilog.Sinks.Console | 5.0.0+ | Konsol log |
| Newtonsoft.Json | 13.0.3 | JSON işleme |
| Microsoft.SqlServer.SqlManagementObjects | 171.30.0 | SQL backup/restore |
| Squid-Box.SevenZipSharp | 1.6.2.24 | 7z sıkıştırma |
| AlphaVSS | 1.4.0 | Volume Shadow Copy |
| Google.Apis.Drive.v3 | 1.68.0.3568 | Google Drive |
| Microsoft.Graph | 5.62.0 | OneDrive |
| FluentFTP | 51.0.0 | FTP/FTPS |
| SSH.NET | 2024.1.0 | SFTP |
| MailKit | 4.3.0 | SMTP e-posta |
| Autofac | 8.1.1 | IoC/DI |
| Microsoft.Extensions.Hosting.WindowsServices | 10.0.0 | Windows Service |
| Autofac.Extensions.DependencyInjection | 9.0.0 | Host/Autofac entegrasyonu |
| Serilog.Extensions.Hosting | 8.0.0 | Service loglama |
| Microsoft.Data.SqlClient | 6.0.2 | SQL bağlantısı |
| System.Security.Cryptography.ProtectedData | 9.0.0 | DPAPI şifreleme |
| System.ServiceProcess.ServiceController | 9.0.0 | VSS servis kontrolü |
| MSTest.TestFramework | 3.6.3 | Unit test |
| Microsoft.NET.Test.Sdk | 17.11.1 | Test runner |

---

## Sorun Giderme

### Yaygın Sorunlar

**"SQL Server bağlantısı kurulamadı"**
- SQL Server servisinin çalıştığından emin olun
- Windows Firewall'da 1433 portunu kontrol edin
- SQL Server Configuration Manager'da TCP/IP'nin etkin olduğunu doğrulayın

**"7-Zip DLL bulunamadı"**
- SevenZipSharp, `7z.dll` dosyasını gerektirir
- Uygulamanın çalıştığı dizinde veya PATH'te bulunmalıdır

**"VSS snapshot oluşturulamadı"**
- Volume Shadow Copy servisinin çalıştığını kontrol edin: `services.msc` → "Volume Shadow Copy"
- Yönetici hakları ile çalıştırın
