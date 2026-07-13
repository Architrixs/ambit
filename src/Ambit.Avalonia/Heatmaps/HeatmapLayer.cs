using SkiaSharp;

namespace Ambit.Avalonia.Heatmaps;

/// <summary>
/// Represents a low-resolution intensity grid rendered as a heatmap overlay.
/// </summary>
public sealed class HeatmapLayer
{
    /// <summary>
    /// Gets or initializes the heatmap row count.
    /// </summary>
    public required int Rows { get; init; }

    /// <summary>
    /// Gets or initializes the heatmap column count.
    /// </summary>
    public required int Columns { get; init; }

    /// <summary>
    /// Gets or initializes the row-major intensity buffer.
    /// </summary>
    public required IReadOnlyList<byte> Intensities { get; init; }

    /// <summary>
    /// Gets or initializes the overall layer opacity.
    /// </summary>
    public float Opacity { get; init; } = 0.6f;

    /// <summary>
    /// Gets or initializes the optional explicit color lookup table.
    /// </summary>
    public IReadOnlyList<SKColor>? ColorLut { get; init; }
}
