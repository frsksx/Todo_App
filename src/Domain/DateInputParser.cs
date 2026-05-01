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

        // Keyword shorthands resolved in local time, then converted to UTC
        var localNow = nowUtc.ToLocalTime();
        var lowerT = t.ToLowerInvariant();

        if (lowerT == "today")
            return localNow.Date.AddHours(9).ToUniversalTime();

        if (lowerT is "tomorrow" or "tomorrow 9" or "tmr" or "tmr 9")
            return localNow.Date.AddDays(1).AddHours(9).ToUniversalTime();

        if (lowerT.StartsWith("tomorrow ", StringComparison.OrdinalIgnoreCase))
        {
            var hourPart = lowerT["tomorrow ".Length..].Trim();
            if (int.TryParse(hourPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) && h is >= 0 and <= 23)
                return localNow.Date.AddDays(1).AddHours(h).ToUniversalTime();
        }

        if (lowerT is "this evening" or "evening")
            return localNow.Date.AddHours(18).ToUniversalTime();

        if (lowerT is "next workday" or "workday")
        {
            var daysAhead = localNow.DayOfWeek switch
            {
                DayOfWeek.Friday => 3,
                DayOfWeek.Saturday => 2,
                _ => 1,
            };
            return localNow.Date.AddDays(daysAhead).AddHours(9).ToUniversalTime();
        }

        var explicitFormats = new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(t, explicitFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed.ToUniversalTime();

        if (DateTime.TryParse(t, culture, DateTimeStyles.AssumeLocal, out parsed))
            return parsed.ToUniversalTime();

        return null;
    }
}
