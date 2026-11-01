using App.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Hosting;

/// <summary>通常起動と自動テストで共有するサービス登録・HTTPパイプライン。</summary>
public static class AppBootstrap
{
    public static WebApplication Build(
        WebApplicationBuilder builder,
        Action<IServiceCollection>? configureDatabase = null)
    {
        builder.Services.AddControllersWithViews();

        if (configureDatabase is null)
        {
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("AppConnection")
                    ?? throw new InvalidOperationException(
                        "ConnectionStrings:AppConnection が設定されていません。")));
        }
        else
        {
            configureDatabase(builder.Services);
        }

        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();
        app.MapControllerRoute(
            name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        return app;
    }
}
