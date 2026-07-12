namespace Ambit;

/// <summary>
/// Represents a translation delta in normalized region space.
/// </summary>
public readonly record struct NormalizedVector
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizedVector"/> struct.
    /// </summary>
    /// <param name="dx">The horizontal delta.</param>
    /// <param name="dy">The vertical delta.</param>
    public NormalizedVector(double dx, double dy)
    {
        Dx = dx;
        Dy = dy;
    }

    /// <summary>
    /// Gets the horizontal delta.
    /// </summary>
    public double Dx { get; }

    /// <summary>
    /// Gets the vertical delta.
    /// </summary>
    public double Dy { get; }
}
