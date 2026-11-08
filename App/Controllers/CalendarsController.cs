using App.Domain.Exceptions;
using App.Domain.Models;
using App.Domain.Services;
using App.Models;
using App.Models.Calendars;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Controllers;

public sealed class CalendarsController : Controller {
    private readonly UserService _userService;
    private readonly UserCalendarService _userCalendarService;
    private readonly TeamService _teamService;
    private readonly TeamCalendarService _teamCalendarService;
    private readonly OneTimeEventService _oneTimeEventService;

    public CalendarsController(
        UserService userService, UserCalendarService userCalendarService,
        TeamService teamService, TeamCalendarService teamCalendarService, OneTimeEventService oneTimeEventService) {
        _userService = userService;
        _userCalendarService = userCalendarService;
        _teamService = teamService;
        _teamCalendarService = teamCalendarService;
        _oneTimeEventService = oneTimeEventService;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(UserCalendars));

    [HttpGet]
    public async Task<IActionResult> UserCalendars(CancellationToken cancellationToken) {
        var calendars = await _userCalendarService.GetAllAsync(cancellationToken);
        var model = calendars.Select(calendar => new UserCalendarListItemViewModel {
            Id = calendar.Id,
            UserId = calendar.UserId,
            UserName = calendar.User.Name,
            Name = calendar.Name,
            CalendarType = calendar.CalendarType,
            TimeZoneId = calendar.TimeZoneId,
            DisplayColor = calendar.DisplayColor,
            IsActive = calendar.IsActive
        }).ToList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUserCalendar(CancellationToken cancellationToken) {
        var model = new CreateUserCalendarViewModel();
        await PopulateUserCalendarOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUserCalendar(
        CreateUserCalendarViewModel model, CancellationToken cancellationToken) {
        if (ModelState.IsValid) {
            try {
                var created = await _userCalendarService.CreateAsync(
                    model.UserId!.Value, model.Name, model.TimeZoneId, model.DisplayColor, cancellationToken);
                TempData["SuccessMessage"] = $"「{created.Name}」を登録しました。";
                return RedirectToAction(nameof(UserCalendars));
            } catch (ArgumentException exception) when (exception.ParamName is "userId" or "name" or "timeZoneId" or "displayColor") {
                var (field, error) = exception.ParamName switch {
                    "userId" => (nameof(model.UserId), "選択した利用者が見つかりません。利用者を選択し直してください。"),
                    "name" => (nameof(model.Name), "カレンダー名を入力し、200文字以内で指定してください。"),
                    "timeZoneId" => (nameof(model.TimeZoneId), "有効なタイムゾーンを選択してください。"),
                    _ => (nameof(model.DisplayColor), "表示色は#に続く6桁の16進数で入力してください。")
                };
                ModelState.AddModelError(field, error);
            }
        }

        await PopulateUserCalendarOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TeamCalendars(CancellationToken cancellationToken) {
        var calendars = await _teamCalendarService.GetAllAsync(cancellationToken);
        return View(calendars.Select(calendar => new TeamCalendarListItemViewModel(
            calendar.Id, calendar.TeamId, calendar.Team.Name, calendar.Name, calendar.CalendarType,
            calendar.TimeZoneId, calendar.DisplayColor, calendar.IsActive)).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> CreateTeamCalendar(CancellationToken cancellationToken, long? teamId = null) {
        var model = new CreateTeamCalendarViewModel { TeamId = teamId };
        await PopulateTeamCalendarOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeamCalendar(CreateTeamCalendarViewModel model, CancellationToken cancellationToken) {
        if (ModelState.IsValid) {
            try {
                var created = await _teamCalendarService.CreateAsync(
                    model.TeamId!.Value, model.Name, model.TimeZoneId, model.DisplayColor, cancellationToken);
                TempData["SuccessMessage"] = $"「{created.Name}」を登録しました。";
                return RedirectToAction(nameof(TeamCalendars));
            } catch (ArgumentException exception) when (exception.ParamName is "teamId" or "name" or "timeZoneId" or "displayColor") {
                var (field, error) = exception.ParamName switch {
                    "teamId" => (nameof(model.TeamId), "選択したチームが見つかりません。チームを選択し直してください。"),
                    "name" => (nameof(model.Name), "カレンダー名を入力し、200文字以内で指定してください。"),
                    "timeZoneId" => (nameof(model.TimeZoneId), "有効なタイムゾーンを選択してください。"),
                    _ => (nameof(model.DisplayColor), "表示色は#に続く6桁の16進数で入力してください。")
                };
                ModelState.AddModelError(field, error);
            }
        }
        await PopulateTeamCalendarOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Events(CancellationToken cancellationToken) {
        var schedules = await _oneTimeEventService.GetAllAsync(cancellationToken);
        var model = schedules.Select(schedule => {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
            return new EventListItemViewModel(schedule.EventId, schedule.Event.Title, schedule.Event.Description,
                schedule.Event.UserCalendar!.User.Name, schedule.Event.UserCalendar.Name,
                TimeZoneInfo.ConvertTime(schedule.StartsAt, zone), TimeZoneInfo.ConvertTime(schedule.EndsAt, zone),
                schedule.TimeZoneId, schedule.Event.Priority);
        }).ToList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateEvent(CancellationToken cancellationToken, long? userCalendarId = null) {
        var model = new CreateEventViewModel { UserCalendarId = userCalendarId };
        await PopulateEventOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(CreateEventViewModel model, CancellationToken cancellationToken) {
        if (ModelState.IsValid) {
            try {
                var result = await _oneTimeEventService.RegisterAsync(new OneTimeEventRequest(
                    model.UserCalendarId!.Value, model.Title, model.StartsAtLocal!.Value,
                    model.EndsAtLocal, model.Description), model.ConfirmOverlap, cancellationToken);
                if (!result.RequiresConfirmation) {
                    TempData["SuccessMessage"] = $"「{model.Title}」を登録しました。";
                    return RedirectToAction(nameof(Events));
                }
                model.Conflicts = result.Conflicts;
            } catch (EventValidationException exception) {
                ModelState.AddModelError(exception.FieldName, exception.Message);
            }
        }
        await PopulateEventOptionsAsync(model, cancellationToken);
        return View(model);
    }

    private async Task PopulateTeamCalendarOptionsAsync(CreateTeamCalendarViewModel model, CancellationToken cancellationToken) {
        var teams = await _teamService.GetAllAsync(cancellationToken);
        model.TeamOptions = teams.Select(team => new SelectListItem(
            $"{team.Name}（ID: {team.Id} / {team.TimeZoneId}）", team.Id.ToString())).ToList();
        model.TimeZoneOptions = TimeZoneSelectList.Create();
        var calendars = await _teamCalendarService.GetAllAsync(cancellationToken);
        model.NameOptions = calendars.Select(calendar => calendar.Name).Distinct(StringComparer.Ordinal).ToList();
    }

    private async Task PopulateEventOptionsAsync(CreateEventViewModel model, CancellationToken cancellationToken) {
        var calendars = await _userCalendarService.GetAllAsync(cancellationToken);
        model.CalendarOptions = calendars.Where(calendar => calendar.IsActive && calendar.CalendarType == "schedule")
            .Select(calendar => new SelectListItem(
                $"{calendar.Name}（{calendar.User.Name} / ID: {calendar.Id} / {calendar.TimeZoneId}）", calendar.Id.ToString())).ToList();
    }

    private async Task PopulateUserCalendarOptionsAsync(CreateUserCalendarViewModel model, CancellationToken cancellationToken) {
        var users = await _userService.GetAllAsync(cancellationToken);
        model.UserOptions = users.Select(user => new SelectListItem(
            $"{user.Name}（ID: {user.Id} / {user.TimeZoneId}）", user.Id.ToString())).ToList();
        model.TimeZoneOptions = TimeZoneSelectList.Create();
        var calendars = await _userCalendarService.GetAllAsync(cancellationToken);
        model.NameOptions = calendars.Select(calendar => calendar.Name).Distinct(StringComparer.Ordinal).ToList();
    }

}
