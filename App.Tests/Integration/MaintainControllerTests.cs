using App.Controllers;
using App.Data;
using App.Domain.Services;
using App.Models.Maintain;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class MaintainControllerTests {
    [Theory]
    [InlineData("2027-03-31T14:59:00Z", 2026)]
    [InlineData("2027-03-31T15:00:00Z", 2027)]
    public async Task 登録画面の初期年度は日本時間の年度境界で切り替わる(string utcNow, int expectedYear) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context, DateTimeOffset.Parse(utcNow));

        var result = controller.CreateFiscalYear().Should().BeOfType<ViewResult>().Subject;

        var model = result.Model.Should().BeOfType<CreateFiscalYearViewModel>().Subject;
        model.Year.Should().Be(expectedYear);
        model.Name.Should().Be($"{expectedYear}年度");
    }

    [Fact]
    public async Task 年度一覧は新しい順に名称と期間と状態を表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new FiscalYearService(context);
        await service.CreateAsync(2025, "前年度");
        await service.CreateAsync(2026, "今年度");
        var controller = CreateController(context);

        var result = (await controller.FiscalYears(CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        var model = result.Model.Should().BeAssignableTo<IReadOnlyList<FiscalYearListItemViewModel>>().Subject;
        model.Select(item => item.Year).Should().Equal(2026, 2025);
        model[0].Name.Should().Be("今年度");
        model[0].StartsOn.Should().Be(new DateOnly(2026, 4, 1));
        model[0].EndsOn.Should().Be(new DateOnly(2027, 3, 31));
        model[0].StatusLabel.Should().Be("運用中");
    }

    [Fact]
    public async Task 登録後は完了メッセージを設定して一覧へ戻る() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);
        var model = new CreateFiscalYearViewModel { Year = 2026, Name = "今年度" };

        var result = (await controller.CreateFiscalYear(model, CancellationToken.None))
            .Should().BeOfType<RedirectToActionResult>().Subject;

        result.ActionName.Should().Be(nameof(MaintainController.FiscalYears));
        controller.TempData["SuccessMessage"].Should().Be("2026年度を登録しました。");
        await using var verification = database.CreateContext();
        var saved = await verification.FiscalYears.SingleAsync();
        saved.Year.Should().Be(2026);
        saved.Name.Should().Be("今年度");
    }

    [Fact]
    public async Task 入力エラーがあれば保存せず登録画面を再表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);
        var model = new CreateFiscalYearViewModel { Year = 2026 };
        controller.ModelState.AddModelError(nameof(model.Name), "名称を入力してください。");

        var result = (await controller.CreateFiscalYear(model, CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        (await context.FiscalYears.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 重複時は入力を保持して年度のエラーを表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await new FiscalYearService(context).CreateAsync(2026, "登録済み年度");
        var controller = CreateController(context);
        var model = new CreateFiscalYearViewModel { Year = 2026, Name = "重複年度" };

        var result = (await controller.CreateFiscalYear(model, CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        controller.ModelState[nameof(model.Year)]!.Errors.Should()
            .ContainSingle(error => error.ErrorMessage == "2026年度は既に登録されています。");
        (await context.FiscalYears.SingleAsync()).Name.Should().Be("登録済み年度");
    }

    [Fact]
    public async Task サービスの入力検証エラーは対応する項目に表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);
        var model = new CreateFiscalYearViewModel { Year = 2026, Name = new string('年', 51) };

        // 直接呼び出しではMVCの検証を通さず、サービス側の検証を確認する。
        var result = (await controller.CreateFiscalYear(model, CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        controller.ModelState[nameof(model.Name)]!.Errors.Should()
            .ContainSingle(error => error.ErrorMessage == "名称を入力し、50文字以内で指定してください。");
        (await context.FiscalYears.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 利用者登録画面は日本時間の選択肢を初期表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);

        var result = controller.CreateUser().Should().BeOfType<ViewResult>().Subject;

        var model = result.Model.Should().BeOfType<CreateUserViewModel>().Subject;
        model.TimeZoneId.Should().Be("Asia/Tokyo");
        model.TimeZoneOptions.Should().ContainSingle(item => item.Value == "Asia/Tokyo");
        model.TimeZoneOptions.Should().ContainSingle(item => item.Value == "UTC");
    }

    [Fact]
    public async Task 利用者一覧はIDと氏名とメールとタイムゾーンを表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new UserService(context);
        await service.CreateAsync("最初の利用者");
        var latest = await service.CreateAsync("次の利用者", "next@example.com", "UTC");
        var controller = CreateController(context);

        var result = (await controller.Users(CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;

        var model = result.Model.Should().BeAssignableTo<IReadOnlyList<UserListItemViewModel>>().Subject;
        model.Select(user => user.Name).Should().Equal("次の利用者", "最初の利用者");
        model[0].Id.Should().Be(latest.Id);
        model[0].Email.Should().Be("next@example.com");
        model[0].TimeZoneId.Should().Be("UTC");
    }

    [Fact]
    public async Task 利用者登録後は完了メッセージを設定して一覧へ戻る() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);
        var model = new CreateUserViewModel { Name = "利用者", Email = "user@example.com" };

        var result = (await controller.CreateUser(model, CancellationToken.None))
            .Should().BeOfType<RedirectToActionResult>().Subject;

        result.ActionName.Should().Be(nameof(MaintainController.Users));
        controller.TempData["SuccessMessage"].Should().Be("「利用者」を登録しました。");
        await using var verification = database.CreateContext();
        (await verification.Users.SingleAsync()).Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task 利用者の入力エラーでは内容と選択肢を保持して保存しない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);
        var model = new CreateUserViewModel { Name = "利用者", Email = "invalid", TimeZoneId = "UTC" };
        controller.ModelState.AddModelError(nameof(model.Email), "正しいメールアドレスを入力してください。");

        var result = (await controller.CreateUser(model, CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        model.Email.Should().Be("invalid");
        model.TimeZoneId.Should().Be("UTC");
        model.TimeZoneOptions.Should().NotBeEmpty();
        (await context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 無効なタイムゾーンは利用者登録画面にエラーを表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);
        var model = new CreateUserViewModel { Name = "利用者", TimeZoneId = "No/Such_Time_Zone" };

        var result = (await controller.CreateUser(model, CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        controller.ModelState[nameof(model.TimeZoneId)]!.Errors.Should()
            .ContainSingle(error => error.ErrorMessage == "有効なタイムゾーンを選択してください。");
        model.TimeZoneOptions.Should().Contain(item => item.Value == "Asia/Tokyo");
        (await context.Users.CountAsync()).Should().Be(0);
    }

    private static MaintainController CreateController(AppDbContext context, DateTimeOffset? utcNow = null) {
        var httpContext = new DefaultHttpContext();
        return new MaintainController(
            new FiscalYearService(context),
            new UserService(context),
            new FixedTimeProvider(utcNow ?? new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero))) {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new MemoryTempDataProvider())
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
