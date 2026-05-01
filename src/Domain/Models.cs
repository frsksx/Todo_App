namespace WindowsTrayTasks.Domain;

public enum TaskState
{
    Action = 1,
    Next = 2,
    OnHold = 3,
    Waiting = 4,
    Someday = 5,
    Done = 6,
}

public enum ReminderStatus
{
    Active = 0,
    Snoozed = 1,
    Overdue = 2,
    Acknowledged = 3,
    Disabled = 4,
}

public sealed class Page
{
    public Guid Id { get; init; }
    public string Name { get; set; } = "";
    public double SortOrder { get; set; }
    public string LastFilterView { get; set; } = "All";
    public Guid? LastFocusedHeadingId { get; set; }
    public string? LastSearchText { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public sealed class Heading
{
    public Guid Id { get; init; }
    public Guid PageId { get; set; }
    public string Title { get; set; } = "";
    public double SortOrder { get; set; }
    public bool Collapsed { get; set; }
    public int ReviewIntervalDays { get; set; } = 7;
    public DateTime? LastReviewedAt { get; set; }
    public DateTime? NextReviewAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public sealed class TaskItem
{
    public Guid Id { get; init; }
    public Guid PageId { get; set; }
    public Guid? HeadingId { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public TaskState State { get; set; } = TaskState.Action;
    public double SortOrder { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<Tag> Tags { get; set; } = new();
}

public sealed class Reminder
{
    public Guid Id { get; init; }
    public Guid TaskId { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime FireAt { get; set; }
    public DateTime? NextFireAt { get; set; }
    public DateTime? LastFiredAt { get; set; }
    public DateTime? LastAcknowledgedAt { get; set; }
    public bool AutoSnoozeEnabled { get; set; } = true;
    public int AutoSnoozeIntervalMinutes { get; set; } = 5;
    public ReminderStatus Status { get; set; } = ReminderStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class Tag
{
    public Guid Id { get; init; }
    public Guid PageId { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public double SortOrder { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
