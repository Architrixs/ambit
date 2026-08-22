# Labels

Labels are part of the core library, not an add-on.

Every `RegionStyle` can have a `LabelStyle`:

```csharp
var style = new RegionStyle
{
    StrokeColorHex = "#3B82F6",
    LabelStyle = new LabelStyle
    {
        TextColorHex = "#FFFFFF",
        BackgroundColorHex = "#1E293B",
        FontSize = 11,
        Placement = LabelPlacement.TopLeft // TopLeft, TopRight, BottomLeft, BottomRight, CenterInside
    }
};

var zone = new RectangleRegion(a, b, style, label: "Gate A");
```

- `label` is the text. Empty or null means no label is drawn.
- `TextColorHex`, `BackgroundColorHex`, `FontSize` control how it looks.
- `Placement` picks the corner. Use `AnchorOverride` if you need an exact spot.

To change it later, use the `With` helper:

```csharp
// Make the label bigger and move it to the top-right
var newStyle = oldStyle.With(
    labelStyle: oldStyle.LabelStyle.With(fontSize: 14, placement: LabelPlacement.TopRight)
);
```

The sample's **Editor & Drawings** page has controls for all of this, text, placement, font size, text color, and background, so you can see changes live.
