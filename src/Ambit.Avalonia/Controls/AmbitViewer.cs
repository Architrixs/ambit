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
/// An OpenSeadragon-inspired viewport container that hosts background content (such as an image or video control)
/// alongside multiple transparent <see cref="AmbitLayer"/> overlays. Manages uniform aspect-fit letterboxing,
/// interactive panning and zooming, and coordinate transformations.
/// </summary>
public class AmbitViewer : Panel
{
    private Control? _content;
    private double _zoom = 1.0;
    private double _panX = 0.0;
    private double _panY = 0.0;
    private bool _isPanZoomEnabled = true;
    private Point? _lastPanPoint;
    private bool _isPanning;
    private Size? _contentSize;
    private SkiaSharp.SKBitmap? _backgroundImage;
    private BackgroundImageLayer? _backgroundImageLayer;
    private readonly PanZoomCoordinateTransform _transform = new();
    private readonly ViewportAnimator _viewportAnimator;

    internal interface IViewportTransformInfo
    {
        Rect GetBaseImageRect();
        Rect GetVisibleImageRect();
        double ZoomFactor { get; }
    }

    /// <summary>
    /// Occurs when the zoom or pan offset changes.
    /// </summary>
    public event EventHandler? PanZoomChanged;

    /// <summary>
    /// Gets or sets the active coordinate transform for the viewport.
    /// </summary>
    public ICoordinateTransform CoordinateTransform => _transform;

    /// <summary>
    /// Gets or sets the background image rendered by Skia behind all hosted controls and layers.
    /// </summary>
    public SkiaSharp.SKBitmap? BackgroundImage
    {
        get => _backgroundImage;
        set
        {
            if (ReferenceEquals(_backgroundImage, value)) return;
            _backgroundImage = value;
            InvalidateMeasure();
            InvalidateVisual();
            _backgroundImageLayer?.InvalidateVisual();
            NotifyTransformChanged();
        }
    }

    /// <summary>
    /// Gets or sets a logical content resolution override (e.g. 1920x1080) for viewport coordinate mapping.
    /// </summary>
    public Size? ContentSize
    {
        get => _contentSize;
        set
        {
            if (_contentSize == value) return;
            _contentSize = value;
            InvalidateMeasure();
            InvalidateVisual();
            NotifyTransformChanged();
        }
    }

