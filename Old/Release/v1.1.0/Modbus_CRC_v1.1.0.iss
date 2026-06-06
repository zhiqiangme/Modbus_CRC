; ======================================================================
; Modbus CRC 工具 Inno Setup 安装脚本
; ======================================================================

; -----------------------------
; 项目目录
; -----------------------------
#define MyPublishDir         "D:\Project\STM32_Mill\Modbus_CRC\Project\bin\Release\Publish"
#define MySetupIconFile      "D:\Project\STM32_Mill\Modbus_CRC\Assets\CRC.ico"
#define MyOutputDir          "D:\Project\STM32_Mill\Modbus_CRC\Release\v1.1.0"

; -----------------------------
; 应用基本信息
; -----------------------------
#define MyFileName           "Modbus_CRC"
#define MyAppName            "Modbus原始帧生成器"
#define MyAppVersion         "1.1.0"
#define MyAppId              "{{89BD572F-9FA5-4268-9B67-D887EA2890D9}}"
#define MyAppExeName         "ModbusFrameTool.exe"

; 发布者信息
#define MyAppPublisher       "zhiqiangme"
#define MyAppURL             "https://github.com/zhiqiangme"

; 安装目录名
#define MyInstallDirName     MyFileName

; 输出安装包文件名
#define MyOutputBaseFilename MyFileName + "_v" + MyAppVersion


[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}

AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#MyInstallDirName}

UninstallDisplayIcon={app}\{#MyAppExeName}

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

DisableProgramGroupPage=yes

OutputDir={#MyOutputDir}
OutputBaseFilename={#MyOutputBaseFilename}

SetupIconFile={#MySetupIconFile}
WizardStyle=modern
SolidCompression=yes
Compression=lzma2


[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"


[Tasks]
Name: "desktopicon"; \
  Description: "{cm:CreateDesktopIcon}"; \
  GroupDescription: "{cm:AdditionalIcons}"


[Files]
Source: "{#MyPublishDir}\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]
Name: "{autoprograms}\{#MyAppName}"; \
  Filename: "{app}\{#MyAppExeName}"

Name: "{autodesktop}\{#MyAppName}"; \
  Filename: "{app}\{#MyAppExeName}"; \
  Tasks: desktopicon


[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent
