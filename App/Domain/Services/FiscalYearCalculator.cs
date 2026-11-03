namespace App.Domain.Services;

/// <summary>4月1日から翌年3月31日までの事業年度を計算します。</summary>
public static class FiscalYearCalculator {
    // 翌年の終了日までDateOnlyで表現できる年度に限定します。
    private const int MinYear = 1;
    private const int MaxYear = 9998;

    /// <summary>指定年度の開始日と終了日を返します。両端の日付を含みます。</summary>
    /// <param name="year">開始日の西暦年（1～9998）。例：2026年度は2026。</param>
    /// <exception cref="ArgumentOutOfRangeException">年度が対応範囲外です。</exception>
    public static (DateOnly StartsOn, DateOnly EndsOn) GetPeriod(int year) {
        if (year < MinYear || year > MaxYear) {
            throw new ArgumentOutOfRangeException(
                nameof(year), year, "年度は1から9998までを指定してください。");
        }

        return (new DateOnly(year, 4, 1), new DateOnly(year + 1, 3, 31));
    }

    /// <summary>指定日が所属する年度を開始日の西暦年で返します。</summary>
    /// <param name="date">対象カレンダーの現地日付（0001年4月1日～9999年3月31日）。</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// 所属年度の開始日または終了日をDateOnlyで表現できません。
    /// </exception>
    public static int GetYear(DateOnly date) {
        var year = date.Month >= 4 ? date.Year : date.Year - 1;
        if (year < MinYear || year > MaxYear){
            throw new ArgumentOutOfRangeException(
                nameof(date), date, "日付は0001年4月1日から9999年3月31日までを指定してください。");
        }

        return year;
    }
}
