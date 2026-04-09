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
#ifndef MyAppVersion
  #define MyAppVersion "0.99.27"
#endif
#define MyAppPublisher "Zafer Bilgisayar"
#define MyAppURL "https://github.com/hzkucuk/KoruMsSqlYedek"
#define MyAppExeName "KoruMsSqlYedek.Win.exe"
#define MyServiceExeName "KoruMsSqlYedek.Service.exe"
#define MyServiceName "KoruMsSqlYedekService"
#define DotNetRuntimeInstaller "windowsdesktop-runtime-10.0.5-win-x64.exe"

; Publish klasörleri
; Manuel derleme için: ISCC.exe KoruMsSqlYedek.iss /DWinPublishDir=...absolute... /DServicePublishDir=...absolute...
#ifndef WinPublishDir
  #define WinPublishDir "..\..\publish\Win"
#endif
#ifndef ServicePublishDir
  #define ServicePublishDir "..\..\publish\Service"
#endif

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
SetupIconFile=KoruMsSqlYedek.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
; Güncelleme sırasında mevcut verileri koru
UsePreviousAppDir=yes
; Versiyon bilgisi
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany=Zafer Bilgisayar
VersionInfoProductName={#MyAppName}
; Lisans ve bilgi
LicenseFile=license.txt
InfoBeforeFile=disclaimer.txt
InfoAfterFile=setup_readme.txt

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
turkish.DotNetRequired=Bu uygulama .NET 10 Desktop Runtime gerektirir. Şimdi otomatik kurulacak...
english.DotNetRequired=This application requires .NET 10 Desktop Runtime. It will be installed automatically...
turkish.DotNetInstalling=.NET 10 Desktop Runtime kuruluyor, lütfen bekleyin...
english.DotNetInstalling=Installing .NET 10 Desktop Runtime, please wait...
turkish.DotNetFailed=.NET 10 Desktop Runtime kurulumu başarısız oldu (hata kodu: %1). Lütfen manuel olarak yükleyin: https://dotnet.microsoft.com/download/dotnet/10.0
english.DotNetFailed=.NET 10 Desktop Runtime installation failed (error code: %1). Please install manually: https://dotnet.microsoft.com/download/dotnet/10.0
turkish.DotNetSuccess=.NET 10 Desktop Runtime başarıyla kuruldu.
english.DotNetSuccess=.NET 10 Desktop Runtime installed successfully.
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

; --- .NET Desktop Runtime Redistributable ---
; dontcopy: PrepareToInstall içinde ExtractTemporaryFile ile çıkarılır (PrepareToInstall, [Files] çıkarımından ÖNCE çalışır)
Source: "redist\{#DotNetRuntimeInstaller}"; Flags: dontcopy nocompression

; --- Kurulum Bilgi Dosyaları ---
Source: "license.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "disclaimer.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "setup_readme.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Başlat Menüsü
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Components: trayapp
Name: "{group}\{#MyAppName} Kaldır"; Filename: "{uninstallexe}"
; Masaüstü
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Dirs]
; %ProgramData%\KoruMsSqlYedek — hem Tray hem Service tarafından erişilir
; Users grubuna Modify izni verilir (Tray kullanıcı bağlamında yazabilsin)
Name: "{commonappdata}\KoruMsSqlYedek"; Permissions: users-modify
Name: "{commonappdata}\KoruMsSqlYedek\Plans"; Permissions: users-modify
Name: "{commonappdata}\KoruMsSqlYedek\Config"; Permissions: users-modify
Name: "{commonappdata}\KoruMsSqlYedek\Logs"; Permissions: users-modify
Name: "{commonappdata}\KoruMsSqlYedek\UploadState"; Permissions: users-modify

[Registry]
; Windows başlangıcında çalıştır (isteğe bağlı)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
; Kurulum sonrası service kur ve başlat (sc.exe ile — exe CLI komut desteklemiyor)
Filename: "sc.exe"; Parameters: "create {#MyServiceName} binPath= ""{app}\Service\{#MyServiceExeName}"" start= auto"; StatusMsg: "{cm:ServiceInstall}"; Components: service; Flags: runhidden waituntilterminated
; Service hesabını LocalSystem yap — VSS (Volume Shadow Copy) yetkisi için zorunlu
Filename: "sc.exe"; Parameters: "config {#MyServiceName} obj= ""LocalSystem"""; Components: service; Flags: runhidden waituntilterminated
; Service açıklaması
Filename: "sc.exe"; Parameters: "description {#MyServiceName} ""Koru MsSql Yedek — SQL Server Yedekleme & Bulut Senkronizasyon Servisi"""; Components: service; Flags: runhidden waituntilterminated
; Servisi başlat
Filename: "sc.exe"; Parameters: "start {#MyServiceName}"; StatusMsg: "{cm:ServiceStart}"; Components: service; Flags: runhidden waituntilterminated
; İsteğe bağlı: Kurulum sonrası Tray uygulamasını başlat (asInvoker — normal kullanıcı olarak)
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Components: trayapp; Flags: shellexec nowait postinstall skipifsilent

