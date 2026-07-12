using FluentAssertions;

namespace Ambit.Core.Tests;

public sealed class GeometryUtilitiesTests
{
    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(2.0, 1.0)]
    public void ClampToUnitInterval_ConstrainsValuesIntoBounds(double value, double expected)
    {
        GeometryUtilities.ClampToUnitInterval(value).Should().Be(expected);
    }

    [Fact]
    public void Translate_ClampsTranslatedPointBackIntoNormalizedSpace()
    {
        var translated = GeometryUtilities.Translate(new NormalizedPoint(0.9, 0.1), new NormalizedVector(0.3, -0.2));

        translated.Should().Be(new NormalizedPoint(1.0, 0.0));
    }

    [Fact]
    public void ProjectPointOntoSegment_ClampsProjectionToSegmentEndpoints()
    {
        var projection = GeometryUtilities.ProjectPointOntoSegment(
            new NormalizedPoint(1.0, 1.0),
            new NormalizedPoint(0.0, 0.0),
            new NormalizedPoint(0.5, 0.0));

        projection.Point.Should().Be(new NormalizedPoint(0.5, 0.0));
        projection.Parameter.Should().Be(1.0);
    }

    [Fact]
    public void DistanceToSegment_ComputesShortestDistanceToProjectedPoint()
    {
        var distance = GeometryUtilities.DistanceToSegment(
            new NormalizedPoint(0.5, 0.5),
            new NormalizedPoint(0.0, 0.0),
            new NormalizedPoint(1.0, 0.0));

        distance.Should().BeApproximately(0.5, 1e-10);
    }

    [Fact]
    public void IsPointNearSegment_UsesInclusiveTolerance()
    {
        var isNear = GeometryUtilities.IsPointNearSegment(
            new NormalizedPoint(0.5, 0.05),
            new NormalizedPoint(0.0, 0.0),
            new NormalizedPoint(1.0, 0.0),
            0.05);

        isNear.Should().BeTrue();
    }

    [Fact]
    public void GetBounds_ComputesMinAndMaxExtents()
    {
        var bounds = GeometryUtilities.GetBounds(
            [
                new NormalizedPoint(0.2, 0.7),
                new NormalizedPoint(0.9, 0.1),
                new NormalizedPoint(0.4, 0.5),
            ]);

        bounds.Left.Should().Be(0.2);
        bounds.Top.Should().Be(0.1);
        bounds.Right.Should().Be(0.9);
        bounds.Bottom.Should().Be(0.7);
        bounds.Width.Should().Be(0.7);
        bounds.Height.Should().Be(0.6);
    }

    [Fact]
    public void GetPolygonCentroid_ComputesAreaWeightedCentroidForRectangle()
    {
        var centroid = GeometryUtilities.GetPolygonCentroid(
            [
                new NormalizedPoint(0.2, 0.3),
                new NormalizedPoint(0.8, 0.3),
                new NormalizedPoint(0.8, 0.7),
                new NormalizedPoint(0.2, 0.7),
            ]);

        centroid.X.Should().BeApproximately(0.5, 1e-10);
        centroid.Y.Should().BeApproximately(0.5, 1e-10);
    }

    [Fact]
    public void GetPolygonCentroid_FallsBackToVertexAverageForDegenerateGeometry()
    {
        var centroid = GeometryUtilities.GetPolygonCentroid(
            [
                new NormalizedPoint(0.1, 0.1),
                new NormalizedPoint(0.5, 0.5),
            ]);

        centroid.Should().Be(new NormalizedPoint(0.3, 0.3));
    }

    [Fact]
    public void IsPointNearPolyline_DetectsProximityForOpenAndClosedPaths()
    {
        NormalizedPoint[] vertices =
        [
            new NormalizedPoint(0.1, 0.1),
            new NormalizedPoint(0.9, 0.1),
            new NormalizedPoint(0.9, 0.9),
        ];

        GeometryUtilities.IsPointNearPolyline(new NormalizedPoint(0.5, 0.12), vertices, 0.03, closed: false).Should().BeTrue();
        GeometryUtilities.IsPointNearPolyline(new NormalizedPoint(0.5, 0.52), vertices, 0.03, closed: false).Should().BeFalse();
        GeometryUtilities.IsPointNearPolyline(new NormalizedPoint(0.5, 0.52), vertices, 0.03, closed: true).Should().BeTrue();
    }

    [Fact]
    public void IsPointInPolygon_UsesFillAndBoundaryHitTesting()
    {
        NormalizedPoint[] polygon =
        [
            new NormalizedPoint(0.1, 0.1),
            new NormalizedPoint(0.9, 0.1),
            new NormalizedPoint(0.8, 0.8),
            new NormalizedPoint(0.2, 0.7),
        ];

        GeometryUtilities.IsPointInPolygon(new NormalizedPoint(0.5, 0.4), polygon, 0d).Should().BeTrue();
        GeometryUtilities.IsPointInPolygon(new NormalizedPoint(0.1, 0.1), polygon, 0d).Should().BeTrue();
        GeometryUtilities.IsPointInPolygon(new NormalizedPoint(0.95, 0.95), polygon, 0.01).Should().BeFalse();
    }

    [Fact]
    public void TranslateAll_UsesClampedTranslationForWholePointSet()
    {
        var translated = GeometryUtilities.TranslateAll(
            [
                new NormalizedPoint(0.8, 0.8),
                new NormalizedPoint(0.9, 0.9),
            ],
            new NormalizedVector(0.3, 0.4));

        translated.Should().ContainInOrder(
            new NormalizedPoint(0.9, 0.9),
            new NormalizedPoint(1.0, 1.0));
    }
}
