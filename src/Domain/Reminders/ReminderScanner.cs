namespace WindowsTrayTasks.Domain.Reminders;

public sealed record ScanResult(
    IReadOnlyList<Reminder> FiredReminders,
    DateTime? NextFireAtUtc,
    int OverdueCount,
    int ActiveCount
);

/// <summary>
/// Pure reminder scan: given the set of currently active reminders and a "now" instant, returns
/// the reminders that should fire and the earliest upcoming next-fire time. Mutates each fired
/// reminder's status / last-fired / next-fire fields per the auto-snooze policy. Persistence and
/// notification dispatch are the caller's job.
/// </summary>
public static class ReminderScanner
{
    public static ScanResult Scan(IReadOnlyList<Reminder> activeReminders, DateTime nowUtc)
    {
        if (activeReminders is null) throw new ArgumentNullException(nameof(activeReminders));

        var fired = new List<Reminder>();
        DateTime? next = null;
        var overdue = 0;

        foreach (var rem in activeReminders)
        {
            if (!rem.Enabled) continue;
            var fireAt = rem.NextFireAt ?? rem.FireAt;

            if (fireAt <= nowUtc)
            {
                if (!rem.AutoSnoozeEnabled && rem.LastFiredAt is not null)
                {
                    rem.Status = ReminderStatus.Overdue;
                    overdue++;
                    continue;
                }

                rem.Status = ReminderStatus.Overdue;
                rem.LastFiredAt = nowUtc;
                rem.UpdatedAt = nowUtc;
                if (rem.AutoSnoozeEnabled)
                {
                    rem.NextFireAt = nowUtc.AddMinutes(rem.AutoSnoozeIntervalMinutes);
                }
                fired.Add(rem);
                overdue++;
                if (rem.NextFireAt is { } nfa && (next is null || nfa < next))
                    next = nfa;
            }
            else if (next is null || fireAt < next)
            {
                next = fireAt;
            }
        }

        return new ScanResult(fired, next, overdue, activeReminders.Count(r => r.Enabled));
    }
}
