namespace WindowsTrayTasks.Domain;

public static class TaskTitleTags
{
    public static string AddTagToken(string title, string tagName)
    {
        var displayName = tagName.Trim().TrimStart('@');
        var normalized = TagExtractor.Normalize(displayName);
        if (normalized.Length == 0) return title;

        var alreadyTagged = TagExtractor.ExtractTags(title)
            .Any(t => TagExtractor.Normalize(t).Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (alreadyTagged) return title;

        var trimmedTitle = title.TrimEnd();
        var token = "@" + displayName;
        return trimmedTitle.Length == 0 ? token : $"{trimmedTitle} {token}";
    }
}
