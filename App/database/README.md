# Database-first workflow

The SQL files in this directory are the source of truth for the database schema.

## 1. Apply the schema to a development database

Start SQL Server or LocalDB, then apply `schema/001_initial.sql` to `FYCalendarDev`.

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -Q "IF DB_ID(N'FYCalendarDev') IS NULL CREATE DATABASE [FYCalendarDev]"
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -d FYCalendarDev -i .\database\schema\001_initial.sql
```

## 2. Generate the EF Core model

Run this from the `App` directory:

```powershell
.\database\scaffold.ps1
```

The command generates entities under `Data/Entities` and `AppDbContext` under `Data`.
Do not add business rules or UI properties to generated files.

## 3. Change workflow

1. Change `schema/*.sql`.
2. Apply the change to the development database.
3. Run `database/scaffold.ps1`.
4. Review the generated diff.
5. Hand-write ViewModels, application services, controllers, and views.

This project treats the SQL schema, not EF Core Migrations, as the source of truth.
