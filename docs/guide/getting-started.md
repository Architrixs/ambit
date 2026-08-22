# Getting Started

A quick walkthrough for adding Ambit to your Avalonia app.

## Install

```bash
dotnet add package Ambit.Core
dotnet add package Ambit.Avalonia
```

Or reference the projects directly:

```xml
<ItemGroup>
  <ProjectReference Include="..\src\Ambit.Core\Ambit.Core.csproj" />
  <ProjectReference Include="..\src\Ambit.Avalonia\Ambit.Avalonia.csproj" />
</ItemGroup>
```

You need **.NET 10**.

If you need more than one registry (for tests or multiple editors), create your own with `new RegionTypeRegistry().RegisterBuiltInTypes()`. Otherwise the default global one is fine.

---

## Show shapes (no editing)

Great for video tiles or image viewers:

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

var overlay = new RegionOverlayControl();

var style = new RegionStyle { StrokeColorHex = "#EF4444", FillColorHex = "#FCA5A5", FillOpacity = 0.2 };

var zone = new RectangleRegion(
    new NormalizedPoint(0.1, 0.1),
    new NormalizedPoint(0.4, 0.4),
    style, label: "Warning Area");

overlay.UpdateRegions(new List<IRegion> { zone });
```

`UpdateRegions` only redraws when the data actually changed.

---

## Let users draw and edit

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

var controller = new RegionEditController();
var editor = new RegionEditorControl(controller);

controller.SetRegions(new IEditableRegion[] { zone });

// Switch tools
void DrawRectangles() => controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId;
void Select() => controller.ActiveDrawTypeId = null;

// Save after the user finishes dragging
controller.RegionsChanged += (_, _) => Save(controller.Regions);

// Pan/zoom is on by default, right-drag to pan, wheel to zoom.
// Turn it off: editor.IsPanZoomEnabled = false;
```

Points are always `0..1`, Ambit handles the mapping to screen pixels, including pan and zoom.
