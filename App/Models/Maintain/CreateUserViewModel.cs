using System.ComponentModel.DataAnnotations;
using App.Domain.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Models.Maintain;

public sealed class CreateUserViewModel {
    private string _name = string.Empty;
    private string? _email;
    private string _timeZoneId = UserService.DefaultTimeZoneId;

    [Display(Name = "氏名")]
    [Required(ErrorMessage = "氏名を入力してください。")]
    [StringLength(100, ErrorMessage = "氏名は100文字以内で入力してください。")]
    public string Name {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    [Display(Name = "メールアドレス")]
    [EmailAddress(ErrorMessage = "正しいメールアドレスを入力してください。")]
    [StringLength(255, ErrorMessage = "メールアドレスは255文字以内で入力してください。")]
    public string? Email {
        get => _email;
        set => _email = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [Display(Name = "タイムゾーン")]
    [Required(ErrorMessage = "タイムゾーンを選択してください。")]
    [StringLength(64, ErrorMessage = "タイムゾーンは64文字以内で指定してください。")]
    public string TimeZoneId {
        get => _timeZoneId;
        set => _timeZoneId = (value ?? string.Empty).Trim();
    }

    [BindNever]
    [ValidateNever]
    public IReadOnlyList<SelectListItem> TimeZoneOptions { get; set; } = [];
}
