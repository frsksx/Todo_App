namespace WindowsTrayTasks.Domain;

public sealed class EntityFactory
{
    private static readonly DateTime SortOrderEpochUtc = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    public EntityFactory(IClock clock, IIdGenerator ids)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _ids = ids ?? throw new ArgumentNullException(nameof(ids));
    }

    public Heading CreateHeading(string title, double? sortOrder = null)
        => CreateHeading(Guid.Empty, title, sortOrder);

    public Heading CreateHeading(Guid pageId, string title, double? sortOrder = null)
    {
        var now = _clock.UtcNow;
        return new Heading
        {
            Id = _ids.NewId(),
            PageId = pageId,
            Title = title,
            SortOrder = sortOrder ?? DefaultSortOrder(now),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public TaskItem CreateTask(string title, Guid? headingId = null, TaskState state = TaskState.Action, double? sortOrder = null)
        => CreateTask(Guid.Empty, title, headingId, state, sortOrder);

    public TaskItem CreateTask(Guid pageId, string title, Guid? headingId = null, TaskState state = TaskState.Action, double? sortOrder = null)
    {
        var now = _clock.UtcNow;
        return new TaskItem
        {
            Id = _ids.NewId(),
            PageId = pageId,
            HeadingId = headingId,
            Title = title,
            State = state,
            Priority = TaskPriority.Medium,
            SortOrder = sortOrder ?? DefaultSortOrder(now),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public Reminder CreateReminder(Guid taskId, DateTime fireAtUtc, int autoSnoozeIntervalMinutes = 5)
    {
        var now = _clock.UtcNow;
        return new Reminder
        {
            Id = _ids.NewId(),
            TaskId = taskId,
            FireAt = fireAtUtc,
            NextFireAt = fireAtUtc,
            Enabled = true,
            Status = ReminderStatus.Active,
            AutoSnoozeEnabled = true,
            AutoSnoozeIntervalMinutes = autoSnoozeIntervalMinutes,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public Page CreatePage(string name, bool isDefault = false, double? sortOrder = null)
    {
        var now = _clock.UtcNow;
        return new Page
        {
            Id = _ids.NewId(),
            Name = name,
            SortOrder = sortOrder ?? DefaultSortOrder(now),
            LastFilterView = "All",
            IsDefault = isDefault,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public Tag CreateTag(Guid pageId, string displayName, double? sortOrder = null)
    {
        var now = _clock.UtcNow;
        return new Tag
        {
            Id = _ids.NewId(),
            PageId = pageId,
            Name = TagExtractor.Normalize(displayName),
            DisplayName = displayName.Trim().TrimStart('@'),
            SortOrder = sortOrder ?? DefaultSortOrder(now),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public static double DefaultSortOrder(DateTime nowUtc)
        => (nowUtc.ToUniversalTime() - SortOrderEpochUtc).TotalSeconds;
}
