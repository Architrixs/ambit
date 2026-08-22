namespace Ambit;

/// <summary>
/// Defines the available anchor placements for a region label.
/// </summary>
public enum LabelPlacement
{
    /// <summary>
    /// Place the label at the top-left corner of the region boundary.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Place the label at the top-right corner of the region boundary.
    /// </summary>
    TopRight,

    /// <summary>
    /// Place the label at the bottom-left corner of the region boundary.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Place the label at the bottom-right corner of the region boundary.
    /// </summary>
    BottomRight,

    /// <summary>
    /// Place the label centered inside the region boundary.
    /// </summary>
    CenterInside
}

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
    /// Gets or initializes the label placement relative to the shape.
    /// </summary>
    public LabelPlacement Placement { get; init; } = LabelPlacement.TopLeft;

    /// <summary>
    /// Gets or initializes the optional explicit anchor override.
    /// A <see langword="null"/> value means the renderer should use the placement relative to the shape.
    /// </summary>
    public NormalizedPoint? AnchorOverride { get; init; }

    /// <summary>
    /// Creates a copy of this style with the specified overrides.
    /// </summary>
    public LabelStyle With(
        string? textColorHex = null,
        string? backgroundColorHex = null,
        bool clearBackgroundColorHex = false,
        double? fontSize = null,
        LabelPlacement? placement = null,
        NormalizedPoint? anchorOverride = null,
        bool clearAnchorOverride = false)
    {
        return new LabelStyle
        {
            TextColorHex = textColorHex ?? TextColorHex,
            BackgroundColorHex = clearBackgroundColorHex ? null : (backgroundColorHex ?? BackgroundColorHex),
            FontSize = fontSize ?? FontSize,
            Placement = placement ?? Placement,
            AnchorOverride = clearAnchorOverride ? null : (anchorOverride ?? AnchorOverride),
        };
    }
}
