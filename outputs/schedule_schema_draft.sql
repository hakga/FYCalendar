/*
    スケジュール管理アプリケーション SQL Server 向けスキーマ草案

    想定:
    - .NET 8 / EF Core 8
    - SQL Server
    - 時刻は DATETIMEOFFSET で保持する
    - 一点の予定は1分間の区間として扱う
    - 曜日は C# enum と対応する tinyint で保持する（日曜日 = 0）
    - event_occurrences は直近1年分を保持する
    - 通常は深夜バッチで生成し、新規登録時は即時生成する
    - FiscalYears.EndsOn は終了日を含まない（例: 2026年度は
      2026-04-01 以上、2027-04-01 未満）

    このファイルは決定版ではなく、EF Core のモデル設計に取り込むための草案である。
*/

/* ================================================================
   1. 利用者・チーム
   ================================================================ */

CREATE TABLE dbo.Users
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    Name            NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(255) NULL,
    TimeZoneId      NVARCHAR(64) NOT NULL
        CONSTRAINT DF_Users_TimeZoneId DEFAULT N'Asia/Tokyo',
    CreatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion      ROWVERSION NOT NULL,

    CONSTRAINT PK_Users PRIMARY KEY (Id)
);
GO

CREATE TABLE dbo.Teams
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    Name            NVARCHAR(200) NOT NULL,
    TimeZoneId      NVARCHAR(64) NOT NULL
        CONSTRAINT DF_Teams_TimeZoneId DEFAULT N'Asia/Tokyo',
    CreatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_Teams_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_Teams_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion      ROWVERSION NOT NULL,

    CONSTRAINT PK_Teams PRIMARY KEY (Id)
);
GO

CREATE TABLE dbo.TeamMembers
(
    TeamId          BIGINT NOT NULL,
    UserId          BIGINT NOT NULL,
    Role            NVARCHAR(30) NOT NULL
        CONSTRAINT DF_TeamMembers_Role DEFAULT N'member',
    JoinedAt        DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_TeamMembers_JoinedAt DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT PK_TeamMembers PRIMARY KEY (TeamId, UserId),
    CONSTRAINT FK_TeamMembers_Teams FOREIGN KEY (TeamId)
        REFERENCES dbo.Teams (Id),
    CONSTRAINT FK_TeamMembers_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id)
);
GO

/* ================================================================
   2. 事業年度
   ================================================================ */

CREATE TABLE dbo.FiscalYears
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    FiscalYear      INT NOT NULL,
    Name            NVARCHAR(50) NOT NULL,
    StartsOn        DATE NOT NULL,
    EndsOn          DATE NOT NULL,
    Status          NVARCHAR(20) NOT NULL
        CONSTRAINT DF_FiscalYears_Status DEFAULT N'open',

    CONSTRAINT PK_FiscalYears PRIMARY KEY (Id),
    CONSTRAINT UQ_FiscalYears_FiscalYear UNIQUE (FiscalYear),
    CONSTRAINT CK_FiscalYears_DateRange CHECK (EndsOn > StartsOn),
    CONSTRAINT CK_FiscalYears_Status CHECK
        (Status IN (N'preparing', N'open', N'closed'))
);
GO

/* ================================================================
   3. 所有者別カレンダー
   ================================================================ */

CREATE TABLE dbo.UserCalendars
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    UserId          BIGINT NOT NULL,
    Name            NVARCHAR(200) NOT NULL,
    CalendarType    NVARCHAR(30) NOT NULL
        CONSTRAINT DF_UserCalendars_CalendarType DEFAULT N'schedule',
    TimeZoneId      NVARCHAR(64) NOT NULL
        CONSTRAINT DF_UserCalendars_TimeZoneId DEFAULT N'Asia/Tokyo',
    DisplayColor    NVARCHAR(20) NULL,
    IsActive        BIT NOT NULL
        CONSTRAINT DF_UserCalendars_IsActive DEFAULT 1,
    CreatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_UserCalendars_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_UserCalendars_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion      ROWVERSION NOT NULL,

    CONSTRAINT PK_UserCalendars PRIMARY KEY (Id),
    CONSTRAINT FK_UserCalendars_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id)
);
GO

