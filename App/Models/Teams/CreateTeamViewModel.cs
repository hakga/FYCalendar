using System.ComponentModel.DataAnnotations;
using App.Domain.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Models.Teams;

public sealed class CreateTeamViewModel {
    private string _name = string.Empty;
    private string _timeZoneId = TeamService.DefaultTimeZoneId;

    [Display(Name = "チーム名")]
    [Required(ErrorMessage = "チーム名を入力してください。")]
    [StringLength(200, ErrorMessage = "チーム名は200文字以内で入力してください。")]
    public string Name {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    [Display(Name = "タイムゾーン")]
    [Required(ErrorMessage = "タイムゾーンを選択してください。")]
    [StringLength(64, ErrorMessage = "タイムゾーンは64文字以内で指定してください。")]
    public string TimeZoneId {
        get => _timeZoneId;
        set => _timeZoneId = (value ?? string.Empty).Trim();
    }

    [BindNever, ValidateNever]
    public IReadOnlyList<SelectListItem> TimeZoneOptions { get; set; } = [];
}
