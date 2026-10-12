using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class Team
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string TimeZoneId { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();

    public virtual ICollection<TeamCalendar> TeamCalendars { get; set; } = new List<TeamCalendar>();

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}