CREATE TABLE dbo.TeamCalendars
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    TeamId          BIGINT NOT NULL,
    Name            NVARCHAR(200) NOT NULL,
    CalendarType    NVARCHAR(30) NOT NULL
        CONSTRAINT DF_TeamCalendars_CalendarType DEFAULT N'schedule',
    TimeZoneId      NVARCHAR(64) NOT NULL
        CONSTRAINT DF_TeamCalendars_TimeZoneId DEFAULT N'Asia/Tokyo',
    DisplayColor    NVARCHAR(20) NULL,
    IsActive        BIT NOT NULL
        CONSTRAINT DF_TeamCalendars_IsActive DEFAULT 1,
    CreatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_TeamCalendars_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_TeamCalendars_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion      ROWVERSION NOT NULL,

    CONSTRAINT PK_TeamCalendars PRIMARY KEY (Id),
    CONSTRAINT FK_TeamCalendars_Teams FOREIGN KEY (TeamId)
        REFERENCES dbo.Teams (Id)
);
GO

CREATE TABLE dbo.SystemCalendars
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    Name            NVARCHAR(200) NOT NULL,
    CalendarType    NVARCHAR(30) NOT NULL,
    TimeZoneId      NVARCHAR(64) NOT NULL
        CONSTRAINT DF_SystemCalendars_TimeZoneId DEFAULT N'Asia/Tokyo',
    IsActive        BIT NOT NULL
        CONSTRAINT DF_SystemCalendars_IsActive DEFAULT 1,
    CreatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_SystemCalendars_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt       DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_SystemCalendars_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion      ROWVERSION NOT NULL,

    CONSTRAINT PK_SystemCalendars PRIMARY KEY (Id)
);
GO

/* ================================================================
   4. 曜日・休日・営業日

   Weekday は C# enum と対応する。
   Sunday = 0, Monday = 1, ..., Saturday = 6
   ================================================================ */

CREATE TABLE dbo.UserCalendarWeekdayRules
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    UserCalendarId  BIGINT NOT NULL,
    Weekday         TINYINT NOT NULL,
    DayType         NVARCHAR(30) NOT NULL,
    ValidFrom       DATE NULL,
    ValidTo         DATE NULL,

    CONSTRAINT PK_UserCalendarWeekdayRules PRIMARY KEY (Id),
    CONSTRAINT FK_UserCalendarWeekdayRules_UserCalendars
        FOREIGN KEY (UserCalendarId) REFERENCES dbo.UserCalendars (Id),
    CONSTRAINT CK_UserCalendarWeekdayRules_Weekday
        CHECK (Weekday BETWEEN 0 AND 6),
    CONSTRAINT CK_UserCalendarWeekdayRules_DayType
        CHECK (DayType IN (N'holiday', N'business_day')),
    CONSTRAINT CK_UserCalendarWeekdayRules_DateRange
        CHECK (ValidTo IS NULL OR ValidFrom IS NULL OR ValidTo >= ValidFrom),
    CONSTRAINT UQ_UserCalendarWeekdayRules
        UNIQUE (UserCalendarId, Weekday, ValidFrom)
);
GO

CREATE TABLE dbo.TeamCalendarWeekdayRules
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    TeamCalendarId  BIGINT NOT NULL,
    Weekday         TINYINT NOT NULL,
    DayType         NVARCHAR(30) NOT NULL,
    ValidFrom       DATE NULL,
    ValidTo         DATE NULL,

    CONSTRAINT PK_TeamCalendarWeekdayRules PRIMARY KEY (Id),
    CONSTRAINT FK_TeamCalendarWeekdayRules_TeamCalendars
        FOREIGN KEY (TeamCalendarId) REFERENCES dbo.TeamCalendars (Id),
    CONSTRAINT CK_TeamCalendarWeekdayRules_Weekday
        CHECK (Weekday BETWEEN 0 AND 6),
    CONSTRAINT CK_TeamCalendarWeekdayRules_DayType
        CHECK (DayType IN (N'holiday', N'business_day')),
    CONSTRAINT CK_TeamCalendarWeekdayRules_DateRange
        CHECK (ValidTo IS NULL OR ValidFrom IS NULL OR ValidTo >= ValidFrom),
    CONSTRAINT UQ_TeamCalendarWeekdayRules
        UNIQUE (TeamCalendarId, Weekday, ValidFrom)
);
GO

