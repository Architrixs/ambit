# Shapes

Ambit ships with five shapes. You can add more without changing the library.

## Built in

**Rectangle** — two corners, 4 corner handles (`corner`). Drag a corner to resize, drag the body to move. `LockAspectRatio` keeps the ratio if you need it. Edge-midpoint handles were intentionally omitted (use a custom region via `IRegionTypeRegistry` if you need single-axis handles).

```csharp
new RectangleRegion(new NormalizedPoint(0.1, 0.1), new NormalizedPoint(0.4, 0.4), style, label: "Area A");
```

**Ellipse** — same 4 corner handles as rectangle, but drawn as an oval. Uses the same bounding box (edge handles omitted for same reason).

```csharp
new EllipseRegion(a, b, style);
```

**Polygon** — closed shape with many points. Each vertex is a handle. Call `InsertVertex` to add a point mid-edge.

```csharp
new PolygonRegion(vertices, style);
```

**Polyline** — like polygon but open. Good for paths with bends.

```csharp
new PolylineRegion(vertices, style);
```

**Line** — just two points. No fill, only stroke. Direction is not built in. Add a `DirectionIndicatorDecoration` if you need arrows.

```csharp
new LineRegion(start, end, style, label: "Connector");
```

All points are `0..1` normalized. All shapes have `Style` and optional `Label`.

## Add your own

See `Extensibility` — implement `IEditableRegion`, a factory, and a renderer, then register both. The sample adds `CircleRegion` this way.
