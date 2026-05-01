using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class TaskTitleTagsTests
{
    [Fact]
    public void AddTagToken_AppendsNormalizedToken()
    {
        var title = TaskTitleTags.AddTagToken("Review inbox", "deep-work");

        Assert.Equal("Review inbox @deep-work", title);
    }

    [Fact]
    public void AddTagToken_DoesNotDuplicateExistingTag()
    {
        var title = TaskTitleTags.AddTagToken("Review inbox @deep-work", "@Deep-Work");

        Assert.Equal("Review inbox @deep-work", title);
    }

    [Fact]
    public void AddTagToken_BlankTag_LeavesTitleUnchanged()
    {
        var title = TaskTitleTags.AddTagToken("Review inbox", "   @   ");

        Assert.Equal("Review inbox", title);
    }
}
