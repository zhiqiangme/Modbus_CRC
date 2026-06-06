; ======================================================================
; Modbus CRC 工具 Inno Setup 安装脚本 v1.2.0
; ======================================================================

; -----------------------------
; 应用基本信息与常量定义
; -----------------------------
#define MyFileName           "Modbus_CRC"
#define MyAppName            "Modbus原始帧生成器"
#define MyAppVersion         "1.0.0"
#define MyAppPublisher       "zhiqiangme"
#define MyAppURL             "https://github.com/zhiqiangme"
#define MyAppId              "{{89BD572F-9FA5-4268-9B67-D887EA2890D9}}"
#define MyInstallDirName     MyFileName
#define MyOutputBaseFilename MyFileName + "_v" + MyAppVersion

; 发布程序与资源目录，路径相对 Deploy\CRC_Installer.iss。
#define MyPublishDir         "Artifacts\ModbusFrameTool"
#define MySetupIconFile      "Assets\CRC.ico"

; 核心执行文件名
#define MyAppExeName         "ModbusFrameTool.exe"
#define DotnetRuntimeExeName "windowsdesktop-runtime-10.0.8-win-x64.exe"
#define DotnetRuntimeMajorPrefix "10."
#define DotnetRuntimeSharedFxPath "dotnet\shared\Microsoft.WindowsDesktop.App"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; 需要可选安装 .NET Desktop Runtime，因此安装器本身使用管理员权限。
PrivilegesRequired=admin
DefaultDirName={autopf}\{#MyAppPublisher}\{#MyInstallDirName}
DefaultGroupName={#MyAppName}
UsePreviousAppDir=no

SetupIconFile={#MySetupIconFile}
UninstallDisplayIcon={app}\{#MyAppExeName}

OutputDir=Output
OutputBaseFilename={#MyOutputBaseFilename}

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

WizardStyle=modern
SolidCompression=yes
Compression=lzma2

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; \
  Description: "{cm:CreateDesktopIcon}"; \
  GroupDescription: "{cm:AdditionalIcons}"

Name: "installruntime"; \
  Description: "安装 .NET Desktop Runtime 10.0.8 x64"; \
  GroupDescription: "可选组件"; \
  Flags: unchecked

[Files]
; 搬运发布后的 Modbus CRC 工具文件。
Source: "{#MyPublishDir}\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

; 可选组件复制到系统临时目录，安装完自动删除。
Source: "Drivers\{#DotnetRuntimeExeName}"; \
  DestDir: "{tmp}"; \
  Flags: ignoreversion deleteafterinstall; \
  Tasks: installruntime

[Icons]
Name: "{autoprograms}\{#MyAppName}"; \
  Filename: "{app}\{#MyAppExeName}"

Name: "{autodesktop}\{#MyAppName}"; \
  Filename: "{app}\{#MyAppExeName}"; \
  Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
  Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
var
  OptionalTasksInitialized: Boolean;

function IsSuccessExitCode(ResultCode: Integer): Boolean;
begin
  { 0=成功，3010=成功但需要重启，1641=成功并已请求重启。 }
  Result := (ResultCode = 0) or (ResultCode = 3010) or (ResultCode = 1641);
end;

function IsWindowsDesktopRuntimeInstalled(): Boolean;
var
  Versions: TArrayOfString;
  I: Integer;
  FindRec: TFindRec;
begin
  Result := False;

  { 优先检查实际运行时目录，注册表在部分环境下可能没有 sharedfx 子项。 }
  if FindFirst(ExpandConstant('{pf64}\{#DotnetRuntimeSharedFxPath}\{#DotnetRuntimeMajorPrefix}*'), FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and
           (FindRec.Name <> '.') and
           (FindRec.Name <> '..') then
        begin
          Result := True;
          Exit;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;

  { 再检查 .NET 安装器常用注册表位置。 }
  if RegGetSubkeyNames(
       HKLM64,
       'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
       Versions) then
  begin
    for I := 0 to GetArrayLength(Versions) - 1 do
    begin
      if Pos('{#DotnetRuntimeMajorPrefix}', Versions[I]) = 1 then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

function WaitForWindowsDesktopRuntimeInstalled(TimeoutSeconds: Integer): Boolean;
var
  I: Integer;
begin
  Result := IsWindowsDesktopRuntimeInstalled();
  if Result then
  begin
    Exit;
  end;

  WizardForm.StatusLabel.Caption := '正在检测 .NET Desktop Runtime 10.0.8 x64...';
  for I := 1 to TimeoutSeconds do
  begin
    Sleep(1000);
    Result := IsWindowsDesktopRuntimeInstalled();
    if Result then
    begin
      Exit;
    end;
  end;
end;

function RunPackageInstaller(FileName: String; Parameters: String; DisplayName: String; ShowFailureMessage: Boolean): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;

  if not FileExists(FileName) then
  begin
    if ShowFailureMessage then
    begin
      MsgBox(DisplayName + ' 安装文件不存在：' + FileName, mbError, MB_OK);
    end;
    Exit;
  end;

  WizardForm.StatusLabel.Caption := '正在安装 ' + DisplayName + '...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    if not Exec(FileName, Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      if ShowFailureMessage then
      begin
        MsgBox(DisplayName + ' 启动失败。', mbError, MB_OK);
      end;
      Exit;
    end;

    if not IsSuccessExitCode(ResultCode) then
    begin
      if ShowFailureMessage then
      begin
        MsgBox(DisplayName + ' 安装失败，退出码：' + IntToStr(ResultCode), mbError, MB_OK);
      end;
      Exit;
    end;

    Result := True;
  finally
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;

procedure InstallOptionalPackages();
var
  RuntimeInstaller: String;
  RuntimeParams: String;
  RuntimeInstallLogPath: String;
  RuntimeRepairLogPath: String;
  LogDirectory: String;
begin
  RuntimeInstaller := ExpandConstant('{tmp}\{#DotnetRuntimeExeName}');
  LogDirectory := ExpandConstant('{app}\install-logs');
  ForceDirectories(LogDirectory);
  RuntimeInstallLogPath := LogDirectory + '\dotnet-desktop-runtime-install.log';
  RuntimeRepairLogPath := LogDirectory + '\dotnet-desktop-runtime-repair.log';

  if WizardIsTaskSelected('installruntime') then
  begin
    if not IsWindowsDesktopRuntimeInstalled() then
    begin
      RuntimeParams := '/install /quiet /norestart /log "' + RuntimeInstallLogPath + '"';
      RunPackageInstaller(RuntimeInstaller, RuntimeParams, '.NET Desktop Runtime 10.0.8 x64', False);

      { 部分机器上安装器可能返回成功但判定为已存在而跳过执行，这里再用 repair 强制修复一次。 }
      if not WaitForWindowsDesktopRuntimeInstalled(20) then
      begin
        RuntimeParams := '/repair /quiet /norestart /log "' + RuntimeRepairLogPath + '"';
        RunPackageInstaller(RuntimeInstaller, RuntimeParams, '.NET Desktop Runtime 10.0.8 x64 修复', False);

        if not WaitForWindowsDesktopRuntimeInstalled(20) then
        begin
          MsgBox(
            '.NET Desktop Runtime 安装程序已执行，但安装后仍未检测到 Microsoft.WindowsDesktop.App 10.x。' #13#10 +
            '安装日志：' + RuntimeInstallLogPath + #13#10 +
            '修复日志：' + RuntimeRepairLogPath,
            mbError,
            MB_OK);
        end;
      end;
    end;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
var
  SelectedTasks: String;
begin
  if (CurPageID = wpSelectTasks) and (not OptionalTasksInitialized) then
  begin
    SelectedTasks := 'desktopicon';

    { 首次进入任务页时，只自动勾选缺失的可选组件，用户仍可手动调整。 }
    if not IsWindowsDesktopRuntimeInstalled() then
    begin
      SelectedTasks := SelectedTasks + ',installruntime';
    end;

    WizardSelectTasks(SelectedTasks);
    OptionalTasksInitialized := True;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    InstallOptionalPackages();
  end;
end;
