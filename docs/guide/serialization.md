# Serialization & DTOs

Ambit is serializer-agnostic. It does not force JSON, XML, or binary serialization dependencies on your project. Instead, it exposes flat data transfer objects (DTOs) that map cleanly to any format.

## DTO Models

* **`RegionDto`**: Holds coordinates, style options, name, type, and array of decorations.
* **`DecorationDto`**: Holds type, anchor position, and key-value properties.

---

## Round-Trip Mapping

Use the `IRegionTypeRegistry` to convert runtime objects to DTOs and vice versa:

```csharp
using Ambit;

// 1. Initialize registry with built-in factories
var registry = new RegionTypeRegistry().RegisterBuiltInTypes();

// 2. Export runtime regions to DTOs
var dtos = controller.Regions
    .Select(region => registry.ToDto(region))
    .ToList();

// 3. Serialize DTOs (e.g. using System.Text.Json)
string json = JsonSerializer.Serialize(dtos);

// 4. Reconstruct from JSON
var loadedDtos = JsonSerializer.Deserialize<List<RegionDto>>(json);
var loadedRegions = loadedDtos
    .Select(dto => registry.CreateRegion(dto))
    .ToList();

// Load back into controller
controller.SetRegions(loadedRegions);
```

---

## Custom Type Factories

If you add a custom region or decoration (e.g. `CircleRegion`), implement `IRegionFactory` or `IDecorationFactory` and register it with the type registry:

```csharp
public class CircleRegionFactory : IRegionFactory
{
    public string TypeId => CircleRegion.CircleTypeId;

    public IEditableRegion Create(RegionDto dto, IReadOnlyList<IDecoration> decorations)
    {
        var radius = double.Parse(dto.Properties["radius"]);
        return new CircleRegion(dto.Vertices[0], radius, dto.Style, dto.Id, decorations);
    }

    public RegionDto ToDto(IRegion region, IReadOnlyList<DecorationDto> decorations)
    {
        var circle = (CircleRegion)region;
        return new RegionDto
        {
            Id = circle.Id,
            TypeId = circle.TypeId,
            Vertices = new[] { circle.Center },
            Decorations = decorations.ToArray(),
            Style = circle.Style,
            Properties = new Dictionary<string, string?> { ["radius"] = circle.Radius.ToString() }
        };
    }
}

// Register
registry.Register(new CircleRegionFactory());
```
