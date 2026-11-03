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

## 命名

テストクラス名は対象との対応が分かる英語名、`[Fact]`・`[Theory]` のメソッド名は
条件と期待結果が伝わる簡潔な日本語にします。例：`FiscalYearServiceTests.未登録の年度はnullを返す`。
補助メソッドやインターフェースで定められた名前は英語とし、Traitの`Category`と
`Unit`・`Integration`・`E2E`も英語を維持します。カテゴリやクラス名での絞り込みを利用できます。

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
テスト用のログ出力先はコンソールのみとし、Data Protectionの鍵はメモリ内で保持します。
Windowsイベントログやユーザープロファイルへの書き込み権限に依存せず起動できます。

ブラウザーはコレクション内で共有し、テストごとに新しいBrowserContextを作るため、
Cookie・localStorageはテスト間で共有しません。DBはE2Eコレクション内で共有されるので、
予定の登録などデータを変更するテストを追加する際は、テストごとのデータ作成・片付けを
行うか、DBとアプリケーションの起動をテスト単位に変更してください。
E2Eコレクションの並列実行は無効にしています。失敗を隠すための自動スキップは行いません。
年度画面の`FiscalYearPageTests`は各テストの前後でSQLite内の年度データを削除し、
テストごとに独立したBrowserContextを使用します。

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

## 年度計算

`App/Domain/Services/FiscalYearCalculator.cs`はDBに依存せず、4月1日から翌年3月31日までを
1年度として計算します。`GetPeriod(year)`が返す`StartsOn`と`EndsOn`は両端を含みます。
`GetYear(date)`には対象カレンダーの現地日付を`DateOnly`で渡します。

```csharp
using App.Domain.Services;

var period = FiscalYearCalculator.GetPeriod(2026);
// StartsOn: 2026-04-01 / EndsOn: 2027-03-31
var year = FiscalYearCalculator.GetYear(new DateOnly(2027, 3, 31));
// 2026
```

開始日と終了日を両方表現できる1～9998年度を対象とし、範囲外は
`ArgumentOutOfRangeException`にします。所属年度の判定も同じ範囲に合わせます。
`App.Tests/Unit/FiscalYearCalculatorTests.cs`で年度境界・暦年境界・うるう日・範囲外を検証します。

```powershell
dotnet test App.Tests/App.Tests.csproj --filter "FullyQualifiedName~FiscalYearCalculatorTests"
```

エンティティの年度プロパティは`FiscalYear.Year`です。既存DB列`FiscalYear`への対応を維持し、
`RenameFiscalYearProperty` Migrationはモデルの名前変更を記録します（スキーマ変更なし）。
年度管理画面と今後の管理機能は`MaintainController`にまとめます。

## 年度の登録・取得

`App/Domain/Services/FiscalYearService.cs`をScopedでDIに登録しています。
`MaintainController`に注入して年度管理画面から使用しています。

- `CreateAsync(year, name)`：年度計算で期間を設定し、状態`open`で保存します。
  名称は前後の空白を除去し、必須・50文字以内とします。
- `GetAllAsync()`：新しい年度から順に一覧を取得します。
- `FindByYearAsync(year)`：指定年度を取得し、未登録なら`null`を返します。

年度は1～9998に限定し、入力不正は`ArgumentException`または`ArgumentOutOfRangeException`、
重複は`App.Domain.Exceptions.DuplicateFiscalYearException`（`Year`プロパティ付き）で通知します。
事前チェック後の競合はDBの一意制約で検出し、同じ年度の存在を確認して重複例外に変換します。
重複以外の保存エラーは`DbUpdateException`として伝えます。
取得した一覧・年度は読み取り用で、戻り値を変更するだけではDBへ保存されません。
各メソッドは`CancellationToken`を受け取ります。

`App.Tests/Integration/FiscalYearServiceTests.cs`はSQLiteのインメモリDBを使用し、
別コンテキストからの読み取り、入力検証、通常の重複、事前チェック後の競合、
重複以外の保存エラー、取得順序、キャンセル、DIでの利用を検証します。

```powershell
dotnet test App.Tests/App.Tests.csproj --filter "FullyQualifiedName~FiscalYearServiceTests"
```

## 年度管理画面

アプリを起動し、上部の「年度管理」から一覧・登録画面を開きます。

- `/Maintain/FiscalYears`：年度・名称・開始日・終了日・状態を、新しい年度から順に表示します。
- `/Maintain/CreateFiscalYear`：年度と名称を入力して登録します。

登録画面の初期値は、日本時間（Asia/Tokyo）で判定した現在年度と「西暦年＋年度」です。
日時はDIに登録した`TimeProvider`から取得し、テストでは年度境界を固定日時で確認します。
年度を変更すると期間と初期名称が連動します。独自に入力した名称は保持します。
開始日・終了日・状態はサーバーで設定し、画面からは指定しません。

入力エラー・重複時は入力内容を保って登録画面に表示し、成功時は一覧へリダイレクトします。
登録フォームは偽造防止トークンを使用します。JavaScriptの有無にかかわらず、
サーバーで必須・範囲・長さの検証を行います。

- `App.Tests/Unit/CreateFiscalYearViewModelTests.cs`：入力モデルの検証。
- `App.Tests/Integration/MaintainControllerTests.cs`：日本時間の年度境界、一覧、保存、エラー表示。
- `App.E2ETests/FiscalYearPageTests.cs`：ナビゲーション、登録、再読み込み、名称の保持、
  重複、JavaScript無効時の検証、偽造防止トークン。

```powershell
dotnet test App.Tests/App.Tests.csproj
dotnet test App.E2ETests/App.E2ETests.csproj
```

## 参考

- [EF Core: SQLiteインメモリDBによるテスト](https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database#sqlite-in-memory)
- [EF Core: SQLiteプロバイダーの制限](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations)
- [Playwright for .NET: テストランナー](https://playwright.dev/dotnet/docs/test-runners)
