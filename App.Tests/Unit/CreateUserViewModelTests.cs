using System.ComponentModel.DataAnnotations;
using App.Models.Maintain;
using FluentAssertions;

namespace App.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class CreateUserViewModelTests {
    [Fact]
    public void 氏名とメールは空白を除去して上限文字数まで入力できる() {
        var name = new string('名', 100);
        var email = new string('a', 243) + "@example.com";
        var model = new CreateUserViewModel { Name = $" {name} ", Email = $" {email} " };

        Validate(model).Should().BeEmpty();
        model.Name.Should().Be(name);
        model.Email.Should().Be(email);
        model.TimeZoneId.Should().Be("Asia/Tokyo");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void 氏名が未入力なら入力エラーになる(string? name) {
        var model = new CreateUserViewModel { Name = name! };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.Name)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void メールアドレスは未入力でも登録できる(string? email) {
        var model = new CreateUserViewModel { Name = "利用者", Email = email };

        Validate(model).Should().BeEmpty();
        model.Email.Should().BeNull();
    }

    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("timeZoneId")]
    public void 文字数の上限を超える項目は入力エラーになる(string field) {
        var model = new CreateUserViewModel { Name = "利用者" };
        var member = field switch {
            "name" => nameof(model.Name),
            "email" => nameof(model.Email),
            _ => nameof(model.TimeZoneId)
        };
        if (field == "name") model.Name = new string('名', 101);
        if (field == "email") model.Email = new string('a', 244) + "@example.com";
        if (field == "timeZoneId") model.TimeZoneId = new string('a', 65);

        Validate(model).Should().Contain(result => result.MemberNames.Contains(member));
    }

    [Fact]
    public void 不正なメールアドレスは入力エラーになる() {
        var model = new CreateUserViewModel { Name = "利用者", Email = "invalid" };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.Email)));
    }

    [Fact]
    public void タイムゾーンが未選択なら入力エラーになる() {
        var model = new CreateUserViewModel { Name = "利用者", TimeZoneId = "" };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.TimeZoneId)));
    }

    private static List<ValidationResult> Validate(CreateUserViewModel model) {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