CREATE TABLE dbo.SystemCalendarWeekdayRules
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    SystemCalendarId BIGINT NOT NULL,
    Weekday         TINYINT NOT NULL,
    DayType         NVARCHAR(30) NOT NULL,
    ValidFrom       DATE NULL,
    ValidTo         DATE NULL,

    CONSTRAINT PK_SystemCalendarWeekdayRules PRIMARY KEY (Id),
    CONSTRAINT FK_SystemCalendarWeekdayRules_SystemCalendars
        FOREIGN KEY (SystemCalendarId) REFERENCES dbo.SystemCalendars (Id),
    CONSTRAINT CK_SystemCalendarWeekdayRules_Weekday
        CHECK (Weekday BETWEEN 0 AND 6),
    CONSTRAINT CK_SystemCalendarWeekdayRules_DayType
        CHECK (DayType IN (N'holiday', N'business_day')),
    CONSTRAINT CK_SystemCalendarWeekdayRules_DateRange
        CHECK (ValidTo IS NULL OR ValidFrom IS NULL OR ValidTo >= ValidFrom),
    CONSTRAINT UQ_SystemCalendarWeekdayRules
        UNIQUE (SystemCalendarId, Weekday, ValidFrom)
);
GO

CREATE TABLE dbo.UserCalendarDates
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    UserCalendarId  BIGINT NOT NULL,
    TargetDate      DATE NOT NULL,
    DateType        NVARCHAR(30) NOT NULL,
    Name            NVARCHAR(200) NOT NULL,
    IsWorkingDay    BIT NULL,
    Priority        INT NOT NULL
        CONSTRAINT DF_UserCalendarDates_Priority DEFAULT 50,

    CONSTRAINT PK_UserCalendarDates PRIMARY KEY (Id),
    CONSTRAINT FK_UserCalendarDates_UserCalendars
        FOREIGN KEY (UserCalendarId) REFERENCES dbo.UserCalendars (Id),
    CONSTRAINT UQ_UserCalendarDates UNIQUE (UserCalendarId, TargetDate)
);
GO

CREATE TABLE dbo.TeamCalendarDates
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    TeamCalendarId  BIGINT NOT NULL,
    TargetDate      DATE NOT NULL,
    DateType        NVARCHAR(30) NOT NULL,
    Name            NVARCHAR(200) NOT NULL,
    IsWorkingDay    BIT NULL,
    Priority        INT NOT NULL
        CONSTRAINT DF_TeamCalendarDates_Priority DEFAULT 50,

    CONSTRAINT PK_TeamCalendarDates PRIMARY KEY (Id),
    CONSTRAINT FK_TeamCalendarDates_TeamCalendars
        FOREIGN KEY (TeamCalendarId) REFERENCES dbo.TeamCalendars (Id),
    CONSTRAINT UQ_TeamCalendarDates UNIQUE (TeamCalendarId, TargetDate)
);
GO

CREATE TABLE dbo.SystemCalendarDates
(
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    SystemCalendarId BIGINT NOT NULL,
    TargetDate      DATE NOT NULL,
    DateType        NVARCHAR(30) NOT NULL,
    Name            NVARCHAR(200) NOT NULL,
    IsWorkingDay    BIT NULL,
    Priority        INT NOT NULL
        CONSTRAINT DF_SystemCalendarDates_Priority DEFAULT 50,

    CONSTRAINT PK_SystemCalendarDates PRIMARY KEY (Id),
    CONSTRAINT FK_SystemCalendarDates_SystemCalendars
        FOREIGN KEY (SystemCalendarId) REFERENCES dbo.SystemCalendars (Id),
    CONSTRAINT UQ_SystemCalendarDates UNIQUE (SystemCalendarId, TargetDate)
);
GO

/* ================================================================
   5. 繰り返しルール
   ================================================================ */

