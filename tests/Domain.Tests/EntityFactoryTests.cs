using WindowsTrayTasks.Domain;
using WindowsTrayTasks.TestSupport;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class EntityFactoryTests
{
    [Fact]
    public void CreateTask_UsesInjectedClockAndIds()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc));
        var factory = new EntityFactory(clock, new SequentialIdGenerator());

        var task = factory.CreateTask("Check test seam", state: TaskState.Next);

        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), task.Id);
        Assert.Equal(clock.UtcNow, task.CreatedAt);
        Assert.Equal(clock.UtcNow, task.UpdatedAt);
        Assert.Equal(TaskState.Next, task.State);
    }

    [Fact]
    public void SequentialIdGenerator_NewId_ReturnsStableIncreasingIds()
    {
        var ids = new SequentialIdGenerator();

        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), ids.NewId());
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), ids.NewId());
    }

    [Fact]
    public void FakeClock_AdvanceAndSet_ControlsUtcNow()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc));

        clock.Advance(TimeSpan.FromMinutes(15));
        Assert.Equal(new DateTime(2026, 5, 1, 10, 15, 0, DateTimeKind.Utc), clock.UtcNow);

        clock.Set(new DateTime(2026, 5, 2, 8, 30, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 5, 2, 8, 30, 0, DateTimeKind.Utc), clock.UtcNow);
    }
}
