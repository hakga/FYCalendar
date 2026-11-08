using App.Domain.Exceptions;
using App.Domain.Services;
using App.Models;
using App.Models.Maintain;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

public sealed class MaintainController : Controller {
    private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
    private readonly FiscalYearService _fiscalYearService;
    private readonly UserService _userService;
    private readonly TimeProvider _timeProvider;

    public MaintainController(
        FiscalYearService fiscalYearService, UserService userService, TimeProvider timeProvider) {
        _fiscalYearService = fiscalYearService;
        _userService = userService;
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
    public IActionResult CreateUser() => View(new CreateUserViewModel { TimeZoneOptions = TimeZoneSelectList.Create() });

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

        model.TimeZoneOptions = TimeZoneSelectList.Create();
        return View(model);
    }

}
