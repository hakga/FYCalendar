namespace App.Models.Maintain;

public sealed class UserListItemViewModel {
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string TimeZoneId { get; init; } = string.Empty;
}
