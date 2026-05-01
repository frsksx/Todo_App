using System.Globalization;
using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class DateInputParserTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("+5m", 0, 5, 0)]
    [InlineData("+2h", 2, 0, 0)]
    [InlineData("+1d", 24, 0, 0)]
    public void Parse_RelativeInput_ReturnsNowPlusOffset(string input, int hours, int minutes, int seconds)
    {
        var parsed = DateInputParser.Parse(input, Now, CultureInfo.InvariantCulture);

        Assert.Equal(Now.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds), parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyInput_ReturnsNull(string input)
    {
        Assert.Null(DateInputParser.Parse(input, Now, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Parse_NullInput_ReturnsNull()
    {
        Assert.Null(DateInputParser.Parse(null, Now, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Parse_InvalidRelativeUnit_ReturnsNull()
    {
        Assert.Null(DateInputParser.Parse("+2w", Now, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Parse_ExplicitDateTime_ReturnsUtcInstant()
    {
        var parsed = DateInputParser.Parse("2026-05-01 14:30", Now, CultureInfo.InvariantCulture);

        Assert.NotNull(parsed);
        Assert.Equal(DateTimeKind.Utc, parsed.Value.Kind);
    }
}
