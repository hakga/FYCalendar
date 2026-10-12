using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.RenameTable(
                name: "UserCalendarWeekdayRules",
                newName: "UserCalendarWeekdayRules",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TeamCalendarWeekdayRules",
                newName: "TeamCalendarWeekdayRules",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "SystemCalendarWeekdayRules",
                newName: "SystemCalendarWeekdayRules",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "RecurrenceWeekdays",
                newName: "RecurrenceWeekdays",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "RecurrenceRules",
                newName: "RecurrenceRules",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "RecurrenceExceptions",
                newName: "RecurrenceExceptions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "FiscalYears",
                newName: "FiscalYears",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "EventSchedules",
                newName: "EventSchedules",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Events",
                newName: "Events",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "EventParticipants",
                newName: "EventParticipants",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "EventOccurrences",
                newName: "EventOccurrences",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "EventInputLogs",
                newName: "EventInputLogs",
                newSchema: "dbo");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserCalendarWeekdayRules_DateRange",
                schema: "dbo",
                table: "UserCalendarWeekdayRules",
                sql: "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserCalendarWeekdayRules_DayType",
                schema: "dbo",
                table: "UserCalendarWeekdayRules",
                sql: "[DayType] IN (N'holiday', N'business_day')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserCalendarWeekdayRules_Weekday",
                schema: "dbo",
                table: "UserCalendarWeekdayRules",
                sql: "[Weekday] BETWEEN 0 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeamCalendarWeekdayRules_DateRange",
                schema: "dbo",
                table: "TeamCalendarWeekdayRules",
                sql: "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeamCalendarWeekdayRules_DayType",
                schema: "dbo",
                table: "TeamCalendarWeekdayRules",
                sql: "[DayType] IN (N'holiday', N'business_day')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeamCalendarWeekdayRules_Weekday",
                schema: "dbo",
                table: "TeamCalendarWeekdayRules",
                sql: "[Weekday] BETWEEN 0 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SystemCalendarWeekdayRules_DateRange",
                schema: "dbo",
                table: "SystemCalendarWeekdayRules",
                sql: "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SystemCalendarWeekdayRules_DayType",
                schema: "dbo",
                table: "SystemCalendarWeekdayRules",
                sql: "[DayType] IN (N'holiday', N'business_day')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SystemCalendarWeekdayRules_Weekday",
                schema: "dbo",
                table: "SystemCalendarWeekdayRules",
                sql: "[Weekday] BETWEEN 0 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceWeekdays_Weekday",
                schema: "dbo",
                table: "RecurrenceWeekdays",
                sql: "[Weekday] BETWEEN 0 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceRules_DateRange",
                schema: "dbo",
                table: "RecurrenceRules",
                sql: "[EndsOn] IS NULL OR [EndsOn] >= [StartsOn]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceRules_DayOfMonth",
                schema: "dbo",
                table: "RecurrenceRules",
                sql: "[DayOfMonth] IS NULL OR [DayOfMonth] BETWEEN 1 AND 31");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceRules_Frequency",
                schema: "dbo",
                table: "RecurrenceRules",
                sql: "[Frequency] IN (N'daily', N'weekly', N'monthly', N'yearly')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceRules_HolidayBehavior",
                schema: "dbo",
                table: "RecurrenceRules",
                sql: "[HolidayBehavior] IN (N'execute', N'skip', N'move_previous_business_day', N'move_next_business_day')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceRules_IntervalValue",
                schema: "dbo",
                table: "RecurrenceRules",
                sql: "[IntervalValue] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceRules_MonthOfYear",
                schema: "dbo",
                table: "RecurrenceRules",
                sql: "[MonthOfYear] IS NULL OR [MonthOfYear] BETWEEN 1 AND 12");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceRules_OccurrenceCount",
                schema: "dbo",
                table: "RecurrenceRules",
                sql: "[OccurrenceCount] IS NULL OR [OccurrenceCount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceRules_WeekOfMonth",
                schema: "dbo",
                table: "RecurrenceRules",
                sql: "[WeekOfMonth] IS NULL OR [WeekOfMonth] BETWEEN -1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceExceptions_Replacement",
                schema: "dbo",
                table: "RecurrenceExceptions",
                sql: "[ExceptionType] = N'skip' OR [ReplacementStartAt] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceExceptions_ReplacementRange",
                schema: "dbo",
                table: "RecurrenceExceptions",
                sql: "[ReplacementEndAt] IS NULL OR [ReplacementStartAt] IS NULL OR [ReplacementEndAt] > [ReplacementStartAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecurrenceExceptions_Type",
                schema: "dbo",
                table: "RecurrenceExceptions",
                sql: "[ExceptionType] IN (N'skip', N'move', N'change')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FiscalYears_DateRange",
                schema: "dbo",
                table: "FiscalYears",
                sql: "[EndsOn] > [StartsOn]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FiscalYears_Status",
                schema: "dbo",
                table: "FiscalYears",
                sql: "[Status] IN (N'preparing', N'open', N'closed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSchedules_DateRange",
                schema: "dbo",
                table: "EventSchedules",
                sql: "[EndsAt] > [StartsAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSchedules_GeneratedRange",
                schema: "dbo",
                table: "EventSchedules",
                sql: "[GeneratedUntil] IS NULL OR [GeneratedFrom] IS NULL OR [GeneratedUntil] > [GeneratedFrom]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSchedules_GeneratedVersion",
                schema: "dbo",
                table: "EventSchedules",
                sql: "[GeneratedVersion] IS NULL OR [GeneratedVersion] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSchedules_GenerationStatus",
                schema: "dbo",
                table: "EventSchedules",
                sql: "[GenerationStatus] IN (N'pending', N'processing', N'reflected', N'failed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_ConflictBehavior",
                schema: "dbo",
                table: "Events",
                sql: "[ConflictBehavior] IN (N'override', N'coexist', N'warning', N'block')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_DataVersion",
                schema: "dbo",
                table: "Events",
                sql: "[DataVersion] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_ExactlyOneOwner",
                schema: "dbo",
                table: "Events",
                sql: "(CASE WHEN [UserCalendarId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [TeamCalendarId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [SystemCalendarId] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Priority",
                schema: "dbo",
                table: "Events",
                sql: "[Priority] BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Status",
                schema: "dbo",
                table: "Events",
                sql: "[Status] IN (N'active', N'cancelled', N'draft')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventParticipants_ExactlyOneTarget",
                schema: "dbo",
                table: "EventParticipants",
                sql: "(CASE WHEN [UserId] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [TeamId] IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventParticipants_ResponseStatus",
                schema: "dbo",
                table: "EventParticipants",
                sql: "[ResponseStatus] IN (N'pending', N'accepted', N'tentative', N'declined')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventParticipants_Role",
                schema: "dbo",
                table: "EventParticipants",
                sql: "[ParticipationRole] IN (N'owner', N'participant', N'observer')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventOccurrences_DateRange",
                schema: "dbo",
                table: "EventOccurrences",
                sql: "[EndsAt] > [StartsAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventOccurrences_GenerationSource",
                schema: "dbo",
                table: "EventOccurrences",
                sql: "[GenerationSource] IN (N'batch', N'immediate', N'manual')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventOccurrences_OccurrenceStatus",
                schema: "dbo",
                table: "EventOccurrences",
                sql: "[OccurrenceStatus] IN (N'generated', N'skipped', N'overridden', N'cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventOccurrences_ReflectionStatus",
                schema: "dbo",
                table: "EventOccurrences",
                sql: "[ReflectionStatus] IN (N'pending', N'processing', N'reflected', N'failed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventInputLogs_Confidence",
                schema: "dbo",
                table: "EventInputLogs",
                sql: "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventInputLogs_ParseStatus",
                schema: "dbo",
                table: "EventInputLogs",
                sql: "[ParseStatus] IN (N'pending', N'confirmed', N'rejected', N'failed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserCalendarWeekdayRules_DateRange",
                schema: "dbo",
                table: "UserCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserCalendarWeekdayRules_DayType",
                schema: "dbo",
                table: "UserCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserCalendarWeekdayRules_Weekday",
                schema: "dbo",
                table: "UserCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TeamCalendarWeekdayRules_DateRange",
                schema: "dbo",
                table: "TeamCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TeamCalendarWeekdayRules_DayType",
                schema: "dbo",
                table: "TeamCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TeamCalendarWeekdayRules_Weekday",
                schema: "dbo",
                table: "TeamCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SystemCalendarWeekdayRules_DateRange",
                schema: "dbo",
                table: "SystemCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SystemCalendarWeekdayRules_DayType",
                schema: "dbo",
                table: "SystemCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SystemCalendarWeekdayRules_Weekday",
                schema: "dbo",
                table: "SystemCalendarWeekdayRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceWeekdays_Weekday",
                schema: "dbo",
                table: "RecurrenceWeekdays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceRules_DateRange",
                schema: "dbo",
                table: "RecurrenceRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceRules_DayOfMonth",
                schema: "dbo",
                table: "RecurrenceRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceRules_Frequency",
                schema: "dbo",
                table: "RecurrenceRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceRules_HolidayBehavior",
                schema: "dbo",
                table: "RecurrenceRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceRules_IntervalValue",
                schema: "dbo",
                table: "RecurrenceRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceRules_MonthOfYear",
                schema: "dbo",
                table: "RecurrenceRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceRules_OccurrenceCount",
                schema: "dbo",
                table: "RecurrenceRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceRules_WeekOfMonth",
                schema: "dbo",
                table: "RecurrenceRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceExceptions_Replacement",
                schema: "dbo",
                table: "RecurrenceExceptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceExceptions_ReplacementRange",
                schema: "dbo",
                table: "RecurrenceExceptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecurrenceExceptions_Type",
                schema: "dbo",
                table: "RecurrenceExceptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FiscalYears_DateRange",
                schema: "dbo",
                table: "FiscalYears");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FiscalYears_Status",
                schema: "dbo",
                table: "FiscalYears");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSchedules_DateRange",
                schema: "dbo",
                table: "EventSchedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSchedules_GeneratedRange",
                schema: "dbo",
                table: "EventSchedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSchedules_GeneratedVersion",
                schema: "dbo",
                table: "EventSchedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSchedules_GenerationStatus",
                schema: "dbo",
                table: "EventSchedules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_ConflictBehavior",
                schema: "dbo",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_DataVersion",
                schema: "dbo",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_ExactlyOneOwner",
                schema: "dbo",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Priority",
                schema: "dbo",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Status",
                schema: "dbo",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventParticipants_ExactlyOneTarget",
                schema: "dbo",
                table: "EventParticipants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventParticipants_ResponseStatus",
                schema: "dbo",
                table: "EventParticipants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventParticipants_Role",
                schema: "dbo",
                table: "EventParticipants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventOccurrences_DateRange",
                schema: "dbo",
                table: "EventOccurrences");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventOccurrences_GenerationSource",
                schema: "dbo",
                table: "EventOccurrences");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventOccurrences_OccurrenceStatus",
                schema: "dbo",
                table: "EventOccurrences");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventOccurrences_ReflectionStatus",
                schema: "dbo",
                table: "EventOccurrences");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventInputLogs_Confidence",
                schema: "dbo",
                table: "EventInputLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventInputLogs_ParseStatus",
                schema: "dbo",
                table: "EventInputLogs");

            migrationBuilder.RenameTable(
                name: "UserCalendarWeekdayRules",
                schema: "dbo",
                newName: "UserCalendarWeekdayRules");

            migrationBuilder.RenameTable(
                name: "TeamCalendarWeekdayRules",
                schema: "dbo",
                newName: "TeamCalendarWeekdayRules");

            migrationBuilder.RenameTable(
                name: "SystemCalendarWeekdayRules",
                schema: "dbo",
                newName: "SystemCalendarWeekdayRules");

            migrationBuilder.RenameTable(
                name: "RecurrenceWeekdays",
                schema: "dbo",
                newName: "RecurrenceWeekdays");

            migrationBuilder.RenameTable(
                name: "RecurrenceRules",
                schema: "dbo",
                newName: "RecurrenceRules");

            migrationBuilder.RenameTable(
                name: "RecurrenceExceptions",
                schema: "dbo",
                newName: "RecurrenceExceptions");

            migrationBuilder.RenameTable(
                name: "FiscalYears",
                schema: "dbo",
                newName: "FiscalYears");

            migrationBuilder.RenameTable(
                name: "EventSchedules",
                schema: "dbo",
                newName: "EventSchedules");

            migrationBuilder.RenameTable(
                name: "Events",
                schema: "dbo",
                newName: "Events");

            migrationBuilder.RenameTable(
                name: "EventParticipants",
                schema: "dbo",
                newName: "EventParticipants");

            migrationBuilder.RenameTable(
                name: "EventOccurrences",
                schema: "dbo",
                newName: "EventOccurrences");

            migrationBuilder.RenameTable(
                name: "EventInputLogs",
                schema: "dbo",
                newName: "EventInputLogs");
        }
    }
}
