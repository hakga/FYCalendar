using App.Data.Entities;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class SqliteDatabaseTests {
    [Fact]
    public async Task ユーザーの既定値と変更が別コンテキストにも保存される() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var context = database.CreateContext()) {
            context.Users.Add(new User { Name = "Initial" });
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext()) {
            var user = await context.Users.SingleAsync();
            user.TimeZoneId.Should().Be("Asia/Tokyo");
            user.RowVersion.Should().HaveCount(8);
            user.CreatedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
            user.Name = "Updated";
            await context.SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        (await verification.Users.SingleAsync()).Name.Should().Be("Updated");
    }

    [Fact]
    public async Task 異なるDB間でデータが共有されない() {
        await using var first = await SqliteTestDatabase.CreateAsync();
        await using var second = await SqliteTestDatabase.CreateAsync();
        await using var firstContext = first.CreateContext();
        await using var secondContext = second.CreateContext();
        firstContext.Users.Add(new User { Name = "Only in first database" });
        await firstContext.SaveChangesAsync();

        (await secondContext.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 存在しないユーザーのカレンダーは登録できない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.UserCalendars.Add(new UserCalendar { UserId = 999, Name = "Invalid" });

        Func<Task> save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task 同じ年度は重複登録できない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.FiscalYears.Add(
            new FiscalYear { Year = 2026, Name = "2026", StartsOn = new(2026, 4, 1), EndsOn = new(2027, 3, 31) });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var saved = await context.FiscalYears.SingleAsync(fiscalYear => fiscalYear.Year == 2026);
        saved.Year.Should().Be(2026);
        context.FiscalYears.Add(
            new FiscalYear { Year = 2026, Name = "Duplicate", StartsOn = new(2026, 4, 1), EndsOn = new(2027, 3, 31) });

        Func<Task> save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task カレンダーのない予定は登録できない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.Events.Add(new Event { Title = "Missing owner" });

        Func<Task> save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task 期間が0分以下の予定は登録できない(int durationMinutes) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var start = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.FromHours(9));
        context.EventSchedules.Add(new EventSchedule {
            Event = new Event { Title = "Invalid duration", UserCalendar = NewCalendar() },
            StartsAt = start,
            EndsAt = start.AddMinutes(durationMinutes)
        });

        Func<Task> save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task 予定は1分間の長さと日時を保って保存できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var start = new DateTimeOffset(2026, 11, 1, 15, 0, 0, TimeSpan.FromHours(9));
        context.EventSchedules.Add(new EventSchedule {
            Event = new Event { Title = "One minute", UserCalendar = NewCalendar() },
            StartsAt = start,
            EndsAt = start.AddMinutes(1)
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var saved = await context.EventSchedules.SingleAsync();

        saved.StartsAt.Should().Be(start);
        (saved.EndsAt - saved.StartsAt).Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task 期間検索は時差を考慮し境界で接する予定を除外する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = NewCalendar();
        var start = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.FromHours(9));
        context.EventSchedules.AddRange(
            new EventSchedule {
                Event = new Event { Title = "Overlapping", UserCalendar = calendar },
                StartsAt = start,
                EndsAt = start.AddHours(1)
            },
            new EventSchedule {
                Event = new Event { Title = "Starts at boundary", UserCalendar = calendar },
                StartsAt = start.AddHours(1),
                EndsAt = start.AddHours(2)
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var searchStart = start.AddMinutes(30).ToUniversalTime();
        var searchEnd = start.AddHours(1).ToUniversalTime();

        var matches = await context.EventSchedules
            .Where(schedule => schedule.StartsAt < searchEnd && schedule.EndsAt > searchStart)
            .Select(schedule => schedule.Event.Title)
            .ToListAsync();

        matches.Should().Equal("Overlapping");
    }

    [Fact]
    public async Task ロールバックしたデータは保存されない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var context = database.CreateContext()) {
            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Users.Add(new User { Name = "Rolled back" });
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verification = database.CreateContext();
        (await verification.Users.CountAsync()).Should().Be(0);
    }

    private static UserCalendar NewCalendar() => new() {
        Name = "Personal",
        User = new User { Name = "Owner" }
    };
}
