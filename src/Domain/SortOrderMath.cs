namespace WindowsTrayTasks.Domain;

public static class SortOrderMath
{
    public const double MinimumGap = 0.000001;

    public static double Between(double? previous, double? next)
    {
        if (previous.HasValue && next.HasValue) return (previous.Value + next.Value) / 2.0;
        if (previous.HasValue) return previous.Value + 1.0;
        if (next.HasValue) return next.Value - 1.0;
        return 1.0;
    }

    public static bool NeedsRenumber(double previous, double next)
        => Math.Abs(next - previous) < MinimumGap;
}
