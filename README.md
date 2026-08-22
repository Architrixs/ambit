![Ambit](assets/ambit_logo.png)

# Ambit

Ambit is a .NET 10 library for drawing and editing shapes over images and video in Avalonia. It works well for things like video analytics zones, photo markup, or diagram tools.

---

## Install

```bash
dotnet add package Ambit.Core
dotnet add package Ambit.Avalonia
```

Needs **.NET 10** and **Avalonia 11.3+**. That's it.

## How it works

**Ambit.Core** has all the math — points, bounds, hit-testing, and the editing state machine. No Avalonia dependency, so you can test it anywhere.

**Ambit.Avalonia** does the drawing. It uses Skia directly (`ICustomDrawOperation`) and gives you two controls:
- `RegionOverlayControl` — just renders, good for 20+ tiles at once
- `RegionEditorControl` — handles mouse input for drawing and editing

All points are stored as `0..1` normalized coordinates. The viewer handles pan, zoom, and letterboxing for you.

---

## Quick start — show some shapes

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

var overlay = new RegionOverlayControl
{
    ContentSize = new Size(1920, 1080),
};

var style = new RegionStyle { StrokeColorHex = "#EF4444", StrokeThickness = 2.0 }
    .With(fillColorHex: "#FCA5A5", fillOpacity: 0.2);

var zone = new RectangleRegion(
    new NormalizedPoint(0.1, 0.1),
    new NormalizedPoint(0.4, 0.4),
    style, label: "Detection Zone");

overlay.UpdateRegions(new List<IRegion> { zone });
// Updates only redraw when something actually changed
```

## Quick start — let users edit

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

var controller = new RegionEditController();
var editor = new RegionEditorControl(controller)
{
    ContentSize = new Size(1920, 1080),
};

controller.SetRegions(new IEditableRegion[] { zone });

// Switch tools
controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId; // draw rectangles
controller.ActiveDrawTypeId = null; // back to select/move

// Save when the user finishes a drag
controller.RegionsChanged += (_, _) => Save(controller.Regions);
```

---

## Make your own shape

You don't need to change Ambit itself. Just add a class and register it. The sample shows this with `CircleRegion`.

```csharp
// 1. Your shape — just implement IEditableRegion
public class TriangleRegion : IEditableRegion { ... }

// 2. Factory for saving/loading + renderer for drawing
public class TriangleFactory : IRegionFactory { ... }
public class TriangleRenderer : IRegionRenderer { ... }

// 3. Register
var registry = new RegionTypeRegistry().RegisterBuiltInTypes();
registry.Register(new TriangleFactory());

var renders = new RegionRenderRegistry().RegisterBuiltInRenderers();
renders.Register(new TriangleRenderer());

var renderer = new RegionOverlayRenderer(renders);
var editor = new RegionEditorControl(controller, renderer);
```

Same idea for decorations like badges or arrows. Check `samples/Ambit.Sample/Pages/RegionKindsPage.cs` — it adds `DirectionIndicatorDecoration` without touching any library code.

---

## Saving and loading

Ambit uses simple DTOs so you can save however you like:

```csharp
var registry = new RegionTypeRegistry().RegisterBuiltInTypes();

// Save
var dtos = controller.Regions.Select(r => registry.ToDto(r)).ToList();
var json = JsonSerializer.Serialize(dtos);

// Load
var loaded = JsonSerializer.Deserialize<List<RegionDto>>(json)!;
controller.SetRegions(loaded.Select(dto => registry.CreateRegion(dto)));
```

---

## License

[MIT](LICENSE)
