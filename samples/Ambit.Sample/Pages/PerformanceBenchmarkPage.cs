using System;
using System.Collections.Generic;
using System.Linq;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

public sealed class PerformanceBenchmarkPage : UserControl, IDisposable
{
    private readonly RegionEditorControl _editor;
    private readonly RegionEditController _controller;
    private readonly ComboBox _regionCountCombobox;
    private readonly TextBlock _statusText;
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

        var controlsPanel = new StackPanel { Spacing = 8 };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Stress test",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
        });

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Throw a lot of shapes on the canvas and see how it feels to pan and zoom.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#64748B")),
            FontSize = 11,
            LineHeight = 16,
        });

        controlsPanel.Children.Add(new TextBlock { Text = "How many shapes?", FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#334155")) });
        
        _regionCountCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "50 shapes", "100 shapes", "150 shapes", "200 shapes" },
            SelectedIndex = 3,
        };
        controlsPanel.Children.Add(_regionCountCombobox);

        var drawButton = new Button
        {
            Content = "Generate",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 0),
        };
        drawButton.Click += (_, _) => GenerateRegions();
        controlsPanel.Children.Add(drawButton);

        var resetButton = new Button
        {
            Content = "Clear",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        resetButton.Click += (_, _) => ResetCanvas();
        controlsPanel.Children.Add(resetButton);

        controlsPanel.Children.Add(new Separator { Background = new SolidColorBrush(Color.Parse("#E2E8F0")), Margin = new Thickness(0, 4, 0, 4) });

        _statusText = new TextBlock
        {
            Text = "Ready",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#475569")),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 16,
        };

        var statusBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6),
            Child = _statusText,
        };
        controlsPanel.Children.Add(statusBorder);

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Tip: hold right-click to pan, scroll to zoom.",
            FontSize = 10,
            FontStyle = FontStyle.Italic,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
            TextWrapping = TextWrapping.Wrap,
        });

        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(6),
            Padding = new Thickness(6),
            VerticalAlignment = VerticalAlignment.Top,
            Child = controlsPanel,
        };

        _canvasContainer = new Grid
        {
            Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
            ClipToBounds = true,
        };
        _canvasContainer.Children.Add(_editor);

        var rightBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(6),
            ClipToBounds = true,
            Child = _canvasContainer,
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(card, 0);
        Grid.SetColumn(rightBorder, 1);

        grid.Children.Add(card);
        grid.Children.Add(rightBorder);

        Content = grid;

        GenerateRegions();
    }

    private void ResetCanvas()
    {
        _controller.SetRegions(Array.Empty<IEditableRegion>());
        _editor.InvalidateVisual();
        _statusText.Text = "Cleared — 0 shapes on canvas.";
    }

    private void GenerateRegions()
    {
        var random = new Random(1337);
        
        var count = _regionCountCombobox.SelectedIndex switch
        {
            0 => 50,
            1 => 100,
            2 => 150,
            3 => 200,
            _ => 200
        };

        var hexColors = new[] { "#3B82F6", "#10B981", "#EF4444", "#F59E0B", "#EC4899", "#8B5CF6" };
        var newRegions = new List<IEditableRegion>();

        for (var i = 0; i < count; i++)
        {
            var shapeType = random.Next(4);
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
            };
            var label = $"Shape {i + 1}";
            IEditableRegion region = shapeType switch
            {
                0 => new RectangleRegion(new NormalizedPoint(cx, cy), new NormalizedPoint(cx + w, cy + h), style, label: label),
                1 => new EllipseRegion(new NormalizedPoint(cx, cy), new NormalizedPoint(cx + w, cy + h), style, label: label),
                2 => new LineRegion(new NormalizedPoint(cx, cy), new NormalizedPoint(cx + w, cy + h), style, label: label),
                _ => new PolygonRegion(new[] { new NormalizedPoint(cx, cy), new NormalizedPoint(cx + w, cy), new NormalizedPoint(cx + w / 2d, cy + h) }, style, label: label)
            };
            newRegions.Add(region);
        }

        _controller.SetRegions(newRegions);
        _editor.FitToCanvas();
        _editor.InvalidateVisual();
        _statusText.Text = $"Showing {count} shapes — try panning and zooming.";
    }

    public void Dispose()
    {
        _editor.Dispose();
    }
}
