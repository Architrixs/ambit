# Ambit Tasks

## Phase 1: Core geometry and model layer (`Ambit.Core`)

- [x] Repository scaffolding: solution, projects, nullable/warnings config, package metadata baseline (§14, §15, §17)
- [ ] Core primitives and contracts: normalized geometry types, region/decorations interfaces, render state, coordinate transform contract (§2, §5, §8)
- [ ] Styling model: `RegionStyle`, `LabelStyle`, `HandleStyle` and public XML docs (§7, §14)
- [ ] Registry abstractions and DTO contracts for regions, decorations, and cell selections (§5, §11)
- [ ] Geometry helpers for clamping, distance/projection, segment proximity, centroid, and bounds math (§4, §8, §12)
- [ ] RectangleRegion: hit-test, corner/edge handles, aspect-lock resize, translation (§6.1, §12)
- [ ] PolygonRegion: closed hit-test, vertex handles, translation, insert-vertex support (§6.1, §12)
- [ ] PolylineRegion: segment proximity hit-test, vertex handles, translation (§6.1, §12)
- [ ] LineRegion: two-point editing and stroke-band hit-testing with decorations-only direction model (§6.1, §6.3, §12)
- [ ] EllipseRegion: bounding-box editing, edge/corner handles, translation, ellipse hit-testing (§6.1, §12)
- [ ] Built-in decorations: direction indicator toggle and label decoration data model (§6.3, §12)
- [ ] Cell grid abstraction and selection helpers with drag-paint semantics tests (§6.4, §12)
- [ ] Built-in factories and DTO mappers for shipped regions/decorations (§11, §15)

## Phase 2: Rendering layer (`Ambit.Avalonia`)

- [ ] Avalonia rendering scaffolding: draw operation host, render registries, Skia lease integration (§2, §5, §9, §13)
- [ ] Shared Skia resource management for allocation-conscious paints, paths, text, and buffers (§9)
- [ ] Built-in region renderers for rectangle, polygon, polyline, line, and ellipse (§6.1, §9)
- [ ] Built-in decoration renderers for direction indicators and labels (§6.3, §7, §9)
- [ ] Hover/selection/handle rendering overlays driven by `RegionRenderState` (§5, §7, §9)
- [ ] Passive overlay update API with content diffing and invalidate-on-change behavior (§9)
- [ ] Heatmap rendering via low-resolution bitmap plus LUT upscale (§3, §9)
- [ ] Rendering smoke/allocation tests for built-in regions and decorations (§12)

## Phase 3: Interaction layer

- [ ] Interaction contracts and state machine for idle, hover, drag, draw, and cell-paint flows (§2, §10, §12)
- [ ] Region hit-test ordering, hover tracking, and cursor resolution logic (§10, §12)
- [ ] Dragging handles and regions with commit-only change publication (§10, §12)
- [ ] New-region creation flows for built-in region kinds (§3, §10, §12)
- [ ] Cell-grid paint interaction with consistent select/deselect stroke behavior (§6.4, §10, §12)
- [ ] Avalonia `RegionEditorControl` bridging pointer events to the controller (§2, §10)

## Phase 4: Sample gallery (`samples/Ambit.Sample`)

- [ ] Sample app shell with multi-page navigation and shared registration/bootstrap (§14)
- [ ] Region kinds page set with editable rectangle, polygon, polyline, line, and ellipse demos (§14.1)
- [ ] Decorations page with independent line direction indicators and label chip demo (§14.2)
- [ ] Cell grid page with drag-paint select and deselect demo (§14.3)
- [ ] Styling page showing per-instance style differences across mixed regions (§14.4)
- [ ] Passive playback page with multiple passive tiles and allocation counter display (§14.5, §15)
- [ ] Heatmap page with live intensity slider updates (§14.6)
- [ ] Coordinate transform page with pan/zoom plus non-square placeholder image alignment demo (§14.7)
- [ ] Extensibility proof page with one custom region and one custom decoration added only via registration (§14.8, §15)
- [ ] Serialization round-trip page with save/load DTO reconstruction demo (§14.9, §15)

## Phase 5: Acceptance pass

- [ ] Verify `Ambit.Core` builds with nullable enabled and no Avalonia dependency (§15)
- [ ] Verify passive playback steady-state render path shows no measurable managed heap growth (§15)
- [ ] Verify all built-in region boundary hit-tests with exact-edge and tolerance cases (§12, §15)
- [ ] Verify custom region extensibility requires only a new implementation plus registry entry (§6.2, §14.8, §15)
- [ ] Verify custom decoration extensibility requires only a new implementation plus registry entry (§6.3, §14.8, §15)
- [ ] Verify a line with two independent direction indicators works without line specialization (§6.3, §14.2, §15)
- [ ] Verify DTO round-trip preserves geometry, style, label, and decoration state exactly (§11, §14.9, §15)
- [ ] Verify the full sample gallery builds and runs using only the public API surface (§14, §15)
