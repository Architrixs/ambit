using Ambit.Avalonia.Heatmaps;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// A passive overlay layer that renders a <see cref="HeatmapLayer"/> inside an <see cref="AmbitViewer"/>.
/// </summary>
public class HeatmapOverlayLayer : AmbitLayer, IDisposable
{
    private readonly RegionOverlayRenderer _renderer;
    private readonly DrawOperation _drawOperation;
    private readonly bool _ownsRenderer;
    private HeatmapLayer? _heatmap;

    /// <summary>
    /// Gets or sets the heatmap data rendered by this layer.
    /// </summary>
    public HeatmapLayer? Heatmap
    {
        get => _heatmap;
        set
        {
            if (ReferenceEquals(_heatmap, value)) return;
            _heatmap = value;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeatmapOverlayLayer"/> class.
    /// </summary>
    /// <param name="renderer">An optional pre-configured renderer.</param>
    public HeatmapOverlayLayer(RegionOverlayRenderer? renderer = null, bool ownsRenderer = true)
    {
        _renderer = renderer ?? new RegionOverlayRenderer();
        _ownsRenderer = renderer is null || ownsRenderer;
        _drawOperation = new DrawOperation(this);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_heatmap != null)
        {
            context.Custom(_drawOperation);
        }
    }

    public void Dispose()
    {
        if (_ownsRenderer)
        {
            _renderer.Dispose();
        }
    }

    private sealed class DrawOperation : ICustomDrawOperation
    {
        private readonly HeatmapOverlayLayer _layer;

        public DrawOperation(HeatmapOverlayLayer layer)
        {
            _layer = layer;
        }

        public Rect Bounds => new(_layer.Bounds.Size);
        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var transform = _layer.Transform;
            var heatmap = _layer._heatmap;
            if (transform == null || heatmap == null) return;

            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();

            _layer._renderer.Render(canvas, Array.Empty<IRegion>(), new RegionRenderState(), transform, heatmap: heatmap, cellGrid: null, backgroundImage: null);

            canvas.Restore();
        }
    }
}
