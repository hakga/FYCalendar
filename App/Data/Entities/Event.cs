using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class Event
{
    public long Id { get; set; }

    public long? UserCalendarId { get; set; }

    public long? TeamCalendarId { get; set; }

    public long? SystemCalendarId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string EventType { get; set; } = null!;

    public int Priority { get; set; }

    public string ConflictBehavior { get; set; } = null!;

    public string Status { get; set; } = null!;

    public long? CreatedByUserId { get; set; }

    public long DataVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<EventInputLog> EventInputLogs { get; set; } = new List<EventInputLog>();

    public virtual ICollection<EventOccurrence> EventOccurrences { get; set; } = new List<EventOccurrence>();

    public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();

    public virtual EventSchedule? EventSchedule { get; set; }

    public virtual SystemCalendar? SystemCalendar { get; set; }

    public virtual TeamCalendar? TeamCalendar { get; set; }

    public virtual UserCalendar? UserCalendar { get; set; }
}
