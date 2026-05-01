using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
