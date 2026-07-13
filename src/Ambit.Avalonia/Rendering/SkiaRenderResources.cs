using SkiaSharp;

namespace Ambit.Avalonia.Rendering;

/// <summary>
/// Reuses common Skia resources across render calls.
/// </summary>
public sealed class SkiaRenderResources : IDisposable
{
    private readonly Dictionary<string, SKColor> _colorCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SKPathEffect?> _pathEffectCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="SkiaRenderResources"/> class.
    /// </summary>
    public SkiaRenderResources()
    {
        StrokePaint = new SKPaint { IsAntialias = true, IsStroke = true, Style = SKPaintStyle.Stroke };
        FillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        OverlayPaint = new SKPaint { IsAntialias = true, IsStroke = true, Style = SKPaintStyle.Stroke };
        HandleFillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        HandleStrokePaint = new SKPaint { IsAntialias = true, IsStroke = true, Style = SKPaintStyle.Stroke };
        TextPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        LabelBackgroundPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        HeatmapPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        SharedPath = new SKPath();
        SharedRoundRect = new SKRoundRect();
    }

    /// <summary>
    /// Gets the reusable stroke paint.
    /// </summary>
    public SKPaint StrokePaint { get; }

    /// <summary>
    /// Gets the reusable fill paint.
    /// </summary>
    public SKPaint FillPaint { get; }

    /// <summary>
    /// Gets the reusable overlay paint.
    /// </summary>
    public SKPaint OverlayPaint { get; }

    /// <summary>
    /// Gets the reusable handle fill paint.
    /// </summary>
    public SKPaint HandleFillPaint { get; }

    /// <summary>
    /// Gets the reusable handle stroke paint.
    /// </summary>
    public SKPaint HandleStrokePaint { get; }

    /// <summary>
    /// Gets the reusable text paint.
    /// </summary>
    public SKPaint TextPaint { get; }

    /// <summary>
    /// Gets the reusable label background paint.
    /// </summary>
    public SKPaint LabelBackgroundPaint { get; }

    /// <summary>
    /// Gets the reusable heatmap paint.
    /// </summary>
    public SKPaint HeatmapPaint { get; }

    /// <summary>
    /// Gets the reusable shared path.
    /// </summary>
    public SKPath SharedPath { get; }

    /// <summary>
    /// Gets the reusable round-rect helper.
    /// </summary>
    public SKRoundRect SharedRoundRect { get; }

    /// <summary>
    /// Gets an SK color from a CSS-style hex string.
    /// </summary>
    /// <param name="hex">The source color string.</param>
    /// <returns>The parsed color.</returns>
    public SKColor GetColor(string hex)
    {
        if (_colorCache.TryGetValue(hex, out var cached))
        {
            return cached;
        }

        if (!SKColor.TryParse(hex, out var parsed))
        {
            throw new ArgumentException($"Invalid color value '{hex}'.", nameof(hex));
        }

        _colorCache[hex] = parsed;
        return parsed;
    }

    /// <summary>
    /// Configures the reusable stroke paint from a region style.
    /// </summary>
    /// <param name="style">The source style.</param>
    /// <returns>The configured stroke paint.</returns>
    public SKPaint ConfigureStrokePaint(RegionStyle style)
    {
        StrokePaint.Color = GetColor(style.StrokeColorHex);
        StrokePaint.StrokeWidth = (float)style.StrokeThickness;
        StrokePaint.PathEffect = GetPathEffect(style.StrokeDashPattern);
        return StrokePaint;
    }

    /// <summary>
    /// Configures the reusable fill paint from a region style.
    /// </summary>
    /// <param name="style">The source style.</param>
    /// <returns>The configured fill paint, or <see langword="null"/> when no fill should be rendered.</returns>
    public SKPaint? ConfigureFillPaint(RegionStyle style)
    {
        if (string.IsNullOrWhiteSpace(style.FillColorHex))
        {
            return null;
        }

        var fillColor = GetColor(style.FillColorHex);
        FillPaint.Color = fillColor.WithAlpha((byte)Math.Clamp((int)Math.Round(style.FillOpacity * 255d), 0, 255));
        return FillPaint;
    }

    /// <summary>
    /// Configures the reusable overlay paint.
    /// </summary>
    /// <param name="color">The overlay color.</param>
    /// <param name="strokeThickness">The overlay stroke thickness.</param>
    /// <returns>The configured overlay paint.</returns>
    public SKPaint ConfigureOverlayPaint(SKColor color, float strokeThickness)
    {
        OverlayPaint.Color = color;
        OverlayPaint.StrokeWidth = strokeThickness;
        OverlayPaint.PathEffect = null;
        return OverlayPaint;
    }

    /// <summary>
    /// Configures the reusable handle paints from a handle style.
    /// </summary>
    /// <param name="style">The source handle style.</param>
    public void ConfigureHandlePaints(HandleStyle style)
    {
        HandleFillPaint.Color = GetColor(style.FillColorHex);
        HandleStrokePaint.Color = GetColor(style.StrokeColorHex);
        HandleStrokePaint.StrokeWidth = 1.5f;
    }

    /// <summary>
    /// Configures the reusable text paint.
    /// </summary>
    /// <param name="textColorHex">The text color.</param>
    /// <param name="fontSize">The font size.</param>
    /// <returns>The configured text paint.</returns>
    public SKPaint ConfigureTextPaint(string textColorHex, float fontSize)
    {
        TextPaint.Color = GetColor(textColorHex);
        TextPaint.TextSize = fontSize;
        return TextPaint;
    }

    /// <summary>
    /// Configures the reusable label background paint.
    /// </summary>
    /// <param name="backgroundColorHex">The background color.</param>
    /// <returns>The configured background paint, or <see langword="null"/> when no background is requested.</returns>
    public SKPaint? ConfigureLabelBackgroundPaint(string? backgroundColorHex)
    {
        if (string.IsNullOrWhiteSpace(backgroundColorHex))
        {
            return null;
        }

        LabelBackgroundPaint.Color = GetColor(backgroundColorHex);
        return LabelBackgroundPaint;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StrokePaint.Dispose();
        FillPaint.Dispose();
        OverlayPaint.Dispose();
        HandleFillPaint.Dispose();
        HandleStrokePaint.Dispose();
        TextPaint.Dispose();
        LabelBackgroundPaint.Dispose();
        HeatmapPaint.Dispose();
        SharedPath.Dispose();
        SharedRoundRect.Dispose();

        foreach (var pathEffect in _pathEffectCache.Values)
        {
            pathEffect?.Dispose();
        }
    }

    private SKPathEffect? GetPathEffect(double[]? pattern)
    {
        if (pattern is null || pattern.Length == 0)
        {
            return null;
        }

        var key = string.Join(",", pattern);
        if (_pathEffectCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var dashPattern = new float[pattern.Length];
        for (var index = 0; index < pattern.Length; index++)
        {
            dashPattern[index] = (float)pattern[index];
        }

        var effect = SKPathEffect.CreateDash(dashPattern, 0f);
        _pathEffectCache[key] = effect;
        return effect;
    }
}
