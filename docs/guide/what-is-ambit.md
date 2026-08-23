# What is Ambit?

Ambit is a small library for drawing shapes over images and video in Avalonia. You give it normalized points (`0..1`), it handles the rest, hit-testing, handles, panning, and fast Skia rendering.

It started for marking regions on frames, but it works the same for photo markup, PDF highlights, or any canvas where you need regions.

**A few things people use it for:**

- **Overlays**, lines, polygons, and masks
- **Images & docs**, rectangles, ellipses, labels over photos or blueprints
- **Grids**, paintable cell matrices for sensitivity or masking

## How it's built

```
Ambit.Core       , points, math, hit tests, editing logic. No Avalonia, easy to test.
       ↓
Ambit.Avalonia   , Skia drawing, controls, and the pan/zoom viewport.
       ↓
Your app         , desktop or browser
```

That's the whole idea.
