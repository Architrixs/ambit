using System;
using System.Collections.Generic;
using Ambit.Avalonia.Controls;
using Ambit.Avalonia.Heatmaps;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

public sealed class HeatmapPage : UserControl
{
    private readonly RegionOverlayControl _overlay;
    private readonly Slider _intensitySlider;
    private readonly Slider _opacitySlider;
    private const int Rows = 8;
    private const int Cols = 8;

    public HeatmapPage()
    {
        _overlay = new RegionOverlayControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

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
            Text = "Live Heatmap",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var description = new TextBlock
        {
            Text = "Demonstrates heatmap overlay rendering:\n\n" +
                   "- Renders an 8x8 low-resolution intensity buffer upscaled smoothly using Skia's linear filtering.\n\n" +
                   "- Use the sliders to mutate the hotspot intensity and overall overlay opacity in real-time.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.LightGray,
            FontSize = 13,
        };
        controlsPanel.Children.Add(description);

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Center Intensity:",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 12, 0, 0),
        });

        _intensitySlider = new Slider
        {
            Minimum = 0,
            Maximum = 255,
            Value = 180,
        };
        _intensitySlider.ValueChanged += (s, e) => UpdateHeatmap();
        controlsPanel.Children.Add(_intensitySlider);

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Overall Opacity:",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 8, 0, 0),
        });

        _opacitySlider = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = 0.65,
        };
        _opacitySlider.ValueChanged += (s, e) => UpdateHeatmap();
        controlsPanel.Children.Add(_opacitySlider);

        // Right canvas area with background frame
        var canvasContainer = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1A1A1A")),
            BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(16),
            Child = _overlay,
        };

        // Main Layout
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(controlsPanel, 0);
        Grid.SetColumn(canvasContainer, 1);

        grid.Children.Add(controlsPanel);
        grid.Children.Add(canvasContainer);

        Content = grid;

        // Initial update
        UpdateHeatmap();
    }

    private void UpdateHeatmap()
    {
        var centerIntensity = (byte)_intensitySlider.Value;
        var opacity = (float)_opacitySlider.Value;

        var intensities = new byte[Rows * Cols];
        for (var r = 0; r < Rows; r++)
        {
            for (var c = 0; c < Cols; c++)
            {
                // Calculate distance from center (3.5, 3.5)
                var dx = c - 3.5;
                var dy = r - 3.5;
                var dist = Math.Sqrt((dx * dx) + (dy * dy));

                // Normal falloff
                var val = Math.Max(0, 1.0 - (dist / 4.0)) * centerIntensity;
                intensities[(r * Cols) + c] = (byte)val;
            }
        }

        var heatmap = new HeatmapLayer
        {
            Rows = Rows,
            Columns = Cols,
            Intensities = intensities,
            Opacity = opacity,
        };

        _overlay.UpdateHeatmap(heatmap);
    }
}
