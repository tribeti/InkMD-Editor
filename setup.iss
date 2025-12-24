; ============================================
; InkMD Editor Installer
; ============================================

#define MyAppName "InkMD Editor"
#define MyAppVersion "0.1.1"
#define MyAppPublisher "tribeti"

[Setup]
AppId={{6FB634CF-7009-4E4F-A592-D325C4D3DC57}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
OutputBaseFilename=InkMD_Editor_Setup
SolidCompression=yes
WizardStyle=modern dynamic windows11
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "D:\VS\InkMD-Editor\Output\InkMD_Editor_0.1.1.0_x64_Test\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "D:\VS\InkMD-Editor\Output\runtime\windowsdesktop-runtime-10.0.1-win-x64.exe"; Flags: dontcopy

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\InkMD_Editor.appinstaller"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\InkMD_Editor.appinstaller"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[UninstallRun]
Filename: "powershell.exe"; \
  Parameters: "-ExecutionPolicy Bypass -NoLogo -NonInteractive -Command ""Get-AppxPackage -Publisher '*tribeti*' | Remove-AppxPackage -ErrorAction SilentlyContinue"""; \
  Flags: runhidden waituntilterminated

[Code]
function IsDotNetDesktopRuntimeInstalled(): Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
  TempFile: String;
begin
  Result := False;
  TempFile := ExpandConstant('{tmp}\dotnet-check.txt');

  if Exec(
       'cmd.exe',
       '/c dotnet --list-runtimes > "' + TempFile + '" 2>&1',
       '',
       SW_HIDE,
       ewWaitUntilTerminated,
       ResultCode) then
  begin
    if LoadStringFromFile(TempFile, Output) then
      Result := Pos('Microsoft.WindowsDesktop.App 10.0', Output) > 0;
  end;

  DeleteFile(TempFile);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  RuntimePath: String;
begin
  Result := '';
  
  if not IsDotNetDesktopRuntimeInstalled() then
  begin
    RuntimePath := ExpandConstant('{tmp}\windowsdesktop-runtime-10.0.1-win-x64.exe');
    ExtractTemporaryFile('windowsdesktop-runtime-10.0.1-win-x64.exe');
    WizardForm.StatusLabel.Caption := 'Installing .NET Desktop Runtime 10.0.1...';
    
    if not Exec(RuntimePath, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    begin
      Result := 'Cannot run .NET Desktop Runtime installer.';
      Exit;
    end;

    if (ResultCode <> 0) and (ResultCode <> 1638) then
    begin
      Result := '.NET Desktop Runtime installation failed.' + #13#10 + 'Error code: ' + IntToStr(ResultCode);
      Exit;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  InstallScript: String;
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    InstallScript := ExpandConstant('{app}\Install.ps1');
    if not FileExists(InstallScript) then
    begin
      MsgBox('Install script not found: ' + InstallScript, mbError, MB_OK);
      Exit;
    end;

    WizardForm.StatusLabel.Caption := 'Installing InkMD Editor package...';

    if not Exec(
       'powershell.exe',
       '-ExecutionPolicy Bypass -NoLogo -NonInteractive -File "' + InstallScript + '" -Force',
       ExpandConstant('{app}'),
       SW_SHOW,
       ewWaitUntilTerminated,
       ResultCode) then
    begin
      MsgBox('Cannot run InkMD installation script.', mbError, MB_OK);
    end
    else if ResultCode <> 0 then
    begin
      MsgBox('InkMD Editor installation failed. Exit code: ' + IntToStr(ResultCode), mbError, MB_OK);
    end;
  end;
end;