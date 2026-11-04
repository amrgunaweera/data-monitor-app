#define AppName "NetPulse Monitor"
#define AppVersion "2.1.0"
#define AppExeName "NetPulseMonitor.exe"
#define StorePublisher GetEnv("NETPULSE_STORE_PUBLISHER")
#define PayloadDir "artifacts\store\payload"

#if StorePublisher == ""
  #error "Set NETPULSE_STORE_PUBLISHER to the exact publisher name from Partner Center."
#endif

[Setup]
AppId={{C52B89FD-03D2-4F60-91E9-0A9CC5E4E2D6}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#StorePublisher}
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=artifacts\store
OutputBaseFilename=NetPulseMonitor-Setup-x64
SetupIconFile=app.ico
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
VersionInfoCompany={#StorePublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
Uninstallable=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v NetPulseMonitor /f"; Flags: runhidden; RunOnceId: "RemoveNetPulseStartup"
Filename: "{sys}\reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v DataMonitor /f"; Flags: runhidden; RunOnceId: "RemoveDataMonitorStartup"

[UninstallDelete]
Type: files; Name: "{userstartup}\NetPulse Monitor.lnk"
Type: files; Name: "{userstartup}\DataMonitor.lnk"
