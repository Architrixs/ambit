using Ambit.Avalonia.Heatmaps;
using SkiaSharp;

namespace Ambit.Avalonia.Rendering;

/// <summary>
/// Renders low-resolution heatmap buffers via an SKBitmap and color lookup table.
/// </summary>
public sealed class HeatmapRenderer : IDisposable
{
    private static readonly SKColor[] DefaultLut = BuildDefaultLut();
    private SKBitmap? _bitmap;
    private int _rows;
    private int _columns;

    /// <summary>
    /// Renders the heatmap onto the supplied canvas.
    /// </summary>
    /// <param name="canvas">The destination canvas.</param>
    /// <param name="layer">The heatmap layer.</param>
    /// <param name="transform">The coordinate transform.</param>
    /// <param name="resources">The shared Skia resource cache.</param>
    public void Render(SKCanvas canvas, HeatmapLayer layer, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (layer.Rows <= 0 || layer.Columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(layer), "Heatmap dimensions must be positive.");
        }

        if (layer.Intensities.Count != (layer.Rows * layer.Columns))
        {
            throw new ArgumentException("Heatmap intensity count must match rows * columns.", nameof(layer));
        }

        EnsureBitmap(layer.Rows, layer.Columns);

        var lut = layer.ColorLut ?? DefaultLut;
        for (var index = 0; index < layer.Intensities.Count; index++)
        {
            var sourceColor = lut[layer.Intensities[index]];
            _bitmap!.SetPixel(index % layer.Columns, index / layer.Columns, sourceColor.WithAlpha((byte)Math.Clamp((int)Math.Round(layer.Opacity * sourceColor.Alpha), 0, 255)));
        }

        var destination = RenderingUtilities.GetControlRect(transform);
        canvas.DrawBitmap(_bitmap, destination, resources.HeatmapPaint);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _bitmap?.Dispose();
    }

    private void EnsureBitmap(int rows, int columns)
    {
        if (_bitmap is not null && _rows == rows && _columns == columns)
        {
            return;
        }

        _bitmap?.Dispose();
        _bitmap = new SKBitmap(columns, rows, SKColorType.Bgra8888, SKAlphaType.Premul);
        _rows = rows;
        _columns = columns;
    }

    private static SKColor[] BuildDefaultLut()
    {
        var colors = new SKColor[256];
        for (var index = 0; index < colors.Length; index++)
        {
            var t = index / 255f;
            var r = (byte)(Math.Clamp((t - 0.5f) * 2f, 0f, 1f) * 255f);
            var g = (byte)(Math.Clamp(1f - Math.Abs((t * 2f) - 1f), 0f, 1f) * 255f);
            var b = (byte)(Math.Clamp((0.5f - t) * 2f, 0f, 1f) * 255f);
            
            // Fade out alpha for lower intensities so cold areas are transparent
            var alpha = (byte)(Math.Min(1.0f, t * 1.5f) * 200);
            colors[index] = new SKColor(r, g, b, alpha);
        }

        return colors;
    }
}
