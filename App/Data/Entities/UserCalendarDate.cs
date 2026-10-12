using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class UserCalendarDate
{
    public long Id { get; set; }

    public long UserCalendarId { get; set; }

    public DateOnly TargetDate { get; set; }

    public string DateType { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool? IsWorkingDay { get; set; }

    public int Priority { get; set; }

    public virtual UserCalendar UserCalendar { get; set; } = null!;
}
