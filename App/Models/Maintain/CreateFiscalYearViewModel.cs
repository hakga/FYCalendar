using System.ComponentModel.DataAnnotations;
using App.Domain.Services;

namespace App.Models.Maintain;

public sealed class CreateFiscalYearViewModel {
    private string _name = string.Empty;

    [Display(Name = "年度")]
    [Required(ErrorMessage = "年度を入力してください。")]
    [Range(1, 9998, ErrorMessage = "年度は1から9998までを指定してください。")]
    public int? Year { get; set; }

    [Display(Name = "名称")]
    [Required(ErrorMessage = "名称を入力してください。")]
    [StringLength(50, ErrorMessage = "名称は50文字以内で入力してください。")]
    public string Name {
        get => _name;
        set => _name = (value ?? string.Empty).Trim();
    }

    public (DateOnly StartsOn, DateOnly EndsOn)? Period =>
        Year is >= 1 and <= 9998 ? FiscalYearCalculator.GetPeriod(Year.Value) : null;
}
