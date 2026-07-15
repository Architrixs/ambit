using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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
                UpdateContentTransform();
                InvalidateVisual();
                NotifyTransformChanged();
                PanZoomChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the X pan offset in pixels.
    /// </summary>
    public double PanX
    {
        get => _panX;
        set
        {
            if (Math.Abs(_panX - value) > double.Epsilon)
            {
                _panX = value;
                UpdateContentTransform();
                InvalidateVisual();
                NotifyTransformChanged();
                PanZoomChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the Y pan offset in pixels.
    /// </summary>
    public double PanY
    {
        get => _panY;
        set
        {
            if (Math.Abs(_panY - value) > double.Epsilon)
            {
                _panY = value;
                UpdateContentTransform();
                InvalidateVisual();
                NotifyTransformChanged();
                PanZoomChanged?.Invoke(this, EventArgs.Empty);
            }
        }
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
        foreach (var child in Children)
        {
            if (child is AmbitLayer layer)
            {
                layer.OnViewerTransformChanged(_transform);
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
            PanX += deltaX;
            PanY += deltaY;
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

    private sealed class PanZoomCoordinateTransform : ICoordinateTransform
    {
        private Rect _bounds;
        private double _imageWidth = 1.0;
        private double _imageHeight = 1.0;

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
