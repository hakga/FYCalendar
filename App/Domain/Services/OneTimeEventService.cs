using System.Data;
using App.Data;
using App.Data.Entities;
using App.Domain.Exceptions;
using App.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Domain.Services;

public sealed class OneTimeEventService(AppDbContext context, TimeProvider timeProvider) {
    public async Task<OneTimeEventRegistrationResult> RegisterAsync(
        OneTimeEventRequest request, bool confirmOverlap = false, CancellationToken cancellationToken = default) {
        var title = request.Title?.Trim();
        if (string.IsNullOrEmpty(title) || title.Length > 300) {
            throw new EventValidationException(nameof(request.Title), "件名を入力し、300文字以内で指定してください。");
        }

        // 重複チェックと登録を同じトランザクションで扱う。
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var calendar = await context.UserCalendars.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.UserCalendarId, cancellationToken);
        if (calendar is null || !calendar.IsActive || calendar.CalendarType != "schedule") {
            throw new EventValidationException(nameof(request.UserCalendarId), "有効な予定用の個人カレンダーを選択してください。");
        }

        TimeZoneInfo zone;
        try {
            zone = TimeZoneInfo.FindSystemTimeZoneById(calendar.TimeZoneId);
        } catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException) {
            throw new EventValidationException(nameof(request.UserCalendarId), "カレンダーのタイムゾーンを確認してください。");
        }

        var startsAt = ResolveLocalTime(request.StartsAtLocal, zone, nameof(request.StartsAtLocal));
        DateTimeOffset endsAt;
        if (request.EndsAtLocal is { } localEnd) {
            endsAt = ResolveLocalTime(localEnd, zone, nameof(request.EndsAtLocal));
        } else {
            try {
                endsAt = startsAt.AddMinutes(1);
            } catch (ArgumentOutOfRangeException) {
                throw new EventValidationException(nameof(request.EndsAtLocal), "終了日時を表現できる開始日時を指定してください。");
            }
        }
        if (endsAt <= startsAt) {
            throw new EventValidationException(nameof(request.EndsAtLocal), "終了日時は開始日時より後にしてください。");
        }

        var conflicts = await FindConflictsAsync(calendar.UserId, startsAt, endsAt, zone, cancellationToken);
        if (conflicts.Count > 0 && !confirmOverlap) {
            return new OneTimeEventRegistrationResult(null, conflicts);
        }

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var generatedFrom = new DateTimeOffset(now.Ticks - now.Ticks % TimeSpan.TicksPerMinute, TimeSpan.Zero);
        var generatedUntil = generatedFrom.AddYears(1);
        var entry = new Event {
            UserCalendarId = calendar.Id,
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            EventType = "appointment",
            Priority = 50,
            ConflictBehavior = "warning",
            Status = "active",
            DataVersion = 1
        };
        var schedule = new EventSchedule {
            Event = entry,
            StartsAt = startsAt,
            EndsAt = endsAt,
            TimeZoneId = calendar.TimeZoneId,
            IsAllDay = false,
            DefinitionVersion = 1,
            GenerationStatus = "reflected",
            GeneratedVersion = 1,
            GeneratedFrom = generatedFrom,
            GeneratedUntil = generatedUntil,
            GeneratedAt = now
        };
        entry.EventSchedule = schedule;

        // 生成範囲は半開区間。範囲と重なる予定は、元の開始・終了を保って1件生成する。
        if (startsAt < generatedUntil && endsAt > generatedFrom) {
            schedule.EventOccurrences.Add(new EventOccurrence {
                Event = entry,
                EventSchedule = schedule,
                StartsAt = startsAt,
                EndsAt = endsAt,
                OccurrenceDate = DateOnly.FromDateTime(request.StartsAtLocal),
                OccurrenceKey = "single",
                OccurrenceStatus = "generated",
                GenerationSource = "immediate",
                ReflectionStatus = "reflected",
                ReflectedAt = now,
                SourceVersion = 1,
                ReflectedVersion = 1
            });
        }

        context.Events.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new OneTimeEventRegistrationResult(entry.Id, conflicts);
    }

    public async Task<IReadOnlyList<EventSchedule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.EventSchedules.AsNoTracking()
            .Where(schedule => schedule.RecurrenceRuleId == null && schedule.Event.UserCalendarId != null)
            .Include(schedule => schedule.Event).ThenInclude(entry => entry.UserCalendar!).ThenInclude(calendar => calendar.User)
            .OrderByDescending(schedule => schedule.EventId).ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<EventConflict>> FindConflictsAsync(
        long userId, DateTimeOffset startsAt, DateTimeOffset endsAt, TimeZoneInfo zone, CancellationToken cancellationToken) {
        // 一回限りの予定は定義から調べ、1年の生成範囲外でも重複を検出する。
        var singles = await context.EventSchedules.AsNoTracking().Where(schedule =>
                schedule.RecurrenceRuleId == null && schedule.Event.Status == "active" &&
                schedule.Event.UserCalendar != null && schedule.Event.UserCalendar.UserId == userId &&
                schedule.Event.UserCalendar.IsActive && schedule.StartsAt < endsAt && schedule.EndsAt > startsAt)
            .Select(schedule => new EventConflict(schedule.EventId, schedule.Event.Title, schedule.Event.UserCalendar!.Name,
                schedule.StartsAt, schedule.EndsAt, schedule.Event.Priority)).ToListAsync(cancellationToken);
        var recurring = await context.EventOccurrences.AsNoTracking().Where(occurrence =>
                occurrence.EventSchedule.RecurrenceRuleId != null && occurrence.Event.Status == "active" &&
                occurrence.OccurrenceStatus == "generated" && occurrence.ReflectionStatus == "reflected" &&
                occurrence.Event.UserCalendar != null && occurrence.Event.UserCalendar.UserId == userId &&
                occurrence.Event.UserCalendar.IsActive && occurrence.StartsAt < endsAt && occurrence.EndsAt > startsAt)
            .Select(occurrence => new EventConflict(occurrence.EventId, occurrence.Event.Title, occurrence.Event.UserCalendar!.Name,
                occurrence.StartsAt, occurrence.EndsAt, occurrence.Event.Priority)).ToListAsync(cancellationToken);

        return singles.Concat(recurring).OrderBy(item => item.StartsAt).ThenBy(item => item.EventId)
            .Select(item => item with {
                StartsAt = TimeZoneInfo.ConvertTime(item.StartsAt, zone),
                EndsAt = TimeZoneInfo.ConvertTime(item.EndsAt, zone)
            }).ToList();
    }

    private static DateTimeOffset ResolveLocalTime(DateTime local, TimeZoneInfo zone, string field) {
        if (local.Ticks % TimeSpan.TicksPerMinute != 0) {
            throw new EventValidationException(field, "日時は秒を含めず、分単位で入力してください。");
        }
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local)) {
            throw new EventValidationException(field, "夏時間への切り替えで存在しない時刻です。別の時刻を指定してください。");
        }
        if (zone.IsAmbiguousTime(local)) {
            throw new EventValidationException(field, "夏時間の切り替えで二通りに解釈される時刻です。別の時刻を指定してください。");
        }
        try {
            var utc = TimeZoneInfo.ConvertTimeToUtc(local, zone);
            if (TimeZoneInfo.ConvertTimeFromUtc(utc, zone) != local) {
                throw new EventValidationException(field, "このタイムゾーンで扱える日時を指定してください。");
            }
            return new DateTimeOffset(utc, TimeSpan.Zero);
        } catch (ArgumentException) {
            throw new EventValidationException(field, "このタイムゾーンで扱える日時を指定してください。");
        }
    }
}
