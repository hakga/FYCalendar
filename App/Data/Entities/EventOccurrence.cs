using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class EventOccurrence
{
    public long Id { get; set; }

    public long EventId { get; set; }

    public long EventScheduleId { get; set; }

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public DateOnly OccurrenceDate { get; set; }

    public string OccurrenceKey { get; set; } = null!;

    public string OccurrenceStatus { get; set; } = null!;

    public string GenerationSource { get; set; } = null!;

    public string ReflectionStatus { get; set; } = null!;

    public DateTimeOffset? ReflectedAt { get; set; }

    public long? SourceVersion { get; set; }

    public long? ReflectedVersion { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;

    public virtual EventSchedule EventSchedule { get; set; } = null!;
}
