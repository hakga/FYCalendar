using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Models.Calendars;

public sealed class CreateTeamCalendarViewModel {
    private string _name = string.Empty;
    private string? _timeZoneId;
    private string? _displayColor;

    [Display(Name = "チーム")]
    [Required(ErrorMessage = "チームを選択してください。")]
    [Range(1, long.MaxValue, ErrorMessage = "チームを選択してください。")]
    public long? TeamId { get; set; }

    [Display(Name = "カレンダー名")]
    [Required(ErrorMessage = "カレンダー名を入力してください。")]
    [StringLength(200, ErrorMessage = "カレンダー名は200文字以内で入力してください。")]
    public string Name {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    [Display(Name = "タイムゾーン")]
    [StringLength(64, ErrorMessage = "タイムゾーンは64文字以内で指定してください。")]
    public string? TimeZoneId {
        get => _timeZoneId;
        set => _timeZoneId = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Display(Name = "表示色")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "表示色は#に続く6桁の16進数で入力してください。")]
    public string? DisplayColor {
        get => _displayColor;
        set => _displayColor = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [BindNever, ValidateNever]
    public IReadOnlyList<SelectListItem> TeamOptions { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<SelectListItem> TimeZoneOptions { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<string> NameOptions { get; set; } = [];
}
