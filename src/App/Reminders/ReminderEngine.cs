using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Domain.Reminders;
using WindowsTrayTasks.Infrastructure.Persistence;

namespace WindowsTrayTasks.Reminders;

/// <summary>
/// UI-side wrapper around the pure <see cref="ReminderScanner"/>. Owns the dispatcher timer,
/// reads/writes through <see cref="Database"/>, and fans out events to the tray and main window.
/// All scan logic lives in <see cref="ReminderScanner"/> for unit-testability.
/// </summary>
public sealed class ReminderEngine : IDisposable
{
    private readonly Database _db;
    private readonly IClock _clock;
    private readonly DispatcherTimer _timer;
    private readonly Dispatcher _dispatcher;
    private bool _paused;

    public event Action<List<Reminder>>? RemindersFired;
    public event Action? StateChanged;

    public bool IsPaused => _paused;

    public ReminderEngine(Database db, IClock clock, Dispatcher dispatcher)
    {
        _db = db;
        _clock = clock;
        _dispatcher = dispatcher;
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromSeconds(15),
        };
        _timer.Tick += (_, _) => Scan();
    }

    public void Start()
    {
        Scan();
        _timer.Start();
    }

    public void TogglePause()
    {
        _paused = !_paused;
        StateChanged?.Invoke();
    }

    public void SnoozeAll(int minutes)
    {
        var now = _clock.UtcNow;
        var reminders = _db.GetActiveReminders();
        foreach (var r in reminders)
        {
            r.NextFireAt = now.AddMinutes(minutes);
            r.Status = ReminderStatus.Snoozed;
            _db.SaveReminder(r);
        }
        StateChanged?.Invoke();
    }

    public void Acknowledge(Guid taskId) => Execute(taskId, ReminderActions.Complete);

    public void Snooze(Guid taskId, int minutes) =>
        Execute(taskId, ReminderActions.SnoozeFor(TimeSpan.FromMinutes(minutes)));

    public void Execute(Guid taskId, ReminderAction action)
    {
        var rem = _db.GetReminderForTask(taskId);
        if (rem is null) return;
        var now = _clock.UtcNow;
        switch (action.Kind)
        {
            case ReminderActionKind.Complete:
                rem.Status = ReminderStatus.Acknowledged;
                rem.LastAcknowledgedAt = now;
                rem.Enabled = false;
                break;
            case ReminderActionKind.Snooze when action.SnoozeDuration is { } dur:
                rem.NextFireAt = now + dur;
                rem.Status = ReminderStatus.Snoozed;
                break;
            case ReminderActionKind.Reschedule when action.RescheduleAt is { } at:
                rem.FireAt = at;
                rem.NextFireAt = at;
                rem.Status = ReminderStatus.Active;
                break;
            case ReminderActionKind.Disable:
                rem.Status = ReminderStatus.Disabled;
                rem.Enabled = false;
                break;
        }
        _db.SaveReminder(rem);
        StateChanged?.Invoke();
    }

    public ReminderSnapshot Snapshot()
    {
        var now = _clock.UtcNow;
        var reminders = _db.GetActiveReminders();
        var overdue = reminders.Count(r => r.NextFireAt is not null && r.NextFireAt <= now);
        var upcoming = reminders
            .Where(r => r.NextFireAt is not null && r.NextFireAt > now)
            .OrderBy(r => r.NextFireAt)
            .FirstOrDefault();
        return new ReminderSnapshot(overdue, reminders.Count, upcoming?.NextFireAt);
    }

    private void Scan()
    {
        if (_paused) return;
        var now = _clock.UtcNow;
        var active = _db.GetActiveReminders();
        var result = ReminderScanner.Scan(active, now);

        foreach (var rem in result.FiredReminders)
        {
            _db.SaveReminder(rem);
        }

        if (result.FiredReminders.Count > 0)
        {
            var fired = result.FiredReminders.ToList();
            _dispatcher.BeginInvoke(() => RemindersFired?.Invoke(fired));
        }

        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}

public readonly record struct ReminderSnapshot(int OverdueCount, int ActiveCount, DateTime? NextFireAtUtc);
