namespace App.Models.Maintain;

public sealed class FiscalYearListItemViewModel {
    public int Year { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartsOn { get; init; }
    public DateOnly EndsOn { get; init; }
    public string Status { get; init; } = string.Empty;

    public string StatusLabel => Status switch {
        "preparing" => "準備中",
        "open" => "運用中",
        "closed" => "終了",
        _ => Status
    };
}
