<#
.SYNOPSIS
    SMTP TLS sertifika sorunlarini sahada teshis eder.

.DESCRIPTION
    "The server's SSL certificate could not be validated" hatasi alan makinede
    calistirilir. Sunucuya ham TLS ile baglanir, sertifika zincirini Windows
    deposuyla kurar, cevrimici iptal (OCSP/CRL) kontrolunu dener, kok sertifika
    deposunu, saati, proxy'yi ve antivirusu raporlar; son olarak uygulamanin kendi
    MailKit.dll'i ile ayni baglantiyi kurup MailKit'in tam hata mesajini yazar.

    Windows PowerShell 5.1 ile calisir (sunucularda PowerShell 7 olmayabilir).
    Cikti hem ekrana hem masaustune SmtpTlsRapor-<tarih>.txt olarak yazilir.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Test-SmtpTls.ps1 -SmtpHost smtp.gmail.com -Port 587
#>
param(
    [Parameter(Mandatory = $true)] [string] $SmtpHost,
    [int] $Port = 587,
    [string] $InstallDir = "C:\Program Files\Koru MsSql Yedek"
)

$ErrorActionPreference = 'Continue'
$report = Join-Path ([Environment]::GetFolderPath('Desktop')) ("SmtpTlsRapor-{0:yyyyMMdd-HHmm}.txt" -f (Get-Date))
Start-Transcript -Path $report | Out-Null

function Section($t) { ""; "==== $t ".PadRight(70, '=') }
function Chain($cert) {
    $ch = New-Object System.Security.Cryptography.X509Certificates.X509Chain
    $ch.ChainPolicy.RevocationMode = 'NoCheck'
    $null = $ch.Build($cert)
    $ch.ChainElements | ForEach-Object { "   - $($_.Certificate.Subject)" }
    $ch.ChainStatus | ForEach-Object { "   ! $($_.Status): $($_.StatusInformation.Trim())" }
}

Section "Makine"
"Bilgisayar : $env:COMPUTERNAME   Kullanici: $env:USERNAME"
"OS         : $((Get-CimInstance Win32_OperatingSystem).Caption)  $((Get-CimInstance Win32_OperatingSystem).Version)"
"Saat       : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')   (yanlis saat -> 'NotTimeValid')"
".NET/PS    : PowerShell $($PSVersionTable.PSVersion)"
"Hedef      : ${SmtpHost}:${Port}"

Section "1) Ham TLS el sikismasi (Windows SChannel)"
$cert = $null
try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.Connect($SmtpHost, $Port)
    $ns = $tcp.GetStream()
    if ($Port -ne 465) {
        $r = New-Object IO.StreamReader($ns); $w = New-Object IO.StreamWriter($ns); $w.AutoFlush = $true; $w.NewLine = "`r`n"
        "Banner   : $($r.ReadLine())"
        $w.WriteLine("EHLO tanilama"); do { $l = $r.ReadLine() } while ($l -match '^250-')
        $w.WriteLine("STARTTLS"); "STARTTLS : $($r.ReadLine())"
    }
    $script:policyErrors = $null
    $ssl = New-Object System.Net.Security.SslStream($ns, $false, {
        param($s, $c, $ch, $e)
        $script:policyErrors = $e
        $script:cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($c)
        $true })
    $ssl.AuthenticateAsClient($SmtpHost)
    "Protokol : $($ssl.SslProtocol)"
    "Politika : $script:policyErrors   (None olmali; NameMismatch = sunucu adi uyusmuyor, ChainErrors = zincir/kok sorunu)"
    "Sertifika: $($script:cert.Subject)"
    "Veren    : $($script:cert.Issuer)"
    "Gecerli  : $($script:cert.NotBefore) -> $($script:cert.NotAfter)"
    "SAN      : $(($script:cert.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.17' } | ForEach-Object { $_.Format($false) }))"
    "Zincir (NoCheck):"; Chain $script:cert
    $ssl.Dispose(); $tcp.Dispose()
    $cert = $script:cert
} catch { "HATA: $($_.Exception.Message)"; if ($_.Exception.InnerException) { "      $($_.Exception.InnerException.Message)" } }

