using App.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace App.E2ETests;

[Collection("Browser")]
[Trait("Category", "E2E")]
public sealed class UserCalendarPageTests(BrowserFixture fixture) : IAsyncLifetime {
    public Task InitializeAsync() => ClearDataAsync();
    public Task DisposeAsync() => ClearDataAsync();

    [Fact]
    public async Task 利用者がいなければ個人カレンダー登録から利用者登録へ案内する() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");
        await page.GetByRole(AriaRole.Navigation)
            .GetByRole(AriaRole.Link, new() { Name = "個人カレンダー", Exact = true }).ClickAsync();

        await Expect(page.GetByText("個人カレンダーが登録されていません。", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "個人カレンダーを登録", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "個人カレンダー登録", Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true })).ToBeDisabledAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "利用者を登録", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "利用者登録", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task 個人カレンダーは利用者のタイムゾーンを引き継いで一覧に表示される() {
        var userId = await CreateUserAsync("所有者", "UTC");
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUserCalendar");
        await page.GetByLabel("利用者", new() { Exact = true }).SelectOptionAsync(userId.ToString());
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync("  業務予定  ");
        await page.GetByLabel("表示色", new() { Exact = true }).FillAsync("#aBc123");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Maintain/UserCalendars");
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("「業務予定」を登録しました。");
        await using var database = fixture.CreateDbContext();
        var saved = await database.UserCalendars.SingleAsync();
        await Expect(page.Locator("tbody tr").Locator("th, td")).ToHaveTextAsync(new[] {
            saved.Id.ToString(), $"所有者（ID: {userId}）", "業務予定", "予定", "UTC", "#ABC123", "有効", "予定を登録"
        });
        await page.ReloadAsync();
        await Expect(page.Locator("tbody tr")).ToHaveCountAsync(1);
        Assert.Equal(1, await database.UserCalendars.CountAsync());
    }

    [Fact]
    public async Task 個人カレンダーのタイムゾーンは利用者と別に指定できる() {
        var userId = await CreateUserAsync("所有者", "UTC");
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUserCalendar");
        await page.GetByLabel("利用者", new() { Exact = true }).SelectOptionAsync(userId.ToString());
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync("日本向けの予定");
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).SelectOptionAsync("Asia/Tokyo");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Maintain/UserCalendars");
        await Expect(page.Locator("tbody tr")).ToContainTextAsync("Asia/Tokyo");
        await Expect(page.Locator("tbody tr")).ToContainTextAsync("未指定");
        await using var database = fixture.CreateDbContext();
        Assert.Equal("Asia/Tokyo", (await database.UserCalendars.SingleAsync()).TimeZoneId);
        Assert.Equal("UTC", (await database.Users.SingleAsync()).TimeZoneId);
    }

    [Fact]
    public async Task 同姓同名でも選択した利用者にカレンダーを登録できる() {
        var selectedUserId = await CreateUserAsync("同じ氏名", "UTC");
        await CreateUserAsync("同じ氏名", "Asia/Tokyo");
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUserCalendar");
        await Expect(page.Locator("#UserId option")).ToHaveCountAsync(3);
        await page.GetByLabel("利用者", new() { Exact = true }).SelectOptionAsync(selectedUserId.ToString());
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync("所有者を区別する予定");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Maintain/UserCalendars");
        await using var database = fixture.CreateDbContext();
        var saved = await database.UserCalendars.SingleAsync();
        Assert.Equal(selectedUserId, saved.UserId);
        Assert.Equal("UTC", saved.TimeZoneId);
    }

    [Theory]
    [InlineData(false, "予定", "", "UserId", "利用者を選択してください。")]
    [InlineData(true, "   ", "", "Name", "カレンダー名を入力してください。")]
    [InlineData(true, "予定", "#GGGGGG", "DisplayColor", "表示色は#に続く6桁の16進数で入力してください。")]
    public async Task JavaScriptが無効でも個人カレンダーの入力不正は拒否される(
        bool selectUser, string name, string color, string field, string error) {
        var userId = await CreateUserAsync("所有者");
        await using var context = await CreateBrowserContextAsync(javaScriptEnabled: false);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUserCalendar");
        await page.GetByLabel("利用者", new() { Exact = true }).SelectOptionAsync(selectUser ? userId.ToString() : "");
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync(name);
        await page.GetByLabel("表示色", new() { Exact = true }).FillAsync(color);
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).SelectOptionAsync("UTC");
        await page.Locator("form").EvaluateAsync("form => form.noValidate = true");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator($"[data-valmsg-for='{field}']")).ToHaveTextAsync(error);
        await Expect(page.GetByLabel("タイムゾーン", new() { Exact = true })).ToHaveValueAsync("UTC");
        await Expect(page.Locator("#UserId option")).ToHaveCountAsync(2);
        await using var database = fixture.CreateDbContext();
        Assert.Equal(0, await database.UserCalendars.CountAsync());
    }

    [Fact]
    public async Task 不正なタイムゾーンを送信しても入力を保持して登録を拒否する() {
        var userId = await CreateUserAsync("所有者");
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUserCalendar");
        await page.GetByLabel("利用者", new() { Exact = true }).SelectOptionAsync(userId.ToString());
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync("業務予定");
        await page.GetByLabel("表示色", new() { Exact = true }).FillAsync("#123456");
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).EvaluateAsync(
            "select => select.add(new Option('不正な値', 'No/Such_Time_Zone', true, true))");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator("[data-valmsg-for='TimeZoneId']")).ToHaveTextAsync("有効なタイムゾーンを選択してください。");
        await Expect(page.GetByLabel("利用者", new() { Exact = true })).ToHaveValueAsync(userId.ToString());
        await Expect(page.GetByLabel("カレンダー名", new() { Exact = true })).ToHaveValueAsync("業務予定");
        await Expect(page.GetByLabel("表示色", new() { Exact = true })).ToHaveValueAsync("#123456");
        await using var database = fixture.CreateDbContext();
        Assert.Equal(0, await database.UserCalendars.CountAsync());
    }

    [Fact]
    public async Task 画面表示後に利用者が削除された場合も項目にエラーを表示する() {
        var userId = await CreateUserAsync("所有者");
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUserCalendar");
        await page.GetByLabel("利用者", new() { Exact = true }).SelectOptionAsync(userId.ToString());
        await page.GetByLabel("カレンダー名", new() { Exact = true }).FillAsync("業務予定");
        await using (var database = fixture.CreateDbContext()) {
            await database.Users.Where(user => user.Id == userId).ExecuteDeleteAsync();
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator("[data-valmsg-for='UserId']")).ToHaveTextAsync(
            "選択した利用者が見つかりません。利用者を選択し直してください。");
        await Expect(page.GetByLabel("カレンダー名", new() { Exact = true })).ToHaveValueAsync("業務予定");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true })).ToBeDisabledAsync();
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(0, await verification.UserCalendars.CountAsync());
    }

    [Fact]
    public async Task 偽造防止トークンのない個人カレンダー登録要求は拒否される() {
        await using var context = await CreateBrowserContextAsync();

        var response = await context.APIRequest.PostAsync("/Maintain/CreateUserCalendar");

        Assert.Equal(400, response.Status);
        await using var database = fixture.CreateDbContext();
        Assert.Equal(0, await database.UserCalendars.CountAsync());
    }

    private Task<IBrowserContext> CreateBrowserContextAsync(bool javaScriptEnabled = true) =>
        fixture.Browser.NewContextAsync(new() { BaseURL = fixture.BaseUrl, JavaScriptEnabled = javaScriptEnabled });

    private async Task<long> CreateUserAsync(string name, string timeZoneId = UserService.DefaultTimeZoneId) {
        await using var database = fixture.CreateDbContext();
        return (await new UserService(database).CreateAsync(name, timeZoneId: timeZoneId)).Id;
    }

    private async Task ClearDataAsync() {
        await using var database = fixture.CreateDbContext();
        await database.UserCalendars.ExecuteDeleteAsync();
        await database.Users.ExecuteDeleteAsync();
    }
}
