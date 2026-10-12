using App.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.Data;

public partial class AppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FiscalYear>().ToTable("FiscalYears", "dbo", table =>
        {
            table.HasCheckConstraint("CK_FiscalYears_DateRange", "[EndsOn] > [StartsOn]");
            table.HasCheckConstraint("CK_FiscalYears_Status", "[Status] IN (N'preparing', N'open', N'closed')");
        });

        modelBuilder.Entity<UserCalendarWeekdayRule>().ToTable("UserCalendarWeekdayRules", "dbo", table =>
        {
            table.HasCheckConstraint("CK_UserCalendarWeekdayRules_Weekday", "[Weekday] BETWEEN 0 AND 6");
            table.HasCheckConstraint("CK_UserCalendarWeekdayRules_DayType", "[DayType] IN (N'holiday', N'business_day')");
            table.HasCheckConstraint("CK_UserCalendarWeekdayRules_DateRange", "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");
        });

        modelBuilder.Entity<TeamCalendarWeekdayRule>().ToTable("TeamCalendarWeekdayRules", "dbo", table =>
        {
            table.HasCheckConstraint("CK_TeamCalendarWeekdayRules_Weekday", "[Weekday] BETWEEN 0 AND 6");
            table.HasCheckConstraint("CK_TeamCalendarWeekdayRules_DayType", "[DayType] IN (N'holiday', N'business_day')");
            table.HasCheckConstraint("CK_TeamCalendarWeekdayRules_DateRange", "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");
        });

        modelBuilder.Entity<SystemCalendarWeekdayRule>().ToTable("SystemCalendarWeekdayRules", "dbo", table =>
        {
            table.HasCheckConstraint("CK_SystemCalendarWeekdayRules_Weekday", "[Weekday] BETWEEN 0 AND 6");
            table.HasCheckConstraint("CK_SystemCalendarWeekdayRules_DayType", "[DayType] IN (N'holiday', N'business_day')");
            table.HasCheckConstraint("CK_SystemCalendarWeekdayRules_DateRange", "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");
        });

        modelBuilder.Entity<RecurrenceRule>().ToTable("RecurrenceRules", "dbo", table =>
        {
            table.HasCheckConstraint("CK_RecurrenceRules_Frequency", "[Frequency] IN (N'daily', N'weekly', N'monthly', N'yearly')");
            table.HasCheckConstraint("CK_RecurrenceRules_IntervalValue", "[IntervalValue] > 0");
            table.HasCheckConstraint("CK_RecurrenceRules_DateRange", "[EndsOn] IS NULL OR [EndsOn] >= [StartsOn]");
            table.HasCheckConstraint("CK_RecurrenceRules_OccurrenceCount", "[OccurrenceCount] IS NULL OR [OccurrenceCount] > 0");
            table.HasCheckConstraint("CK_RecurrenceRules_DayOfMonth", "[DayOfMonth] IS NULL OR [DayOfMonth] BETWEEN 1 AND 31");
            table.HasCheckConstraint("CK_RecurrenceRules_MonthOfYear", "[MonthOfYear] IS NULL OR [MonthOfYear] BETWEEN 1 AND 12");
            table.HasCheckConstraint("CK_RecurrenceRules_WeekOfMonth", "[WeekOfMonth] IS NULL OR [WeekOfMonth] BETWEEN -1 AND 5");
            table.HasCheckConstraint("CK_RecurrenceRules_HolidayBehavior", "[HolidayBehavior] IN (N'execute', N'skip', N'move_previous_business_day', N'move_next_business_day')");
        });

        modelBuilder.Entity<RecurrenceWeekday>().ToTable("RecurrenceWeekdays", "dbo", table =>
        {
            table.HasCheckConstraint("CK_RecurrenceWeekdays_Weekday", "[Weekday] BETWEEN 0 AND 6");
        });

        modelBuilder.Entity<Event>().ToTable("Events", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Events_ExactlyOneOwner", "(CASE WHEN [UserCalendarId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [TeamCalendarId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [SystemCalendarId] IS NULL THEN 0 ELSE 1 END) = 1");
            table.HasCheckConstraint("CK_Events_Priority", "[Priority] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_Events_ConflictBehavior", "[ConflictBehavior] IN (N'override', N'coexist', N'warning', N'block')");
            table.HasCheckConstraint("CK_Events_Status", "[Status] IN (N'active', N'cancelled', N'draft')");
            table.HasCheckConstraint("CK_Events_DataVersion", "[DataVersion] > 0");
        });

        modelBuilder.Entity<EventSchedule>().ToTable("EventSchedules", "dbo", table =>
        {
            table.HasCheckConstraint("CK_EventSchedules_DateRange", "[EndsAt] > [StartsAt]");
            table.HasCheckConstraint("CK_EventSchedules_GenerationStatus", "[GenerationStatus] IN (N'pending', N'processing', N'reflected', N'failed')");
            table.HasCheckConstraint("CK_EventSchedules_GeneratedRange", "[GeneratedUntil] IS NULL OR [GeneratedFrom] IS NULL OR [GeneratedUntil] > [GeneratedFrom]");
            table.HasCheckConstraint("CK_EventSchedules_GeneratedVersion", "[GeneratedVersion] IS NULL OR [GeneratedVersion] > 0");
        });

        modelBuilder.Entity<RecurrenceException>().ToTable("RecurrenceExceptions", "dbo", table =>
        {
            table.HasCheckConstraint("CK_RecurrenceExceptions_Type", "[ExceptionType] IN (N'skip', N'move', N'change')");
            table.HasCheckConstraint("CK_RecurrenceExceptions_Replacement", "[ExceptionType] = N'skip' OR [ReplacementStartAt] IS NOT NULL");
            table.HasCheckConstraint("CK_RecurrenceExceptions_ReplacementRange", "[ReplacementEndAt] IS NULL OR [ReplacementStartAt] IS NULL OR [ReplacementEndAt] > [ReplacementStartAt]");
        });

        modelBuilder.Entity<EventOccurrence>().ToTable("EventOccurrences", "dbo", table =>
        {
            table.HasCheckConstraint("CK_EventOccurrences_DateRange", "[EndsAt] > [StartsAt]");
            table.HasCheckConstraint("CK_EventOccurrences_OccurrenceStatus", "[OccurrenceStatus] IN (N'generated', N'skipped', N'overridden', N'cancelled')");
            table.HasCheckConstraint("CK_EventOccurrences_GenerationSource", "[GenerationSource] IN (N'batch', N'immediate', N'manual')");
            table.HasCheckConstraint("CK_EventOccurrences_ReflectionStatus", "[ReflectionStatus] IN (N'pending', N'processing', N'reflected', N'failed')");
        });

        modelBuilder.Entity<EventParticipant>().ToTable("EventParticipants", "dbo", table =>
        {
            table.HasCheckConstraint("CK_EventParticipants_ExactlyOneTarget", "(CASE WHEN [UserId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [TeamId] IS NULL THEN 0 ELSE 1 END) = 1");
            table.HasCheckConstraint("CK_EventParticipants_Role", "[ParticipationRole] IN (N'owner', N'participant', N'observer')");
            table.HasCheckConstraint("CK_EventParticipants_ResponseStatus", "[ResponseStatus] IN (N'pending', N'accepted', N'tentative', N'declined')");
        });

        modelBuilder.Entity<EventInputLog>().ToTable("EventInputLogs", "dbo", table =>
        {
            table.HasCheckConstraint("CK_EventInputLogs_ParseStatus", "[ParseStatus] IN (N'pending', N'confirmed', N'rejected', N'failed')");
            table.HasCheckConstraint("CK_EventInputLogs_Confidence", "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 1)");
        });
    }
}
