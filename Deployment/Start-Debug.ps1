# ============================================================
# KoruMsSqlYedek — Geliştirici Debug Başlatma Scripti
# Kullanım: .\Deployment\Start-Debug.ps1
#
# Sıra:
#   1. KoruMsSqlYedek.Service  (Named Pipe sunucusu — console modda)
#   2. KoruMsSqlYedek.Win      (Tray uygulaması — pipe'a bağlanır)
# ============================================================

$root = Split-Path $PSScriptRoot -Parent

$serviceExe = "$root\KoruMsSqlYedek.Service\bin\Debug\net10.0-windows\KoruMsSqlYedek.Service.exe"
$winExe     = "$root\KoruMsSqlYedek.Win\bin\Debug\net10.0-windows\KoruMsSqlYedek.Win.exe"

# --- Varlık kontrolü ---
if (-not (Test-Path $serviceExe)) {
    Write-Error "Service exe bulunamadı: $serviceExe"
    Write-Host "Önce 'Build Solution' (Ctrl+Shift+B) yapın." -ForegroundColor Yellow
    exit 1
}
if (-not (Test-Path $winExe)) {
    Write-Error "Win exe bulunamadı: $winExe"
    Write-Host "Önce 'Build Solution' (Ctrl+Shift+B) yapın." -ForegroundColor Yellow
    exit 1
}

# --- 1. Service başlat (yeni pencerede, Named Pipe sunucusu açılsın) ---
Write-Host "[1/2] Service baslatiliyor..." -ForegroundColor Cyan
$svcProc = Start-Process -FilePath $serviceExe -PassThru

# Named Pipe sunucusunun açılması için kısa bekleme
Write-Host "      Named Pipe hazir olmasini bekleniyor (3 sn)..." -ForegroundColor DarkGray
Start-Sleep -Seconds 3

# --- 2. Tray uygulamasını başlat ---
Write-Host "[2/2] Tray uygulamasi baslatiliyor..." -ForegroundColor Cyan
$winProc = Start-Process -FilePath $winExe -PassThru

Write-Host ""
Write-Host "Her iki uygulama calisiyor." -ForegroundColor Green
Write-Host "  Service PID : $($svcProc.Id)"
Write-Host "  Win PID     : $($winProc.Id)"
Write-Host ""
Write-Host "Durdurmak icin bu pencereyi kapatin veya:" -ForegroundColor DarkGray
Write-Host "  Stop-Process -Id $($svcProc.Id)  # Service durdur" -ForegroundColor DarkGray
Write-Host "  Stop-Process -Id $($winProc.Id)  # Win durdur"     -ForegroundColor DarkGray
