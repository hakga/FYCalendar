using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using FYCalendar.TestSupport;

namespace App.E2ETests;

[CollectionDefinition("Browser", DisableParallelization = true)]
public sealed class BrowserCollection : ICollectionFixture<BrowserFixture>
{
}

public sealed class BrowserFixture : IAsyncLifetime
{
    private SqliteTestDatabase? _database;
    private WebApplication? _application;
    private IPlaywright? _playwright;
    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; private set; } = null!;

    public TestAppDbContext CreateDbContext() =>
        _database?.CreateContext() ?? throw new InvalidOperationException("テスト用DBが初期化されていません。");

    public async Task InitializeAsync()
    {
        try
        {
            _database = await SqliteTestDatabase.CreateAsync();
            _application = TestApplication.Create(_database.Connection);
            await _application.StartAsync();
            BaseUrl = _application.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            _playwright = await Playwright.CreateAsync();
            Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
            Browser = null!;
        }
        _playwright?.Dispose();
        _playwright = null;
        if (_application is not null)
        {
            await _application.DisposeAsync();
            _application = null;
        }
        if (_database is not null)
        {
            await _database.DisposeAsync();
            _database = null;
        }
    }
}
