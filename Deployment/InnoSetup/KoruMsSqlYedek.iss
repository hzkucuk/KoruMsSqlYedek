; KoruMsSqlYedek — Inno Setup Script
; SQL Server Yedekleme & Bulut Senkronizasyon Sistemi
;
; Kullanım:
;   1. Build-Release.ps1 ile publish edin
;   2. Bu .iss dosyasını Inno Setup Compiler ile derleyin
;   3. Veya: ISCC.exe KoruMsSqlYedek.iss
;
; Gereksinim: Inno Setup 6.2+

; === TANIMLAMALAR ===
#define MyAppName "Koru MsSql Yedek"
#define MyAppVersion "0.75.0"
#define MyAppPublisher "HZK"
#define MyAppURL "https://github.com/hzkucuk/KoruMsSqlYedek"
#define MyAppExeName "KoruMsSqlYedek.Win.exe"
#define MyServiceExeName "KoruMsSqlYedek.Service.exe"
#define MyServiceName "KoruMsSqlYedekService"

; Publish klasörleri (Build-Release.ps1 çıktısı)
; Bu yolları kendi ortamınıza göre güncelleyin
#define WinPublishDir "..\publish\Win"
#define ServicePublishDir "..\publish\Service"

[Setup]
AppId={{8F2C7A1E-3D5B-4E6F-A8C9-1B2D3E4F5A6B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Çıktı
OutputDir=..\..\releases
OutputBaseFilename=KoruMsSqlYedek_v{#MyAppVersion}_Setup
; Sıkıştırma
Compression=lzma2/ultra64
SolidCompression=yes
; Minimum OS: Windows 10 (gerekli: .NET 10 Desktop Runtime)
MinVersion=10.0
; Yönetici yetkisi gerekli (service kurulumu için)
PrivilegesRequired=admin
; Mimari
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; UI
WizardStyle=modern
SetupIconFile=compiler:SetupClassicIcon.ico
; Güncelleme sırasında mevcut verileri koru
UsePreviousAppDir=yes
; Versiyon bilgisi
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
; Lisans ve bilgi
; LicenseFile=..\..\LICENSE
InfoBeforeFile=setup_readme.txt

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
turkish.DotNetRequired=Bu uygulama .NET 10 Desktop Runtime gerektirir. Lütfen önce yükleyin.
english.DotNetRequired=This application requires .NET 10 Desktop Runtime. Please install it first.
turkish.ServiceInstall=Windows Service kuruluyor...
english.ServiceInstall=Installing Windows Service...
turkish.ServiceStart=Windows Service başlatılıyor...
english.ServiceStart=Starting Windows Service...
turkish.ServiceStop=Windows Service durduruluyor...
english.ServiceStop=Stopping Windows Service...
turkish.ServiceUninstall=Windows Service kaldırılıyor...
english.ServiceUninstall=Uninstalling Windows Service...

[Types]
Name: "full"; Description: "Tam Kurulum (Tray Uygulaması + Windows Service)"
Name: "trayonly"; Description: "Sadece Tray Uygulaması"
Name: "serviceonly"; Description: "Sadece Windows Service"
Name: "custom"; Description: "Özel Kurulum"; Flags: iscustom

[Components]
Name: "trayapp"; Description: "Tray Uygulaması (System Tray'de çalışır)"; Types: full trayonly custom; Flags: fixed
Name: "service"; Description: "Windows Service (Arka plan yedekleme motoru)"; Types: full serviceonly custom

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Components: trayapp; Flags: unchecked
Name: "startup"; Description: "Windows başlangıcında otomatik çalıştır"; Components: trayapp; Flags: unchecked

[Files]
; --- Tray Uygulaması Dosyaları ---
Source: "{#WinPublishDir}\*"; DestDir: "{app}"; Components: trayapp; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.xml"

; --- Service Dosyaları ---
Source: "{#ServicePublishDir}\*"; DestDir: "{app}\Service"; Components: service; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.xml"

; --- Helper Scriptler ---
Source: "install-service.cmd"; DestDir: "{app}\Service"; Components: service; Flags: ignoreversion
Source: "uninstall-service.cmd"; DestDir: "{app}\Service"; Components: service; Flags: ignoreversion

; --- Kurulum Bilgi Dosyası ---
Source: "setup_readme.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Başlat Menüsü
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Components: trayapp
Name: "{group}\{#MyAppName} Kaldır"; Filename: "{uninstallexe}"
; Masaüstü
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Windows başlangıcında çalıştır (isteğe bağlı)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
; Kurulum sonrası service kur ve başlat
Filename: "{app}\Service\{#MyServiceExeName}"; Parameters: "install"; StatusMsg: "{cm:ServiceInstall}"; Components: service; Flags: runhidden waituntilterminated
; Service hesabını LocalSystem yap — VSS (Volume Shadow Copy) yetkisi için zorunlu
Filename: "sc.exe"; Parameters: "config {#MyServiceName} obj= ""LocalSystem"""; Components: service; Flags: runhidden waituntilterminated
Filename: "{app}\Service\{#MyServiceExeName}"; Parameters: "start"; StatusMsg: "{cm:ServiceStart}"; Components: service; Flags: runhidden waituntilterminated
; İsteğe bağlı: Kurulum sonrası Tray uygulamasını başlat
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Components: trayapp; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Kaldırma öncesi service durdur ve kaldır
Filename: "{app}\Service\{#MyServiceExeName}"; Parameters: "stop"; RunOnceId: "StopService"; Components: service; Flags: runhidden waituntilterminated
Filename: "{app}\Service\{#MyServiceExeName}"; Parameters: "uninstall"; RunOnceId: "UninstallService"; Components: service; Flags: runhidden waituntilterminated

[UninstallDelete]
; Service log dosyaları (opsiyonel temizlik)
Type: filesandordirs; Name: "{app}\Service\logs"

[Code]
// .NET 10 Desktop Runtime kurulu mu kontrol et
// Konum: C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\10.*
function IsDotNet10DesktopInstalled(): Boolean;
var
  RuntimeBase: String;
  FindRec: TFindRec;
begin
  Result := False;
  RuntimeBase := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if DirExists(RuntimeBase) then
  begin
    if FindFirst(RuntimeBase + '\10.*', FindRec) then
    begin
      Result := True;
      FindClose(FindRec);
    end;
  end;
end;

// Kurulum başlamadan önce kontroller
function InitializeSetup(): Boolean;
begin
  Result := True;

  // .NET 10 Desktop Runtime kontrolü
  if not IsDotNet10DesktopInstalled() then
  begin
    MsgBox(ExpandConstant('{cm:DotNetRequired}') + #13#10#13#10 +
           'İndir / Download: https://dotnet.microsoft.com/download/dotnet/10.0',
           mbError, MB_OK);
    Result := False;
    Exit;
  end;
end;

// Güncelleme öncesi tray uygulamasını kapat
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    // Çalışan tray uygulamasını kapat
    Exec('taskkill', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    // Kısa bekle (dosya kilidi serbest kalsın)
    Sleep(1000);
  end;
end;

// Kaldırma öncesi tray uygulamasını kapat
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    Exec('taskkill', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(500);
  end;
end;
