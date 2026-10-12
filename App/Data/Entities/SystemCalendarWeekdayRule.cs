using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class SystemCalendarWeekdayRule
{
    public long Id { get; set; }

    public long SystemCalendarId { get; set; }

    public byte Weekday { get; set; }

    public string DayType { get; set; } = null!;

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public virtual SystemCalendar SystemCalendar { get; set; } = null!;
}
