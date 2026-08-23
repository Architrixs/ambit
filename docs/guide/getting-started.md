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

You need a registry that knows your shapes. Most apps just make one and reuse it:

```csharp
var registry = new RegionTypeRegistry().RegisterBuiltInTypes();
registry.Register(new MyCustomFactory()); // if you have custom shapes
```

If you run tests or have two editors with different custom shapes, make a separate registry for each. Otherwise they will share the same global one and custom shapes can leak between tests.

---

## Show shapes (no editing)

Great for video tiles or image viewers:

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

var overlay = new RegionOverlayControl();

var style = new RegionStyle { StrokeColorHex = "#EF4444", FillColorHex = "#FCA5A5", FillOpacity = 0.2 };

var area = new RectangleRegion(
    new NormalizedPoint(0.1, 0.1),
    new NormalizedPoint(0.4, 0.4),
    style, label: "Warning Area");

overlay.UpdateRegions(new List<IRegion> { area });
```

`UpdateRegions` only redraws when the data actually changed.

---

## Let users draw and edit

```csharp
using Ambit;
using Ambit.Avalonia.Controls;

var controller = new RegionEditController();
var editor = new RegionEditorControl(controller);

controller.SetRegions(new IEditableRegion[] { area });

// Switch tools
void DrawRectangles() => controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId;
void Select() => controller.ActiveDrawTypeId = null;

// Save after the user finishes dragging
controller.RegionsChanged += (_, _) => Save(controller.Regions);

// Pan/zoom is on by default, right-drag to pan, wheel to zoom.
// Turn it off: editor.IsPanZoomEnabled = false;
```

Points are always `0..1`, Ambit handles the mapping to screen pixels, including pan and zoom.
