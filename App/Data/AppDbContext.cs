using System;
using System.Collections.Generic;
using App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventInputLog> EventInputLogs { get; set; }

    public virtual DbSet<EventOccurrence> EventOccurrences { get; set; }

    public virtual DbSet<EventParticipant> EventParticipants { get; set; }

    public virtual DbSet<EventSchedule> EventSchedules { get; set; }

    public virtual DbSet<FiscalYear> FiscalYears { get; set; }

    public virtual DbSet<RecurrenceException> RecurrenceExceptions { get; set; }

    public virtual DbSet<RecurrenceRule> RecurrenceRules { get; set; }

    public virtual DbSet<RecurrenceWeekday> RecurrenceWeekdays { get; set; }

    public virtual DbSet<SystemCalendar> SystemCalendars { get; set; }

    public virtual DbSet<SystemCalendarDate> SystemCalendarDates { get; set; }

    public virtual DbSet<SystemCalendarWeekdayRule> SystemCalendarWeekdayRules { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<TeamCalendar> TeamCalendars { get; set; }

    public virtual DbSet<TeamCalendarDate> TeamCalendarDates { get; set; }

    public virtual DbSet<TeamCalendarWeekdayRule> TeamCalendarWeekdayRules { get; set; }

    public virtual DbSet<TeamMember> TeamMembers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserCalendar> UserCalendars { get; set; }

    public virtual DbSet<UserCalendarDate> UserCalendarDates { get; set; }

    public virtual DbSet<UserCalendarWeekdayRule> UserCalendarWeekdayRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => new { e.Status, e.Priority }, "IX_Events_Status");

            entity.HasIndex(e => e.SystemCalendarId, "IX_Events_SystemCalendarId");

            entity.HasIndex(e => e.TeamCalendarId, "IX_Events_TeamCalendarId");

            entity.HasIndex(e => e.UserCalendarId, "IX_Events_UserCalendarId");

            entity.Property(e => e.ConflictBehavior)
                .HasMaxLength(30)
                .HasDefaultValue("warning");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.DataVersion).HasDefaultValue(1L);
            entity.Property(e => e.EventType)
                .HasMaxLength(30)
                .HasDefaultValue("appointment");
            entity.Property(e => e.Priority).HasDefaultValue(50);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("active");
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Events)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Events_CreatedByUser");

            entity.HasOne(d => d.SystemCalendar).WithMany(p => p.Events)
                .HasForeignKey(d => d.SystemCalendarId)
                .HasConstraintName("FK_Events_SystemCalendars");

            entity.HasOne(d => d.TeamCalendar).WithMany(p => p.Events)
                .HasForeignKey(d => d.TeamCalendarId)
                .HasConstraintName("FK_Events_TeamCalendars");

            entity.HasOne(d => d.UserCalendar).WithMany(p => p.Events)
                .HasForeignKey(d => d.UserCalendarId)
                .HasConstraintName("FK_Events_UserCalendars");
        });

        modelBuilder.Entity<EventInputLog>(entity =>
        {
            entity.Property(e => e.Confidence).HasColumnType("decimal(5, 4)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.ParseStatus)
                .HasMaxLength(30)
                .HasDefaultValue("pending");
            entity.Property(e => e.ParserVersion).HasMaxLength(50);
            entity.Property(e => e.RawText).HasMaxLength(2000);

            entity.HasOne(d => d.CreatedEvent).WithMany(p => p.EventInputLogs)
                .HasForeignKey(d => d.CreatedEventId)
                .HasConstraintName("FK_EventInputLogs_Events");

            entity.HasOne(d => d.User).WithMany(p => p.EventInputLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_EventInputLogs_Users");
        });

        modelBuilder.Entity<EventOccurrence>(entity =>
        {
            entity.HasIndex(e => new { e.EventId, e.StartsAt }, "IX_EventOccurrences_Event");

            entity.HasIndex(e => new { e.ReflectionStatus, e.ReflectedVersion }, "IX_EventOccurrences_Reflection");

            entity.HasIndex(e => new { e.StartsAt, e.EndsAt }, "IX_EventOccurrences_TimeRange");

            entity.HasIndex(e => new { e.EventScheduleId, e.OccurrenceKey }, "UQ_EventOccurrences_Key").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.EndsAt).HasPrecision(0);
            entity.Property(e => e.GenerationSource)
                .HasMaxLength(30)
                .HasDefaultValue("batch");
            entity.Property(e => e.LastErrorCode).HasMaxLength(100);
            entity.Property(e => e.LastErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.OccurrenceKey).HasMaxLength(200);
            entity.Property(e => e.OccurrenceStatus)
                .HasMaxLength(30)
                .HasDefaultValue("generated");
            entity.Property(e => e.ReflectedAt).HasPrecision(0);
            entity.Property(e => e.ReflectionStatus)
                .HasMaxLength(30)
                .HasDefaultValue("pending");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.StartsAt).HasPrecision(0);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");

            entity.HasOne(d => d.Event).WithMany(p => p.EventOccurrences)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventOccurrences_Events");

            entity.HasOne(d => d.EventSchedule).WithMany(p => p.EventOccurrences)
                .HasForeignKey(d => d.EventScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventOccurrences_EventSchedules");
        });

        modelBuilder.Entity<EventParticipant>(entity =>
        {
            entity.Property(e => e.ParticipationRole)
                .HasMaxLength(30)
                .HasDefaultValue("participant");
            entity.Property(e => e.ResponseStatus)
                .HasMaxLength(30)
                .HasDefaultValue("pending");

            entity.HasOne(d => d.Event).WithMany(p => p.EventParticipants)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventParticipants_Events");

            entity.HasOne(d => d.Team).WithMany(p => p.EventParticipants)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK_EventParticipants_Teams");

            entity.HasOne(d => d.User).WithMany(p => p.EventParticipants)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_EventParticipants_Users");
        });

        modelBuilder.Entity<EventSchedule>(entity =>
        {
            entity.HasIndex(e => new { e.GenerationStatus, e.GeneratedVersion, e.GeneratedUntil }, "IX_EventSchedules_Generation");

            entity.HasIndex(e => e.EventId, "UQ_EventSchedules_Event").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.DefinitionVersion).HasDefaultValue(1L);
            entity.Property(e => e.EndsAt).HasPrecision(0);
            entity.Property(e => e.GeneratedAt).HasPrecision(0);
            entity.Property(e => e.GeneratedFrom).HasPrecision(0);
            entity.Property(e => e.GeneratedUntil).HasPrecision(0);
            entity.Property(e => e.GenerationStatus)
                .HasMaxLength(20)
                .HasDefaultValue("pending");
            entity.Property(e => e.LastGenerationError).HasMaxLength(2000);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.StartsAt).HasPrecision(0);
            entity.Property(e => e.TimeZoneId)
                .HasMaxLength(64)
                .HasDefaultValue("Asia/Tokyo");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");

            entity.HasOne(d => d.Event).WithOne(p => p.EventSchedule)
                .HasForeignKey<EventSchedule>(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventSchedules_Events");

            entity.HasOne(d => d.RecurrenceRule).WithMany(p => p.EventSchedules)
                .HasForeignKey(d => d.RecurrenceRuleId)
                .HasConstraintName("FK_EventSchedules_RecurrenceRules");
        });

        modelBuilder.Entity<FiscalYear>(entity =>
        {
            entity.HasIndex(e => e.Year, "UQ_FiscalYears_FiscalYear").IsUnique();

            entity.Property(e => e.Year).HasColumnName("FiscalYear");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("open");
        });

        modelBuilder.Entity<RecurrenceException>(entity =>
        {
            entity.HasIndex(e => new { e.RecurrenceRuleId, e.OriginalStartAt }, "UQ_RecurrenceExceptions_Original").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.ExceptionType).HasMaxLength(20);
            entity.Property(e => e.OriginalStartAt).HasPrecision(0);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ReplacementEndAt).HasPrecision(0);
            entity.Property(e => e.ReplacementStartAt).HasPrecision(0);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.RecurrenceExceptions)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_RecurrenceExceptions_Users");

            entity.HasOne(d => d.RecurrenceRule).WithMany(p => p.RecurrenceExceptions)
                .HasForeignKey(d => d.RecurrenceRuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecurrenceExceptions_RecurrenceRules");
        });

        modelBuilder.Entity<RecurrenceRule>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.Frequency).HasMaxLength(20);
            entity.Property(e => e.HolidayBehavior)
                .HasMaxLength(40)
                .HasDefaultValue("execute");
            entity.Property(e => e.IntervalValue).HasDefaultValue(1);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
        });

        modelBuilder.Entity<RecurrenceWeekday>(entity =>
        {
            entity.HasKey(e => new { e.RecurrenceRuleId, e.Weekday });

            entity.HasOne(d => d.RecurrenceRule).WithMany(p => p.RecurrenceWeekdays)
                .HasForeignKey(d => d.RecurrenceRuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecurrenceWeekdays_RecurrenceRules");
        });

        modelBuilder.Entity<SystemCalendar>(entity =>
        {
            entity.Property(e => e.CalendarType).HasMaxLength(30);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TimeZoneId)
                .HasMaxLength(64)
                .HasDefaultValue("Asia/Tokyo");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
        });

        modelBuilder.Entity<SystemCalendarDate>(entity =>
        {
            entity.HasIndex(e => new { e.SystemCalendarId, e.TargetDate }, "UQ_SystemCalendarDates").IsUnique();

            entity.Property(e => e.DateType).HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Priority).HasDefaultValue(50);

            entity.HasOne(d => d.SystemCalendar).WithMany(p => p.SystemCalendarDates)
                .HasForeignKey(d => d.SystemCalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SystemCalendarDates_SystemCalendars");
        });

        modelBuilder.Entity<SystemCalendarWeekdayRule>(entity =>
        {
            entity.HasIndex(e => new { e.SystemCalendarId, e.Weekday, e.ValidFrom }, "UQ_SystemCalendarWeekdayRules").IsUnique();

            entity.Property(e => e.DayType).HasMaxLength(30);

            entity.HasOne(d => d.SystemCalendar).WithMany(p => p.SystemCalendarWeekdayRules)
                .HasForeignKey(d => d.SystemCalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SystemCalendarWeekdayRules_SystemCalendars");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TimeZoneId)
                .HasMaxLength(64)
                .HasDefaultValue("Asia/Tokyo");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
        });

        modelBuilder.Entity<TeamCalendar>(entity =>
        {
            entity.HasIndex(e => e.TeamId, "IX_TeamCalendars_TeamId");

            entity.Property(e => e.CalendarType)
                .HasMaxLength(30)
                .HasDefaultValue("schedule");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.DisplayColor).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TimeZoneId)
                .HasMaxLength(64)
                .HasDefaultValue("Asia/Tokyo");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamCalendars)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamCalendars_Teams");
        });

        modelBuilder.Entity<TeamCalendarDate>(entity =>
        {
            entity.HasIndex(e => new { e.TeamCalendarId, e.TargetDate }, "UQ_TeamCalendarDates").IsUnique();

            entity.Property(e => e.DateType).HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Priority).HasDefaultValue(50);

            entity.HasOne(d => d.TeamCalendar).WithMany(p => p.TeamCalendarDates)
                .HasForeignKey(d => d.TeamCalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamCalendarDates_TeamCalendars");
        });

        modelBuilder.Entity<TeamCalendarWeekdayRule>(entity =>
        {
            entity.HasIndex(e => new { e.TeamCalendarId, e.Weekday, e.ValidFrom }, "UQ_TeamCalendarWeekdayRules").IsUnique();

            entity.Property(e => e.DayType).HasMaxLength(30);

            entity.HasOne(d => d.TeamCalendar).WithMany(p => p.TeamCalendarWeekdayRules)
                .HasForeignKey(d => d.TeamCalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamCalendarWeekdayRules_TeamCalendars");
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.UserId });

            entity.HasIndex(e => e.UserId, "IX_TeamMembers_UserId");

            entity.Property(e => e.JoinedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .HasDefaultValue("member");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamMembers_Teams");

            entity.HasOne(d => d.User).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamMembers_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TimeZoneId)
                .HasMaxLength(64)
                .HasDefaultValue("Asia/Tokyo");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
        });

        modelBuilder.Entity<UserCalendar>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserCalendars_UserId");

            entity.Property(e => e.CalendarType)
                .HasMaxLength(30)
                .HasDefaultValue("schedule");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");
            entity.Property(e => e.DisplayColor).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TimeZoneId)
                .HasMaxLength(64)
                .HasDefaultValue("Asia/Tokyo");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())");

            entity.HasOne(d => d.User).WithMany(p => p.UserCalendars)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserCalendars_Users");
        });

        modelBuilder.Entity<UserCalendarDate>(entity =>
        {
            entity.HasIndex(e => new { e.UserCalendarId, e.TargetDate }, "UQ_UserCalendarDates").IsUnique();

            entity.Property(e => e.DateType).HasMaxLength(30);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Priority).HasDefaultValue(50);

            entity.HasOne(d => d.UserCalendar).WithMany(p => p.UserCalendarDates)
                .HasForeignKey(d => d.UserCalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserCalendarDates_UserCalendars");
        });

        modelBuilder.Entity<UserCalendarWeekdayRule>(entity =>
        {
            entity.HasIndex(e => new { e.UserCalendarId, e.Weekday, e.ValidFrom }, "UQ_UserCalendarWeekdayRules").IsUnique();

            entity.Property(e => e.DayType).HasMaxLength(30);

            entity.HasOne(d => d.UserCalendar).WithMany(p => p.UserCalendarWeekdayRules)
                .HasForeignKey(d => d.UserCalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserCalendarWeekdayRules_UserCalendars");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
