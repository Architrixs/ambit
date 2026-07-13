using Ambit.Avalonia.Heatmaps;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace Ambit.Avalonia.Controls;

internal sealed class RegionOverlayDrawOperation : ICustomDrawOperation
{
    private readonly RegionOverlayRenderer _renderer;
    private Rect _bounds;
    private IReadOnlyList<IRegion> _regions = Array.Empty<IRegion>();
    private RegionRenderState _state = new();
    private ICoordinateTransform _transform = DefaultCoordinateTransform.Instance;
    private HeatmapLayer? _heatmap;
    private int _version;

    public RegionOverlayDrawOperation(RegionOverlayRenderer renderer)
    {
        _renderer = renderer;
    }

    public Rect Bounds => _bounds;

    public void Update(
        Rect bounds,
        IReadOnlyList<IRegion> regions,
        RegionRenderState state,
        ICoordinateTransform transform,
        HeatmapLayer? heatmap,
        int version)
    {
        _bounds = bounds;
        _regions = regions;
        _state = state;
        _transform = transform;
        _heatmap = heatmap;
        _version = version;
    }

    public bool HitTest(Point p) => false;

    public void Render(ImmediateDrawingContext context)
    {
        var feature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
        if (feature is null)
        {
            return;
        }

        using var lease = feature.Lease();
        var canvas = lease.SkCanvas;
        canvas.Save();
        canvas.ClipRect(new SkiaSharp.SKRect((float)_bounds.X, (float)_bounds.Y, (float)_bounds.Right, (float)_bounds.Bottom));
        _renderer.Render(canvas, _regions, _state, _transform, _heatmap);
        canvas.Restore();
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return other is RegionOverlayDrawOperation operation && operation._version == _version;
    }

    public void Dispose()
    {
    }

    private sealed class DefaultCoordinateTransform : ICoordinateTransform
    {
        public static DefaultCoordinateTransform Instance { get; } = new();

        public ControlPoint ToControlSpace(NormalizedPoint p)
        {
            return new ControlPoint(p.X, p.Y);
        }

        public NormalizedPoint ToNormalizedSpace(ControlPoint point)
        {
            return new NormalizedPoint(point.X, point.Y);
        }
    }
}
