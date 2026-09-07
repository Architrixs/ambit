namespace Ambit;

/// <summary>
/// Represents a point in control pixel space (Avalonia-free). This is intentionally in <see cref="Ambit"/>
/// core so <see cref="ICoordinateTransform"/> stays dependency-free — do not replace with <c>Avalonia.Point</c>.
/// </summary>
public readonly record struct ControlPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ControlPoint"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate in pixels.</param>
    /// <param name="y">The vertical coordinate in pixels.</param>
    public ControlPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the horizontal coordinate in pixels.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the vertical coordinate in pixels.
    /// </summary>
    public double Y { get; }
}
