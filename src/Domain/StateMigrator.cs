namespace WindowsTrayTasks.Domain;

public enum LegacyTaskState
{
    Inbox = 1,
    Next = 2,
    Waiting = 3,
    Scheduled = 4,
    Someday = 5,
    Done = 6,
    Archived = 7,
}

public sealed record StateMigrationResult(TaskState State, bool PreserveAsArchived);

public static class StateMigrator
{
    public static StateMigrationResult FromLegacy(int legacyState)
        => (LegacyTaskState)legacyState switch
        {
            LegacyTaskState.Inbox => new(TaskState.Action, PreserveAsArchived: false),
            LegacyTaskState.Next => new(TaskState.Next, PreserveAsArchived: false),
            LegacyTaskState.Waiting => new(TaskState.Waiting, PreserveAsArchived: false),
            LegacyTaskState.Scheduled => new(TaskState.Next, PreserveAsArchived: false),
            LegacyTaskState.Someday => new(TaskState.Someday, PreserveAsArchived: false),
            LegacyTaskState.Done => new(TaskState.Done, PreserveAsArchived: false),
            LegacyTaskState.Archived => new(TaskState.Done, PreserveAsArchived: true),
            _ => new(TaskState.Action, PreserveAsArchived: false),
        };
}
