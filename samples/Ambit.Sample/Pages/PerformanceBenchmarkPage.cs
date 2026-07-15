using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ambit.Avalonia.Controls;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;

namespace Ambit.Sample.Pages;

public sealed class PerformanceBenchmarkPage : UserControl, IDisposable
{
    private readonly RegionEditorControl _editor;
    private readonly RegionEditController _controller;
    private readonly ComboBox _regionCountCombobox;
    private readonly TextBlock _metricsText;
    private readonly Grid _canvasContainer;

    public PerformanceBenchmarkPage()
    {
        _controller = new RegionEditController();
        _editor = new RegionEditorControl(_controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BackgroundImage = SharedAssets.SchoenbrunnSKBitmap,
            IsPanZoomEnabled = true,
        };

        // 1. Sidebar Controls (compact 220px layout)
        var controlsPanel = new StackPanel { Spacing = 12 };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Performance Benchmark",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 4),
        });

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Verify performance under load. Canvas is fully editable, pannable (Right-click + Drag), and zoomable (Scroll wheel).",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
            FontSize = 11,
            LineHeight = 16,
        });

        // Shape count selector (max 200)
        controlsPanel.Children.Add(new TextBlock { Text = "Region Count:", FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#94A3B8")), Margin = new Thickness(0, 4, 0, 0) });
        
        _regionCountCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "50 Shapes", "100 Shapes", "150 Shapes", "200 Shapes" },
            SelectedIndex = 3, // 200 Shapes default
        };
        controlsPanel.Children.Add(_regionCountCombobox);

        // Action Buttons
        var drawButton = new Button
        {
            Content = "Generate & Draw Shapes",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 0),
        };
        drawButton.Click += (s, e) => GenerateRegions();
        controlsPanel.Children.Add(drawButton);

        var runBenchmarkButton = new Button
        {
            Content = "Run Skia Benchmark",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        runBenchmarkButton.Click += (s, e) => RunBenchmark();
        controlsPanel.Children.Add(runBenchmarkButton);

        var resetButton = new Button
        {
            Content = "Reset Canvas",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        resetButton.Click += (s, e) => ResetCanvas();
        controlsPanel.Children.Add(resetButton);

        // Metrics output box
        controlsPanel.Children.Add(new Separator { Background = new SolidColorBrush(Color.Parse("#334155")), Margin = new Thickness(0, 4, 0, 4) });
        controlsPanel.Children.Add(new TextBlock { Text = "Benchmark Results:", FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#94A3B8")) });

        _metricsText = new TextBlock
        {
            Text = "Click 'Generate & Draw' to begin, then run the benchmark to calculate Skia drawing speed.",
            FontSize = 11,
            FontWeight = FontWeight.Normal,
            Foreground = new SolidColorBrush(Color.Parse("#10B981")), // Neon Green
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
        };

        var metricsBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#0F172A")),
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Child = _metricsText,
        };
        controlsPanel.Children.Add(metricsBorder);

        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E293B")),
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(12),
            Padding = new Thickness(12),
            VerticalAlignment = VerticalAlignment.Top,
            Child = controlsPanel,
        };

        // 2. Right Canvas Area
        _canvasContainer = new Grid
        {
            Background = new SolidColorBrush(Color.Parse("#0F172A")),
            ClipToBounds = true,
        };
        _canvasContainer.Children.Add(_editor);

        var rightBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(12),
            ClipToBounds = true,
            Child = _canvasContainer,
        };

        // 3. Grid layout (compact 220px control bar)
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(card, 0);
        Grid.SetColumn(rightBorder, 1);

        grid.Children.Add(card);
        grid.Children.Add(rightBorder);

        Content = grid;

        // Draw initial 200 shapes on start
        GenerateRegions();
    }

    private void ResetCanvas()
    {
        _controller.SetRegions(Array.Empty<IEditableRegion>());
        _editor.InvalidateVisual();
        _metricsText.Text = "Canvas reset. Total shapes: 0.";
    }

    private void GenerateRegions()
    {
        var random = new Random(1337); // Seed for reproducible layout
        
        var count = _regionCountCombobox.SelectedIndex switch
        {
            0 => 50,
            1 => 100,
            2 => 150,
            3 => 200,
            _ => 200
        };

        var hexColors = new[]
        {
            "#3B82F6", // Blue
            "#10B981", // Green
            "#EF4444", // Red
            "#F59E0B", // Orange
            "#EC4899", // Pink
            "#8B5CF6", // Purple
        };

        var newRegions = new List<IEditableRegion>();

        for (var i = 0; i < count; i++)
        {
            var shapeType = random.Next(4); // 0 = Rect, 1 = Ellipse, 2 = Line, 3 = Polygon

            var cx = 0.05 + random.NextDouble() * 0.7;
            var cy = 0.05 + random.NextDouble() * 0.7;
            var w = 0.05 + random.NextDouble() * 0.15;
            var h = 0.05 + random.NextDouble() * 0.15;

            var strokeColor = hexColors[random.Next(hexColors.Length)];
            var style = new RegionStyle
            {
                StrokeColorHex = strokeColor,
                StrokeThickness = 2.0,
                FillColorHex = strokeColor,
                FillOpacity = 0.15,
                LabelStyle = new LabelStyle
                {
                    TextColorHex = "#FFFFFF",
                    BackgroundColorHex = "#1E293B",
                    FontSize = 10.0,
                    Placement = (LabelPlacement)random.Next(5)
                }
            };

            var label = $"Shape {i + 1}";

            IEditableRegion region = shapeType switch
            {
                0 => new RectangleRegion(new NormalizedPoint(cx, cy), new NormalizedPoint(cx + w, cy + h), style, label: label),
                1 => new EllipseRegion(new NormalizedPoint(cx, cy), new NormalizedPoint(cx + w, cy + h), style, label: label),
                2 => new LineRegion(new NormalizedPoint(cx, cy), new NormalizedPoint(cx + w, cy + h), style, label: label),
                _ => new PolygonRegion(new[]
                {
                    new NormalizedPoint(cx, cy),
                    new NormalizedPoint(cx + w, cy),
                    new NormalizedPoint(cx + w / 2d, cy + h)
                }, style, label: label)
            };

            newRegions.Add(region);
        }

        _controller.SetRegions(newRegions);
        _editor.FitToCanvas();
        _editor.InvalidateVisual();

        var totalVertices = newRegions.Sum(r => r.Vertices.Count);
        _metricsText.Text = $"Generated {count} editable shapes successfully!\n" +
                             $"Total Vertices: {totalVertices}\n\n" +
                             $"Click 'Run Skia Benchmark' to test drawing performance.";
    }

    private void RunBenchmark()
    {
        var regions = _controller.Regions.ToList();
        if (regions.Count == 0)
        {
            _metricsText.Text = "Please draw shapes first before running the benchmark.";
            return;
        }

        _metricsText.Text = "Running benchmark (100 passes)...";

        // Create in-memory Skia surface matching typical demo window size
        using var surface = SKSurface.Create(new SKImageInfo(1280, 800));
        if (surface == null)
        {
            _metricsText.Text = "Failed to initialize Skia surface.";
            return;
        }

        var canvas = surface.Canvas;
        var transform = new SimpleTransform(1280, 800);
        var state = new RegionRenderState();
        var renderer = new RegionOverlayRenderer();

        // Warm up pass
        renderer.Render(canvas, regions, state, transform);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
        {
            renderer.Render(canvas, regions, state, transform);
        }
        sw.Stop();

        var totalMs = sw.Elapsed.TotalMilliseconds;
        var avgMs = totalMs / 100.0;
        var fps = 1000.0 / avgMs;

        var totalVertices = regions.Sum(r => r.Vertices.Count);

        _metricsText.Text = $"Benchmark Results (100 passes):\n\n" +
                             $"Shapes: {regions.Count}\n" +
                             $"Total Vertices: {totalVertices}\n" +
                             $"Avg Render: {avgMs:F2} ms\n" +
                             $"Max Est. Frame Rate: {fps:F1} FPS";
    }

    public void Dispose()
    {
        _editor.Dispose();
    }

    private sealed class SimpleTransform(double w, double h) : ICoordinateTransform
    {
        public ControlPoint ToControlSpace(NormalizedPoint p) => new ControlPoint(p.X * w, p.Y * h);
        public NormalizedPoint ToNormalizedSpace(ControlPoint p) => new NormalizedPoint(p.X / w, p.Y / h);
    }
}
