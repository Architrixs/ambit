using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class LineRegionTests
{
    [Fact]
    public void HitTestBody_UsesStrokeBandProximity()
    {
        var region = CreateRegion();

        region.HitTestBody(new NormalizedPoint(0.5, 0.549), 0.05).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.5, 0.7), 0.05).Should().BeFalse();
    }

    [Fact]
    public void Decorations_ArePurelyAttachedData()
    {
        var inDecoration = new LabelDecoration(new NormalizedPoint(0.2, 0.5), "in");
        var outDecoration = new LabelDecoration(new NormalizedPoint(0.8, 0.5), "out");
        var region = new LineRegion(
            new NormalizedPoint(0.2, 0.5),
            new NormalizedPoint(0.8, 0.5),
            CreateStyle(),
            decorations: [inDecoration, outDecoration]);

        region.Decorations.Should().ContainInOrder(inDecoration, outDecoration);
    }

    [Fact]
    public void MoveHandle_UpdatesRequestedEndpoint()
    {
        var region = CreateRegion();

        region.MoveHandle(0, new NormalizedPoint(0.1, 0.2));

        region.Vertices[0].Should().Be(new NormalizedPoint(0.1, 0.2));
        region.Vertices[1].Should().Be(new NormalizedPoint(0.8, 0.5));
    }

    [Fact]
    public void Translate_ClampsLineToUnitBounds()
    {
        var region = CreateRegion();

        region.Translate(new NormalizedVector(0.5, 0.7));

        AssertVertices(
            region.Vertices,
            new NormalizedPoint(0.4, 1.0),
            new NormalizedPoint(1.0, 1.0));
    }

    private static LineRegion CreateRegion()
    {
        return new LineRegion(
            new NormalizedPoint(0.2, 0.5),
            new NormalizedPoint(0.8, 0.5),
            CreateStyle());
    }

    private static RegionStyle CreateStyle()
    {
        return new RegionStyle { StrokeColorHex = "#FFAA00" };
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
