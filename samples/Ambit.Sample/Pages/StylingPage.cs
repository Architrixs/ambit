using System;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

public sealed class StylingPage : UserControl
{
    public StylingPage()
    {
        var controller = new RegionEditController();
        var editor = new RegionEditorControl(controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // 1. Dash-pattern cyan rectangle with no fill and custom handles
        var style1 = new RegionStyle
        {
            StrokeColorHex = "#06B6D4",
            StrokeThickness = 2.0,
            StrokeDashPattern = new double[] { 8.0, 4.0 },
            DefaultHandleStyle = new HandleStyle
            {
                RadiusPixels = 6.0,
                FillColorHex = "#06B6D4",
                StrokeColorHex = "#FFFFFF",
            },
            LabelStyle = new LabelStyle
            {
                TextColorHex = "#06B6D4",
                BackgroundColorHex = "#083344",
                FontSize = 11.0,
            }
        };
        var region1 = new RectangleRegion(
            new NormalizedPoint(0.1, 0.1),
            new NormalizedPoint(0.4, 0.4),
            style1,
            label: "Dashed Cyan ROI");

        // 2. Thick solid red ellipse with transparent pink fill
        var style2 = new RegionStyle
        {
            StrokeColorHex = "#EF4444",
            StrokeThickness = 4.0,
            FillColorHex = "#FCA5A5",
            FillOpacity = 0.3,
            DefaultHandleStyle = new HandleStyle
            {
                RadiusPixels = 4.0,
                FillColorHex = "#FFFFFF",
                StrokeColorHex = "#EF4444",
            },
            LabelStyle = new LabelStyle
            {
                TextColorHex = "#EF4444",
                BackgroundColorHex = "#450A0A",
                FontSize = 13.0,
            }
        };
        var region2 = new EllipseRegion(
            new NormalizedPoint(0.6, 0.1),
            new NormalizedPoint(0.9, 0.4),
            style2,
            label: "Warning Zone");

        // 3. Polygon with purple styling
        var style3 = new RegionStyle
        {
            StrokeColorHex = "#8B5CF6",
            StrokeThickness = 3.0,
            FillColorHex = "#C4B5FD",
            FillOpacity = 0.15,
            LabelStyle = new LabelStyle
            {
                TextColorHex = "#FFFFFF",
                BackgroundColorHex = "#4C1D95",
                FontSize = 12.0,
            }
        };
        var region3 = new PolygonRegion(
            new[]
            {
                new NormalizedPoint(0.1, 0.6),
                new NormalizedPoint(0.4, 0.6),
                new NormalizedPoint(0.25, 0.9),
            },
            style3,
            label: "Purple Mask");

        // 4. Yellow line crossing with thick dash styling
        var style4 = new RegionStyle
        {
            StrokeColorHex = "#EAB308",
            StrokeThickness = 5.0,
            StrokeDashPattern = new double[] { 15.0, 5.0 },
            LabelStyle = new LabelStyle
            {
                TextColorHex = "#EAB308",
                BackgroundColorHex = "#422006",
                FontSize = 12.0,
            }
        };
        var region4 = new LineRegion(
            new NormalizedPoint(0.6, 0.6),
            new NormalizedPoint(0.9, 0.9),
            style4,
            label: "Dashed Tripwire");

        controller.SetRegions(new IEditableRegion[] { region1, region2, region3, region4 });

        // Left controls panel
        var controlsPanel = new StackPanel
        {
            Width = 240,
            Spacing = 12,
            Margin = new Thickness(16),
            VerticalAlignment = VerticalAlignment.Top,
        };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Per-Instance Styling",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var description = new TextBlock
        {
            Text = "Demonstrates that RegionStyle is configured per instance:\n\n" +
                   "- Cyan: Dashed outline, custom cyan-and-white handle styling.\n\n" +
                   "- Red: Thick stroke, semi-transparent red fill.\n\n" +
                   "- Purple: Standard solid polygon styling.\n\n" +
                   "- Yellow: Very thick dashed line pattern.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.LightGray,
            FontSize = 13,
        };
        controlsPanel.Children.Add(description);

        var canvasContainer = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1A1A1A")),
            BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(16),
            Child = editor,
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(controlsPanel, 0);
        Grid.SetColumn(canvasContainer, 1);

        grid.Children.Add(controlsPanel);
        grid.Children.Add(canvasContainer);

        Content = grid;
    }
}
