namespace Ambit;

/// <summary>
/// Manages the interaction state machine for editing regions, handles, decorations,
/// and cell grid painting. This class is fully testable without Avalonia — the consuming
/// control bridges pointer events from the UI framework into this controller.
/// </summary>
public sealed class RegionEditController
{
    private readonly List<IEditableRegion> _regions = [];
    private readonly HashSet<(int Row, int Col)> _selectedCells = [];

    // Drag state
    private NormalizedPoint _dragStartNormalized;
    private NormalizedPoint _lastDragNormalized;
    private IEditableRegion? _dragRegion;
    private int _dragHandleIndex;

    // Drawing state
    private IEditableRegion? _drawingRegion;

    // Cell paint state
    private CellSelectionStroke? _paintStroke;

    /// <summary>
    /// Raised when the region collection is mutated and the host should persist the result.
    /// Only raised on commit (pointer release), never during intermediate drag moves.
    /// </summary>
    public event EventHandler? RegionsChanged;

    /// <summary>
    /// Raised when transient render state (hover, selection, handle highlight) changes
    /// and the overlay should be redrawn.
    /// </summary>
    public event EventHandler? RenderStateChanged;

    /// <summary>
    /// Raised when the interaction state implies a different cursor.
    /// The string value is a cursor name (e.g. "Arrow", "SizeAll", "Hand", "Cross",
    /// "SizeNorthSouth", "SizeWestEast", "SizeNorthwestSoutheast", "SizeNortheastSouthwest").
    /// A <see langword="null"/> value means the default cursor.
    /// </summary>
    public event EventHandler<string?>? CursorChanged;

    /// <summary>
    /// Raised when the selected cell set changes during a paint stroke.
    /// </summary>
    public event EventHandler? CellsChanged;

    /// <summary>
    /// Gets the current state machine state.
    /// </summary>
    public RegionEditState State { get; private set; }

    /// <summary>
    /// Gets or sets the coordinate transform used to convert between control and normalized space.
    /// </summary>
    public ICoordinateTransform? CoordinateTransform { get; set; }

    /// <summary>
    /// Gets or sets the handle grab radius in control pixels.
    /// </summary>
    public double HandleGrabRadiusPixels { get; set; } = 8.0;

    /// <summary>
    /// Gets or sets the body hit-test tolerance in control pixels.
    /// </summary>
    public double BodyHitTolerancePixels { get; set; } = 4.0;

    /// <summary>
    /// Gets the identifier of the region currently under the pointer, if any.
    /// </summary>
    public Guid? HoveredRegionId { get; private set; }

    /// <summary>
    /// Gets the handle index currently under the pointer, if any.
    /// </summary>
    public int? HoveredHandleIndex { get; private set; }

    /// <summary>
    /// Gets or sets the identifier of the currently selected region, if any.
    /// </summary>
    public Guid? SelectedRegionId { get; set; }

    /// <summary>
    /// Gets the mutable selected cell set for grid interaction.
    /// </summary>
    public ISet<(int Row, int Col)> SelectedCells => _selectedCells;

    /// <summary>
    /// Gets the selected cells as a read-only set for rendering.
    /// </summary>
    public IReadOnlySet<(int Row, int Col)> SelectedCellsReadOnly => _selectedCells;

