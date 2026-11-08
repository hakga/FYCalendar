using App.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace App.E2ETests;

[Collection("Browser")]
[Trait("Category", "E2E")]
public sealed class TeamManagementPageTests(BrowserFixture fixture) : IAsyncLifetime {
    public Task InitializeAsync() => ClearDataAsync();
    public Task DisposeAsync() => ClearDataAsync();

    [Fact]
    public async Task チーム未登録ならカレンダー登録からチーム登録へ案内する() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");
        await page.GetByRole(AriaRole.Navigation).GetByRole(AriaRole.Link, new() { Name = "チームカレンダー", Exact = true }).ClickAsync();
        await Expect(page.GetByText("チームカレンダーが登録されていません。", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "チームカレンダーを登録", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true })).ToBeDisabledAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "チームを登録", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "チーム登録", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task チームを登録して一覧からタイムゾーンを引き継いだカレンダーを作成できる() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");
        await page.GetByRole(AriaRole.Navigation).GetByRole(AriaRole.Link, new() { Name = "チーム管理", Exact = true }).ClickAsync();
        await Expect(page.GetByText("チームが登録されていません。", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "チームを登録", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("タイムゾーン", new() { Exact = true })).ToHaveValueAsync("Asia/Tokyo");
        await page.GetByLabel("チーム名", new() { Exact = true }).FillAsync("  開発チーム  ");
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).SelectOptionAsync("UTC");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("「開発チーム」を登録しました。");
        await page.ReloadAsync();
        await Expect(page.Locator("tbody tr")).ToHaveCountAsync(1);
        await using var database = fixture.CreateDbContext();
        var team = await database.Teams.SingleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "カレンダーを登録", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("チーム", new() { Exact = true })).ToHaveValueAsync(team.Id.ToString());
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync("  共有予定  ");
        await page.GetByLabel("表示色", new() { Exact = true }).FillAsync("#aBc123");
        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Calendars/TeamCalendars");
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("「共有予定」を登録しました。");
        var saved = await database.TeamCalendars.SingleAsync();
        await Expect(page.Locator("tbody tr").Locator("th, td")).ToHaveTextAsync(new[] {
            saved.Id.ToString(), $"開発チーム（ID: {team.Id}）", "共有予定", "予定", "UTC", "#ABC123", "有効"
        });
        await page.ReloadAsync();
        Assert.Equal(1, await database.Teams.CountAsync());
        Assert.Equal(1, await database.TeamCalendars.CountAsync());
        Assert.Equal(0, await database.TeamMembers.CountAsync());
    }

    [Fact]
    public async Task 同名の利用者をIDで選び二重追加を拒否して所属だけを解除できる() {
        await using var database = fixture.CreateDbContext();
        var service = new TeamService(database);
        var team = await service.CreateAsync("開発");
        var other = await service.CreateAsync("別チーム");
        var userService = new UserService(database);
        var user = await userService.CreateAsync("同じ氏名");
        await userService.CreateAsync("同じ氏名");
        await service.AddMemberAsync(other.Id, user.Id);
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Teams/Members/{team.Id}");
        await Expect(page.GetByText("メンバーが登録されていません。", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.Locator("#UserId option")).ToHaveCountAsync(3);
        await page.GetByLabel("追加する利用者", new() { Exact = true }).SelectOptionAsync(user.Id.ToString());

        await page.GetByRole(AriaRole.Button, new() { Name = "メンバーを追加", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Teams/Members/{team.Id}");
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("メンバーを追加しました。");
        await page.ReloadAsync();
        await Expect(page.Locator("tbody tr")).ToHaveCountAsync(1);
        await Expect(page.Locator("#UserId option")).ToHaveCountAsync(2);
        await page.Locator("#UserId").EvaluateAsync(
            "(select, id) => select.add(new Option('重複', id, true, true))", user.Id.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "メンバーを追加", Exact = true }).ClickAsync();
        await Expect(page.Locator("[data-valmsg-for='UserId']")).ToHaveTextAsync("選択した利用者は既にこのチームのメンバーです。");
        Assert.Equal(2, await database.TeamMembers.CountAsync());
        await page.GetByRole(AriaRole.Button, new() { Name = $"同じ氏名（ID: {user.Id}）をメンバーから外す", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("メンバーから外しました。");
        await Expect(page.GetByText("メンバーが登録されていません。", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.Locator("#UserId option")).ToHaveCountAsync(3);
        Assert.Equal(2, await database.Users.CountAsync());
        var remaining = await database.TeamMembers.AsNoTracking().SingleAsync();
        Assert.Equal(other.Id, remaining.TeamId);
    }

    [Fact]
    public async Task 同名チームのカレンダーも所有者を区別してタイムゾーンを個別指定できる() {
        await using var database = fixture.CreateDbContext();
        var service = new TeamService(database);
        var first = await service.CreateAsync("同じチーム名", "UTC");
        var second = await service.CreateAsync("同じチーム名");
        await new TeamCalendarService(database).CreateAsync(first.Id, "登録済みの名前");
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Calendars/CreateTeamCalendar?teamId={second.Id}");
        await Expect(page.Locator("#TeamId option")).ToHaveCountAsync(3);
        await Expect(page.Locator("#CalendarNameOptions option")).ToHaveAttributeAsync("value", "登録済みの名前");
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync("登録済みの名前");
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).SelectOptionAsync("UTC");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Calendars/TeamCalendars");
        var saved = await database.TeamCalendars.AsNoTracking().SingleAsync(calendar => calendar.TeamId == second.Id);
        Assert.Equal("UTC", saved.TimeZoneId);
        Assert.Equal("Asia/Tokyo", (await database.Teams.AsNoTracking().SingleAsync(team => team.Id == second.Id)).TimeZoneId);
    }

    [Theory]
    [InlineData("Name", "チーム名を入力してください。")]
    [InlineData("TimeZoneId", "有効なタイムゾーンを選択してください。")]
    public async Task JavaScriptが無効でもチームの入力不正を拒否する(string field, string error) {
        await using var context = await CreateBrowserContextAsync(javaScriptEnabled: false);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Teams/Create");
        await page.GetByLabel("チーム名", new() { Exact = true }).FillAsync(field == "Name" ? "   " : "開発");
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).SelectOptionAsync("UTC");
        if (field == "TimeZoneId") {
            await page.Locator("#TimeZoneId").EvaluateAsync("select => select.add(new Option('不正な値', 'No/Such_Time_Zone', true, true))");
        }
        await page.Locator("form").EvaluateAsync("form => form.noValidate = true");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator($"[data-valmsg-for='{field}']")).ToHaveTextAsync(error);
        await using var database = fixture.CreateDbContext();
        Assert.Equal(0, await database.Teams.CountAsync());
    }

    [Theory]
    [InlineData("TeamId", "チームを選択してください。")]
    [InlineData("Name", "カレンダー名を入力してください。")]
    [InlineData("DisplayColor", "表示色は#に続く6桁の16進数で入力してください。")]
    public async Task JavaScriptが無効でもチームカレンダーの入力不正を拒否する(string field, string error) {
        var teamId = await CreateTeamAsync();
        await using var context = await CreateBrowserContextAsync(javaScriptEnabled: false);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Calendars/CreateTeamCalendar?teamId={teamId}");
        await page.GetByLabel("チーム", new() { Exact = true }).SelectOptionAsync(field == "TeamId" ? "" : teamId.ToString());
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync(field == "Name" ? "   " : "共有予定");
        await page.GetByLabel("表示色", new() { Exact = true }).FillAsync(field == "DisplayColor" ? "#GGGGGG" : "#123456");
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).SelectOptionAsync("UTC");
        await page.Locator("form").EvaluateAsync("form => form.noValidate = true");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator($"[data-valmsg-for='{field}']")).ToHaveTextAsync(error);
        await Expect(page.GetByLabel("タイムゾーン", new() { Exact = true })).ToHaveValueAsync("UTC");
        await Expect(page.Locator("#TeamId option")).ToHaveCountAsync(2);
        await using var database = fixture.CreateDbContext();
        Assert.Equal(0, await database.TeamCalendars.CountAsync());
    }

    [Fact]
    public async Task 表示後にチームが削除されてもカレンダーを保存しない() {
        var teamId = await CreateTeamAsync();
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Calendars/CreateTeamCalendar?teamId={teamId}");
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync("共有予定");
        await using var database = fixture.CreateDbContext();
        await database.Teams.Where(team => team.Id == teamId).ExecuteDeleteAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator("[data-valmsg-for='TeamId']")).ToHaveTextAsync("選択したチームが見つかりません。チームを選択し直してください。");
        Assert.Equal(0, await database.TeamCalendars.CountAsync());
    }

    [Fact]
    public async Task 表示後に利用者が削除されてもメンバーを追加しない() {
        var teamId = await CreateTeamAsync();
        await using var database = fixture.CreateDbContext();
        var user = await new UserService(database).CreateAsync("利用者");
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Teams/Members/{teamId}");
        await page.GetByLabel("追加する利用者", new() { Exact = true }).SelectOptionAsync(user.Id.ToString());
        await database.Users.Where(item => item.Id == user.Id).ExecuteDeleteAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "メンバーを追加", Exact = true }).ClickAsync();

        await Expect(page.Locator("[data-valmsg-for='UserId']")).ToHaveTextAsync("選択した利用者が見つかりません。利用者を選択し直してください。");
        Assert.Equal(0, await database.TeamMembers.CountAsync());
    }

    [Theory]
    [InlineData("/Teams/Create")]
    [InlineData("/Teams/AddMember/1")]
    [InlineData("/Teams/RemoveMember/1")]
    [InlineData("/Calendars/CreateTeamCalendar")]
    public async Task 偽造防止トークンのない更新要求を拒否する(string path) {
        await using var context = await CreateBrowserContextAsync();

        var response = await context.APIRequest.PostAsync(path);

        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task 存在しないチームは404でメンバー解除のGET要求は405になる() {
        await using var context = await CreateBrowserContextAsync();

        Assert.Equal(404, (await context.APIRequest.GetAsync($"/Teams/Members/{long.MaxValue}")).Status);
        Assert.Equal(405, (await context.APIRequest.GetAsync("/Teams/RemoveMember/1?userId=1")).Status);
    }

    private Task<IBrowserContext> CreateBrowserContextAsync(bool javaScriptEnabled = true) =>
        fixture.Browser.NewContextAsync(new() { BaseURL = fixture.BaseUrl, JavaScriptEnabled = javaScriptEnabled });

    private async Task<long> CreateTeamAsync() {
        await using var database = fixture.CreateDbContext();
        return (await new TeamService(database).CreateAsync("開発チーム")).Id;
    }

    private async Task ClearDataAsync() {
        await using var database = fixture.CreateDbContext();
        await database.TeamMembers.ExecuteDeleteAsync();
        await database.TeamCalendars.ExecuteDeleteAsync();
        await database.Teams.ExecuteDeleteAsync();
        await database.Users.ExecuteDeleteAsync();
    }
}
