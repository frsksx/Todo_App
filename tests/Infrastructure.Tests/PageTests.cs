using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure.Persistence;
using WindowsTrayTasks.TestSupport;

namespace WindowsTrayTasks.Infrastructure.Tests;

public sealed class PageTests
{
    [Fact]
    public void Database_FreshInit_CreatesDefaultPage()
    {
        using var temp = new TempDatabase();

        var page = temp.Database.GetDefaultPage();

        Assert.Equal("Tasks", page.Name);
        Assert.True(page.IsDefault);
        Assert.Equal(page.Id, temp.Database.GetActivePageId());
    }

    [Fact]
    public void Database_SaveHeadingAndTask_AreScopedByPage()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock, new SequentialIdGenerator(100));
        var work = temp.Database.GetDefaultPage();
        work.Name = "Work";
        temp.Database.SavePage(work);
        var home = factory.CreatePage("Home");
        temp.Database.SavePage(home);

        var workHeading = factory.CreateHeading(work.Id, "Inbox");
        var homeHeading = factory.CreateHeading(home.Id, "Inbox");
        temp.Database.SaveHeading(workHeading);
        temp.Database.SaveHeading(homeHeading);
        temp.Database.SaveTask(factory.CreateTask(work.Id, "Work task", workHeading.Id));
        temp.Database.SaveTask(factory.CreateTask(home.Id, "Home task", homeHeading.Id));

        Assert.Single(temp.Database.GetTasks(pageId: work.Id));
        Assert.Single(temp.Database.GetTasks(pageId: home.Id));
        Assert.Equal(work.Id, temp.Database.GetHeadings(work.Id).Single().PageId);
        Assert.Equal(home.Id, temp.Database.GetHeadings(home.Id).Single().PageId);
    }

    [Fact]
    public void Database_ActivePage_RoundTripsThroughSettings()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock, new SequentialIdGenerator(200));
        var page = factory.CreatePage("Private");
        temp.Database.SavePage(page);

        temp.Database.SaveActivePageId(page.Id);
        var reopened = new Database(temp.Clock, temp.Path, new SequentialIdGenerator(300));

        Assert.Equal(page.Id, reopened.GetActivePageId());
    }

    [Fact]
    public void Database_SaveTaskWithAtTokens_CreatesPageScopedTags()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock, new SequentialIdGenerator(400));
        var page = temp.Database.GetDefaultPage();
        var task = factory.CreateTask(page.Id, "Email invoice @computer @online-work");

        temp.Database.SaveTask(task);

        var tags = temp.Database.GetTags(page.Id);
        Assert.Equal(["computer", "online-work"], tags.Select(t => t.Name).OrderBy(n => n).ToArray());
        var loaded = temp.Database.GetTasks(pageId: page.Id).Single();
        Assert.Equal(2, loaded.Tags.Count);
    }

    [Fact]
    public void Database_InlineTitleChange_ReconcilesTaskTags()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock, new SequentialIdGenerator(500));
        var page = temp.Database.GetDefaultPage();
        var task = factory.CreateTask(page.Id, "Pay bill @home @online");
        temp.Database.SaveTask(task);

        task.Title = "Pay bill @home";
        temp.Database.SaveTask(task);

        var loaded = temp.Database.GetTasks(pageId: page.Id).Single();
        Assert.Equal(["home"], loaded.Tags.Select(t => t.Name).ToArray());
    }

    [Fact]
    public void Database_SameTagNameOnDifferentPages_CreatesSeparateTags()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock, new SequentialIdGenerator(600));
        var work = temp.Database.GetDefaultPage();
        var home = factory.CreatePage("Home");
        temp.Database.SavePage(home);

        temp.Database.SaveTask(factory.CreateTask(work.Id, "Call @phone"));
        temp.Database.SaveTask(factory.CreateTask(home.Id, "Call @phone"));

        Assert.Single(temp.Database.GetTags(work.Id));
        Assert.Single(temp.Database.GetTags(home.Id));
        Assert.NotEqual(temp.Database.GetTags(work.Id).Single().Id, temp.Database.GetTags(home.Id).Single().Id);
    }
}
