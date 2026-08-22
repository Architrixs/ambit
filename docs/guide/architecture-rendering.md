# Rendering, how it draws fast

`Ambit.Avalonia` draws straight to Skia. No per-shape controls, just one `ICustomDrawOperation` per overlay.

```csharp
var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>().Lease();
var canvas = lease.SkCanvas;
_renderer.Render(canvas, regions, state, transform);
```

## Why it's fast

- Paints and paths are created once and reused, no new allocations each frame
- `UpdateRegions()` diffs first, only redraws when something changed
- Good for 20+ tiles at once

## Where points go

`ICoordinateTransform` converts between your `0..1` points and screen pixels:

```csharp
public interface ICoordinateTransform
{
    ControlPoint ToControlSpace(NormalizedPoint p);
    NormalizedPoint ToNormalizedSpace(ControlPoint p);
}
```

`AmbitViewer` implements this and handles pan, zoom, and letterboxing for you.
