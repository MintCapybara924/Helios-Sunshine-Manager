# Sunshine Multi-Instance Manager

一款现代化的 WPF 应用程序，用于在单台 Windows 电脑上管理多个 [Sunshine](https://github.com/LizardByte/Sunshine)（及其分支）串流实例。

**语言 / README**: [English](README.md) | [繁體中文](README.zh-TW.md) | 简体中文 | [日本語](README.ja.md)

## 功能特色

- **多实例管理** - 创建、编辑、克隆和删除独立的串流实例，每个实例都有独立端口和配置目录。
- **支持多个分支** - 可管理 [Sunshine](https://github.com/LizardByte/Sunshine)、[Apollo](https://github.com/ClassicOldSong/Apollo)、[Vibeshine](https://github.com/Nonary/vibeshine) 和 [Vibepollo](https://github.com/Nonary/Vibepollo)。
- **Service 运行模式** - 实例通过后台 Windows 服务（LocalSystem）控制，适用于 UAC 与登录界面等安全桌面场景。
- **每实例运行控制** - 每个实例支持 Start / Stop / Open Web UI，并提供 Start All / Stop All 批量操作。
- **独立音频路由** - 可为每个实例指定独立音频输出设备。
- **音量同步** - 可选将系统音量同步到受管理实例。
- **应用内版本获取与安装** - 可检查 GitHub Releases，并下载/安装任一支持分支的最新安装包（稳定版或预览版）。  
	这是**手动触发**的更新流程，不是后台自动更新。
- **现代桌面体验** - Fluent 风格 UI、深色/浅色主题、系统托盘集成与实时日志查看。

## 系统要求

- **操作系统**: Windows 10 / 11 (x64)
- **运行环境**: [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)
- **权限**: 管理员（服务注册和实例启动所需）
- **Sunshine**: 至少安装一个支持的分支（Sunshine、Apollo、Vibeshine 或 Vibepollo）

## 安装方式

1. 从最新版本下载 `SunshineMultiInstanceManagerSetup.exe`。
2. 运行安装程序并按照屏幕提示完成安装。
3. 以管理员身份启动 `SunshineMultiInstanceManager.exe`。

首次启动时，应用程序会自动：
- 将 **Spawner Service** 注册为 Windows 服务（LocalSystem）。
- 启动服务并通过它管理所有实例。

不需要任何手动服务配置。

## 使用方式

1. **新增实例** - 点击新增按钮，选择产品分支，设置名称和端口。
2. **配置** - 针对每个实例调整音频设备、无头模式、额外参数等选项。
3. **启动** - 启用实例并点击启动（或全部启动）。Spawner Service 会以完整系统权限启动每个实例。
4. **连接** - 使用 [Moonlight](https://moonlight-stream.org/) 或任何兼容的客户端连接到配置的端口。
5. **Web UI** - 点击实例旁的链接图标，打开其网页配置面板。

## 从源码构建

```bash
# 构建应用程序
dotnet build src/SunshineMultiInstanceManager.App/SunshineMultiInstanceManager.App.csproj

# 发布（自动包含 Spawner Service）
dotnet publish src/SunshineMultiInstanceManager.App/SunshineMultiInstanceManager.App.csproj -p:PublishProfile=win-x64-fd
```

发布产物位于 `publish/win-x64-fd/`，包含主应用程序和 `service/` 子目录中的 Spawner Service。

## 架构

```
SunshineMultiInstanceManager.App      WPF 桌面应用程序（UI + 本地控制）
SunshineMultiInstanceManager.Core     共享库（进程管理、配置、音频、显示、更新）
SunshineMultiInstanceManager.Spawner  Windows 服务（以 SYSTEM 运行，通过 Named Pipe 指令启动实例）
```

App 通过 Named Pipe 与 Spawner Service 通信。Service 使用分配到用户交互式会话的 SYSTEM Token 启动 Sunshine 实例，使其能够捕获安全桌面（UAC 和登录界面）——与标准 Sunshine 服务安装具有相同的能力。

## 已知限制

> **Vibeshine / Vibepollo 安装冲突**：虽然本管理器的目标是让多个 Sunshine 系列分支共存，但 Vibeshine 和 Vibepollo 的安装程序会强制要求卸载其他已安装的 Sunshine 系列分支，不同意卸载就无法继续安装。如果先安装 Vibe 系列，再安装 Sunshine 或 Apollo，则可以暂时共存。但当 Vibe 系列需要更新时，安装程序仍会再次要求卸载其他分支。

## 免责声明

本项目主要为个人使用而开发，不保证所有功能在任何环境或配置下均能正常工作，使用风险由用户自行承担。

## 灵感来源

本项目的灵感来自 [Apollo Fleet Launcher](https://github.com/drajabr/Apollo-Fleet-Launcher)，一款用于多开 Apollo 实例的启动器。Sunshine Multi-Instance Manager 在此基础上扩展了对更多 Sunshine 系列分支的支持。

## AI 披露声明

本项目在开发过程中使用了 AI 辅助，包括 OpenAI Codex 与 Anthropic Claude。

## 许可证

本项目采用 [GNU 通用公共许可证第 3 版](LICENSE) 许可。
