; InertiCorp Inno Setup Script
; Builds a Windows installer for InertiCorp CEO Survival Game

#define MyAppName "InertiCorp"
#define MyAppVersion "0.2.10"
#define MyAppPublisher "Artificially Irreverent"
#define MyAppURL "https://github.com/justinbroadbent/InertiCorp"
#define MyAppExeName "InertiCorp.exe"

[Setup]
; Unique ID for this application (generated GUID)
AppId={{A7B3C4D5-E6F7-4890-ABCD-123456789ABC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output settings - these are overridden by command line in CI
OutputDir=..\build\installer
OutputBaseFilename=InertiCorp-Setup
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
; Modern installer look
WizardStyle=modern
; Require Windows 10 or later
MinVersion=10.0
; 64-bit only
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Privileges - install for current user by default
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Uninstall info
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main game files - include everything from build directory
; Path is relative to the .iss file location (installer/)
Source: "..\build\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "installer"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up user config files in AppData on uninstall
Type: files; Name: "{userappdata}\{#MyAppName}\llm_settings.cfg"
Type: files; Name: "{userappdata}\{#MyAppName}\settings.cfg"
Type: files; Name: "{userappdata}\{#MyAppName}\music_settings.cfg"
Type: dirifempty; Name: "{userappdata}\{#MyAppName}"

[Code]
// Custom code for handling LLM model directory
procedure CurStepChanged(CurStep: TSetupStep);
var
  ModelsDir: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Create models directory for LLM files
    ModelsDir := ExpandConstant('{app}\models');
    if not DirExists(ModelsDir) then
      CreateDir(ModelsDir);
  end;
end;

// Prompt to delete downloaded AI models during uninstall
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ModelsDir: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    ModelsDir := ExpandConstant('{app}\models');
    if DirExists(ModelsDir) then
    begin
      if MsgBox('Do you want to delete downloaded AI models?' + #13#10 +
                '(These can be re-downloaded later if needed)',
                mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(ModelsDir, True, True, True);
      end;
    end;
  end;
end;
