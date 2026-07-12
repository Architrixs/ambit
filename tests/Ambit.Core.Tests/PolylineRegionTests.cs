using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class PolylineRegionTests
{
    [Fact]
    public void Constructor_RejectsFewerThanTwoVertices()
    {
        var action = () => new PolylineRegion([new NormalizedPoint(0.1, 0.1)], CreateStyle());

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HitTestBody_UsesSegmentProximityWithoutTreatingInteriorAsFilled()
    {
        var region = CreateRegion();

        region.HitTestBody(new NormalizedPoint(0.5, 0.52), 0.05).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.5, 0.2), 0.05).Should().BeFalse();
    }

    [Fact]
    public void MoveHandle_UpdatesRequestedVertex()
    {
        var region = CreateRegion();

        region.MoveHandle(1, new NormalizedPoint(0.5, 0.4));

        region.Vertices[1].Should().Be(new NormalizedPoint(0.5, 0.4));
    }

    [Fact]
    public void Translate_ClampsPolylineToUnitBounds()
    {
        var region = CreateRegion();

        region.Translate(new NormalizedVector(-0.3, 0.5));

        AssertVertices(
            region.Vertices,
            new NormalizedPoint(0.0, 0.5),
            new NormalizedPoint(0.2, 0.7),
            new NormalizedPoint(0.5, 1.0));
    }

    private static PolylineRegion CreateRegion()
    {
        return new PolylineRegion(
            [
                new NormalizedPoint(0.1, 0.1),
                new NormalizedPoint(0.3, 0.3),
                new NormalizedPoint(0.6, 0.6),
            ],
            CreateStyle());
    }

    private static RegionStyle CreateStyle()
    {
        return new RegionStyle { StrokeColorHex = "#33CC66" };
    }

    private static void AssertVertices(IReadOnlyList<NormalizedPoint> actual, params NormalizedPoint[] expected)
    {
        actual.Should().HaveCount(expected.Length);
        for (var index = 0; index < expected.Length; index++)
        {
            actual[index].X.Should().BeApproximately(expected[index].X, 1e-10);
            actual[index].Y.Should().BeApproximately(expected[index].Y, 1e-10);
        }
    }
}
