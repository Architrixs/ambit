using Xunit;

namespace Ambit.Core.Tests;

/// <summary>
/// A simple identity coordinate transform where normalized space equals control pixel space (1:1 mapping over [0,1]).
/// </summary>
internal sealed class IdentityTransform : ICoordinateTransform
{
    /// <summary>
    /// Gets a shared identity transform instance scaled to 1000×1000 control pixels.
    /// </summary>
    public static IdentityTransform Default { get; } = new(1000, 1000);

    private readonly double _width;
    private readonly double _height;

    public IdentityTransform(double width, double height)
    {
        _width = width;
        _height = height;
    }

    public ControlPoint ToControlSpace(NormalizedPoint p) =>
        new(p.X * _width, p.Y * _height);

    public NormalizedPoint ToNormalizedSpace(ControlPoint point) =>
        new(point.X / _width, point.Y / _height);
}

public class RegionEditControllerTests
{
    private static readonly RegionStyle DefaultStyle = new() { StrokeColorHex = "#FF0000" };

    private static RegionEditController CreateController(params IEditableRegion[] regions)
    {
        var controller = new RegionEditController
        {
            CoordinateTransform = IdentityTransform.Default,
        };
        controller.SetRegions(regions);
        return controller;
    }

    private static RectangleRegion CreateRect(double left = 0.2, double top = 0.2, double right = 0.8, double bottom = 0.8)
    {
        return new RectangleRegion(
            new NormalizedPoint(left, top),
            new NormalizedPoint(right, bottom),
            DefaultStyle);
    }

    #region State machine basics

    [Fact]
    public void InitialState_IsIdle()
    {
        var controller = CreateController();
        Assert.Equal(RegionEditState.Idle, controller.State);
    }

