namespace Ambit;

/// <summary>
/// Represents a direction-indicator arrow attached to a region.
/// </summary>
public sealed class DirectionIndicatorDecoration : IToggleDecoration
{
    /// <summary>
    /// The built-in type identifier for direction indicator decorations.
    /// </summary>
    public const string DirectionIndicatorTypeId = "direction-arrow";

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectionIndicatorDecoration"/> class.
    /// </summary>
    /// <param name="anchor">The anchor position in normalized region space.</param>
    /// <param name="directionSign">The arrow direction sign. Allowed values are <c>-1</c> and <c>1</c>.</param>
    public DirectionIndicatorDecoration(NormalizedPoint anchor, int directionSign = 1)
    {
        Anchor = anchor;
        DirectionSign = NormalizeDirectionSign(directionSign);
    }

    /// <inheritdoc />
    public string TypeId => DirectionIndicatorTypeId;

    /// <inheritdoc />
    public NormalizedPoint Anchor { get; set; }

    /// <inheritdoc />
    public bool IsInteractive => true;

    /// <summary>
    /// Gets the current arrow direction sign.
    /// </summary>
    public int DirectionSign { get; private set; }

    /// <inheritdoc />
    public void Toggle()
    {
        DirectionSign = -DirectionSign;
    }

    private static int NormalizeDirectionSign(int directionSign)
    {
        return directionSign switch
        {
            1 => 1,
            -1 => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(directionSign), "Direction sign must be either -1 or 1."),
        };
    }
}
