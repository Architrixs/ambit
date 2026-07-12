namespace Ambit;

/// <summary>
/// Defines the visual styling for interactive handles.
/// </summary>
public sealed class HandleStyle
{
    /// <summary>
    /// Gets or initializes the handle radius in control pixels.
    /// </summary>
    public double RadiusPixels { get; init; } = 5.0;

    /// <summary>
    /// Gets or initializes the handle fill color in hex format.
    /// </summary>
    public string FillColorHex { get; init; } = "#FFFFFF";

    /// <summary>
    /// Gets or initializes the handle stroke color in hex format.
    /// </summary>
    public string StrokeColorHex { get; init; } = "#2680EB";

    /// <summary>
    /// Gets a reusable default handle style instance.
    /// </summary>
    public static HandleStyle Default { get; } = new();
}
