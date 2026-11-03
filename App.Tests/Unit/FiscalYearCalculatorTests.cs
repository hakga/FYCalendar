using App.Domain.Services;
using FluentAssertions;

namespace App.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class FiscalYearCalculatorTests {
    [Theory]
    [InlineData(2026, 2027)]
    [InlineData(2023, 2024)] // 終了年がうるう年でも3月31日で終了する。
    [InlineData(2024, 2025)]
    [InlineData(1, 2)]
    [InlineData(9998, 9999)]
    public void 年度の期間は4月1日から翌年3月31日になる(int year, int endYear) {
        var period = FiscalYearCalculator.GetPeriod(year);

        period.StartsOn.Should().Be(new DateOnly(year, 4, 1));
        period.EndsOn.Should().Be(new DateOnly(endYear, 3, 31));
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(9999)]
    [InlineData(int.MaxValue)]
    public void 範囲外の年度は期間を計算できない(int year) {
        Action calculate = () => FiscalYearCalculator.GetPeriod(year);

        calculate.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("year");
    }

    [Theory]
    [InlineData(2026, 3, 31, 2025)]
    [InlineData(2026, 4, 1, 2026)]
    [InlineData(2026, 12, 31, 2026)]
    [InlineData(2027, 1, 1, 2026)]
    [InlineData(2027, 3, 31, 2026)]
    [InlineData(2027, 4, 1, 2027)]
    [InlineData(2024, 2, 29, 2023)]
    [InlineData(2024, 4, 1, 2024)]
    [InlineData(1, 4, 1, 1)]
    [InlineData(9999, 3, 31, 9998)]
    public void 日付が所属する年度を取得できる(
        int year, int month, int day, int expectedYear) {
        var date = new DateOnly(year, month, day);

        FiscalYearCalculator.GetYear(date).Should().Be(expectedYear);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 3, 31)]
    [InlineData(9999, 4, 1)]
    [InlineData(9999, 12, 31)]
    public void 年度全体を表現できない日付は受け付けない(
        int year, int month, int day) {
        var date = new DateOnly(year, month, day);
        Action calculate = () => FiscalYearCalculator.GetYear(date);

        calculate.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("date");
    }
}
