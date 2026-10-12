using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class RecurrenceException
{
    public long Id { get; set; }

    public long RecurrenceRuleId { get; set; }

    public DateTimeOffset OriginalStartAt { get; set; }

    public string ExceptionType { get; set; } = null!;

    public DateTimeOffset? ReplacementStartAt { get; set; }

    public DateTimeOffset? ReplacementEndAt { get; set; }

    public string? Reason { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual RecurrenceRule RecurrenceRule { get; set; } = null!;
}
