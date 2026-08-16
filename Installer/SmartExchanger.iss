#define MyAppName "SmartExchanger"
#define MyAppPublisher "Bartosz Zieba"
#define MyAppURL "https://github.com/kiviuu/SmartExchanger"
#define MyAppExeName "SmartExchanger.exe"

#ifndef MyAppVersion
  #define MyAppVersion "1.2.1"
#endif

#ifndef MyAppVersionNumeric
  #define MyAppVersionNumeric "1.2.1.0"
#endif

#ifndef PublishDir
  #define PublishDir AddBackslash(SourcePath) + "..\artifacts\publish\win-x64"
#endif

#ifndef InstallerOutputDir
  #define InstallerOutputDir AddBackslash(SourcePath) + "..\artifacts\installer"
#endif

#if FileExists(AddBackslash(SourcePath) + "SmartExchanger\SmartExchanger.csproj")
  #define RepoRoot SourcePath
#elif FileExists(AddBackslash(SourcePath) + "..\SmartExchanger\SmartExchanger.csproj")
  #define RepoRoot AddBackslash(SourcePath) + ".."
#else
  #error "Cannot locate repository root."
#endif

#define AppIconPath AddBackslash(RepoRoot) + "SmartExchanger\Assets\Icons\small-icon.ico"

[Setup]

AppId={{2C05CAC7-8B99-4846-8F1F-38D4289D451F}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}

AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

DisableProgramGroupPage=yes

PrivilegesRequired=admin

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

MinVersion=10.0.19041

OutputDir={#InstallerOutputDir}
OutputBaseFilename=SmartExchanger-{#MyAppVersion}-win-x64-setup

SetupIconFile={#AppIconPath}
UninstallDisplayIcon={app}\{#MyAppExeName}

Compression=lzma2/max
SolidCompression=yes

WizardStyle=modern

CloseApplications=yes
RestartApplications=no

VersionInfoVersion={#MyAppVersionNumeric}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersionNumeric}
VersionInfoProductTextVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=SmartExchanger Installer


[Tasks]

Name: "desktopicon"; \
Description: "{cm:CreateDesktopIcon}"; \
GroupDescription: "{cm:AdditionalIcons}"; \
Flags: unchecked


[Files]

Source: "{#PublishDir}\*"; \
DestDir: "{app}"; \
Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]

Name: "{group}\{#MyAppName}"; \
Filename: "{app}\{#MyAppExeName}"; \
WorkingDir: "{app}"; \
IconFilename: "{app}\{#MyAppExeName}"

Name: "{autodesktop}\{#MyAppName}"; \
Filename: "{app}\{#MyAppExeName}"; \
WorkingDir: "{app}"; \
IconFilename: "{app}\{#MyAppExeName}"; \
Tasks: desktopicon


[Run]

Filename: "{app}\{#MyAppExeName}"; \
Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
WorkingDir: "{app}"; \
Flags: nowait postinstall skipifsilent