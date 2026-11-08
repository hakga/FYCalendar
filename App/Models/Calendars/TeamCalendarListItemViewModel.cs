namespace App.Models.Calendars;

public sealed record TeamCalendarListItemViewModel(
    long Id, long TeamId, string TeamName, string Name, string CalendarType,
    string TimeZoneId, string? DisplayColor, bool IsActive) {
    public string CalendarTypeLabel => CalendarType == "schedule" ? "予定" : CalendarType;
    public string StatusLabel => IsActive ? "有効" : "無効";
}
