# Rendering & UI Layer

`Ambit.Avalonia` contains the Skia-sharp rendering loop, caching systems, custom draw operations, and Avalonia wrappers.

## Custom Draw Operation

To bypass standard Avalonia rendering overhead, Ambit uses a custom draw operation (`RegionOverlayDrawOperation`) that hooks directly into SkiaSharp. This gives direct access to the `SKCanvas` and hardware acceleration.

```csharp
public void Render(ImmediateDrawingContext context)
{
    var lease = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
    using var skiaLease = lease.Lease();
    var canvas = skiaLease.SkCanvas;

    // Direct hardware-accelerated Skia rendering...
    _renderer.Render(canvas, ...);
}
```

---

## Allocation-Free Rendering

In steady-state playbacks (e.g. streaming multiple camera channels with overlays), Ambit maintains **zero managed heap allocations** inside the render loop:
* **Resource Reusability**: All `SKPaint` and `SKPath` instances are allocated once and cached inside `SkiaRenderResources`.
* **Value-Type Operations**: Render state signatures and coordinate matrices avoid object allocation.
* **No Boxing**: Identifiers, states, and properties utilize value-types to prevent garbage generation.

---

## Coordinate Transformations

The `ICoordinateTransform` interface handles translating points between **Normalized space** and the actual **Control Pixels**:

```csharp
public interface ICoordinateTransform
{
    ControlPoint ToControlSpace(NormalizedPoint p);
    NormalizedPoint ToNormalizedSpace(ControlPoint p);
}
```

This abstraction allows the canvas to seamlessly pan, zoom, or stretch annotations over non-square ratios (like letterboxed video feeds).
