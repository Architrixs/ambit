namespace Ambit;

/// <summary>
/// Provides reusable geometry helpers for normalized region math.
/// </summary>
public static class GeometryUtilities
{
    /// <summary>
    /// Clamps a scalar value into the inclusive <c>[0, 1]</c> interval.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <returns>The clamped value.</returns>
    public static double ClampToUnitInterval(double value)
    {
        return value switch
        {
            < 0d => 0d,
            > 1d => 1d,
            _ => value,
        };
    }

    /// <summary>
    /// Translates a normalized point by the specified delta and clamps the result.
    /// </summary>
    /// <param name="point">The source point.</param>
    /// <param name="delta">The translation delta.</param>
    /// <returns>The translated point.</returns>
    public static NormalizedPoint Translate(NormalizedPoint point, NormalizedVector delta)
    {
        return new NormalizedPoint(point.X + delta.Dx, point.Y + delta.Dy);
    }

    /// <summary>
    /// Computes the squared Euclidean distance between two normalized points.
    /// </summary>
    /// <param name="first">The first point.</param>
    /// <param name="second">The second point.</param>
    /// <returns>The squared distance.</returns>
    public static double DistanceSquared(NormalizedPoint first, NormalizedPoint second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return (dx * dx) + (dy * dy);
    }

    /// <summary>
    /// Computes the Euclidean distance between two normalized points.
    /// </summary>
    /// <param name="first">The first point.</param>
    /// <param name="second">The second point.</param>
    /// <returns>The distance.</returns>
    public static double Distance(NormalizedPoint first, NormalizedPoint second)
    {
        return Math.Sqrt(DistanceSquared(first, second));
    }

    /// <summary>
    /// Projects a normalized point onto a line segment.
    /// </summary>
    /// <param name="point">The point to project.</param>
    /// <param name="segmentStart">The segment start point.</param>
    /// <param name="segmentEnd">The segment end point.</param>
    /// <returns>The closest point on the segment and its normalized parameter.</returns>
    public static SegmentProjection ProjectPointOntoSegment(
        NormalizedPoint point,
        NormalizedPoint segmentStart,
        NormalizedPoint segmentEnd)
    {
        var dx = segmentEnd.X - segmentStart.X;
        var dy = segmentEnd.Y - segmentStart.Y;
        var segmentLengthSquared = (dx * dx) + (dy * dy);

        if (segmentLengthSquared <= double.Epsilon)
        {
            return new SegmentProjection(segmentStart, 0d);
        }

        var pointDx = point.X - segmentStart.X;
        var pointDy = point.Y - segmentStart.Y;
        var rawParameter = ((pointDx * dx) + (pointDy * dy)) / segmentLengthSquared;
        var parameter = ClampToUnitInterval(rawParameter);
        var projectedPoint = new NormalizedPoint(segmentStart.X + (parameter * dx), segmentStart.Y + (parameter * dy));

        return new SegmentProjection(projectedPoint, parameter);
    }

    /// <summary>
    /// Computes the shortest distance from a normalized point to a line segment.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <param name="segmentStart">The segment start point.</param>
    /// <param name="segmentEnd">The segment end point.</param>
    /// <returns>The shortest distance to the segment.</returns>
    public static double DistanceToSegment(
        NormalizedPoint point,
        NormalizedPoint segmentStart,
        NormalizedPoint segmentEnd)
    {
        var projection = ProjectPointOntoSegment(point, segmentStart, segmentEnd);
        return Distance(point, projection.Point);
    }

    /// <summary>
    /// Determines whether a normalized point lies within the specified distance of a line segment.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <param name="segmentStart">The segment start point.</param>
    /// <param name="segmentEnd">The segment end point.</param>
    /// <param name="toleranceNormalized">The maximum allowed distance.</param>
    /// <returns><see langword="true"/> when the point is within tolerance; otherwise <see langword="false"/>.</returns>
    public static bool IsPointNearSegment(
        NormalizedPoint point,
        NormalizedPoint segmentStart,
        NormalizedPoint segmentEnd,
        double toleranceNormalized)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(toleranceNormalized);

        return DistanceToSegment(point, segmentStart, segmentEnd) <= toleranceNormalized;
    }

    /// <summary>
    /// Computes the bounding box of a point set.
    /// </summary>
    /// <param name="points">The points to bound.</param>
    /// <returns>The bounding box spanning all points.</returns>
    public static NormalizedBounds GetBounds(IReadOnlyList<NormalizedPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count == 0)
        {
            throw new ArgumentException("At least one point is required to compute bounds.", nameof(points));
        }

        var left = points[0].X;
        var right = points[0].X;
        var top = points[0].Y;
        var bottom = points[0].Y;

        for (var index = 1; index < points.Count; index++)
        {
            var point = points[index];
            left = Math.Min(left, point.X);
            right = Math.Max(right, point.X);
            top = Math.Min(top, point.Y);
            bottom = Math.Max(bottom, point.Y);
        }

        return new NormalizedBounds(left, top, right, bottom);
    }

    /// <summary>
    /// Computes the area-weighted centroid of a polygon.
    /// </summary>
    /// <param name="vertices">The polygon vertices in winding order.</param>
    /// <returns>The polygon centroid, or the arithmetic mean for degenerate polygons.</returns>
    public static NormalizedPoint GetPolygonCentroid(IReadOnlyList<NormalizedPoint> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        if (vertices.Count == 0)
        {
            throw new ArgumentException("At least one point is required to compute a centroid.", nameof(vertices));
        }

        if (vertices.Count < 3)
        {
            return GetVertexAverage(vertices);
        }

        double areaFactor = 0d;
        double centroidXFactor = 0d;
        double centroidYFactor = 0d;

        for (var index = 0; index < vertices.Count; index++)
        {
            var current = vertices[index];
            var next = vertices[(index + 1) % vertices.Count];
            var cross = (current.X * next.Y) - (next.X * current.Y);

            areaFactor += cross;
            centroidXFactor += (current.X + next.X) * cross;
            centroidYFactor += (current.Y + next.Y) * cross;
        }

        if (Math.Abs(areaFactor) <= double.Epsilon)
        {
            return GetVertexAverage(vertices);
        }

        var scale = 1d / (3d * areaFactor);
        return new NormalizedPoint(centroidXFactor * scale, centroidYFactor * scale);
    }

    private static NormalizedPoint GetVertexAverage(IReadOnlyList<NormalizedPoint> vertices)
    {
        double sumX = 0d;
        double sumY = 0d;

        for (var index = 0; index < vertices.Count; index++)
        {
            sumX += vertices[index].X;
            sumY += vertices[index].Y;
        }

        return new NormalizedPoint(sumX / vertices.Count, sumY / vertices.Count);
    }
}
