# Modbus RTU 原始帧生成器

一个面向 Windows 的 Modbus RTU CRC16 计算与原始帧生成工具。输入从站地址、功能码、寄存器地址和数据后，软件会自动生成带 CRC 的 Modbus RTU 原始帧，并复制到剪贴板，便于在网络调试、串口调试或远程维护平台中直接发送。

## 功能特性

- 自动计算 Modbus RTU CRC16，按低字节在前的帧尾顺序输出。
- 支持从站地址、功能码、寄存器地址和数据字段的十六进制/十进制输入。
- 支持粘贴已有原始帧并重新解析、纠正 CRC。
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
- Inno Setup，用于生成安装程序

## 构建

在 `D:\Project\Modbus_CRC\Win_x64` 目录执行：

```powershell
dotnet restore Project.slnx
dotnet build Project.slnx -c Release -p:Platform=x64
```

用途：还原依赖并编译 Release x64 版本。

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

1. 填写从站地址、功能码、寄存器地址和数据。
2. 根据需要点击字段左侧的 `0x` / `DEC` 按钮切换输入进制。
3. 点击“生成并复制”，或在输入变化后直接使用已复制的原始帧。
4. 如需校验已有帧，可将完整原始帧粘贴到顶部输入框并导入。

## 贡献

欢迎提交 Issue 或 Pull Request。提交前请先阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 许可证

本项目基于 [MIT License](LICENSE) 开源。