CREATE TABLE dbo.RecurrenceRules
(
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    Frequency           NVARCHAR(20) NOT NULL,
    IntervalValue       INT NOT NULL
        CONSTRAINT DF_RecurrenceRules_IntervalValue DEFAULT 1,
    StartsOn            DATE NOT NULL,
    EndsOn              DATE NULL,
    OccurrenceCount     INT NULL,

    -- 毎月20日、毎年4月20日など
    DayOfMonth          TINYINT NULL,
    MonthOfYear         TINYINT NULL,

    -- 毎月第1月曜日など。-1 は最終週を表す案
    WeekOfMonth         SMALLINT NULL,

    HolidayBehavior     NVARCHAR(40) NOT NULL
        CONSTRAINT DF_RecurrenceRules_HolidayBehavior DEFAULT N'execute',

    CreatedAt            DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_RecurrenceRules_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt            DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_RecurrenceRules_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion           ROWVERSION NOT NULL,

    CONSTRAINT PK_RecurrenceRules PRIMARY KEY (Id),
    CONSTRAINT CK_RecurrenceRules_Frequency
        CHECK (Frequency IN (N'daily', N'weekly', N'monthly', N'yearly')),
    CONSTRAINT CK_RecurrenceRules_IntervalValue
        CHECK (IntervalValue > 0),
    CONSTRAINT CK_RecurrenceRules_DateRange
        CHECK (EndsOn IS NULL OR EndsOn >= StartsOn),
    CONSTRAINT CK_RecurrenceRules_OccurrenceCount
        CHECK (OccurrenceCount IS NULL OR OccurrenceCount > 0),
    CONSTRAINT CK_RecurrenceRules_DayOfMonth
        CHECK (DayOfMonth IS NULL OR DayOfMonth BETWEEN 1 AND 31),
    CONSTRAINT CK_RecurrenceRules_MonthOfYear
        CHECK (MonthOfYear IS NULL OR MonthOfYear BETWEEN 1 AND 12),
    CONSTRAINT CK_RecurrenceRules_WeekOfMonth
        CHECK (WeekOfMonth IS NULL OR WeekOfMonth BETWEEN -1 AND 5),
    CONSTRAINT CK_RecurrenceRules_HolidayBehavior
        CHECK (HolidayBehavior IN
            (N'execute', N'skip', N'move_previous_business_day',
             N'move_next_business_day'))
);
GO

CREATE TABLE dbo.RecurrenceWeekdays
(
    RecurrenceRuleId    BIGINT NOT NULL,
    Weekday             TINYINT NOT NULL,

    CONSTRAINT PK_RecurrenceWeekdays PRIMARY KEY
        (RecurrenceRuleId, Weekday),
    CONSTRAINT FK_RecurrenceWeekdays_RecurrenceRules
        FOREIGN KEY (RecurrenceRuleId) REFERENCES dbo.RecurrenceRules (Id),
    CONSTRAINT CK_RecurrenceWeekdays_Weekday
        CHECK (Weekday BETWEEN 0 AND 6)
);
GO

/* ================================================================
   6. 予定本体・日時
   ================================================================ */

CREATE TABLE dbo.Events
(
    Id                  BIGINT IDENTITY(1,1) NOT NULL,

    UserCalendarId      BIGINT NULL,
    TeamCalendarId      BIGINT NULL,
    SystemCalendarId    BIGINT NULL,

    Title               NVARCHAR(300) NOT NULL,
    Description         NVARCHAR(MAX) NULL,
    EventType           NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Events_EventType DEFAULT N'appointment',
    Priority            INT NOT NULL
        CONSTRAINT DF_Events_Priority DEFAULT 50,
    ConflictBehavior    NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Events_ConflictBehavior DEFAULT N'warning',
    Status              NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Events_Status DEFAULT N'active',

    CreatedByUserId     BIGINT NULL,
    DataVersion         BIGINT NOT NULL
        CONSTRAINT DF_Events_DataVersion DEFAULT 1,
    CreatedAt           DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_Events_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt           DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_Events_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion           ROWVERSION NOT NULL,

    CONSTRAINT PK_Events PRIMARY KEY (Id),
    CONSTRAINT FK_Events_UserCalendars FOREIGN KEY (UserCalendarId)
        REFERENCES dbo.UserCalendars (Id),
    CONSTRAINT FK_Events_TeamCalendars FOREIGN KEY (TeamCalendarId)
        REFERENCES dbo.TeamCalendars (Id),
    CONSTRAINT FK_Events_SystemCalendars FOREIGN KEY (SystemCalendarId)
        REFERENCES dbo.SystemCalendars (Id),
    CONSTRAINT FK_Events_CreatedByUser FOREIGN KEY (CreatedByUserId)
        REFERENCES dbo.Users (Id),
    CONSTRAINT CK_Events_ExactlyOneOwner CHECK
        ((CASE WHEN UserCalendarId IS NULL THEN 0 ELSE 1 END)
       + (CASE WHEN TeamCalendarId IS NULL THEN 0 ELSE 1 END)
       + (CASE WHEN SystemCalendarId IS NULL THEN 0 ELSE 1 END) = 1),
    CONSTRAINT CK_Events_Priority CHECK (Priority BETWEEN 0 AND 100),
    CONSTRAINT CK_Events_ConflictBehavior CHECK
        (ConflictBehavior IN (N'override', N'coexist', N'warning', N'block')),
    CONSTRAINT CK_Events_Status CHECK
        (Status IN (N'active', N'cancelled', N'draft')),
    CONSTRAINT CK_Events_DataVersion CHECK (DataVersion > 0)
);
GO

