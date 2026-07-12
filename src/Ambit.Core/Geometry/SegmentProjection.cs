namespace Ambit;

/// <summary>
/// Represents the closest point on a segment to a test point.
/// </summary>
public readonly record struct SegmentProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SegmentProjection"/> struct.
    /// </summary>
    /// <param name="point">The projected point on the segment.</param>
    /// <param name="parameter">The normalized segment parameter in the inclusive <c>[0, 1]</c> interval.</param>
    public SegmentProjection(NormalizedPoint point, double parameter)
    {
        Point = point;
        Parameter = GeometryUtilities.ClampToUnitInterval(parameter);
    }

    /// <summary>
    /// Gets the projected point on the segment.
    /// </summary>
    public NormalizedPoint Point { get; }

    /// <summary>
    /// Gets the normalized segment parameter in the inclusive <c>[0, 1]</c> interval.
    /// </summary>
    public double Parameter { get; }
}
