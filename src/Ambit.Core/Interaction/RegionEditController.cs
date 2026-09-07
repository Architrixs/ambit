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
    private (int Row, int Col)? _hoveredCell;

    // Drag state
    private NormalizedPoint _dragStartNormalized;
    private NormalizedPoint _lastDragNormalized;
    private IEditableRegion? _dragRegion;
    private int _dragHandleIndex;

    // Drawing state
    private IEditableRegion? _drawingRegion;
    // For multi-vertex drawing (polygon/polyline): the committed vertices (not including preview)
    private List<NormalizedPoint>? _multiVertexPoints;
    // For multi-vertex drawing (polygon/polyline): true while we are in click-to-add mode
    private bool _isMultiVertexDraw;
    // Suppresses the background press that fires just before a double-tap commit
    private bool _suppressNextBackgroundPress;
    // Style captured at draw-start so rebuilds during drag keep it
    private RegionStyle? _drawingStyle;

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
    public double HandleGrabRadiusPixels { get; set; } = 12.0;

    /// <summary>
    /// Gets or sets the body hit-test tolerance in control pixels.
    /// Lines use a slightly larger tolerance because they're thin and hard to grab.
    /// </summary>
    public double BodyHitTolerancePixels { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets the default style used for newly drawn regions.
    /// Hosts can update this to make styling persistent across future drawings.
    /// </summary>
    public RegionStyle DefaultDrawStyle { get; set; } = AmbitConfiguration.Default.DefaultRegionStyle;

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
    /// Gets or sets an optional registry used to create custom region types during drawing.
    /// When set, <see cref="ActiveDrawTypeId"/> values not matching built-ins are resolved via this registry.
    /// </summary>
    public IRegionTypeRegistry? RegionTypeRegistry { get; set; }

    private string? _activeDrawTypeId;

    /// <summary>
    /// Gets or sets the type identifier for new regions to draw.
    /// When non-null, pointer presses on the background start a new-region draw flow
    /// instead of deselecting. Set to <see langword="null"/> to disable draw mode.
    /// Built-in types are handled directly; unknown types are resolved via <see cref="RegionTypeRegistry"/> if set.
    /// </summary>
    public string? ActiveDrawTypeId
    {
        get => _activeDrawTypeId;
        set
        {
            if (_activeDrawTypeId != value)
            {
                _activeDrawTypeId = value;
                if (State == RegionEditState.DrawingNewRegion)
                {
                    CancelActiveOperation();
                }
            }
        }
    }

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
            HoveredCell = _hoveredCell,
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
        var bodyToleranceNormalized = ComputeNormalizedTolerance(BodyHitTolerancePixels, transform);

        // Iterate in reverse so top-most (last-added) regions have priority.
        for (var regionIndex = _regions.Count - 1; regionIndex >= 0; regionIndex--)
        {
            var region = _regions[regionIndex];

            for (var decorationIndex = 0; decorationIndex < region.Decorations.Count; decorationIndex++)
            {
                var decoration = region.Decorations[decorationIndex];
                if (decoration.IsInteractive &&
                    IsWithinPixelRadius(controlPoint, decoration.Anchor, HandleGrabRadiusPixels, transform))
                {
                    return HitTestResult.DecorationHit(region, decoration, decorationIndex);
                }
            }

            var handles = region.GetHandles();
            for (var handleIndex = 0; handleIndex < handles.Count; handleIndex++)
            {
                var handle = handles[handleIndex];
                if (IsWithinPixelRadius(controlPoint, handle.Position, HandleGrabRadiusPixels, transform))
                {
                    return HitTestResult.Handle(region, handle.Index);
                }
            }

            var minimumLineTolerance = ComputeNormalizedTolerance(18.0, transform);
            var bodyTol = region.TypeId == LineRegion.LineTypeId || region.TypeId == PolylineRegion.PolylineTypeId
                ? Math.Max(bodyToleranceNormalized * 2.4, minimumLineTolerance)
                : bodyToleranceNormalized;
            if (region.HitTestBody(normalizedPoint, bodyTol))
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

        if (State == RegionEditState.DrawingNewRegion || ActiveDrawTypeId is not null || (IsCellPaintMode && CellGrid is not null))
        {
            HandleBackgroundPress(normalizedPoint);
            return;
        }

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
                // Multi-vertex shapes (polygon/polyline) stay in draw mode after each click.
                // Single-drag shapes (rect/ellipse/line) commit on release.
                if (!_isMultiVertexDraw)
                {
                    CommitDrawing();
                }
                // For multi-vertex drawing, stay in DrawingNewRegion — don't run UpdateHover.
                return;

            case RegionEditState.PaintingCells:
                CommitCellPaint();
                break;
        }

        // After commit, update hover for the release position.
        UpdateHover(controlPoint);
    }

    /// <summary>
    /// Processes a pointer-double-tapped event. Commits an in-progress multi-vertex draw.
    /// </summary>
    /// <param name="controlPoint">The double-tap position in control-pixel space.</param>
    public void OnPointerDoubleTapped(ControlPoint controlPoint)
    {
        if (State == RegionEditState.DrawingNewRegion && _isMultiVertexDraw)
        {
            // Suppress the background-press that was already fired as part of this double-tap gesture.
            _suppressNextBackgroundPress = true;
            CommitMultiVertexDrawing();
            UpdateHover(controlPoint);
        }
    }

    /// <summary>
    /// Deletes the currently selected region, if any.
    /// </summary>
    /// <returns><see langword="true"/> if a region was deleted; otherwise <see langword="false"/>.</returns>
    public bool DeleteSelected()
    {
        if (SelectedRegionId is null) return false;
        var idx = _regions.FindIndex(r => r.Id == SelectedRegionId);
        if (idx < 0) { SelectedRegionId = null; return false; }
        _regions.RemoveAt(idx);
        SelectedRegionId = null;
        HoveredRegionId = null;
        HoveredHandleIndex = null;
        State = RegionEditState.Idle;
        OnRegionsChanged();
        OnRenderStateChanged();
        return true;
    }

    /// <summary>
    /// Cancels any in-progress drag, draw, or paint operation and resets state to idle.
    /// </summary>
    public void CancelActiveOperation()
    {
        _dragRegion = null;
        _drawingRegion = null;
        _drawingStyle = null;
        _paintStroke = null;
        _isMultiVertexDraw = false;
        _multiVertexPoints = null;
        _suppressNextBackgroundPress = false;
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
    /// <param name="handleIndex">The handle index, used to determine corner diagonal for rect-style handles.</param>
    /// <returns>The cursor name.</returns>
    public static string ResolveCursorForHandle(string handleKind, int handleIndex)
    {
        return handleKind switch
        {
            "corner" => handleIndex switch
            {
                0 or 2 => "SizeNorthwestSoutheast",
                1 or 3 => "SizeNortheastSouthwest",
                _ => "SizeAll",
            },
            "vertex" => "Cross",
            // Custom handle kinds (e.g. sample's "edge-midpoint", "direction-toggle")
            // fall through to SizeAll — callers can override via custom mapping if needed.
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

        // Suppress duplicate press fired as part of a double-tap commit gesture.
        if (_suppressNextBackgroundPress)
        {
            _suppressNextBackgroundPress = false;
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
        var defaultStyle = DefaultDrawStyle;
        var typeId = ActiveDrawTypeId!;

        // Multi-vertex built-ins (polygon/polyline) have dedicated click-to-add flow.
        if (typeId == PolygonRegion.PolygonTypeId || typeId == PolylineRegion.PolylineTypeId)
        {
            if (_isMultiVertexDraw && _multiVertexPoints is not null)
            {
                _multiVertexPoints.Add(normalizedPoint);
                RebuildMultiVertexDrawingRegion(normalizedPoint, typeId, defaultStyle);
                return;
            }

            _multiVertexPoints = [normalizedPoint];
            _isMultiVertexDraw = true;
            _drawingStyle = defaultStyle;
            RebuildMultiVertexDrawingRegion(normalizedPoint, typeId, defaultStyle);
            _dragStartNormalized = normalizedPoint;
            State = RegionEditState.DrawingNewRegion;
            OnCursorChanged("Cross");
            OnRenderStateChanged();
            return;
        }

        // Unified drafting — all types (built-in + custom) go via factory.CreateDraft; fallback preserves old behavior.
        _drawingStyle = defaultStyle;
        _drawingRegion = TryCreateDraftViaRegistry(typeId, normalizedPoint, normalizedPoint, defaultStyle);
        if (_drawingRegion is null)
        {
            // Graceful fallback for tests/samples without registry set: keep last-resort built-ins.
            _drawingRegion = typeId switch
            {
                RectangleRegion.RectangleTypeId => new RectangleRegion(normalizedPoint, normalizedPoint, defaultStyle),
                EllipseRegion.EllipseTypeId => new EllipseRegion(normalizedPoint, normalizedPoint, defaultStyle),
                LineRegion.LineTypeId => new LineRegion(normalizedPoint, normalizedPoint, defaultStyle),
                _ => null,
            };
        }

        if (_drawingRegion is null) return;
        _isMultiVertexDraw = false;
        _dragStartNormalized = normalizedPoint;
        State = RegionEditState.DrawingNewRegion;
        OnCursorChanged("Cross");
        OnRenderStateChanged();
    }

    private IEditableRegion? TryCreateDraftViaRegistry(string typeId, NormalizedPoint a, NormalizedPoint b, RegionStyle style, Guid? id = null)
    {
        var registry = RegionTypeRegistry;
        if (registry is not null)
        {
            try
            {
                var factory = registry.GetRegionFactory(typeId);
                return factory.CreateDraft(id ?? Guid.NewGuid(), a, b, style, Array.Empty<IDecoration>());
            }
            catch { /* fall through to dto path */ }
        }
        // Fallback dto path (uses registry.CreateRegion if factory has no custom CreateDraft, else null)
        if (registry is null) return null;
        try
        {
            var dto = new RegionDto
            {
                Id = id ?? Guid.NewGuid(),
                TypeId = typeId,
                Vertices = new[] { a, b },
                Decorations = Array.Empty<DecorationDto>(),
                Style = style,
                Label = null,
                Properties = new Dictionary<string, string?>(),
            };
            return registry.CreateRegion(dto);
        }
        catch { return null; }
    }

    /// <summary>
    /// Rebuilds the <see cref="_drawingRegion"/> preview from <see cref="_multiVertexPoints"/>
    /// plus a live <paramref name="previewPoint"/> as the final, movable vertex.
    /// The preview region is constructed from (committed vertices + preview) so the shape is
    /// always valid and shows the live cursor feedback.
    /// </summary>
    private void RebuildMultiVertexDrawingRegion(NormalizedPoint previewPoint, string typeId, RegionStyle style)
    {
        if (_multiVertexPoints is null) return;

        // Combine committed vertices + preview.
        var pts = new NormalizedPoint[_multiVertexPoints.Count + 1];
        _multiVertexPoints.CopyTo(pts, 0);
        pts[pts.Length - 1] = previewPoint;

        if (typeId == PolygonRegion.PolygonTypeId)
        {
            // Polygon requires ≥3 vertices — pad with the preview if needed.
            while (pts.Length < 3)
            {
                var last = pts[pts.Length - 1];
                Array.Resize(ref pts, pts.Length + 1);
                pts[pts.Length - 1] = last;
            }
            _drawingRegion = new PolygonRegion(pts, style, _drawingRegion?.Id);
        }
        else
        {
            // Polyline requires ≥2 vertices.
            while (pts.Length < 2)
            {
                var last = pts[pts.Length - 1];
                Array.Resize(ref pts, pts.Length + 1);
                pts[pts.Length - 1] = last;
            }
            _drawingRegion = new PolylineRegion(pts, style, _drawingRegion?.Id);
        }

        if (State != RegionEditState.DrawingNewRegion)
        {
            _dragStartNormalized = previewPoint;
            State = RegionEditState.DrawingNewRegion;
            OnCursorChanged("Cross");
        }
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

        if (_isMultiVertexDraw && _multiVertexPoints is not null)
        {
            // Rebuild the preview region with committed vertices + current cursor as preview.
            var typeId = _drawingRegion.TypeId;
            var style = _drawingRegion.Style;
            RebuildMultiVertexDrawingRegion(normalizedPoint, typeId, style);
        }
        else
        {
            var style = _drawingRegion.Style;
            var id = _drawingRegion.Id;
            var typeId = _drawingRegion.TypeId;
            var draft = TryCreateDraftViaRegistry(typeId, _dragStartNormalized, normalizedPoint, style, id);
            _drawingRegion = draft ?? typeId switch
            {
                RectangleRegion.RectangleTypeId => new RectangleRegion(_dragStartNormalized, normalizedPoint, style, id),
                EllipseRegion.EllipseTypeId => new EllipseRegion(_dragStartNormalized, normalizedPoint, style, id),
                LineRegion.LineTypeId => new LineRegion(_dragStartNormalized, normalizedPoint, style, id),
                _ => _drawingRegion,
            };
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
            _hoveredCell = CellGrid?.HitTestCell(normalizedPoint);
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
            // Prevent committing zero-size or near-zero-size shapes — pixel-aware so zoom doesn't skew the threshold.
            var keep = true;
            if (_drawingRegion.Vertices.Count >= 2 && CoordinateTransform is not null)
            {
                var p0 = CoordinateTransform.ToControlSpace(_drawingRegion.Vertices[0]);
                var p1 = CoordinateTransform.ToControlSpace(_drawingRegion.Vertices[1]);
                var dx = p1.X - p0.X;
                var dy = p1.Y - p0.Y;
                var distPixels = Math.Sqrt(dx * dx + dy * dy);
                if (distPixels < 8.0) // ~8px minimum drag — ~click-and-release mistake at any zoom
                {
                    keep = false;
                }
            }
            else if (_drawingRegion.Vertices.Count >= 2)
            {
                // Fallback when no transform (tests) — keep old normalized check.
                var p0 = _drawingRegion.Vertices[0];
                var p1 = _drawingRegion.Vertices[1];
                var dx = p1.X - p0.X;
                var dy = p1.Y - p0.Y;
                if (Math.Sqrt(dx * dx + dy * dy) < 0.005) keep = false;
            }

            if (keep)
            {
                _regions.Add(_drawingRegion);
                SelectedRegionId = _drawingRegion.Id;
            }
            _drawingRegion = null;
        }

        _drawingStyle = null;
        _isMultiVertexDraw = false;
        _multiVertexPoints = null;
        State = RegionEditState.Idle;
        OnRegionsChanged();
        OnRenderStateChanged();
    }

    private void CommitMultiVertexDrawing()
    {
        IEditableRegion? committed = null;

        if (_drawingRegion is not null && _multiVertexPoints is not null)
        {
            var style = _drawingRegion.Style;
            var id = _drawingRegion.Id;

            // In a real double-click gesture, the second press adds a duplicate of the commit
            // position to _multiVertexPoints before DoubleTapped fires. Drop the last vertex
            // to remove that duplicate. In the case of a programmatic OnPointerDoubleTapped
            // call (e.g. Enter key or test code) there may be no duplicate — so only trim
            // when we have more vertices than needed.
            var pts = _multiVertexPoints.ToArray();
            if (pts.Length > (_drawingRegion is PolygonRegion ? 3 : 2))
            {
                // Check if the last two vertices are coincident (double-click duplicate).
                var last = pts[pts.Length - 1];
                var prev = pts[pts.Length - 2];
                const double epsilon = 1e-9;
                if (Math.Abs(last.X - prev.X) < epsilon && Math.Abs(last.Y - prev.Y) < epsilon)
                {
                    pts = pts.Take(pts.Length - 1).ToArray();
                }
            }

            if (_drawingRegion is PolygonRegion && pts.Length >= 3)
            {
                committed = new PolygonRegion(pts, style, id);
            }
            else if (_drawingRegion is PolylineRegion && pts.Length >= 2)
            {
                committed = new PolylineRegion(pts, style, id);
            }
        }

        if (committed is not null)
        {
            _regions.Add(committed);
            SelectedRegionId = committed.Id;
        }

        _drawingRegion = null;
        _multiVertexPoints = null;
        _drawingStyle = null;
        _isMultiVertexDraw = false;
        _suppressNextBackgroundPress = false;
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
        if (State == RegionEditState.DrawingNewRegion || ActiveDrawTypeId is not null || (IsCellPaintMode && CellGrid is not null))
        {
            var prevHover = HoveredRegionId;
            HoveredRegionId = null;
            HoveredHandleIndex = null;
            if (IsCellPaintMode && CellGrid != null && CoordinateTransform != null)
            {
                var norm = CoordinateTransform.ToNormalizedSpace(controlPoint);
                var cell = CellGrid.HitTestCell(norm);
                if (_hoveredCell != cell)
                {
                    _hoveredCell = cell;
                    OnRenderStateChanged();
                }
            }
            else if (_hoveredCell != null)
            {
                _hoveredCell = null;
                OnRenderStateChanged();
            }
            if (prevHover != null) OnRenderStateChanged();
            OnCursorChanged("Cross");
            return;
        }
        else if (_hoveredCell != null)
        {
            _hoveredCell = null;
            OnRenderStateChanged();
        }

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

    private static bool IsWithinPixelRadius(ControlPoint controlPoint, NormalizedPoint normalizedAnchor, double radiusPixels, ICoordinateTransform transform)
    {
        var anchorControl = transform.ToControlSpace(normalizedAnchor);
        var dx = controlPoint.X - anchorControl.X;
        var dy = controlPoint.Y - anchorControl.Y;
        return (dx * dx + dy * dy) <= radiusPixels * radiusPixels;
    }

    private static double ComputeNormalizedTolerance(double pixelRadius, ICoordinateTransform transform)
    {
        var origin = transform.ToNormalizedSpace(new ControlPoint(0, 0));
        var offset = transform.ToNormalizedSpace(new ControlPoint(pixelRadius, pixelRadius));
        var dx = Math.Abs(offset.X - origin.X);
        var dy = Math.Abs(offset.Y - origin.Y);
        return Math.Max(dx, dy);
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
