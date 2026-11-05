using App.Domain.Exceptions;
using App.Domain.Models;
using App.Domain.Services;
using App.Models.Maintain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Controllers;

public sealed class MaintainController : Controller {
    private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
    private readonly FiscalYearService _fiscalYearService;
    private readonly UserService _userService;
    private readonly UserCalendarService _userCalendarService;
    private readonly OneTimeEventService _oneTimeEventService;
    private readonly TimeProvider _timeProvider;

    public MaintainController(
        FiscalYearService fiscalYearService, UserService userService,
        UserCalendarService userCalendarService, OneTimeEventService oneTimeEventService, TimeProvider timeProvider) {
        _fiscalYearService = fiscalYearService;
        _userService = userService;
        _userCalendarService = userCalendarService;
        _oneTimeEventService = oneTimeEventService;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(FiscalYears));

    [HttpGet]
    public async Task<IActionResult> FiscalYears(CancellationToken cancellationToken) {
        var fiscalYears = await _fiscalYearService.GetAllAsync(cancellationToken);
        var model = fiscalYears.Select(item => new FiscalYearListItemViewModel {
            Year = item.Year,
            Name = item.Name,
            StartsOn = item.StartsOn,
            EndsOn = item.EndsOn,
            Status = item.Status
        }).ToList();
        return View(model);
    }

    [HttpGet]
    public IActionResult CreateFiscalYear() {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), DefaultTimeZone).DateTime);
        var year = FiscalYearCalculator.GetYear(today);
        return View(new CreateFiscalYearViewModel { Year = year, Name = $"{year}年度" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFiscalYear(
        CreateFiscalYearViewModel model, CancellationToken cancellationToken) {
        if (!ModelState.IsValid) {
            return View(model);
        }

        try {
            var created = await _fiscalYearService.CreateAsync(model.Year!.Value, model.Name, cancellationToken);
            TempData["SuccessMessage"] = $"{created.Year}年度を登録しました。";
            return RedirectToAction(nameof(FiscalYears));
        } catch (DuplicateFiscalYearException exception) {
            ModelState.AddModelError(nameof(model.Year), exception.Message);
        } catch (ArgumentException exception) when (exception.ParamName is "year" or "name") {
            var field = exception.ParamName == "year" ? nameof(model.Year) : nameof(model.Name);
            var error = exception.ParamName == "year"
                ? "年度は1から9998までを指定してください。"
                : "名称を入力し、50文字以内で指定してください。";
            ModelState.AddModelError(field, error);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken) {
        var users = await _userService.GetAllAsync(cancellationToken);
        var model = users.Select(user => new UserListItemViewModel {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            TimeZoneId = user.TimeZoneId
        }).ToList();
        return View(model);
    }

    [HttpGet]
    public IActionResult CreateUser() => View(new CreateUserViewModel { TimeZoneOptions = GetTimeZoneOptions() });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model, CancellationToken cancellationToken) {
        if (ModelState.IsValid) {
            try {
                var created = await _userService.CreateAsync(model.Name, model.Email, model.TimeZoneId, cancellationToken);
                TempData["SuccessMessage"] = $"「{created.Name}」を登録しました。";
                return RedirectToAction(nameof(Users));
            } catch (ArgumentException exception) when (exception.ParamName is "name" or "email" or "timeZoneId") {
                var (field, error) = exception.ParamName switch {
                    "name" => (nameof(model.Name), "氏名を入力し、100文字以内で指定してください。"),
                    "email" => (nameof(model.Email), "メールアドレスは正しい形式で255文字以内で入力してください。"),
                    _ => (nameof(model.TimeZoneId), "有効なタイムゾーンを選択してください。")
                };
                ModelState.AddModelError(field, error);
            }
        }

        model.TimeZoneOptions = GetTimeZoneOptions();
        return View(model);
    }

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
        model.TimeZoneOptions = GetTimeZoneOptions();
    }

    private static IReadOnlyList<SelectListItem> GetTimeZoneOptions() =>
        TimeZoneInfo.GetSystemTimeZones()
            .Select(zone => TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId) ? ianaId : zone.Id)
            .Append(UserService.DefaultTimeZoneId)
            .Append("UTC")
            .Distinct(StringComparer.Ordinal)
            .Select(TimeZoneInfo.FindSystemTimeZoneById)
            .OrderBy(zone => zone.BaseUtcOffset)
            .ThenBy(zone => zone.Id, StringComparer.Ordinal)
            .Select(zone => new SelectListItem($"{zone.DisplayName} ({zone.Id})", zone.Id))
            .ToList();
}
