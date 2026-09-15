<#
.SYNOPSIS
    %ProgramData%\KoruMsSqlYedek veri dizininin izinlerini onarır.

.DESCRIPTION
    v0.99.91 / v0.99.92 kurulumları Plans ve Config dizinlerini Users için salt
    okunur (ya da tamamen erişimsiz) bırakmış olabilir. Bu script tüm ağacı
    v0.99.93 şemasına getirir:

        SYSTEM, Administrators  -> Tam yetki
        Users                   -> Değiştirme (Modify)   [tüm ağaç]
        Users                   -> Erişim yok            [yalnızca Updates]

    Yöntem: önce `icacls /reset /T` ile ağaçtaki her dosya ve klasör
    ProgramData'dan kalıtımla gelen varsayılan ACL'e döndürülür (elle eklenmiş
    ya da önceki sürümden kalan tüm açık/yasak girdiler silinir), sonra Users'a
    Modify verilir ve Updates'ten Users kaldırılır. Servis açılışta aynı şemayı
    yeniden uygular; bu script servisi beklemeden onarım yapmak içindir.

    Tray uygulaması yükseltilmeden (asInvoker) çalışır: yönetici hesabında bile
    UAC filtreli token'da Administrators girdisi devre dışıdır, dosyalar ancak
    Users girdisiyle okunur/yazılır. Bu yüzden Users:Modify ŞARTTIR.

.PARAMETER Path
    Veri dizini. Varsayılan: $env:ProgramData\KoruMsSqlYedek

.PARAMETER WhatIf
    Hiçbir şey değiştirmez, yalnızca mevcut durumu ve çalıştırılacak
    komutları gösterir.

.EXAMPLE
    # Yönetici PowerShell'de:
    .\Repair-DataDirAcl.ps1 -WhatIf
    .\Repair-DataDirAcl.ps1
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Path = (Join-Path $env:ProgramData 'KoruMsSqlYedek')
)

# Yerel komutların (icacls/takeown) stderr çıktısı PowerShell 5.1'de
# ErrorActionPreference=Stop ile betiği yarıda kesebiliyor; çıkış kodunu
# kendimiz kontrol ediyoruz.
$ErrorActionPreference = 'Continue'

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
$SidUsers = '*S-1-5-32-545'

$NoAccessDirs = @('Updates')

$script:Failed = 0

function Invoke-Native {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)] [string]$Exe,
        [Parameter(Mandatory)] [string[]]$Arguments,
        [string]$Target = $Path
    )
    $display = "$Exe $($Arguments -join ' ')"
    if (-not $PSCmdlet.ShouldProcess($Target, $display)) { return }

    $out = & $Exe @Arguments 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE

    if ($code -ne 0) {
        $script:Failed++
        Write-Warning "$display`n  çıkış kodu $code"
        $out | Select-Object -Last 5 | ForEach-Object { Write-Warning "  $_" }
    }
    else {
        Write-Host "  OK  $display" -ForegroundColor DarkGray
        $summary = $out | Where-Object { $_ -match 'processed|işlendi' } | Select-Object -Last 1
        if ($summary) { Write-Host "      $summary" -ForegroundColor DarkGray }
    }
}

function Show-Acl {
    param([string]$Dir, [string]$Label)
    if (-not (Test-Path -LiteralPath $Dir)) { return }
    $lines = & icacls.exe $Dir 2>&1 | ForEach-Object { "$_" } | Where-Object { $_ -and $_ -notmatch 'processed|işlendi' }
    $first = $true
    foreach ($l in $lines) {
        $l = $l.Replace($Dir, '').Trim()
        if ($first) { Write-Host ("  {0,-12} {1}" -f $Label, $l); $first = $false }
        else        { Write-Host ("  {0,-12} {1}" -f '', $l) }
    }
}

Write-Host "`nVeri dizini : $Path"
Write-Host "Mevcut izinler:" -ForegroundColor Cyan
Show-Acl $Path '(kök)'
foreach ($n in 'Plans', 'Config', 'History', 'Updates') { Show-Acl (Join-Path $Path $n) $n }

