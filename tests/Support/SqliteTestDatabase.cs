using App.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FYCalendar.TestSupport;

/// <summary>インスタンスごとに独立したDB。接続を開いている間だけ内容を保持する。</summary>
public sealed class SqliteTestDatabase : IAsyncDisposable {
    public SqliteConnection Connection { get; } = new("Data Source=:memory:;Foreign Keys=True");

    public static async Task<SqliteTestDatabase> CreateAsync() {
        var database = new SqliteTestDatabase();
        try {
            await database.Connection.OpenAsync();
            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return database;
        }
        catch {
            await database.DisposeAsync();
            throw;
        }
    }

    public TestAppDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(Connection)
            .Options;
        return new TestAppDbContext(options);
    }

    public ValueTask DisposeAsync() => Connection.DisposeAsync();
}
