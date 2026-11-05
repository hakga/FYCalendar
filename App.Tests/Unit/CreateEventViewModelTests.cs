using System.ComponentModel.DataAnnotations;
using App.Models.Maintain;
using FluentAssertions;

namespace App.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class CreateEventViewModelTests {
    [Fact]
    public void 件名は空白を除去して300文字まで入力でき終了は省略できる() {
        var title = new string('予', 300);
        var model = ValidModel();
        model.Title = $" {title} ";

        Validate(model).Should().BeEmpty();
        model.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("UserCalendarId")]
    [InlineData("Title")]
    [InlineData("StartsAtLocal")]
    public void 必須項目が未入力なら入力エラーになる(string field) {
        var model = ValidModel();
        if (field == nameof(model.UserCalendarId)) model.UserCalendarId = null;
        if (field == nameof(model.Title)) model.Title = "   ";
        if (field == nameof(model.StartsAtLocal)) model.StartsAtLocal = null;

        Validate(model).Should().Contain(result => result.MemberNames.Contains(field));
    }

    [Fact]
    public void 件名が300文字を超えると入力エラーになる() {
        var model = ValidModel();
        model.Title = new string('予', 301);

        Validate(model).Should().Contain(result => result.MemberNames.Contains(nameof(model.Title)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void 終了日時が開始以前なら入力エラーになる(int minutes) {
        var model = ValidModel();
        model.EndsAtLocal = model.StartsAtLocal!.Value.AddMinutes(minutes);

        Validate(model).Should().Contain(result => result.MemberNames.Contains(nameof(model.EndsAtLocal)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void 秒を含む日時は入力エラーになる(bool startsAt) {
        var model = ValidModel();
        var field = startsAt ? nameof(model.StartsAtLocal) : nameof(model.EndsAtLocal);
        if (startsAt) model.StartsAtLocal = model.StartsAtLocal!.Value.AddSeconds(1);
        else model.EndsAtLocal = model.StartsAtLocal!.Value.AddMinutes(1).AddTicks(1);

        Validate(model).Should().Contain(result => result.MemberNames.Contains(field));
    }

    private static CreateEventViewModel ValidModel() => new() {
        UserCalendarId = 1, Title = "面談", StartsAtLocal = new DateTime(2026, 11, 4, 15, 0, 0)
    };

    private static List<ValidationResult> Validate(CreateEventViewModel model) {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
