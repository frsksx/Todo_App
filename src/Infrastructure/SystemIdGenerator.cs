using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Infrastructure;

public sealed class SystemIdGenerator : IIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}
