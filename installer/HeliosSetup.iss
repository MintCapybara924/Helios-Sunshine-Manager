; ============================================================
; HeliosSetup.iss - Inno Setup 6.x script
;
; Build steps:
;   1) dotnet publish src\SunshineMultiInstanceManager.App\Helios.App.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish\win-x64-fd
;      (PublishSpawnerService target will place spawner payload under publish\win-x64-fd\service\)
;   2) iscc.exe installer\HeliosSetup.iss
;   3) Output: installer\dist\HeliosSetup_<version>.exe
; ============================================================

#define MyAppName      "Helios"
#define MyAppVersion   "0.8.1"
#define MyAppPublisher "Helios"
#define MyAppExeName   "Helios.exe"
#define MyAppSource    "..\publish\win-x64-fd"
#define MyServiceName    "HeliosService"
#define MyDistDir      "dist"

[Setup]
AppId={{A3F7B8C2-4D1E-4F9A-B2E5-7C3D8A1F6E04}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com
AppSupportURL=https://github.com
DefaultDirName={autopf}\Helios
DefaultGroupName={#MyAppName}
OutputDir={#MyDistDir}
OutputBaseFilename=HeliosSetup_{#MyAppVersion}
SetupIconFile=..\src\SunshineMultiInstanceManager.App\Assets\SMIM.ico
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
WizardStyle=modern
PrivilegesRequired=admin
MinVersion=10.0.17763
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesetrad"; MessagesFile: "Languages\ChineseTraditional.isl"
Name: "chinesesimp"; MessagesFile: "Languages\ChineseSimplified.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
english.DotNetRuntimeRequiredBody=This installer requires .NET 8 Desktop Runtime (x64).%nA download page will open now. Please install the runtime and then run this setup again.
english.ShortcutGroup=Additional shortcuts:
english.CreateDesktopShortcut=Create a desktop shortcut
english.CreateQuickLaunchShortcut=Create a Quick Launch shortcut
english.LaunchApp=Launch {#MyAppName}

chinesetrad.DotNetRuntimeRequiredBody=此安裝程式需要 .NET 8 Desktop Runtime (x64)。%n即將開啟下載頁面，請先安裝 Runtime，完成後再重新執行本安裝程式。
chinesetrad.ShortcutGroup=其他捷徑:
chinesetrad.CreateDesktopShortcut=建立桌面捷徑
chinesetrad.CreateQuickLaunchShortcut=建立快速啟動捷徑
chinesetrad.LaunchApp=啟動 {#MyAppName}

chinesesimp.DotNetRuntimeRequiredBody=此安装程序需要 .NET 8 Desktop Runtime (x64)。%n即将打开下载页面，请先安装 Runtime，完成后再重新运行此安装程序。
chinesesimp.ShortcutGroup=其他快捷方式:
chinesesimp.CreateDesktopShortcut=创建桌面快捷方式
chinesesimp.CreateQuickLaunchShortcut=创建快速启动快捷方式
chinesesimp.LaunchApp=启动 {#MyAppName}

japanese.DotNetRuntimeRequiredBody=このインストーラーには .NET 8 Desktop Runtime (x64) が必要です。%nこれからダウンロードページを開きます。ランタイムをインストールしてから、セットアップを再実行してください。
japanese.ShortcutGroup=追加のショートカット:
japanese.CreateDesktopShortcut=デスクトップ ショートカットを作成する
japanese.CreateQuickLaunchShortcut=クイック起動ショートカットを作成する
japanese.LaunchApp={#MyAppName} を起動

korean.DotNetRuntimeRequiredBody=이 설치 프로그램을 실행하려면 .NET 8 Desktop Runtime (x64)가 필요합니다.%n지금 다운로드 페이지를 엽니다. 런타임 설치 후 설치를 다시 실행해 주세요.
korean.ShortcutGroup=추가 바로 가기:
korean.CreateDesktopShortcut=바탕 화면 바로 가기 만들기
korean.CreateQuickLaunchShortcut=빠른 실행 바로 가기 만들기
korean.LaunchApp={#MyAppName} 실행

french.DotNetRuntimeRequiredBody=Ce programme d'installation requiert .NET 8 Desktop Runtime (x64).%nUne page de téléchargement va s'ouvrir. Installez le runtime puis relancez cette installation.
french.ShortcutGroup=Raccourcis supplémentaires :
french.CreateDesktopShortcut=Créer un raccourci sur le Bureau
french.CreateQuickLaunchShortcut=Créer un raccourci Lancement rapide
french.LaunchApp=Lancer {#MyAppName}

german.DotNetRuntimeRequiredBody=Dieses Installationsprogramm erfordert .NET 8 Desktop Runtime (x64).%nEine Downloadseite wird jetzt geöffnet. Installieren Sie die Runtime und starten Sie das Setup anschließend erneut.
german.ShortcutGroup=Zusätzliche Verknüpfungen:
german.CreateDesktopShortcut=Desktopverknüpfung erstellen
german.CreateQuickLaunchShortcut=Schnellstartverknüpfung erstellen
german.LaunchApp={#MyAppName} starten

spanish.DotNetRuntimeRequiredBody=Este instalador requiere .NET 8 Desktop Runtime (x64).%nSe abrirá una página de descarga. Instala el runtime y vuelve a ejecutar este instalador.
spanish.ShortcutGroup=Accesos directos adicionales:
spanish.CreateDesktopShortcut=Crear un acceso directo en el escritorio
spanish.CreateQuickLaunchShortcut=Crear un acceso directo de Inicio rápido
spanish.LaunchApp=Iniciar {#MyAppName}

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopShortcut}"; GroupDescription: "{cm:ShortcutGroup}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchShortcut}"; GroupDescription: "{cm:ShortcutGroup}"; Flags: unchecked

[Files]
Source: "{#MyAppSource}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppSource}\service\*"; DestDir: "{app}\service"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent shellexec; Verb: "runas"

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden; RunOnceId: "StopSvc"
Filename: "{sys}\sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden; RunOnceId: "DeleteSvc"
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""\Helios\Helios_AutoStart"" /F"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteSMIMAutoStartTask"
Filename: "taskkill.exe"; Parameters: "/f /im {#MyAppExeName}"; Flags: runhidden waituntilterminated; RunOnceId: "KillManagerLauncher"

[Code]
function ServiceExists(const ServiceName: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('cmd.exe', '/C sc query "' + ServiceName + '" >nul 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function ServiceRunning(const ServiceName: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('cmd.exe', '/C sc query "' + ServiceName + '" | find "RUNNING" >nul', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function IsDotNet8DesktopInstalled(): Boolean;
var
  FindRec: TFindRec;
  BasePath: String;
begin
  Result := False;
  BasePath := ExpandConstant('{commonpf64}') + '\dotnet\shared\Microsoft.WindowsDesktop.App\';
  if FindFirst(BasePath + '8.*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Exit;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  if not IsDotNet8DesktopInstalled() then
  begin
    MsgBox(
      ExpandConstant('{cm:DotNetRuntimeRequiredBody}'),
      mbInformation,
      MB_OK);

    if not ShellExec(
      'open',
      'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe',
      '',
      '',
      SW_SHOWNORMAL,
      ewNoWait,
      ErrorCode) then
    begin
      ShellExec(
        'open',
        'https://dotnet.microsoft.com/en-us/download/dotnet/8.0',
        '',
        '',
        SW_SHOWNORMAL,
        ewNoWait,
        ErrorCode);
    end;

    Result := False;
    Exit;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM Helios.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop HeliosService', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
