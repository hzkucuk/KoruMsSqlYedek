@echo off
:: MikroSqlDbYedek — Windows Service Kaldirma Scripti
:: Yonetici yetkisi ile calistirin.
:: Topshelf CLI ile service durdurur ve kaldirir.

echo ================================================
echo  MikroSqlDbYedek Service Kaldirma
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

:: Service durdurma
echo [1/2] Service durduruluyor...
"%~dp0MikroSqlDbYedek.Service.exe" stop
if %errorlevel% neq 0 (
    echo [UYARI] Service durdurulamadi (zaten durdurulmus olabilir).
)

echo.

:: Service kaldirma
echo [2/2] Service kaldiriliyor...
"%~dp0MikroSqlDbYedek.Service.exe" uninstall
if %errorlevel% neq 0 (
    echo [HATA] Service kaldirma basarisiz.
    pause
    exit /b 1
)

echo.
echo ================================================
echo  Service basariyla kaldirildi.
echo ================================================
echo.
pause
