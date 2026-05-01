namespace WindowsTrayTasks.Domain;

public enum DateFilterBucket
{
    All,
    Today,
    Tomorrow,
    ThisWeek,
    Upcoming,
    Overdue,
    NoDate,
}

public sealed record FilterCriteria(
    string Mode,
    string SearchText,
    DateTime NowUtc
)
{
    public static FilterCriteria Default(DateTime nowUtc) => new("All", string.Empty, nowUtc);
}

public sealed record ComposedFilterCriteria(
    Guid? PageId,
    string StateMode,
    bool IncludeDone,
    DateFilterBucket DateBucket,
    string SearchText,
    IReadOnlySet<string> TagNames,
    bool UntaggedOnly,
    Guid? FocusedHeadingId,
    DateTime NowUtc,
    bool InboxOnly = false
)
{
    public static ComposedFilterCriteria Default(DateTime nowUtc, Guid? pageId = null)
        => new(pageId, "All", false, DateFilterBucket.All, string.Empty, new HashSet<string>(StringComparer.OrdinalIgnoreCase), false, null, nowUtc);
}

public enum PerspectiveKind { Normal, Forecast, Inbox, Available, Waiting, Someday, Completed, Review }

public sealed record Perspective(string Name, PerspectiveKind Kind, ComposedFilterCriteria? Criteria = null)
{
    public static readonly IReadOnlyList<Perspective> BuiltIns = BuildBuiltIns();

    private static IReadOnlyList<Perspective> BuildBuiltIns()
    {
        var emptyTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var epoch = DateTime.UtcNow;
        return
        [
            new("Inbox",     PerspectiveKind.Inbox,     new(null, "Actions + Next", false, DateFilterBucket.All, "", emptyTags, false, null, epoch, InboxOnly: true)),
            new("Forecast",  PerspectiveKind.Forecast,  null),
            new("Available", PerspectiveKind.Available, new(null, "Actions + Next", false, DateFilterBucket.All, "", emptyTags, false, null, epoch)),
            new("Waiting",   PerspectiveKind.Waiting,   new(null, "Only Waiting For", false, DateFilterBucket.All, "", emptyTags, false, null, epoch)),
            new("Someday",   PerspectiveKind.Someday,   new(null, "Only Someday/Maybe", false, DateFilterBucket.All, "", emptyTags, false, null, epoch)),
            new("Completed", PerspectiveKind.Completed, new(null, "Only Completed", true, DateFilterBucket.All, "", emptyTags, false, null, epoch)),
            new("Review",    PerspectiveKind.Review,    new(null, "Show All", false, DateFilterBucket.All, "", emptyTags, false, null, epoch)),
        ];
    }
}

public static class TaskFilter
{
    public static IEnumerable<TaskItem> Apply(
        IEnumerable<TaskItem> tasks,
        IReadOnlyDictionary<Guid, Reminder> activeReminders,
        FilterCriteria criteria)
    {
        var stateMode = criteria.Mode is "Today" or "Upcoming" or "Overdue" ? "All" : criteria.Mode;
        var dateBucket = criteria.Mode switch
        {
            "Today" => DateFilterBucket.Today,
            "Upcoming" => DateFilterBucket.Upcoming,
            "Overdue" => DateFilterBucket.Overdue,
            _ => DateFilterBucket.All,
        };
        return Apply(tasks, activeReminders, new ComposedFilterCriteria(
            PageId: null,
            StateMode: stateMode,
            IncludeDone: true,
            DateBucket: dateBucket,
            SearchText: criteria.SearchText,
            TagNames: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            UntaggedOnly: false,
            FocusedHeadingId: null,
            NowUtc: criteria.NowUtc));
    }

