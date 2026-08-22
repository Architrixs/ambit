# Ambit

**Ambit** is a standalone, general-purpose .NET 10 library that provides interactive shape drawing/editing and passive annotation overlays in Avalonia applications. 

Designed for high-performance and cross-platform desktop applications (Windows/Linux), Ambit is ideal for drawing regions of interest (ROIs), tripwire lines, mask polygons, bounding boxes, text labels, and heatmap overlays. It is equally suited for video surveillance analytics, photo markup, PDF page annotations, or canvas diagramming tools.

---

## Key Features

* **Three-Layer Architecture**: Clean separation between geometry/data models (pure C#, 0 dependencies), Skia-optimized rendering, and Avalonia input controls.
* **Open-Closed Principle (OCP)**: Genuinely extensible. Add new region shapes or visual decorations (arrows, badges, counters) by implementing an interface and registering them—without modifying the library's core codebase.
* **Allocation-Free Rendering**: Zero managed heap allocations in the steady-state rendering loop, optimized for high-concurrency environments (e.g., viewing up to 24 simultaneous camera streams).
* **Flexible Coordinate Mapping**: Translates coordinates dynamically between normalized `0..1` region space and control pixel coordinates, seamlessly handling letterboxing, pan, and zoom.
* **Agnostic Serialization**: Exposes plain DTO (POCO) mappers ready for any serialization format (JSON, XML, Protocol Buffers).

---

## Installation

```bash
dotnet add package Ambit.Core
dotnet add package Ambit.Avalonia
# or via PackageReference
# <PackageReference Include="Ambit.Core" Version="0.1.*" />
# <PackageReference Include="Ambit.Avalonia" Version="0.1.*" />
```
Target: **.NET 10**, **Avalonia 11.3+** (`Avalonia.Skia` default backend). No extra native deps.

For isolated registries (recommended) use `AmbitConfiguration.CreateRegistry()` instead of the global `IRegionTypeRegistry.Default`.

## Architecture Overview

Ambit is divided into three primary packages:

1. **`Ambit.Core`**:
   - Holds pure geometry structures (`NormalizedPoint`, `NormalizedVector`, `NormalizedBounds`).
   - Defines interfaces: `IRegion`, `IEditableRegion`, `IDecoration`, `IHitTestable`, `IHandleProvider`.
   - Implements the platform-agnostic interaction state machine (`RegionEditController`).
   - **Zero Avalonia/Skia dependency** — fully unit-testable.
2. **`Ambit.Avalonia`**:
   - Contains Skia-optimized drawing operations (`ICustomDrawOperation` and `ISkiaSharpApiLeaseFeature`).
   - Provides built-in shape renderers (Rectangle, Ellipse, Line, Polyline, Polygon) and the `LabelDecoration` renderer.
   - Exposes controls: `AmbitViewer` (pan/zoom/letterbox viewport + `ICoordinateTransform`), `RegionOverlayControl` (passive), `RegionEditorControl` (interactive) and layers (`AmbitLayer`, `RegionDrawingLayer`, `CellGridLayer`, `HeatmapOverlayLayer`).

---

## Quick Start

### 1. Passive Overlay Mode
For rendering annotations over camera streams, images, or documents without user interaction:

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

// 1. Instantiate the passive control (AmbitViewer handles letterbox + ICoordinateTransform)
var overlayControl = new RegionOverlayControl
{
    ContentSize = new Size(1920, 1080), // optional: native frame size for correct aspect-fit
    // BackgroundImage = yourSkBitmap, // or overlayControl.Content = yourVideoControl
};

// 2. Define styled regions — use With() for copy-on-write edits
var baseStyle = new RegionStyle { StrokeColorHex = "#EF4444", StrokeThickness = 2.0 };
var style = baseStyle.With(fillColorHex: "#FCA5A5", fillOpacity: 0.2);

var rectangleRegion = new RectangleRegion(
    new NormalizedPoint(0.1, 0.1), 
    new NormalizedPoint(0.4, 0.4), 
    style,
    label: "Detection Zone"
);

// 3. Push regions to the control (only invalidates/redraws when content changes)
overlayControl.UpdateRegions(new List<IRegion> { rectangleRegion });

// Coordinate mapping — all vertices are 0..1 (NormalizedPoint), the viewer maps to pixels:
// var ctrlPt = overlayControl.CoordinateTransform.ToControlSpace(new NormalizedPoint(0.5, 0.5));
```

### 2. Interactive Editing Mode
To let users draw, select, move, and reshape annotations:

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

// 1. Create the interaction controller — inject the viewer's transform for correct hit-testing
var controller = new RegionEditController
{
    // Prefer isolated registry in new code:
    // RegionTypeRegistry = AmbitConfiguration.CreateRegistry()
};
var viewerTransform = new RegionEditorControl(controller)
{
    ContentSize = new Size(1920, 1080),
};
// controller.CoordinateTransform is set automatically by AmbitViewer/RegionDrawingLayer

// 2. Populate with initial regions to edit
controller.SetRegions(new IEditableRegion[] { rectangleRegion });

// 3. Bind the controller to the editor UI control (layers: RegionDrawingLayer + CellGridLayer)
var editorControl = new RegionEditorControl(controller);

// 4. Set the desired active draw mode (e.g. Draw Rectangle, Draw Polygon, or null for Selection Mode)
controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId; 

// 5. Listen to committed changes (PointerReleased) to save back to your database/config
controller.RegionsChanged += (sender, args) => 
{
    var updatedRegions = controller.Regions;
    // Save to configuration...
};
// Tip: controller.RegionsChanged fires only on commit (PointerReleased), not every PointerMoved.
```

---

## Extending Ambit (OCP Compliance)

Adding a custom region shape or decoration requires **no modifications** to `Ambit.Core`/`Ambit.Avalonia` — only interfaces + registry entries. See `samples/Ambit.Sample/Pages/RegionKindsPage.cs` for a live proof (`CircleRegion` + `DirectionIndicatorDecoration`/`CountBadgeDecoration` added solely via registration).

### Adding a Custom Region (e.g. `TriangleRegion`)
1. Create a class implementing `IEditableRegion` (pure C#, no Avalonia ref) — include `Bounds`, `GetHandles()`, `HitTestBody`, `MoveHandle`, `Translate`.
2. Create `IRegionFactory` (`Create`/`ToDto`) and `IRegionRenderer` (Skia).
3. Register both:
   ```csharp
   // Factory/DTO registry (serialization + drawing factory)
   var typeRegistry = AmbitConfiguration.CreateRegistry(); // isolated, or IRegionTypeRegistry.Default
   typeRegistry.Register(new TriangleRegionFactory());

   // Render registry
   var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
   renderRegistry.Register(new TriangleRegionRenderer());
   var renderer = new RegionOverlayRenderer(renderRegistry);
   var editor = new RegionEditorControl(controller, renderer)
   {
       // For custom shapes to be drawable via the controller (P4), wire the registry:
       // controller.RegionTypeRegistry = typeRegistry; // enables ActiveDrawTypeId="triangle"
   };
   ```

### Adding a Custom Decoration (e.g. `WarningBadge` — `DirectionIndicatorDecoration` is the sample's proof)
1. Create a class implementing `IDecoration` / `IAnchorableDecoration` / `IToggleDecoration` (open `TypeId` string, no enum).
2. Create `IDecorationFactory` and `IDecorationRenderer`.
3. Register with the same registries (`typeRegistry.Register(factory)` + `renderRegistry.Register(renderer)`). Two `DirectionIndicatorDecoration`s on one `LineRegion` = independent arrows per end, no `LineRegion` subclass needed.

> **Note:** `DirectionIndicatorDecoration` ("direction-arrow") is **not** a built-in — it's intentionally sample-only to prove OCP. Core ships only `LabelDecoration` (`"label-badge"`). Copy the sample's `DirectionIndicatorDecoration` to your app.

---

## Serialization & Persistence

Ambit is serializer-agnostic. It maps runtime instances to flat DTOs (`RegionDto`, `DecorationDto`) that map directly to JSON or other formats:

```csharp
using System.Text.Json;
using Ambit;

// Setup type registry
var typeRegistry = new RegionTypeRegistry().RegisterBuiltInTypes();

// 1. Serialize to JSON
var dtos = controller.Regions.Select(r => typeRegistry.ToDto(r)).ToList();
string json = JsonSerializer.Serialize(dtos);

// 2. Deserialize from JSON
var loadedDtos = JsonSerializer.Deserialize<List<RegionDto>>(json);
var loadedRegions = loadedDtos.Select(dto => typeRegistry.CreateRegion(dto)).ToList();

controller.SetRegions(loadedRegions);
```

---

## License
Ambit is licensed under the [MIT License](LICENSE).
