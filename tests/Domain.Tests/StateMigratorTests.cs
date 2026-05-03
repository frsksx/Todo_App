using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class StateMigratorTests
{
    [Theory]
    [InlineData((int)LegacyTaskState.Inbox, TaskState.Action, false)]
    [InlineData((int)LegacyTaskState.Next, TaskState.Next, false)]
    [InlineData((int)LegacyTaskState.Waiting, TaskState.Waiting, false)]
    [InlineData((int)LegacyTaskState.Scheduled, TaskState.Next, false)]
    [InlineData((int)LegacyTaskState.Someday, TaskState.Someday, false)]
    [InlineData((int)LegacyTaskState.Done, TaskState.Done, false)]
    [InlineData((int)LegacyTaskState.Archived, TaskState.Archived, true)]
    [InlineData(99, TaskState.Action, false)]
    public void FromLegacy_MapsOldStatesToCurrentModel(int legacyState, TaskState expectedState, bool expectedArchived)
    {
        var result = StateMigrator.FromLegacy(legacyState);

        Assert.Equal(expectedState, result.State);
        Assert.Equal(expectedArchived, result.PreserveAsArchived);
    }
}
