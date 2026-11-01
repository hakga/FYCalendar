using App.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FYCalendar.TestSupport;

/// <summary>
/// 本番モデルを再利用し、SQLiteで実行できるようテスト時だけ型・既定値を変換する。
/// SQL ServerのMigrationおよびデータベース生成rowversionの検証には使用しない。
/// </summary>
public sealed class TestAppDbContext(DbContextOptions<AppDbContext> options)
    : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (!Database.IsSqlite())
        {
            return;
        }

        var utcTicksConverter = new ValueConverter<DateTimeOffset, long>(
            value => value.UtcDateTime.Ticks,
            value => new DateTimeOffset(value, TimeSpan.Zero));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // SQLiteにはdboスキーマおよびN'...'リテラルがない。
            entityType.SetSchema(null);
            foreach (var constraint in entityType.GetCheckConstraints().ToArray())
            {
                var sql = constraint.Sql.Replace("N'", "'", StringComparison.Ordinal);
                entityType.RemoveCheckConstraint(constraint.ModelName);
                entityType.AddCheckConstraint(constraint.ModelName, sql);
            }

            foreach (var property in entityType.GetProperties())
            {
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType)
                    == typeof(DateTimeOffset))
                {
                    // オフセットの違う日時もSQL側で比較できるようUTCのticksで保存する。
                    property.SetValueConverter(utcTicksConverter);
                    property.SetColumnType("INTEGER");
                    if (property.GetDefaultValueSql() is not null)
                    {
                        property.SetDefaultValueSql(
                            "((CAST(strftime('%s', 'now') AS INTEGER) + 62135596800) * 10000000)");
                    }
                }

                if (property.Name == "RowVersion" && property.ClrType == typeof(byte[]))
                {
                    // SQLiteの行バージョンは初期値のみ。SQL Serverの更新動作とは異なる。
                    property.SetColumnType("BLOB");
                    property.SetDefaultValueSql("randomblob(8)");
                    property.ValueGenerated = ValueGenerated.OnAdd;
                    property.IsConcurrencyToken = false;
                }
            }
        }
    }
}
