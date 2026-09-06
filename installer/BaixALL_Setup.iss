; Script de Instalação Inno Setup para o BaixALL
; Compatível com Inno Setup 6+
; Aplicativo Self-Contained para Windows x64 (Não exige .NET instalado)

#define MyAppName "BaixALL"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "BaixALL"
#define MyAppURL "https://github.com/BaixALL/BaixALL"
#define MyAppExeName "BaixALL.exe"

[Setup]
; Identificador GUID único para o instalador
AppId={{C272C15E-55AE-48F7-9CD1-8C5874251B5A}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\installer_output
OutputBaseFilename=BaixALL_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DisableProgramGroupPage=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Executável principal self-contained gerado pelo dotnet publish
Source: "..\publish\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Documentação e licença
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Icons]
Name: "{autoprograms}\{#MyAppName}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\{#MyAppName}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Garante que apenas arquivos temporários criados pela aplicação sejam limpos na desinstalação
; NOTA: Os vídeos baixados pelo usuário NÃO são excluídos
Type: files; Name: "{app}\*.tmp"
Type: files; Name: "{app}\*.log"

[Code]
// Código personalizado de desinstalação seguro
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Informa ao usuário que seus vídeos baixados foram preservados com segurança
  end;
end;
