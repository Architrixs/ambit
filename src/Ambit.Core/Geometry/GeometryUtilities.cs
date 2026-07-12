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
    /// Translates a point set by the specified delta while clamping the result to the unit square.
    /// </summary>
    /// <param name="points">The points to translate.</param>
    /// <param name="delta">The requested translation delta.</param>
    /// <returns>A translated copy of the point set.</returns>
    public static IReadOnlyList<NormalizedPoint> TranslateAll(IReadOnlyList<NormalizedPoint> points, NormalizedVector delta)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count == 0)
        {
            return Array.Empty<NormalizedPoint>();
        }

        var adjustedDelta = ConstrainTranslationToUnitBounds(points, delta);
        var translated = new NormalizedPoint[points.Count];
        for (var index = 0; index < points.Count; index++)
        {
            translated[index] = Translate(points[index], adjustedDelta);
        }

        return translated;
    }

    /// <summary>
    /// Adjusts a requested translation so a point set remains inside the normalized unit square.
    /// </summary>
    /// <param name="points">The points to constrain.</param>
    /// <param name="delta">The requested translation delta.</param>
    /// <returns>The clamped translation delta.</returns>
    public static NormalizedVector ConstrainTranslationToUnitBounds(IReadOnlyList<NormalizedPoint> points, NormalizedVector delta)
    {
        var bounds = GetBounds(points);
        var adjustedDx = delta.Dx;
        var adjustedDy = delta.Dy;

        if ((bounds.Left + adjustedDx) < 0d)
        {
            adjustedDx = -bounds.Left;
        }
        else if ((bounds.Right + adjustedDx) > 1d)
        {
            adjustedDx = 1d - bounds.Right;
        }

        if ((bounds.Top + adjustedDy) < 0d)
        {
            adjustedDy = -bounds.Top;
        }
        else if ((bounds.Bottom + adjustedDy) > 1d)
        {
            adjustedDy = 1d - bounds.Bottom;
        }

        return new NormalizedVector(adjustedDx, adjustedDy);
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
    /// Determines whether a normalized point lies within tolerance of a polyline or polygon boundary.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <param name="vertices">The ordered vertices defining the path.</param>
    /// <param name="toleranceNormalized">The maximum distance from the path.</param>
    /// <param name="closed">A value indicating whether the final segment closes back to the first vertex.</param>
    /// <returns><see langword="true"/> when the point lies within tolerance of any segment; otherwise <see langword="false"/>.</returns>
    public static bool IsPointNearPolyline(
        NormalizedPoint point,
        IReadOnlyList<NormalizedPoint> vertices,
        double toleranceNormalized,
        bool closed)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentOutOfRangeException.ThrowIfNegative(toleranceNormalized);

        if (vertices.Count == 0)
        {
            return false;
        }

        var segmentCount = closed ? vertices.Count : vertices.Count - 1;
        if (segmentCount <= 0)
        {
            return Distance(point, vertices[0]) <= toleranceNormalized;
        }

        for (var index = 0; index < segmentCount; index++)
        {
            var start = vertices[index];
            var end = vertices[(index + 1) % vertices.Count];
            if (IsPointNearSegment(point, start, end, toleranceNormalized))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a normalized point intersects a closed polygon.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <param name="vertices">The polygon vertices in winding order.</param>
    /// <param name="toleranceNormalized">The boundary tolerance.</param>
    /// <returns><see langword="true"/> when the point lies inside the polygon or on its boundary; otherwise <see langword="false"/>.</returns>
    public static bool IsPointInPolygon(
        NormalizedPoint point,
        IReadOnlyList<NormalizedPoint> vertices,
        double toleranceNormalized)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentOutOfRangeException.ThrowIfNegative(toleranceNormalized);

        if (vertices.Count < 3)
        {
            return false;
        }

        if (IsPointNearPolyline(point, vertices, toleranceNormalized, closed: true))
        {
            return true;
        }

        var inside = false;
        for (int index = 0, previous = vertices.Count - 1; index < vertices.Count; previous = index++)
        {
            var current = vertices[index];
            var prior = vertices[previous];

            var intersects = ((current.Y > point.Y) != (prior.Y > point.Y))
                && (point.X < (((prior.X - current.X) * (point.Y - current.Y)) / (prior.Y - current.Y)) + current.X);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
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
