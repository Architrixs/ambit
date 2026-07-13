using System;
using System.Collections.Generic;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

public sealed class DecorationsPage : UserControl
{
    public DecorationsPage()
    {
        var controller = new RegionEditController();
        var editor = new RegionEditorControl(controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // Populate with decorations
        var lineStyle = new RegionStyle { StrokeColorHex = "#F6B726", StrokeThickness = 3.0 };
        var lineDec1 = new DirectionIndicatorDecoration(new NormalizedPoint(0.3, 0.5), directionSign: 1);
        var lineDec2 = new DirectionIndicatorDecoration(new NormalizedPoint(0.7, 0.5), directionSign: -1);
        var tripwire = new LineRegion(
            new NormalizedPoint(0.2, 0.5),
            new NormalizedPoint(0.8, 0.5),
            lineStyle,
            decorations: new IDecoration[] { lineDec1, lineDec2 },
            label: null);

        var rectStyle = new RegionStyle
        {
            StrokeColorHex = "#2680EB",
            FillColorHex = "#2680EB",
            FillOpacity = 0.15,
            StrokeDashPattern = new double[] { 6.0, 4.0 },
            LabelStyle = new LabelStyle
            {
                TextColorHex = "#FFFFFF",
                BackgroundColorHex = "#1F2937",
                FontSize = 12.0,
            }
        };
        var labelDec = new LabelDecoration(new NormalizedPoint(0.5, 0.5), "Detection Area A");
        var activeZone = new RectangleRegion(
            new NormalizedPoint(0.3, 0.1),
            new NormalizedPoint(0.7, 0.4),
            rectStyle,
            decorations: new IDecoration[] { labelDec });

        controller.SetRegions(new IEditableRegion[] { tripwire, activeZone });

        // Controls panel
        var controlsPanel = new StackPanel
        {
            Width = 240,
            Spacing = 12,
            Margin = new Thickness(16),
            VerticalAlignment = VerticalAlignment.Top,
        };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Decorations",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var description = new TextBlock
        {
            Text = "Demonstrates composable adornments attached to region geometry:\n\n" +
                   "1. Click the orange arrows on the tripwire line. They toggle direction signs immediately and independently on click.\n\n" +
                   "2. The rectangle includes a custom Label Decoration chip rendered at its center.\n\n" +
                   "3. Drag the shapes or their vertices; decorations follow the geometry dynamically.",
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
