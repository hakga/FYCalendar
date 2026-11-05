using App.Domain.Services;
using FluentAssertions;
using FYCalendar.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace App.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class UserServiceTests {
    [Fact]
    public async Task 利用者の入力内容とDBの既定値が別コンテキストから取得できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var created = await new UserService(context)
            .CreateAsync("  佐藤 花子  ", " sato@example.com ", " America/New_York ");

        created.Id.Should().BeGreaterThan(0);
        await using var verification = database.CreateContext();
        var saved = await verification.Users.SingleAsync();
        saved.Id.Should().Be(created.Id);
        saved.Name.Should().Be("佐藤 花子");
        saved.Email.Should().Be("sato@example.com");
        saved.TimeZoneId.Should().Be("America/New_York");
        saved.CreatedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
        saved.UpdatedAt.Should().BeAfter(DateTimeOffset.UnixEpoch);
        saved.RowVersion.Should().HaveCount(8);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task メール未入力の利用者は日本時間を既定として登録される(string? email) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        await new UserService(context).CreateAsync("利用者", email);

        await using var verification = database.CreateContext();
        var saved = await verification.Users.SingleAsync();
        saved.Email.Should().BeNull();
        saved.TimeZoneId.Should().Be("Asia/Tokyo");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task 氏名が空の利用者は登録できない(string? name) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Func<Task> create = () => new UserService(context).CreateAsync(name!);

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("name");
        (await context.Users.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("timeZoneId")]
    public async Task 文字数の上限を超える利用者情報は保存されない(string field) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var name = field == "name" ? new string('名', 101) : "利用者";
        var email = field == "email" ? new string('a', 244) + "@example.com" : null;
        var timeZoneId = field == "timeZoneId" ? new string('a', 65) : "Asia/Tokyo";

        Func<Task> create = () => new UserService(context).CreateAsync(name, email, timeZoneId);

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName(field);
        (await context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 不正なメールアドレスの利用者は登録できない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Func<Task> create = () => new UserService(context).CreateAsync("利用者", "invalid");

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("email");
        (await context.Users.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("No/Such_Time_Zone")]
    public async Task 無効なタイムゾーンの利用者は登録できない(string timeZoneId) {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Func<Task> create = () => new UserService(context).CreateAsync("利用者", timeZoneId: timeZoneId);

        await create.Should().ThrowAsync<ArgumentException>().WithParameterName("timeZoneId");
        (await context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task 同姓同名や同じメールの利用者も別のIDで登録できる() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new UserService(context);

        var first = await service.CreateAsync("同じ氏名", "shared@example.com");
        var second = await service.CreateAsync("同じ氏名", "shared@example.com");

        first.Id.Should().NotBe(second.Id);
        (await context.Users.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task 利用者一覧は登録の新しい順に取得され変更は保存されない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext()) {
            var service = new UserService(seed);
            await service.CreateAsync("最初の利用者");
            await service.CreateAsync("次の利用者");
        }
        await using var context = database.CreateContext();

        var users = await new UserService(context).GetAllAsync();

        users.Select(user => user.Name).Should().Equal("次の利用者", "最初の利用者");
        users[0].Name = "保存されない変更";
        await context.SaveChangesAsync();
        await using var verification = database.CreateContext();
        (await verification.Users.SingleAsync(user => user.Id == users[0].Id)).Name.Should().Be("次の利用者");
    }

    [Fact]
    public async Task 利用者が未登録なら空の一覧を返す() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var users = await new UserService(context).GetAllAsync();

        users.Should().BeEmpty();
    }

    [Fact]
    public async Task キャンセルされた利用者は登録されない() {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> create = () => new UserService(context).CreateAsync("利用者", cancellationToken: cancellation.Token);

        await create.Should().ThrowAsync<OperationCanceledException>();
        await using var verification = database.CreateContext();
        (await verification.Users.CountAsync()).Should().Be(0);
    }
}
