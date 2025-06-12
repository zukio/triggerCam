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
    "DirectShowLib" = "1.0.0"
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
