using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.TestSupport;

public sealed class SequentialIdGenerator : IIdGenerator
{
    private long _counter;

    public SequentialIdGenerator(long start = 0)
    {
        _counter = start;
    }

    public Guid NewId()
    {
        var n = Interlocked.Increment(ref _counter);
        return Guid.Parse($"00000000-0000-0000-0000-{n:000000000000}");
    }
}
