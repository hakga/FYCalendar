using App.Data;
using App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Domain.Services;

/// <summary>チームカレンダーの登録と取得を行います。</summary>
public sealed class TeamCalendarService(AppDbContext context) {
    public async Task<TeamCalendar> CreateAsync(
        long teamId, string name, string? timeZoneId = null, string? displayColor = null,
        CancellationToken cancellationToken = default) {
        if (teamId <= 0) {
            throw new ArgumentOutOfRangeException(nameof(teamId), "チームを選択してください。");
        }
        var normalizedName = CalendarValidation.NormalizeName(name);
        var normalizedColor = CalendarValidation.NormalizeDisplayColor(displayColor);
        var team = await context.Teams.AsNoTracking().SingleOrDefaultAsync(team => team.Id == teamId, cancellationToken)
            ?? throw new ArgumentException("選択したチームが見つかりません。チームを選択し直してください。", nameof(teamId));
        var calendar = new TeamCalendar {
            TeamId = teamId,
            Name = normalizedName,
            TimeZoneId = CalendarValidation.NormalizeTimeZoneId(string.IsNullOrWhiteSpace(timeZoneId) ? team.TimeZoneId : timeZoneId),
            DisplayColor = normalizedColor,
            CalendarType = "schedule",
            IsActive = true
        };
        context.TeamCalendars.Add(calendar);
        await context.SaveChangesAsync(cancellationToken);
        return calendar;
    }

    public async Task<IReadOnlyList<TeamCalendar>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.TeamCalendars.AsNoTracking().Include(calendar => calendar.Team)
            .OrderByDescending(calendar => calendar.Id).ToListAsync(cancellationToken);
}
