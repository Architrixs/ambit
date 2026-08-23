# Extras

Two extras that are not shapes.

## Cell painting

A fixed grid for things like sensitivity masks.

```csharp
var grid = new CellGrid(rows: 9, cols: 16);
controller.CellGrid = grid;
controller.IsCellPaintMode = true;
```

- Paint by dragging. The first cell decides if the stroke selects or deselects.
- `controller.SelectedCells` is a `HashSet<(int Row, int Col)>`.
- Listen to `CellsChanged` to save.

The sample page **Cell Grid Editor** lets you change rows and cols live.

## Heatmap

An overlay for intensity data.

```csharp
var heatmap = new HeatmapLayer
{
    Rows = 8, Columns = 8,
    Intensities = new byte[64], // 0..255
    Opacity = 0.65
};
editor.Heatmap = heatmap;
```

Ambit builds a small bitmap from the intensities and upscales it with linear filtering. No per-cell shaders.

The **Video & Analytics** page has sliders for intensity and opacity so you can see it update.
