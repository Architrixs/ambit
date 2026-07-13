# What is Ambit?

Ambit is an interactive drawing and passive annotation overlay library built for .NET 10 and Avalonia UI. It is designed to bridge the gap between abstract geometric shapes (regions) and real-time UI components, ensuring high rendering performance and complete design flexibility.

## Key Scenarios

While Ambit was initially conceived to support **Video Management Systems (VMS)** rendering Regions of Interest (ROIs), tripwire lines, and motion-sensitivity grids over camera streams, it is fully domain-neutral:

* **Video Analytics**: Draw lines to detect crossing events, or polygon grids to set up zone detection rules.
* **Document & Image Markup**: Place comment markers, rectangles, and highlight ellipses over photographs, blueprints, or PDF pages.
* **Cell Grids**: Interact with multi-select matrices for setting up sensitivity values or custom masking.

## Three-Tier Architecture

To achieve clean separation of concerns and platform independence, Ambit splits its implementation into three layers:

```
┌───────────────────────────────────────────────┐
│                 Ambit.Core                    │
│   (Pure C# • Geometry Models • Controller)    │
└───────────────────────┬───────────────────────┘
                        ▼
┌───────────────────────────────────────────────┐
│               Ambit.Avalonia                  │
│  (Skia Draw Operations • Controls & Bridges)  │
└───────────────────────┬───────────────────────┘
                        ▼
┌───────────────────────────────────────────────┐
│               Consumer App                    │
│    (Desktop Window • WebAssembly Browser)     │
└───────────────────────────────────────────────┘
```
