using App.Data.Entities;
using App.Domain.Exceptions;
using App.Domain.Services;
using App.Models;
using App.Models.Teams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Controllers;

public sealed class TeamsController(TeamService teamService, UserService userService) : Controller {
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) {
        var teams = await teamService.GetAllAsync(cancellationToken);
        return View(teams.Select(team => new TeamListItemViewModel(team.Id, team.Name, team.TimeZoneId)).ToList());
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateTeamViewModel { TimeZoneOptions = TimeZoneSelectList.Create() });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTeamViewModel model, CancellationToken cancellationToken) {
        if (ModelState.IsValid) {
            try {
                var team = await teamService.CreateAsync(model.Name, model.TimeZoneId, cancellationToken);
                TempData["SuccessMessage"] = $"「{team.Name}」を登録しました。";
                return RedirectToAction(nameof(Index));
            } catch (ArgumentException exception) when (exception.ParamName is "name" or "timeZoneId") {
                var field = exception.ParamName == "name" ? nameof(model.Name) : nameof(model.TimeZoneId);
                ModelState.AddModelError(field, exception.ParamName == "name"
                    ? "チーム名を入力し、200文字以内で指定してください。"
                    : "有効なタイムゾーンを選択してください。");
            }
        }
        model.TimeZoneOptions = TimeZoneSelectList.Create();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Members(long id, CancellationToken cancellationToken) {
        var team = await teamService.FindAsync(id, cancellationToken);
        if (team is null) return NotFound();
        var model = new TeamMembersViewModel();
        await PopulateMembersAsync(model, team, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(long id, TeamMembersViewModel model, CancellationToken cancellationToken) {
        var team = await teamService.FindAsync(id, cancellationToken);
        if (team is null) return NotFound();
        if (ModelState.IsValid) {
            try {
                await teamService.AddMemberAsync(id, model.UserId!.Value, cancellationToken);
                TempData["SuccessMessage"] = "メンバーを追加しました。";
                return RedirectToAction(nameof(Members), new { id });
            } catch (DuplicateTeamMemberException exception) {
                ModelState.AddModelError(nameof(model.UserId), exception.Message);
            } catch (ArgumentException exception) when (exception.ParamName is "teamId" or "userId") {
                if (exception.ParamName == "teamId") return NotFound();
                ModelState.AddModelError(nameof(model.UserId), "選択した利用者が見つかりません。利用者を選択し直してください。");
            }
        }
        await PopulateMembersAsync(model, team, cancellationToken);
        return View(nameof(Members), model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(long id, long userId, CancellationToken cancellationToken) {
        if (!ModelState.IsValid || userId <= 0) return BadRequest();
        try {
            var removed = await teamService.RemoveMemberAsync(id, userId, cancellationToken);
            TempData["SuccessMessage"] = removed ? "メンバーから外しました。" : "この利用者は既にメンバーから外れています。";
            return RedirectToAction(nameof(Members), new { id });
        } catch (ArgumentException exception) when (exception.ParamName == "teamId") {
            return NotFound();
        }
    }

    private async Task PopulateMembersAsync(TeamMembersViewModel model, Team team, CancellationToken cancellationToken) {
        model.TeamId = team.Id;
        model.TeamName = team.Name;
        var members = await teamService.GetMembersAsync(team.Id, cancellationToken);
        model.Members = members.Select(member => new TeamMemberListItemViewModel(
            member.UserId, member.User.Name, member.User.Email, member.Role)).ToList();
        var memberIds = members.Select(member => member.UserId).ToHashSet();
        var users = await userService.GetAllAsync(cancellationToken);
        model.UserOptions = users.Where(user => !memberIds.Contains(user.Id))
            .Select(user => new SelectListItem($"{user.Name}（ID: {user.Id} / {user.TimeZoneId}）", user.Id.ToString())).ToList();
    }
}
