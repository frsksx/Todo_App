namespace WindowsTrayTasks.Domain;

public interface IClock
{
    DateTime UtcNow { get; }
}
