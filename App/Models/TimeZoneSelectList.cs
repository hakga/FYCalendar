using App.Domain.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Models;

internal static class TimeZoneSelectList {
    public static IReadOnlyList<SelectListItem> Create() =>
        TimeZoneInfo.GetSystemTimeZones()
            .Select(zone => TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId) ? ianaId : zone.Id)
            .Append(UserService.DefaultTimeZoneId)
            .Append("UTC")
            .Distinct(StringComparer.Ordinal)
            .Select(TimeZoneInfo.FindSystemTimeZoneById)
            .OrderBy(zone => zone.BaseUtcOffset)
            .ThenBy(zone => zone.Id, StringComparer.Ordinal)
            .Select(zone => new SelectListItem($"{zone.DisplayName} ({zone.Id})", zone.Id))
            .ToList();
}
