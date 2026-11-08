using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Models.Teams;

public sealed class TeamMembersViewModel {
    [Display(Name = "追加する利用者")]
    [Required(ErrorMessage = "利用者を選択してください。")]
    [Range(1, long.MaxValue, ErrorMessage = "利用者を選択してください。")]
    public long? UserId { get; set; }

    [BindNever, ValidateNever]
    public long TeamId { get; set; }

    [BindNever, ValidateNever]
    public string TeamName { get; set; } = string.Empty;

    [BindNever, ValidateNever]
    public IReadOnlyList<TeamMemberListItemViewModel> Members { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<SelectListItem> UserOptions { get; set; } = [];
}

public sealed record TeamMemberListItemViewModel(long UserId, string Name, string? Email, string Role) {
    public string RoleLabel => Role == "member" ? "メンバー" : Role;
}
