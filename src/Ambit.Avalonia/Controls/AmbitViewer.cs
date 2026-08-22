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
    /// Gets or sets the current zoom level.
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            var clamped = Math.Clamp(value, 0.1, 20.0);
            if (Math.Abs(_zoom - clamped) > double.Epsilon)
            {
                _zoom = clamped;
                CoercePan();
                UpdateContentTransform();
                InvalidateVisual();
                NotifyTransformChanged();
                PanZoomChanged?.Invoke(this, EventArgs.Empty);
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
                _panX = coerced;
                UpdateContentTransform();
                InvalidateVisual();
                NotifyTransformChanged();
                PanZoomChanged?.Invoke(this, EventArgs.Empty);
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
                _panY = coerced;
                UpdateContentTransform();
                InvalidateVisual();
                NotifyTransformChanged();
                PanZoomChanged?.Invoke(this, EventArgs.Empty);
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
        _zoom = 1.0;
        _panX = 0.0;
        _panY = 0.0;
        UpdateContentTransform();
        InvalidateVisual();
        NotifyTransformChanged();
        PanZoomChanged?.Invoke(this, EventArgs.Empty);
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
            // Allow elastic overshoot — resistance when beyond limits, bounce back on release
            var desiredX = _panX + deltaX;
            var desiredY = _panY + deltaY;
            var (minX, maxX, minY, maxY) = GetPanLimitsWithY();
            if (desiredX < minX) desiredX = minX + (desiredX - minX) * 0.35;
            else if (desiredX > maxX) desiredX = maxX + (desiredX - maxX) * 0.35;
            if (desiredY < minY) desiredY = minY + (desiredY - minY) * 0.35;
            else if (desiredY > maxY) desiredY = maxY + (desiredY - maxY) * 0.35;
            _panX = desiredX;
            _panY = desiredY;
            UpdateContentTransform();
            InvalidateVisual();
            NotifyTransformChanged();
            PanZoomChanged?.Invoke(this, EventArgs.Empty);
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
            // Bounce back if overshot
            var (minX, maxX, minY, maxY) = GetPanLimitsWithY();
            var targetX = Math.Clamp(_panX, minX, maxX);
            var targetY = Math.Clamp(_panY, minY, maxY);
            if (Math.Abs(targetX - _panX) > 0.5 || Math.Abs(targetY - _panY) > 0.5)
            {
                AnimatePanTo(targetX, targetY);
            }
            e.Handled = true;
            return;
        }

        base.OnPointerReleased(e);
    }

    private async void AnimatePanTo(double targetX, double targetY)
    {
        var startX = _panX;
        var startY = _panY;
        var durationMs = 220;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < durationMs)
        {
            var t = sw.ElapsedMilliseconds / (double)durationMs;
            // Ease-out cubic
            t = 1 - Math.Pow(1 - t, 3);
            _panX = startX + (targetX - startX) * t;
            _panY = startY + (targetY - startY) * t;
            UpdateContentTransform();
            InvalidateVisual();
            NotifyTransformChanged();
            await System.Threading.Tasks.Task.Delay(16);
        }
        _panX = targetX;
        _panY = targetY;
        UpdateContentTransform();
        InvalidateVisual();
        NotifyTransformChanged();
        PanZoomChanged?.Invoke(this, EventArgs.Empty);
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

        var zoomFactor = 1.08;
        var oldZoom = _zoom;
        var newZoom = delta > 0 ? oldZoom * zoomFactor : oldZoom / zoomFactor;

        newZoom = Math.Clamp(newZoom, 0.1, 20.0);

        if (Math.Abs(newZoom - oldZoom) > double.Epsilon)
        {
            var ctrlCenterX = Bounds.Width / 2.0;
            var ctrlCenterY = Bounds.Height / 2.0;

            var baseImgX = (point.X - _panX - ctrlCenterX) / oldZoom + ctrlCenterX;
            var baseImgY = (point.Y - _panY - ctrlCenterY) / oldZoom + ctrlCenterY;

            PanX += (baseImgX - ctrlCenterX) * (oldZoom - newZoom);
            PanY += (baseImgY - ctrlCenterY) * (oldZoom - newZoom);
            Zoom = newZoom;

            e.Handled = true;
        }
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

                canvas.Restore();
            }
        }
    }

    private sealed class PanZoomCoordinateTransform : ICoordinateTransform
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
