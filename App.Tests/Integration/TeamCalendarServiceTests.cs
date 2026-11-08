using App.Domain.Services;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class TeamCalendarServiceTests {
    [Fact]
    public async Task カレンダーはチームのタイムゾーンを引き継いで独立した所有者として保存される() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var team = await new TeamService(context).CreateAsync("開発", "UTC");

        var created = await new TeamCalendarService(context).CreateAsync(team.Id, "  共有予定  ", displayColor: " #aBc123 ");

        await using var verification = database.CreateContext();
        var saved = await verification.TeamCalendars.Include(calendar => calendar.Team).SingleAsync();
        saved.Id.Should().Be(created.Id).And.BeGreaterThan(0);
        saved.TeamId.Should().Be(team.Id);
        saved.Team.Name.Should().Be("開発");
        saved.Name.Should().Be("共有予定");
        saved.TimeZoneId.Should().Be("UTC");
        saved.DisplayColor.Should().Be("#ABC123");
        saved.CalendarType.Should().Be("schedule");
        saved.IsActive.Should().BeTrue();
        saved.RowVersion.Should().HaveCount(8);
        saved.CreatedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
        (await verification.UserCalendars.CountAsync()).Should().Be(0);
        (await verification.TeamMembers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task タイムゾーンは個別指定できチーム設定の変更も保存済みカレンダーに影響しない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var team = await new TeamService(context).CreateAsync("開発", "UTC");
        var service = new TeamCalendarService(context);
        var inherited = await service.CreateAsync(team.Id, "予定", "   ", "  ");
        var explicitZone = await service.CreateAsync(team.Id, "予定", " Asia/Tokyo ");
        team.TimeZoneId = "America/New_York";
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var calendars = await service.GetAllAsync();

        calendars.Select(calendar => calendar.Id).Should().Equal(explicitZone.Id, inherited.Id);
        calendars.Select(calendar => calendar.TimeZoneId).Should().Equal("Asia/Tokyo", "UTC");
        calendars.Should().OnlyContain(calendar => calendar.DisplayColor == null && calendar.Team.TimeZoneId == "America/New_York");
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    public async Task 存在しないチームのカレンダーは登録できない(long teamId) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Func<Task> create = () => new TeamCalendarService(context).CreateAsync(teamId, "共有予定");

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("teamId");
        (await context.TeamCalendars.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("  ", null, null, "name")]
    [InlineData("共有予定", "No/Such_Time_Zone", null, "timeZoneId")]
    [InlineData("共有予定", null, "#GGGGGG", "displayColor")]
    [InlineData("共有予定", null, "#123456\nx", "displayColor")]
    public async Task 不正な設定はカレンダーを保存しない(
        string name, string? zone, string? color, string field) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var team = await new TeamService(context).CreateAsync("開発");

        Func<Task> create = () => new TeamCalendarService(context).CreateAsync(team.Id, name, zone, color);

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName(field);
        (await context.TeamCalendars.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 名前の上限を超えると登録せず上限内なら登録できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var team = await new TeamService(context).CreateAsync("開発");
        var service = new TeamCalendarService(context);

        Func<Task> create = () => service.CreateAsync(team.Id, new string('名', 201));

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("name");
        await service.CreateAsync(team.Id, new string('名', 200));
        (await context.TeamCalendars.SingleAsync()).Name.Should().HaveLength(200);
    }
}
