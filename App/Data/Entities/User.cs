using System;
using System.Collections.Generic;

namespace App.Data.Entities;

public partial class User
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Email { get; set; }

    public string TimeZoneId { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<EventInputLog> EventInputLogs { get; set; } = new List<EventInputLog>();

    public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<RecurrenceException> RecurrenceExceptions { get; set; } = new List<RecurrenceException>();

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

    public virtual ICollection<UserCalendar> UserCalendars { get; set; } = new List<UserCalendar>();
}
