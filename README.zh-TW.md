# Helios

Formerly Sunshine Multi-Instance Manager.

一款現代化的 WPF 應用程式，用於在單台 Windows 電腦上管理多個 [Sunshine](https://github.com/LizardByte/Sunshine)（及其分支）串流實例。

**語言 / README**: [English](README.md) | 繁體中文 | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

## 功能特色

- **多實例管理** - 建立、編輯、複製和刪除獨立的串流實例，每個實例都有獨立的埠號與設定目錄。
- **支援多個分支** - 可管理 [Sunshine](https://github.com/LizardByte/Sunshine)、[Apollo](https://github.com/ClassicOldSong/Apollo)、[Vibeshine](https://github.com/Nonary/vibeshine) 與 [Vibepollo](https://github.com/Nonary/Vibepollo)。
- **Service 執行模式** - 實例由背景 Windows 服務（LocalSystem）控制，適用於 UAC 與登入畫面等安全桌面情境。
- **每個實例的執行控制** - 每個實例可執行 Start / Stop / Open Web UI，並提供 Start All / Stop All 批次操作。
- **獨立音訊路由** - 可為每個實例指定個別音訊輸出裝置。
- **音量同步** - 可選擇將系統音量同步到受管理實例。
- **App 內版本抓取與安裝** - 可檢查 GitHub Releases，並下載/安裝任一支援分支的最新安裝檔（穩定版或預覽版）。  
	這是**手動觸發**的更新流程，不是背景自動更新。
- **現代化桌面體驗** - Fluent 風格 UI、深色/淺色主題、系統匣整合與即時日誌檢視。

## 系統需求

- **作業系統**: Windows 10 / 11 (x64)
- **執行環境**: [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)
- **權限**: 系統管理員（服務註冊和實例啟動所需）
- **Sunshine**: 至少安裝一個支援的分支（Sunshine、Apollo、Vibeshine 或 Vibepollo）

## 安裝方式

1. 從最新版本下載 `HeliosSetup.exe`。
2. 執行安裝程式並依照畫面指示完成安裝。
3. 以系統管理員身分啟動 `Helios.exe`。

首次啟動時，應用程式會自動：
- 將 **Spawner Service** 註冊為 Windows 服務（LocalSystem）。
- 啟動服務並透過它管理所有實例。

不需要任何手動服務設定。

## 使用方式

1. **新增實例** - 點擊新增按鈕，選擇產品分支，設定名稱和埠號。
2. **設定** - 針對每個實例調整音訊裝置、無頭模式、額外參數等選項。
3. **啟動** - 啟用實例並點擊啟動（或全部啟動）。Spawner Service 會以完整系統權限啟動每個實例。
4. **連線** - 使用 [Moonlight](https://moonlight-stream.org/) 或任何相容的客戶端連線到設定的埠號。
5. **Web UI** - 點擊實例旁的連結圖示，開啟其網頁設定介面。

## 從原始碼建置

```bash
# 建置應用程式
dotnet build src/SunshineMultiInstanceManager.App/Helios.App.csproj

# 發佈（自動包含 Spawner Service）
dotnet publish src/SunshineMultiInstanceManager.App/Helios.App.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish/win-x64-fd
```

發佈產出位於 `publish/win-x64-fd/`，包含主應用程式和 `service/` 子目錄中的 Spawner Service。

## 架構

```
Helios.App      WPF 桌面應用程式（UI + 本地控制）
Helios.Core     共用函式庫（程序管理、設定、音訊、顯示、更新）
Helios.Spawner  Windows 服務（以 SYSTEM 執行，透過 Named Pipe 指令啟動實例）
```

App 透過 Named Pipe 與 Spawner Service 通訊。Service 使用指派到使用者互動式工作階段的 SYSTEM Token 啟動 Sunshine 實例，使其能夠擷取安全桌面（UAC 和登入畫面）——與標準 Sunshine 服務安裝具有相同的能力。

## 已知限制

> **Vibeshine / Vibepollo 安裝衝突**：雖然本管理器的目標是讓多個 Sunshine 系列分支共存，但 Vibeshine 和 Vibepollo 的安裝程式會強制要求移除其他已安裝的 Sunshine 系列分支，不同意移除就無法繼續安裝。如果先安裝 Vibe 系列，再安裝 Sunshine 或 Apollo，則可以暫時共存。但當 Vibe 系列需要更新時，安裝程式仍會再次要求移除其他分支。

## 免責聲明

本專案主要為個人使用而開發，不保證所有功能在任何環境或配置下皆能正常運作，使用風險由使用者自行承擔。

## 靈感來源

本專案的靈感來自 [Apollo Fleet Launcher](https://github.com/drajabr/Apollo-Fleet-Launcher)，一款用於多開 Apollo 實例的啟動器。Helios 在此基礎上擴展了對更多 Sunshine 系列分支的支援。

## AI 揭露聲明

本專案在開發過程中使用了 AI 輔助，包括 OpenAI Codex 與 Anthropic Claude。

## 授權條款

本專案採用 [GNU 通用公共授權條款第 3 版](LICENSE) 授權。
