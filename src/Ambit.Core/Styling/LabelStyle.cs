namespace Ambit;

/// <summary>
/// Defines how a region label should be rendered.
/// </summary>
public sealed class LabelStyle
{
    /// <summary>
    /// Gets or initializes the text color in hex format.
    /// </summary>
    public required string TextColorHex { get; init; }

    /// <summary>
    /// Gets or initializes the optional label background color in hex format.
    /// </summary>
    public string? BackgroundColorHex { get; init; }

    /// <summary>
    /// Gets or initializes the label font size in control pixels.
    /// </summary>
    public double FontSize { get; init; } = 12.0;

    /// <summary>
    /// Gets or initializes the optional explicit anchor override.
    /// A <see langword="null"/> value means the renderer should use the region centroid.
    /// </summary>
    public NormalizedPoint? AnchorOverride { get; init; }
}
