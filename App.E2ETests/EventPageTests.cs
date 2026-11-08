using App.Domain.Models;
using App.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace App.E2ETests;

[Collection("Browser")]
[Trait("Category", "E2E")]
public sealed class EventPageTests(BrowserFixture fixture) : IAsyncLifetime {
    private static DateTime Start => DateTime.UtcNow.Date.AddDays(1).AddHours(10);

    public Task InitializeAsync() => ClearDataAsync();
    public Task DisposeAsync() => ClearDataAsync();

    [Fact]
    public async Task カレンダー未登録なら予定登録からカレンダー登録へ案内する() {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");
        await page.GetByRole(AriaRole.Navigation).GetByRole(AriaRole.Link, new() { Name = "予定一覧", Exact = true }).ClickAsync();
        await Expect(page.GetByText("予定が登録されていません。", new() { Exact = true })).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "予定を登録", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true })).ToBeDisabledAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "個人カレンダーを登録", Exact = true }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "個人カレンダー登録", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task カレンダーから一点の予定を登録すると1分間として一覧に表示される() {
        var calendarId = await CreateCalendarAsync();
        var start = Start;
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/Calendars/UserCalendars");
        await page.GetByRole(AriaRole.Link, new() { Name = "予定を登録", Exact = true }).ClickAsync();
        await Expect(page.GetByLabel("個人カレンダー", new() { Exact = true })).ToHaveValueAsync(calendarId.ToString());
        await FillEventAsync(page, "  面談  ", start);
        await page.GetByLabel("説明", new() { Exact = true }).FillAsync("打ち合わせの説明");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Calendars/Events");
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("「面談」を登録しました。");
        await using var database = fixture.CreateDbContext();
        var entry = await database.Events.SingleAsync();
        await Expect(page.Locator("tbody tr").Locator("th, td")).ToHaveTextAsync(new[] {
            entry.Id.ToString(), "所有者", "個人の予定", "面談", start.ToString("yyyy/MM/dd HH:mm"),
            start.AddMinutes(1).ToString("yyyy/MM/dd HH:mm"), "Asia/Tokyo", "50"
        });
        var occurrence = await database.EventOccurrences.SingleAsync();
        Assert.Equal(TimeSpan.FromMinutes(1), occurrence.EndsAt - occurrence.StartsAt);
        Assert.Equal("immediate", occurrence.GenerationSource);
        Assert.Equal("reflected", occurrence.ReflectionStatus);
        Assert.Equal("打ち合わせの説明", entry.Description);
        await page.ReloadAsync();
        await Expect(page.Locator("tbody tr")).ToHaveCountAsync(1);
        Assert.Equal(1, await database.Events.CountAsync());
    }

    [Fact]
    public async Task 開始と終了を指定した期間の予定を登録できる() {
        var calendarId = await CreateCalendarAsync();
        var start = Start;
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Calendars/CreateEvent?userCalendarId={calendarId}");
        await FillEventAsync(page, "会議", start, start.AddHours(2));

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Calendars/Events");
        await using var database = fixture.CreateDbContext();
        var schedule = await database.EventSchedules.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(2), schedule.EndsAt - schedule.StartsAt);
    }

    [Fact]
    public async Task 重複は保存前に確認し入力を変えたら確認をやり直す() {
        var calendarId = await CreateCalendarAsync();
        var start = Start;
        await SeedEventAsync(calendarId, "先の会議", start, start.AddHours(1));
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Calendars/CreateEvent?userCalendarId={calendarId}");
        await FillEventAsync(page, "重複する面談", start.AddMinutes(30));

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator("[data-conflict-warning]")).ToContainTextAsync("先の会議");
        await Expect(page.GetByLabel("件名", new() { Exact = true })).ToHaveValueAsync("重複する面談");
        await using (var database = fixture.CreateDbContext()) {
            Assert.Equal(1, await database.Events.CountAsync());
        }
        var confirmation = page.GetByLabel("重複を確認して登録する", new() { Exact = true });
        await confirmation.CheckAsync();
        await page.GetByLabel("件名", new() { Exact = true }).FillAsync("変更した面談");
        await Expect(confirmation).Not.ToBeCheckedAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();
        await Expect(page.Locator("[data-conflict-warning]")).ToBeVisibleAsync();
        await page.GetByLabel("重複を確認して登録する", new() { Exact = true }).CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Calendars/Events");
        await Expect(page.Locator("tbody tr")).ToHaveCountAsync(2);
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(2, await verification.Events.CountAsync());
        Assert.Equal(2, await verification.EventOccurrences.CountAsync());
    }

    [Theory]
    [InlineData("title", "Title", "件名を入力してください。")]
    [InlineData("start", "StartsAtLocal", "開始日時を入力してください。")]
    [InlineData("end", "EndsAtLocal", "終了日時は開始日時より後にしてください。")]
    [InlineData("seconds", "StartsAtLocal", "日時は秒を含めず、分単位で入力してください。")]
    public async Task JavaScriptが無効でも予定の入力不正はサーバーで拒否される(string scenario, string field, string error) {
        var calendarId = await CreateCalendarAsync();
        var start = Start;
        await using var context = await CreateBrowserContextAsync(javaScriptEnabled: false);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Calendars/CreateEvent?userCalendarId={calendarId}");
        await FillEventAsync(page, "面談", start);
        if (scenario == "title") await page.GetByLabel("件名", new() { Exact = true }).FillAsync("   ");
        if (scenario == "start") await page.GetByLabel("開始日時", new() { Exact = true }).FillAsync("");
        if (scenario == "end") await page.GetByLabel("終了日時", new() { Exact = true }).FillAsync(start.ToString("yyyy-MM-ddTHH:mm"));
        if (scenario == "seconds") await page.GetByLabel("開始日時", new() { Exact = true }).FillAsync(start.AddSeconds(1).ToString("yyyy-MM-ddTHH:mm:ss"));
        await page.Locator("form").EvaluateAsync("form => form.noValidate = true");

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator($"[data-valmsg-for='{field}']")).ToHaveTextAsync(error);
        await Expect(page.GetByLabel("個人カレンダー", new() { Exact = true })).ToHaveValueAsync(calendarId.ToString());
        await using var database = fixture.CreateDbContext();
        Assert.Equal(0, await database.Events.CountAsync());
    }

    [Fact]
    public async Task 画面表示後に無効になったカレンダーには登録できない() {
        var calendarId = await CreateCalendarAsync();
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Calendars/CreateEvent?userCalendarId={calendarId}");
        await FillEventAsync(page, "面談", Start);
        await using (var database = fixture.CreateDbContext()) {
            var calendar = await database.UserCalendars.SingleAsync();
            calendar.IsActive = false;
            await database.SaveChangesAsync();
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page.Locator("[data-valmsg-for='UserCalendarId']")).ToHaveTextAsync("有効な予定用の個人カレンダーを選択してください。");
        await Expect(page.GetByLabel("件名", new() { Exact = true })).ToHaveValueAsync("面談");
        await using var verification = fixture.CreateDbContext();
        Assert.Equal(0, await verification.Events.CountAsync());
    }

    [Fact]
    public async Task 発生データの生成範囲より先の予定も登録して一覧に表示できる() {
        var calendarId = await CreateCalendarAsync();
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"/Calendars/CreateEvent?userCalendarId={calendarId}");
        await FillEventAsync(page, "将来の面談", Start.AddYears(2));

        await page.GetByRole(AriaRole.Button, new() { Name = "登録する", Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Calendars/Events");
        await Expect(page.Locator("tbody tr")).ToContainTextAsync("将来の面談");
        await using var database = fixture.CreateDbContext();
        Assert.Equal(1, await database.EventSchedules.CountAsync());
        Assert.Equal(0, await database.EventOccurrences.CountAsync());
    }

    [Fact]
    public async Task 偽造防止トークンのない予定登録要求は拒否される() {
        await using var context = await CreateBrowserContextAsync();

        var response = await context.APIRequest.PostAsync("/Calendars/CreateEvent");

        Assert.Equal(400, response.Status);
        await using var database = fixture.CreateDbContext();
        Assert.Equal(0, await database.Events.CountAsync());
    }

    private Task<IBrowserContext> CreateBrowserContextAsync(bool javaScriptEnabled = true) =>
        fixture.Browser.NewContextAsync(new() { BaseURL = fixture.BaseUrl, JavaScriptEnabled = javaScriptEnabled });

    private static async Task FillEventAsync(IPage page, string title, DateTime start, DateTime? end = null) {
        await page.GetByLabel("件名", new() { Exact = true }).FillAsync(title);
        await page.GetByLabel("開始日時", new() { Exact = true }).FillAsync(start.ToString("yyyy-MM-ddTHH:mm"));
        if (end is { } value) await page.GetByLabel("終了日時", new() { Exact = true }).FillAsync(value.ToString("yyyy-MM-ddTHH:mm"));
    }

    private async Task<long> CreateCalendarAsync() {
        await using var database = fixture.CreateDbContext();
        var user = await new UserService(database).CreateAsync("所有者");
        return (await new UserCalendarService(database).CreateAsync(user.Id, "個人の予定")).Id;
    }

    private async Task SeedEventAsync(long calendarId, string title, DateTime start, DateTime end) {
        await using var database = fixture.CreateDbContext();
        await new OneTimeEventService(database, TimeProvider.System).RegisterAsync(new(calendarId, title, start, end));
    }

    private async Task ClearDataAsync() {
        await using var database = fixture.CreateDbContext();
        await database.EventOccurrences.ExecuteDeleteAsync();
        await database.EventSchedules.ExecuteDeleteAsync();
        await database.Events.ExecuteDeleteAsync();
        await database.UserCalendars.ExecuteDeleteAsync();
        await database.Users.ExecuteDeleteAsync();
    }
}
