#ifndef MyAppVersion
  #define MyAppVersion "0.1.0-beta.2"
#endif

#ifndef MyFileVersion
  #define MyFileVersion "0.1.0.2"
#endif

#ifndef PublishDir
  #define PublishDir "..\bin\Release\net8.0-windows\win-x64\publish"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

[Setup]
AppId={{9A037FA8-11D6-4C98-BB3D-916BEBD08622}
AppName=SilkWheel
AppVersion={#MyAppVersion}
AppVerName=SilkWheel {#MyAppVersion}
AppPublisher=Raymond Studio
AppPublisherURL=https://silkwheel.raymondstudio.cn/
AppSupportURL=https://github.com/RaymondGuoCGI/SilkWheel/issues
AppUpdatesURL=https://github.com/RaymondGuoCGI/SilkWheel/releases/latest
DefaultDirName={localappdata}\Programs\SilkWheel
DefaultGroupName=SilkWheel
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=SilkWheel-Setup-{#MyAppVersion}-win-x64
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\SilkWheel.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
VersionInfoVersion={#MyFileVersion}
VersionInfoCompany=Raymond Studio
VersionInfoDescription=SilkWheel Windows installer
VersionInfoProductName=SilkWheel

[Files]
Source: "{#PublishDir}\SilkWheel.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\SilkWheel"; Filename: "{app}\SilkWheel.exe"; WorkingDir: "{app}"; IconFilename: "{app}\SilkWheel.exe"

[Run]
Filename: "{app}\SilkWheel.exe"; Parameters: "--background"; Description: "Launch SilkWheel"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'SilkWheel');
end;
