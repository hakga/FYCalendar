# Data FirstからCode Firstへの移行手順

## 1. 目的

既存のSQL Serverスキーマを出発点としてEF CoreのEntityとDbContextを生成し、その後のスキーマ管理を **Code First + EF Core Migrations** へ切り替えるための手順である。

この手順では、移行後の正（Source of Truth）を次のように定める。

| 項目 | 移行前 | 移行後 |
|---|---|---|
| スキーマの正 | DDL SQL／実DB | Entity／DbContext／Migration |
| Entity生成 | DBからScaffold | コードを直接編集 |
| DB変更 | SQLを直接適用 | Migrationを生成して適用 |
| 変更履歴 | SQLファイル | `Migrations`ディレクトリ |

EF Core公式ドキュメントでも、EFモデルを正にする場合はMigrations、DBスキーマを正にする場合はReverse Engineeringを使用する、と整理されている。

- [Managing Database Schemas](https://learn.microsoft.com/en-us/ef/core/managing-schemas/)
- [Reverse Engineering](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding)
- [Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

## 2. 全体の流れ

```text
既存DDL／既存DB
    ↓
検証用DBへスキーマを再現
    ↓
Entity／DbContextを一度だけScaffold
    ↓
Scaffoldで欠落した制約・DB機能を調査してコードへ補完
    ↓
InitialCreate Migrationを生成
    ↓
生成SQLと元DDLを比較
    ↓
開発DBをMigration管理へ切り替え
    ↓
以後はEntity／DbContext変更 → Migration生成 → DB更新
```

重要なのは、**Scaffold直後にInitialCreateを作らないこと**である。Scaffoldで取り込まれなかったCHECK制約、トリガー、シーケンス、特殊なインデックスなどを先に確認し、必要なものをEFモデルへ補完する。

## 3. 前提条件

本プロジェクトで使用した構成は次のとおり。

- .NET 8
- EF Core 8.0.11
- SQL Server LocalDB
- プロジェクト: `App/App.csproj`
- DbContext: `App.Data.AppDbContext`
- 接続文字列名: `ConnectionStrings:AppConnection`
- 開発DB: `FYCalendarDev`
- EF CLI: リポジトリローカルツール

必要なNuGetパッケージ:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

作業前に次を行う。

1. 既存DBに必要なデータがある場合はバックアップする。
2. 作業用ブランチを作成する。
3. DDL、DB、アプリが同じスキーマを表しているか確認する。
4. 接続先DB名を明確にする。

## 4. 既存スキーマを検証用DBへ再現する

### 4.1 LocalDBの確認

```powershell
SqlLocalDB info MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
```

接続確認:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C `
  -Q "SELECT @@SERVERNAME AS ServerName"
```

インスタンス自体が破損しており、開発DBを失っても問題ない場合は再作成する。

```powershell
SqlLocalDB stop MSSQLLocalDB -k
SqlLocalDB delete MSSQLLocalDB
SqlLocalDB create MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
SqlLocalDB info MSSQLLocalDB
```

LocalDBはWindowsユーザー単位で管理されるため、アプリやコマンドを実行するユーザーと、インスタンスを作成したユーザーを一致させる。

### 4.2 UTF-8のSQLファイルを実行する

SQLファイルに日本語が含まれる場合は、次のどちらかを使用する。

- ファイルを`UTF-8 with BOM`で保存する。
- `sqlcmd`に`-f 65001`を指定する。

VS CodeでBOMを付ける場合は、ステータスバーの`UTF-8`をクリックし、`エンコード付きで保存`、`UTF-8 with BOM`の順に選択する。

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -b -r 1 `
  -f 65001 `
  -d FYCalendarDev `
  -i .\database\legacy\001_initial.sql
```

文字コードを誤ると、日本語コメントの一部がShift-JISのマルチバイト文字として解釈され、改行が失われることがある。今回、次の列定義が直前の`--`コメントへ取り込まれた。

```sql
WeekOfMonth SMALLINT NULL,
```

その結果、後続のCHECK制約で次のエラーが発生した。

```text
Invalid column name 'WeekOfMonth'.
```

### 4.3 途中失敗したDBの扱い

DDLは`GO`単位で適用されるため、途中でエラーになると一部のテーブルだけが残る。その状態でDDLを再実行すると、次のエラーになる。

```text
There is already an object named 'Users' in the database.
```

開発データが不要ならDBを作り直す。

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -b -d master `
  -Q "IF DB_ID(N'FYCalendarDev') IS NOT NULL BEGIN ALTER DATABASE [FYCalendarDev] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [FYCalendarDev]; END; CREATE DATABASE [FYCalendarDev];"
```

## 5. DBからEntityとDbContextを一度だけ生成する

`App`ディレクトリで実行する。

```powershell
dotnet tool restore

dotnet tool run dotnet-ef dbcontext scaffold `
  "Name=ConnectionStrings:AppConnection" `
  Microsoft.EntityFrameworkCore.SqlServer `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext `
  --context-dir Data `
  --output-dir Data\Entities `
  --namespace App.Data.Entities `
  --context-namespace App.Data `
  --no-onconfiguring `
  --schema dbo `
  --use-database-names `
  --force
```

生成先:

```text
App/Data/AppDbContext.cs
App/Data/Entities/*.cs
```

生成直後にビルドする。

```powershell
dotnet build
```

このScaffoldは移行時の1回だけ実行する。Code Firstへ切り替えた後に`--force`付きで再実行すると、手で変更したEntityやDbContextが上書きされるため実行しない。

## 6. Scaffold結果とDBスキーマを比較する

Reverse Engineeringは主要なテーブル、列、キー、外部キー、インデックスなどを取り込むが、DB固有機能を完全に再現できるとは限らない。最低限、次を比較する。

- テーブル数
- 列名、型、NULL可否、最大長、精度
- 主キー、外部キー、削除動作
- UNIQUE制約、インデックス、フィルター
- DEFAULT制約
- CHECK制約
- IDENTITY、ROWVERSION、計算列
- トリガー、シーケンス、ビュー
- 照合順序、既定スキーマ

SQL Server側の確認例:

```sql
SELECT COUNT(*) AS TableCount
FROM sys.tables
WHERE name <> N'__EFMigrationsHistory';

SELECT
    OBJECT_SCHEMA_NAME(parent_object_id) AS SchemaName,
    OBJECT_NAME(parent_object_id) AS TableName,
    name AS ConstraintName,
    definition
FROM sys.check_constraints
ORDER BY TableName, ConstraintName;
```

今回のScaffold結果では、21テーブルと主要マッピングは生成されたが、元DDLに存在した41個のCHECK制約は生成されなかった。

## 7. Scaffoldで欠落した定義をコードへ補完する

生成された`AppDbContext`はpartial classで、末尾から次のpartial methodを呼び出す。

```csharp
OnModelCreatingPartial(modelBuilder);
```

そのため、別ファイルに実装するとDbContext本体を整理したまま追加構成を管理できる。

```text
App/Data/AppDbContext.CheckConstraints.cs
```

例:

```csharp
using App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Data;

public partial class AppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecurrenceRule>()
            .ToTable("RecurrenceRules", "dbo", table =>
            {
                table.HasCheckConstraint(
                    "CK_RecurrenceRules_WeekOfMonth",
                    "[WeekOfMonth] IS NULL OR [WeekOfMonth] BETWEEN -1 AND 5");
            });
    }
}
```

追加後にビルドする。

```powershell
dotnet build
```

## 8. 初回Migrationを作成する

すべての不足定義をモデルへ追加してから作成する。

```powershell
dotnet tool run dotnet-ef migrations add InitialCreate `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext `
  --output-dir Migrations
```

生成物:

```text
Migrations/yyyyMMddHHmmss_InitialCreate.cs
Migrations/yyyyMMddHHmmss_InitialCreate.Designer.cs
Migrations/AppDbContextModelSnapshot.cs
```

Migrationは自動生成後に必ずレビューする。特に次を確認する。

- 意図しない`DropTable`、`DropColumn`がないか
- 列名変更が「削除＋追加」になっていないか
- DEFAULT、CHECK、INDEX、FKの名前と式が正しいか
- `Down`で安全に戻せるか

生成SQLも確認する。

```powershell
dotnet tool run dotnet-ef migrations script 0 InitialCreate `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext `
  --output .\database\InitialCreate.generated.sql
```

元DDLと生成SQLを比較し、重要なスキーマ差分がないことを確認する。

## 9. DBをMigration管理へ切り替える

### 9.1 開発DBを作り直せる場合

今回のように開発DBのデータが不要なら、この方法を推奨する。

```powershell
dotnet tool run dotnet-ef database drop --force `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext

dotnet tool run dotnet-ef database update `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

EF Coreが`__EFMigrationsHistory`を作成し、適用済みMigrationを記録する。

### 9.2 既存データを保持する場合

共有DBや本番DBを作り直せない場合は、初回Migrationを既存DBへそのまま適用してはいけない。既存テーブルとの重複エラーになる。

次の手順でベースライン化する。

1. DBをバックアップする。
2. 現在のDBと同じモデルから`InitialCreate`を生成する。
3. 空DBにInitialCreateを適用し、既存DBとのスキーマ差分を確認する。
4. `migrations script 0 InitialCreate`を生成する。
5. 既存DBにはテーブル作成部分を実行せず、`__EFMigrationsHistory`の作成とInitialCreateの履歴登録だけを行う。
6. 次のMigrationから通常どおり適用する。

これは既存DBとモデルが一致していることが前提である。共有DBや本番DBでは、生成SQLと履歴登録SQLをDBAがレビューしてから実行する。

## 10. 今回のプロジェクトで実際に行った補正

今回、`InitialCreate`を生成・適用した後でCHECK制約の欠落を検出した。この時点の確認結果は次のとおりだった。

```text
AppTableCount          21
CheckConstraintCount    0
```

そこで次を行った。

1. `AppDbContext.CheckConstraints.cs`へ41個のCHECK制約を追加した。
2. `AddCheckConstraints` Migrationを生成した。
3. 既存DBへ追加Migrationを適用した。
4. Model Snapshotに41個の制約が含まれることを確認した。

最終状態:

```text
AppTableCount          21
CheckConstraintCount   41

20261012071744_InitialCreate
20261012072636_AddCheckConstraints
```

このMigration履歴は有効なので、適用済みの2件を削除・統合する必要はない。次回同じ移行を行う場合は、CHECK制約を補完してからInitialCreateを生成すれば、1件目にすべて含められる。

## 11. 移行完了の確認

### 11.1 DBオブジェクト数

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C `
  -d FYCalendarDev `
  -Q "SELECT COUNT(*) AS AppTableCount FROM sys.tables WHERE name <> N'__EFMigrationsHistory'; SELECT COUNT(*) AS CheckConstraintCount FROM sys.check_constraints; SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId"
```

本プロジェクトの期待値:

```text
AppTableCount          21
CheckConstraintCount   41
Migration履歴           2件
```

### 11.2 モデルとSnapshotの一致

```powershell
dotnet tool run dotnet-ef migrations has-pending-model-changes `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

期待される表示:

```text
No changes have been made to the model since the last migration.
```

### 11.3 ビルド

```powershell
dotnet build
```

## 12. 移行後の通常開発フロー

### 12.1 モデルを変更する

変更対象は次のいずれか。

- `Data/Entities/*.cs`
- `Data/AppDbContext.cs`
- `Data/AppDbContext.*.cs`
- `IEntityTypeConfiguration<T>`を採用している場合は各構成クラス

MigrationやModel Snapshotを先に手書きしない。

### 12.2 Migrationを生成する

```powershell
dotnet tool run dotnet-ef migrations add AddExampleColumn `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

Migration名は変更内容を表す英語名にする。

例:

```text
AddEventCategory
RenameUserDisplayName
AddCalendarVisibility
```

### 12.3 生成内容をレビューする

列名変更は、EF Coreが`DropColumn`と`AddColumn`として生成する場合がある。そのまま適用するとデータが失われるため、必要に応じて`RenameColumn`へ修正する。

### 12.4 開発DBへ適用する

```powershell
dotnet tool run dotnet-ef database update `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

### 12.5 完了確認

```powershell
dotnet build

dotnet tool run dotnet-ef migrations has-pending-model-changes `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

### 12.6 Gitへ含めるもの

- Entity／DbContextの変更
- 新しいMigration本体
- Migration Designer
- `AppDbContextModelSnapshot.cs`
- 必要なテスト、設計資料

Migration一式とモデル変更は同じコミットに含める。

## 13. Migrationを取り消す場合

### 未適用の最新Migration

```powershell
dotnet tool run dotnet-ef migrations remove `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

### ローカル開発DBへ適用済みの場合

一つ前のMigrationへ戻してから削除する。

```powershell
dotnet tool run dotnet-ef database update PreviousMigrationName `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext

dotnet tool run dotnet-ef migrations remove `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

`PreviousMigrationName`は、実際に戻すMigration名へ置き換える。

共有DBや本番DBへ適用済みのMigrationファイルは削除しない。修正用の新しいMigrationを追加する。

## 14. 配布用SQLを作成する場合

本番や検証環境でSQLレビューが必要な場合は、冪等スクリプトを生成する。

```powershell
dotnet tool run dotnet-ef migrations script --idempotent `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext `
  --output .\database\deploy.sql
```

本番環境では、生成SQLまたはMigration Bundleをデプロイ工程で適用する。接続文字列をソースコードや生成物へ埋め込まない。

- [Applying Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [EF Core tools reference](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

## 15. 今回発生した問題と対処一覧

| 症状 | 原因 | 対処 |
|---|---|---|
| LocalDBが起動しない | LocalDBインスタンス異常 | `MSSQLLocalDB`を停止・削除・再作成し、接続確認する |
| `Invalid column name 'WeekOfMonth'` | UTF-8 SQLをShift-JISとして読み、列定義がコメントへ取り込まれた | UTF-8 BOMで保存、または`sqlcmd -f 65001`を使用する |
| テーブル数が途中までしかない | DDLが途中の`GO`バッチで停止した | 開発DBを削除・再作成して最初から適用する |
| `Users`が既に存在する | 途中失敗したDBへDDLを再実行した | DBの状態を確認し、不要なら再作成する |
| PowerShellスクリプトが未署名 | Execution Policy | 現在プロセスだけ`Set-ExecutionPolicy -Scope Process Bypass`を設定する |
| `Cannot open database "AppDev"` | User Secretsまたは環境変数が接続文字列を上書きした | `ConnectionStrings__AppConnection`とUser Secretsを確認する |
| Scaffold後のCHECK制約数が0 | CHECK制約がEFモデルへ生成されなかった | Fluent APIで制約を追加し、Migrationを生成する |
| Migration再実行でオブジェクト重複 | Migration履歴とDB実体が一致していない | `__EFMigrationsHistory`と対象DBを確認する |

接続設定の確認例:

```powershell
$env:ConnectionStrings__AppConnection
dotnet user-secrets list --project .\App.csproj
```

不要な環境変数:

```powershell
Remove-Item Env:ConnectionStrings__AppConnection
```

不要なUser Secret:

```powershell
dotnet user-secrets remove "ConnectionStrings:AppConnection" `
  --project .\App.csproj
```

## 16. 完了チェックリスト

- [ ] EntityとDbContextがビルドできる
- [ ] 元DBのテーブル数とMigration作成DBの業務テーブル数が一致する
- [ ] 主キー、外部キー、UNIQUE、INDEX、DEFAULT、CHECKを比較した
- [ ] DB固有オブジェクトの扱いを決めた
- [ ] 生成Migrationの`Up`と`Down`をレビューした
- [ ] 生成SQLを元DDLと比較した
- [ ] `__EFMigrationsHistory`に想定したMigrationが記録されている
- [ ] `has-pending-model-changes`が差分なしを返す
- [ ] Database First用スクリプトを通常運用から外した
- [ ] Entity、DbContext、Migration、SnapshotをGitへコミットした
