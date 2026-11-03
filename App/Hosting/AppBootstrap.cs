using App.Data;
using App.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace App.Hosting;
/*
 * 当ファイルはPlaywrightのテストでも通常と共通の設定でアプリを起動するために共通する処理をまとめたものです。
 Build の第2引数 configureDatabase にテスト用の登録処理を渡すと、DB接続をSQLiteへ差し替えられる。
 これにより、コントローラーや画面、年度サービスは実際のコードを使いながら、テスト用DBで動作を確認できる。
*/
/// <summary>通常起動と自動テストで共有するサービス登録・HTTPパイプライン。</summary>
public static class AppBootstrap {
    public static WebApplication Build(
        WebApplicationBuilder builder,
        Action<IServiceCollection>? configureDatabase = null) {
        builder.Services.AddControllersWithViews();
        builder.Services.AddScoped<FiscalYearService>();
        builder.Services.AddSingleton(TimeProvider.System);

        if (configureDatabase is null) {
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("AppConnection")
                    ?? throw new InvalidOperationException(
                        "ConnectionStrings:AppConnection が設定されていません。")));
        }
        else {
            configureDatabase(builder.Services);
        }

        var app = builder.Build();
        if (!app.Environment.IsDevelopment()) {
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
