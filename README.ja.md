# Helios

単一の Windows マシン上で複数の [Sunshine](https://github.com/LizardByte/Sunshine)（およびそのフォーク）ストリーミングインスタンスを管理するモダンな WPF アプリケーションです。

**言語 / README**: [English](README.md) | [繁體中文](README.zh-TW.md) | [简体中文](README.zh-CN.md) | 日本語

## 機能

- **マルチインスタンス管理** - 独立したストリーミングインスタンスを作成・編集・複製・削除できます。各インスタンスは個別のポートと設定ディレクトリを持ちます。
- **複数ブランチ対応** - [Sunshine](https://github.com/LizardByte/Sunshine)、[Apollo](https://github.com/ClassicOldSong/Apollo)、[Vibeshine](https://github.com/Nonary/vibeshine)、[Vibepollo](https://github.com/Nonary/Vibepollo) を管理できます。
- **サービスベース実行** - インスタンスはバックグラウンド Windows サービス（LocalSystem）で制御され、UAC やサインイン画面などのセキュアデスクトップ環境に対応します。
- **インスタンス単位の実行操作** - 各インスタンスで Start / Stop / Open Web UI を実行でき、Start All / Stop All による一括操作にも対応します。
- **インスタンス別オーディオルーティング** - 各インスタンスに個別のオーディオ出力デバイスを割り当てできます。
- **音量同期** - システム音量を管理対象インスタンスへ同期する機能を任意で有効化できます。
- **アプリ内リリース取得とインストール** - GitHub Releases を確認し、対応ブランチの最新インストーラー（安定版またはプレリリース）をダウンロードしてインストールできます。  
	これは**ユーザー手動実行**の更新フローであり、バックグラウンド自動更新ではありません。
- **モダンなデスクトップ UX** - Fluent スタイル UI、ライト/ダークテーマ、システムトレイ連携、ランタイムログビューアを提供します。

## システム要件

- **OS**: Windows 10 / 11 (x64)
- **ランタイム**: [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)
- **権限**: 管理者（サービス登録とインスタンス起動に必要）
- **Sunshine**: 対応ブランチのいずれかがインストール済みであること（Sunshine、Apollo、Vibeshine、または Vibepollo）

## インストール

1. 最新リリースから `HeliosSetup.exe` をダウンロード。
2. インストーラーを実行し、画面の指示に従ってインストールを完了。
3. `Helios.exe` を管理者として起動。

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
dotnet build src/SunshineMultiInstanceManager.App/Helios.App.csproj

# 発行（Spawner Service を自動的に含む）
dotnet publish src/SunshineMultiInstanceManager.App/Helios.App.csproj -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -o publish/win-x64-fd
```

発行出力は `publish/win-x64-fd/` にあり、メインアプリケーションと `service/` サブディレクトリ内の Spawner Service が含まれます。

## アーキテクチャ

```
Helios.App      WPF デスクトップアプリケーション（UI + ローカル制御）
Helios.Core     共有ライブラリ（プロセス管理、設定、オーディオ、ディスプレイ、アップデート）
Helios.Spawner  Windows サービス（SYSTEM として実行、Named Pipe コマンドでインスタンスを起動）
```

App は Named Pipe を介して Spawner Service と通信します。Service はユーザーの対話型セッションに割り当てられた SYSTEM トークンを使用して Sunshine インスタンスを起動し、セキュアデスクトップ（UAC およびログイン画面）のキャプチャを可能にします — 標準の Sunshine サービスインストールと同等の機能です。

## 既知の制限事項

> **Vibeshine / Vibepollo インストーラーの競合**：本マネージャーは複数の Sunshine 系ブランチの共存を目的としていますが、Vibeshine および Vibepollo のインストーラーは、他にインストールされている Sunshine 系ブランチのアンインストールを強制的に求めます。同意しない場合、インストールを続行できません。Vibe 系を先にインストールし、その後 Sunshine や Apollo をインストールすれば一時的に共存可能ですが、Vibe 系のアップデート時に再び他のブランチの削除を求められます。

## 免責事項

本プロジェクトは主に個人利用を目的として開発されており、すべての機能があらゆる環境や構成で正常に動作することを保証するものではありません。ご利用は自己責任でお願いいたします。

## インスピレーション

本プロジェクトは、Apollo のマルチインスタンスランチャーである [Apollo Fleet Launcher](https://github.com/drajabr/Apollo-Fleet-Launcher) に着想を得ています。Helios は、そのコンセプトを基に、より多くの Sunshine 系ブランチへの対応を追加しています。

## AI に関する開示

本プロジェクトは、OpenAI Codex および Anthropic Claude を含む AI の支援を受けて開発されました。

## ライセンス

本プロジェクトは [GNU General Public License v3.0](LICENSE) の下でライセンスされています。
