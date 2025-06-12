# triggerCam - シリアル通信撮影タスクトレイアプリ

**バージョン**: 1.0.0
**更新日**: 2025年6月12日

シリアル通信で受信したトリガーに応じて、PC接続のWebカメラで**静止画**または**動画**を撮影・保存し、さらに保存完了時に**UDPで通知を送信する**タスクトレイ常駐型ユーティリティです。

## 概要

triggerCamは、シリアル通信（COMポート）経由で外部デバイスからのトリガー信号を受け取り、接続されたWebカメラで静止画撮影や動画録画を自動的に行うWindows用タスクトレイアプリケーションです。撮影完了時には指定したIPアドレス・ポートにUDP通知を送信することができます。

## 主な機能

- シリアル通信による遠隔トリガー
- 静止画撮影（PNG/JPEG形式）
- 動画録画（MP4形式、H.264エンコード）
- 撮影完了時のUDP通知
- タスクトレイ常駐型の軽量設計
- 多重起動防止機能

## 要件定義

``` md

# ■ 要件定義書 (Ver.1.2)

**プロジェクト**: triggerCam シリアル通信撮影タスクトレイアプリ
**更新日**: 2025年6月12日
**作成者**: Zukio

---

## 【目的】

シリアル通信で受信したトリガーに応じて、PC接続のWebカメラで**静止画**または**動画**を撮影・保存し、さらに保存完了時に**UDPで通知を送信する**タスクトレイ常駐型ユーティリティをC#で開発する。

---

## 【基本方針】

* 対応する撮影モード：

  * ✅ 静止画（PNG/JPEG）
  * ✅ 動画（MP4, H.264エンコード）
* 撮影制御はトリガーによる

  * `START` → 動画録画開始
  * `STOP` → 動画録画終了
* タスクトレイ常駐アプリとして軽量に動作
* 多重起動禁止（単一インスタンス）

---

## 【機能要件】

### 1. シリアル通信受信

| 機能              | 詳細                                    |
| --------------- | ------------------------------------- |
| COMポート列挙        | 起動時または再読み込みで現在接続されているCOMポート一覧を表示      |
| ボーレート設定         | 9600bps 固定 or GUIで選択（任意）              |
| トリガー文字列指定（自由形式） | START / STOP / SNAP など任意の文字列をGUIで設定可能 |
| 受信処理            | 非同期で受信し、トリガーに対応する動作をキューイング            |

---

### 2. 映像デバイス選択・撮影制御

| 機能            | 詳細                                                             |
| ------------- | -------------------------------------------------------------- |
| カメラデバイス列挙     | OpenCVで取得可能なVideoCaptureデバイスをリスト表示                             |
| プレビュー機能（任意）   | 必須ではないが、プレビュー用Windowを開く機能も実装可能（例: 320x240の確認画面）                |
| 静止画撮影         | トリガー受信時に1枚保存（保存形式: PNG/JPEG）                                   |
| 動画撮影          | STARTで録画開始、STOPで録画終了 → MP4で保存（指定FPS、H.264エンコード）                |
| 保存先パス・ファイル名制御 | GUIで保存ディレクトリを指定し、ファイル名はテンプレート（例: coaster\_yyyyMMdd\_HHmmss）に従う |
| カメラ未接続時の警告    | 起動時または録画開始時に、カメラ未接続の場合はダイアログで明示的に警告                            |

---

### 3. UDP通知機能

| 機能           | 詳細                               |
| ------------ | -------------------------------- |
| 通知先IP/PORT設定 | GUIからUDP送信先のIPアドレスとポート番号を指定可能    |
| 通知形式         | JSONまたはプレーンテキスト形式を選択可能           |
| 通知タイミング      | 録画完了時 / 静止画保存完了時 に以下のようなメッセージを送信 |

{ "type": "video_saved", "path": "C:/videos/coaster_20250612_113300.mp4" }

---

### 4. タスクトレイ機能

| 機能     | 詳細                                         |
| ------ | ------------------------------------------ |
| 常駐     | Windowsのタスクトレイに常駐。タスクバー表示は行わない             |
| メニュー構成 | 設定画面／ログ画面／撮影開始・停止／アプリ終了                    |
| ログ表示   | シリアル受信や撮影・保存・通知などの履歴をGUIで確認可能（ログファイル保存も検討） |

---

### 5. 多重起動制御

| 機能       | 詳細                                             |
| -------- | ---------------------------------------------- |
| 単一インスタンス | 起動時に既存のプロセスをチェックし、多重起動をブロック。必要に応じて既存インスタンスを前面化 |

---

## 【非機能要件】

| 項目         | 内容                                                               |
| ---------- | ---------------------------------------------------------------- |
| 対応OS       | Windows 10 / 11（64bit）                                           |
| 開発言語       | **C# (.NET 6〜8)**（WPFベース）                                        |
| カメラ制御ライブラリ | **OpenCvSharp4** または **AForge.NET / MediaFoundation**（性能比較のうえ選定） |
| 実行形式       | `.exe`（ClickOnce / インストーラ形式の提供も視野）                               |
| メモリ使用量     | 常駐時に50MB以下を目安                                                    |
| エラー処理      | COMポート切断、カメラ未検出時は自動復旧または警告表示で回避                                  |

---

## 【GUI要素一覧】

| 要素          | 内容                             |
| ----------- | ------------------------------ |
| COMポート選択    | 現在のシリアルポート一覧を表示、選択ドロップダウン      |
| トリガー文字列     | START / STOP / SNAP など任意指定     |
| カメラ選択       | 使用するカメラの選択（デバイス名表示）            |
| 撮影モード切替     | 静止画 / 動画（トグルまたはラジオボタン）         |
| 保存先フォルダ指定   | ファイル保存先のディレクトリパスをGUIで選択可能      |
| ファイル名テンプレート | coaster\_{yyyyMMdd\_HHmmss} など |
| UDP送信先設定    | IP\:PORT + 通知形式（JSON/プレーン）     |

---

## 【警告ダイアログ条件一覧】

| 条件                | 表示される警告文例                               |
| ----------------- | --------------------------------------- |
| カメラ未接続            | 「カメラが見つかりません。デバイスを確認してください。」            |
| COMポート未選択 or 通信不能 | 「シリアル通信ポートが未接続です。設定を確認してください。」          |
| 録画中にSTOPが来ない      | 「録画停止トリガーが一定時間内に検出されません。録画を強制終了しますか？」など |

---

## 【今後の拡張性】

* 🔜 OCR処理との連携（文字抽出・アルファ画像生成の自動化）
* 🔜 ファイルアップロード（FTP/SFTP/Google Driveなど）
* 🔜 GUIでの録画ログ閲覧・フィルタリング
* 🔜 WebSocket連携によるリアルタイムステータス監視
* 🔜 AIベースの不正トリガー検出（例：誤反応対策）

```

