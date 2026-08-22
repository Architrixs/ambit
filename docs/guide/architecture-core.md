# Core — the math that doesn't need Avalonia

`Ambit.Core` has no UI dependency. You can use it in unit tests or on a server.

## Points are 0..1

Everything is stored normalized:

- `(0, 0)` top-left
- `(1, 1)` bottom-right

This way zoom or window resize doesn't change your data. The viewer maps it to pixels for you.

## The editor state machine

`RegionEditController` is a small state machine. It takes pointer positions and decides what to do:

| State | What it means |
|---|---|
| `Idle` | Nothing happening |
| `Hover` | Mouse over something — updates cursor |
| `DraggingHandle` | Resizing a shape |
| `DraggingRegion` | Moving a shape |
| `DrawingNewRegion` | Drawing a new shape |
| `PaintingCells` | Painting grid cells |

## Hit testing order

When you click, Ambit checks in this order:

1. Decoration handles (like arrow toggles)
2. Shape handles (corners)
3. Shape bodies
4. Background — starts a new shape or clears selection
