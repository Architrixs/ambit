namespace Ambit;

/// <summary>
/// Represents a clamped point in normalized region space.
/// </summary>
/// <param name="x">The horizontal coordinate.</param>
/// <param name="y">The vertical coordinate.</param>
public readonly record struct NormalizedPoint(double x, double y)
{
    /// <summary>
    /// Gets the horizontal coordinate clamped to the inclusive <c>[0, 1]</c> interval.
    /// </summary>
    public double X { get; } = Clamp(x);

    /// <summary>
    /// Gets the vertical coordinate clamped to the inclusive <c>[0, 1]</c> interval.
    /// </summary>
    public double Y { get; } = Clamp(y);

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
