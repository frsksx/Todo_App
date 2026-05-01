using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.TestSupport;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTime utcNow)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; private set; }

    public void Set(DateTime utcNow)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public void Advance(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }
}
