# FYCalendar テストプロジェクト追加差分

指定された`C:\Users\hs.SONE\source\repos\hakga\FYCalendar`を読み取り、作業用コピーに
テスト基盤を実装しました。元のリポジトリは、このセッションの書き込み許可範囲外のため
変更していません。

## 取り込み

`Apply-FYCalendarTestSetup.ps1`と`FYCalendar-test-setup.patch`を同じディレクトリに置き、
通常のPowerShellから次を実行してください。適用前に`git apply --check`で検査します。
既存の内容と合わなければ適用を中止し、強制上書きは行いません。

```powershell
& "C:\Users\hs.SONE\Documents\Codex\2026-09-20\realtime-voice-chat\outputs\Apply-FYCalendarTestSetup.ps1" -CheckOnly
& "C:\Users\hs.SONE\Documents\Codex\2026-09-20\realtime-voice-chat\outputs\Apply-FYCalendarTestSetup.ps1"
```

スクリプトの代わりに直接適用する場合:

```powershell
git -C "C:\Users\hs.SONE\source\repos\hakga\FYCalendar" apply "C:\Users\hs.SONE\Documents\Codex\2026-09-20\realtime-voice-chat\outputs\FYCalendar-test-setup.patch"
```

## 追加内容

- `App.Tests`: xUnit、FluentAssertions、SQLiteインメモリDB、EF Core InMemory、カバレッジ。
- `App.E2ETests`: xUnit、Playwright、Chromium、SQLiteを使う自動起動サーバー。
- `tests/Support`: SQL Server向けモデルをSQLiteで利用するテスト専用の変換設定。
- 既存HomeController、DB制約・保存・検索、ホーム画面・Privacy遷移の初期テスト。
- ルートとApp配下の両方のApp.slnへのテストプロジェクト登録。
- `App/Hosting/AppBootstrap.cs`: 通常起動とE2Eで同じMVC構成を使用。
- `App/Program.cs`: 共有起動処理の呼び出しに変更。本番DB登録は従来のSQL Server。
- `tests/README.md`: 実行、カバレッジ、DBの使い分け、SQLiteの制約。

既存のエンティティ、SQL Server用Migration、App.csprojのパッケージ版は変更しません。
EF Coreパッケージは既存の8.0.11に合わせています。

## 適用後の実行

```powershell
Set-Location "C:\Users\hs.SONE\source\repos\hakga\FYCalendar"
dotnet restore App.sln
dotnet test App.Tests/App.Tests.csproj
dotnet build App.E2ETests/App.E2ETests.csproj
pwsh App.E2ETests/bin/Debug/net8.0/playwright.ps1 install chromium
dotnet test App.E2ETests/App.E2ETests.csproj
```

Playwrightの準備にはPowerShell 7の`pwsh`とブラウザーダウンロードが必要です。
E2Eは空きポートでテスト対象を自動起動し、手動のサーバー起動やSQL Serverへの接続は不要です。

## このセッションでの検証

- C# 12として41ファイルの構文検査: エラーなし。
- テストプロジェクトのXMLとProjectReferenceの参照先: 検査済み。
- 差分の空白検査と、指定された元リポジトリへの`git apply --check`: 検査済み。
- NuGet復元: ユーザーNuGet.Configへのアクセス制限で失敗。
- ビルド、xUnit実行、Playwright実行: 復元ができないため未確認。

構文検査はコンパイル・テストの成功を保証するものではありません。
初回取り込み後に上記のビルド・テストを実行してください。
