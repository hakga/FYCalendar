using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class TeamCalendarDate
{
    public long Id { get; set; }

    public long TeamCalendarId { get; set; }

    public DateOnly TargetDate { get; set; }

    public string DateType { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool? IsWorkingDay { get; set; }

    public int Priority { get; set; }

    public virtual TeamCalendar TeamCalendar { get; set; } = null!;
}