    [Fact]
    public void SetRegions_PopulatesRegionsList()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);
        Assert.Single(controller.Regions);
        Assert.Same(rect, controller.Regions[0]);
    }

    [Fact]
    public void CancelActiveOperation_ResetsToIdle()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        // Start a drag, then cancel.
        var center = new ControlPoint(500, 500);
        controller.OnPointerPressed(center);
        Assert.Equal(RegionEditState.DraggingRegion, controller.State);

        controller.CancelActiveOperation();
        Assert.Equal(RegionEditState.Idle, controller.State);
    }

    #endregion

    #region Hover and cursor tracking

    [Fact]
    public void Hover_OverRegionBody_SetsHoverState()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        controller.OnPointerMoved(new ControlPoint(500, 500));
        Assert.Equal(RegionEditState.Hover, controller.State);
        Assert.Equal(rect.Id, controller.HoveredRegionId);
        Assert.Null(controller.HoveredHandleIndex);
    }

    [Fact]
    public void Hover_OverHandle_SetsHoveredHandleIndex()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        // Top-left corner handle is at (200, 200) in 1000×1000 control space.
        controller.OnPointerMoved(new ControlPoint(200, 200));
        Assert.Equal(RegionEditState.Hover, controller.State);
        Assert.Equal(rect.Id, controller.HoveredRegionId);
        Assert.Equal(0, controller.HoveredHandleIndex); // TopLeft handle index
    }

    [Fact]
    public void Hover_OverBackground_SetsIdle()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        controller.OnPointerMoved(new ControlPoint(50, 50));
        Assert.Equal(RegionEditState.Idle, controller.State);
        Assert.Null(controller.HoveredRegionId);
    }

    [Fact]
    public void Hover_CursorChanged_RaisedWithCorrectCursor()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);
        string? lastCursor = null;
        controller.CursorChanged += (_, cursor) => lastCursor = cursor;

        // Hover over body.
        controller.OnPointerMoved(new ControlPoint(500, 500));
        Assert.Equal("SizeAll", lastCursor);

        // Hover over background.
        controller.OnPointerMoved(new ControlPoint(50, 50));
        Assert.Null(lastCursor);
    }


    #endregion

    #region Hit test ordering


    [Fact]
    public void HitTest_Priority_HandleBeforeBody()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        // Hit test at a corner handle (which is on the body boundary).
        var result = controller.HitTest(new ControlPoint(200, 200));
        Assert.Equal(HitTestKind.Handle, result.Kind);
    }

    [Fact]
    public void HitTest_LastRegion_HasPriority()
    {
        // Two overlapping rectangles — the second (last-added) should be hit first.
        var rect1 = CreateRect(0.1, 0.1, 0.9, 0.9);
        var rect2 = CreateRect(0.2, 0.2, 0.8, 0.8);
        var controller = CreateController(rect1, rect2);

        var result = controller.HitTest(new ControlPoint(500, 500));
        Assert.Equal(HitTestKind.Body, result.Kind);
        Assert.Same(rect2, result.Region);
    }

    #endregion

    #region Region selection and body drag

    [Fact]
    public void Press_OnBody_SelectsRegion()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        controller.OnPointerPressed(new ControlPoint(500, 500));
        Assert.Equal(rect.Id, controller.SelectedRegionId);
    }

    [Fact]
    public void Press_OnBackground_DeselectsRegion()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        controller.OnPointerPressed(new ControlPoint(500, 500));
        Assert.Equal(rect.Id, controller.SelectedRegionId);

        controller.OnPointerReleased(new ControlPoint(500, 500));
        controller.OnPointerPressed(new ControlPoint(50, 50));
        Assert.Null(controller.SelectedRegionId);
    }

    [Fact]
    public void Drag_RegionBody_TranslatesRegion()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        var originalLeft = rect.Bounds.Left;
        var originalTop = rect.Bounds.Top;

        // Press at center, drag right.
        controller.OnPointerPressed(new ControlPoint(500, 500));
        Assert.Equal(RegionEditState.DraggingRegion, controller.State);

        controller.OnPointerMoved(new ControlPoint(600, 500));
        Assert.True(rect.Bounds.Left > originalLeft, "Region should move right");
        Assert.Equal(originalTop, rect.Bounds.Top, 5); // Y unchanged

        controller.OnPointerReleased(new ControlPoint(600, 500));
        // After release, UpdateHover runs — pointer is still over the moved region.
        Assert.True(
            controller.State is RegionEditState.Idle or RegionEditState.Hover,
            "State should be Idle or Hover after drag release");
    }

    [Fact]
    public void Drag_RegionBody_RaisesRegionsChangedOnRelease()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        var changedCount = 0;
        controller.RegionsChanged += (_, _) => changedCount++;

        controller.OnPointerPressed(new ControlPoint(500, 500));
        controller.OnPointerMoved(new ControlPoint(550, 500));
        Assert.Equal(0, changedCount); // No change events during drag.

        controller.OnPointerReleased(new ControlPoint(550, 500));
        Assert.Equal(1, changedCount); // Exactly one on commit.
    }

    [Fact]
    public void Drag_RegionBody_RaisesRenderStateChangedDuringDrag()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        var renderStateChangedCount = 0;
        controller.RenderStateChanged += (_, _) => renderStateChangedCount++;

        controller.OnPointerPressed(new ControlPoint(500, 500));
        var afterPress = renderStateChangedCount;

        controller.OnPointerMoved(new ControlPoint(550, 500));
        Assert.True(renderStateChangedCount > afterPress, "RenderStateChanged should fire during drag for live feedback");
    }

    #endregion

    #region Handle drag

    [Fact]
    public void Drag_Handle_ResizesRegion()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        var originalRight = rect.Bounds.Right;

        // Bottom-right corner handle is at (800, 800) in 1000x1000.
        controller.OnPointerPressed(new ControlPoint(800, 800));
        Assert.Equal(RegionEditState.DraggingHandle, controller.State);

        // Drag it to expand.
        controller.OnPointerMoved(new ControlPoint(900, 900));
        Assert.True(rect.Bounds.Right > originalRight, "Right edge should expand");

        controller.OnPointerReleased(new ControlPoint(900, 900));
        // After release, UpdateHover runs — pointer is still over the resized region.
        Assert.True(
            controller.State is RegionEditState.Idle or RegionEditState.Hover,
            "State should be Idle or Hover after handle drag release");
    }

    [Fact]
    public void Drag_Handle_RaisesRegionsChangedOnlyOnRelease()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        var changedCount = 0;
        controller.RegionsChanged += (_, _) => changedCount++;

        controller.OnPointerPressed(new ControlPoint(800, 800));
        controller.OnPointerMoved(new ControlPoint(850, 850));
        Assert.Equal(0, changedCount);

        controller.OnPointerReleased(new ControlPoint(850, 850));
        Assert.Equal(1, changedCount);
    }

    #endregion



    #region New region drawing

    [Fact]
    public void Draw_Rectangle_CreatesRegionOnRelease()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId;

        var changedCount = 0;
        controller.RegionsChanged += (_, _) => changedCount++;

        // Press at (200, 200) to start drawing.
        controller.OnPointerPressed(new ControlPoint(200, 200));
        Assert.Equal(RegionEditState.DrawingNewRegion, controller.State);
        Assert.NotNull(controller.DrawingRegion);

        // Drag to (600, 600).
        controller.OnPointerMoved(new ControlPoint(600, 600));

        // Release commits the region.
        controller.OnPointerReleased(new ControlPoint(600, 600));
        // After release, UpdateHover runs — pointer is over the newly created region.
        Assert.True(
            controller.State is RegionEditState.Idle or RegionEditState.Hover,
            "State should be Idle or Hover after draw commit");
        Assert.Single(controller.Regions);
        Assert.Equal(RectangleRegion.RectangleTypeId, controller.Regions[0].TypeId);
        Assert.Equal(1, changedCount);
    }

    [Fact]
    public void Draw_Line_CreatesLineRegion()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = LineRegion.LineTypeId;

        controller.OnPointerPressed(new ControlPoint(100, 500));
        controller.OnPointerMoved(new ControlPoint(900, 500));
        controller.OnPointerReleased(new ControlPoint(900, 500));

        Assert.Single(controller.Regions);
        Assert.Equal(LineRegion.LineTypeId, controller.Regions[0].TypeId);
    }

    [Fact]
    public void Draw_Ellipse_CreatesEllipseRegion()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = EllipseRegion.EllipseTypeId;

        controller.OnPointerPressed(new ControlPoint(200, 200));
        controller.OnPointerMoved(new ControlPoint(800, 800));
        controller.OnPointerReleased(new ControlPoint(800, 800));

        Assert.Single(controller.Regions);
        Assert.Equal(EllipseRegion.EllipseTypeId, controller.Regions[0].TypeId);
    }

    [Fact]
    public void Draw_Rectangle_ClickAndRelease_DoesNotCreateRegion()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId;

        controller.OnPointerPressed(new ControlPoint(200, 200));
        controller.OnPointerReleased(new ControlPoint(200, 200));

        Assert.Empty(controller.Regions);
        Assert.Equal(RegionEditState.Idle, controller.State);
    }

    [Fact]
    public void Draw_Polygon_ClickBackgroundThenClickHandle_CancelsDrawing()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);
        controller.ActiveDrawTypeId = PolygonRegion.PolygonTypeId;

        controller.OnPointerPressed(new ControlPoint(100, 100));
        Assert.Equal(RegionEditState.DrawingNewRegion, controller.State);
        Assert.NotNull(controller.DrawingRegion);

        controller.OnPointerPressed(new ControlPoint(200, 200));

        Assert.Null(controller.DrawingRegion);
        Assert.Equal(RegionEditState.DraggingHandle, controller.State);
    }

    [Fact]
    public void Draw_Polygon_CreatesPolygonRegionOnDoubleTap()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = PolygonRegion.PolygonTypeId;

        var changedCount = 0;
        controller.RegionsChanged += (_, _) => changedCount++;

        // Click 1: start at p1.
        controller.OnPointerPressed(new ControlPoint(200, 200));
        Assert.Equal(RegionEditState.DrawingNewRegion, controller.State);
        controller.OnPointerReleased(new ControlPoint(200, 200));

        // Move to p2, click 2: add a vertex.
        controller.OnPointerMoved(new ControlPoint(800, 200));
        controller.OnPointerPressed(new ControlPoint(800, 200));
        controller.OnPointerReleased(new ControlPoint(800, 200));

        // Move to p3, click 3: add a vertex.
        controller.OnPointerMoved(new ControlPoint(500, 800));
        controller.OnPointerPressed(new ControlPoint(500, 800));
        controller.OnPointerReleased(new ControlPoint(500, 800));

        // Double-tap commits (second press of double-click already happened in the OS,
        // so we simulate the DoubleTapped event directly).
        controller.OnPointerDoubleTapped(new ControlPoint(500, 800));

        Assert.Single(controller.Regions);
        Assert.Equal(PolygonRegion.PolygonTypeId, controller.Regions[0].TypeId);
        Assert.Equal(1, changedCount);
    }

    [Fact]
    public void Draw_Polygon_CreatesPolygonRegion()
    {
        // Legacy: single press+move+release no longer commits for polygon.
        // After one press the drawing region exists but is not yet committed.
        var controller = CreateController();
        controller.ActiveDrawTypeId = PolygonRegion.PolygonTypeId;

        controller.OnPointerPressed(new ControlPoint(200, 200));
        Assert.Equal(RegionEditState.DrawingNewRegion, controller.State);
        // Release does NOT commit multi-vertex shapes.
        controller.OnPointerReleased(new ControlPoint(800, 800));
        Assert.Empty(controller.Regions);
        Assert.Equal(RegionEditState.DrawingNewRegion, controller.State);
    }

    [Fact]
    public void Draw_Polyline_CreatesPolylineRegionOnDoubleTap()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = PolylineRegion.PolylineTypeId;

        var changedCount = 0;
        controller.RegionsChanged += (_, _) => changedCount++;

        // Click 1: start.
        controller.OnPointerPressed(new ControlPoint(100, 500));
        controller.OnPointerReleased(new ControlPoint(100, 500));

        // Click 2: add second vertex.
        controller.OnPointerMoved(new ControlPoint(500, 500));
        controller.OnPointerPressed(new ControlPoint(500, 500));
        controller.OnPointerReleased(new ControlPoint(500, 500));

        // Click 3: add third vertex.
        controller.OnPointerMoved(new ControlPoint(900, 500));
        controller.OnPointerPressed(new ControlPoint(900, 500));
        controller.OnPointerReleased(new ControlPoint(900, 500));

        controller.OnPointerDoubleTapped(new ControlPoint(900, 500));

        Assert.Single(controller.Regions);
        Assert.Equal(PolylineRegion.PolylineTypeId, controller.Regions[0].TypeId);
        Assert.Equal(1, changedCount);
    }

    [Fact]
    public void Draw_Polyline_CreatesPolylineRegion()
    {
        // Legacy: single press+move+release no longer commits for polyline.
        var controller = CreateController();
        controller.ActiveDrawTypeId = PolylineRegion.PolylineTypeId;

        controller.OnPointerPressed(new ControlPoint(200, 200));
        Assert.Equal(RegionEditState.DrawingNewRegion, controller.State);
        controller.OnPointerReleased(new ControlPoint(800, 800));
        Assert.Empty(controller.Regions);
        Assert.Equal(RegionEditState.DrawingNewRegion, controller.State);
    }

    [Fact]
    public void Draw_DrawingRegionNull_WhenNotDrawing()
    {
        var controller = CreateController();
        Assert.Null(controller.DrawingRegion);
    }

    [Fact]
    public void Draw_SelectsNewRegionAfterCommit()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId;

        controller.OnPointerPressed(new ControlPoint(200, 200));
        controller.OnPointerMoved(new ControlPoint(600, 600));
        controller.OnPointerReleased(new ControlPoint(600, 600));

        Assert.Equal(controller.Regions[0].Id, controller.SelectedRegionId);
    }

    [Fact]
    public void Draw_Disabled_WhenActiveDrawTypeIdNull()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = null;

        controller.OnPointerPressed(new ControlPoint(500, 500));
        Assert.Equal(RegionEditState.Idle, controller.State);
        Assert.Empty(controller.Regions);
    }

    [Fact]
    public void Draw_UnknownType_DoesNothing()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = "unknown-type";

        controller.OnPointerPressed(new ControlPoint(500, 500));
        Assert.Equal(RegionEditState.Idle, controller.State);
        Assert.Empty(controller.Regions);
    }

    #endregion

    #region Cell grid painting

    [Fact]
    public void CellPaint_SelectsCells()
    {
        var controller = CreateController();
        controller.CellGrid = new CellGrid(4, 4);
        controller.IsCellPaintMode = true;

        var cellsChangedCount = 0;
        controller.CellsChanged += (_, _) => cellsChangedCount++;

        controller.OnPointerPressed(new ControlPoint(125, 125)); // Cell (0, 0)
        Assert.Equal(RegionEditState.PaintingCells, controller.State);
        Assert.Contains((0, 0), controller.SelectedCells);

        controller.OnPointerMoved(new ControlPoint(375, 125)); // Cell (0, 1)
        Assert.Contains((0, 1), controller.SelectedCells);
        Assert.True(cellsChangedCount > 0);

        controller.OnPointerReleased(new ControlPoint(375, 125));
        Assert.Equal(RegionEditState.Idle, controller.State);
    }

    [Fact]
    public void CellPaint_DeselectsCells()
    {
        var controller = CreateController();
        controller.CellGrid = new CellGrid(4, 4);
        controller.IsCellPaintMode = true;

        // Pre-select a cell.
        controller.SelectedCells.Add((0, 0));

        // Start stroke on an already-selected cell — should deselect.
        controller.OnPointerPressed(new ControlPoint(125, 125));
        Assert.DoesNotContain((0, 0), controller.SelectedCells);
    }

    [Fact]
    public void CellPaint_ConsistentStrokeDirection()
    {
        var controller = CreateController();
        controller.CellGrid = new CellGrid(4, 4);
        controller.IsCellPaintMode = true;

        // Start on unselected cell → stroke is "select".
        controller.OnPointerPressed(new ControlPoint(125, 125)); // (0,0) → select
        controller.OnPointerMoved(new ControlPoint(375, 125));   // (0,1) → also select
        controller.OnPointerMoved(new ControlPoint(625, 125));   // (0,2) → also select

        Assert.Contains((0, 0), controller.SelectedCells);
        Assert.Contains((0, 1), controller.SelectedCells);
        Assert.Contains((0, 2), controller.SelectedCells);

        controller.OnPointerReleased(new ControlPoint(625, 125));
    }

    [Fact]
    public void CellPaint_TakesPriorityOverDraw()
    {
        var controller = CreateController();
        controller.CellGrid = new CellGrid(4, 4);
        controller.IsCellPaintMode = true;
        controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId;

        controller.OnPointerPressed(new ControlPoint(125, 125));
        // Cell paint mode should take priority.
        Assert.Equal(RegionEditState.PaintingCells, controller.State);
    }

    [Fact]
    public void CellPaint_InactiveWhenNotInPaintMode()
    {
        var controller = CreateController();
        controller.CellGrid = new CellGrid(4, 4);
        controller.IsCellPaintMode = false;

        controller.OnPointerPressed(new ControlPoint(125, 125));
        Assert.NotEqual(RegionEditState.PaintingCells, controller.State);
    }

    #endregion

    #region BuildRenderState

    [Fact]
    public void BuildRenderState_ReflectsCurrentState()
    {
        var rect = CreateRect();
        var controller = CreateController(rect);

        controller.OnPointerMoved(new ControlPoint(500, 500));
        var state = controller.BuildRenderState();

        Assert.Equal(rect.Id, state.HoveredRegionId);
        Assert.Null(state.SelectedRegionId);
        Assert.Null(state.HoveredHandleIndex);
    }

    [Fact]
    public void BuildRenderState_IncludesSelectedCells()
    {
        var controller = CreateController();
        controller.SelectedCells.Add((1, 2));

        var state = controller.BuildRenderState();
        Assert.NotNull(state.SelectedCells);
        Assert.Contains((1, 2), state.SelectedCells);
    }

    [Fact]
    public void BuildRenderState_NullSelectedCells_WhenEmpty()
    {
        var controller = CreateController();
        var state = controller.BuildRenderState();
        Assert.Null(state.SelectedCells);
    }

    #endregion

    #region Cursor resolution

    [Fact]
    public void ResolveCursorForHandle_CornerHandles()
    {
        Assert.Equal("SizeNorthwestSoutheast", RegionEditController.ResolveCursorForHandle("corner", 0));
        Assert.Equal("SizeNortheastSouthwest", RegionEditController.ResolveCursorForHandle("corner", 1));
        Assert.Equal("SizeNorthwestSoutheast", RegionEditController.ResolveCursorForHandle("corner", 2));
        Assert.Equal("SizeNortheastSouthwest", RegionEditController.ResolveCursorForHandle("corner", 3));
    }

    [Fact]
    public void ResolveCursorForHandle_EdgeHandles()
    {
        // Edge-midpoint handles are no longer used by built-in shapes.
        // The resolver still returns SizeAll as a safe default for unknown handle kinds.
        Assert.Equal("SizeAll", RegionEditController.ResolveCursorForHandle("edge-midpoint", 4));
        Assert.Equal("SizeAll", RegionEditController.ResolveCursorForHandle("edge-midpoint", 5));
    }

    [Fact]
    public void ResolveCursorForHandle_VertexHandles()
    {
        Assert.Equal("Cross", RegionEditController.ResolveCursorForHandle("vertex", 0));
    }

    [Fact]
    public void ResolveCursorForHandle_UnknownKind_ReturnsSizeAll()
    {
        Assert.Equal("SizeAll", RegionEditController.ResolveCursorForHandle("custom-handle", 0));
    }

    #endregion

    #region Draw mode cursor

    [Fact]
    public void DrawMode_Background_ShowsCrossCursor()
    {
        var controller = CreateController();
        controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId;

        string? lastCursor = null;
        controller.CursorChanged += (_, cursor) => lastCursor = cursor;

        controller.OnPointerMoved(new ControlPoint(500, 500));
        Assert.Equal("Cross", lastCursor);
    }

    #endregion

    #region Multiple regions

    [Fact]
    public void MultipleRegions_CanSelectDifferentRegions()
    {
        var rect1 = CreateRect(0.1, 0.1, 0.4, 0.4);
        var rect2 = CreateRect(0.6, 0.6, 0.9, 0.9);
        var controller = CreateController(rect1, rect2);

        // Click on rect1.
        controller.OnPointerPressed(new ControlPoint(250, 250));
        Assert.Equal(rect1.Id, controller.SelectedRegionId);

        controller.OnPointerReleased(new ControlPoint(250, 250));

        // Click on rect2.
        controller.OnPointerPressed(new ControlPoint(750, 750));
        Assert.Equal(rect2.Id, controller.SelectedRegionId);
    }


    #endregion

    #region No transform

    [Fact]
    public void NoTransform_PointerEvents_AreNoOps()
    {
        var controller = new RegionEditController();
        // No transform set — all pointer events should be safe no-ops.
        controller.OnPointerPressed(new ControlPoint(100, 100));
        controller.OnPointerMoved(new ControlPoint(200, 200));
        controller.OnPointerReleased(new ControlPoint(200, 200));

        Assert.Equal(RegionEditState.Idle, controller.State);
    }

    [Fact]
    public void HitTest_NoTransform_ReturnsBackground()
    {
        var controller = new RegionEditController();
        var result = controller.HitTest(new ControlPoint(100, 100));
        Assert.Equal(HitTestKind.Background, result.Kind);
    }

    #endregion
}
