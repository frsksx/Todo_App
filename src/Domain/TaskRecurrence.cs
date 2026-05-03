namespace WindowsTrayTasks.Domain;

public static class TaskRecurrence
{
    public static readonly IReadOnlyList<string> Presets =
    [
        "daily",
        "weekly",
        "biweekly",
        "monthly",
        "quarterly",
        "yearly",
    ];

    public static bool IsSupported(string? recurrence)
        => Normalize(recurrence) is not null;

    public static string? Normalize(string? recurrence)
    {
        var value = recurrence?.Trim().ToLowerInvariant();
        return value switch
        {
            "daily" => "daily",
            "weekly" => "weekly",
            "biweekly" or "by-weekly" or "bi-weekly" => "biweekly",
            "monthly" => "monthly",
            "quarterly" => "quarterly",
            "yearly" or "annually" => "yearly",
            _ => null,
        };
    }

    public static DateTime? NextUtc(DateTime? anchorUtc, string? recurrence)
    {
        if (anchorUtc is null) return null;
        return NextUtc(anchorUtc.Value, recurrence);
    }

    public static DateTime? NextUtc(DateTime anchorUtc, string? recurrence)
        => Normalize(recurrence) switch
        {
            "daily" => anchorUtc.AddDays(1),
            "weekly" => anchorUtc.AddDays(7),
            "biweekly" => anchorUtc.AddDays(14),
            "monthly" => anchorUtc.AddMonths(1),
            "quarterly" => anchorUtc.AddMonths(3),
            "yearly" => anchorUtc.AddYears(1),
            _ => null,
        };
}
