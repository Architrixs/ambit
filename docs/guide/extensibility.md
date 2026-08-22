# Extensibility (Open-Closed Principle)

Ambit strictly adheres to the Open-Closed Principle (OCP). You can add new shape types and decoration items from outside the library namespace and register them with the rendering engine at runtime **without modifying any library file**. Live proof: `samples/Ambit.Sample/Pages/RegionKindsPage.cs` adds `CircleRegion` + `DirectionIndicatorDecoration`/`CountBadgeDecoration` solely via registration.

> **Built-in vs sample:** Core ships only `label-badge` decoration. `direction-arrow` (`DirectionIndicatorDecoration`) is **intentionally sample-only** — copy it to your app to prove the same pattern.

---

## 1. Custom Region Shape

To add a new shape (e.g. `TriangleRegion`):

### Define the Shape Model
Implement `IEditableRegion` in a pure C# class (no Avalonia ref, include `Bounds`):

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
    // Implement IEditableRegion: HitTestBody, MoveHandle, Translate, GetHandles...
}
```
Add a matching `IRegionFactory` (`Create`/`ToDto`) for serialization.

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

### Register Both Registries
Register the factory (DTO + drawing) **and** the renderer. Prefer isolated registries:

```csharp
var typeRegistry = AmbitConfiguration.CreateRegistry(); // or IRegionTypeRegistry.Default
typeRegistry.Register(new TriangleRegionFactory());
// Use TryRegister/RegisterOrReplace to avoid duplicate-Id exceptions during hot-reload

var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
renderRegistry.Register(new TriangleRegionRenderer());
var renderer = new RegionOverlayRenderer(renderRegistry);
var editor = new RegionEditorControl(controller, renderer);
// For drawing via controller (P4 registry-driven), wire: controller.RegionTypeRegistry = typeRegistry;
// then controller.ActiveDrawTypeId = TriangleRegion.TriangleTypeId works without editing the controller.
```

---

## 2. Custom Decorations

To add custom markers (e.g. a `StatusBadge` or `DirectionIndicatorDecoration`):

1. Create a class implementing `IDecoration` / `IAnchorableDecoration` / `IToggleDecoration` (open `TypeId` string, no enum).
2. Create `IDecorationFactory` + `IDecorationRenderer`.
3. Register with both registries:
   ```csharp
   typeRegistry.Register(new StatusBadgeFactory());
   renderRegistry.Register(new StatusBadgeRenderer());
   ```
   Two `DirectionIndicatorDecoration`s on one `LineRegion` → independent arrows per end, no `LineRegion` subclass needed (see sample).
