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
    Write-Host "`n[1/6] NuGet paketleri geri yukleniyor..." -ForegroundColor Yellow
    dotnet restore KoruMsSqlYedek.slnx --verbosity minimal
    if ($LASTEXITCODE -ne 0) { Write-Error "NuGet restore basarisiz."; exit 1 }

    # --- 2. Build ---
    Write-Host "`n[2/6] Cozum derleniyor ($Configuration)..." -ForegroundColor Yellow
    dotnet build KoruMsSqlYedek.slnx -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { Write-Error "Build basarisiz."; exit 1 }

    # --- 3. Test ---
    if (-not $SkipTests) {
        Write-Host "`n[3/6] Testler calistiriliyor..." -ForegroundColor Yellow
        dotnet test KoruMsSqlYedek.slnx -c $Configuration --no-build --verbosity minimal
        if ($LASTEXITCODE -ne 0) { Write-Error "Testler basarisiz."; exit 1 }
    }
    else {
        Write-Host "`n[3/6] Testler atlandi (-SkipTests)." -ForegroundColor DarkGray
    }

    # --- 4. Publish ---
    Write-Host "`n[4/6] Projeler publish ediliyor..." -ForegroundColor Yellow

    $publishBase = Join-Path $rootDir "publish"
    $winPublish = Join-Path $publishBase "Win"
    $servicePublish = Join-Path $publishBase "Service"

    # Temizle
    if (Test-Path $publishBase) { Remove-Item $publishBase -Recurse -Force }

    # Win (Tray App)
    dotnet publish KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj -c $Configuration -o $winPublish --no-build
    if ($LASTEXITCODE -ne 0) { Write-Error "Win publish basarisiz."; exit 1 }

    # Service
    dotnet publish KoruMsSqlYedek.Service\KoruMsSqlYedek.Service.csproj -c $Configuration -o $servicePublish --no-build
    if ($LASTEXITCODE -ne 0) { Write-Error "Service publish basarisiz."; exit 1 }

    # --- 5. 7z.dll kopyalama (SevenZipSharp icin gerekli) ---
    Write-Host "`n[5/6] 7z.dll dosyalari kontrol ediliyor..." -ForegroundColor Yellow

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
    Write-Host "`n[6/6] ZIP arsivi olusturuluyor..." -ForegroundColor Yellow

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
