using WindowsTrayTasks.Domain;

namespace WindowsTrayTasks.Domain.Tests;

public sealed class SortOrderMathTests
{
    [Fact]
    public void Between_TwoNeighbors_ReturnsMidpoint()
    {
        Assert.Equal(2.0, SortOrderMath.Between(1.0, 3.0));
    }

    [Fact]
    public void Between_TopOfList_ReturnsLowerThanNext()
    {
        Assert.Equal(0.0, SortOrderMath.Between(null, 1.0));
    }

    [Fact]
    public void Between_BottomOfList_ReturnsHigherThanPrevious()
    {
        Assert.Equal(4.0, SortOrderMath.Between(3.0, null));
    }

    [Fact]
    public void NeedsRenumber_GapBelowMinimum_ReturnsTrue()
    {
        Assert.True(SortOrderMath.NeedsRenumber(1.0, 1.0 + SortOrderMath.MinimumGap / 2));
    }
}
