using App.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Infrastructure;

/// <summary>制約・SQL・トランザクションに依存しない単純なテスト専用。</summary>
internal static class InMemoryTestDatabase {
    public static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid():N}")
            .Options);
}
