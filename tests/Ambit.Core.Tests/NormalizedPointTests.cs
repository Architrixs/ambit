using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class NormalizedPointTests
{
    [Theory]
    [InlineData(-0.5, 1.5, 0.0, 1.0)]
    [InlineData(0.0, 0.0, 0.0, 0.0)]
    [InlineData(0.25, 0.75, 0.25, 0.75)]
    public void Constructor_ClampsCoordinatesIntoInclusiveUnitInterval(double x, double y, double expectedX, double expectedY)
    {
        var point = new NormalizedPoint(x, y);

        point.X.Should().Be(expectedX);
        point.Y.Should().Be(expectedY);
    }
}