CREATE TABLE dbo.EventSchedules
(
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    EventId             BIGINT NOT NULL,

    -- 一点の予定も StartsAt から1分後の EndsAt を保存する
    StartsAt            DATETIMEOFFSET(0) NOT NULL,
    EndsAt              DATETIMEOFFSET(0) NOT NULL,
    IsAllDay            BIT NOT NULL
        CONSTRAINT DF_EventSchedules_IsAllDay DEFAULT 0,
    TimeZoneId          NVARCHAR(64) NOT NULL
        CONSTRAINT DF_EventSchedules_TimeZoneId DEFAULT N'Asia/Tokyo',
    RecurrenceRuleId    BIGINT NULL,

    -- 発生日生成の状態。通常は深夜バッチ、新規登録時は即時処理で更新する。
    GenerationStatus    NVARCHAR(20) NOT NULL
        CONSTRAINT DF_EventSchedules_GenerationStatus DEFAULT N'pending',
    DefinitionVersion   BIGINT NOT NULL
        CONSTRAINT DF_EventSchedules_DefinitionVersion DEFAULT 1,
    GeneratedVersion    BIGINT NULL,
    GeneratedFrom       DATETIMEOFFSET(0) NULL,
    GeneratedUntil      DATETIMEOFFSET(0) NULL,
    GeneratedAt         DATETIMEOFFSET(0) NULL,
    LastGenerationError NVARCHAR(2000) NULL,

    CreatedAt            DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_EventSchedules_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt            DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_EventSchedules_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion           ROWVERSION NOT NULL,

    CONSTRAINT PK_EventSchedules PRIMARY KEY (Id),
    CONSTRAINT UQ_EventSchedules_Event UNIQUE (EventId),
    CONSTRAINT FK_EventSchedules_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (Id),
    CONSTRAINT FK_EventSchedules_RecurrenceRules FOREIGN KEY (RecurrenceRuleId)
        REFERENCES dbo.RecurrenceRules (Id),
    CONSTRAINT CK_EventSchedules_DateRange CHECK (EndsAt > StartsAt),
    CONSTRAINT CK_EventSchedules_GenerationStatus CHECK
        (GenerationStatus IN (N'pending', N'processing', N'reflected', N'failed')),
    CONSTRAINT CK_EventSchedules_GeneratedRange CHECK
        (GeneratedUntil IS NULL OR GeneratedFrom IS NULL
         OR GeneratedUntil > GeneratedFrom),
    CONSTRAINT CK_EventSchedules_GeneratedVersion CHECK
        (GeneratedVersion IS NULL OR GeneratedVersion > 0)
);
GO

/* ================================================================
   7. 繰り返し予定の例外
   ================================================================ */

