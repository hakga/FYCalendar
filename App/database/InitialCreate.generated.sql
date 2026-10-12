IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [FiscalYears] (
    [Id] bigint NOT NULL IDENTITY,
    [FiscalYear] int NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [StartsOn] date NOT NULL,
    [EndsOn] date NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'open',
    CONSTRAINT [PK_FiscalYears] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RecurrenceRules] (
    [Id] bigint NOT NULL IDENTITY,
    [Frequency] nvarchar(20) NOT NULL,
    [IntervalValue] int NOT NULL DEFAULT 1,
    [StartsOn] date NOT NULL,
    [EndsOn] date NULL,
    [OccurrenceCount] int NULL,
    [DayOfMonth] tinyint NULL,
    [MonthOfYear] tinyint NULL,
    [WeekOfMonth] smallint NULL,
    [HolidayBehavior] nvarchar(40) NOT NULL DEFAULT N'execute',
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_RecurrenceRules] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SystemCalendars] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [CalendarType] nvarchar(30) NOT NULL,
    [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N'Asia/Tokyo',
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_SystemCalendars] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Teams] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N'Asia/Tokyo',
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Teams] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Email] nvarchar(255) NULL,
    [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N'Asia/Tokyo',
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RecurrenceWeekdays] (
    [RecurrenceRuleId] bigint NOT NULL,
    [Weekday] tinyint NOT NULL,
    CONSTRAINT [PK_RecurrenceWeekdays] PRIMARY KEY ([RecurrenceRuleId], [Weekday]),
    CONSTRAINT [FK_RecurrenceWeekdays_RecurrenceRules] FOREIGN KEY ([RecurrenceRuleId]) REFERENCES [RecurrenceRules] ([Id])
);
GO

CREATE TABLE [SystemCalendarDates] (
    [Id] bigint NOT NULL IDENTITY,
    [SystemCalendarId] bigint NOT NULL,
    [TargetDate] date NOT NULL,
    [DateType] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [IsWorkingDay] bit NULL,
    [Priority] int NOT NULL DEFAULT 50,
    CONSTRAINT [PK_SystemCalendarDates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SystemCalendarDates_SystemCalendars] FOREIGN KEY ([SystemCalendarId]) REFERENCES [SystemCalendars] ([Id])
);
GO

CREATE TABLE [SystemCalendarWeekdayRules] (
    [Id] bigint NOT NULL IDENTITY,
    [SystemCalendarId] bigint NOT NULL,
    [Weekday] tinyint NOT NULL,
    [DayType] nvarchar(30) NOT NULL,
    [ValidFrom] date NULL,
    [ValidTo] date NULL,
    CONSTRAINT [PK_SystemCalendarWeekdayRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SystemCalendarWeekdayRules_SystemCalendars] FOREIGN KEY ([SystemCalendarId]) REFERENCES [SystemCalendars] ([Id])
);
GO

CREATE TABLE [TeamCalendars] (
    [Id] bigint NOT NULL IDENTITY,
    [TeamId] bigint NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [CalendarType] nvarchar(30) NOT NULL DEFAULT N'schedule',
    [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N'Asia/Tokyo',
    [DisplayColor] nvarchar(20) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TeamCalendars] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TeamCalendars_Teams] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id])
);
GO

