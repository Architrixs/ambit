using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class RectangleRegionTests
{
    [Fact]
    public void Constructor_NormalizesDiagonalCornersIntoBoundsOrder()
    {
        var region = CreateRegion(new NormalizedPoint(0.8, 0.7), new NormalizedPoint(0.2, 0.3));

        region.Vertices.Should().ContainInOrder(
            new NormalizedPoint(0.2, 0.3),
            new NormalizedPoint(0.8, 0.7));
        AssertBounds(region.Bounds, 0.2, 0.3, 0.8, 0.7);
    }

    [Fact]
    public void GetHandles_ReturnsFourCornersAndFourEdgeMidpoints()
    {
        var region = CreateRegion(new NormalizedPoint(0.2, 0.3), new NormalizedPoint(0.8, 0.7));

        var handles = region.GetHandles();

        handles.Should().HaveCount(8);
        handles.Select(static handle => handle.HandleKind).Should().ContainInOrder(
            "corner",
            "corner",
            "corner",
            "corner",
            "edge-midpoint",
            "edge-midpoint",
            "edge-midpoint",
            "edge-midpoint");
        handles.Select(static handle => handle.Position).Should().ContainInOrder(
            new NormalizedPoint(0.2, 0.3),
            new NormalizedPoint(0.8, 0.3),
            new NormalizedPoint(0.8, 0.7),
            new NormalizedPoint(0.2, 0.7),
            new NormalizedPoint(0.5, 0.3),
            new NormalizedPoint(0.8, 0.5),
            new NormalizedPoint(0.5, 0.7),
            new NormalizedPoint(0.2, 0.5));
    }

    [Fact]
    public void HitTestBody_ReturnsTrueForInteriorAndExactBoundaryPoints()
    {
        var region = CreateRegion(new NormalizedPoint(0.2, 0.3), new NormalizedPoint(0.8, 0.7));

        region.HitTestBody(new NormalizedPoint(0.5, 0.5), 0d).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.2, 0.5), 0d).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.8, 0.7), 0d).Should().BeTrue();
    }

    [Fact]
    public void HitTestBody_UsesToleranceForNearBoundaryPoints()
    {
        var region = CreateRegion(new NormalizedPoint(0.2, 0.3), new NormalizedPoint(0.8, 0.7));

        region.HitTestBody(new NormalizedPoint(0.19, 0.5), 0.01).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.18, 0.5), 0.01).Should().BeFalse();
    }

    [Fact]
    public void Translate_MovesRectangleByRequestedDeltaWithinBounds()
    {
        var region = CreateRegion(new NormalizedPoint(0.2, 0.3), new NormalizedPoint(0.4, 0.5));

        region.Translate(new NormalizedVector(0.1, -0.2));

        AssertBounds(region.Bounds, 0.3, 0.1, 0.5, 0.3);
    }

    [Fact]
    public void Translate_ClampsMotionToKeepRectangleInsideUnitSquare()
    {
        var region = CreateRegion(new NormalizedPoint(0.7, 0.6), new NormalizedPoint(0.9, 0.8));

        region.Translate(new NormalizedVector(0.4, 0.5));

        AssertBounds(region.Bounds, 0.8, 0.8, 1.0, 1.0);
    }

    [Fact]
    public void MoveHandle_CornerDragResizesRectangleFreelyWhenAspectLockIsDisabled()
    {
        var region = CreateRegion(new NormalizedPoint(0.2, 0.3), new NormalizedPoint(0.8, 0.7));

        region.MoveHandle(0, new NormalizedPoint(0.1, 0.2));

        AssertBounds(region.Bounds, 0.1, 0.2, 0.8, 0.7);
    }

    [Fact]
    public void MoveHandle_EdgeDragUpdatesOnlyItsSingleAxis()
    {
        var region = CreateRegion(new NormalizedPoint(0.2, 0.3), new NormalizedPoint(0.8, 0.7), lockAspectRatio: true);

        region.MoveHandle(5, new NormalizedPoint(0.9, 0.1));

        AssertBounds(region.Bounds, 0.2, 0.3, 0.9, 0.7);
    }

    [Fact]
    public void MoveHandle_CornerDragPreservesAspectRatioWhenEnabled()
    {
        var region = CreateRegion(new NormalizedPoint(0.2, 0.2), new NormalizedPoint(0.6, 0.4), lockAspectRatio: true);

        region.MoveHandle(2, new NormalizedPoint(0.8, 0.7));

        region.Bounds.Width.Should().BeApproximately(region.Bounds.Height * 2d, 1e-10);
        AssertBounds(region.Bounds, 0.2, 0.2, 0.8, 0.5);
    }

    [Fact]
    public void MoveHandle_CornerAspectLockScalesBackWhenDraggedPastUnitBounds()
    {
        var region = CreateRegion(new NormalizedPoint(0.6, 0.6), new NormalizedPoint(0.8, 0.7), lockAspectRatio: true);

        region.MoveHandle(2, new NormalizedPoint(1.0, 1.0));

        AssertBounds(region.Bounds, 0.6, 0.6, 1.0, 0.8);
        region.Bounds.Width.Should().BeApproximately(region.Bounds.Height * 2d, 1e-10);
    }

    [Fact]
    public void MoveHandle_ThrowsForUnknownHandleIndex()
    {
        var region = CreateRegion(new NormalizedPoint(0.2, 0.3), new NormalizedPoint(0.8, 0.7));

        var action = () => region.MoveHandle(99, new NormalizedPoint(0.5, 0.5));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static RectangleRegion CreateRegion(
        NormalizedPoint first,
        NormalizedPoint second,
        bool lockAspectRatio = false)
    {
        return new RectangleRegion(
            first,
            second,
            new RegionStyle
            {
                StrokeColorHex = "#FF0000",
            },
            lockAspectRatio: lockAspectRatio);
    }

    private static void AssertBounds(NormalizedBounds bounds, double left, double top, double right, double bottom)
    {
        bounds.Left.Should().BeApproximately(left, 1e-10);
        bounds.Top.Should().BeApproximately(top, 1e-10);
        bounds.Right.Should().BeApproximately(right, 1e-10);
        bounds.Bottom.Should().BeApproximately(bottom, 1e-10);
    }
}
