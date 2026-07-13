# Core Logic & Math

The `Ambit.Core` assembly houses the data structures, geometric equations, and state machine that coordinate the interaction rules of Ambit. It is a dependency-free project, ensuring usability and testability.

## Normalized Region Space

To decouple annotations from resolution changes (e.g. video scaling, resizing windows), all coordinates in Ambit are stored in **Normalized Region Space**, where:
* `(0.0, 0.0)` represents the top-left corner of the bounding area.
* `(1.0, 1.0)` represents the bottom-right corner.

All coordinates are clamped to the inclusive interval `[0.0, 1.0]`.

---

## State Machine

The interaction states are governed by `RegionEditController`. It handles pointer and keyboard input events using a rigid state machine:

| State | Trigger | Description |
|---|---|---|
| `Idle` | Default | No active interaction. |
| `Hover` | PointerMove | Pointer is hovering over a hit-testable target. |
| `DraggingRegion` | PointerPressed on body | Translating the position of a region. |
| `DraggingHandle` | PointerPressed on handle | Reshaping or resizing a region. |
| `DrawingNewRegion` | PointerPressed on background with active draw type | Drawing a new shape. |
| `PaintingCells` | PointerPressed on grid background | Drag-painting selection cells. |

---

## Hit-Test Priorities

When a pointer press event is processed, a hit-test is run using the following order of precedence:

1. **Interactive Decoration Anchors** (highest priority)
2. **Region Handles**
3. **Region Bodies**
4. **Background** (lowest priority)
