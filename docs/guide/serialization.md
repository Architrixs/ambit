# Saving and loading

Ambit doesn't pick a format for you. It gives you plain DTOs you can save however you want.

## The DTOs

- `RegionDto` — type, points, style, label, decorations
- `DecorationDto` — type, anchor, properties

## Save and load

```csharp
using Ambit;

var registry = new RegionTypeRegistry().RegisterBuiltInTypes();

// Save
var dtos = controller.Regions.Select(r => registry.ToDto(r)).ToList();
var json = JsonSerializer.Serialize(dtos);

// Load
var loaded = JsonSerializer.Deserialize<List<RegionDto>>(json)!;
var regions = loaded.Select(dto => registry.CreateRegion(dto));
controller.SetRegions(regions);
```

## Custom shapes

If you added your own shape (like `CircleRegion`), add its factory before saving/loading:

```csharp
registry.Register(new CircleRegionFactory());
```

That's it — the same registry you use for drawing is used for saving.