    /// <summary>
    /// Gets or sets the background UI control (e.g., video player or image view) hosted inside the viewport.
    /// </summary>
    public Control? Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(_content, value)) return;
            if (_content != null)
            {
                Children.Remove(_content);
            }
            _content = value;
            if (_content != null)
            {
                Children.Insert(0, _content);
            }
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether interactive pan and zoom gestures are enabled.
    /// </summary>
    public bool IsPanZoomEnabled
    {
        get => _isPanZoomEnabled;
        set
        {
            if (_isPanZoomEnabled == value) return;
            _isPanZoomEnabled = value;
            if (!value)
            {
                ResetPanZoom();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current zoom level. Clamped to 0.1..20; non-finite values are ignored.
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            if (!double.IsFinite(value)) return;
            var clamped = Math.Clamp(value, 1.0, 20.0);
            if (Math.Abs(_zoom - clamped) > double.Epsilon)
            {
                SetViewport(clamped, _panX, _panY);
            }
        }
    }

    /// <summary>
    /// Gets or sets the X pan offset in pixels. Clamped so content stays in view.
    /// </summary>
    public double PanX
    {
        get => _panX;
        set
        {
            var coerced = CoercePanX(value);
            if (Math.Abs(_panX - coerced) > double.Epsilon)
            {
                SetViewport(_zoom, coerced, _panY);
            }
        }
    }

    /// <summary>
    /// Gets or sets the Y pan offset in pixels. Clamped so content stays in view.
    /// </summary>
    public double PanY
    {
        get => _panY;
        set
        {
            var coerced = CoercePanY(value);
            if (Math.Abs(_panY - coerced) > double.Epsilon)
            {
                SetViewport(_zoom, _panX, coerced);
            }
        }
    }

    private double CoercePanX(double desired)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return desired;
        var (minX, maxX) = GetPanLimits();
        return Math.Clamp(desired, minX, maxX);
    }

    private double CoercePanY(double desired)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return desired;
        var (_, _, minY, maxY) = GetPanLimitsWithY();
        return Math.Clamp(desired, minY, maxY);
    }

    private void CoercePan()
    {
        var (minX, maxX, minY, maxY) = GetPanLimitsWithY();
        _panX = Math.Clamp(_panX, minX, maxX);
        _panY = Math.Clamp(_panY, minY, maxY);
    }

    private (double minX, double maxX) GetPanLimits()
    {
        var (minX, maxX, _, _) = GetPanLimitsWithY();
        return (minX, maxX);
    }

    private (double minX, double maxX, double minY, double maxY) GetPanLimitsWithY()
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return (-double.MaxValue, double.MaxValue, -double.MaxValue, double.MaxValue);
        var baseRect = _transform.GetBaseImageRect();
        var cx = Bounds.Width / 2.0;
        var cy = Bounds.Height / 2.0;
        var scaledW = baseRect.Width * _zoom;
        var scaledH = baseRect.Height * _zoom;
        // Keep at least 25% of viewport visible or 80px, whichever is smaller
        var minVisibleX = Math.Min(80, Bounds.Width * 0.25);
        var minVisibleY = Math.Min(80, Bounds.Height * 0.25);
        var scaledLeftWithoutPan = (baseRect.Left - cx) * _zoom + cx;
        var scaledTopWithoutPan = (baseRect.Top - cy) * _zoom + cy;
        var scaledRightWithoutPan = scaledLeftWithoutPan + scaledW;
        var scaledBottomWithoutPan = scaledTopWithoutPan + scaledH;
        var vwLeft = 0.0;
        var vwRight = Bounds.Width;
        var vwTop = 0.0;
        var vwBottom = Bounds.Height;
        var minPanX = vwLeft + minVisibleX - scaledRightWithoutPan;
        var maxPanX = vwRight - minVisibleX - scaledLeftWithoutPan;
        var minPanY = vwTop + minVisibleY - scaledBottomWithoutPan;
        var maxPanY = vwBottom - minVisibleY - scaledTopWithoutPan;
        // If image smaller than viewport, keep centered — no panning
        if (scaledW <= Bounds.Width)
        {
            minPanX = maxPanX = 0;
        }
        if (scaledH <= Bounds.Height)
        {
            minPanY = maxPanY = 0;
        }
        return (minPanX, maxPanX, minPanY, maxPanY);
    }

    /// <summary>
    /// Resets the pan offset and zoom level to default (aspect fit).
    /// </summary>
    public void ResetPanZoom()
    {
        SetViewport(1.0, 0.0, 0.0);
    }

    /// <summary>
    /// Sets zoom and pan so the content fills the available viewport with uniform scaling.
    /// </summary>
    public void FitToCanvas()
    {
        ResetPanZoom();
    }

    public AmbitViewer()
    {
        ClipToBounds = true;
        Focusable = true;
        Background = Brushes.Transparent;
        _viewportAnimator = new ViewportAnimator(
            () => (_zoom, _panX, _panY),
            t => { var z = t.zoom; var x = t.panX; var y = t.panY; CoerceTarget(ref z, ref x, ref y); return (z, x, y); },
            (z, x, y, raise) => ApplyViewport(z, x, y, raise));
        _backgroundImageLayer = new BackgroundImageLayer(this);
        Children.Add(_backgroundImageLayer);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(0, 0);
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            size = new Size(Math.Max(size.Width, child.DesiredSize.Width), Math.Max(size.Height, child.DesiredSize.Height));
        }
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            child.Arrange(new Rect(finalSize));
        }

        UpdateTransformGeometry(finalSize);
        CoercePan();
        _viewportAnimator.SyncTargetToCurrent();
        UpdateContentTransform();
        NotifyTransformChanged();
        return finalSize;
    }

    private void UpdateTransformGeometry(Size finalSize)
    {
        double imgW, imgH;
        if (_backgroundImage != null)
        {
            imgW = _backgroundImage.Width;
            imgH = _backgroundImage.Height;
        }
        else if (_contentSize.HasValue && _contentSize.Value.Width > 0 && _contentSize.Value.Height > 0)
        {
            imgW = _contentSize.Value.Width;
            imgH = _contentSize.Value.Height;
        }
        else if (_content != null && _content.DesiredSize.Width > 0 && _content.DesiredSize.Height > 0)
        {
            imgW = _content.DesiredSize.Width;
            imgH = _content.DesiredSize.Height;
        }
        else
        {
            imgW = finalSize.Width > 0 ? finalSize.Width : 1;
            imgH = finalSize.Height > 0 ? finalSize.Height : 1;
        }

        _transform.Update(new Rect(finalSize), imgW, imgH);
        _transform.Zoom = _zoom;
        _transform.PanX = _panX;
        _transform.PanY = _panY;
    }

    private void ApplyViewport(double zoom, double panX, double panY, bool raiseChanged)
    {
        if (!double.IsFinite(zoom)) zoom = _zoom;
        _zoom = Math.Clamp(zoom, 1.0, 20.0);
        _panX = double.IsFinite(panX) ? panX : _panX;
        _panY = double.IsFinite(panY) ? panY : _panY;
        CoercePan();
        UpdateContentTransform();
        InvalidateVisual();
        NotifyTransformChanged();
        if (raiseChanged) PanZoomChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetViewport(double zoom, double panX, double panY, bool raiseChanged = true)
    {
        _viewportAnimator.Stop();
        ApplyViewport(zoom, panX, panY, raiseChanged);
        _viewportAnimator.SyncTargetToCurrent();
    }

    private void UpdateContentTransform()
    {
        if (_content == null) return;

        var baseRect = _transform.GetBaseImageRect();
        var contentW = _content.DesiredSize.Width > 0 ? _content.DesiredSize.Width : baseRect.Width;
        var contentH = _content.DesiredSize.Height > 0 ? _content.DesiredSize.Height : baseRect.Height;
        if (contentW <= 0 || contentH <= 0) return;

        var scaleX = (baseRect.Width * _zoom) / contentW;
        var scaleY = (baseRect.Height * _zoom) / contentH;

        var cx = Bounds.Width / 2.0;
        var cy = Bounds.Height / 2.0;
        var tx = (baseRect.Left - cx) * _zoom + cx + _panX;
        var ty = (baseRect.Top - cy) * _zoom + cy + _panY;

        _content.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
        _content.RenderTransform = new TransformGroup
        {
            Children =
            [
                new ScaleTransform(scaleX, scaleY),
                new TranslateTransform(tx, ty)
            ]
        };
    }

    private void NotifyTransformChanged()
    {
        _transform.Zoom = _zoom;
        _transform.PanX = _panX;
        _transform.PanY = _panY;

        _backgroundImageLayer?.InvalidateVisual();

        foreach (var child in Children)
        {
            if (child is AmbitLayer layer)
            {
                layer.OnViewerTransformChanged(_transform);
                layer.InvalidateVisual();
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;

        var isRightOrMiddle = properties.IsRightButtonPressed || properties.IsMiddleButtonPressed;

        if (_isPanZoomEnabled && isRightOrMiddle)
        {
            _viewportAnimator.Stop();
            _isPanning = true;
            _lastPanPoint = point;
            e.Handled = true;
            return;
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var point = e.GetPosition(this);

        if (_isPanning && _lastPanPoint.HasValue)
        {
            var deltaX = point.X - _lastPanPoint.Value.X;
            var deltaY = point.Y - _lastPanPoint.Value.Y;
            SetViewport(_zoom, _panX + deltaX, _panY + deltaY);
            _lastPanPoint = point;
            e.Handled = true;
            return;
        }

        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            _lastPanPoint = null;
            e.Handled = true;
            return;
        }

        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (!_isPanZoomEnabled)
        {
            return;
        }

        var point = e.GetPosition(this);
        var delta = e.Delta.Y;

        var oldZoom = _zoom;
        var normalizedUnderCursor = _transform.ToNormalizedSpace(new ControlPoint(point.X, point.Y));
        var newZoom = oldZoom * Math.Exp(delta * 0.16);
        newZoom = Math.Clamp(newZoom, 1.0, 20.0);

        if (Math.Abs(newZoom - oldZoom) > double.Epsilon)
        {
            var baseRect = _transform.GetBaseImageRect();
            var ctrlCenterX = Bounds.Width / 2.0;
            var ctrlCenterY = Bounds.Height / 2.0;
            var baseImgX = baseRect.Left + (normalizedUnderCursor.X * baseRect.Width);
            var baseImgY = baseRect.Top + (normalizedUnderCursor.Y * baseRect.Height);
            var nextPanX = point.X - (((baseImgX - ctrlCenterX) * newZoom) + ctrlCenterX);
            var nextPanY = point.Y - (((baseImgY - ctrlCenterY) * newZoom) + ctrlCenterY);
            AnimateViewportTo(newZoom, nextPanX, nextPanY);
            e.Handled = true;
        }
    }

    private void AnimateViewportTo(double zoom, double panX, double panY)
    {
        if (!double.IsFinite(zoom) || !double.IsFinite(panX) || !double.IsFinite(panY)) return;
        zoom = Math.Clamp(zoom, 1.0, 20.0);
        // Browser WASM: JS interop per animation tick is expensive — apply immediately for responsiveness.
        if (OperatingSystem.IsBrowser())
        {
            SetViewport(zoom, panX, panY);
            return;
        }
        _viewportAnimator.AnimateTo(zoom, panX, panY);
    }

    private void CoerceTarget(ref double z, ref double x, ref double y)
    {
        var curZ = _zoom; var curX = _panX; var curY = _panY;
        _zoom = z; _panX = x; _panY = y; CoercePan();
        z = _zoom; x = _panX; y = _panY;
        _zoom = curZ; _panX = curX; _panY = curY;
    }

    private sealed class BackgroundImageLayer : Control
    {
        private readonly AmbitViewer _viewer;
        private readonly DrawOperation _drawOperation;

        public BackgroundImageLayer(AmbitViewer viewer)
        {
            _viewer = viewer;
            _drawOperation = new DrawOperation(_viewer, this);
            ClipToBounds = true;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            context.Custom(_drawOperation);
        }

        private sealed class DrawOperation : ICustomDrawOperation
        {
            private readonly AmbitViewer _viewer;
            private readonly BackgroundImageLayer _layer;

            public DrawOperation(AmbitViewer viewer, BackgroundImageLayer layer)
            {
                _viewer = viewer;
                _layer = layer;
            }

            public Rect Bounds => new(_layer.Bounds.Size);
            public void Dispose() { }
            public bool Equals(ICustomDrawOperation? other) => false;
            public bool HitTest(Point p) => false;

            public void Render(ImmediateDrawingContext context)
            {
                var bitmap = _viewer.BackgroundImage;
                if (bitmap == null) return;

                var transform = _viewer.CoordinateTransform;
                if (transform == null) return;

                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature == null) return;

                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;
                canvas.Save();

                var tl = transform.ToControlSpace(new NormalizedPoint(0, 0));
                var br = transform.ToControlSpace(new NormalizedPoint(1, 1));
                var destRect = new SkiaSharp.SKRect((float)tl.X, (float)tl.Y, (float)br.X, (float)br.Y);
                canvas.DrawBitmap(bitmap, destRect);
                using var borderPaint = new SKPaint
                {
                    Color = new SKColor(0x94, 0xA3, 0xB8, 0xD0),
                    StrokeWidth = 1f,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                };
                canvas.DrawRect(destRect, borderPaint);

                canvas.Restore();
            }
        }
    }

    private sealed class PanZoomCoordinateTransform : ICoordinateTransform, IViewportTransformInfo, Rendering.RenderingUtilities.IViewportTransform
    {
        private Rect _bounds;
        private double _imageWidth = 1.0;
        private double _imageHeight = 1.0;

        public double ImageWidth => _imageWidth;
        public double ImageHeight => _imageHeight;

        public double Zoom { get; set; } = 1.0;
        public double PanX { get; set; } = 0.0;
        public double PanY { get; set; } = 0.0;

        public void Update(Rect bounds, double imageWidth, double imageHeight)
        {
            _bounds = bounds;
            _imageWidth = imageWidth > 0 ? imageWidth : 1.0;
            _imageHeight = imageHeight > 0 ? imageHeight : 1.0;
        }

        public Rect GetBaseImageRect()
        {
            var ctrlW = _bounds.Width;
            var ctrlH = _bounds.Height;

            var scale = Math.Min(ctrlW / _imageWidth, ctrlH / _imageHeight);
            var baseW = _imageWidth * scale;
            var baseH = _imageHeight * scale;

            var baseX = _bounds.Left + (ctrlW - baseW) / 2.0;
            var baseY = _bounds.Top + (ctrlH - baseH) / 2.0;

            return new Rect(baseX, baseY, baseW, baseH);
        }

        public Rect GetVisibleImageRect()
        {
            var baseRect = GetBaseImageRect();
            var cx = _bounds.Width / 2.0;
            var cy = _bounds.Height / 2.0;
            var left = ((baseRect.Left - cx) * Zoom) + cx + PanX;
            var top = ((baseRect.Top - cy) * Zoom) + cy + PanY;
            return new Rect(left, top, baseRect.Width * Zoom, baseRect.Height * Zoom);
        }

        public double ZoomFactor => Zoom;

        Rect Rendering.RenderingUtilities.IViewportTransform.GetVisibleImageRect() => GetVisibleImageRect();

        public ControlPoint ToControlSpace(NormalizedPoint p)
        {
            var baseRect = GetBaseImageRect();
            var baseImgX = baseRect.Left + (p.X * baseRect.Width);
            var baseImgY = baseRect.Top + (p.Y * baseRect.Height);

            var ctrlCenterX = _bounds.Width / 2.0;
            var ctrlCenterY = _bounds.Height / 2.0;

            var x = (baseImgX - ctrlCenterX) * Zoom + ctrlCenterX + PanX;
            var y = (baseImgY - ctrlCenterY) * Zoom + ctrlCenterY + PanY;

            return new ControlPoint(x, y);
        }

        public NormalizedPoint ToNormalizedSpace(ControlPoint p)
        {
            var baseRect = GetBaseImageRect();
            var ctrlCenterX = _bounds.Width / 2.0;
            var ctrlCenterY = _bounds.Height / 2.0;

            var baseImgX = (p.X - PanX - ctrlCenterX) / Zoom + ctrlCenterX;
            var baseImgY = (p.Y - PanY - ctrlCenterY) / Zoom + ctrlCenterY;

            var nx = baseRect.Width <= double.Epsilon ? 0d : (baseImgX - baseRect.Left) / baseRect.Width;
            var ny = baseRect.Height <= double.Epsilon ? 0d : (baseImgY - baseRect.Top) / baseRect.Height;

            return new NormalizedPoint(nx, ny);
        }
    }
}
