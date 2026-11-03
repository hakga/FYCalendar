using System.ComponentModel.DataAnnotations;
using App.Models.Maintain;
using FluentAssertions;

namespace App.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class CreateFiscalYearViewModelTests {
    [Fact]
    public void 名称は空白除去後に50文字まで入力できる() {
        var name = new string('年', 50);
        var model = new CreateFiscalYearViewModel { Year = 2026, Name = $"  {name}  " };

        Validate(model).Should().BeEmpty();
        model.Name.Should().Be(name);
    }

    [Fact]
    public void 年度が未入力なら入力エラーになる() {
        var model = new CreateFiscalYearViewModel { Name = "名称" };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.Year)));
        model.Period.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9999)]
    public void 範囲外の年度は入力エラーになる(int year) {
        var model = new CreateFiscalYearViewModel { Year = year, Name = "名称" };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.Year)));
        model.Period.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void 名称が未入力なら入力エラーになる(string? name) {
        var model = new CreateFiscalYearViewModel { Year = 2026, Name = name! };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.Name)));
    }

    [Fact]
    public void 名称が50文字を超えると入力エラーになる() {
        var model = new CreateFiscalYearViewModel { Year = 2026, Name = new string('年', 51) };

        Validate(model).Should().ContainSingle(result => result.MemberNames.Contains(nameof(model.Name)));
    }

    private static List<ValidationResult> Validate(CreateFiscalYearViewModel model) {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
