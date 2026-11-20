#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#define AppName "Profynus Music"

[Setup]
AppId={{B7E2C1A4-5D3F-4E8A-9C61-2F0A7D9B4E15}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Profynus
DefaultDirName={autopf}\Profynus
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\Profynus.Desktop.exe
UninstallDisplayName={#AppName}
SetupIconFile=..\..\Profynus.Desktop\Assets\logo.ico
OutputDir=..\..\artifacts\releases
OutputBaseFilename=Profynus-Setup-{#AppVersion}-x64
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; Flags: unchecked

[Files]
Source: "..\..\artifacts\publish\win-x64\Profynus\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\Profynus.Desktop.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\Profynus.Desktop.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Profynus.Desktop.exe"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