[UninstallRun]
; Kaldırma öncesi service durdur ve kaldır (sc.exe ile)
Filename: "sc.exe"; Parameters: "stop {#MyServiceName}"; RunOnceId: "StopService"; Components: service; Flags: runhidden waituntilterminated
; Servisin durmasını bekle
Filename: "cmd.exe"; Parameters: "/c timeout /t 3 /nobreak >nul"; RunOnceId: "WaitServiceStop"; Components: service; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete {#MyServiceName}"; RunOnceId: "UninstallService"; Components: service; Flags: runhidden waituntilterminated

[UninstallDelete]
; Service log dosyaları (opsiyonel temizlik)
Type: filesandordirs; Name: "{app}\Service\logs"

[Code]
var
  DotNetNeeded: Boolean;

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
  DotNetNeeded := not IsDotNet10DesktopInstalled();
end;

// Dosyalar kopyalandıktan sonra, asıl kurulum öncesi .NET Runtime kur
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  RuntimePath: String;
  ResultCode: Integer;
begin
  Result := '';
  NeedsRestart := False;

  if DotNetNeeded then
  begin
    // Önce runtime dosyasını {tmp} klasörüne çıkar
    // (PrepareToInstall, [Files] çıkarımından ÖNCE çalışır; dontcopy dosyalar manuel çıkarılmalı)
    ExtractTemporaryFile('{#DotNetRuntimeInstaller}');
    RuntimePath := ExpandConstant('{tmp}\{#DotNetRuntimeInstaller}');
    // Kullanıcıya bilgi ver
    WizardForm.StatusLabel.Caption := ExpandConstant('{cm:DotNetInstalling}');
    WizardForm.StatusLabel.Update;

    // Sessiz kurulum: /install /quiet /norestart
    if not Exec(RuntimePath, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      Result := ExpandConstant('{cm:DotNetFailed}');
      StringChange(Result, '%1', IntToStr(ResultCode));
      Exit;
    end;

    // Başarı kontrolleri:
    // 0 = başarılı kurulum
    // 1641 = kurulum başarılı, yeniden başlatma başlatıldı
    // 3010 = kurulum başarılı, yeniden başlatma gerekli
    if (ResultCode <> 0) and (ResultCode <> 1641) and (ResultCode <> 3010) then
    begin
      Result := ExpandConstant('{cm:DotNetFailed}');
      StringChange(Result, '%1', IntToStr(ResultCode));
      Exit;
    end;

    if (ResultCode = 1641) or (ResultCode = 3010) then
      NeedsRestart := True;

    // Kurulum sonrası doğrulama
    if IsDotNet10DesktopInstalled() then
      Log('.NET 10 Desktop Runtime başarıyla kuruldu.')
    else if not NeedsRestart then
      Log('.NET 10 Desktop Runtime kurulumu tamamlandı ancak doğrulanamadı (restart gerekebilir).');
  end;
end;

// Güncelleme öncesi servisi durdur ve tray uygulamasını kapat
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    // Çalışan servisi durdur (güncelleme öncesi dosya kilidi önleme)
    Exec('sc.exe', 'stop {#MyServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    // Çalışan tray uygulamasını kapat
    Exec('taskkill', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    // Kısa bekle (dosya kilidi serbest kalsın)
    Sleep(2000);
  end;
end;

// Wizard açıldığında memo kontrollerini okunabilir yap (standart beyaz tema)
procedure InitializeWizard();
begin
  // Lisans sayfası memo
  WizardForm.LicenseMemo.Color := clWhite;
  WizardForm.LicenseMemo.Font.Color := clBlack;

  // Kurulum öncesi bilgi sayfası memo (disclaimer)
  WizardForm.InfoBeforeMemo.Color := clWhite;
  WizardForm.InfoBeforeMemo.Font.Color := clBlack;

  // Kurulum sonrası bilgi sayfası memo (setup_readme)
  WizardForm.InfoAfterMemo.Color := clWhite;
  WizardForm.InfoAfterMemo.Font.Color := clBlack;
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
