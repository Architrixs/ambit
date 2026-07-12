namespace Ambit;

/// <summary>
/// Represents a point in control pixel space.
/// </summary>
/// <param name="x">The horizontal coordinate in pixels.</param>
/// <param name="y">The vertical coordinate in pixels.</param>
public readonly record struct ControlPoint(double x, double y)
{
    /// <summary>
    /// Gets the horizontal coordinate in pixels.
    /// </summary>
    public double X { get; } = x;

    /// <summary>
    /// Gets the vertical coordinate in pixels.
    /// </summary>
    public double Y { get; } = y;
}
