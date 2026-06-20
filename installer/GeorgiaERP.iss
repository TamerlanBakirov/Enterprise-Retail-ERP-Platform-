[Setup]
AppId={{F47AC10B-58CC-4372-A567-0E02B2C3D479}
AppName=Georgia ERP
AppVersion=1.0.0
AppPublisher=Georgia ERP
AppPublisherURL=https://georgia-erp.com
DefaultDirName={autopf}\GeorgiaERP
DefaultGroupName=Georgia ERP
OutputDir=output
OutputBaseFilename=GeorgiaERP_Setup_1.0.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\GeorgiaERP.exe
SetupIconFile=..\src\GeorgiaERP.Desktop\Assets\app.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\src\GeorgiaERP.Desktop\bin\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Georgia ERP"; Filename: "{app}\GeorgiaERP.exe"
Name: "{commondesktop}\Georgia ERP"; Filename: "{app}\GeorgiaERP.exe"; Tasks: desktopicon
Name: "{group}\Uninstall Georgia ERP"; Filename: "{uninstallexe}"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\GeorgiaERP.exe"; Description: "Launch Georgia ERP"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\GeorgiaERP"
