using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class EventSchedule
{
    public long Id { get; set; }

    public long EventId { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public bool IsAllDay { get; set; }

    public string TimeZoneId { get; set; } = null!;

    public long? RecurrenceRuleId { get; set; }

    public string GenerationStatus { get; set; } = null!;

    public long DefinitionVersion { get; set; }

    public long? GeneratedVersion { get; set; }

    public DateTimeOffset? GeneratedFrom { get; set; }

    public DateTimeOffset? GeneratedUntil { get; set; }

    public DateTimeOffset? GeneratedAt { get; set; }

    public string? LastGenerationError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<EventOccurrence> EventOccurrences { get; set; } = new List<EventOccurrence>();

    public virtual RecurrenceRule? RecurrenceRule { get; set; }
}
