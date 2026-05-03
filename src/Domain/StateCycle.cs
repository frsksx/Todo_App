namespace WindowsTrayTasks.Domain;

public static class StateCycle
{
    private static readonly TaskState[] _all =
    [
        TaskState.Action,
        TaskState.Next,
        TaskState.OnHold,
        TaskState.Waiting,
        TaskState.Someday,
        TaskState.Done,
    ];

    public static IReadOnlyList<TaskState> All => _all;

    public static TaskState Move(TaskState state, int delta)
    {
        var index = Array.IndexOf(_all, state);
        if (index < 0) return state;
        index = ((index + delta) % _all.Length + _all.Length) % _all.Length;
        return _all[index];
    }
}
