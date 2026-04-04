# KoruMsSqlYedek — Release Build & Package Script
# Kullanım: .\Deployment\Build-Release.ps1 [-Configuration Release] [-SkipTests]
# Çıktı: releases\KoruMsSqlYedek_vX.Y.Z.zip

param(
    [string]$Configuration = "Release",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# --- Proje kök dizini ---
$rootDir = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not (Test-Path "$rootDir\KoruMsSqlYedek.slnx")) {
    $rootDir = Split-Path -Parent $PSScriptRoot
    if (-not (Test-Path "$rootDir\KoruMsSqlYedek.slnx")) {
        $rootDir = $PSScriptRoot
        if (-not (Test-Path "$rootDir\KoruMsSqlYedek.slnx")) {
            Write-Error "Solution dosyasi bulunamadi. Script'i proje kokunden calistirin."
            exit 1
        }
    }
}

Push-Location $rootDir

try {
    # --- Versiyon bilgisini AssemblyInfo.cs'den oku ---
    $assemblyInfoPath = Join-Path $rootDir "KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs"
    if (-not (Test-Path $assemblyInfoPath)) {
        Write-Error "AssemblyInfo.cs bulunamadi: $assemblyInfoPath"
        exit 1
    }

    $assemblyContent = Get-Content $assemblyInfoPath -Raw
    if ($assemblyContent -match 'AssemblyVersion\("(\d+\.\d+\.\d+)\.\d+"\)') {
        $version = $Matches[1]
    }
    else {
        Write-Error "AssemblyInfo.cs'den versiyon okunamadi."
        exit 1
    }

    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " KoruMsSqlYedek Build & Package" -ForegroundColor Cyan
    Write-Host " Versiyon: $version" -ForegroundColor Cyan
    Write-Host " Konfigürasyon: $Configuration" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    # --- 1. NuGet Restore ---
    Write-Host "`n[1/7] NuGet paketleri geri yukleniyor..." -ForegroundColor Yellow
    dotnet restore KoruMsSqlYedek.slnx --verbosity minimal
    if ($LASTEXITCODE -ne 0) { Write-Error "NuGet restore basarisiz."; exit 1 }

    # --- 2. Build ---
    Write-Host "`n[2/7] Cozum derleniyor ($Configuration)..." -ForegroundColor Yellow
    dotnet build KoruMsSqlYedek.slnx -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { Write-Error "Build basarisiz."; exit 1 }

    # --- 3. Test ---
    if (-not $SkipTests) {
        Write-Host "`n[3/7] Testler calistiriliyor..." -ForegroundColor Yellow
        dotnet test KoruMsSqlYedek.slnx -c $Configuration --no-build --verbosity minimal
        if ($LASTEXITCODE -ne 0) { Write-Error "Testler basarisiz."; exit 1 }
    }
    else {
        Write-Host "`n[3/7] Testler atlandi (-SkipTests)." -ForegroundColor DarkGray
    }

    # --- 4. Publish ---
    Write-Host "`n[4/7] Projeler publish ediliyor..." -ForegroundColor Yellow

    $publishBase = Join-Path $rootDir "publish"
    $winPublish = Join-Path $publishBase "Win"
    $servicePublish = Join-Path $publishBase "Service"

    # Temizle
    if (Test-Path $publishBase) { Remove-Item $publishBase -Recurse -Force }

    # Win (Tray App)
    dotnet publish KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj -c $Configuration -o $winPublish -r win-x64 --self-contained false
    if ($LASTEXITCODE -ne 0) { Write-Error "Win publish basarisiz."; exit 1 }

    # Service
    dotnet publish KoruMsSqlYedek.Service\KoruMsSqlYedek.Service.csproj -c $Configuration -o $servicePublish -r win-x64 --self-contained false
    if ($LASTEXITCODE -ne 0) { Write-Error "Service publish basarisiz."; exit 1 }

    # --- 5. 7z.dll kopyalama (SevenZipSharp icin gerekli) ---
    Write-Host "`n[5/7] 7z.dll dosyalari kontrol ediliyor..." -ForegroundColor Yellow

    # NuGet paketinden 7z.dll bul
    $sevenZipPkgDir = Get-ChildItem -Path (Join-Path $env:USERPROFILE ".nuget\packages\squid-box.sevenzipsharp") -Directory | Sort-Object Name -Descending | Select-Object -First 1
    if ($sevenZipPkgDir) {
        $nativeDir = Join-Path $sevenZipPkgDir.FullName "runtimes"
        if (Test-Path $nativeDir) {
            Write-Host "  7z native runtimes bulundu: $nativeDir" -ForegroundColor DarkGray
        }
    }

    # publish dizinlerinde x64/x86 klasorleri olustur (7z.dll elle eklenecekse)
    foreach ($pubDir in @($winPublish, $servicePublish)) {
        $x64Dir = Join-Path $pubDir "x64"
        $x86Dir = Join-Path $pubDir "x86"
        if (-not (Test-Path $x64Dir)) { New-Item -Path $x64Dir -ItemType Directory -Force | Out-Null }
        if (-not (Test-Path $x86Dir)) { New-Item -Path $x86Dir -ItemType Directory -Force | Out-Null }
    }

    Write-Host "  NOT: 7z.dll dosyalarini x64/ ve x86/ klasorlerine manuel olarak ekleyin." -ForegroundColor DarkYellow
    Write-Host "  Kaynak: https://www.7-zip.org/download.html (7z Extra)" -ForegroundColor DarkGray

    # --- 6. ZIP Arsiv ---
    Write-Host "`n[6/7] ZIP arsivi olusturuluyor..." -ForegroundColor Yellow

    $releasesDir = Join-Path $rootDir "releases"
    if (-not (Test-Path $releasesDir)) { New-Item -Path $releasesDir -ItemType Directory -Force | Out-Null }

    $zipName = "KoruMsSqlYedek_v$version.zip"
    $zipPath = Join-Path $releasesDir $zipName

    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    # Stage dizini olustur
    $stageDir = Join-Path $publishBase "stage"
    $stageWin = Join-Path $stageDir "KoruMsSqlYedek"
    $stageService = Join-Path $stageDir "KoruMsSqlYedek\Service"

    if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
    New-Item -Path $stageWin -ItemType Directory -Force | Out-Null
    New-Item -Path $stageService -ItemType Directory -Force | Out-Null

    # Dosyalari kopyala
    Copy-Item -Path "$winPublish\*" -Destination $stageWin -Recurse -Force
    Copy-Item -Path "$servicePublish\*" -Destination $stageService -Recurse -Force

    # Helper scriptleri kopyala
    $scriptsDir = Join-Path $rootDir "Deployment"
    if (Test-Path "$scriptsDir\install-service.cmd") {
        Copy-Item "$scriptsDir\install-service.cmd" -Destination $stageService -Force
    }
    if (Test-Path "$scriptsDir\uninstall-service.cmd") {
        Copy-Item "$scriptsDir\uninstall-service.cmd" -Destination $stageService -Force
    }

    # ZIP olustur
    Compress-Archive -Path "$stageDir\KoruMsSqlYedek" -DestinationPath $zipPath -CompressionLevel Optimal
    $zipSize = (Get-Item $zipPath).Length / 1MB

    # --- 7. Inno Setup Installer (opsiyonel — ISCC.exe PATH'de olmalı) ---
    Write-Host "`n[7/7] Inno Setup installer derleniyor (opsiyonel)..." -ForegroundColor Yellow
    $issPath = Join-Path $rootDir "Deployment\InnoSetup\KoruMsSqlYedek.iss"
    $isccExe = (Get-Command "ISCC.exe" -ErrorAction SilentlyContinue)?.Source
    if (-not $isccExe) {
        # Inno Setup varsayilan kurulum yolu
        $defaultIscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
        if (Test-Path $defaultIscc) { $isccExe = $defaultIscc }
    }
    if ($isccExe -and (Test-Path $issPath)) {
        Write-Host "  ISCC.exe bulundu: $isccExe" -ForegroundColor DarkGray
        & $isccExe $issPath
        if ($LASTEXITCODE -eq 0) {
            $setupFile = Join-Path $rootDir "releases\KoruMsSqlYedek_v$version`_Setup.exe"
            if (Test-Path $setupFile) {
                $setupSize = (Get-Item $setupFile).Length / 1MB
                Write-Host "  Setup: $setupFile ($([math]::Round($setupSize, 1)) MB)" -ForegroundColor DarkGray
            }
        } else {
            Write-Warning "Inno Setup derleme basarisiz oldu (LASTEXITCODE=$LASTEXITCODE). Devam ediliyor."
        }
    } else {
        Write-Host "  ISCC.exe bulunamadi — installer derlenmedi. (Inno Setup 6 yukleyin veya PATH'e ekleyin)" -ForegroundColor DarkYellow
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " Build basarili!" -ForegroundColor Green
    Write-Host " Versiyon: $version" -ForegroundColor Green
    Write-Host " ZIP: $zipPath" -ForegroundColor Green
    Write-Host " Boyut: $([math]::Round($zipSize, 1)) MB" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

    # Temizlik
    if (Test-Path $publishBase) { Remove-Item $publishBase -Recurse -Force }
}
finally {
    Pop-Location
}
