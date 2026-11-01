using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace App.E2ETests;

[Collection("Browser")]
[Trait("Category", "E2E")]
public sealed class HomePageTests(BrowserFixture fixture)
{
    [Fact]
    public async Task Home_ShowsWelcomeHeading()
    {
        await using var context = await fixture.Browser.NewContextAsync(new()
        {
            BaseURL = fixture.BaseUrl
        });
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync("/");

        Assert.Equal(200, response!.Status);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Welcome", Exact = true }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Navigation_OpensPrivacyPage()
    {
        await using var context = await fixture.Browser.NewContextAsync(new()
        {
            BaseURL = fixture.BaseUrl
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("/");

        await page.GetByRole(AriaRole.Navigation)
            .GetByRole(AriaRole.Link, new() { Name = "Privacy", Exact = true }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Privacy Policy", Exact = true }))
            .ToBeVisibleAsync();
        await Expect(page).ToHaveURLAsync($"{fixture.BaseUrl}/Home/Privacy");
    }
}
