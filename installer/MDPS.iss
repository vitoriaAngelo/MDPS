#define MyAppName "MDPS"
#define MyAppVersion "1.3.8"
#define MyAppPublisher "@misttylol"
#define MyAppExeName "MDPS.exe"

[Setup]
AppId={{BC53F0ED-CE38-4D61-8B3D-E63D195623AF}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\MDPS
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\release
OutputBaseFilename=MDPS-Setup-win-x64
SetupIconFile=..\src\MDPS\Assets\mdps.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
LicenseFile=..\LICENSE

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: checkedonce

[Files]
Source: "..\release\MDPS\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autodesktop}\MDPS"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\MDPS"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar MDPS"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir MDPS"; Flags: nowait postinstall skipifsilent
