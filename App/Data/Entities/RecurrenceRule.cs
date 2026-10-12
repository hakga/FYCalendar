using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class RecurrenceRule
{
    public long Id { get; set; }

    public string Frequency { get; set; } = null!;

    public int IntervalValue { get; set; }

    public DateOnly StartsOn { get; set; }

    public DateOnly? EndsOn { get; set; }

    public int? OccurrenceCount { get; set; }

    public byte? DayOfMonth { get; set; }

    public byte? MonthOfYear { get; set; }

    public short? WeekOfMonth { get; set; }

    public string HolidayBehavior { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<EventSchedule> EventSchedules { get; set; } = new List<EventSchedule>();

    public virtual ICollection<RecurrenceException> RecurrenceExceptions { get; set; } = new List<RecurrenceException>();

    public virtual ICollection<RecurrenceWeekday> RecurrenceWeekdays { get; set; } = new List<RecurrenceWeekday>();
}
