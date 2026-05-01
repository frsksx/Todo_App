using System.Text.RegularExpressions;

namespace WindowsTrayTasks.Domain;

public static partial class TagExtractor
{
    public static IReadOnlyList<string> ExtractTags(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return [];

        var tags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in TagPattern().Matches(title))
        {
            var token = match.Groups["tag"].Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}');
            if (token.Length == 0) continue;
            if (seen.Add(token)) tags.Add(token);
        }
        return tags;
    }

    public static string Normalize(string tag)
        => tag.Trim().TrimStart('@').ToLowerInvariant();

    [GeneratedRegex(@"(?<![\w.])@(?<tag>[\p{L}\p{N}_][\p{L}\p{N}_\-.]*)", RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();
}
