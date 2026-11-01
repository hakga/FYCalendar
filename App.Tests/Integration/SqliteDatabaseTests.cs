using App.Data.Entities;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class SqliteDatabaseTests
{
    [Fact]
    public async Task User_DefaultValuesAndChanges_AreSavedAcrossContexts()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var context = database.CreateContext())
        {
            context.Users.Add(new User { Name = "Initial" });
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
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
    public async Task SeparateDatabases_DoNotShareRows()
    {
        await using var first = await SqliteTestDatabase.CreateAsync();
        await using var second = await SqliteTestDatabase.CreateAsync();
        await using var firstContext = first.CreateContext();
        await using var secondContext = second.CreateContext();
        firstContext.Users.Add(new User { Name = "Only in first database" });
        await firstContext.SaveChangesAsync();

        (await secondContext.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ForeignKey_RejectsCalendarWithoutUser()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.UserCalendars.Add(new UserCalendar { UserId = 999, Name = "Invalid" });

        Func<Task> save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task UniqueConstraint_RejectsDuplicateFiscalYear()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.FiscalYears.AddRange(
            new FiscalYear { FiscalYear1 = 2026, Name = "2026", StartsOn = new(2026, 4, 1), EndsOn = new(2027, 4, 1) },
            new FiscalYear { FiscalYear1 = 2026, Name = "Duplicate", StartsOn = new(2026, 4, 1), EndsOn = new(2027, 4, 1) });

        Func<Task> save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task CheckConstraint_RejectsEventWithoutCalendar()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        context.Events.Add(new Event { Title = "Missing owner" });

        Func<Task> save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CheckConstraint_RejectsNonPositiveDuration(int durationMinutes)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var start = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.FromHours(9));
        context.EventSchedules.Add(new EventSchedule
        {
            Event = new Event { Title = "Invalid duration", UserCalendar = NewCalendar() },
            StartsAt = start,
            EndsAt = start.AddMinutes(durationMinutes)
        });

        Func<Task> save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task OneMinuteSchedule_RoundTripsWithSameInstant()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var start = new DateTimeOffset(2026, 11, 1, 15, 0, 0, TimeSpan.FromHours(9));
        context.EventSchedules.Add(new EventSchedule
        {
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
    public async Task TimeRangeQuery_UsesUtcAndExcludesTouchingBoundary()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = NewCalendar();
        var start = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.FromHours(9));
        context.EventSchedules.AddRange(
            new EventSchedule
            {
                Event = new Event { Title = "Overlapping", UserCalendar = calendar },
                StartsAt = start, EndsAt = start.AddHours(1)
            },
            new EventSchedule
            {
                Event = new Event { Title = "Starts at boundary", UserCalendar = calendar },
                StartsAt = start.AddHours(1), EndsAt = start.AddHours(2)
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
    public async Task Transaction_RollbackDoesNotPersistRows()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var context = database.CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            context.Users.Add(new User { Name = "Rolled back" });
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verification = database.CreateContext();
        (await verification.Users.CountAsync()).Should().Be(0);
    }

    private static UserCalendar NewCalendar() => new()
    {
        Name = "Personal", User = new User { Name = "Owner" }
    };
}
