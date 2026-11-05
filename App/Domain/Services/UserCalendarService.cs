using System.Text.RegularExpressions;
using App.Data;
using App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Domain.Services;

/// <summary>個人カレンダーの登録と取得を行います。</summary>
public sealed class UserCalendarService(AppDbContext context) {
    public async Task<UserCalendar> CreateAsync(
        long userId, string name, string? timeZoneId = null, string? displayColor = null,
        CancellationToken cancellationToken = default) {
        if (userId <= 0) {
            throw new ArgumentOutOfRangeException(nameof(userId), "利用者を選択してください。");
        }

        var normalizedName = name?.Trim();
        if (string.IsNullOrEmpty(normalizedName) || normalizedName.Length > 200) {
            throw new ArgumentException("カレンダー名を入力し、200文字以内で指定してください。", nameof(name));
        }

        var normalizedColor = string.IsNullOrWhiteSpace(displayColor) ? null : displayColor.Trim();
        if (normalizedColor is not null && !Regex.IsMatch(normalizedColor, @"\A#[0-9A-Fa-f]{6}\z")) {
            throw new ArgumentException("表示色は#に続く6桁の16進数で入力してください。", nameof(displayColor));
        }

        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ArgumentException("選択した利用者が見つかりません。利用者を選択し直してください。", nameof(userId));
        var effectiveTimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? user.TimeZoneId : timeZoneId.Trim();
        if (string.IsNullOrWhiteSpace(effectiveTimeZoneId) || effectiveTimeZoneId.Length > 64) {
            throw new ArgumentException("有効なタイムゾーンを選択してください。", nameof(timeZoneId));
        }
        try {
            _ = TimeZoneInfo.FindSystemTimeZoneById(effectiveTimeZoneId);
        } catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) {
            throw new ArgumentException("有効なタイムゾーンを選択してください。", nameof(timeZoneId), exception);
        }

        var calendar = new UserCalendar {
            UserId = userId,
            Name = normalizedName,
            CalendarType = "schedule",
            TimeZoneId = effectiveTimeZoneId,
            DisplayColor = normalizedColor?.ToUpperInvariant(),
            IsActive = true
        };
        context.UserCalendars.Add(calendar);
        await context.SaveChangesAsync(cancellationToken);
        return calendar;
    }

    /// <summary>登録の新しい順に、所有する利用者とともにカレンダーを取得します。</summary>
    public async Task<IReadOnlyList<UserCalendar>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.UserCalendars.AsNoTracking().Include(calendar => calendar.User)
            .OrderByDescending(calendar => calendar.Id).ToListAsync(cancellationToken);
}
