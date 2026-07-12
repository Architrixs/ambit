using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class PolygonRegionTests
{
    [Fact]
    public void Constructor_RejectsFewerThanThreeVertices()
    {
        var action = () => new PolygonRegion(
            [new NormalizedPoint(0.1, 0.1), new NormalizedPoint(0.9, 0.1)],
            CreateStyle());

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HitTestBody_ReturnsTrueForInteriorAndBoundaryPoints()
    {
        var region = CreateRegion();

        region.HitTestBody(new NormalizedPoint(0.4, 0.4), 0d).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.1, 0.1), 0d).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.91, 0.1), 0.02).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.95, 0.95), 0.01).Should().BeFalse();
    }

    [Fact]
    public void GetHandles_ReturnsOneVertexHandlePerVertex()
    {
        var region = CreateRegion();

        var handles = region.GetHandles();

        handles.Should().HaveCount(4);
        handles.Select(static handle => handle.HandleKind).Should().OnlyContain(static kind => kind == "vertex");
        handles.Select(static handle => handle.Position).Should().ContainInOrder(region.Vertices);
    }

    [Fact]
    public void MoveHandle_UpdatesOnlyRequestedVertex()
    {
        var region = CreateRegion();

        region.MoveHandle(1, new NormalizedPoint(0.8, 0.2));

        region.Vertices[1].Should().Be(new NormalizedPoint(0.8, 0.2));
        region.Vertices[0].Should().Be(new NormalizedPoint(0.1, 0.1));
    }

    [Fact]
    public void Translate_ClampsPolygonToUnitBounds()
    {
        var region = CreateRegion();

        region.Translate(new NormalizedVector(0.3, 0.7));

        AssertVertices(
            region.Vertices,
            new NormalizedPoint(0.2, 0.5),
            new NormalizedPoint(1.0, 0.5),
            new NormalizedPoint(0.9, 1.0),
            new NormalizedPoint(0.3, 1.0));
    }

    [Fact]
    public void InsertVertex_AddsVertexImmediatelyAfterRequestedIndex()
    {
        var region = CreateRegion();

        region.InsertVertex(1, new NormalizedPoint(0.95, 0.5));

        region.Vertices.Should().HaveCount(5);
        region.Vertices[2].Should().Be(new NormalizedPoint(0.95, 0.5));
    }

    private static PolygonRegion CreateRegion()
    {
        return new PolygonRegion(
            [
                new NormalizedPoint(0.1, 0.1),
                new NormalizedPoint(0.9, 0.1),
                new NormalizedPoint(0.8, 0.6),
                new NormalizedPoint(0.2, 0.6),
            ],
            CreateStyle());
    }

    private static RegionStyle CreateStyle()
    {
        return new RegionStyle { StrokeColorHex = "#00AAFF" };
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
