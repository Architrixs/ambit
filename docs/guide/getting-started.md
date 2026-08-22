# Getting Started

This guide walks you through setting up Ambit in your C# Avalonia application.

## Installation

Via NuGet (once published):
```bash
dotnet add package Ambit.Core
dotnet add package Ambit.Avalonia
```

Or via project references:
```xml
<ItemGroup>
  <ProjectReference Include="..\src\Ambit.Core\Ambit.Core.csproj" />
  <ProjectReference Include="..\src\Ambit.Avalonia\Ambit.Avalonia.csproj" />
</ItemGroup>
```

Target: **.NET 10**, **Avalonia 11.3+**. No native deps.

> **Isolated vs global registry:** Prefer `AmbitConfiguration.CreateRegistry()` (returns a fresh `RegionTypeRegistry` with built-ins) and pass it explicitly. `IRegionTypeRegistry.Default` is a global singleton retained for single-registry apps — avoid it when you need per-test/per-control isolation.

---

## 1. Passive Overlay (Playback Mode)

Use `RegionOverlayControl` to render static or real-time annotations over an existing control (like a video player or an image control):

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

// Instantiate the control
var overlay = new RegionOverlayControl();

// Create styled regions
var style = new RegionStyle 
{ 
    StrokeColorHex = "#EF4444", 
    FillColorHex = "#FCA5A5", 
    FillOpacity = 0.2 
};

var zone = new RectangleRegion(
    new NormalizedPoint(0.1, 0.1),
    new NormalizedPoint(0.4, 0.4),
    style,
    label: "Warning Area"
);

// Push to the overlay — diffs and only invalidates when content actually changed
overlay.UpdateRegions(new List<IRegion> { zone });
```

---

## 2. Interactive Annotation (Editing Mode)

Use `RegionEditorControl` driven by `RegionEditController` to let users draw and edit annotations:

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

// 1. Create the interaction controller
var controller = new RegionEditController();

// 2. Supply the starting regions
controller.SetRegions(new IEditableRegion[] { zone });

// 3. Instantiate the control
var editor = new RegionEditorControl(controller);

// 4. Bind drawing mode to UI buttons
void DrawRectangleMode() => controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId;
void SelectMode() => controller.ActiveDrawTypeId = null; // Turns on drag/reshape mode

// 5. Handle save events — fires only on commit (PointerReleased), not every PointerMoved
controller.RegionsChanged += (sender, e) => {
    var updated = controller.Regions;
    // Save to configuration file or database
};

// 6. Coordinate mapping — vertices are 0..1, the viewer translates to pixels
//    AmbitViewer implements ICoordinateTransform (pan/zoom/letterbox) and is set automatically:
//    editor.CoordinateTransform.ToControlSpace(new NormalizedPoint(0.5, 0.5))
//    editor.CoordinateTransform.ToNormalizedSpace(new ControlPoint(100, 200))
//    For custom transforms, set controller.CoordinateTransform explicitly.
```