if ($cert) {
    Section "2) Cevrimici iptal kontrolu (OCSP/CRL) - MailKit bunu yapar, tarayici yapmaz"
    $ch = New-Object System.Security.Cryptography.X509Certificates.X509Chain
    $ch.ChainPolicy.RevocationMode = 'Online'
    $ch.ChainPolicy.RevocationFlag = 'ExcludeRoot'
    $ch.ChainPolicy.UrlRetrievalTimeout = [TimeSpan]::FromSeconds(20)
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $ok = $ch.Build($cert)
    "Sonuc: $(if ($ok) { 'GECTI' } else { 'BASARISIZ' })  ($($sw.ElapsedMilliseconds) ms)"
    $ch.ChainStatus | ForEach-Object { "   ! $($_.Status): $($_.StatusInformation.Trim())" }
    "   (RevocationStatusUnknown/OfflineRevocation = OCSP/CRL adreslerine guvenlik duvari izin vermiyor)"
    "   (PartialChain/UntrustedRoot = kok sertifika bu makinede yok; Windows kok guncellemesi engelli)"

    Section "3) Kok sertifika deposu"
    $rootSubj = ($ch.ChainElements | Select-Object -Last 1).Certificate.Subject
    "Zincirin koku : $rootSubj"
    $inStore = Get-ChildItem Cert:\LocalMachine\Root | Where-Object { $_.Subject -eq $rootSubj }
    "Depoda var mi : $(if ($inStore) { 'EVET' } else { 'HAYIR  <-- sorun buyuk ihtimalle bu' })"
    "Otomatik kok guncelleme (DisableRootAutoUpdate, 1=kapali): $((Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\SystemCertificates\AuthRoot' -ErrorAction SilentlyContinue).DisableRootAutoUpdate)"
    try { $r = Invoke-WebRequest -Uri 'http://ctldl.windowsupdate.com/msdownload/update/v3/static/trustedr/en/authrootstl.cab' -Method Head -UseBasicParsing -TimeoutSec 10; "ctldl.windowsupdate.com erisimi: $($r.StatusCode)" }
    catch { "ctldl.windowsupdate.com erisimi: HATA - $($_.Exception.Message)   (kok sertifikalar indirilemiyor)" }
}

Section "4) Araya giren yazilim (TLS tarama / proxy)"
try { Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct | ForEach-Object { "Antivirus : $($_.displayName)" } } catch { "Antivirus : (SecurityCenter2 yok - sunucu)" }
Get-Service | Where-Object { $_.DisplayName -match 'ESET|Kaspersky|Avast|AVG|Bitdefender|Sophos|Norton|McAfee|Trend|Fortinet|Zscaler|Netskope|Symantec' } | ForEach-Object { "Servis    : $($_.DisplayName) [$($_.Status)]" }
Get-ChildItem Cert:\LocalMachine\Root, Cert:\CurrentUser\Root | Where-Object { $_.Subject -match 'Kaspersky|ESET|Avast|AVG|Bitdefender|Fortinet|FortiGate|Zscaler|Sophos|Norton|McAfee|Trend Micro|Proxy|Inspection|Firewall|Netskope|Palo Alto|WatchGuard|Sonic' } | ForEach-Object { "Supheli kok: $($_.Subject)" }
$ie = Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings' -ErrorAction SilentlyContinue
"IE proxy  : Enable=$($ie.ProxyEnable) Server=$($ie.ProxyServer) PAC=$($ie.AutoConfigURL)"
"WinHTTP   : $((netsh winhttp show proxy | Select-Object -Skip 3) -join ' ' )"

Section "5) Uygulamanin MailKit'i ile baglanti (tam hata mesaji)"
$mk = Join-Path $InstallDir 'MailKit.dll'; $mm = Join-Path $InstallDir 'MimeKit.dll'
if (Test-Path $mk) {
    try {
        Add-Type -Path $mm; Add-Type -Path $mk
        "MailKit $((Get-Item $mk).VersionInfo.FileVersion)"
        $c = New-Object MailKit.Net.Smtp.SmtpClient
        $c.Timeout = 20000
        $opt = if ($Port -eq 465) { [MailKit.Security.SecureSocketOptions]::SslOnConnect } else { [MailKit.Security.SecureSocketOptions]::StartTls }
        try { $c.Connect($SmtpHost, $Port, $opt); "SONUC: BAGLANDI (TLS $($c.SslProtocol))"; $c.Disconnect($true) }
        catch { "SONUC: HATA"; $e = $_.Exception; if ($e.InnerException -and $e.GetType().Name -eq 'MethodInvocationException') { $e = $e.InnerException }
                while ($e) { "  [$($e.GetType().Name)]"; $e.Message -split "`n" | ForEach-Object { "     $_" }; $e = $e.InnerException } }
        $c.Dispose()
    } catch { "MailKit yuklenemedi: $($_.Exception.Message)   (uygulama self-contained ise bu adim PowerShell'de calismayabilir; 1-4 yeterli)" }
} else { "MailKit.dll bulunamadi: $mk  (-InstallDir ile kurulum dizinini verin)" }

Section "Rapor"
"Dosya: $report"
Stop-Transcript | Out-Null
