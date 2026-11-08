using App.Domain.Exceptions;
using App.Domain.Services;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class TeamServiceTests {
    [Fact]
    public async Task チームは名前を整えてタイムゾーンとともに保存される() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var team = await new TeamService(context).CreateAsync("  開発チーム  ", " UTC ");

        await using var verification = database.CreateContext();
        var saved = await verification.Teams.SingleAsync();
        saved.Id.Should().Be(team.Id).And.BeGreaterThan(0);
        saved.Name.Should().Be("開発チーム");
        saved.TimeZoneId.Should().Be("UTC");
        saved.CreatedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
        saved.UpdatedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
        saved.RowVersion.Should().HaveCount(8);
    }

    [Fact]
    public async Task 同名のチームもIDで区別して新しい順に取得できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new TeamService(context);
        var first = await service.CreateAsync(new string('名', 200));
        var second = await service.CreateAsync(first.Name);
        context.ChangeTracker.Clear();

        var teams = await service.GetAllAsync();

        teams.Select(team => team.Id).Should().Equal(second.Id, first.Id);
        teams.Should().OnlyContain(team => team.TimeZoneId == "Asia/Tokyo");
        (await service.FindAsync(first.Id))!.Id.Should().Be(first.Id);
        (await service.FindAsync(long.MaxValue)).Should().BeNull();
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "UTC", "name")]
    [InlineData("  ", "UTC", "name")]
    [InlineData("チーム", "", "timeZoneId")]
    [InlineData("チーム", "No/Such_Time_Zone", "timeZoneId")]
    public async Task 名前やタイムゾーンが不正ならチームを保存しない(string? name, string zone, string field) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Func<Task> create = () => new TeamService(context).CreateAsync(name!, zone);

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName(field);
        (await context.Teams.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task チーム名が200文字を超えると保存しない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Func<Task> create = () => new TeamService(context).CreateAsync(new string('名', 201));

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("name");
        (await context.Teams.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task メンバーは複数チームに所属でき同じチームへの二重追加は拒否する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new TeamService(context);
        var first = await service.CreateAsync("開発");
        var second = await service.CreateAsync("運用");
        var user = await new UserService(context).CreateAsync("利用者", "user@example.com");

        await service.AddMemberAsync(first.Id, user.Id);
        await service.AddMemberAsync(second.Id, user.Id);
        Func<Task> duplicate = () => service.AddMemberAsync(first.Id, user.Id);

        var exception = (await duplicate.Should().ThrowAsync<DuplicateTeamMemberException>()).Which;
        exception.TeamId.Should().Be(first.Id);
        exception.UserId.Should().Be(user.Id);
        (await context.TeamMembers.CountAsync()).Should().Be(2);
        context.ChangeTracker.Clear();
        var member = (await service.GetMembersAsync(first.Id)).Should().ContainSingle().Subject;
        member.User.Name.Should().Be("利用者");
        member.User.Email.Should().Be("user@example.com");
        member.Role.Should().Be("member");
        member.JoinedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task メンバー解除は利用者や別チームの所属やカレンダーを削除しない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new TeamService(context);
        var first = await service.CreateAsync("開発");
        var second = await service.CreateAsync("運用");
        var user = await new UserService(context).CreateAsync("利用者");
        await service.AddMemberAsync(first.Id, user.Id);
        await service.AddMemberAsync(second.Id, user.Id);
        await new TeamCalendarService(context).CreateAsync(first.Id, "共有予定");

        (await service.RemoveMemberAsync(first.Id, user.Id)).Should().BeTrue();
        (await service.RemoveMemberAsync(first.Id, user.Id)).Should().BeFalse();

        (await service.GetMembersAsync(first.Id)).Should().BeEmpty();
        (await service.GetMembersAsync(second.Id)).Should().ContainSingle();
        (await context.Users.CountAsync()).Should().Be(1);
        (await context.TeamCalendars.CountAsync()).Should().Be(1);
        await service.AddMemberAsync(first.Id, user.Id);
        (await service.GetMembersAsync(first.Id)).Should().ContainSingle();
    }

    [Theory]
    [InlineData(true, 0L, "teamId")]
    [InlineData(true, long.MaxValue, "teamId")]
    [InlineData(false, 0L, "userId")]
    [InlineData(false, long.MaxValue, "userId")]
    public async Task 存在しないチームや利用者をメンバーに登録できない(bool missingTeam, long id, string field) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new TeamService(context);
        var team = await service.CreateAsync("チーム");
        var user = await new UserService(context).CreateAsync("利用者");

        Func<Task> add = () => service.AddMemberAsync(missingTeam ? id : team.Id, missingTeam ? user.Id : id);

        await add.Should().ThrowAsync<ArgumentException>().WithParameterName(field);
        (await context.TeamMembers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 存在しないチームのメンバー解除はエラーになる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Func<Task> remove = () => new TeamService(context).RemoveMemberAsync(long.MaxValue, 1);

        await remove.Should().ThrowAsync<ArgumentException>().WithParameterName("teamId");
    }

    [Fact]
    public async Task キャンセルされたメンバー追加は保存しない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new TeamService(context);
        var team = await service.CreateAsync("チーム");
        var user = await new UserService(context).CreateAsync("利用者");

        Func<Task> add = () => service.AddMemberAsync(team.Id, user.Id, new CancellationToken(true));

        await add.Should().ThrowAsync<OperationCanceledException>();
        (await context.TeamMembers.CountAsync()).Should().Be(0);
    }
}
