using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class RecurrenceWeekday
{
    public long RecurrenceRuleId { get; set; }

    public byte Weekday { get; set; }

    public virtual RecurrenceRule RecurrenceRule { get; set; } = null!;
}
