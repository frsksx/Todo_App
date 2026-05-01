namespace WindowsTrayTasks.Domain;

public sealed record QuickPreset(string Label, string Shorthand);

public static class QuickPresets
{
    public static readonly IReadOnlyList<QuickPreset> All =
    [
        new("+10m",        "+10m"),
        new("+1h",         "+1h"),
        new("+4h",         "+4h"),
        new("Tomorrow 9",  "tomorrow 9"),
        new("Next workday","next workday"),
    ];
}
