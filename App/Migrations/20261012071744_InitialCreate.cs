using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "open")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecurrenceRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Frequency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IntervalValue = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: true),
                    DayOfMonth = table.Column<byte>(type: "tinyint", nullable: true),
                    MonthOfYear = table.Column<byte>(type: "tinyint", nullable: true),
                    WeekOfMonth = table.Column<short>(type: "smallint", nullable: true),
                    HolidayBehavior = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "execute"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemCalendars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CalendarType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Tokyo"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Tokyo"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Tokyo"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecurrenceWeekdays",
                columns: table => new
                {
                    RecurrenceRuleId = table.Column<long>(type: "bigint", nullable: false),
                    Weekday = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceWeekdays", x => new { x.RecurrenceRuleId, x.Weekday });
                    table.ForeignKey(
                        name: "FK_RecurrenceWeekdays_RecurrenceRules",
                        column: x => x.RecurrenceRuleId,
                        principalTable: "RecurrenceRules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemCalendarDates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemCalendarId = table.Column<long>(type: "bigint", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DateType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 50)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemCalendarDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemCalendarDates_SystemCalendars",
                        column: x => x.SystemCalendarId,
                        principalTable: "SystemCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemCalendarWeekdayRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemCalendarId = table.Column<long>(type: "bigint", nullable: false),
                    Weekday = table.Column<byte>(type: "tinyint", nullable: false),
                    DayType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemCalendarWeekdayRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemCalendarWeekdayRules_SystemCalendars",
                        column: x => x.SystemCalendarId,
                        principalTable: "SystemCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamCalendars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CalendarType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "schedule"),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Tokyo"),
                    DisplayColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCalendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamCalendars_Teams",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RecurrenceExceptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecurrenceRuleId = table.Column<long>(type: "bigint", nullable: false),
                    OriginalStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReplacementStartAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ReplacementEndAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurrenceExceptions_RecurrenceRules",
                        column: x => x.RecurrenceRuleId,
                        principalTable: "RecurrenceRules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecurrenceExceptions_Users",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    TeamId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "member"),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => new { x.TeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserCalendars",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CalendarType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "schedule"),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Tokyo"),
                    DisplayColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCalendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCalendars_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamCalendarDates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamCalendarId = table.Column<long>(type: "bigint", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DateType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 50)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCalendarDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamCalendarDates_TeamCalendars",
                        column: x => x.TeamCalendarId,
                        principalTable: "TeamCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeamCalendarWeekdayRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamCalendarId = table.Column<long>(type: "bigint", nullable: false),
                    Weekday = table.Column<byte>(type: "tinyint", nullable: false),
                    DayType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamCalendarWeekdayRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamCalendarWeekdayRules_TeamCalendars",
                        column: x => x.TeamCalendarId,
                        principalTable: "TeamCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserCalendarId = table.Column<long>(type: "bigint", nullable: true),
                    TeamCalendarId = table.Column<long>(type: "bigint", nullable: true),
                    SystemCalendarId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "appointment"),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    ConflictBehavior = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "warning"),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "active"),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    DataVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_CreatedByUser",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Events_SystemCalendars",
                        column: x => x.SystemCalendarId,
                        principalTable: "SystemCalendars",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Events_TeamCalendars",
                        column: x => x.TeamCalendarId,
                        principalTable: "TeamCalendars",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Events_UserCalendars",
                        column: x => x.UserCalendarId,
                        principalTable: "UserCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserCalendarDates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserCalendarId = table.Column<long>(type: "bigint", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DateType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 50)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCalendarDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCalendarDates_UserCalendars",
                        column: x => x.UserCalendarId,
                        principalTable: "UserCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserCalendarWeekdayRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserCalendarId = table.Column<long>(type: "bigint", nullable: false),
                    Weekday = table.Column<byte>(type: "tinyint", nullable: false),
                    DayType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCalendarWeekdayRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCalendarWeekdayRules_UserCalendars",
                        column: x => x.UserCalendarId,
                        principalTable: "UserCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventInputLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ParsedDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParserVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ParseStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    CreatedEventId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInputLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventInputLogs_Events",
                        column: x => x.CreatedEventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventInputLogs_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventParticipants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    TeamId = table.Column<long>(type: "bigint", nullable: true),
                    ParticipationRole = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "participant"),
                    ResponseStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventParticipants_Events",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventParticipants_Teams",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventParticipants_Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventSchedules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Tokyo"),
                    RecurrenceRuleId = table.Column<long>(type: "bigint", nullable: true),
                    GenerationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    DefinitionVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    GeneratedVersion = table.Column<long>(type: "bigint", nullable: true),
                    GeneratedFrom = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    GeneratedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    LastGenerationError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSchedules_Events",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventSchedules_RecurrenceRules",
                        column: x => x.RecurrenceRuleId,
                        principalTable: "RecurrenceRules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventOccurrences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    EventScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurrenceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OccurrenceStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "generated"),
                    GenerationSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "batch"),
                    ReflectionStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    ReflectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    SourceVersion = table.Column<long>(type: "bigint", nullable: true),
                    ReflectedVersion = table.Column<long>(type: "bigint", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false, defaultValueSql: "(sysdatetimeoffset())"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventOccurrences_EventSchedules",
                        column: x => x.EventScheduleId,
                        principalTable: "EventSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventOccurrences_Events",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventInputLogs_CreatedEventId",
                table: "EventInputLogs",
                column: "CreatedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventInputLogs_UserId",
                table: "EventInputLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventOccurrences_Event",
                table: "EventOccurrences",
                columns: new[] { "EventId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventOccurrences_Reflection",
                table: "EventOccurrences",
                columns: new[] { "ReflectionStatus", "ReflectedVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_EventOccurrences_TimeRange",
                table: "EventOccurrences",
                columns: new[] { "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_EventOccurrences_Key",
                table: "EventOccurrences",
                columns: new[] { "EventScheduleId", "OccurrenceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_EventId",
                table: "EventParticipants",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_TeamId",
                table: "EventParticipants",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_UserId",
                table: "EventParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedByUserId",
                table: "Events",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status",
                table: "Events",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_SystemCalendarId",
                table: "Events",
                column: "SystemCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_TeamCalendarId",
                table: "Events",
                column: "TeamCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UserCalendarId",
                table: "Events",
                column: "UserCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSchedules_Generation",
                table: "EventSchedules",
                columns: new[] { "GenerationStatus", "GeneratedVersion", "GeneratedUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSchedules_RecurrenceRuleId",
                table: "EventSchedules",
                column: "RecurrenceRuleId");

            migrationBuilder.CreateIndex(
                name: "UQ_EventSchedules_Event",
                table: "EventSchedules",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_FiscalYears_FiscalYear",
                table: "FiscalYears",
                column: "FiscalYear",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurrenceExceptions_CreatedByUserId",
                table: "RecurrenceExceptions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_RecurrenceExceptions_Original",
                table: "RecurrenceExceptions",
                columns: new[] { "RecurrenceRuleId", "OriginalStartAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SystemCalendarDates",
                table: "SystemCalendarDates",
                columns: new[] { "SystemCalendarId", "TargetDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SystemCalendarWeekdayRules",
                table: "SystemCalendarWeekdayRules",
                columns: new[] { "SystemCalendarId", "Weekday", "ValidFrom" },
                unique: true,
                filter: "[ValidFrom] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_TeamCalendarDates",
                table: "TeamCalendarDates",
                columns: new[] { "TeamCalendarId", "TargetDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamCalendars_TeamId",
                table: "TeamCalendars",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "UQ_TeamCalendarWeekdayRules",
                table: "TeamCalendarWeekdayRules",
                columns: new[] { "TeamCalendarId", "Weekday", "ValidFrom" },
                unique: true,
                filter: "[ValidFrom] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_UserCalendarDates",
                table: "UserCalendarDates",
                columns: new[] { "UserCalendarId", "TargetDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCalendars_UserId",
                table: "UserCalendars",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_UserCalendarWeekdayRules",
                table: "UserCalendarWeekdayRules",
                columns: new[] { "UserCalendarId", "Weekday", "ValidFrom" },
                unique: true,
                filter: "[ValidFrom] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventInputLogs");

            migrationBuilder.DropTable(
                name: "EventOccurrences");

            migrationBuilder.DropTable(
                name: "EventParticipants");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "RecurrenceExceptions");

            migrationBuilder.DropTable(
                name: "RecurrenceWeekdays");

            migrationBuilder.DropTable(
                name: "SystemCalendarDates");

            migrationBuilder.DropTable(
                name: "SystemCalendarWeekdayRules");

            migrationBuilder.DropTable(
                name: "TeamCalendarDates");

            migrationBuilder.DropTable(
                name: "TeamCalendarWeekdayRules");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "UserCalendarDates");

            migrationBuilder.DropTable(
                name: "UserCalendarWeekdayRules");

            migrationBuilder.DropTable(
                name: "EventSchedules");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "RecurrenceRules");

            migrationBuilder.DropTable(
                name: "SystemCalendars");

            migrationBuilder.DropTable(
                name: "TeamCalendars");

            migrationBuilder.DropTable(
                name: "UserCalendars");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
