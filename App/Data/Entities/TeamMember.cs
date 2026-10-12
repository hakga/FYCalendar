using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class TeamMember
{
    public long TeamId { get; set; }

    public long UserId { get; set; }

    public string Role { get; set; } = null!;

    public DateTimeOffset JoinedAt { get; set; }

    public virtual Team Team { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
