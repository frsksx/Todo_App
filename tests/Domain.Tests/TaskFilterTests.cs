using WindowsTrayTasks.Domain;
using WindowsTrayTasks.TestSupport;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class TaskFilterTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Apply_StateMode_ReturnsOnlyMatchingState()
    {
        var tasks = new[]
        {
            TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000001"), state: TaskState.Action),
            TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000002"), state: TaskState.Next),
        };

        var result = TaskFilter.Apply(tasks, new Dictionary<Guid, Reminder>(), new FilterCriteria("Next", "", Now)).ToList();

        Assert.Single(result);
        Assert.Equal(TaskState.Next, result[0].State);
    }

    [Fact]
    public void Apply_SearchText_MatchesTitleAndNotes()
    {
        var tasks = new[]
        {
            TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000001"), title: "Call bank"),
            TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000002"), title: "Errand", notes: "bank card"),
            TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000003"), title: "Email"),
        };

        var result = TaskFilter.Apply(tasks, new Dictionary<Guid, Reminder>(), new FilterCriteria("All", "bank", Now)).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_OverdueMode_ReturnsTasksWithDueReminder()
    {
        var task = TestEntities.Task();
        var reminder = TestEntities.Reminder(taskId: task.Id, nextFireAt: Now.AddMinutes(-1));

        var result = TaskFilter.Apply(
            [task],
            new Dictionary<Guid, Reminder> { [task.Id] = reminder },
            new FilterCriteria("Overdue", "", Now)).ToList();

        Assert.Single(result);
        Assert.Equal(task.Id, result[0].Id);
    }

    [Fact]
    public void Apply_ComposedFilter_AndsPageStateDateSearchAndTags()
    {
        var page = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var matching = TestEntities.Task(
            id: Guid.Parse("00000000-0000-0000-0000-000000000011"),
            title: "Prepare deck @computer",
            state: TaskState.Next,
            dueAt: Now);
        matching.PageId = page;
        matching.Tags.Add(new Tag
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000012"),
            PageId = page,
            Name = "computer",
            DisplayName = "computer",
        });

        var wrongPage = TestEntities.Task(
            id: Guid.Parse("00000000-0000-0000-0000-000000000013"),
            title: "Prepare deck @computer",
            state: TaskState.Next,
            dueAt: Now);
        wrongPage.PageId = Guid.Parse("00000000-0000-0000-0000-000000000099");
        wrongPage.Tags.Add(matching.Tags[0]);

        var criteria = new ComposedFilterCriteria(
            PageId: page,
            StateMode: "Next",
            IncludeDone: false,
            DateBucket: DateFilterBucket.Today,
            SearchText: "deck",
            TagNames: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "computer" },
            UntaggedOnly: false,
            FocusedHeadingId: null,
            NowUtc: Now);

        var result = TaskFilter.Apply([matching, wrongPage], new Dictionary<Guid, Reminder>(), criteria).ToList();

        Assert.Single(result);
        Assert.Equal(matching.Id, result[0].Id);
    }

    [Fact]
    public void Apply_UntaggedOnly_ExcludesTaggedTasks()
    {
        var tagged = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000021"));
        tagged.Tags.Add(new Tag { Name = "home", DisplayName = "home" });
        var untagged = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000022"));

        var criteria = ComposedFilterCriteria.Default(Now) with { UntaggedOnly = true };

        var result = TaskFilter.Apply([tagged, untagged], new Dictionary<Guid, Reminder>(), criteria).ToList();

        Assert.Single(result);
        Assert.Equal(untagged.Id, result[0].Id);
    }

    [Fact]
    public void Apply_OnlyCompleted_IgnoresIncludeDoneCheckbox()
    {
        var done = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000031"), state: TaskState.Done);
        var active = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000032"), state: TaskState.Action);
        var criteria = ComposedFilterCriteria.Default(Now) with
        {
            StateMode = "Only Completed",
            IncludeDone = false,
        };

        var result = TaskFilter.Apply([done, active], new Dictionary<Guid, Reminder>(), criteria).ToList();

        Assert.Single(result);
        Assert.Equal(done.Id, result[0].Id);
    }

    [Fact]
    public void Apply_ShowAll_IncludesCompletedTasks()
    {
        var done = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000041"), state: TaskState.Done);
        var active = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000042"), state: TaskState.Action);
        var archived = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000043"), state: TaskState.Archived);
        archived.ArchivedAt = Now.AddDays(-1);
        var criteria = ComposedFilterCriteria.Default(Now) with
        {
            StateMode = "Show All",
            IncludeDone = false,
        };

        var result = TaskFilter.Apply([done, active, archived], new Dictionary<Guid, Reminder>(), criteria).ToList();

        Assert.Equal([done.Id, active.Id], result.Select(t => t.Id));
    }

    [Fact]
    public void Apply_ArchivedMode_ReturnsOnlyArchivedTasks()
    {
        var archived = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000051"), state: TaskState.Archived);
        archived.ArchivedAt = Now.AddDays(-1);
        var done = TestEntities.Task(id: Guid.Parse("00000000-0000-0000-0000-000000000052"), state: TaskState.Done);
        var criteria = ComposedFilterCriteria.Default(Now) with
        {
            StateMode = "Archived",
            IncludeDone = true,
        };

        var result = TaskFilter.Apply([archived, done], new Dictionary<Guid, Reminder>(), criteria).ToList();

        Assert.Single(result);
        Assert.Equal(archived.Id, result[0].Id);
    }
}
