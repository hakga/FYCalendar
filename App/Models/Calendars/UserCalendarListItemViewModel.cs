namespace App.Models.Calendars;

public sealed class UserCalendarListItemViewModel {
    public long Id { get; init; }
    public long UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CalendarType { get; init; } = string.Empty;
    public string TimeZoneId { get; init; } = string.Empty;
    public string? DisplayColor { get; init; }
    public bool IsActive { get; init; }

    public string CalendarTypeLabel => CalendarType == "schedule" ? "予定" : CalendarType;
    public string StatusLabel => IsActive ? "有効" : "無効";
}
