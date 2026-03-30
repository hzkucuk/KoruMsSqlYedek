@echo off
:: KoruMsSqlYedek — Windows Service Kurulum Scripti
:: Yonetici yetkisi ile calistirin.
:: Topshelf CLI ile service kurulumu yapar ve baslatir.

echo ================================================
echo  KoruMsSqlYedek Service Kurulumu
echo ================================================
echo.

:: Yonetici yetkisi kontrol
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [HATA] Bu script yonetici yetkisi gerektirir.
    echo        Sag tikla -^> "Yonetici olarak calistir" secin.
    echo.
    pause
    exit /b 1
)

:: Service kurulumu
echo [1/3] Service kuruluyor...
"%~dp0KoruMsSqlYedek.Service.exe" install
if %errorlevel% neq 0 (
    echo [HATA] Service kurulumu basarisiz.
    pause
    exit /b 1
)

echo.

:: LocalSystem hesabı — VSS için gerekli
echo [2/3] Service hesabi LocalSystem olarak ayarlaniyor (VSS yetkisi)...
sc config KoruMsSqlYedekService obj= "LocalSystem"
if %errorlevel% neq 0 (
    echo [UYARI] Hesap ayarlanamadi. Servis varsayilan hesapla calisacak.
)

echo.

:: Service baslatma
echo [3/3] Service baslatiliyor...
"%~dp0KoruMsSqlYedek.Service.exe" start
if %errorlevel% neq 0 (
    echo [UYARI] Service baslatilamadi. Manuel olarak baslatmayi deneyin:
    echo         net start KoruMsSqlYedekService
    pause
    exit /b 1
)

echo.
echo ================================================
echo  Service basariyla kuruldu ve baslatildi.
echo  Service adi: KoruMsSqlYedekService
echo ================================================
echo.
pause
