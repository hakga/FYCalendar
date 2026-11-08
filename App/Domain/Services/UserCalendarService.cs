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

        var normalizedName = CalendarValidation.NormalizeName(name);
        var normalizedColor = CalendarValidation.NormalizeDisplayColor(displayColor);

        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ArgumentException("選択した利用者が見つかりません。利用者を選択し直してください。", nameof(userId));
        var effectiveTimeZoneId = CalendarValidation.NormalizeTimeZoneId(
            string.IsNullOrWhiteSpace(timeZoneId) ? user.TimeZoneId : timeZoneId);

        var calendar = new UserCalendar {
            UserId = userId,
            Name = normalizedName,
            CalendarType = "schedule",
            TimeZoneId = effectiveTimeZoneId,
            DisplayColor = normalizedColor,
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
