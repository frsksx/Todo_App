using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class TaskRecurrenceTests
{
    [Theory]
    [InlineData("daily", "2026-05-02T10:00:00Z")]
    [InlineData("weekly", "2026-05-08T10:00:00Z")]
    [InlineData("biweekly", "2026-05-15T10:00:00Z")]
    [InlineData("monthly", "2026-06-01T10:00:00Z")]
    [InlineData("quarterly", "2026-08-01T10:00:00Z")]
    [InlineData("yearly", "2027-05-01T10:00:00Z")]
    public void NextUtc_AdvancesSupportedPreset(string recurrence, string expected)
    {
        var anchor = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

        var next = TaskRecurrence.NextUtc(anchor, recurrence);

        Assert.Equal(DateTime.Parse(expected).ToUniversalTime(), next);
    }

    [Theory]
    [InlineData("by-weekly")]
    [InlineData("bi-weekly")]
    public void Normalize_AcceptsBiweeklySpellings(string recurrence)
    {
        Assert.Equal("biweekly", TaskRecurrence.Normalize(recurrence));
    }
}