CREATE TABLE dbo.RecurrenceExceptions
(
    Id                   BIGINT IDENTITY(1,1) NOT NULL,
    RecurrenceRuleId     BIGINT NOT NULL,
    OriginalStartAt      DATETIMEOFFSET(0) NOT NULL,
    ExceptionType        NVARCHAR(20) NOT NULL,
    ReplacementStartAt   DATETIMEOFFSET(0) NULL,
    ReplacementEndAt     DATETIMEOFFSET(0) NULL,
    Reason               NVARCHAR(1000) NULL,
    CreatedByUserId      BIGINT NULL,
    CreatedAt             DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_RecurrenceExceptions_CreatedAt DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT PK_RecurrenceExceptions PRIMARY KEY (Id),
    CONSTRAINT FK_RecurrenceExceptions_RecurrenceRules
        FOREIGN KEY (RecurrenceRuleId) REFERENCES dbo.RecurrenceRules (Id),
    CONSTRAINT FK_RecurrenceExceptions_Users
        FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT UQ_RecurrenceExceptions_Original
        UNIQUE (RecurrenceRuleId, OriginalStartAt),
    CONSTRAINT CK_RecurrenceExceptions_Type
        CHECK (ExceptionType IN (N'skip', N'move', N'change')),
    CONSTRAINT CK_RecurrenceExceptions_Replacement
        CHECK (ExceptionType = N'skip' OR ReplacementStartAt IS NOT NULL),
    CONSTRAINT CK_RecurrenceExceptions_ReplacementRange
        CHECK (ReplacementEndAt IS NULL
               OR ReplacementStartAt IS NULL
               OR ReplacementEndAt > ReplacementStartAt)
);
GO

/* ================================================================
   8. 発生日

   EventSchedules.GeneratedFrom / GeneratedUntil は、対象期間の計算済み範囲。
   EventOccurrences.ReflectionStatus は、各発生日レコードの反映状態。
   ================================================================ */

CREATE TABLE dbo.EventOccurrences
(
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    EventId             BIGINT NOT NULL,
    EventScheduleId     BIGINT NOT NULL,

    StartsAt            DATETIMEOFFSET(0) NOT NULL,
    EndsAt              DATETIMEOFFSET(0) NOT NULL,
    OccurrenceDate      DATE NOT NULL,
    OccurrenceKey       NVARCHAR(200) NOT NULL,

    OccurrenceStatus    NVARCHAR(30) NOT NULL
        CONSTRAINT DF_EventOccurrences_OccurrenceStatus DEFAULT N'generated',
    GenerationSource    NVARCHAR(30) NOT NULL
        CONSTRAINT DF_EventOccurrences_GenerationSource DEFAULT N'batch',

    ReflectionStatus    NVARCHAR(30) NOT NULL
        CONSTRAINT DF_EventOccurrences_ReflectionStatus DEFAULT N'pending',
    ReflectedAt         DATETIMEOFFSET(0) NULL,
    SourceVersion       BIGINT NULL,
    ReflectedVersion    BIGINT NULL,
    LastErrorCode       NVARCHAR(100) NULL,
    LastErrorMessage    NVARCHAR(2000) NULL,

    CreatedAt            DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_EventOccurrences_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt            DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_EventOccurrences_UpdatedAt DEFAULT SYSDATETIMEOFFSET(),
    RowVersion           ROWVERSION NOT NULL,

    CONSTRAINT PK_EventOccurrences PRIMARY KEY (Id),
    CONSTRAINT FK_EventOccurrences_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (Id),
    CONSTRAINT FK_EventOccurrences_EventSchedules FOREIGN KEY (EventScheduleId)
        REFERENCES dbo.EventSchedules (Id),
    CONSTRAINT UQ_EventOccurrences_Key
        UNIQUE (EventScheduleId, OccurrenceKey),
    CONSTRAINT CK_EventOccurrences_DateRange CHECK (EndsAt > StartsAt),
    CONSTRAINT CK_EventOccurrences_OccurrenceStatus CHECK
        (OccurrenceStatus IN
            (N'generated', N'skipped', N'overridden', N'cancelled')),
    CONSTRAINT CK_EventOccurrences_GenerationSource CHECK
        (GenerationSource IN (N'batch', N'immediate', N'manual')),
    CONSTRAINT CK_EventOccurrences_ReflectionStatus CHECK
        (ReflectionStatus IN (N'pending', N'processing', N'reflected', N'failed'))
);
GO

/* ================================================================
   9. 参加者
   ================================================================ */

