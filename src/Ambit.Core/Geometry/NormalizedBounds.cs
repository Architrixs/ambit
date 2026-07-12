namespace Ambit;

/// <summary>
/// Represents an axis-aligned bounding box in normalized region space.
/// </summary>
public readonly record struct NormalizedBounds
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizedBounds"/> struct.
    /// </summary>
    /// <param name="left">The left edge coordinate.</param>
    /// <param name="top">The top edge coordinate.</param>
    /// <param name="right">The right edge coordinate.</param>
    /// <param name="bottom">The bottom edge coordinate.</param>
    public NormalizedBounds(double left, double top, double right, double bottom)
    {
        Left = Validate(left, nameof(left), right, nameof(right));
        Top = Validate(top, nameof(top), bottom, nameof(bottom));
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    /// Gets the left edge coordinate.
    /// </summary>
    public double Left { get; }

    /// <summary>
    /// Gets the top edge coordinate.
    /// </summary>
    public double Top { get; }

    /// <summary>
    /// Gets the right edge coordinate.
    /// </summary>
    public double Right { get; }

    /// <summary>
    /// Gets the bottom edge coordinate.
    /// </summary>
    public double Bottom { get; }

    /// <summary>
    /// Gets the bounding box width.
    /// </summary>
    public double Width => Right - Left;

    /// <summary>
    /// Gets the bounding box height.
    /// </summary>
    public double Height => Bottom - Top;

    private static double Validate(double value, string valueName, double otherValue, string otherName)
    {
        if (value > otherValue)
        {
            throw new ArgumentOutOfRangeException(valueName, $"{valueName} must be less than or equal to {otherName}.");
        }

        return value;
    }
}
