using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class TaskBoardMovesTests
{
    private static readonly Guid WorkPage = Guid.Parse("00000000-0000-0000-0000-000000000101");
    private static readonly Guid HomePage = Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid SourceHeadingId = Guid.Parse("00000000-0000-0000-0000-000000000201");
    private static readonly Guid TargetHeadingId = Guid.Parse("00000000-0000-0000-0000-000000000202");
    private static readonly Guid NextHeadingId = Guid.Parse("00000000-0000-0000-0000-000000000203");

    [Fact]
    public void MoveHeadingNearHeading_CrossPage_MovesHeadingAndItsTasks()
    {
        var source = Heading(SourceHeadingId, WorkPage, "Source", 10);
        var target = Heading(TargetHeadingId, HomePage, "Target", 20);
        var next = Heading(NextHeadingId, HomePage, "Next", 30);
        var child = Task("Child", WorkPage, SourceHeadingId, 1);
        var archivedChild = Task("Archived child", WorkPage, SourceHeadingId, 2);
        archivedChild.ArchivedAt = DateTime.UtcNow;
        var unrelated = Task("Other", WorkPage, null, 3);

        var changed = TaskBoardMoves.MoveHeadingNearHeading(
            source,
            target,
            after: true,
            [source, target, next],
            [child, archivedChild, unrelated],
            out var movedTasks);

        Assert.True(changed);
        Assert.Equal(HomePage, source.PageId);
        Assert.Equal(25, source.SortOrder);
        Assert.Equal([child, archivedChild], movedTasks);
        Assert.All(movedTasks, t => Assert.Equal(HomePage, t.PageId));
        Assert.Equal(WorkPage, unrelated.PageId);
    }

    [Fact]
    public void MoveHeadingNearHeading_SameHeading_IsNoOp()
    {
        var source = Heading(SourceHeadingId, WorkPage, "Source", 10);

        var changed = TaskBoardMoves.MoveHeadingNearHeading(
            source,
            source,
            after: true,
            [source],
            [],
            out var movedTasks);

        Assert.False(changed);
        Assert.Empty(movedTasks);
        Assert.Equal(WorkPage, source.PageId);
        Assert.Equal(10, source.SortOrder);
    }

    [Fact]
    public void MoveHeadingToPage_PutsHeadingAtEndAndMovesChildren()
    {
        var source = Heading(SourceHeadingId, WorkPage, "Source", 1);
        var target = Heading(TargetHeadingId, HomePage, "Target", 20);
        var child = Task("Child", WorkPage, SourceHeadingId, 1);

        TaskBoardMoves.MoveHeadingToPage(
            source,
            HomePage,
            [target],
            [child],
            out var movedTasks);

        Assert.Equal(HomePage, source.PageId);
        Assert.Equal(21, source.SortOrder);
        Assert.Equal([child], movedTasks);
        Assert.Equal(HomePage, child.PageId);
    }

    [Fact]
    public void MoveInboxToPage_AppendsNoHeadingTasksInStableOrder()
    {
        var existing = Task("Existing", HomePage, null, 5);
        var second = Task("Second", WorkPage, null, 2);
        var first = Task("First", WorkPage, null, 1);
        var headed = Task("Headed", WorkPage, SourceHeadingId, 3);

        var movedTasks = TaskBoardMoves.MoveInboxToPage(
            WorkPage,
            HomePage,
            [existing, second, first, headed]);

        Assert.Equal([first, second], movedTasks);
        Assert.Equal(HomePage, first.PageId);
        Assert.Equal(HomePage, second.PageId);
        Assert.Equal(6, first.SortOrder);
        Assert.Equal(7, second.SortOrder);
        Assert.Equal(WorkPage, headed.PageId);
    }

    private static Heading Heading(Guid id, Guid pageId, string title, double sortOrder)
        => new()
        {
            Id = id,
            PageId = pageId,
            Title = title,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    private static TaskItem Task(string title, Guid pageId, Guid? headingId, double sortOrder)
        => new()
        {
            Id = Guid.NewGuid(),
            PageId = pageId,
            HeadingId = headingId,
            Title = title,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
}
