using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class EventParticipant
{
    public long Id { get; set; }

    public long EventId { get; set; }

    public long? UserId { get; set; }

    public long? TeamId { get; set; }

    public string ParticipationRole { get; set; } = null!;

    public string ResponseStatus { get; set; } = null!;

    public virtual Event Event { get; set; } = null!;

    public virtual Team? Team { get; set; }

    public virtual User? User { get; set; }
}
