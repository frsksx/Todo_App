using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class StateCycleTests
{
    [Fact]
    public void Move_ForwardCyclesThroughAllStates()
    {
        Assert.Equal(TaskState.Next, StateCycle.Move(TaskState.Action, 1));
        Assert.Equal(TaskState.OnHold, StateCycle.Move(TaskState.Next, 1));
        Assert.Equal(TaskState.Waiting, StateCycle.Move(TaskState.OnHold, 1));
        Assert.Equal(TaskState.Someday, StateCycle.Move(TaskState.Waiting, 1));
        Assert.Equal(TaskState.Done, StateCycle.Move(TaskState.Someday, 1));
        Assert.Equal(TaskState.Action, StateCycle.Move(TaskState.Done, 1));
        Assert.Equal(TaskState.Archived, StateCycle.Move(TaskState.Archived, 1));
    }

    [Fact]
    public void Move_BackwardWrapsFromActionToDone()
    {
        Assert.Equal(TaskState.Done, StateCycle.Move(TaskState.Action, -1));
    }
}
