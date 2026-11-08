using App.Controllers;
using App.Data;
using App.Domain.Services;
using App.Models.Teams;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class TeamsControllerTests {
    [Fact]
    public async Task 登録後はチーム一覧へ戻り同名でもIDで区別する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var first = await new TeamService(context).CreateAsync("開発");
        var controller = CreateController(context);

        var redirect = (await controller.Create(new CreateTeamViewModel { Name = "開発", TimeZoneId = "UTC" }, CancellationToken.None))
            .Should().BeOfType<RedirectToActionResult>().Subject;

        redirect.ActionName.Should().Be(nameof(TeamsController.Index));
        controller.TempData["SuccessMessage"].Should().Be("「開発」を登録しました。");
        var result = (await controller.Index(CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;
        var list = result.Model.Should().BeAssignableTo<IReadOnlyList<TeamListItemViewModel>>().Subject;
        list.Should().HaveCount(2);
        list[0].Id.Should().BeGreaterThan(first.Id);
        list[0].TimeZoneId.Should().Be("UTC");
    }

    [Fact]
    public async Task 不正なタイムゾーンは入力と選択肢を保持して再表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);
        var model = new CreateTeamViewModel { Name = "開発", TimeZoneId = "No/Such_Time_Zone" };

        var result = (await controller.Create(model, CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        model.Name.Should().Be("開発");
        model.TimeZoneOptions.Should().NotBeEmpty();
        controller.ModelState[nameof(model.TimeZoneId)]!.Errors.Should().ContainSingle();
        (await context.Teams.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 所属済み利用者は追加候補に表示せず重複送信は項目にエラーを表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new TeamService(context);
        var team = await service.CreateAsync("開発");
        var userService = new UserService(context);
        var member = await userService.CreateAsync("同じ氏名");
        var candidate = await userService.CreateAsync("同じ氏名");
        await service.AddMemberAsync(team.Id, member.Id);
        var controller = CreateController(context);

        var page = (await controller.Members(team.Id, CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;
        var model = page.Model.Should().BeOfType<TeamMembersViewModel>().Subject;
        model.UserOptions.Should().ContainSingle(option => option.Value == candidate.Id.ToString() && option.Text.Contains($"ID: {candidate.Id}"));
        model.Members.Should().ContainSingle(item => item.UserId == member.Id);
        var duplicate = (await controller.AddMember(team.Id, new TeamMembersViewModel { UserId = member.Id }, CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        duplicate.ViewName.Should().Be(nameof(TeamsController.Members));
        controller.ModelState[nameof(model.UserId)]!.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "選択した利用者は既にこのチームのメンバーです。");
        (await context.TeamMembers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task メンバー追加先は送信モデルでなくルートのチームを使う() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new TeamService(context);
        var target = await service.CreateAsync("追加先");
        var other = await service.CreateAsync("別のチーム");
        var user = await new UserService(context).CreateAsync("利用者");

        var result = (await CreateController(context).AddMember(target.Id,
            new TeamMembersViewModel { TeamId = other.Id, UserId = user.Id }, CancellationToken.None))
            .Should().BeOfType<RedirectToActionResult>().Subject;

        result.ActionName.Should().Be(nameof(TeamsController.Members));
        result.RouteValues!["id"].Should().Be(target.Id);
        (await context.TeamMembers.SingleAsync()).TeamId.Should().Be(target.Id);
    }

    [Fact]
    public async Task 入力不正や削除済みの利用者はメンバー画面にエラーを表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var team = await new TeamService(context).CreateAsync("開発");
        await new UserService(context).CreateAsync("追加できる利用者");
        var controller = CreateController(context);
        var model = new TeamMembersViewModel { UserId = long.MaxValue };

        var result = (await controller.AddMember(team.Id, model, CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        model.TeamId.Should().Be(team.Id);
        model.TeamName.Should().Be("開発");
        model.UserOptions.Should().ContainSingle();
        controller.ModelState[nameof(model.UserId)]!.Errors.Should().ContainSingle();
        (await context.TeamMembers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task メンバー解除後は同じチームのメンバー画面へ戻る() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new TeamService(context);
        var team = await service.CreateAsync("開発");
        var user = await new UserService(context).CreateAsync("利用者");
        await service.AddMemberAsync(team.Id, user.Id);
        var controller = CreateController(context);

        var result = (await controller.RemoveMember(team.Id, user.Id, CancellationToken.None))
            .Should().BeOfType<RedirectToActionResult>().Subject;

        result.ActionName.Should().Be(nameof(TeamsController.Members));
        result.RouteValues!["id"].Should().Be(team.Id);
        controller.TempData["SuccessMessage"].Should().Be("メンバーから外しました。");
        (await context.TeamMembers.CountAsync()).Should().Be(0);
        (await context.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task 存在しないチームは404で不正な解除要求は400を返す() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);

        (await controller.Members(long.MaxValue, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.AddMember(long.MaxValue, new TeamMembersViewModel { UserId = 1 }, CancellationToken.None))
            .Should().BeOfType<NotFoundResult>();
        (await controller.RemoveMember(long.MaxValue, 1, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.RemoveMember(1, 0, CancellationToken.None)).Should().BeOfType<BadRequestResult>();
    }

    private static TeamsController CreateController(AppDbContext context) {
        var httpContext = new DefaultHttpContext();
        return new TeamsController(new TeamService(context), new UserService(context)) {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new MemoryTempDataProvider())
        };
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
