using System;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

public sealed class CoordinateTransformPage : UserControl
{
    private readonly RegionEditorControl _editor;
    private readonly PanZoomTransform _transform;
    private readonly Slider _zoomSlider;
    private readonly Slider _panXSlider;
    private readonly Slider _panYSlider;
    private readonly Border _imagePlaceholder;
    private readonly Grid _canvasContainer;

    public CoordinateTransformPage()
    {
        var controller = new RegionEditController();
        _transform = new PanZoomTransform();
        controller.CoordinateTransform = _transform;

        _editor = new RegionEditorControl(controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // Add some regions
        var style = new RegionStyle { StrokeColorHex = "#10B981", FillColorHex = "#10B981", FillOpacity = 0.2 };
        var rect = new RectangleRegion(new NormalizedPoint(0.2, 0.2), new NormalizedPoint(0.5, 0.5), style, label: "Zone 1");
        var line = new LineRegion(new NormalizedPoint(0.6, 0.3), new NormalizedPoint(0.8, 0.7), new RegionStyle { StrokeColorHex = "#EF4444", StrokeThickness = 3.0 }, label: "Tripwire");
        controller.SetRegions(new IEditableRegion[] { rect, line });

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
            Text = "Coordinate Mapping",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var description = new TextBlock
        {
            Text = "Demonstrates that regions remain anchored to the source frame as it undergoes aspect-fit scaling, pan, and zoom.\n\n" +
                   "The gray box represents a 16:9 video frame.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.LightGray,
            FontSize = 13,
        };
        controlsPanel.Children.Add(description);

        controlsPanel.Children.Add(new TextBlock { Text = "Zoom:", FontWeight = FontWeight.SemiBold });
        _zoomSlider = new Slider { Minimum = 0.5, Maximum = 2.0, Value = 1.0 };
        _zoomSlider.ValueChanged += (s, e) => UpdateTransform();
        controlsPanel.Children.Add(_zoomSlider);

        controlsPanel.Children.Add(new TextBlock { Text = "Pan X:", FontWeight = FontWeight.SemiBold });
        _panXSlider = new Slider { Minimum = -300, Maximum = 300, Value = 0 };
        _panXSlider.ValueChanged += (s, e) => UpdateTransform();
        controlsPanel.Children.Add(_panXSlider);

        controlsPanel.Children.Add(new TextBlock { Text = "Pan Y:", FontWeight = FontWeight.SemiBold });
        _panYSlider = new Slider { Minimum = -300, Maximum = 300, Value = 0 };
        _panYSlider.ValueChanged += (s, e) => UpdateTransform();
        controlsPanel.Children.Add(_panYSlider);

        var resetButton = new Button
        {
            Content = "Reset Pan/Zoom",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 16, 0, 0),
        };
        resetButton.Click += (s, e) =>
        {
            _zoomSlider.Value = 1.0;
            _panXSlider.Value = 0;
            _panYSlider.Value = 0;
            UpdateTransform();
        };
        controlsPanel.Children.Add(resetButton);

        // Right canvas area
        _imagePlaceholder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D3748")),
            BorderBrush = new SolidColorBrush(Color.Parse("#4A5568")),
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = "16:9 Image Frame",
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            }
        };

        _canvasContainer = new Grid
        {
            Background = new SolidColorBrush(Color.Parse("#1A1A1A")),
            ClipToBounds = true,
        };
        _canvasContainer.Children.Add(_imagePlaceholder);
        _canvasContainer.Children.Add(_editor);

        var rightBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(16),
            Child = _canvasContainer,
        };

        _canvasContainer.SizeChanged += (s, e) => UpdateTransform();

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(controlsPanel, 0);
        Grid.SetColumn(rightBorder, 1);

        grid.Children.Add(controlsPanel);
        grid.Children.Add(rightBorder);

        Content = grid;
    }

    private void UpdateTransform()
    {
        _transform.ControlBounds = _canvasContainer.Bounds;
        _transform.Zoom = _zoomSlider.Value;
        _transform.PanX = _panXSlider.Value;
        _transform.PanY = _panYSlider.Value;

        var imgRect = _transform.GetImageControlRect();
        if (imgRect.Width > 0 && imgRect.Height > 0)
        {
            _imagePlaceholder.Width = imgRect.Width;
            _imagePlaceholder.Height = imgRect.Height;
            _imagePlaceholder.Margin = new Thickness(imgRect.Left, imgRect.Top, 0, 0);
        }

        _editor.InvalidateVisual();
    }

    private sealed class PanZoomTransform : ICoordinateTransform
    {
        public double PanX { get; set; } = 0.0;
        public double PanY { get; set; } = 0.0;
        public double Zoom { get; set; } = 1.0;
        public Rect ControlBounds { get; set; }
        public double ImageWidth { get; } = 16.0;
        public double ImageHeight { get; } = 9.0;

        public Rect GetImageControlRect()
        {
            if (ControlBounds.Width <= 0 || ControlBounds.Height <= 0)
            {
                return new Rect(0, 0, 0, 0);
            }

            var controlAspect = ControlBounds.Width / ControlBounds.Height;
            var imageAspect = ImageWidth / ImageHeight;

            double fitW, fitH, fitX, fitY;
            if (controlAspect > imageAspect)
            {
                fitH = ControlBounds.Height;
                fitW = fitH * imageAspect;
                fitX = (ControlBounds.Width - fitW) / 2.0;
                fitY = 0;
            }
            else
            {
                fitW = ControlBounds.Width;
                fitH = fitW / imageAspect;
                fitX = 0;
                fitY = (ControlBounds.Height - fitH) / 2.0;
            }

            var centerX = fitX + fitW / 2.0;
            var centerY = fitY + fitH / 2.0;

            var w = fitW * Zoom;
            var h = fitH * Zoom;
            var x = centerX - w / 2.0 + PanX;
            var y = centerY - h / 2.0 + PanY;

            return new Rect(x, y, w, h);
        }

        public ControlPoint ToControlSpace(NormalizedPoint p)
        {
            var rect = GetImageControlRect();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return new ControlPoint(p.X, p.Y);
            }
            return new ControlPoint(
                rect.Left + (p.X * rect.Width),
                rect.Top + (p.Y * rect.Height));
        }

        public NormalizedPoint ToNormalizedSpace(ControlPoint p)
        {
            var rect = GetImageControlRect();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return new NormalizedPoint(0, 0);
            }
            var x = (p.X - rect.Left) / rect.Width;
            var y = (p.Y - rect.Top) / rect.Height;
            return new NormalizedPoint(x, y);
        }
    }
}
