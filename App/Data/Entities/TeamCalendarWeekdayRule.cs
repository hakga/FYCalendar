using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class TeamCalendarWeekdayRule
{
    public long Id { get; set; }

    public long TeamCalendarId { get; set; }

    public byte Weekday { get; set; }

    public string DayType { get; set; } = null!;

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public virtual TeamCalendar TeamCalendar { get; set; } = null!;
}