CREATE TABLE [RecurrenceExceptions] (
    [Id] bigint NOT NULL IDENTITY,
    [RecurrenceRuleId] bigint NOT NULL,
    [OriginalStartAt] datetimeoffset(0) NOT NULL,
    [ExceptionType] nvarchar(20) NOT NULL,
    [ReplacementStartAt] datetimeoffset(0) NULL,
    [ReplacementEndAt] datetimeoffset(0) NULL,
    [Reason] nvarchar(1000) NULL,
    [CreatedByUserId] bigint NULL,
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    CONSTRAINT [PK_RecurrenceExceptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecurrenceExceptions_RecurrenceRules] FOREIGN KEY ([RecurrenceRuleId]) REFERENCES [RecurrenceRules] ([Id]),
    CONSTRAINT [FK_RecurrenceExceptions_Users] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [TeamMembers] (
    [TeamId] bigint NOT NULL,
    [UserId] bigint NOT NULL,
    [Role] nvarchar(30) NOT NULL DEFAULT N'member',
    [JoinedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    CONSTRAINT [PK_TeamMembers] PRIMARY KEY ([TeamId], [UserId]),
    CONSTRAINT [FK_TeamMembers_Teams] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]),
    CONSTRAINT [FK_TeamMembers_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [UserCalendars] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [CalendarType] nvarchar(30) NOT NULL DEFAULT N'schedule',
    [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N'Asia/Tokyo',
    [DisplayColor] nvarchar(20) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_UserCalendars] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserCalendars_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [TeamCalendarDates] (
    [Id] bigint NOT NULL IDENTITY,
    [TeamCalendarId] bigint NOT NULL,
    [TargetDate] date NOT NULL,
    [DateType] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [IsWorkingDay] bit NULL,
    [Priority] int NOT NULL DEFAULT 50,
    CONSTRAINT [PK_TeamCalendarDates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TeamCalendarDates_TeamCalendars] FOREIGN KEY ([TeamCalendarId]) REFERENCES [TeamCalendars] ([Id])
);
GO

CREATE TABLE [TeamCalendarWeekdayRules] (
    [Id] bigint NOT NULL IDENTITY,
    [TeamCalendarId] bigint NOT NULL,
    [Weekday] tinyint NOT NULL,
    [DayType] nvarchar(30) NOT NULL,
    [ValidFrom] date NULL,
    [ValidTo] date NULL,
    CONSTRAINT [PK_TeamCalendarWeekdayRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TeamCalendarWeekdayRules_TeamCalendars] FOREIGN KEY ([TeamCalendarId]) REFERENCES [TeamCalendars] ([Id])
);
GO

CREATE TABLE [Events] (
    [Id] bigint NOT NULL IDENTITY,
    [UserCalendarId] bigint NULL,
    [TeamCalendarId] bigint NULL,
    [SystemCalendarId] bigint NULL,
    [Title] nvarchar(300) NOT NULL,
    [Description] nvarchar(max) NULL,
    [EventType] nvarchar(30) NOT NULL DEFAULT N'appointment',
    [Priority] int NOT NULL DEFAULT 50,
    [ConflictBehavior] nvarchar(30) NOT NULL DEFAULT N'warning',
    [Status] nvarchar(30) NOT NULL DEFAULT N'active',
    [CreatedByUserId] bigint NULL,
    [DataVersion] bigint NOT NULL DEFAULT CAST(1 AS bigint),
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Events_CreatedByUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Events_SystemCalendars] FOREIGN KEY ([SystemCalendarId]) REFERENCES [SystemCalendars] ([Id]),
    CONSTRAINT [FK_Events_TeamCalendars] FOREIGN KEY ([TeamCalendarId]) REFERENCES [TeamCalendars] ([Id]),
    CONSTRAINT [FK_Events_UserCalendars] FOREIGN KEY ([UserCalendarId]) REFERENCES [UserCalendars] ([Id])
);
GO

CREATE TABLE [UserCalendarDates] (
    [Id] bigint NOT NULL IDENTITY,
    [UserCalendarId] bigint NOT NULL,
    [TargetDate] date NOT NULL,
    [DateType] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [IsWorkingDay] bit NULL,
    [Priority] int NOT NULL DEFAULT 50,
    CONSTRAINT [PK_UserCalendarDates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserCalendarDates_UserCalendars] FOREIGN KEY ([UserCalendarId]) REFERENCES [UserCalendars] ([Id])
);
GO

CREATE TABLE [UserCalendarWeekdayRules] (
    [Id] bigint NOT NULL IDENTITY,
    [UserCalendarId] bigint NOT NULL,
    [Weekday] tinyint NOT NULL,
    [DayType] nvarchar(30) NOT NULL,
    [ValidFrom] date NULL,
    [ValidTo] date NULL,
    CONSTRAINT [PK_UserCalendarWeekdayRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserCalendarWeekdayRules_UserCalendars] FOREIGN KEY ([UserCalendarId]) REFERENCES [UserCalendars] ([Id])
);
GO

CREATE TABLE [EventInputLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [UserId] bigint NULL,
    [RawText] nvarchar(2000) NOT NULL,
    [ParsedDataJson] nvarchar(max) NULL,
    [ParserVersion] nvarchar(50) NULL,
    [ParseStatus] nvarchar(30) NOT NULL DEFAULT N'pending',
    [Confidence] decimal(5,4) NULL,
    [CreatedEventId] bigint NULL,
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    CONSTRAINT [PK_EventInputLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventInputLogs_Events] FOREIGN KEY ([CreatedEventId]) REFERENCES [Events] ([Id]),
    CONSTRAINT [FK_EventInputLogs_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [EventParticipants] (
    [Id] bigint NOT NULL IDENTITY,
    [EventId] bigint NOT NULL,
    [UserId] bigint NULL,
    [TeamId] bigint NULL,
    [ParticipationRole] nvarchar(30) NOT NULL DEFAULT N'participant',
    [ResponseStatus] nvarchar(30) NOT NULL DEFAULT N'pending',
    CONSTRAINT [PK_EventParticipants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventParticipants_Events] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]),
    CONSTRAINT [FK_EventParticipants_Teams] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]),
    CONSTRAINT [FK_EventParticipants_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [EventSchedules] (
    [Id] bigint NOT NULL IDENTITY,
    [EventId] bigint NOT NULL,
    [StartsAt] datetimeoffset(0) NOT NULL,
    [EndsAt] datetimeoffset(0) NOT NULL,
    [IsAllDay] bit NOT NULL,
    [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N'Asia/Tokyo',
    [RecurrenceRuleId] bigint NULL,
    [GenerationStatus] nvarchar(20) NOT NULL DEFAULT N'pending',
    [DefinitionVersion] bigint NOT NULL DEFAULT CAST(1 AS bigint),
    [GeneratedVersion] bigint NULL,
    [GeneratedFrom] datetimeoffset(0) NULL,
    [GeneratedUntil] datetimeoffset(0) NULL,
    [GeneratedAt] datetimeoffset(0) NULL,
    [LastGenerationError] nvarchar(2000) NULL,
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_EventSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventSchedules_Events] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]),
    CONSTRAINT [FK_EventSchedules_RecurrenceRules] FOREIGN KEY ([RecurrenceRuleId]) REFERENCES [RecurrenceRules] ([Id])
);
GO