CREATE TABLE dbo.EventParticipants
(
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    EventId             BIGINT NOT NULL,
    UserId              BIGINT NULL,
    TeamId              BIGINT NULL,
    ParticipationRole   NVARCHAR(30) NOT NULL
        CONSTRAINT DF_EventParticipants_Role DEFAULT N'participant',
    ResponseStatus      NVARCHAR(30) NOT NULL
        CONSTRAINT DF_EventParticipants_ResponseStatus DEFAULT N'pending',

    CONSTRAINT PK_EventParticipants PRIMARY KEY (Id),
    CONSTRAINT FK_EventParticipants_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (Id),
    CONSTRAINT FK_EventParticipants_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id),
    CONSTRAINT FK_EventParticipants_Teams FOREIGN KEY (TeamId)
        REFERENCES dbo.Teams (Id),
    CONSTRAINT CK_EventParticipants_ExactlyOneTarget CHECK
        ((CASE WHEN UserId IS NULL THEN 0 ELSE 1 END)
       + (CASE WHEN TeamId IS NULL THEN 0 ELSE 1 END) = 1),
    CONSTRAINT CK_EventParticipants_Role CHECK
        (ParticipationRole IN (N'owner', N'participant', N'observer')),
    CONSTRAINT CK_EventParticipants_ResponseStatus CHECK
        (ResponseStatus IN
            (N'pending', N'accepted', N'tentative', N'declined'))
);
GO

/* ================================================================
   10. 自然言語入力の記録
   ================================================================ */

CREATE TABLE dbo.EventInputLogs
(
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    UserId              BIGINT NULL,
    RawText             NVARCHAR(2000) NOT NULL,
    ParsedDataJson      NVARCHAR(MAX) NULL,
    ParserVersion       NVARCHAR(50) NULL,
    ParseStatus         NVARCHAR(30) NOT NULL
        CONSTRAINT DF_EventInputLogs_ParseStatus DEFAULT N'pending',
    Confidence          DECIMAL(5,4) NULL,
    CreatedEventId      BIGINT NULL,
    CreatedAt           DATETIMEOFFSET(0) NOT NULL
        CONSTRAINT DF_EventInputLogs_CreatedAt DEFAULT SYSDATETIMEOFFSET(),

    CONSTRAINT PK_EventInputLogs PRIMARY KEY (Id),
    CONSTRAINT FK_EventInputLogs_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id),
    CONSTRAINT FK_EventInputLogs_Events FOREIGN KEY (CreatedEventId)
        REFERENCES dbo.Events (Id),
    CONSTRAINT CK_EventInputLogs_ParseStatus CHECK
        (ParseStatus IN (N'pending', N'confirmed', N'rejected', N'failed')),
    CONSTRAINT CK_EventInputLogs_Confidence CHECK
        (Confidence IS NULL OR (Confidence >= 0 AND Confidence <= 1))
);
GO

/* ================================================================
   11. インデックス
   ================================================================ */

CREATE INDEX IX_TeamMembers_UserId
    ON dbo.TeamMembers (UserId);
GO

CREATE INDEX IX_UserCalendars_UserId
    ON dbo.UserCalendars (UserId);
GO

CREATE INDEX IX_TeamCalendars_TeamId
    ON dbo.TeamCalendars (TeamId);
GO

CREATE INDEX IX_Events_UserCalendarId
    ON dbo.Events (UserCalendarId);
GO

CREATE INDEX IX_Events_TeamCalendarId
    ON dbo.Events (TeamCalendarId);
GO

CREATE INDEX IX_Events_SystemCalendarId
    ON dbo.Events (SystemCalendarId);
GO

CREATE INDEX IX_Events_Status
    ON dbo.Events (Status, Priority);
GO

CREATE INDEX IX_EventSchedules_Generation
    ON dbo.EventSchedules (GenerationStatus, GeneratedVersion, GeneratedUntil);
GO

CREATE INDEX IX_EventOccurrences_TimeRange
    ON dbo.EventOccurrences (StartsAt, EndsAt);
GO

CREATE INDEX IX_EventOccurrences_Event
    ON dbo.EventOccurrences (EventId, StartsAt);
GO

CREATE INDEX IX_EventOccurrences_Reflection
    ON dbo.EventOccurrences (ReflectionStatus, ReflectedVersion);
GO

