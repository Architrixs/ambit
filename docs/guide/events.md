# When things happen

Ambit is quiet until you do something. Here's what fires and when.

## Controller events

| Event | When it fires | What to do with it |
|---|---|---|
| `RegionsChanged` | User finishes a drag, finishes drawing, or clicks a toggle decoration. Never during the drag — only on mouse up. | Save to your database or config. This is the only one you need for persistence. |
| `RenderStateChanged` | Hover changes, selection changes, or anything that needs a redraw. Can fire a lot while moving the mouse. | Just redraw. The sample listens and calls `InvalidateVisual()`. |
| `CellsChanged` | User paints grid cells. | Update your cell selection storage. |
| `CursorChanged` | Hover moves from a handle to a body to background. Returns a name like `Hand`, `Cross`, `SizeAll`. | Set the cursor. `RegionDrawingLayer` already does this. |
| `PanZoomChanged` | User pans (right-drag) or zooms (wheel). | Sync a video player or update an overlay if you keep your own transform. |

### Clicking outside to deselect

If you click empty canvas and no shape is under the cursor, Ambit clears `SelectedRegionId` and fires `RenderStateChanged` — not `RegionsChanged`. That's intentional: selection is UI state, not saved data. The sample's sidebar updates on `RenderStateChanged` so it clears right away.

```csharp
controller.RegionsChanged += (_, _) => Save(controller.Regions); // save
controller.RenderStateChanged += (_, _) => UpdateUi(controller.SelectedRegionId); // select
controller.CellsChanged += (_, _) => SaveGrid(controller.SelectedCells);
```

## Viewer events

`AmbitViewer.PanZoomChanged` also fires on zoom/pan. The controller's `CoordinateTransform` is updated automatically — you usually don't need to set it yourself.
