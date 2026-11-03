using App.Data;
using App.Domain.Exceptions;
using App.Domain.Services;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class FiscalYearServiceTests {
    [Fact]
    public async Task 年度は名称と期間を整えてopenで登録される() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new FiscalYearService(context);

        var created = await service.CreateAsync(2026, "  2026年度  ");

        created.Id.Should().BeGreaterThan(0);
        await using var verification = database.CreateContext();
        var saved = await verification.FiscalYears.SingleAsync();
        saved.Id.Should().Be(created.Id);
        saved.Year.Should().Be(2026);
        saved.Name.Should().Be("2026年度");
        saved.StartsOn.Should().Be(new DateOnly(2026, 4, 1));
        saved.EndsOn.Should().Be(new DateOnly(2027, 3, 31));
        saved.Status.Should().Be("open");
    }

    [Fact]
    public async Task 名称は空白除去後に50文字まで登録できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new FiscalYearService(context);
        var name = new string('年', 50);

        await service.CreateAsync(2023, $" {name} ");

        await using var verification = database.CreateContext();
        var saved = await verification.FiscalYears.SingleAsync();
        saved.Name.Should().Be(name);
        saved.EndsOn.Should().Be(new DateOnly(2024, 3, 31));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task 名称が空の年度は登録できない(string? name) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new FiscalYearService(context);

        Func<Task> create = () => service.CreateAsync(2026, name!);

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("name");
        (await context.FiscalYears.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 名称が50文字を超える年度は登録できない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new FiscalYearService(context);

        Func<Task> create = () => service.CreateAsync(2026, new string('年', 51));

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("name");
        (await context.FiscalYears.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9999)]
    public async Task 範囲外の年度は登録できない(int year) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new FiscalYearService(context);

        Func<Task> create = () => service.CreateAsync(year, "範囲外年度");

        await create.Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("year");
        (await context.FiscalYears.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 同じ年度は重複登録できず既存データを保持する() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var originalContext = database.CreateContext()) {
            await new FiscalYearService(originalContext).CreateAsync(2026, "登録済み年度");
        }
        await using var context = database.CreateContext();
        var service = new FiscalYearService(context);

        Func<Task> create = () => service.CreateAsync(2026, "重複年度");

        var exception = (await create.Should().ThrowAsync<DuplicateFiscalYearException>()).Which;
        exception.Year.Should().Be(2026);
        await using var verification = database.CreateContext();
        var saved = await verification.FiscalYears.SingleAsync();
        saved.Name.Should().Be("登録済み年度");
    }

    [Fact]
    public async Task 保存直前の重複を検出して次の年度を登録できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(database.Connection)
            .AddInterceptors(new InsertCompetingFiscalYearInterceptor(database))
            .Options;
        await using var context = new TestAppDbContext(options);
        var service = new FiscalYearService(context);

        Func<Task> create = () => service.CreateAsync(2026, "後から保存する年度");

        var exception = (await create.Should().ThrowAsync<DuplicateFiscalYearException>()).Which;
        exception.Year.Should().Be(2026);
        exception.InnerException.Should().BeOfType<DbUpdateException>();
        await service.CreateAsync(2027, "次の年度");
        await using var verification = database.CreateContext();
        (await verification.FiscalYears.CountAsync()).Should().Be(2);
        (await verification.FiscalYears.SingleAsync(item => item.Year == 2026))
            .Name.Should().Be("先に保存した年度");
    }

    [Fact]
    public async Task 重複以外のDBエラーはそのまま通知される() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER RejectFiscalYearInsert
            BEFORE INSERT ON FiscalYears
            BEGIN
                SELECT RAISE(ABORT, 'Rejected for integration test');
            END;
            """);
        var service = new FiscalYearService(context);

        Func<Task> create = () => service.CreateAsync(2026, "保存できない年度");

        await create.Should().ThrowAsync<DbUpdateException>();
        (await context.FiscalYears.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 年度一覧は新しい順に取得できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext()) {
            var service = new FiscalYearService(seed);
            await service.CreateAsync(2025, "2025年度");
            await service.CreateAsync(2027, "2027年度");
            await service.CreateAsync(2026, "2026年度");
        }
        await using var context = database.CreateContext();

        var fiscalYears = await new FiscalYearService(context).GetAllAsync();

        fiscalYears.Select(item => item.Year).Should().Equal(2027, 2026, 2025);
    }

    [Fact]
    public async Task 年度が未登録なら空の一覧を返す() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var fiscalYears = await new FiscalYearService(context).GetAllAsync();

        fiscalYears.Should().BeEmpty();
    }

    [Fact]
    public async Task 指定年度の取得結果を変更しても保存されない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext()) {
            var service = new FiscalYearService(seed);
            await service.CreateAsync(2025, "2025年度");
            await service.CreateAsync(2026, "2026年度");
        }
        await using var context = database.CreateContext();

        var found = await new FiscalYearService(context).FindByYearAsync(2026);

        found.Should().NotBeNull();
        found!.Year.Should().Be(2026);
        found.Name.Should().Be("2026年度");
        found.Name = "保存されない変更";
        await context.SaveChangesAsync();
        await using var verification = database.CreateContext();
        (await verification.FiscalYears.SingleAsync(item => item.Year == 2026))
            .Name.Should().Be("2026年度");
    }

    [Fact]
    public async Task 未登録の年度はnullを返す() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var found = await new FiscalYearService(context).FindByYearAsync(2026);

        found.Should().BeNull();
    }

    [Fact]
    public async Task キャンセルされた年度は登録されない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new FiscalYearService(context);

        Func<Task> create = () => service.CreateAsync(2026, "2026年度", cancellation.Token);

        await create.Should().ThrowAsync<OperationCanceledException>();
        (await context.FiscalYears.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DI経由で年度を登録できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var application = TestApplication.Create(database.Connection);
        await using var scope = application.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<FiscalYearService>();

        await service.CreateAsync(2026, "DI経由の登録");

        await using var verification = database.CreateContext();
        (await verification.FiscalYears.SingleAsync()).Name.Should().Be("DI経由の登録");
    }

    // 事前チェック後、保存の直前に別コンテキストが同じ年度を登録する状況を再現する。
    private sealed class InsertCompetingFiscalYearInterceptor(SqliteTestDatabase database) : SaveChangesInterceptor {
        private bool _hasInserted;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            if (!_hasInserted) {
                _hasInserted = true;
                await using var competingContext = database.CreateContext();
                await new FiscalYearService(competingContext)
                    .CreateAsync(2026, "先に保存した年度", cancellationToken);
            }

            return result;
        }
    }
}
