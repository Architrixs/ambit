using Ambit.Avalonia.Heatmaps;
using SkiaSharp;

namespace Ambit.Avalonia.Rendering;

/// <summary>
/// Renders a region collection, decorations, overlays, and optional heatmap in one Skia pass.
/// </summary>
public sealed class RegionOverlayRenderer : IDisposable
{
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
    /// Renders a full overlay pass.
    /// </summary>
    /// <param name="canvas">The destination canvas.</param>
    /// <param name="regions">The regions to render.</param>
    /// <param name="state">The transient render state.</param>
    /// <param name="transform">The coordinate transform.</param>
    /// <param name="heatmap">The optional heatmap layer.</param>
    public void Render(SKCanvas canvas, IReadOnlyList<IRegion> regions, RegionRenderState state, ICoordinateTransform transform, HeatmapLayer? heatmap = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(transform);

        if (heatmap is not null)
        {
            _heatmapRenderer.Render(canvas, heatmap, transform, _resources);
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

            if (!string.IsNullOrWhiteSpace(region.Label) && region.Style.LabelStyle is not null)
            {
                var labelAnchor = region.Style.LabelStyle.AnchorOverride ?? RenderingUtilities.GetDefaultLabelAnchor(region);
                LabelDecorationRenderer.DrawLabel(canvas, labelAnchor, region.Label, region.Style.LabelStyle, transform, _resources);
            }

            if (region is IHandleProvider handleProvider && (region.Id == state.SelectedRegionId || region.Id == state.HoveredRegionId))
            {
                RenderHandles(canvas, handleProvider.GetHandles(), region.Style.DefaultHandleStyle, state, transform);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _heatmapRenderer.Dispose();
        _resources.Dispose();
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
