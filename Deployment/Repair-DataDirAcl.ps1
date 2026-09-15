<#
.SYNOPSIS
    %ProgramData%\KoruMsSqlYedek veri dizininin izinlerini onarır.

.DESCRIPTION
    v0.99.91 / v0.99.92 kurulumları Plans ve Config dizinlerini Users için salt
    okunur (ya da tamamen erişimsiz) bırakmış olabilir. Bu script tüm ağacı
    v0.99.93 şemasına getirir:

        SYSTEM, Administrators  -> Tam yetki
        Users                   -> Değiştirme (Modify)   [tüm dizinler]
        Users                   -> Erişim yok            [yalnızca Updates]

    Kalıtım kesilir (kök ProgramData'dan sarkan CREATOR OWNER vb. girdiler
    temizlenir) ve haklar ağaçtaki her dosya/klasöre yeniden uygulanır.
    Sahipliği Administrators grubuna alır; böylece daha önce başka bir
    hesabın oluşturduğu dosyalar da düzeltilebilir.

.PARAMETER Path
    Veri dizini. Varsayılan: $env:ProgramData\KoruMsSqlYedek

.PARAMETER WhatIf
    Hiçbir şey değiştirmez, yalnızca mevcut ve hedef durumu gösterir.

.EXAMPLE
    # Yönetici PowerShell'de:
    .\Repair-DataDirAcl.ps1
    .\Repair-DataDirAcl.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Path = (Join-Path $env:ProgramData 'KoruMsSqlYedek')
)

$ErrorActionPreference = 'Stop'

# --- Yönetici kontrolü ------------------------------------------------------
$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$identity
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Bu script yönetici olarak çalıştırılmalıdır (PowerShell'i 'Yönetici olarak çalıştır')."
    exit 1
}

if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
    Write-Error "Dizin bulunamadı: $Path"
    exit 1
}

# SID'ler — yerel dil ayarından bağımsız (Türkçe Windows'ta 'Users' = 'Kullanıcılar')
$SidSystem = '*S-1-5-18'
$SidAdmins = '*S-1-5-32-544'
$SidUsers  = '*S-1-5-32-545'

$ModifyDirs = 'Plans', 'Config', 'Logs', 'UploadState', 'History', 'WebView2'
$NoAccessDirs = 'Updates'

function Invoke-Icacls {
    param([string[]]$Arguments)
    $display = 'icacls ' + ($Arguments -join ' ')
    if ($PSCmdlet.ShouldProcess($Path, $display)) {
        $out = & icacls.exe @Arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "icacls hata kodu $LASTEXITCODE döndü:`n$($out -join "`n")"
        }
        else {
            # "Successfully processed N files" satırını kısa göster
            $summary = $out | Where-Object { $_ -match 'processed|işlendi' } | Select-Object -Last 1
            Write-Host "  OK  $display" -ForegroundColor DarkGray
            if ($summary) { Write-Host "      $summary" -ForegroundColor DarkGray }
        }
    }
}

Write-Host "`nVeri dizini : $Path"
Write-Host "Mevcut izinler (kök):" -ForegroundColor Cyan
& icacls.exe $Path | Select-Object -SkipLast 2 | ForEach-Object { Write-Host "  $_" }

Write-Host "`nHedef şema:" -ForegroundColor Cyan
Write-Host "  SYSTEM / Administrators : Tam yetki (tüm ağaç)"
Write-Host "  Users                   : Değiştirme -> $($ModifyDirs -join ', ')"
Write-Host "  Users                   : Erişim yok  -> $($NoAccessDirs -join ', ')"
Write-Host ""

# --- 1) Sahipliği al --------------------------------------------------------
# Başka hesabın oluşturduğu ya da kilitlenmiş dosyalarda ACL yazabilmek için.
Write-Host "1/4 Sahiplik Administrators grubuna alınıyor..." -ForegroundColor Yellow
if ($PSCmdlet.ShouldProcess($Path, 'takeown /A /R')) {
    & takeown.exe /F $Path /A /R /D Y *> $null
}

# --- 2) Kök: kalıtımı kes, temel hakları yaz --------------------------------
# /inheritance:r  -> kalıtımı kapat, kalıtımla gelen tüm girdileri sil
# /grant:r        -> mevcut açık girdileri de değiştir (birikme olmasın)
# /T              -> ağaçtaki tüm alt öğelere uygula
Write-Host "2/4 Kök ACL sıfırlanıyor (SYSTEM + Administrators F, Users RX)..." -ForegroundColor Yellow
Invoke-Icacls @(
    $Path, '/inheritance:r',
    '/grant:r', "$SidSystem`:(OI)(CI)F",
    "$SidAdmins`:(OI)(CI)F",
    "$SidUsers`:(OI)(CI)RX",
    '/T', '/C', '/Q'
)

# --- 3) Çalışma dizinleri: Users Modify -------------------------------------
Write-Host "3/4 Users için Değiştirme hakkı veriliyor..." -ForegroundColor Yellow
foreach ($name in $ModifyDirs) {
    $dir = Join-Path $Path $name
    if (-not (Test-Path -LiteralPath $dir)) {
        if ($PSCmdlet.ShouldProcess($dir, 'New-Item Directory')) {
            New-Item -ItemType Directory -Path $dir | Out-Null
        }
    }
    Invoke-Icacls @($dir, '/grant', "$SidUsers`:(OI)(CI)M", '/T', '/C', '/Q')
}

# --- 4) Updates: Users erişimsiz --------------------------------------------
Write-Host "4/4 Updates dizininden Users kaldırılıyor..." -ForegroundColor Yellow
foreach ($name in $NoAccessDirs) {
    $dir = Join-Path $Path $name
    if (Test-Path -LiteralPath $dir) {
        Invoke-Icacls @($dir, '/remove', $SidUsers, '/T', '/C', '/Q')
    }
}

# --- Sonuç ------------------------------------------------------------------
if (-not $WhatIfPreference) {
    Write-Host "`nSonuç:" -ForegroundColor Cyan
    foreach ($name in @('') + $ModifyDirs + $NoAccessDirs) {
        $dir = if ($name) { Join-Path $Path $name } else { $Path }
        if (Test-Path -LiteralPath $dir) {
            $line = (& icacls.exe $dir | Select-Object -First 1) -replace [regex]::Escape($dir), ''
            $rest = & icacls.exe $dir | Select-Object -Skip 1 | Select-Object -SkipLast 2
            Write-Host ("  {0,-12} {1}" -f ($(if ($name) { $name } else { '(kök)' }), $line.Trim()))
            $rest | ForEach-Object { Write-Host ("  {0,-12} {1}" -f '', $_.Trim()) }
        }
    }
    Write-Host "`nTamamlandı. Servisi yeniden başlatmanız gerekmez; servis açılışta aynı şemayı uygular." -ForegroundColor Green
}
