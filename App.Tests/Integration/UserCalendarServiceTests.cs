using App.Domain.Services;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class UserCalendarServiceTests {
    [Fact]
    public async Task 個人カレンダーは利用者に関連付けられて有効な予定用として保存される() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者", timeZoneId: "UTC");

        var created = await new UserCalendarService(context).CreateAsync(user.Id, "  業務予定  ", displayColor: " #aBc123 ");

        await using var verification = database.CreateContext();
        var saved = await verification.UserCalendars.Include(calendar => calendar.User).SingleAsync();
        saved.Id.Should().Be(created.Id).And.BeGreaterThan(0);
        saved.UserId.Should().Be(user.Id);
        saved.User.Name.Should().Be("所有者");
        saved.Name.Should().Be("業務予定");
        saved.TimeZoneId.Should().Be("UTC");
        saved.DisplayColor.Should().Be("#ABC123");
        saved.CalendarType.Should().Be("schedule");
        saved.IsActive.Should().BeTrue();
        saved.CreatedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
        saved.UpdatedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
        saved.RowVersion.Should().HaveCount(8);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task タイムゾーンと色が未指定なら利用者の設定と色なしで保存される(string? value) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者", timeZoneId: "America/New_York");

        await new UserCalendarService(context).CreateAsync(user.Id, "カレンダー", value, value);

        await using var verification = database.CreateContext();
        var saved = await verification.UserCalendars.SingleAsync();
        saved.TimeZoneId.Should().Be("America/New_York");
        saved.DisplayColor.Should().BeNull();
    }

    [Fact]
    public async Task 明示したタイムゾーンを設定しても利用者の設定は変わらない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者", timeZoneId: "UTC");

        await new UserCalendarService(context).CreateAsync(user.Id, new string('名', 200), " Asia/Tokyo ");

        await using var verification = database.CreateContext();
        var saved = await verification.UserCalendars.SingleAsync();
        saved.Name.Should().HaveLength(200);
        saved.TimeZoneId.Should().Be("Asia/Tokyo");
        (await verification.Users.SingleAsync()).TimeZoneId.Should().Be("UTC");
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MaxValue)]
    public async Task 存在しない利用者のカレンダーは登録できない(long userId) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Func<Task> create = () => new UserCalendarService(context).CreateAsync(userId, "カレンダー");

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("userId");
        (await context.UserCalendars.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task カレンダー名が空なら登録できない(string? name) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");

        Func<Task> create = () => new UserCalendarService(context).CreateAsync(user.Id, name!);

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("name");
        (await context.UserCalendars.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("timeZoneId")]
    [InlineData("displayColor")]
    public async Task 不正なカレンダー設定は保存されない(string field) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");
        var name = field == "name" ? new string('名', 201) : "カレンダー";
        var timeZoneId = field == "timeZoneId" ? new string('a', 65) : null;
        var color = field == "displayColor" ? "#GGGGGG" : null;

        Func<Task> create = () => new UserCalendarService(context).CreateAsync(user.Id, name, timeZoneId, color);

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName(field);
        (await context.UserCalendars.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 存在しないタイムゾーンは登録できない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");

        Func<Task> create = () => new UserCalendarService(context).CreateAsync(user.Id, "カレンダー", "No/Such_Time_Zone");

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("timeZoneId");
        (await context.UserCalendars.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 同じ利用者に同名のカレンダーも複数登録できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");
        var service = new UserCalendarService(context);

        var first = await service.CreateAsync(user.Id, "同じ名前");
        var second = await service.CreateAsync(user.Id, "同じ名前");

        first.Id.Should().NotBe(second.Id);
        (await context.UserCalendars.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task 個人カレンダーは登録の新しい順に利用者とともに取得できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext()) {
            var first = await new UserService(seed).CreateAsync("最初の所有者");
            var second = await new UserService(seed).CreateAsync("次の所有者");
            var service = new UserCalendarService(seed);
            await service.CreateAsync(first.Id, "最初の予定");
            await service.CreateAsync(second.Id, "次の予定");
        }
        await using var context = database.CreateContext();

        var calendars = await new UserCalendarService(context).GetAllAsync();

        calendars.Select(calendar => calendar.Name).Should().Equal("次の予定", "最初の予定");
        calendars.Select(calendar => calendar.User.Name).Should().Equal("次の所有者", "最初の所有者");
        calendars[0].Name = "保存されない変更";
        calendars[0].User.Name = "所有者への変更";
        await context.SaveChangesAsync();
        await using var verification = database.CreateContext();
        var saved = await verification.UserCalendars.Include(calendar => calendar.User)
            .SingleAsync(calendar => calendar.Id == calendars[0].Id);
        saved.Name.Should().Be("次の予定");
        saved.User.Name.Should().Be("次の所有者");
    }

    [Fact]
    public async Task カレンダーが未登録なら空の一覧を返す() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        (await new UserCalendarService(context).GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task キャンセルされた個人カレンダーは登録されない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> create = () => new UserCalendarService(context)
            .CreateAsync(user.Id, "カレンダー", cancellationToken: cancellation.Token);

        await create.Should().ThrowAsync<OperationCanceledException>();
        (await context.UserCalendars.CountAsync()).Should().Be(0);
    }
}