## ダウンロード

最新のリリース版は[こちら](https://github.com/Zukio/triggerCam/releases/latest)からダウンロードできます。

ダウンロードした ZIP ファイルを解凍し、`triggerCam.exe`を実行してください。

## バージョン（ブランチ）

- master: 公開ブランチ
- dev: 開発用／編集中の可能性があります【！】

## 使用方法

### 1. 基本メニュー

アプリケーションを起動すると、タスクトレイにアイコンが表示されます。このアイコンをクリックすると、以下のメニュー項目が展開します。

![Screenshot of the application](Assets/contextMenu.png)

![Screenshot of the application](Assets/trayIcon.png)

- **COMポート選択**: PC に接続されているシリアルポートの一覧が表示されます。
- **ボーレート設定**: シリアル通信のボーレートを設定できます（デフォルト: 9600bps）。
- **トリガー文字列**: 録画開始・停止・静止画撮影などのトリガー文字列を設定できます。
- **カメラ選択**: 使用するカメラデバイスを選択できます。
- **撮影モード**: 静止画撮影 / 動画録画モードを切り替えられます。
- **撮影データの保存場所**: 撮影ファイルの保存場所を設定できます。
- **撮影データを開く**: 保存場所をエクスプローラで開きます。
- **UDP送信先設定**: 通知先のUDPアドレスとポート番号を設定できます。
- **保存ボタン**: 変更した設定を保存します。
- **Exit ボタン**: アプリケーションを終了します。

### 2. UDP 通信（状態通知）

撮影の状態の変化を検知すると、以下のメッセージが UDP で送信されます。

- カメラ接続: `Connected {deviceName}`
- カメラ切断: `disConnected`
- 撮影開始: `RecStart`
- 静止画撮影完了: `SnapSaved {imagePath}`
- 動画撮影停止: `RecStop {videoPath}`

### 3. 外部からのコマンド受信

外部アプリケーションからUDPを通じてコマンドを受信し、カメラ操作や撮影を行うことができます。コマンドはJSON形式で送信します。

#### コマンド一覧

| コマンド | 説明 | パラメータ | レスポンス |
|---------|------|-----------|-----------|
| `exit` | アプリケーションを終了する | なし | `{"status":"success","message":"Application will exit"}` |
| `camera_list` | 利用可能なカメラの一覧を取得 | なし | `{"status":"success","data":{"cameras":["Camera1","Camera2"]}}` |
| `camera_select` | 使用するカメラを選択 | カメラ名 | `{"status":"success","message":"Camera selected"}` |
| `snap` | 静止画を撮影 | ファイル名（省略可） | `{"status":"success","data":{"path":"撮影ファイルパス"}}` |
| `rec_start` | 録画を開始する | ファイル名（省略可） | `{"status":"success","message":"Recording started"}` |
| `rec_stop` | 録画を停止する | なし | `{"status":"success","data":{"path":"録画ファイルパス"}}` |

#### エラーレスポンス

コマンド実行時にエラーが発生した場合、以下のような形式でエラー情報が返されます：

```json
{
  "status": "error",
  "message": "エラーの詳細メッセージ"
}
```

主なエラーケース：

- カメラ操作失敗: デバイスへのアクセスエラーなど
- 撮影開始失敗: ディスク容量不足、権限エラーなど
- 既に録画中: 録画開始コマンドを重複実行
- 録画していない: 録画停止コマンドを録画していない状態で実行
- 不正なコマンド: 未知のコマンドや不正なパラメータ

#### コマンド送信例

```json
{
  "command": "camera_select",
  "param": "HD WebCam"
}
```

```json
{
  "command": "rec_start",
  "param": {
    "fileName": "video_20250612",
    "path": "C:/CustomVideos"
  }
}
```

```json
{
  "command": "snap",
  "param": "snapshot_20250612"  // ファイル名のみ指定（パスはデフォルト）
}
```

### 4. 撮影機能

`rec_start`コマンドで録画を開始し、`rec_stop`コマンドで録画を停止できます。録画ファイルはMP4形式で保存され、以下の優先順位で保存場所が決定されます：

1. UDPのrec_startコマンドで指定されたパス
2. 起動時引数（/videosDir）で指定されたパス
3. 設定ファイルに保存された値
4. デフォルト値（実行パスのVideosフォルダ）

静止画の場合は`snap`コマンドを使用し、PNG/JPEG形式で保存されます。

ファイル名は以下の方法で指定できます：

- コマンドの`fileName`パラメータで指定
- コマンドのパラメータを文字列で直接指定
- 省略時は現在の日時（yyyyMMdd_HHmmss形式）を使用

### 5. 起動時引数

アプリケーションの起動時に、以下の引数で監視対象のCOMポートやカメラデバイス、通信用のUDPアドレス、ファイルの保存場所を指定できます。

- `/comPort="COM3"` - 使用するシリアルポート
- `/baudRate="9600"` - シリアル通信のボーレート
- `/cameraName="your-camera-name"` - 使用するカメラデバイス名
- `/udpTo="127.0.0.1:23456"` - 状態通知の送信先UDPアドレス
- `/udpListen="127.0.0.1:10001"` - コマンド受信用のUDPアドレス
- `/videosDir="C:/Videos"` - 撮影ファイルの保存場所

### 6. 多重起動制御

アプリケーションは多重起動を制御します。すでに起動中の場合、新たなインスタンスの起動は阻止されます。

### 7. ログ

実行 exe と同じディレクトリに、`ConsoleLogs`というディレクトリが自動で作成されます。アプリケーションの起動毎に、このディレクトリ内に日付をファイル名とするログファイルが作成され、各種情報やエラーメッセージが記録されます。

### 8. エラーハンドリング

アプリケーションは以下のような状況で適切なエラーハンドリングを実装しています：

- カメラデバイスの切断/再接続
- 撮影ファイルの保存失敗
- シリアル通信の切断/再接続
- UDPコマンドの不正な形式
- メモリリソースの管理
- ファイルシステムの操作エラー

エラーが発生した場合：

1. エラーログが記録されます
2. UDPコマンドの場合はエラーレスポンスが返されます
3. UIに適切なエラーメッセージが表示されます
4. 可能な場合は自動的にリカバリを試みます

### システム要件

- **オペレーティングシステム**: Windows 10/11 (64bit)
- **開発環境**: Visual Studio 2022
- **フレームワーク**: .NET 6.0, Windows Forms (WinForms)

## 開発者向け情報

このプロジェクトは Windows Forms（WinForms）アプリケーションとして開発されました。
WinFormsは、Windowsデスクトップアプリケーションを作成するための.NETフレームワークで、
特にシステムトレイアプリケーションの開発に適しています。

開発環境は Visual Studio 2022 です。

### 開発環境のセットアップ

1. プロジェクトをカスタマイズするには、このリポジトリをクローンまたはダウンロードします。
2. Visual Studio 2022 でプロジェクトを開きます。
3. 必要な依存関係や NuGet パッケージをインストールします。

   ```powershell
   # NuGet パッケージのインストール
   Install-Package NAudio -Version 2.2.1
   Install-Package OpenCvSharp4 -Version 4.9.0
   Install-Package OpenCvSharp4.runtime.win -Version 4.9.0
   Install-Package System.IO.Ports -Version 8.0.0
   ```

4. ビルドして実行します。

### セットアップスクリプト

開発環境を素早くセットアップするには、以下のPowerShellスクリプトを`setup.ps1`として保存して実行できます：

```powershell
# triggerCam セットアップスクリプト

# 実行ポリシーの確認と変更
$policy = Get-ExecutionPolicy
if ($policy -ne "RemoteSigned" -and $policy -ne "Unrestricted") {
    Write-Host "ExecutionPolicy を一時的に変更します..." -ForegroundColor Yellow
    Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process -Force
}

# 依存パッケージのインストール確認
$requiredPackages = @{
    "NAudio" = "2.2.1"
    "OpenCvSharp4" = "4.9.0"
    "OpenCvSharp4.runtime.win" = "4.9.0"
    "System.IO.Ports" = "8.0.0"
}

Write-Host "必要なNuGetパッケージを確認しています..." -ForegroundColor Cyan

# プロジェクトファイルを読み込む
$csprojPath = ".\triggerCam.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Host "エラー: $csprojPath が見つかりません。カレントディレクトリをプロジェクトのルートに設定してください。" -ForegroundColor Red
    exit 1
}

$csprojContent = Get-Content $csprojPath -Raw

# 既存のパッケージをチェックし、必要なパッケージをインストール
foreach ($package in $requiredPackages.GetEnumerator()) {
    $packageName = $package.Key
    $version = $package.Value
    
    if (-not ($csprojContent -match "<PackageReference Include=`"$packageName`" Version=`"$version`"")) {
        Write-Host "$packageName バージョン $version をインストールしています..." -ForegroundColor Yellow
        dotnet add package $packageName -v $version
    } else {
        Write-Host "$packageName バージョン $version は既にインストールされています" -ForegroundColor Green
    }
}

# ディレクトリ構造の確認と作成
$directories = @(
    ".\bin\Debug\net6.0-windows\Videos",
    ".\bin\Debug\net6.0-windows\ConsoleLogs"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        Write-Host "$dir を作成しています..." -ForegroundColor Yellow
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
        Write-Host "作成完了: $dir" -ForegroundColor Green
    } else {
        Write-Host "$dir は既に存在します" -ForegroundColor Green
    }
}

# プロジェクトのビルド
Write-Host "プロジェクトをビルドしています..." -ForegroundColor Cyan
dotnet build -c Debug

if ($LASTEXITCODE -eq 0) {
    Write-Host "セットアップが完了しました！以下のコマンドでアプリケーションを実行できます：" -ForegroundColor Green
    Write-Host "dotnet run" -ForegroundColor White -BackgroundColor DarkGreen
} else {
    Write-Host "ビルド中にエラーが発生しました。エラーを修正してから再度試してください。" -ForegroundColor Red
}
```

このスクリプトを実行するには：

```powershell
.\setup.ps1
```

### 貢献

貢献は大歓迎です！バグ報告、新機能の提案など、どうぞお気軽に Issue を開いてください。
