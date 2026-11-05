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

    [Fact]
    public async Task 利用者未登録の個人カレンダー画面は利用者なしのモデルを返す() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var result = (await CreateController(context).CreateUserCalendar(CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        var model = result.Model.Should().BeOfType<CreateUserCalendarViewModel>().Subject;
        model.UserOptions.Should().BeEmpty();
        model.UserId.Should().BeNull();
        model.TimeZoneId.Should().BeNull();
        model.TimeZoneOptions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task 個人カレンダーの利用者選択肢は同姓同名でもIDで区別できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new UserService(context);
        var first = await service.CreateAsync("同じ氏名", timeZoneId: "UTC");
        var second = await service.CreateAsync("同じ氏名");

        var result = (await CreateController(context).CreateUserCalendar(CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        var model = result.Model.Should().BeOfType<CreateUserCalendarViewModel>().Subject;
        model.UserOptions.Select(option => option.Value).Should().Equal(second.Id.ToString(), first.Id.ToString());
        model.UserOptions.Should().Contain(option => option.Text.Contains($"ID: {first.Id}") && option.Text.Contains("UTC"));
        model.UserOptions.Should().Contain(option => option.Text.Contains($"ID: {second.Id}"));
    }

    [Fact]
    public async Task 個人カレンダー一覧は所有者と設定を表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者", timeZoneId: "UTC");
        var created = await new UserCalendarService(context).CreateAsync(user.Id, "業務予定", displayColor: "#aBc123");

        var result = (await CreateController(context).UserCalendars(CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        var model = result.Model.Should().BeAssignableTo<IReadOnlyList<UserCalendarListItemViewModel>>().Subject;
        var calendar = model.Should().ContainSingle().Subject;
        calendar.Id.Should().Be(created.Id);
        calendar.UserId.Should().Be(user.Id);
        calendar.UserName.Should().Be("所有者");
        calendar.Name.Should().Be("業務予定");
        calendar.TimeZoneId.Should().Be("UTC");
        calendar.DisplayColor.Should().Be("#ABC123");
        calendar.CalendarTypeLabel.Should().Be("予定");
        calendar.StatusLabel.Should().Be("有効");
    }

    [Fact]
    public async Task 個人カレンダー登録後は設定を保存して一覧へ戻る() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者", timeZoneId: "UTC");
        var controller = CreateController(context);
        var model = new CreateUserCalendarViewModel { UserId = user.Id, Name = "業務予定" };

        var result = (await controller.CreateUserCalendar(model, CancellationToken.None))
            .Should().BeOfType<RedirectToActionResult>().Subject;

        result.ActionName.Should().Be(nameof(MaintainController.UserCalendars));
        controller.TempData["SuccessMessage"].Should().Be("「業務予定」を登録しました。");
        await using var verification = database.CreateContext();
        var saved = await verification.UserCalendars.SingleAsync();
        saved.UserId.Should().Be(user.Id);
        saved.TimeZoneId.Should().Be("UTC");
    }

    [Fact]
    public async Task 個人カレンダーの入力エラーは内容と選択肢を保持して保存しない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");
        var controller = CreateController(context);
        var model = new CreateUserCalendarViewModel {
            UserId = user.Id, Name = "業務予定", TimeZoneId = "UTC", DisplayColor = "invalid"
        };
        controller.ModelState.AddModelError(nameof(model.DisplayColor), "表示色が不正です。");

        var result = (await controller.CreateUserCalendar(model, CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        model.TimeZoneId.Should().Be("UTC");
        model.DisplayColor.Should().Be("invalid");
        model.UserOptions.Should().Contain(option => option.Value == user.Id.ToString());
        model.TimeZoneOptions.Should().NotBeEmpty();
        (await context.UserCalendars.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 存在しない利用者を指定すると項目にエラーを表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var controller = CreateController(context);
        var model = new CreateUserCalendarViewModel { UserId = long.MaxValue, Name = "業務予定" };

        var result = (await controller.CreateUserCalendar(model, CancellationToken.None))
            .Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        controller.ModelState[nameof(model.UserId)]!.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == "選択した利用者が見つかりません。利用者を選択し直してください。");
        (await context.UserCalendars.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 予定登録画面は有効な予定用カレンダーだけを選択肢にする() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");
        var service = new UserCalendarService(context);
        var active = await service.CreateAsync(user.Id, "有効な予定");
        var inactive = await service.CreateAsync(user.Id, "無効な予定");
        inactive.IsActive = false;
        await context.SaveChangesAsync();

        var result = (await CreateController(context).CreateEvent(CancellationToken.None, active.Id))
            .Should().BeOfType<ViewResult>().Subject;

        var model = result.Model.Should().BeOfType<CreateEventViewModel>().Subject;
        model.UserCalendarId.Should().Be(active.Id);
        model.CalendarOptions.Should().ContainSingle(option => option.Value == active.Id.ToString());
    }

    [Fact]
    public async Task 予定登録後は現地時刻で一覧に表示する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");
        var calendar = await new UserCalendarService(context).CreateAsync(user.Id, "個人の予定");
        var controller = CreateController(context);
        var model = new CreateEventViewModel {
            UserCalendarId = calendar.Id, Title = "面談", StartsAtLocal = new DateTime(2026, 11, 4, 15, 0, 0)
        };

        var registered = (await controller.CreateEvent(model, CancellationToken.None))
            .Should().BeOfType<RedirectToActionResult>().Subject;

        registered.ActionName.Should().Be(nameof(MaintainController.Events));
        controller.TempData["SuccessMessage"].Should().Be("「面談」を登録しました。");
        var result = (await controller.Events(CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;
        var list = result.Model.Should().BeAssignableTo<IReadOnlyList<EventListItemViewModel>>().Subject;
        list.Should().ContainSingle().Which.StartsAt.Hour.Should().Be(15);
        list[0].EndsAt.Minute.Should().Be(1);
        list[0].StartsAt.Offset.Should().Be(TimeSpan.FromHours(9));
    }

    [Fact]
    public async Task 予定が重複したら保存前に確認を表示し確認後に登録する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");
        var calendar = await new UserCalendarService(context).CreateAsync(user.Id, "個人の予定");
        var controller = CreateController(context);
        var start = new DateTime(2026, 11, 4, 15, 0, 0);
        await controller.CreateEvent(new CreateEventViewModel {
            UserCalendarId = calendar.Id, Title = "先の予定", StartsAtLocal = start, EndsAtLocal = start.AddHours(1)
        }, CancellationToken.None);
        var model = new CreateEventViewModel { UserCalendarId = calendar.Id, Title = "次の予定", StartsAtLocal = start };

        var result = (await controller.CreateEvent(model, CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        model.Conflicts.Should().ContainSingle(item => item.Title == "先の予定");
        model.CalendarOptions.Should().NotBeEmpty();
        (await context.Events.CountAsync()).Should().Be(1);
        model.ConfirmOverlap = true;
        (await controller.CreateEvent(model, CancellationToken.None)).Should().BeOfType<RedirectToActionResult>();
        (await context.Events.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task 予定の入力エラーは入力と選択肢を保持して保存しない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var user = await new UserService(context).CreateAsync("所有者");
        var calendar = await new UserCalendarService(context).CreateAsync(user.Id, "個人の予定");
        var controller = CreateController(context);
        var model = new CreateEventViewModel { UserCalendarId = calendar.Id, Description = "入力済みの説明" };
        controller.ModelState.AddModelError(nameof(model.Title), "件名を入力してください。");

        var result = (await controller.CreateEvent(model, CancellationToken.None)).Should().BeOfType<ViewResult>().Subject;

        result.Model.Should().BeSameAs(model);
        model.Description.Should().Be("入力済みの説明");
        model.CalendarOptions.Should().ContainSingle();
        (await context.Events.CountAsync()).Should().Be(0);
    }

    private static MaintainController CreateController(AppDbContext context, DateTimeOffset? utcNow = null) {
        var httpContext = new DefaultHttpContext();
        return new MaintainController(
            new FiscalYearService(context),
            new UserService(context),
            new UserCalendarService(context),
            new OneTimeEventService(context,
                new FixedTimeProvider(utcNow ?? new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero))),
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
