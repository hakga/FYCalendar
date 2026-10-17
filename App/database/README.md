# Database workflow

このプロジェクトは **Code First + EF Core Migrations** でデータベーススキーマを管理する。

現在の正（Source of Truth）は次のファイルである。

- `Data/Entities/*.cs`: Entity
- `Data/AppDbContext.cs`: テーブル、列、キー、インデックス、リレーションの構成
- `Data/AppDbContext.CheckConstraints.cs`: CHECK制約
- `Migrations/*.cs`: DB変更履歴とModel Snapshot

`database/legacy`配下のSQLとScaffoldスクリプトは、Data First時代の参照資料である。通常の開発では実行しない。

## 通常の変更手順

以下は`App`ディレクトリで実行する。

1. EntityまたはDbContextの構成を変更する。
2. Migrationを生成する。

```powershell
dotnet tool restore

dotnet tool run dotnet-ef migrations add AddExampleColumn `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

`AddExampleColumn`は、実際の変更内容を表すMigration名へ置き換える。

3. 生成されたMigrationの`Up`、`Down`、Model Snapshotをレビューする。
4. 開発DBへ反映する。

```powershell
dotnet tool run dotnet-ef database update `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

5. ビルドとモデル差分を確認する。

```powershell
dotnet build

dotnet tool run dotnet-ef migrations has-pending-model-changes `
  --project .\App.csproj `
  --startup-project .\App.csproj `
  --context AppDbContext
```

`No changes have been made to the model since the last migration.`と表示されれば、モデル変更はMigrationへ記録済みである。

## 現在の基準値

- 業務テーブル: 21
- CHECK制約: 41
- 適用済みMigration:
  - `20261012071744_InitialCreate`
  - `20261012072636_AddCheckConstraints`

確認SQL:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C `
  -d FYCalendarDev `
  -Q "SELECT COUNT(*) AS AppTableCount FROM sys.tables WHERE name <> N'__EFMigrationsHistory'; SELECT COUNT(*) AS CheckConstraintCount FROM sys.check_constraints; SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId"
```

## 移行手順書

Data Firstの既存スキーマをCode Firstへ移行するときは、[DATA_FIRST_TO_CODE_FIRST.md](./DATA_FIRST_TO_CODE_FIRST.md)を参照する。
