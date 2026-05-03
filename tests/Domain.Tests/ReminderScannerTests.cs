using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Domain.Reminders;
using WindowsTrayTasks.TestSupport;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class ReminderScannerTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Scan_PastNextFireAt_MarksOverdueAndAdvances()
    {
        var reminder = TestEntities.Reminder(nextFireAt: Now.AddMinutes(-1), intervalMinutes: 10);

        var result = ReminderScanner.Scan([reminder], Now);

        Assert.Single(result.FiredReminders);
        Assert.Equal(ReminderStatus.Overdue, reminder.Status);
        Assert.Equal(Now, reminder.LastFiredAt);
        Assert.Equal(Now.AddMinutes(10), reminder.NextFireAt);
        Assert.Equal(Now.AddMinutes(10), result.NextFireAtUtc);
    }

    [Fact]
    public void Scan_FutureNextFireAt_DoesNotFire()
    {
        var reminder = TestEntities.Reminder(nextFireAt: Now.AddMinutes(30));

        var result = ReminderScanner.Scan([reminder], Now);

        Assert.Empty(result.FiredReminders);
        Assert.Equal(Now.AddMinutes(30), result.NextFireAtUtc);
        Assert.Equal(0, result.OverdueCount);
    }

    [Fact]
    public void Scan_MultiplePastReminders_CoalescesIntoOneResultList()
    {
        var first = TestEntities.Reminder(
            id: Guid.Parse("00000000-0000-0000-0000-000000000101"),
            nextFireAt: Now.AddMinutes(-10));
        var second = TestEntities.Reminder(
            id: Guid.Parse("00000000-0000-0000-0000-000000000102"),
            taskId: Guid.Parse("00000000-0000-0000-0000-000000000002"),
            nextFireAt: Now.AddMinutes(-5));

        var result = ReminderScanner.Scan([first, second], Now);

        Assert.Equal(2, result.FiredReminders.Count);
        Assert.Equal(2, result.OverdueCount);
    }

    [Fact]
    public void Scan_DisabledReminder_DoesNotFire()
    {
        var reminder = TestEntities.Reminder(nextFireAt: Now.AddMinutes(-1), enabled: false);

        var result = ReminderScanner.Scan([reminder], Now);

        Assert.Empty(result.FiredReminders);
        Assert.Equal(0, result.ActiveCount);
    }

    [Fact]
    public void Scan_OneShotReminder_DoesNotFireAgainAfterFirstOverdueNotification()
    {
        var reminder = TestEntities.Reminder(
            nextFireAt: Now.AddMinutes(-1),
            autoSnooze: false);

        var first = ReminderScanner.Scan([reminder], Now);
        var second = ReminderScanner.Scan([reminder], Now.AddSeconds(20));

        Assert.Single(first.FiredReminders);
        Assert.Empty(second.FiredReminders);
        Assert.Equal(1, second.OverdueCount);
        Assert.Equal(ReminderStatus.Overdue, reminder.Status);
    }
}
