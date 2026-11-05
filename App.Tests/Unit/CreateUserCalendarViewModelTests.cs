using System.ComponentModel.DataAnnotations;
using App.Models.Maintain;
using FluentAssertions;

namespace App.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class CreateUserCalendarViewModelTests {
    [Fact]
    public void カレンダー名は空白を除去して200文字まで入力できる() {
        var name = new string('名', 200);
        var model = new CreateUserCalendarViewModel { UserId = 1, Name = $" {name} ", DisplayColor = " #aBc123 " };

        Validate(model).Should().BeEmpty();
        model.Name.Should().Be(name);
        model.DisplayColor.Should().Be("#aBc123");
        model.TimeZoneId.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void 利用者が未選択または不正なら入力エラーになる(long? userId) {
        var model = new CreateUserCalendarViewModel { UserId = userId, Name = "カレンダー" };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.UserId)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void カレンダー名が未入力なら入力エラーになる(string? name) {
        var model = new CreateUserCalendarViewModel { UserId = 1, Name = name! };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.Name)));
    }

    [Fact]
    public void カレンダー名が200文字を超えると入力エラーになる() {
        var model = new CreateUserCalendarViewModel { UserId = 1, Name = new string('名', 201) };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.Name)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void タイムゾーンと表示色は未入力でも検証を通過する(string? value) {
        var model = new CreateUserCalendarViewModel { UserId = 1, Name = "カレンダー", TimeZoneId = value, DisplayColor = value };

        Validate(model).Should().BeEmpty();
        model.TimeZoneId.Should().BeNull();
        model.DisplayColor.Should().BeNull();
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#ABC")]
    [InlineData("#GGGGGG")]
    public void 表示色が6桁の16進数でなければ入力エラーになる(string color) {
        var model = new CreateUserCalendarViewModel { UserId = 1, Name = "カレンダー", DisplayColor = color };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.DisplayColor)));
    }

    [Fact]
    public void タイムゾーンが64文字を超えると入力エラーになる() {
        var model = new CreateUserCalendarViewModel { UserId = 1, Name = "カレンダー", TimeZoneId = new string('a', 65) };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.TimeZoneId)));
    }

    private static List<ValidationResult> Validate(CreateUserCalendarViewModel model) {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