Write-Host "`nHedef şema:" -ForegroundColor Cyan
Write-Host "  SYSTEM / Administrators : Tam yetki (ProgramData kalıtımı)"
Write-Host "  Users                   : Değiştirme (tüm ağaç)"
Write-Host "  Users                   : Erişim yok  -> $($NoAccessDirs -join ', ')"
Write-Host ""

# --- 1) Sahiplik (isteğe bağlı, en iyi çaba) --------------------------------
# Başka hesabın oluşturduğu ve Administrators girdisi olmayan dosyalarda
# icacls WRITE_DAC alamaz. takeown bunu çözer; ancak /D parametresinin kabul
# ettiği harf arayüz diline bağlıdır (İngilizce Y, Türkçe E vb.). İlk deneme
# başarısız olursa diğerini dener; ikisi de olmazsa uyarı verip devam eder.
Write-Host "1/4 Sahiplik Administrators grubuna alınıyor (en iyi çaba)..." -ForegroundColor Yellow
if ($PSCmdlet.ShouldProcess($Path, 'takeown /A /R')) {
    $ok = $false
    foreach ($answer in 'Y', 'E') {
        $null = & takeown.exe /F $Path /A /R /D $answer 2>&1
        if ($LASTEXITCODE -eq 0) { $ok = $true; Write-Host "  OK  takeown /D $answer" -ForegroundColor DarkGray; break }
    }
    if (-not $ok) { Write-Warning "takeown başarısız oldu; sahiplik değiştirilmedi, devam ediliyor." }
}

# --- 2) Ağacı varsayılan (kalıtımlı) ACL'e döndür ---------------------------
# /reset: her öğedeki açık girdileri siler ve üst dizinden kalıtımı açar.
# Önceki sürümlerin bıraktığı kilitler, elle eklenmiş girdiler ve Deny'lar gider.
Write-Host "2/4 Ağaç varsayılan ACL'e döndürülüyor (icacls /reset)..." -ForegroundColor Yellow
Invoke-Native 'icacls.exe' @($Path, '/reset', '/T', '/C', '/Q')

# --- 3) Users: Modify (tüm ağaç) --------------------------------------------
Write-Host "3/4 Users için Değiştirme hakkı veriliyor..." -ForegroundColor Yellow
Invoke-Native 'icacls.exe' @($Path, '/grant', "$SidUsers`:(OI)(CI)M", '/T', '/C', '/Q')

# --- 4) Updates: Users erişimsiz --------------------------------------------
# Kalıtımı kesip Users'ı kaldırmak gerekir; aksi halde üstten gelen Modify iner.
Write-Host "4/4 Updates dizininden Users kaldırılıyor..." -ForegroundColor Yellow
foreach ($name in $NoAccessDirs) {
    $dir = Join-Path $Path $name
    if (Test-Path -LiteralPath $dir) {
        Invoke-Native 'icacls.exe' @($dir, '/inheritance:d') -Target $dir
        Invoke-Native 'icacls.exe' @($dir, '/remove:g', $SidUsers, '/T', '/C', '/Q') -Target $dir
    }
}

# --- Sonuç ------------------------------------------------------------------
if (-not $WhatIfPreference) {
    Write-Host "`nSonuç:" -ForegroundColor Cyan
    Show-Acl $Path '(kök)'
    foreach ($n in 'Plans', 'Config', 'History', 'Updates') { Show-Acl (Join-Path $Path $n) $n }

    if ($script:Failed -gt 0) {
        Write-Host "`n$($script:Failed) adım hata verdi — yukarıdaki uyarıları inceleyin." -ForegroundColor Red
        exit 2
    }
    Write-Host "`nTamamlandı. Tray uygulamasını yeniden başlatın; servisin yeniden başlatılması gerekmez." -ForegroundColor Green
}
