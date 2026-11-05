using App.Data;
using App.Data.Entities;
using App.Domain.Exceptions;
using App.Domain.Models;
using App.Domain.Services;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class OneTimeEventServiceTests {
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Start = new(2026, 11, 4, 15, 0, 0);

    [Fact]
    public async Task 一点の予定は1分間として定義と発生データをまとめて保存する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        var local = new DateTime(2026, 11, 4, 0, 30, 0);

        var result = await CreateService(context).RegisterAsync(new(calendar.Id, "  面談  ", local, Description: " 説明 "));

        result.RequiresConfirmation.Should().BeFalse();
        await using var verification = database.CreateContext();
        var entry = await verification.Events.SingleAsync();
        var schedule = await verification.EventSchedules.SingleAsync();
        var occurrence = await verification.EventOccurrences.SingleAsync();
        entry.Id.Should().Be(result.EventId);
        entry.Title.Should().Be("面談");
        entry.Description.Should().Be("説明");
        entry.UserCalendarId.Should().Be(calendar.Id);
        entry.Priority.Should().Be(50);
        entry.Status.Should().Be("active");
        schedule.StartsAt.Should().Be(new DateTimeOffset(2026, 11, 3, 15, 30, 0, TimeSpan.Zero));
        (schedule.EndsAt - schedule.StartsAt).Should().Be(TimeSpan.FromMinutes(1));
        schedule.RecurrenceRuleId.Should().BeNull();
        schedule.GenerationStatus.Should().Be("reflected");
        schedule.GeneratedVersion.Should().Be(schedule.DefinitionVersion);
        schedule.GeneratedFrom.Should().Be(Now);
        schedule.GeneratedUntil.Should().Be(Now.AddYears(1));
        schedule.GeneratedAt.Should().Be(Now);
        occurrence.EventId.Should().Be(entry.Id);
        occurrence.EventScheduleId.Should().Be(schedule.Id);
        occurrence.StartsAt.Should().Be(schedule.StartsAt);
        occurrence.EndsAt.Should().Be(schedule.EndsAt);
        occurrence.OccurrenceDate.Should().Be(new DateOnly(2026, 11, 4));
        occurrence.OccurrenceKey.Should().Be("single");
        occurrence.GenerationSource.Should().Be("immediate");
        occurrence.ReflectionStatus.Should().Be("reflected");
        occurrence.ReflectedAt.Should().Be(Now);
        occurrence.SourceVersion.Should().Be(1);
        occurrence.ReflectedVersion.Should().Be(1);
        (await verification.RecurrenceRules.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(-30, 0, false)]
    [InlineData(60, 90, false)]
    [InlineData(30, 90, true)]
    [InlineData(15, 45, true)]
    [InlineData(-30, 90, true)]
    public async Task 重複は半開区間で判定し未確認では保存しない(int startMinutes, int endMinutes, bool overlaps) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        var service = CreateService(context);
        var original = await service.RegisterAsync(new(calendar.Id, "先の予定", Start, Start.AddHours(1)));

        var result = await service.RegisterAsync(new(calendar.Id, "次の予定", Start.AddMinutes(startMinutes), Start.AddMinutes(endMinutes)));

        result.RequiresConfirmation.Should().Be(overlaps);
        (await context.Events.CountAsync()).Should().Be(overlaps ? 1 : 2);
        if (overlaps) {
            result.Conflicts.Should().ContainSingle(item => item.EventId == original.EventId && item.Title == "先の予定");
        }
    }

    [Fact]
    public async Task 重複を確認すれば既存予定を保持して新しい予定を登録できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        var service = CreateService(context);
        await service.RegisterAsync(new(calendar.Id, "先の予定", Start, Start.AddHours(1)));
        var request = new OneTimeEventRequest(calendar.Id, "確認済みの予定", Start.AddMinutes(30));
        (await service.RegisterAsync(request)).RequiresConfirmation.Should().BeTrue();

        var result = await service.RegisterAsync(request, confirmOverlap: true);

        result.EventId.Should().NotBeNull();
        (await context.Events.CountAsync()).Should().Be(2);
        (await context.EventOccurrences.CountAsync()).Should().Be(2);
        (await context.Events.OrderBy(entry => entry.Id).FirstAsync()).Title.Should().Be("先の予定");
    }

    [Fact]
    public async Task 同じ利用者の別カレンダーは時差を考慮して重複を警告する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        var other = await new UserCalendarService(context).CreateAsync(calendar.UserId, "UTCの予定", "UTC");
        var service = CreateService(context);
        await service.RegisterAsync(new(calendar.Id, "日本の予定", Start, Start.AddHours(1)));

        var result = await service.RegisterAsync(new(other.Id, "重複する予定", new DateTime(2026, 11, 4, 6, 30, 0)));

        result.RequiresConfirmation.Should().BeTrue();
        result.Conflicts.Should().ContainSingle().Which.StartsAt.Hour.Should().Be(6);
        result.Conflicts[0].StartsAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task 別の利用者の予定は重複警告の対象にしない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var first = await CreateCalendarAsync(context);
        var second = await CreateCalendarAsync(context);
        var service = CreateService(context);
        await service.RegisterAsync(new(first.Id, "先の予定", Start));

        (await service.RegisterAsync(new(second.Id, "別の利用者の予定", Start))).EventId.Should().NotBeNull();
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("draft")]
    public async Task 有効でない予定は重複警告の対象にしない(string status) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        var service = CreateService(context);
        await service.RegisterAsync(new(calendar.Id, "取り消す予定", Start));
        (await context.Events.SingleAsync()).Status = status;
        await context.SaveChangesAsync();

        (await service.RegisterAsync(new(calendar.Id, "新しい予定", Start))).EventId.Should().NotBeNull();
    }

    [Theory]
    [InlineData("2026-11-02T10:00", "2026-11-02T10:01", 0)]
    [InlineData("2026-11-02T23:59", "2026-11-03T00:00", 0)]
    [InlineData("2026-11-02T23:59", "2026-11-03T00:01", 1)]
    [InlineData("2027-11-02T23:59", "2027-11-03T00:00", 1)]
    [InlineData("2027-11-03T00:00", "2027-11-03T00:01", 0)]
    [InlineData("2028-11-03T00:00", "2028-11-03T00:01", 0)]
    public async Task 発生データは今後1年の範囲と重なる予定だけ生成する(string start, string end, int expectedCount) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context, "UTC");

        await CreateService(context).RegisterAsync(new(calendar.Id, "範囲の境界", DateTime.Parse(start), DateTime.Parse(end)));

        (await context.EventSchedules.CountAsync()).Should().Be(1);
        (await context.EventOccurrences.CountAsync()).Should().Be(expectedCount);
        if (expectedCount == 1) {
            (await context.EventOccurrences.SingleAsync()).StartsAt
                .Should().Be(new DateTimeOffset(DateTime.SpecifyKind(DateTime.Parse(start), DateTimeKind.Utc)));
        }
    }

    [Fact]
    public async Task 発生データのない将来の予定でも重複を検出できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        var request = new OneTimeEventRequest(calendar.Id, "将来の予定", new DateTime(2028, 11, 4, 15, 0, 0));
        var service = CreateService(context);
        await service.RegisterAsync(request);

        (await service.RegisterAsync(request)).RequiresConfirmation.Should().BeTrue();
        (await context.EventOccurrences.CountAsync()).Should().Be(0);
        (await context.Events.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData("gap-start", "StartsAtLocal")]
    [InlineData("ambiguous-start", "StartsAtLocal")]
    [InlineData("gap-end", "EndsAtLocal")]
    [InlineData("ambiguous-end", "EndsAtLocal")]
    public async Task 夏時間で存在しない時刻や曖昧な時刻は拒否する(string scenario, string field) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context, "America/New_York");
        var ambiguous = scenario.Contains("ambiguous");
        var start = ambiguous ? new DateTime(2027, 11, 7, 0, 30, 0) : new DateTime(2027, 3, 14, 1, 30, 0);
        var invalid = ambiguous ? new DateTime(2027, 11, 7, 1, 30, 0) : new DateTime(2027, 3, 14, 2, 30, 0);
        var request = scenario.EndsWith("start")
            ? new OneTimeEventRequest(calendar.Id, "夏時間", invalid)
            : new OneTimeEventRequest(calendar.Id, "夏時間", start, invalid);

        Func<Task> register = () => CreateService(context).RegisterAsync(request);

        (await register.Should().ThrowAsync<EventValidationException>()).Which.FieldName.Should().Be(field);
        (await context.Events.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 夏時間の切り替えをまたぐ一点の予定も実時間で1分になる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context, "America/New_York");

        await CreateService(context).RegisterAsync(new(calendar.Id, "切り替え直前", new DateTime(2027, 3, 14, 1, 59, 0)));

        var schedule = await context.EventSchedules.SingleAsync();
        (schedule.EndsAt - schedule.StartsAt).Should().Be(TimeSpan.FromMinutes(1));
        TimeZoneInfo.ConvertTime(schedule.EndsAt, TimeZoneInfo.FindSystemTimeZoneById(calendar.TimeZoneId)).Hour.Should().Be(3);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("start-second")]
    [InlineData("end-second")]
    [InlineData("end-before")]
    [InlineData("calendar-missing")]
    [InlineData("calendar-inactive")]
    [InlineData("calendar-type")]
    public async Task 不正な予定は定義も発生データも保存しない(string scenario) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        if (scenario == "calendar-inactive") calendar.IsActive = false;
        if (scenario == "calendar-type") calendar.CalendarType = "holiday";
        await context.SaveChangesAsync();
        var request = new OneTimeEventRequest(calendar.Id, "面談", Start);
        request = scenario switch {
            "title" => request with { Title = new string('予', 301) },
            "start-second" => request with { StartsAtLocal = Start.AddTicks(1) },
            "end-second" => request with { EndsAtLocal = Start.AddMinutes(1).AddSeconds(1) },
            "end-before" => request with { EndsAtLocal = Start },
            "calendar-missing" => request with { UserCalendarId = long.MaxValue },
            _ => request
        };

        Func<Task> register = () => CreateService(context).RegisterAsync(request);

        await register.Should().ThrowAsync<EventValidationException>();
        (await context.Events.CountAsync()).Should().Be(0);
        (await context.EventSchedules.CountAsync()).Should().Be(0);
        (await context.EventOccurrences.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 発生データの保存に失敗したら予定全体をロールバックする() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER RejectOccurrence BEFORE INSERT ON EventOccurrences
            BEGIN SELECT RAISE(ABORT, 'Rejected for integration test'); END;
            """);

        Func<Task> register = () => CreateService(context).RegisterAsync(new(calendar.Id, "失敗する予定", Start));

        await register.Should().ThrowAsync<DbUpdateException>();
        await using var verification = database.CreateContext();
        (await verification.Events.CountAsync()).Should().Be(0);
        (await verification.EventSchedules.CountAsync()).Should().Be(0);
        (await verification.EventOccurrences.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("generated", "reflected", true)]
    [InlineData("skipped", "reflected", false)]
    [InlineData("cancelled", "reflected", false)]
    [InlineData("generated", "pending", false)]
    public async Task 有効な繰り返し発生データも重複確認の対象になる(string status, string reflection, bool warns) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        var entry = new Event { UserCalendarId = calendar.Id, Title = "定例会議" };
        var schedule = new EventSchedule {
            Event = entry,
            StartsAt = new DateTimeOffset(2026, 10, 28, 6, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 10, 28, 7, 0, 0, TimeSpan.Zero),
            TimeZoneId = "Asia/Tokyo",
            RecurrenceRule = new RecurrenceRule {
                Frequency = "weekly", IntervalValue = 1, StartsOn = new DateOnly(2026, 10, 28), HolidayBehavior = "execute"
            }
        };
        context.EventOccurrences.Add(new EventOccurrence {
            Event = entry, EventSchedule = schedule,
            StartsAt = new DateTimeOffset(2026, 11, 4, 6, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 11, 4, 7, 0, 0, TimeSpan.Zero),
            OccurrenceDate = new DateOnly(2026, 11, 4), OccurrenceKey = "20261104",
            OccurrenceStatus = status, ReflectionStatus = reflection
        });
        await context.SaveChangesAsync();

        var result = await CreateService(context).RegisterAsync(new(calendar.Id, "面談", Start.AddMinutes(30)));

        result.RequiresConfirmation.Should().Be(warns);
        if (warns) result.Conflicts.Should().ContainSingle(item => item.Title == "定例会議");
    }

    [Theory]
    [InlineData("Asia/Tokyo", 1, 1, 1, 0, 0)]
    [InlineData("UTC", 9999, 12, 31, 23, 59)]
    public async Task 表現できない日時の一点の予定は拒否する(string zone, int year, int month, int day, int hour, int minute) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context, zone);

        Func<Task> register = () => CreateService(context)
            .RegisterAsync(new(calendar.Id, "範囲外", new DateTime(year, month, day, hour, minute, 0)));

        await register.Should().ThrowAsync<EventValidationException>();
        (await context.Events.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 予定一覧は発生データの有無によらず現地表示に必要な情報を取得できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext()) {
            var calendar = await CreateCalendarAsync(seed);
            var service = CreateService(seed);
            await service.RegisterAsync(new(calendar.Id, "近い予定", Start));
            await service.RegisterAsync(new(calendar.Id, "将来の予定", Start.AddYears(2)));
        }
        await using var context = database.CreateContext();

        var schedules = await CreateService(context).GetAllAsync();

        schedules.Select(schedule => schedule.Event.Title).Should().Equal("将来の予定", "近い予定");
        schedules[0].Event.UserCalendar!.User.Name.Should().Be("所有者");
        schedules[0].Event.Title = "保存されない変更";
        await context.SaveChangesAsync();
        await using var verification = database.CreateContext();
        (await verification.Events.SingleAsync(entry => entry.Id == schedules[0].EventId)).Title.Should().Be("将来の予定");
    }

    [Fact]
    public async Task キャンセルされた予定は保存されない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var calendar = await CreateCalendarAsync(context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> register = () => CreateService(context)
            .RegisterAsync(new(calendar.Id, "キャンセル", Start), cancellationToken: cancellation.Token);

        await register.Should().ThrowAsync<OperationCanceledException>();
        (await context.Events.CountAsync()).Should().Be(0);
    }

    private static OneTimeEventService CreateService(AppDbContext context) => new(context, new FixedTimeProvider());

    private static async Task<UserCalendar> CreateCalendarAsync(AppDbContext context, string zone = "Asia/Tokyo") {
        var user = await new UserService(context).CreateAsync("所有者", timeZoneId: zone);
        return await new UserCalendarService(context).CreateAsync(user.Id, "個人の予定");
    }

    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
