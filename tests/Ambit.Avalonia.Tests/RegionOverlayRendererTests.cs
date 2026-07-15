using Ambit.Avalonia.Heatmaps;
using Ambit.Avalonia.Rendering;
using FluentAssertions;
using SkiaSharp;

namespace Ambit.Avalonia.Tests;

public sealed class RegionOverlayRendererTests
{
    [Fact]
    public void Render_DoesNotThrowForBuiltInRegionsDecorationsAndHeatmap()
    {
        var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
        renderRegistry.Register(new TestDecorationRenderer());
        using var renderer = new RegionOverlayRenderer(renderRegistry);
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));

        var regions = CreateAllocationRegions();
        var state = new RegionRenderState
        {
            HoveredRegionId = regions[0].Id,
            SelectedRegionId = regions[1].Id,
            HoveredHandleIndex = 1,
        };
        var heatmap = new HeatmapLayer
        {
            Rows = 4,
            Columns = 4,
            Intensities =
            [
                (byte)0, 32, 64, 96,
                16, 48, 80, 112,
                24, 56, 88, 160,
                40, 72, 128, 255,
            ],
            Opacity = 0.5f,
        };

        var action = () => renderer.Render(surface.Canvas, regions, state, new TestCoordinateTransform(800, 600), heatmap);

        action.Should().NotThrow();
    }

    [Fact]
    public void Render_WarmedUpLoopStaysWithinSmallManagedAllocationBudget()
    {
        var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
        renderRegistry.Register(new TestDecorationRenderer());
        using var renderer = new RegionOverlayRenderer(renderRegistry);
        using var surface = SKSurface.Create(new SKImageInfo(640, 480));

        var regions = CreateRegions();
        var state = new RegionRenderState
        {
            SelectedRegionId = regions[0].Id,
        };
        var transform = new TestCoordinateTransform(640, 480);

        for (var index = 0; index < 5; index++)
        {
            renderer.Render(surface.Canvas, regions, state, transform);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 200; index++)
        {
            renderer.Render(surface.Canvas, regions, state, transform);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var averagePerRender = allocated / 200d;
        averagePerRender.Should().BeLessThan(3_000d);
    }

    private static IReadOnlyList<IRegion> CreateRegions()
    {
        var sharedStyle = new RegionStyle
        {
            StrokeColorHex = "#33A1FD",
            StrokeThickness = 2.5,
            FillColorHex = "#44AAFF",
            FillOpacity = 0.25,
            StrokeDashPattern = [6, 3],
            LabelStyle = new LabelStyle
            {
                TextColorHex = "#FFFFFF",
                BackgroundColorHex = "#111111",
                FontSize = 12,
            },
        };

        return
        [
            new RectangleRegion(
                new NormalizedPoint(0.1, 0.1),
                new NormalizedPoint(0.3, 0.35),
                sharedStyle,
                decorations:
                [
                    new TestDecoration(new NormalizedPoint(0.12, 0.08)),
                ],
                label: "Rectangle",
                lockAspectRatio: true),
            new PolygonRegion(
                [
                    new NormalizedPoint(0.45, 0.1),
                    new NormalizedPoint(0.75, 0.14),
                    new NormalizedPoint(0.7, 0.35),
                    new NormalizedPoint(0.5, 0.32),
                ],
                sharedStyle,
                label: "Polygon"),
            new PolylineRegion(
                [
                    new NormalizedPoint(0.1, 0.55),
                    new NormalizedPoint(0.25, 0.7),
                    new NormalizedPoint(0.4, 0.62),
                ],
                sharedStyle,
                label: "Polyline"),
            new LineRegion(
                new NormalizedPoint(0.5, 0.55),
                new NormalizedPoint(0.9, 0.75),
                sharedStyle,
                decorations:
                [
                    new TestDecoration(new NormalizedPoint(0.58, 0.59)),
                    new TestDecoration(new NormalizedPoint(0.82, 0.71)),
                ],
                label: "Line"),
            new EllipseRegion(
                new NormalizedPoint(0.55, 0.15),
                new NormalizedPoint(0.9, 0.42),
                sharedStyle,
                decorations:
                [
                    new TestDecoration(new NormalizedPoint(0.62, 0.1)),
                ],
                label: "Ellipse"),
        ];
    }

    private sealed class TestDecoration(NormalizedPoint anchor) : IDecoration
    {
        public string TypeId => "test-decoration";
        public NormalizedPoint Anchor { get; } = anchor;
        public bool IsInteractive => false;
    }

    private sealed class TestDecorationRenderer : IDecorationRenderer
    {
        public string TypeId => "test-decoration";
        public void Render(SKCanvas canvas, IDecoration decoration, IRegion owner, RegionRenderState state, ICoordinateTransform transform, SkiaRenderResources resources)
        {
        }
    }

    private static IReadOnlyList<IRegion> CreateAllocationRegions()
    {
        var style = new RegionStyle
        {
            StrokeColorHex = "#33A1FD",
            StrokeThickness = 2.5,
            FillColorHex = "#44AAFF",
            FillOpacity = 0.25,
        };

        return
        [
            new RectangleRegion(
                new NormalizedPoint(0.1, 0.1),
                new NormalizedPoint(0.3, 0.35),
                style),
            new PolygonRegion(
                [
                    new NormalizedPoint(0.45, 0.1),
                    new NormalizedPoint(0.75, 0.14),
                    new NormalizedPoint(0.7, 0.35),
                    new NormalizedPoint(0.5, 0.32),
                ],
                style),
            new PolylineRegion(
                [
                    new NormalizedPoint(0.1, 0.55),
                    new NormalizedPoint(0.25, 0.7),
                    new NormalizedPoint(0.4, 0.62),
                ],
                style),
            new LineRegion(
                new NormalizedPoint(0.5, 0.55),
                new NormalizedPoint(0.9, 0.75),
                style),
            new EllipseRegion(
                new NormalizedPoint(0.55, 0.15),
                new NormalizedPoint(0.9, 0.42),
                style),
        ];
    }

    private sealed class TestCoordinateTransform(double width, double height) : ICoordinateTransform
    {
        public ControlPoint ToControlSpace(NormalizedPoint p)
        {
            return new ControlPoint(p.X * width, p.Y * height);
        }

        public NormalizedPoint ToNormalizedSpace(ControlPoint controlPoint)
        {
            return new NormalizedPoint(controlPoint.X / width, controlPoint.Y / height);
        }
    }
}