    public static IEnumerable<TaskItem> Apply(
        IEnumerable<TaskItem> tasks,
        IReadOnlyDictionary<Guid, Reminder> activeReminders,
        ComposedFilterCriteria criteria)
    {
        if (tasks is null) throw new ArgumentNullException(nameof(tasks));
        activeReminders ??= new Dictionary<Guid, Reminder>();

        var today = criteria.NowUtc.ToLocalTime().Date;
        IEnumerable<TaskItem> filtered = tasks;

        if (criteria.PageId is { } pageId)
        {
            filtered = filtered.Where(t => t.PageId == pageId);
        }

        if (criteria.InboxOnly)
        {
            filtered = filtered.Where(t => t.HeadingId is null);
        }

        if (criteria.FocusedHeadingId is { } focused)
        {
            filtered = filtered.Where(t => (t.HeadingId ?? Guid.Empty) == focused);
        }

        filtered = criteria.StateMode switch
        {
            "Action" => filtered.Where(t => t.State == TaskState.Action),
            "Next" => filtered.Where(t => t.State == TaskState.Next),
            "OnHold" => filtered.Where(t => t.State == TaskState.OnHold),
            "Waiting" => filtered.Where(t => t.State == TaskState.Waiting),
            "Someday" => filtered.Where(t => t.State == TaskState.Someday),
            "Done" => filtered.Where(t => t.State == TaskState.Done),
            "Only Next" => filtered.Where(t => t.State == TaskState.Next),
            "Actions + Next" => filtered.Where(t => t.State is TaskState.Action or TaskState.Next),
            "All except Done" => filtered.Where(t => t.State != TaskState.Done),
            "Show All" => filtered,
            "Only On Hold" => filtered.Where(t => t.State == TaskState.OnHold),
            "Only Waiting For" => filtered.Where(t => t.State == TaskState.Waiting),
            "Only Someday/Maybe" => filtered.Where(t => t.State == TaskState.Someday),
            "Only Completed" => filtered.Where(t => t.State == TaskState.Done),
            "Archived" => filtered.Where(t => t.ArchivedAt is not null),
            _ => filtered,
        };

        if (!criteria.IncludeDone && criteria.StateMode is not "Done" and not "Archived")
        {
            filtered = filtered.Where(t => t.State != TaskState.Done);
        }

        filtered = criteria.DateBucket switch
        {
            DateFilterBucket.Today => filtered.Where(t => MatchesLocalDate(t, activeReminders, today)),
            DateFilterBucket.Tomorrow => filtered.Where(t => MatchesLocalDate(t, activeReminders, today.AddDays(1))),
            DateFilterBucket.ThisWeek => filtered.Where(t => MatchesDateRange(t, activeReminders, today, today.AddDays(7))),
            DateFilterBucket.Upcoming => filtered.Where(t => IsUpcoming(t, activeReminders, criteria.NowUtc)),
            DateFilterBucket.Overdue => filtered.Where(t => IsOverdue(t, activeReminders, criteria.NowUtc)),
            DateFilterBucket.NoDate => filtered.Where(t => t.StartAt is null && t.DueAt is null && !HasReminderDate(t, activeReminders)),
            _ => filtered,
        };

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var q = criteria.SearchText.Trim();
            filtered = filtered.Where(t =>
                t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (t.Notes != null && t.Notes.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        if (criteria.UntaggedOnly)
        {
            filtered = filtered.Where(t => t.Tags.Count == 0);
        }
        else if (criteria.TagNames.Count > 0)
        {
            filtered = filtered.Where(t => t.Tags.Any(tag => criteria.TagNames.Contains(tag.Name)));
        }

        return filtered;
    }

    private static bool MatchesLocalDate(TaskItem task, IReadOnlyDictionary<Guid, Reminder> reminders, DateTime localDate)
        => (task.StartAt.HasValue && task.StartAt.Value.ToLocalTime().Date == localDate)
           || (task.DueAt.HasValue && task.DueAt.Value.ToLocalTime().Date == localDate)
           || (reminders.TryGetValue(task.Id, out var rem)
               && rem.NextFireAt.HasValue
               && rem.NextFireAt.Value.ToLocalTime().Date == localDate);

    private static bool MatchesDateRange(TaskItem task, IReadOnlyDictionary<Guid, Reminder> reminders, DateTime startLocalInclusive, DateTime endLocalExclusive)
        => IsInLocalRange(task.StartAt, startLocalInclusive, endLocalExclusive)
           || IsInLocalRange(task.DueAt, startLocalInclusive, endLocalExclusive)
           || (reminders.TryGetValue(task.Id, out var rem) && IsInLocalRange(rem.NextFireAt, startLocalInclusive, endLocalExclusive));

    private static bool IsUpcoming(TaskItem task, IReadOnlyDictionary<Guid, Reminder> reminders, DateTime nowUtc)
        => (task.StartAt.HasValue && task.StartAt.Value > nowUtc)
           || (task.DueAt.HasValue && task.DueAt.Value > nowUtc)
           || (reminders.TryGetValue(task.Id, out var rem) && rem.NextFireAt.HasValue && rem.NextFireAt.Value > nowUtc);

    private static bool IsOverdue(TaskItem task, IReadOnlyDictionary<Guid, Reminder> reminders, DateTime nowUtc)
        => (task.DueAt.HasValue && task.DueAt.Value < nowUtc)
           || (reminders.TryGetValue(task.Id, out var rem) && rem.NextFireAt.HasValue && rem.NextFireAt.Value <= nowUtc);

    private static bool HasReminderDate(TaskItem task, IReadOnlyDictionary<Guid, Reminder> reminders)
        => reminders.TryGetValue(task.Id, out var rem) && rem.NextFireAt.HasValue;

    private static bool IsInLocalRange(DateTime? utc, DateTime startLocalInclusive, DateTime endLocalExclusive)
    {
        if (!utc.HasValue) return false;
        var local = utc.Value.ToLocalTime();
        return local >= startLocalInclusive && local < endLocalExclusive;
    }
}
