namespace Ambit;

/// <summary>
/// Represents a translation delta in normalized region space.
/// </summary>
/// <param name="dx">The horizontal delta.</param>
/// <param name="dy">The vertical delta.</param>
public readonly record struct NormalizedVector(double dx, double dy)
{
    /// <summary>
    /// Gets the horizontal delta.
    /// </summary>
    public double Dx { get; } = dx;

    /// <summary>
    /// Gets the vertical delta.
    /// </summary>
    public double Dy { get; } = dy;
}
