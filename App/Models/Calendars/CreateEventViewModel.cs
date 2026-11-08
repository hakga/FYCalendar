using System.ComponentModel.DataAnnotations;
using App.Domain.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Models.Calendars;

public sealed class CreateEventViewModel : IValidatableObject {
    private string _title = string.Empty;

    [Display(Name = "個人カレンダー")]
    [Required(ErrorMessage = "個人カレンダーを選択してください。")]
    [Range(1, long.MaxValue, ErrorMessage = "個人カレンダーを選択してください。")]
    public long? UserCalendarId { get; set; }

    [Display(Name = "件名")]
    [Required(ErrorMessage = "件名を入力してください。")]
    [StringLength(300, ErrorMessage = "件名は300文字以内で入力してください。")]
    public string Title {
        get => _title;
        set => _title = (value ?? string.Empty).Trim();
    }

    [Display(Name = "開始日時")]
    [Required(ErrorMessage = "開始日時を入力してください。")]
    public DateTime? StartsAtLocal { get; set; }

    [Display(Name = "終了日時")]
    public DateTime? EndsAtLocal { get; set; }

    [Display(Name = "説明")]
    public string? Description { get; set; }

    [Display(Name = "重複を確認して登録する")]
    public bool ConfirmOverlap { get; set; }

    [BindNever, ValidateNever]
    public IReadOnlyList<SelectListItem> CalendarOptions { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<EventConflict> Conflicts { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (StartsAtLocal is { } start && start.Ticks % TimeSpan.TicksPerMinute != 0) {
            yield return new ValidationResult("日時は秒を含めず、分単位で入力してください。", [nameof(StartsAtLocal)]);
        }
        if (EndsAtLocal is { } end) {
            if (end.Ticks % TimeSpan.TicksPerMinute != 0) {
                yield return new ValidationResult("日時は秒を含めず、分単位で入力してください。", [nameof(EndsAtLocal)]);
            }
            if (StartsAtLocal is { } begins && end <= begins) {
                yield return new ValidationResult("終了日時は開始日時より後にしてください。", [nameof(EndsAtLocal)]);
            }
        }
    }
}
