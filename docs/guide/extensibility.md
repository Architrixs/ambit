# Extensibility (Open-Closed Principle)

Ambit strictly adheres to the Open-Closed Principle (OCP). You can add new shape types and decoration items from outside the library namespace and register them with the rendering engine at runtime.

---

## 1. Custom Region Shape

To add a new shape (e.g. `TriangleRegion`):

### Define the Shape Model
Implement `IEditableRegion` in a pure C# class:

```csharp
public class TriangleRegion : IEditableRegion
{
    public const string TriangleTypeId = "triangle";

    public Guid Id { get; } = Guid.NewGuid();
    public string TypeId => TriangleTypeId;
    public IReadOnlyList<NormalizedPoint> Vertices { get; }
    public IReadOnlyList<IDecoration> Decorations { get; } = Array.Empty<IDecoration>();
    public RegionStyle Style { get; }

    // Implement IEditableRegion methods: HitTestBody, MoveHandle, Translate, GetHandles...
}
```

### Implement the Skia Renderer
Implement `IRegionRenderer` using Skia:

```csharp
public class TriangleRegionRenderer : IRegionRenderer
{
    public string TypeId => TriangleRegion.TriangleTypeId;

    public void Render(SKCanvas canvas, IRegion region, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
    {
        var triangle = (TriangleRegion)region;
        // Draw the vertices...
        canvas.DrawPath(path, resources.ConfigureStrokePaint(region.Style));
    }
}
```

### Register the Renderer
Register the custom renderer with the `RegionRenderRegistry`:

```csharp
var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
renderRegistry.Register(new TriangleRegionRenderer());

var renderer = new RegionOverlayRenderer(renderRegistry);
var editor = new RegionEditorControl(controller, renderer);
```

---

## 2. Custom Decorations

To add custom markers (e.g. a `StatusBadge`):

1. Create a class implementing `IDecoration`.
2. Create a renderer implementing `IDecorationRenderer`.
3. Register it with the same `RegionRenderRegistry`:
   ```csharp
   renderRegistry.Register(new StatusBadgeRenderer());
   ```
