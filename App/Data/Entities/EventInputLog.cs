using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class EventInputLog
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string RawText { get; set; } = null!;

    public string? ParsedDataJson { get; set; }

    public string? ParserVersion { get; set; }

    public string ParseStatus { get; set; } = null!;

    public decimal? Confidence { get; set; }

    public long? CreatedEventId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public virtual Event? CreatedEvent { get; set; }

    public virtual User? User { get; set; }
}
