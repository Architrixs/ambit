# Styling

Every region has its own style. No global theme.

## RegionStyle

```csharp
new RegionStyle
{
    StrokeColorHex = "#3B82F6",    // border color
    StrokeThickness = 2.0,          // in pixels
    StrokeDashPattern = null,       // null is solid, try new[] { 6.0, 4.0 } for dashed
    FillColorHex = "#3B82F6",       // null means no fill
    FillOpacity = 0.15,             // 0 is transparent, 1 is solid
    LabelStyle = new LabelStyle { ... }, // null means no label
    DefaultHandleStyle = new HandleStyle { ... }
}
```

Use `With` to tweak an existing style without rebuilding it:

```csharp
var bigger = style.With(strokeThickness: 3.0, fillOpacity: 0.25);
```

## HandleStyle

Controls the little grab handles.

```csharp
new HandleStyle
{
    RadiusPixels = 5.0,
    FillColorHex = "#FFFFFF",
    StrokeColorHex = "#2680EB"
}
```

## LabelStyle

Labels are part of the style, not a separate decoration.

```csharp
new LabelStyle
{
    TextColorHex = "#FFFFFF",
    BackgroundColorHex = "#1E293B",
    FontSize = 11,
    Placement = LabelPlacement.TopLeft
}
```

`Placement` can be `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`, or `CenterInside`. Use `AnchorOverride` if you need an exact spot.

If a region has `Label = "Gate A"` and a `LabelStyle`, it draws automatically. Empty label means no chip.

## Quick demo

The **Editor & Drawings** page lets you change text, placement, stroke color and width live.
