using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// An interactive layer that renders a cell grid and allows drag-painting cell selection
/// when enabled via the <see cref="RegionEditController"/>.
/// </summary>
public class CellGridLayer : AmbitLayer, IDisposable
{
    private readonly RegionEditController _controller;
    private readonly RegionOverlayRenderer _renderer;
    private readonly DrawOperation _drawOperation;

    /// <summary>
    /// Initializes a new instance of the <see cref="CellGridLayer"/> class.
    /// </summary>
    /// <param name="controller">The interaction controller to drive.</param>
    /// <param name="renderer">An optional pre-configured renderer.</param>
    public CellGridLayer(RegionEditController controller, RegionOverlayRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        _controller = controller;
        _renderer = renderer ?? new RegionOverlayRenderer();
        _drawOperation = new DrawOperation(this);

        _controller.CellsChanged += OnCellsChanged;
    }

    /// <summary>
    /// Gets the interaction controller associated with this layer.
    /// </summary>
    public RegionEditController Controller => _controller;

    /// <inheritdoc />
    public override void OnViewerTransformChanged(ICoordinateTransform transform)
    {
        base.OnViewerTransformChanged(transform);
        _controller.CoordinateTransform = transform;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_controller.CellGrid != null)
        {
            context.Custom(_drawOperation);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (_controller.IsCellPaintMode && _controller.CellGrid != null)
        {
            var point = e.GetPosition(this);
            _controller.OnPointerPressed(new ControlPoint(point.X, point.Y));
            e.Handled = true;
            return;
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_controller.IsCellPaintMode && _controller.CellGrid != null)
        {
            var point = e.GetPosition(this);
            _controller.OnPointerMoved(new ControlPoint(point.X, point.Y));
            e.Handled = true;
            return;
        }

        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_controller.IsCellPaintMode && _controller.CellGrid != null)
        {
            var point = e.GetPosition(this);
            _controller.OnPointerReleased(new ControlPoint(point.X, point.Y));
            e.Handled = true;
            return;
        }

        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!e.Handled && Parent is AmbitViewer viewer)
        {
            viewer.RaiseEvent(e);
        }
    }

    private void OnCellsChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    public void Dispose()
    {
        _controller.CellsChanged -= OnCellsChanged;
        _renderer.Dispose();
    }

    private sealed class DrawOperation : ICustomDrawOperation
    {
        private readonly CellGridLayer _layer;

        public DrawOperation(CellGridLayer layer)
        {
            _layer = layer;
        }

        public Rect Bounds => new(_layer.Bounds.Size);
        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            var transform = _layer.Transform ?? _layer._controller.CoordinateTransform;
            var cellGrid = _layer._controller.CellGrid;
            if (transform == null || cellGrid == null) return;

            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();

            var state = _layer._controller.BuildRenderState();
            _layer._renderer.Render(canvas, Array.Empty<IRegion>(), state, transform, heatmap: null, cellGrid: cellGrid, backgroundImage: null);

            canvas.Restore();
        }
    }
}
