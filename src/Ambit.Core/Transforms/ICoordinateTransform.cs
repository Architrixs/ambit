namespace Ambit;

/// <summary>
/// Maps between normalized region coordinates and control pixel coordinates.
/// </summary>
public interface ICoordinateTransform
{
    /// <summary>
    /// Converts a point from normalized region space into control pixel space.
    /// </summary>
    /// <param name="point">The normalized point to convert.</param>
    /// <returns>The corresponding control-space point.</returns>
    ControlPoint ToControlSpace(NormalizedPoint point);

    /// <summary>
    /// Converts a point from control pixel space into normalized region space.
    /// </summary>
    /// <param name="point">The control-space point to convert.</param>
    /// <returns>The corresponding normalized point.</returns>
    NormalizedPoint ToNormalizedSpace(ControlPoint point);
}
