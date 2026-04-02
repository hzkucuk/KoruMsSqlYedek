# ═══════════════════════════════════════════════════════════════════
# Koru MsSql Yedek — Build & Installer Script
# ═══════════════════════════════════════════════════════════════════
# Kullanım:
#   .\build.ps1                     # Mevcut versiyonu kullanır
#   .\build.ps1 -Version "0.63.0"  # Belirtilen versiyonu kullanır
# ═══════════════════════════════════════════════════════════════════

param(
    [string]$Version,
    [string]$Configuration = "Release",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path "$Root\KoruMsSqlYedek.slnx")) {
    $Root = $PSScriptRoot
}

# ── Versiyon tespiti ──────────────────────────────────────────────
if (-not $Version) {
    $assemblyInfo = Get-Content "$Root\KoruMsSqlYedek.Win\Properties\AssemblyInfo.cs" -Raw
    if ($assemblyInfo -match 'AssemblyVersion\("(\d+\.\d+\.\d+)') {
        $Version = $Matches[1]
    } else {
        Write-Error "Versiyon tespit edilemedi. -Version parametresi kullanin."
        exit 1
    }
}
Write-Host "═══ Koru MsSql Yedek v$Version Build ═══" -ForegroundColor Cyan

# ── Temizlik ──────────────────────────────────────────────────────
$publishWin     = "$Root\publish\win"
$publishService = "$Root\publish\service"
$installerOut   = "$Root\installer\output"

if (Test-Path "$Root\publish") { Remove-Item "$Root\publish" -Recurse -Force }
if (Test-Path $installerOut) { Remove-Item $installerOut -Recurse -Force }
New-Item -ItemType Directory -Path $publishWin -Force | Out-Null
New-Item -ItemType Directory -Path $publishService -Force | Out-Null
New-Item -ItemType Directory -Path $installerOut -Force | Out-Null

# ── dotnet publish — Win (Tray App) ──────────────────────────────
Write-Host "`n[1/4] Win uygulamasi yayinlaniyor..." -ForegroundColor Yellow
dotnet publish "$Root\KoruMsSqlYedek.Win\KoruMsSqlYedek.Win.csproj" `
    -c $Configuration `
    -o $publishWin `
    --self-contained false `
    -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { Write-Error "Win publish basarisiz!"; exit 1 }
Write-Host "  Win publish OK" -ForegroundColor Green

# ── dotnet publish — Service ─────────────────────────────────────
Write-Host "`n[2/4] Service yayinlaniyor..." -ForegroundColor Yellow
dotnet publish "$Root\KoruMsSqlYedek.Service\KoruMsSqlYedek.Service.csproj" `
    -c $Configuration `
    -o $publishService `
    --self-contained false `
    -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { Write-Error "Service publish basarisiz!"; exit 1 }
Write-Host "  Service publish OK" -ForegroundColor Green

# ── Inno Setup Compile ───────────────────────────────────────────
if (-not $SkipInstaller) {
    Write-Host "`n[3/4] Installer derleniyor..." -ForegroundColor Yellow

    $isccPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    )
    $iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $iscc) {
        Write-Error "Inno Setup 6 (ISCC.exe) bulunamadi. Lutfen kurun: https://jrsoftware.org/isinfo.php"
        exit 1
    }

    & $iscc "/DMyAppVersion=$Version" "$Root\installer\setup.iss"
    if ($LASTEXITCODE -ne 0) { Write-Error "Installer derleme basarisiz!"; exit 1 }
    Write-Host "  Installer OK" -ForegroundColor Green
} else {
    Write-Host "`n[3/4] Installer atlandi (-SkipInstaller)" -ForegroundColor DarkGray
}

# ── Sonuç ─────────────────────────────────────────────────────────
Write-Host "`n[4/4] Build tamamlandi!" -ForegroundColor Green
$installerFile = Get-ChildItem "$installerOut\*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($installerFile) {
    Write-Host "  Installer: $($installerFile.FullName)" -ForegroundColor Cyan
    Write-Host "  Boyut: $([math]::Round($installerFile.Length / 1MB, 2)) MB" -ForegroundColor Cyan
}

Write-Host "`n═══ Build Basarili — v$Version ═══" -ForegroundColor Cyan
