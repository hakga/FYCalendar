using App.Data.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Infrastructure;

[Trait("Category", "Unit")]
public sealed class InMemoryDatabaseTests {
    [Fact]
    public async Task InMemoryDBでユーザーを保存して取得できる() {
        await using var context = InMemoryTestDatabase.CreateContext();
        // InMemoryはSQLの既定値を生成しないため、必要な値を明示する。
        context.Users.Add(new User {
            Name = "Test User",
            TimeZoneId = "Asia/Tokyo",
            RowVersion = [],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var user = await context.Users.SingleAsync();

        user.Name.Should().Be("Test User");
    }
}
