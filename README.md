# Modbus RTU 调试工具

一个面向 Windows 的 Modbus RTU 原始帧生成、串口发送与响应解析工具。选择功能码并填写地址、数量或数据后，软件会自动生成带 CRC 的 Modbus RTU 请求帧，可复制到剪贴板，也可以直接通过串口发送并解析响应。

## 功能特性

- 自动计算 Modbus RTU CRC16，按低字节在前的帧尾顺序输出。
- 支持 `01/02/03/04/05/06/0F/10` 功能码模板，按请求类型切换字段。
- 支持从站地址、起始地址、数量和单值字段的十六进制/十进制输入。
- 支持多线圈和多寄存器写入值列表。
- 支持粘贴已有请求帧并重新解析、纠正 CRC。
- 支持响应帧解析，显示 CRC 状态、异常码和寄存器/线圈数据。
- 支持通过串口直接发送当前请求帧并读取响应。
- 提供通信日志，记录生成、发送和接收帧。
- 生成结果同时显示带空格原始帧和 CRC 值，并自动复制连续十六进制帧。
- 提供 Inno Setup 安装脚本，可打包 Windows x64 安装程序。

## 项目结构

```text
.
├── Win_x64/                 # .NET WPF 主程序源码
├── Deploy/                  # 发布产物、安装脚本和安装包资源
├── Old/                     # 历史版本代码，仅作参考
├── README.md
└── LICENSE
```

## 环境要求

- Windows 10/11 x64
- .NET SDK 10.x，用于从源码构建
- .NET Desktop Runtime 10.x，用于运行非自包含发布版本
- NuGet 会还原 `RJCP.SerialPortStream`，用于串口通信
- Inno Setup，用于生成安装程序

## 构建

在 `D:\Project\Modbus_CRC\Win_x64` 目录执行：

```powershell
dotnet restore Project.slnx
dotnet build Project.slnx -c Release -p:Platform=x64
```

用途：还原依赖并编译 Release x64 版本。

## 协议层验证

在 `D:\Project\Modbus_CRC\Win_x64` 目录执行：

```powershell
dotnet run --project Project.ProtocolChecks\Project.ProtocolChecks.csproj -c Release -p:Platform=x64
```

用途：在没有真实 Modbus 从站设备时，验证功能码模板、CRC、请求帧导入纠错和响应帧解析。

## 发布程序文件

在 `D:\Project\Modbus_CRC\Win_x64` 目录执行：

```powershell
dotnet publish Project.UI\Project.UI.csproj -p:PublishProfile=FolderProfile
```

用途：按发布配置生成程序文件，输出到 `Deploy\Artifacts\ModbusFrameTool`。

## 打包安装程序

在 `D:\Project\Modbus_CRC` 目录执行：

```powershell
iscc Deploy\CRC_Installer.iss
```

用途：使用 Inno Setup 编译安装包，输出到 `Deploy\Output`。

## 使用说明

1. 选择功能码，填写从站地址、起始地址、数量或写入数据。
2. 根据需要点击字段左侧的 `0x` / `DEC` 按钮切换输入进制。
3. 点击“生成并复制”，或在输入变化后直接使用已复制的请求帧。
4. 如需校验已有请求帧，可将完整帧粘贴到顶部输入框并导入。
5. 如需串口发送，选择 COM 口和串口参数后点击“发送”。
6. 如需解析设备返回帧，可粘贴响应帧到“响应解析”区域并点击“解析”。

## 贡献

欢迎提交 Issue 或 Pull Request。提交前请先阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 许可证

本项目基于 [MIT License](LICENSE) 开源。
