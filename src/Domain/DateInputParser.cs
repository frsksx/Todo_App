using System.Globalization;

namespace WindowsTrayTasks.Domain;

/// <summary>
/// Parses user-typed date/time strings into UTC <see cref="DateTime"/>s.
/// Supports the shorthand grammar <c>+Nm</c> / <c>+Nh</c> / <c>+Nd</c> relative to a "now" instant,
/// plus explicit <c>yyyy-MM-dd HH:mm</c> / <c>yyyy-MM-dd</c> forms and a final fallback to
/// <see cref="DateTime.TryParse(string, IFormatProvider?, DateTimeStyles, out DateTime)"/>.
/// </summary>
public static class DateInputParser
{
    public static DateTime? Parse(string? text, DateTime nowUtc)
        => Parse(text, nowUtc, CultureInfo.CurrentCulture);

    public static DateTime? Parse(string? text, DateTime nowUtc, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = text.Trim();

        if (t.Length >= 2 && t[0] == '+')
        {
            var unit = t[^1];
            if (int.TryParse(t.AsSpan(1, t.Length - 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            {
                return unit switch
                {
                    'm' or 'M' => nowUtc.AddMinutes(n),
                    'h' or 'H' => nowUtc.AddHours(n),
                    'd' or 'D' => nowUtc.AddDays(n),
                    _ => null,
                };
            }
            return null;
        }

        var explicitFormats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(t, explicitFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed.ToUniversalTime();

        if (DateTime.TryParse(t, culture, DateTimeStyles.AssumeLocal, out parsed))
            return parsed.ToUniversalTime();

        return null;
    }
}
