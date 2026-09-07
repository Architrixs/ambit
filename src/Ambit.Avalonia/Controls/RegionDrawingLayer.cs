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
using System.Collections.Generic;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// An interactive layer that renders regions, decorations, labels, and selection handles,
/// while bridging pointer events to a <see cref="RegionEditController"/>.
/// </summary>
public class RegionDrawingLayer : AmbitLayer, IDisposable
{
    private readonly RegionEditController _controller;
    private readonly RegionOverlayRenderer _renderer;
    private readonly DrawOperation _drawOperation;
    private readonly bool _ownsRenderer;
    private int _contentVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionDrawingLayer"/> class.
    /// </summary>
    /// <param name="controller">The interaction controller to drive.</param>
    /// <param name="renderer">An optional pre-configured renderer.</param>
    public RegionDrawingLayer(RegionEditController controller, RegionOverlayRenderer? renderer = null, bool ownsRenderer = true)
    {
        ArgumentNullException.ThrowIfNull(controller);
        _controller = controller;
        _renderer = renderer ?? new RegionOverlayRenderer();
        _ownsRenderer = renderer is null || ownsRenderer;
        _drawOperation = new DrawOperation(this);

        _controller.RenderStateChanged += OnControllerRenderStateChanged;
        _controller.CursorChanged += OnControllerCursorChanged;
        this.DoubleTapped += OnDoubleTappedEvent;
    }

    /// <summary>
    /// Gets the interaction controller managed by this layer.
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
        context.Custom(_drawOperation);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;
        var isRightOrMiddle = properties.IsRightButtonPressed || properties.IsMiddleButtonPressed;

        if (isRightOrMiddle)
        {
            base.OnPointerPressed(e);
            return;
        }

        // Always let the controller decide — it handles deselection when clicking background
        // with no hover target (HandleBackgroundPress clears SelectedRegionId).
        _controller.OnPointerPressed(new ControlPoint(point.X, point.Y));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var point = e.GetPosition(this);
        _controller.OnPointerMoved(new ControlPoint(point.X, point.Y));
        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var point = e.GetPosition(this);
        _controller.OnPointerReleased(new ControlPoint(point.X, point.Y));
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

    private void OnDoubleTappedEvent(object? sender, TappedEventArgs e)
    {
        var point = e.GetPosition(this);
        _controller.OnPointerDoubleTapped(new ControlPoint(point.X, point.Y));
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            _controller.CancelActiveOperation();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            if (_controller.DeleteSelected()) e.Handled = true;
        }
        else if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            _controller.OnPointerDoubleTapped(new ControlPoint(0, 0));
            e.Handled = true;
        }
    }

    private void OnControllerRenderStateChanged(object? sender, EventArgs e)
    {
        _contentVersion++;
        InvalidateVisual();
    }

    private void OnControllerCursorChanged(object? sender, string? cursorName)
    {
        Cursor = cursorName switch
        {
            "SizeAll" => new Cursor(StandardCursorType.SizeAll),
            "Hand" => new Cursor(StandardCursorType.Hand),
            "Cross" => new Cursor(StandardCursorType.Cross),
            "SizeNorthSouth" => new Cursor(StandardCursorType.SizeNorthSouth),
            "SizeWestEast" => new Cursor(StandardCursorType.SizeWestEast),
            "SizeNorthwestSoutheast" => new Cursor(StandardCursorType.TopLeftCorner),
            "SizeNortheastSouthwest" => new Cursor(StandardCursorType.TopRightCorner),
            "Arrow" => new Cursor(StandardCursorType.Arrow),
            _ => Cursor.Default,
        };
    }

    public void Dispose()
    {
        _controller.RenderStateChanged -= OnControllerRenderStateChanged;
        _controller.CursorChanged -= OnControllerCursorChanged;
        this.DoubleTapped -= OnDoubleTappedEvent;
        if (_ownsRenderer)
        {
            _renderer.Dispose();
        }
    }

    // Reused buffer to avoid per-frame List<IRegion> alloc during drawing.
    private IRegion[] _drawBuffer = Array.Empty<IRegion>();
    private IReadOnlyList<IRegion> GetRegionsWithDrawing(IRegion? drawing, IReadOnlyList<IEditableRegion> regions)
    {
        if (drawing is null) return regions;
        if (_drawBuffer.Length != regions.Count + 1) _drawBuffer = new IRegion[regions.Count + 1];
        for (var i = 0; i < regions.Count; i++) _drawBuffer[i] = regions[i];
        _drawBuffer[regions.Count] = drawing;
        return new ArraySegment<IRegion>(_drawBuffer, 0, regions.Count + 1);
    }

    private sealed class DrawOperation : ICustomDrawOperation
    {
        private readonly RegionDrawingLayer _layer;

        public DrawOperation(RegionDrawingLayer layer)
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
            if (transform == null) return;

            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();

            var regions = _layer.GetRegionsWithDrawing(_layer._controller.DrawingRegion, _layer._controller.Regions);
            var state = _layer._controller.BuildRenderState();
            _layer._renderer.Render(canvas, regions, state, transform, heatmap: null, cellGrid: null, backgroundImage: null);

            canvas.Restore();
        }
    }
}