CREATE TABLE [EventOccurrences] (
    [Id] bigint NOT NULL IDENTITY,
    [EventId] bigint NOT NULL,
    [EventScheduleId] bigint NOT NULL,
    [StartsAt] datetimeoffset(0) NOT NULL,
    [EndsAt] datetimeoffset(0) NOT NULL,
    [OccurrenceDate] date NOT NULL,
    [OccurrenceKey] nvarchar(200) NOT NULL,
    [OccurrenceStatus] nvarchar(30) NOT NULL DEFAULT N'generated',
    [GenerationSource] nvarchar(30) NOT NULL DEFAULT N'batch',
    [ReflectionStatus] nvarchar(30) NOT NULL DEFAULT N'pending',
    [ReflectedAt] datetimeoffset(0) NULL,
    [SourceVersion] bigint NULL,
    [ReflectedVersion] bigint NULL,
    [LastErrorCode] nvarchar(100) NULL,
    [LastErrorMessage] nvarchar(2000) NULL,
    [CreatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [UpdatedAt] datetimeoffset(0) NOT NULL DEFAULT ((sysdatetimeoffset())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_EventOccurrences] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventOccurrences_EventSchedules] FOREIGN KEY ([EventScheduleId]) REFERENCES [EventSchedules] ([Id]),
    CONSTRAINT [FK_EventOccurrences_Events] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id])
);
GO

CREATE INDEX [IX_EventInputLogs_CreatedEventId] ON [EventInputLogs] ([CreatedEventId]);
GO

CREATE INDEX [IX_EventInputLogs_UserId] ON [EventInputLogs] ([UserId]);
GO

