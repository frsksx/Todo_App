using WindowsTrayTasks.Infrastructure.Persistence;

namespace WindowsTrayTasks.TestSupport;

public sealed class TempDatabase : IDisposable
{
    public TempDatabase(FakeClock? clock = null)
    {
        Clock = clock ?? new FakeClock(new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc));
        Database = Database.CreateTemp(Clock, new SequentialIdGenerator());
        Path = Database.GetTempPath() ?? throw new InvalidOperationException("Temp database path was not exposed.");
    }

    public FakeClock Clock { get; }
    public Database Database { get; }
    public string Path { get; }

    public void Dispose()
    {
        Database.Dispose();
    }
}
