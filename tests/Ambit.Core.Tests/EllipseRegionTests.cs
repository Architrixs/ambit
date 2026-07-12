using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class EllipseRegionTests
{
    [Fact]
    public void HitTestBody_ReturnsTrueForCenterAndBoundary()
    {
        var region = CreateRegion();

        region.HitTestBody(new NormalizedPoint(0.5, 0.5), 0d).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.9, 0.5), 0d).Should().BeTrue();
        region.HitTestBody(new NormalizedPoint(0.95, 0.5), 0.01).Should().BeFalse();
    }

    [Fact]
    public void GetHandles_MatchesBoundingBoxHandleLayout()
    {
        var region = CreateRegion();

        var handles = region.GetHandles();

        handles.Should().HaveCount(8);
        handles[0].Position.Should().Be(new NormalizedPoint(0.1, 0.2));
        handles[2].Position.Should().Be(new NormalizedPoint(0.9, 0.8));
        handles[5].Position.Should().Be(new NormalizedPoint(0.9, 0.5));
    }

    [Fact]
    public void MoveHandle_EdgeDragUpdatesOnlySingleAxis()
    {
        var region = CreateRegion();

        region.MoveHandle(4, new NormalizedPoint(0.5, 0.1));

        AssertBounds(region.Bounds, 0.1, 0.1, 0.9, 0.8);
    }

    [Fact]
    public void MoveHandle_CornerAspectLockPreservesBoundingBoxRatio()
    {
        var region = new EllipseRegion(
            new NormalizedPoint(0.2, 0.2),
            new NormalizedPoint(0.6, 0.4),
            CreateStyle(),
            lockAspectRatio: true);

        region.MoveHandle(2, new NormalizedPoint(0.8, 0.7));

        region.Bounds.Width.Should().BeApproximately(region.Bounds.Height * 2d, 1e-10);
        AssertBounds(region.Bounds, 0.2, 0.2, 0.8, 0.5);
    }

    [Fact]
    public void Translate_ClampsEllipseToUnitBounds()
    {
        var region = CreateRegion();

        region.Translate(new NormalizedVector(0.3, 0.4));

        AssertBounds(region.Bounds, 0.2, 0.4, 1.0, 1.0);
    }

    private static EllipseRegion CreateRegion()
    {
        return new EllipseRegion(
            new NormalizedPoint(0.1, 0.2),
            new NormalizedPoint(0.9, 0.8),
            CreateStyle());
    }

    private static RegionStyle CreateStyle()
    {
        return new RegionStyle { StrokeColorHex = "#AA44FF" };
    }

    private static void AssertBounds(NormalizedBounds bounds, double left, double top, double right, double bottom)
    {
        bounds.Left.Should().BeApproximately(left, 1e-10);
        bounds.Top.Should().BeApproximately(top, 1e-10);
        bounds.Right.Should().BeApproximately(right, 1e-10);
        bounds.Bottom.Should().BeApproximately(bottom, 1e-10);
    }
}
