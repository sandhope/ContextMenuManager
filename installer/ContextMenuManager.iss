; ============================================================
;  ContextMenuManager Inno Setup script (Unpackaged installer)
; ============================================================
;  Required /D switches passed by CI (or local invocation):
;    /DMyAppVersion=1.0.0
;    /DMyAppArch=x64            ; or arm64
;    /DSourceDir=absolute\path\to\publish
;    /DOutputDir=absolute\path\to\dist
;
;  Example:
;    ISCC.exe /DMyAppVersion=1.0.0 /DMyAppArch=x64 ^
;             /DSourceDir="D:\code\ContextMenuManagerWinUI\bin\Release\net10.0-windows10.0.26100.0\win-x64\publish" ^
;             /DOutputDir="D:\code\ContextMenuManagerWinUI\dist" ^
;             installer\ContextMenuManager.iss
; ============================================================

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef MyAppArch
  #define MyAppArch "x64"
#endif
#ifndef SourceDir
  #error "SourceDir must be provided via /DSourceDir=... "
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

#define MyAppName            "ContextMenuManager"
#define MyAppPublisher       "ContextMenuManager"
#define MyAppURL             "https://github.com/sandhope/ContextMenuManager"
#define MyAppExeName         "ContextMenuManager.exe"

; Stable GUID; do NOT change once released or upgrades will not detect prior installs.
#define MyAppId              "{{7F3B2C9A-1E54-4D8C-B6A2-9C0D3F5E7B12}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
AllowNoIcons=yes
OutputDir={#OutputDir}
OutputBaseFilename=ContextMenuManager-Setup-{#MyAppVersion}-win-{#MyAppArch}
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
SetupIconFile=..\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
CloseApplications=yes
RestartApplications=no
MinVersion=10.0.17763

; ---- Architecture gating ----
#if MyAppArch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif MyAppArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
  #error "Unsupported MyAppArch. Use x64 or arm64."
#endif

[Languages]
Name: "en";   MessagesFile: "compiler:Default.isl"
Name: "zhcn"; MessagesFile: "{#SourcePath}\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Recursively copy the entire publish output.
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}";                Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";          Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Remove per-install runtime files/logs left in {app} so no orphans remain.
Type: files; Name: "{app}\startup.log"
Type: files; Name: "{app}\crash.log"
