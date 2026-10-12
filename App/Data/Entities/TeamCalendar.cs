using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class TeamCalendar
{
    public long Id { get; set; }

    public long TeamId { get; set; }

    public string Name { get; set; } = null!;

    public string CalendarType { get; set; } = null!;

    public string TimeZoneId { get; set; } = null!;

    public string? DisplayColor { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual Team Team { get; set; } = null!;

    public virtual ICollection<TeamCalendarDate> TeamCalendarDates { get; set; } = new List<TeamCalendarDate>();

    public virtual ICollection<TeamCalendarWeekdayRule> TeamCalendarWeekdayRules { get; set; } = new List<TeamCalendarWeekdayRule>();
}
