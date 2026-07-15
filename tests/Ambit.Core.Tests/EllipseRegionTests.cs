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
    public void GetHandles_ReturnsFourCorners()
    {
        var region = CreateRegion();

        var handles = region.GetHandles();

        handles.Should().HaveCount(4);
        handles.Select(h => h.HandleKind).Should().AllBe("corner");
        handles[0].Position.Should().Be(new NormalizedPoint(0.1, 0.2)); // TopLeft
        handles[1].Position.Should().Be(new NormalizedPoint(0.9, 0.2)); // TopRight
        handles[2].Position.Should().Be(new NormalizedPoint(0.9, 0.8)); // BottomRight
        handles[3].Position.Should().Be(new NormalizedPoint(0.1, 0.8)); // BottomLeft
    }

    [Fact]
    public void MoveHandle_CornerDragResizesFromOppositeAnchor()
    {
        var region = CreateRegion();

        // Move bottom-right corner (index 2); top-left should stay fixed.
        region.MoveHandle(2, new NormalizedPoint(0.95, 0.9));

        AssertBounds(region.Bounds, 0.1, 0.2, 0.95, 0.9);
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
