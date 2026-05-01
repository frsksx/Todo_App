using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.TestSupport;

public static class TestEntities
{
    public static EntityFactory Factory(FakeClock? clock = null, SequentialIdGenerator? ids = null)
        => new(clock ?? new FakeClock(new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc)), ids ?? new SequentialIdGenerator());

    public static TaskItem Task(
        Guid? id = null,
        string title = "Task",
        TaskState state = TaskState.Action,
        Guid? headingId = null,
        DateTime? dueAt = null,
        string? notes = null)
        => new()
        {
            Id = id ?? Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Title = title,
            State = state,
            HeadingId = headingId,
            DueAt = dueAt,
            Notes = notes,
            SortOrder = 1,
            CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        };

    public static Reminder Reminder(
        Guid? id = null,
        Guid? taskId = null,
        DateTime? fireAt = null,
        DateTime? nextFireAt = null,
        bool enabled = true,
        ReminderStatus status = ReminderStatus.Active,
        bool autoSnooze = true,
        int intervalMinutes = 5)
        => new()
        {
            Id = id ?? Guid.Parse("00000000-0000-0000-0000-000000000101"),
            TaskId = taskId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"),
            FireAt = fireAt ?? new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            NextFireAt = nextFireAt,
            Enabled = enabled,
            Status = status,
            AutoSnoozeEnabled = autoSnooze,
            AutoSnoozeIntervalMinutes = intervalMinutes,
            CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        };
}
