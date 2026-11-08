using System.Text.RegularExpressions;

namespace App.Domain.Services;

internal static class CalendarValidation {
    public static string NormalizeName(string? name) {
        var value = name?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 200) {
            throw new ArgumentException("カレンダー名を入力し、200文字以内で指定してください。", nameof(name));
        }
        return value;
    }

    public static string NormalizeTimeZoneId(string? timeZoneId) {
        var value = timeZoneId?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 64) {
            throw new ArgumentException("有効なタイムゾーンを選択してください。", nameof(timeZoneId));
        }
        try {
            _ = TimeZoneInfo.FindSystemTimeZoneById(value);
        } catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) {
            throw new ArgumentException("有効なタイムゾーンを選択してください。", nameof(timeZoneId), exception);
        }
        return value;
    }

    public static string? NormalizeDisplayColor(string? displayColor) {
        var value = string.IsNullOrWhiteSpace(displayColor) ? null : displayColor.Trim();
        if (value is not null && !Regex.IsMatch(value, @"\A#[0-9A-Fa-f]{6}\z")) {
            throw new ArgumentException("表示色は#に続く6桁の16進数で入力してください。", nameof(displayColor));
        }
        return value?.ToUpperInvariant();
    }
}