CREATE INDEX [IX_EventOccurrences_Event] ON [EventOccurrences] ([EventId], [StartsAt]);
GO

CREATE INDEX [IX_EventOccurrences_Reflection] ON [EventOccurrences] ([ReflectionStatus], [ReflectedVersion]);
GO

CREATE INDEX [IX_EventOccurrences_TimeRange] ON [EventOccurrences] ([StartsAt], [EndsAt]);
GO

CREATE UNIQUE INDEX [UQ_EventOccurrences_Key] ON [EventOccurrences] ([EventScheduleId], [OccurrenceKey]);
GO

CREATE INDEX [IX_EventParticipants_EventId] ON [EventParticipants] ([EventId]);
GO

CREATE INDEX [IX_EventParticipants_TeamId] ON [EventParticipants] ([TeamId]);
GO

CREATE INDEX [IX_EventParticipants_UserId] ON [EventParticipants] ([UserId]);
GO

CREATE INDEX [IX_Events_CreatedByUserId] ON [Events] ([CreatedByUserId]);
GO

CREATE INDEX [IX_Events_Status] ON [Events] ([Status], [Priority]);
GO

CREATE INDEX [IX_Events_SystemCalendarId] ON [Events] ([SystemCalendarId]);
GO

CREATE INDEX [IX_Events_TeamCalendarId] ON [Events] ([TeamCalendarId]);
GO

CREATE INDEX [IX_Events_UserCalendarId] ON [Events] ([UserCalendarId]);
GO

CREATE INDEX [IX_EventSchedules_Generation] ON [EventSchedules] ([GenerationStatus], [GeneratedVersion], [GeneratedUntil]);
GO

CREATE INDEX [IX_EventSchedules_RecurrenceRuleId] ON [EventSchedules] ([RecurrenceRuleId]);
GO

CREATE UNIQUE INDEX [UQ_EventSchedules_Event] ON [EventSchedules] ([EventId]);
GO

CREATE UNIQUE INDEX [UQ_FiscalYears_FiscalYear] ON [FiscalYears] ([FiscalYear]);
GO

CREATE INDEX [IX_RecurrenceExceptions_CreatedByUserId] ON [RecurrenceExceptions] ([CreatedByUserId]);
GO

CREATE UNIQUE INDEX [UQ_RecurrenceExceptions_Original] ON [RecurrenceExceptions] ([RecurrenceRuleId], [OriginalStartAt]);
GO

CREATE UNIQUE INDEX [UQ_SystemCalendarDates] ON [SystemCalendarDates] ([SystemCalendarId], [TargetDate]);
GO

CREATE UNIQUE INDEX [UQ_SystemCalendarWeekdayRules] ON [SystemCalendarWeekdayRules] ([SystemCalendarId], [Weekday], [ValidFrom]) WHERE [ValidFrom] IS NOT NULL;
GO

CREATE UNIQUE INDEX [UQ_TeamCalendarDates] ON [TeamCalendarDates] ([TeamCalendarId], [TargetDate]);
GO

CREATE INDEX [IX_TeamCalendars_TeamId] ON [TeamCalendars] ([TeamId]);
GO

CREATE UNIQUE INDEX [UQ_TeamCalendarWeekdayRules] ON [TeamCalendarWeekdayRules] ([TeamCalendarId], [Weekday], [ValidFrom]) WHERE [ValidFrom] IS NOT NULL;
GO

CREATE INDEX [IX_TeamMembers_UserId] ON [TeamMembers] ([UserId]);
GO

CREATE UNIQUE INDEX [UQ_UserCalendarDates] ON [UserCalendarDates] ([UserCalendarId], [TargetDate]);
GO

CREATE INDEX [IX_UserCalendars_UserId] ON [UserCalendars] ([UserId]);
GO

CREATE UNIQUE INDEX [UQ_UserCalendarWeekdayRules] ON [UserCalendarWeekdayRules] ([UserCalendarId], [Weekday], [ValidFrom]) WHERE [ValidFrom] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261012071744_InitialCreate', N'8.0.11');
GO

COMMIT;
GO

