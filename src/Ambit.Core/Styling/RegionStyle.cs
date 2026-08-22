namespace Ambit;

/// <summary>
/// Defines the persisted styling for a region instance.
/// </summary>
public sealed class RegionStyle
{
    /// <summary>
    /// Gets or initializes the stroke color in hex format.
    /// </summary>
    public required string StrokeColorHex { get; init; }

    /// <summary>
    /// Gets or initializes the stroke thickness in control pixels.
    /// </summary>
    public double StrokeThickness { get; init; } = 2.0;

    /// <summary>
    /// Gets or initializes the optional stroke dash pattern.
    /// </summary>
    public double[]? StrokeDashPattern { get; init; }

    /// <summary>
    /// Gets or initializes the optional fill color in hex format.
    /// </summary>
    public string? FillColorHex { get; init; }

    /// <summary>
    /// Gets or initializes the fill opacity.
    /// </summary>
    public double FillOpacity { get; init; } = 0.25;

    /// <summary>
    /// Gets or initializes the default handle style for interactive editing.
    /// </summary>
    public HandleStyle DefaultHandleStyle { get; init; } = HandleStyle.Default;

    /// <summary>
    /// Gets or initializes the optional label style.
    /// </summary>
    public LabelStyle? LabelStyle { get; init; }

    /// <summary>
    /// Creates a copy of this style with the specified overrides.
    /// Eliminates verbose manual cloning seen in host code (e.g. CloneWithStyle).
    /// </summary>
    public RegionStyle With(
        string? strokeColorHex = null,
        double? strokeThickness = null,
        double[]? strokeDashPattern = null,
        bool clearStrokeDashPattern = false,
        string? fillColorHex = null,
        bool clearFillColorHex = false,
        double? fillOpacity = null,
        HandleStyle? defaultHandleStyle = null,
        LabelStyle? labelStyle = null,
        bool clearLabelStyle = false)
    {
        return new RegionStyle
        {
            StrokeColorHex = strokeColorHex ?? StrokeColorHex,
            StrokeThickness = strokeThickness ?? StrokeThickness,
            StrokeDashPattern = clearStrokeDashPattern ? null : (strokeDashPattern ?? StrokeDashPattern),
            FillColorHex = clearFillColorHex ? null : (fillColorHex ?? FillColorHex),
            FillOpacity = fillOpacity ?? FillOpacity,
            DefaultHandleStyle = defaultHandleStyle ?? DefaultHandleStyle,
            LabelStyle = clearLabelStyle ? null : (labelStyle ?? LabelStyle),
        };
    }
}
