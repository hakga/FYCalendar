using App.Controllers;
using App.Data;
using App.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FYCalendar.TestSupport;

public static class TestApplication {
    public static WebApplication Create(SqliteConnection connection) {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            ApplicationName = typeof(HomeController).Assembly.GetName().Name,
            ContentRootPath = FindAppDirectory(),
            EnvironmentName = "Testing",
            Args = []
        });

        // テストはOSのイベントログやユーザープロファイルへの書き込みに依存させない。
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

        // 空きポートをOSが割り当てる。既存の開発サーバーとは独立して起動する。
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        return AppBootstrap.Build(builder, services =>
            services.AddScoped<AppDbContext>(_ => new TestAppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connection)
                    .Options)));
    }

    private static string FindAppDirectory() {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent) {
            var path = Path.Combine(directory.FullName, "App");
            if (File.Exists(Path.Combine(path, "App.csproj"))) {
                return path;
            }
        }

        throw new DirectoryNotFoundException("テスト対象のApp/App.csprojが見つかりません。");
    }
}
