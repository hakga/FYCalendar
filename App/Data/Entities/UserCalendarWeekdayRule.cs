using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class UserCalendarWeekdayRule
{
    public long Id { get; set; }

    public long UserCalendarId { get; set; }

    public byte Weekday { get; set; }

    public string DayType { get; set; } = null!;

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public virtual UserCalendar UserCalendar { get; set; } = null!;
}
