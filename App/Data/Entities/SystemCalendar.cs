using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class SystemCalendar
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string CalendarType { get; set; } = null!;

    public string TimeZoneId { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<SystemCalendarDate> SystemCalendarDates { get; set; } = new List<SystemCalendarDate>();

    public virtual ICollection<SystemCalendarWeekdayRule> SystemCalendarWeekdayRules { get; set; } = new List<SystemCalendarWeekdayRule>();
}
