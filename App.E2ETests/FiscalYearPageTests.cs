using App.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace App.E2ETests;

[Collection("Browser")]
[Trait("Category", "E2E")]
public sealed class FiscalYearPageTests(BrowserFixture fixture) : IAsyncLifetime {
    public Task InitializeAsync() => ClearFiscalYearsAsync();
    public Task DisposeAsync() => ClearFiscalYearsAsync();

    [Fact]
    public async Task ナビゲーションから空の年度一覧を表示できる() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await page.GetByRole(AriaRole.Navigation)
            .GetByRole(AriaRole.Link, new() { Name = "年度管理", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "年度管理", Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("年度が登録されていません。", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "年度を登録", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "年度登録", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task 年度を登録すると一覧に期間と状態が表示される() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateFiscalYear");

        await page.GetByLabel("年度", new() { Exact = true }).FillAsync("2028");
        await Expect(page.GetByLabel("名称", new() { Exact = true })).ToHaveValueAsync("2028年度");
        await Expect(page.Locator("#FiscalYearPeriod")).ToHaveTextAsync("2028/04/01 ～ 2029/03/31");
        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Maintain/FiscalYears");
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("2028年度を登録しました。");
        await Expect(page.Locator("tbody tr").Locator("th, td")).ToHaveTextAsync(
            new[] { "2028", "2028年度", "2028/04/01", "2029/03/31", "運用中" });
        await page.ReloadAsync();
        await Expect(page.Locator("tbody tr")).ToHaveCountAsync(1);
        await using var database = fixture.CreateDbContext();
        Assert.Equal(1, await database.FiscalYears.CountAsync());
    }

    [Fact]
    public async Task 独自の名称は年度を変更しても保持して登録できる() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateFiscalYear");

        await page.GetByLabel("名称", new() { Exact = true }).FillAsync("事業計画年度");
        await page.GetByLabel("年度", new() { Exact = true }).FillAsync("2027");
        await Expect(page.GetByLabel("名称", new() { Exact = true })).ToHaveValueAsync("事業計画年度");
        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Maintain/FiscalYears");
        await Expect(page.Locator("tbody tr")).ToContainTextAsync("事業計画年度");
    }

    [Fact]
    public async Task 同じ年度の登録では入力を保持してエラーを表示する() {
        await using (var database = fixture.CreateDbContext()) {
            await new FiscalYearService(database).CreateAsync(2026, "登録済み年度");
        }
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateFiscalYear");
        await page.GetByLabel("年度", new() { Exact = true }).FillAsync("2026");
        await page.GetByLabel("名称", new() { Exact = true }).FillAsync("重複年度");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator("[data-valmsg-for='Year']")).ToHaveTextAsync("2026年度は既に登録されています。");
        await Expect(page.GetByLabel("年度", new() { Exact = true })).ToHaveValueAsync("2026");
        await Expect(page.GetByLabel("名称", new() { Exact = true })).ToHaveValueAsync("重複年度");
        await using var verification = fixture.CreateDbContext();
        Assert.Equal("登録済み年度", (await verification.FiscalYears.SingleAsync()).Name);
    }

    [Theory]
    [InlineData("", "名称", "Year", "年度を入力してください。")]
    [InlineData("0", "名称", "Year", "年度は1から9998までを指定してください。")]
    [InlineData("2026", "   ", "Name", "名称を入力してください。")]
    public async Task JavaScriptが無効でも入力不正はサーバーで拒否される(
        string year, string name, string field, string error) {
        await using var context = await CreateBrowserContextAsync(javaScriptEnabled: false);
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Maintain/CreateFiscalYear");
        await page.GetByLabel("年度", new() { Exact = true }).FillAsync(year);
        await page.GetByLabel("名称", new() { Exact = true }).FillAsync(name);
        // ブラウザー標準の入力制約も外し、サーバー側の検証を確認する。
        await page.Locator("form").EvaluateAsync("form => form.noValidate = true");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator($"[data-valmsg-for='{field}']")).ToHaveTextAsync(error);
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(0, await verification.FiscalYears.CountAsync());
    }

    [Fact]
    public async Task 偽造防止トークンのない登録要求は拒否される() {
        await using var context = await CreateBrowserContextAsync();

        var response = await context.APIRequest.PostAsync("/Maintain/CreateFiscalYear");

        Assert.Equal(400, response.Status);
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(0, await verification.FiscalYears.CountAsync());
    }

    private Task<IBrowserContext> CreateBrowserContextAsync(bool javaScriptEnabled = true) =>
        fixture.Browser.NewContextAsync(new() { BaseURL = fixture.BaseUrl, JavaScriptEnabled = javaScriptEnabled });

    private async Task ClearFiscalYearsAsync() {
        await using var database = fixture.CreateDbContext();
        await database.FiscalYears.ExecuteDeleteAsync();
    }
}
