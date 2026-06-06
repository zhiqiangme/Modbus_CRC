# 贡献指南

感谢你愿意改进这个项目。为了让维护成本可控，请在提交前确认变更范围清晰、说明完整。

## 提交 Issue

提交问题时请尽量包含：

- 软件版本或提交号。
- Windows 版本与 .NET Runtime 版本。
- 复现步骤、实际结果和期望结果。
- 相关原始帧、输入参数或截图。

## 提交 Pull Request

1. Fork 仓库并基于最新主分支创建功能分支。
2. 保持变更聚焦，避免混入无关格式化。
3. 如果修改 CRC、解析或复制逻辑，请补充可复现的输入输出说明。
4. 在 `D:\Project\Modbus_CRC\Win_x64` 目录执行构建检查：

```powershell
dotnet build Project.slnx -c Release -p:Platform=x64
```

用途：确认 WPF 主程序可以正常编译。

## 代码风格

- C# 代码保持 nullable enabled 和 implicit usings 的现有风格。
- 新增非命令行代码时添加简洁中文注释。
- 用户可见文本优先使用简体中文。
