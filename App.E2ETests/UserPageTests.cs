using App.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace App.E2ETests;

[Collection("Browser")]
[Trait("Category", "E2E")]
public sealed class UserPageTests(BrowserFixture fixture) : IAsyncLifetime {
    public Task InitializeAsync() => ClearUsersAsync();
    public Task DisposeAsync() => ClearUsersAsync();

    [Fact]
    public async Task ナビゲーションから空の利用者一覧を表示できる() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await page.GetByRole(AriaRole.Navigation)
            .GetByRole(AriaRole.Link, new() { Name = "利用者管理", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "利用者管理", Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("利用者が登録されていません。", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "利用者を登録", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "利用者登録", Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByLabel("タイムゾーン", new() { Exact = true })).ToHaveValueAsync("Asia/Tokyo");
    }

    [Fact]
    public async Task 利用者を登録すると一覧に表示され再読み込みで重複しない() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUser");
        await page.GetByLabel("氏名", new() { Exact = true }).FillAsync("  佐藤 花子  ");
        await page.GetByLabel("メールアドレス", new() { Exact = true }).FillAsync("sato@example.com");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Maintain/Users");
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("「佐藤 花子」を登録しました。");
        await using var database = fixture.CreateDbContext();
        var saved = await database.Users.SingleAsync();
        await Expect(page.Locator("tbody tr").Locator("th, td")).ToHaveTextAsync(
            new[] { saved.Id.ToString(), "佐藤 花子", "sato@example.com", "Asia/Tokyo" });
        await page.ReloadAsync();
        await Expect(page.Locator("tbody tr")).ToHaveCountAsync(1);
        Assert.Equal(1, await database.Users.CountAsync());
    }

    [Fact]
    public async Task メール未入力でも選択したタイムゾーンで登録できる() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUser");
        await page.GetByLabel("氏名", new() { Exact = true }).FillAsync("利用者");
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).SelectOptionAsync("UTC");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Maintain/Users");
        await Expect(page.Locator("tbody tr")).ToContainTextAsync("未登録");
        await Expect(page.Locator("tbody tr")).ToContainTextAsync("UTC");
        await using var database = fixture.CreateDbContext();
        var saved = await database.Users.SingleAsync();
        Assert.Null(saved.Email);
        Assert.Equal("UTC", saved.TimeZoneId);
    }

    [Theory]
    [InlineData("   ", "user@example.com", "Name", "氏名を入力してください。")]
    [InlineData("利用者", "invalid", "Email", "正しいメールアドレスを入力してください。")]
    public async Task JavaScriptが無効でも利用者の入力不正はサーバーで拒否される(
        string name, string email, string field, string error) {
        await using var context = await CreateBrowserContextAsync(javaScriptEnabled: false);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUser");
        await page.GetByLabel("氏名", new() { Exact = true }).FillAsync(name);
        await page.GetByLabel("メールアドレス", new() { Exact = true }).FillAsync(email);
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).SelectOptionAsync("UTC");
        await page.Locator("form").EvaluateAsync("form => form.noValidate = true");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator($"[data-valmsg-for='{field}']")).ToHaveTextAsync(error);
        await Expect(page.GetByLabel("メールアドレス", new() { Exact = true })).ToHaveValueAsync(email);
        await Expect(page.GetByLabel("タイムゾーン", new() { Exact = true })).ToHaveValueAsync("UTC");
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(0, await verification.Users.CountAsync());
    }

    [Fact]
    public async Task 無効なタイムゾーンを送信しても登録されず選択肢が再表示される() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUser");
        await page.GetByLabel("氏名", new() { Exact = true }).FillAsync("利用者");
        await page.GetByLabel("タイムゾーン", new() { Exact = true }).EvaluateAsync(
            "select => select.add(new Option('不正な値', 'No/Such_Time_Zone', true, true))");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator("[data-valmsg-for='TimeZoneId']")).ToHaveTextAsync("有効なタイムゾーンを選択してください。");
        await Expect(page.GetByLabel("氏名", new() { Exact = true })).ToHaveValueAsync("利用者");
        await Expect(page.Locator("#TimeZoneId option[value='Asia/Tokyo']")).ToHaveCountAsync(1);
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(0, await verification.Users.CountAsync());
    }

    [Fact]
    public async Task 同姓同名の利用者も別のIDで一覧に表示できる() {
        await using (var database = fixture.CreateDbContext()) {
            await new UserService(database).CreateAsync("同じ氏名");
        }
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateUser");
        await page.GetByLabel("氏名", new() { Exact = true }).FillAsync("同じ氏名");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Maintain/Users");
        await Expect(page.Locator("tbody tr")).ToHaveCountAsync(2);
        var ids = await page.Locator("tbody th").AllTextContentsAsync();
        Assert.NotEqual(ids[0], ids[1]);
    }

    [Fact]
    public async Task 偽造防止トークンのない利用者登録要求は拒否される() {
        await using var context = await CreateBrowserContextAsync();

        var response = await context.APIRequest.PostAsync("/Maintain/CreateUser");

        Assert.Equal(400, response.Status);
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(0, await verification.Users.CountAsync());
    }

    private Task<IBrowserContext> CreateBrowserContextAsync(bool javaScriptEnabled = true) =>
        fixture.Browser.NewContextAsync(new() { BaseURL = fixture.BaseUrl, JavaScriptEnabled = javaScriptEnabled });

    private async Task ClearUsersAsync() {
        await using var database = fixture.CreateDbContext();
        await database.Users.ExecuteDeleteAsync();
    }
}
