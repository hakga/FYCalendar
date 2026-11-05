namespace App.Domain.Models;

public sealed record OneTimeEventRequest(
    long UserCalendarId, string Title, DateTime StartsAtLocal,
    DateTime? EndsAtLocal = null, string? Description = null);

public sealed record EventConflict(
    long EventId, string Title, string CalendarName,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Priority);

public sealed record OneTimeEventRegistrationResult(long? EventId, IReadOnlyList<EventConflict> Conflicts) {
    public bool RequiresConfirmation => EventId is null && Conflicts.Count > 0;
}
