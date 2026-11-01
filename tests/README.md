# FYCalendar のテスト

対象は .NET 8 / EF Core 8.0.11。テスト基盤と既存機能のスモークテストを用意しています。
予定登録・繰り返し展開など未実装の機能のテストは、各機能の実装と一緒に追加します。

## 構成

| 場所 | 用途 |
| --- | --- |
| `App.Tests/Unit` | DBを使わない単体テスト |
| `App.Tests/Integration` | SQLiteインメモリDBによる結合テスト |
| `App.Tests/Infrastructure` | 単純なテスト用のEF Core InMemory設定と利用例 |
| `App.E2ETests` | Playwright / Chromium の画面テスト |
| `tests/Support` | 両テストプロジェクトで共有するDB・アプリ起動処理 |

テストフレームワークはxUnit、`App.Tests`のアサーションはFluentAssertionsです。
EF Coreのパッケージ版は既存のAppと8.0.11で統一しています。

## 実行

リポジトリのルート（App.slnがあるディレクトリ）で実行します。

```powershell
dotnet restore App.sln
dotnet test App.Tests/App.Tests.csproj
```

単体テストのみ:

```powershell
dotnet test App.Tests/App.Tests.csproj --filter "Category=Unit"
```

DB結合テストのみ:

```powershell
dotnet test App.Tests/App.Tests.csproj --filter "Category=Integration"
```

カバレッジ:

```powershell
dotnet test App.Tests/App.Tests.csproj --collect:"XPlat Code Coverage"
```

Playwrightの初回準備（PowerShell 7の`pwsh`を使用）:

```powershell
dotnet build App.E2ETests/App.E2ETests.csproj
pwsh App.E2ETests/bin/Debug/net8.0/playwright.ps1 install chromium
dotnet test App.E2ETests/App.E2ETests.csproj
```

Release構成でビルドした場合はパスの`Debug`を`Release`に変えてください。
ブラウザーを準備した後は、`dotnet test App.sln`ですべて実行できます。
初回はNuGetパッケージとPlaywrightのブラウザーのダウンロードが必要です。

## SQLiteの使い方

```csharp
await using var database = await SqliteTestDatabase.CreateAsync();
await using var context = database.CreateContext();
// contextを使って登録・検索・制約を検証する
```

`SqliteTestDatabase`ごとに独立したインメモリDBを作成します。同じインスタンスから
作った複数のDbContextは同じDBを参照します。DBの接続は最後まで開いたままにし、
DisposeAsync時に破棄します。テストごとにインスタンスを作ればデータが混ざりません。

初期化には`EnsureCreatedAsync`を使います。既存のSQL Server用MigrationはSQLiteに
適用しません。本番DBへの接続・Migration実行・データ変更は行いません。

`TestAppDbContext`は本番のAppDbContextを継承し、テスト時だけ以下を調整します。

- `dbo`スキーマを除去し、CHECK制約の`N'...'`をSQLiteの文字列リテラルに置換。
- DateTimeOffsetをUTCのticksに変換し、期間条件をSQLで比較可能にする。
  読み戻すオフセットはUTCになるため、元のオフセット文字列は検証対象にしない。
- SQL Serverの`sysdatetimeoffset()`既定値をSQLiteのUTC時刻式に置換。
- RowVersionは8バイトの初期値だけを生成し、同時更新トークンとしての機能は無効化。
  SQL Serverのrowversion自動更新・同時更新制御の検証は別途SQL Server上で行う。

外部キー・一意制約・CHECK制約・トランザクションを確認するテストはSQLiteを使用します。
SQL Server固有の照合順序、長さ制限、型、インデックス、Migrationまで同等になるわけでは
ありません。SQL Serverの動作確認が必要な場合はSQL Server向けの結合テストを追加します。

## InMemoryの使い方

`App.Tests/Infrastructure/InMemoryTestDatabase.cs`は、単純な検索など制約・SQLに依存しない
場合だけ使用します。SQLの既定値は生成されないため、必須値はテストデータで明示します。
外部キー、一意性、CHECK制約、トランザクションを保証する用途には使用しません。

## Playwrightの起動方式

E2Eテストは共有の`AppBootstrap`から実際のMVCアプリケーションを起動します。
DB登録だけをSQLiteのTestAppDbContextに差し替え、Kestrelは127.0.0.1の空きポートで待機します。
本番用の接続文字列やSQL Server、手動で起動した開発サーバーは不要です。

ブラウザーはコレクション内で共有し、テストごとに新しいBrowserContextを作るため、
Cookie・localStorageはテスト間で共有しません。DBはE2Eコレクション内で共有されるので、
予定の登録などデータを変更するテストを追加する際は、テストごとのデータ作成・片付けを
行うか、DBとアプリケーションの起動をテスト単位に変更してください。
E2Eコレクションの並列実行は無効にしています。失敗を隠すための自動スキップは行いません。

## 初期テスト

- 既存HomeControllerのビューと設定値の受け渡し。
- EF Core InMemoryを使った単純なユーザー検索。
- SQLiteの既定値、別DbContextへの保存、DB間の独立性。
- 外部キー・年度の一意性・所有カレンダー必須・期間のCHECK制約。
- 1分間の予定の保存、タイムゾーンの異なる期間検索、隣接時間の除外。
- トランザクションのロールバック。
- Chromiumでホーム画面を表示し、Privacyへ遷移。

期間検索はDBの比較処理を検証するもので、予定登録サービスの重複警告機能を実装したもの
ではありません。警告や確定処理のテストはその機能の実装時に追加してください。

## 参考

- [EF Core: SQLiteインメモリDBによるテスト](https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database#sqlite-in-memory)
- [EF Core: SQLiteプロバイダーの制限](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations)
- [Playwright for .NET: テストランナー](https://playwright.dev/dotnet/docs/test-runners)
