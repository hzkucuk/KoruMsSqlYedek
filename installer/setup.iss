; ═══════════════════════════════════════════════════════════════════
; Koru MsSql Yedek — Inno Setup 6 Installer Script
; ═══════════════════════════════════════════════════════════════════
; Kurulum: Program Files\KoruMsSqlYedek
; Veri:    %APPDATA%\KoruMsSqlYedek (Plans, Config, Logs, UploadState)
; Kaldırma: Uygulama dosyaları silinir, kullanıcı verileri KORUNUR
; ═══════════════════════════════════════════════════════════════════

#define MyAppName       "Koru MsSql Yedek"
#define MyAppPublisher  "HZK"
#define MyAppURL        "https://github.com/hzkucuk/KoruMsSqlYedek"
#define MyAppExeName    "KoruMsSqlYedek.Win.exe"
#define MyServiceExe    "KoruMsSqlYedek.Service.exe"
#define MyServiceName   "KoruMsSqlYedekService"

; Versiyon build.ps1 tarafından dışarıdan verilir:
;   iscc /DMyAppVersion=0.63.0 setup.iss
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

[Setup]
AppId={{7A3F8B2C-4D5E-6F01-A2B3-C4D5E6F7A8B9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\KoruMsSqlYedek
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=..\installer\output
OutputBaseFilename=KoruMsSqlYedek_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}.0
MinVersion=10.0

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupicon"; Description: "Windows başlangıcında otomatik çalıştır"; GroupDescription: "Ek seçenekler:"

[Files]
; ── Win (Tray) uygulaması ─────────────────────────────────────────
Source: "..\publish\win\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; ── Windows Service ───────────────────────────────────────────────
Source: "..\publish\service\*"; DestDir: "{app}\Service"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Kaldır"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Windows başlangıcında otomatik çalıştırma (kullanıcı seçerse)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueName: "KoruMsSqlYedek"; ValueType: string; \
  ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
; ── Kurulum sonrası: Servisi yükle ve başlat ─────────────────────
Filename: "sc.exe"; Parameters: "create {#MyServiceName} binPath= ""{app}\Service\{#MyServiceExe}"" start= auto DisplayName= ""Koru MsSql Yedek Service"""; \
  Flags: runhidden waituntilterminated; StatusMsg: "Windows Service yükleniyor..."
Filename: "sc.exe"; Parameters: "description {#MyServiceName} ""SQL Server yedekleme ve bulut senkronizasyon servisi"""; \
  Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "start {#MyServiceName}"; \
  Flags: runhidden waituntilterminated; StatusMsg: "Servis başlatılıyor..."

; Uygulamayı başlat
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} uygulamasını başlat"; \
  Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
; ── Kaldırma: Servisi durdur ve sil ──────────────────────────────
Filename: "sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden waituntilterminated

; ── Kaldırma öncesi çalışan uygulamayı kapat ─────────────────────
Filename: "taskkill.exe"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden

[Code]
// ═══════════════════════════════════════════════════════════════════
// Upgrade: mevcut versiyon çalışıyorsa kapat
// ═══════════════════════════════════════════════════════════════════
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Çalışan tray uygulamasını kapat
  Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  // Servisi durdur (upgrade için)
  Exec('sc.exe', 'stop {#MyServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(1500); // Servisin kapanması için bekle
  Result := True;
end;

// ═══════════════════════════════════════════════════════════════════
// NOT: %APPDATA%\KoruMsSqlYedek dizini kaldırma sırasında SİLİNMEZ.
// Kullanıcının planları, ayarları ve logları korunur.
// ═══════════════════════════════════════════════════════════════════
