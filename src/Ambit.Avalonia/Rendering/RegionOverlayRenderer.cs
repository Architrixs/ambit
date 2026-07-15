using Ambit.Avalonia.Heatmaps;
using SkiaSharp;

namespace Ambit.Avalonia.Rendering;

/// <summary>
/// Renders a region collection, decorations, overlays, and optional heatmap in one Skia pass.
/// </summary>
public sealed class RegionOverlayRenderer : IDisposable
{
    private static readonly LabelStyle DefaultLabelStyle = new()
    {
        TextColorHex = "#FFFFFF",
        BackgroundColorHex = "#1E293B",
        FontSize = 11.0,
        Placement = LabelPlacement.TopLeft
    };

    private readonly RegionRenderRegistry _registry;
    private readonly SkiaRenderResources _resources;
    private readonly HeatmapRenderer _heatmapRenderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionOverlayRenderer"/> class.
    /// </summary>
    /// <param name="registry">The renderer registry to use.</param>
    public RegionOverlayRenderer(RegionRenderRegistry? registry = null)
    {
        _registry = registry ?? new RegionRenderRegistry().RegisterBuiltInRenderers();
        _resources = new SkiaRenderResources();
        _heatmapRenderer = new HeatmapRenderer();
    }

    /// <summary>
    /// Gets the duration of the last render pass in milliseconds.
    /// </summary>
    public double LastRenderTimeMs { get; private set; }

    /// <summary>
    /// Renders a full overlay pass.
    /// </summary>
    /// <param name="canvas">The destination canvas.</param>
    /// <param name="regions">The regions to render.</param>
    /// <param name="state">The transient render state.</param>
    /// <param name="transform">The coordinate transform.</param>
    /// <param name="heatmap">The optional heatmap layer.</param>
    public void Render(SKCanvas canvas, IReadOnlyList<IRegion> regions, RegionRenderState state, ICoordinateTransform transform, HeatmapLayer? heatmap = null, ICellGrid? cellGrid = null, SKBitmap? backgroundImage = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(transform);

        if (backgroundImage is not null)
        {
            var tl = transform.ToControlSpace(new NormalizedPoint(0, 0));
            var br = transform.ToControlSpace(new NormalizedPoint(1, 1));
            var destRect = new SKRect((float)tl.X, (float)tl.Y, (float)br.X, (float)br.Y);
            canvas.DrawBitmap(backgroundImage, destRect);
        }

        if (heatmap is not null)
        {
            _heatmapRenderer.Render(canvas, heatmap, transform, _resources);
        }

        if (cellGrid is not null)
        {
            RenderCellGrid(canvas, cellGrid, state, transform);
        }

        for (var index = 0; index < regions.Count; index++)
        {
            var region = regions[index];
            _registry.GetRegionRenderer(region.TypeId).Render(canvas, region, state, transform, _resources);

            for (var decorationIndex = 0; decorationIndex < region.Decorations.Count; decorationIndex++)
            {
                var decoration = region.Decorations[decorationIndex];
                _registry.GetDecorationRenderer(decoration.TypeId).Render(canvas, decoration, region, state, transform, _resources);
            }

            if (!string.IsNullOrWhiteSpace(region.Label))
            {
                var labelStyle = region.Style.LabelStyle ?? DefaultLabelStyle;
                var labelAnchor = labelStyle.AnchorOverride ?? RenderingUtilities.GetLabelAnchor(region, labelStyle.Placement);
                LabelDecorationRenderer.DrawLabel(canvas, labelAnchor, region.Label, labelStyle, transform, _resources);
            }

            if (region is IHandleProvider handleProvider && (region.Id == state.SelectedRegionId || region.Id == state.HoveredRegionId))
            {
                RenderHandles(canvas, handleProvider.GetHandles(), region.Style.DefaultHandleStyle, state, transform);
            }
        }

        LastRenderTimeMs = sw.Elapsed.TotalMilliseconds;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _heatmapRenderer.Dispose();
        _resources.Dispose();
    }

    private void RenderCellGrid(SKCanvas canvas, ICellGrid grid, RegionRenderState state, ICoordinateTransform transform)
    {
        var linePaint = _resources.StrokePaint;
        linePaint.Color = new SKColor(128, 128, 128, 80); // Semi-transparent gray
        linePaint.StrokeWidth = 1.0f;
        linePaint.PathEffect = null;

        var fillPaint = _resources.FillPaint;
        fillPaint.Color = new SKColor(244, 63, 94, 100); // Semi-transparent rose (#F43F5E with opacity)

        var rows = grid.Rows;
        var cols = grid.Columns;

        // Draw grid lines
        for (var r = 0; r <= rows; r++)
        {
            var p1 = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint(0, (double)r / rows));
            var p2 = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint(1, (double)r / rows));
            canvas.DrawLine(p1, p2, linePaint);
        }

        for (var c = 0; c <= cols; c++)
        {
            var p1 = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint((double)c / cols, 0));
            var p2 = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint((double)c / cols, 1));
            canvas.DrawLine(p1, p2, linePaint);
        }

        // Fill selected cells
        if (state.SelectedCells is not null)
        {
            foreach (var cell in state.SelectedCells)
            {
                var r = cell.Row;
                var c = cell.Col;
                if (r >= 0 && r < rows && c >= 0 && c < cols)
                {
                    var topLeft = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint((double)c / cols, (double)r / rows));
                    var bottomRight = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint((double)(c + 1) / cols, (double)(r + 1) / rows));
                    canvas.DrawRect(new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y), fillPaint);
                }
            }
        }

        // Fill hovered cell preview
        if (state.HoveredCell is { } hovered && hovered.Row >= 0 && hovered.Row < rows && hovered.Col >= 0 && hovered.Col < cols)
        {
            var topLeft = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint((double)hovered.Col / cols, (double)hovered.Row / rows));
            var bottomRight = RenderingUtilities.ToSkPoint(transform, new NormalizedPoint((double)(hovered.Col + 1) / cols, (double)(hovered.Row + 1) / rows));
            using var hoverPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = new SKColor(255, 255, 255, 60), // Subtle light highlight overlay for hovered cell
                IsAntialias = true
            };
            canvas.DrawRect(new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y), hoverPaint);
        }
    }

    private void RenderHandles(
        SKCanvas canvas,
        IReadOnlyList<RegionHandle> handles,
        HandleStyle style,
        RegionRenderState state,
        ICoordinateTransform transform)
    {
        _resources.ConfigureHandlePaints(style);
        var radius = (float)style.RadiusPixels;

        for (var index = 0; index < handles.Count; index++)
        {
            var handle = handles[index];
            var point = RenderingUtilities.ToSkPoint(transform, handle.Position);
            canvas.DrawCircle(point, radius, _resources.HandleFillPaint);
            canvas.DrawCircle(point, radius, _resources.HandleStrokePaint);

            if (state.HoveredHandleIndex == handle.Index)
            {
                canvas.DrawCircle(point, radius + 2f, _resources.ConfigureOverlayPaint(new SKColor(0xFF, 0xC8, 0x3D), 1.5f));
            }
        }
    }
}
