namespace Ambit;

/// <summary>
/// Represents a clamped point in normalized region space.
/// </summary>
public readonly record struct NormalizedPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizedPoint"/> struct.
    /// </summary>
    /// <param name="x">The horizontal coordinate.</param>
    /// <param name="y">The vertical coordinate.</param>
    public NormalizedPoint(double x, double y)
    {
        X = Clamp(x);
        Y = Clamp(y);
    }

    /// <summary>
    /// Gets the horizontal coordinate clamped to the inclusive <c>[0, 1]</c> interval.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the vertical coordinate clamped to the inclusive <c>[0, 1]</c> interval.
    /// </summary>
    public double Y { get; }

    private static double Clamp(double value)
    {
        return value switch
        {
            < 0d => 0d,
            > 1d => 1d,
            _ => value,
        };
    }
}
