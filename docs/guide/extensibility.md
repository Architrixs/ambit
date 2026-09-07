# Making your own shapes

You can add new shapes or decorations without changing Ambit. The sample does this with `CircleRegion` + `CountBadgeDecoration` registered only in `RegionKindsPage.cs` (now under `samples/Ambit.Sample/Extensibility/`), take a look there for a full example. `DirectionIndicatorDecoration` is also sample-only — `LabelDecoration` remains the single built-in decoration; this proves the registry is truly open.

## A new shape

Say you want a triangle.

**1. Make the shape**

```csharp
public class TriangleRegion : IEditableRegion
{
    public const string TriangleTypeId = "triangle";
    public Guid Id { get; } = Guid.NewGuid();
    public string TypeId => TriangleTypeId;
    public IReadOnlyList<NormalizedPoint> Vertices { get; }
    public IReadOnlyList<IDecoration> Decorations { get; } = Array.Empty<IDecoration>();
    public RegionStyle Style { get; }
    public string? Label { get; }
    public NormalizedBounds Bounds => GeometryUtilities.GetBounds(Vertices);
    // ... HitTestBody, MoveHandle, Translate, GetHandles
}
```

You'll also need a small factory to save/load it (`IRegionFactory`).

**2. Make the renderer**

```csharp
public class TriangleRenderer : IRegionRenderer
{
    public string TypeId => TriangleRegion.TriangleTypeId;
    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform t, SkiaRenderResources r)
    {
        // draw with canvas.DrawPath(...)
    }
}
```

**3. Plug it in**

```csharp
var types = new RegionTypeRegistry().RegisterBuiltInTypes();
types.Register(new TriangleFactory());

var renders = new RegionRenderRegistry().RegisterBuiltInRenderers();
renders.Register(new TriangleRenderer());

var editor = new RegionEditorControl(controller, new RegionOverlayRenderer(renders));
controller.RegionTypeRegistry = types;
controller.ActiveDrawTypeId = TriangleRegion.TriangleTypeId;
```

That's it, no library changes needed.

## A new decoration

Decorations are little extras you attach to a shape, like an arrow or a badge.

1. Make a class with `IDecoration`
2. Make a factory and a renderer
3. Register both, same as above

You can put two badges on one `LineRegion` to get independent arrows at each end. The line itself doesn't need to know anything about arrows.
