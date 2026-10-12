using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class UserCalendar
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Name { get; set; } = null!;

    public string CalendarType { get; set; } = null!;

    public string TimeZoneId { get; set; } = null!;

    public string? DisplayColor { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserCalendarDate> UserCalendarDates { get; set; } = new List<UserCalendarDate>();

    public virtual ICollection<UserCalendarWeekdayRule> UserCalendarWeekdayRules { get; set; } = new List<UserCalendarWeekdayRule>();
}
