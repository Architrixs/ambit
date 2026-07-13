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

## Architecture Overview

Ambit is divided into three primary packages:

1. **`Ambit.Core`**:
   - Holds pure geometry structures (`NormalizedPoint`, `NormalizedVector`, `NormalizedBounds`).
   - Defines interfaces: `IRegion`, `IEditableRegion`, `IDecoration`, `IHitTestable`, `IHandleProvider`.
   - Implements the platform-agnostic interaction state machine (`RegionEditController`).
   - Completely free of Avalonia and SkiaSharp dependencies.
2. **`Ambit.Avalonia`**:
   - Contains Skia-optimized drawing operations (`ICustomDrawOperation` and `ISkiaSharpApiLeaseFeature`).
   - Provides built-in shape renderers (Rectangle, Ellipse, Line, Polyline, Polygon) and decoration renderers.
   - Exposes controls: `RegionOverlayControl` (passive rendering) and `RegionEditorControl` (interactive editing).

---

## Quick Start

### 1. Passive Overlay Mode
For rendering annotations over camera streams, images, or documents without user interaction:

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

// 1. Instantiate the passive control
var overlayControl = new RegionOverlayControl();

// 2. Define styled regions
var style = new RegionStyle 
{ 
    StrokeColorHex = "#EF4444", 
    StrokeThickness = 2.0,
    FillColorHex = "#FCA5A5", 
    FillOpacity = 0.2 
};

var rectangleRegion = new RectangleRegion(
    new NormalizedPoint(0.1, 0.1), 
    new NormalizedPoint(0.4, 0.4), 
    style,
    label: "Detection Zone"
);

// 3. Push regions to the control (only invalidates/redraws when content changes)
overlayControl.UpdateRegions(new IReadOnlyList<IRegion>[] { rectangleRegion });
```

### 2. Interactive Editing Mode
To let users draw, select, move, and reshape annotations:

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

// 1. Create the interaction controller
var controller = new RegionEditController();

// 2. Populate with initial regions to edit
controller.SetRegions(new IEditableRegion[] { rectangleRegion });

// 3. Bind the controller to the editor UI control
var editorControl = new RegionEditorControl(controller);

// 4. Set the desired active draw mode (e.g. Draw Rectangle, Draw Polygon, or null for Selection Mode)
controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId; 

// 5. Listen to committed changes (PointerReleased) to save back to your database/config
controller.RegionsChanged += (sender, args) => 
{
    var updatedRegions = controller.Regions;
    // Save to configuration...
};
```

---

## Extending Ambit (OCP Compliance)

Adding a custom region shape or decoration requires no modifications to the Ambit codebase. 

### Adding a Custom Region (e.g. `TriangleRegion`)
1. Create a class implementing `IEditableRegion` containing geometry math.
2. Create a renderer implementing `IRegionRenderer` using Skia Sharp.
3. Register the renderer:
   ```csharp
   var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
   renderRegistry.Register(new TriangleRegionRenderer());
   var renderer = new RegionOverlayRenderer(renderRegistry);

   var editor = new RegionEditorControl(controller, renderer);
   ```

### Adding a Custom Decoration (e.g. `WarningBadge`)
1. Create a class implementing `IDecoration`.
2. Create a renderer implementing `IDecorationRenderer`.
3. Register the decoration renderer using the same `RegionRenderRegistry`.

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
