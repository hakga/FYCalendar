namespace App.Models.Calendars;

public sealed record EventListItemViewModel(
    long Id, string Title, string? Description, string UserName, string CalendarName,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, string TimeZoneId, int Priority);
