using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Ambit.Avalonia.Controls;

/// <summary>
/// An interactive Avalonia control that bridges pointer events to a <see cref="RegionEditController"/>
/// and renders the region collection using the shared overlay renderer.
/// </summary>
public sealed class RegionEditorControl : Control, IDisposable
{
    private readonly RegionEditController _controller;
    private readonly RegionOverlayRenderer _renderer;
    private readonly RegionOverlayDrawOperation _drawOperation;
    private readonly BoundsCoordinateTransform _boundsTransform = new();
    private int _contentVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegionEditorControl"/> class.
    /// </summary>
    /// <param name="controller">The interaction controller to drive.</param>
    /// <param name="renderer">An optional pre-configured renderer. When <see langword="null"/>,
    /// a default renderer with built-in region/decoration renderers is created.</param>
    public RegionEditorControl(RegionEditController controller, RegionOverlayRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(controller);

        _controller = controller;
        _renderer = renderer ?? new RegionOverlayRenderer();
        _drawOperation = new RegionOverlayDrawOperation(_renderer);
        ClipToBounds = true;
        Focusable = true;

        _controller.RenderStateChanged += OnControllerRenderStateChanged;
        _controller.CursorChanged += OnControllerCursorChanged;
        _controller.CellsChanged += OnControllerCellsChanged;
    }

    /// <summary>
    /// Gets the interaction controller managed by this control.
    /// </summary>
    public RegionEditController Controller => _controller;

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        _boundsTransform.Update(Bounds);
        _controller.CoordinateTransform ??= _boundsTransform;

        var regions = BuildRenderableRegionList();
        var state = _controller.BuildRenderState();

        _drawOperation.Update(
            new Rect(Bounds.Size),
            regions,
            state,
            _controller.CoordinateTransform,
            heatmap: null,
            ++_contentVersion,
            _controller.CellGrid);
        context.Custom(_drawOperation);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetPosition(this);
        _controller.OnPointerPressed(new ControlPoint(point.X, point.Y));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var point = e.GetPosition(this);
        _controller.OnPointerMoved(new ControlPoint(point.X, point.Y));
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var point = e.GetPosition(this);
        _controller.OnPointerReleased(new ControlPoint(point.X, point.Y));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            _controller.CancelActiveOperation();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _controller.RenderStateChanged -= OnControllerRenderStateChanged;
        _controller.CursorChanged -= OnControllerCursorChanged;
        _controller.CellsChanged -= OnControllerCellsChanged;
        _renderer.Dispose();
    }

    private IReadOnlyList<IRegion> BuildRenderableRegionList()
    {
        var drawing = _controller.DrawingRegion;
        if (drawing is null)
        {
            return _controller.Regions;
        }

        // Include the in-progress drawing region for preview.
        var list = new List<IRegion>(_controller.Regions.Count + 1);
        list.AddRange(_controller.Regions);
        list.Add(drawing);
        return list;
    }

    private void OnControllerRenderStateChanged(object? sender, EventArgs e)
    {
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

    private void OnControllerCellsChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private sealed class BoundsCoordinateTransform : ICoordinateTransform
    {
        private Rect _bounds;

        public void Update(Rect bounds)
        {
            _bounds = bounds;
        }

        public ControlPoint ToControlSpace(NormalizedPoint p)
        {
            return new ControlPoint(_bounds.Left + (p.X * _bounds.Width), _bounds.Top + (p.Y * _bounds.Height));
        }

        public NormalizedPoint ToNormalizedSpace(ControlPoint controlPoint)
        {
            var x = _bounds.Width <= double.Epsilon ? 0d : (controlPoint.X - _bounds.Left) / _bounds.Width;
            var y = _bounds.Height <= double.Epsilon ? 0d : (controlPoint.Y - _bounds.Top) / _bounds.Height;
            return new NormalizedPoint(x, y);
        }
    }
}
