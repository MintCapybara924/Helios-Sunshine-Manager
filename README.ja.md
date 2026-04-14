# Sunshine Multi-Instance Manager

単一の Windows マシン上で複数の [Sunshine](https://github.com/LizardByte/Sunshine)（およびそのフォーク）ストリーミングインスタンスを管理するモダンな WPF アプリケーションです。

**言語 / README**: [English](README.md) | [繁體中文](README.zh-TW.md) | [简体中文](README.zh-CN.md) | 日本語

## 機能

- **マルチインスタンス管理** - 独立したストリーミングインスタンスの作成、編集、クローン、削除が可能。各インスタンスは固有のポート、設定ファイル、資格情報を持ちます。
- **複数ブランチ対応** - [Sunshine](https://github.com/LizardByte/Sunshine)、[Apollo](https://github.com/ClassicOldSong/Apollo)、[Vibeshine](https://github.com/Nonary/vibeshine)、[Vibepollo](https://github.com/Nonary/Vibepollo) に対応。
- **サービスベース起動** - インスタンスは LocalSystem として実行されるバックグラウンド Windows Service 経由で起動され、UAC プロンプトや Windows ログイン画面を含むデスクトップ全体のキャプチャが可能です。
- **ブート時自動起動** - Windows タスクスケジューラと連携し、ユーザーログオン時にマネージャーを自動起動。
- **ディスプレイ変更検出** - ディスプレイ構成の変更（解像度、モニター数）を検出し、インスタンスを自動再起動。スマートデバウンスと UAC 認識機能付き。
- **インスタンス別オーディオルーティング** - 各インスタンスに個別のオーディオ出力デバイスを割り当て可能。
- **音量同期** - システムデフォルトのオーディオ音量をリアルタイムで監視し、各インスタンスに設定されたオーディオデバイスに同期。
- **自動アップデート** - GitHub Releases から対応ブランチのアップデートを確認・インストール。プレリリース切り替えと進捗追跡に対応。
- **モダン UI** - Fluent/WinUI スタイルのインターフェース。ダーク/ライトテーマ切り替え、システムトレイ統合、リアルタイムログビューアを搭載。

## システム要件

- **OS**: Windows 10 / 11 (x64)
- **ランタイム**: [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)
- **権限**: 管理者（サービス登録とインスタンス起動に必要）
- **Sunshine**: 対応ブランチのいずれかがインストール済みであること（Sunshine、Apollo、Vibeshine、または Vibepollo）

## インストール

1. 最新リリースから `SunshineMultiInstanceManagerSetup.exe` をダウンロード。
2. インストーラーを実行し、画面の指示に従ってインストールを完了。
3. `SunshineMultiInstanceManager.exe` を管理者として起動。

初回起動時、アプリケーションは自動的に：
- **Spawner Service** を Windows サービス（LocalSystem）として登録。
- サービスを起動し、それを通じてインスタンスを管理。

手動でのサービス設定は不要です。

## 使い方

1. **インスタンスを追加** - 追加ボタンをクリックし、製品ブランチを選択、名前とポートを設定。
2. **設定** - 各インスタンスのオーディオデバイス、ヘッドレスモード、追加引数などのオプションを調整。
3. **起動** - インスタンスを有効にして起動（または全て起動）をクリック。Spawner Service がフルシステム権限で各インスタンスを起動します。
4. **接続** - [Moonlight](https://moonlight-stream.org/) または互換性のあるクライアントで設定したポートに接続。
5. **Web UI** - インスタンス横のリンクアイコンをクリックして Web 設定パネルを開く。

## ソースからビルド

```bash
# アプリケーションをビルド
dotnet build src/SunshineMultiInstanceManager.App/SunshineMultiInstanceManager.App.csproj

# 発行（Spawner Service を自動的に含む）
dotnet publish src/SunshineMultiInstanceManager.App/SunshineMultiInstanceManager.App.csproj -p:PublishProfile=win-x64-fd
```

発行出力は `publish/win-x64-fd/` にあり、メインアプリケーションと `service/` サブディレクトリ内の Spawner Service が含まれます。

## アーキテクチャ

```
SunshineMultiInstanceManager.App      WPF デスクトップアプリケーション（UI + ローカル制御）
SunshineMultiInstanceManager.Core     共有ライブラリ（プロセス管理、設定、オーディオ、ディスプレイ、アップデート）
SunshineMultiInstanceManager.Spawner  Windows サービス（SYSTEM として実行、Named Pipe コマンドでインスタンスを起動）
```

App は Named Pipe を介して Spawner Service と通信します。Service はユーザーの対話型セッションに割り当てられた SYSTEM トークンを使用して Sunshine インスタンスを起動し、セキュアデスクトップ（UAC およびログイン画面）のキャプチャを可能にします — 標準の Sunshine サービスインストールと同等の機能です。

## 既知の制限事項

> **Vibeshine / Vibepollo インストーラーの競合**：本マネージャーは複数の Sunshine 系ブランチの共存を目的としていますが、Vibeshine および Vibepollo のインストーラーは、他にインストールされている Sunshine 系ブランチのアンインストールを強制的に求めます。同意しない場合、インストールを続行できません。Vibe 系を先にインストールし、その後 Sunshine や Apollo をインストールすれば一時的に共存可能ですが、Vibe 系のアップデート時に再び他のブランチの削除を求められます。

## 免責事項

本プロジェクトは主に個人利用を目的として開発されており、すべての機能があらゆる環境や構成で正常に動作することを保証するものではありません。ご利用は自己責任でお願いいたします。

## インスピレーション

本プロジェクトは、Apollo のマルチインスタンスランチャーである [Apollo Fleet Launcher](https://github.com/drajabr/Apollo-Fleet-Launcher) に着想を得ています。Sunshine Multi-Instance Manager は、そのコンセプトを基に、より多くの Sunshine 系ブランチへの対応を追加しています。

## AI に関する開示

本プロジェクトは、OpenAI Codex および Anthropic Claude を含む AI の支援を受けて開発されました。

## ライセンス

本プロジェクトは [GNU General Public License v3.0](LICENSE) の下でライセンスされています。
