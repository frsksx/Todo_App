namespace WindowsTrayTasks.Domain;

public enum ReminderActionKind
{
    Complete,
    Snooze,
    Reschedule,
    Open,
    Disable,
}

public sealed record ReminderAction(
    ReminderActionKind Kind,
    TimeSpan? SnoozeDuration = null,
    DateTime? RescheduleAt = null);

public static class ReminderActions
{
    public static readonly ReminderAction Complete = new(ReminderActionKind.Complete);
    public static readonly ReminderAction Open = new(ReminderActionKind.Open);
    public static readonly ReminderAction Disable = new(ReminderActionKind.Disable);
    public static readonly ReminderAction Snooze10m = new(ReminderActionKind.Snooze, TimeSpan.FromMinutes(10));
    public static readonly ReminderAction Snooze1h = new(ReminderActionKind.Snooze, TimeSpan.FromHours(1));

    public static ReminderAction SnoozeFor(TimeSpan duration) =>
        new(ReminderActionKind.Snooze, SnoozeDuration: duration);

    public static ReminderAction RescheduleTo(DateTime utc) =>
        new(ReminderActionKind.Reschedule, RescheduleAt: utc);
}
