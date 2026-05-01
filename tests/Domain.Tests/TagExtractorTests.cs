using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class TagExtractorTests
{
    [Fact]
    public void ExtractTags_AtHome_ReturnsHome()
    {
        Assert.Equal(["home"], TagExtractor.ExtractTags("Call plumber @home"));
    }

    [Fact]
    public void ExtractTags_TrailingPunctuation_StripsPunctuation()
    {
        Assert.Equal(["office"], TagExtractor.ExtractTags("Send note @office."));
    }

    [Fact]
    public void ExtractTags_HyphenUnderscoreDot_AreAllowedInsideToken()
    {
        Assert.Equal(["online-work_v2"], TagExtractor.ExtractTags("Deploy @online-work_v2"));
    }

    [Fact]
    public void ExtractTags_EmailAddress_IsNotExtracted()
    {
        Assert.Empty(TagExtractor.ExtractTags("email me@example.com"));
    }

    [Fact]
    public void ExtractTags_Duplicates_AreReturnedOnce()
    {
        Assert.Equal(["home"], TagExtractor.ExtractTags("@home @Home"));
    }

    [Fact]
    public void Normalize_TrimsAtSignAndLowercases()
    {
        Assert.Equal("home", TagExtractor.Normalize("@Home"));
    }
}