    /// <summary>
    /// Gets or sets the cell grid used for cell-paint interactions.
    /// </summary>
    public ICellGrid? CellGrid { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether cell-paint mode is active.
    /// When active, pointer presses on the grid background start a paint stroke
    /// instead of a region selection.
    /// </summary>
    public bool IsCellPaintMode { get; set; }

    /// <summary>
    /// Gets or sets the type identifier for new regions to draw.
    /// When non-null, pointer presses on the background start a new-region draw flow
    /// instead of deselecting. Set to <see langword="null"/> to disable draw mode.
    /// </summary>
    public string? ActiveDrawTypeId { get; set; }

    /// <summary>
    /// Gets the current region collection.
    /// </summary>
    public IReadOnlyList<IEditableRegion> Regions => _regions;

    /// <summary>
    /// Gets the in-progress region being drawn, if any.
    /// This is exposed so the rendering layer can show a preview during drawing.
    /// </summary>
    public IEditableRegion? DrawingRegion => _drawingRegion;

    /// <summary>
    /// Replaces the current region collection.
    /// </summary>
    /// <param name="regions">The new region set.</param>
    public void SetRegions(IEnumerable<IEditableRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        _regions.Clear();
        _regions.AddRange(regions);
    }

    /// <summary>
    /// Builds a <see cref="RegionRenderState"/> snapshot reflecting the current transient state.
    /// </summary>
    /// <returns>The current render state.</returns>
    public RegionRenderState BuildRenderState()
    {
        return new RegionRenderState
        {
            HoveredRegionId = HoveredRegionId,
            SelectedRegionId = SelectedRegionId,
            HoveredHandleIndex = HoveredHandleIndex,
            SelectedCells = _selectedCells.Count > 0 ? new HashSet<(int, int)>(_selectedCells) : null,
        };
    }

    /// <summary>
    /// Performs a priority-ordered hit test at the specified control-space point.
    /// Priority: interactive decorations → handles → region bodies → background.
    /// </summary>
    /// <param name="controlPoint">The point in control-pixel space.</param>
    /// <returns>The hit test result.</returns>
    public HitTestResult HitTest(ControlPoint controlPoint)
    {
        var transform = CoordinateTransform;
        if (transform is null)
        {
            return HitTestResult.Background();
        }

        var normalizedPoint = transform.ToNormalizedSpace(controlPoint);
        var handleRadiusNormalized = ComputeNormalizedTolerance(HandleGrabRadiusPixels, transform);
        var bodyToleranceNormalized = ComputeNormalizedTolerance(BodyHitTolerancePixels, transform);

        // Iterate in reverse so top-most (last-added) regions have priority.
        for (var regionIndex = _regions.Count - 1; regionIndex >= 0; regionIndex--)
        {
            var region = _regions[regionIndex];

            // 1. Check interactive decoration anchors.
            for (var decorationIndex = 0; decorationIndex < region.Decorations.Count; decorationIndex++)
            {
                var decoration = region.Decorations[decorationIndex];
                if (decoration.IsInteractive &&
                    GeometryUtilities.Distance(normalizedPoint, decoration.Anchor) <= handleRadiusNormalized)
                {
                    return HitTestResult.DecorationHit(region, decoration, decorationIndex);
                }
            }

            // 2. Check region handles.
            var handles = region.GetHandles();
            for (var handleIndex = 0; handleIndex < handles.Count; handleIndex++)
            {
                var handle = handles[handleIndex];
                if (GeometryUtilities.Distance(normalizedPoint, handle.Position) <= handleRadiusNormalized)
                {
                    return HitTestResult.Handle(region, handle.Index);
                }
            }

            // 3. Check region body.
            if (region.HitTestBody(normalizedPoint, bodyToleranceNormalized))
            {
                return HitTestResult.Body(region);
            }
        }

        return HitTestResult.Background();
    }

    /// <summary>
    /// Processes a pointer-pressed event at the specified control-space point.
    /// </summary>
    /// <param name="controlPoint">The pressed point in control-pixel space.</param>
    public void OnPointerPressed(ControlPoint controlPoint)
    {
        var transform = CoordinateTransform;
        if (transform is null)
        {
            return;
        }

        var normalizedPoint = transform.ToNormalizedSpace(controlPoint);
        var hit = HitTest(controlPoint);

        switch (hit.Kind)
        {
            case HitTestKind.Decoration:
                HandleDecorationPress(hit);
                break;

            case HitTestKind.Handle:
                BeginHandleDrag(hit, normalizedPoint);
                break;

            case HitTestKind.Body:
                BeginRegionDrag(hit, normalizedPoint);
                break;

            case HitTestKind.Background:
                HandleBackgroundPress(normalizedPoint);
                break;
        }
    }

    /// <summary>
    /// Processes a pointer-moved event at the specified control-space point.
    /// </summary>
    /// <param name="controlPoint">The current pointer position in control-pixel space.</param>
    public void OnPointerMoved(ControlPoint controlPoint)
    {
        var transform = CoordinateTransform;
        if (transform is null)
        {
            return;
        }

        var normalizedPoint = transform.ToNormalizedSpace(controlPoint);

        switch (State)
        {
            case RegionEditState.DraggingHandle:
                UpdateHandleDrag(normalizedPoint);
                break;

            case RegionEditState.DraggingRegion:
                UpdateRegionDrag(normalizedPoint);
                break;

            case RegionEditState.DrawingNewRegion:
                UpdateDrawing(normalizedPoint);
                break;

            case RegionEditState.PaintingCells:
                UpdateCellPaint(normalizedPoint);
                break;

            default:
                UpdateHover(controlPoint);
                break;
        }
    }

    /// <summary>
    /// Processes a pointer-released event at the specified control-space point.
    /// </summary>
    /// <param name="controlPoint">The release position in control-pixel space.</param>
    public void OnPointerReleased(ControlPoint controlPoint)
    {
        switch (State)
        {
            case RegionEditState.DraggingHandle:
            case RegionEditState.DraggingRegion:
                CommitDrag();
                break;

            case RegionEditState.DrawingNewRegion:
                CommitDrawing();
                break;

            case RegionEditState.PaintingCells:
                CommitCellPaint();
                break;
        }

        // After commit, update hover for the release position.
        UpdateHover(controlPoint);
    }

    /// <summary>
    /// Cancels any in-progress drag, draw, or paint operation and resets state to idle.
    /// </summary>
    public void CancelActiveOperation()
    {
        _dragRegion = null;
        _drawingRegion = null;
        _paintStroke = null;
        State = RegionEditState.Idle;
        HoveredRegionId = null;
        HoveredHandleIndex = null;
        OnCursorChanged(null);
        OnRenderStateChanged();
    }

    /// <summary>
    /// Resolves the appropriate cursor name for a given handle kind.
    /// </summary>
    /// <param name="handleKind">The handle kind string.</param>
    /// <param name="handleIndex">The handle index, used to determine corner/edge for rect-style handles.</param>
    /// <returns>The cursor name.</returns>
    public static string ResolveCursorForHandle(string handleKind, int handleIndex)
    {
        return handleKind switch
        {
            "corner" => handleIndex switch
            {
                0 or 2 => "SizeNorthwestSoutheast", // TopLeft / BottomRight
                1 or 3 => "SizeNortheastSouthwest", // TopRight / BottomLeft
                _ => "SizeAll",
            },
            "edge-midpoint" => handleIndex switch
            {
                4 or 6 => "SizeNorthSouth",  // Top / Bottom
                5 or 7 => "SizeWestEast",    // Right / Left
                _ => "SizeAll",
            },
            "vertex" => "Cross",
            _ => "SizeAll",
        };
    }

    private void HandleDecorationPress(HitTestResult hit)
    {
        // Toggle decoration immediately on click — no drag needed.
        if (hit.Decoration is IToggleDecoration toggle)
        {
            toggle.Toggle();
        }

        // Select the owning region.
        SelectedRegionId = hit.Region!.Id;
        OnRegionsChanged();
        OnRenderStateChanged();
    }

    private void BeginHandleDrag(HitTestResult hit, NormalizedPoint normalizedPoint)
    {
        _dragRegion = hit.Region;
        _dragHandleIndex = hit.HandleIndex!.Value;
        _dragStartNormalized = normalizedPoint;
        _lastDragNormalized = normalizedPoint;
        SelectedRegionId = hit.Region!.Id;
        State = RegionEditState.DraggingHandle;
        OnRenderStateChanged();
    }

    private void BeginRegionDrag(HitTestResult hit, NormalizedPoint normalizedPoint)
    {
        _dragRegion = hit.Region;
        _dragStartNormalized = normalizedPoint;
        _lastDragNormalized = normalizedPoint;
        SelectedRegionId = hit.Region!.Id;
        State = RegionEditState.DraggingRegion;
        OnCursorChanged("SizeAll");
        OnRenderStateChanged();
    }

    private void HandleBackgroundPress(NormalizedPoint normalizedPoint)
    {
        // Cell paint mode takes priority over draw mode.
        if (IsCellPaintMode && CellGrid is not null)
        {
            _paintStroke = new CellSelectionStroke(CellGrid, _selectedCells, normalizedPoint);
            State = RegionEditState.PaintingCells;
            OnRenderStateChanged();
            return;
        }

        if (ActiveDrawTypeId is not null)
        {
            BeginDrawing(normalizedPoint);
            return;
        }

        // Clicking on background deselects.
        if (SelectedRegionId is not null)
        {
            SelectedRegionId = null;
            OnRenderStateChanged();
        }
    }

    private void BeginDrawing(NormalizedPoint normalizedPoint)
    {
        // Create a minimal initial region based on type.
        // For two-point types (rectangle, ellipse, line) we use the press point as both corners.
        // For multi-vertex types (polygon, polyline) we start with the minimum vertex count.
        var defaultStyle = new RegionStyle { StrokeColorHex = "#2680EB" };
        var typeId = ActiveDrawTypeId!;

        _drawingRegion = typeId switch
        {
            RectangleRegion.RectangleTypeId => new RectangleRegion(normalizedPoint, normalizedPoint, defaultStyle),
            EllipseRegion.EllipseTypeId => new EllipseRegion(normalizedPoint, normalizedPoint, defaultStyle),
            LineRegion.LineTypeId => new LineRegion(normalizedPoint, normalizedPoint, defaultStyle),
            PolygonRegion.PolygonTypeId => new PolygonRegion(
                [normalizedPoint, normalizedPoint, normalizedPoint],
                defaultStyle),
            PolylineRegion.PolylineTypeId => new PolylineRegion(
                [normalizedPoint, normalizedPoint],
                defaultStyle),
            _ => null,
        };

        if (_drawingRegion is null)
        {
            return;
        }

        _dragStartNormalized = normalizedPoint;
        State = RegionEditState.DrawingNewRegion;
        OnCursorChanged("Cross");
        OnRenderStateChanged();
    }

    private void UpdateHandleDrag(NormalizedPoint normalizedPoint)
    {
        _dragRegion?.MoveHandle(_dragHandleIndex, normalizedPoint);
        _lastDragNormalized = normalizedPoint;
        OnRenderStateChanged();
    }

    private void UpdateRegionDrag(NormalizedPoint normalizedPoint)
    {
        if (_dragRegion is null)
        {
            return;
        }

        var delta = new NormalizedVector(
            normalizedPoint.X - _lastDragNormalized.X,
            normalizedPoint.Y - _lastDragNormalized.Y);
        _dragRegion.Translate(delta);
        _lastDragNormalized = normalizedPoint;
        OnRenderStateChanged();
    }

    private void UpdateDrawing(NormalizedPoint normalizedPoint)
    {
        if (_drawingRegion is null)
        {
            return;
        }

        // Move the second point/corner to the current position.
        // For two-vertex types this is handle index 1.
        // For polygon (3 vertices), move vertex 2 (bottom-right of the initial triangle).
        var handles = _drawingRegion.GetHandles();
        if (handles.Count >= 2)
        {
            _drawingRegion.MoveHandle(handles.Count - 1, normalizedPoint);
        }

        OnRenderStateChanged();
    }

    private void UpdateCellPaint(NormalizedPoint normalizedPoint)
    {
        if (_paintStroke is null)
        {
            return;
        }

        if (_paintStroke.Visit(normalizedPoint))
        {
            OnCellsChanged();
            OnRenderStateChanged();
        }
    }

    private void CommitDrag()
    {
        _dragRegion = null;
        State = RegionEditState.Idle;
        OnRegionsChanged();
    }

    private void CommitDrawing()
    {
        if (_drawingRegion is not null)
        {
            _regions.Add(_drawingRegion);
            SelectedRegionId = _drawingRegion.Id;
            _drawingRegion = null;
        }

        State = RegionEditState.Idle;
        OnRegionsChanged();
        OnRenderStateChanged();
    }

    private void CommitCellPaint()
    {
        _paintStroke = null;
        State = RegionEditState.Idle;
        OnCellsChanged();
    }

    private void UpdateHover(ControlPoint controlPoint)
    {
        var hit = HitTest(controlPoint);
        var previousHoveredRegionId = HoveredRegionId;
        var previousHoveredHandleIndex = HoveredHandleIndex;

        switch (hit.Kind)
        {
            case HitTestKind.Decoration:
                HoveredRegionId = hit.Region!.Id;
                HoveredHandleIndex = null;
                State = RegionEditState.Hover;
                OnCursorChanged("Hand");
                break;

            case HitTestKind.Handle:
                HoveredRegionId = hit.Region!.Id;
                HoveredHandleIndex = hit.HandleIndex;
                State = RegionEditState.Hover;
                var handles = hit.Region.GetHandles();
                var handle = handles.FirstOrDefault(h => h.Index == hit.HandleIndex);
                OnCursorChanged(ResolveCursorForHandle(handle.HandleKind, handle.Index));
                break;

            case HitTestKind.Body:
                HoveredRegionId = hit.Region!.Id;
                HoveredHandleIndex = null;
                State = RegionEditState.Hover;
                OnCursorChanged("SizeAll");
                break;

            default:
                HoveredRegionId = null;
                HoveredHandleIndex = null;
                State = ActiveDrawTypeId is not null
                    ? RegionEditState.Hover
                    : RegionEditState.Idle;
                OnCursorChanged(ActiveDrawTypeId is not null ? "Cross" : null);
                break;
        }

        if (HoveredRegionId != previousHoveredRegionId || HoveredHandleIndex != previousHoveredHandleIndex)
        {
            OnRenderStateChanged();
        }
    }

    private static double ComputeNormalizedTolerance(double pixelRadius, ICoordinateTransform transform)
    {
        // Convert a pixel radius to an approximate normalized tolerance.
        // We use two points separated by the pixel radius to determine the scale.
        var origin = transform.ToNormalizedSpace(new ControlPoint(0, 0));
        var offset = transform.ToNormalizedSpace(new ControlPoint(pixelRadius, pixelRadius));
        var dx = Math.Abs(offset.X - origin.X);
        var dy = Math.Abs(offset.Y - origin.Y);

        // Use the average of horizontal and vertical scales for a reasonable approximation.
        return (dx + dy) / 2.0;
    }

    private void OnRegionsChanged()
    {
        RegionsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnRenderStateChanged()
    {
        RenderStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCursorChanged(string? cursorName)
    {
        CursorChanged?.Invoke(this, cursorName);
    }

    private void OnCellsChanged()
    {
        CellsChanged?.Invoke(this, EventArgs.Empty);
    }
}
