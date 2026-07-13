using System;
using System.Collections.Generic;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

public sealed class RegionKindsPage : UserControl
{
    private readonly RegionEditorControl _editor;
    private readonly CheckBox _aspectRatioLockCheckbox;

    public RegionKindsPage()
    {
        var controller = new RegionEditController();
        _editor = new RegionEditorControl(controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // Add some initial shapes
        var style = new RegionStyle { StrokeColorHex = "#2680EB", FillColorHex = "#2680EB", FillOpacity = 0.2 };
        var rect = new RectangleRegion(new NormalizedPoint(0.1, 0.1), new NormalizedPoint(0.4, 0.4), style);
        var ellipse = new EllipseRegion(new NormalizedPoint(0.6, 0.1), new NormalizedPoint(0.9, 0.4), style);
        controller.SetRegions(new IEditableRegion[] { rect, ellipse });

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
            Text = "Region Kinds",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Select Draw Mode:",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
        });

        var selectModeButton = new RadioButton { Content = "Select / Edit", IsChecked = true };
        var rectButton = new RadioButton { Content = "Draw Rectangle" };
        var ellipseButton = new RadioButton { Content = "Draw Ellipse" };
        var lineButton = new RadioButton { Content = "Draw Line" };
        var polylineButton = new RadioButton { Content = "Draw Polyline" };
        var polygonButton = new RadioButton { Content = "Draw Polygon" };

        selectModeButton.IsCheckedChanged += (s, e) => { if (selectModeButton.IsChecked == true) controller.ActiveDrawTypeId = null; };
        rectButton.IsCheckedChanged += (s, e) => { if (rectButton.IsChecked == true) controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId; };
        ellipseButton.IsCheckedChanged += (s, e) => { if (ellipseButton.IsChecked == true) controller.ActiveDrawTypeId = EllipseRegion.EllipseTypeId; };
        lineButton.IsCheckedChanged += (s, e) => { if (lineButton.IsChecked == true) controller.ActiveDrawTypeId = LineRegion.LineTypeId; };
        polylineButton.IsCheckedChanged += (s, e) => { if (polylineButton.IsChecked == true) controller.ActiveDrawTypeId = PolylineRegion.PolylineTypeId; };
        polygonButton.IsCheckedChanged += (s, e) => { if (polygonButton.IsChecked == true) controller.ActiveDrawTypeId = PolygonRegion.PolygonTypeId; };

        controlsPanel.Children.Add(selectModeButton);
        controlsPanel.Children.Add(rectButton);
        controlsPanel.Children.Add(ellipseButton);
        controlsPanel.Children.Add(lineButton);
        controlsPanel.Children.Add(polylineButton);
        controlsPanel.Children.Add(polygonButton);

        _aspectRatioLockCheckbox = new CheckBox
        {
            Content = "Lock Aspect Ratio (Rect/Ellipse)",
            IsChecked = false,
            Margin = new Thickness(0, 8, 0, 0),
        };
        _aspectRatioLockCheckbox.IsCheckedChanged += (s, e) =>
        {
            var isLocked = _aspectRatioLockCheckbox.IsChecked == true;
            foreach (var region in controller.Regions)
            {
                if (region is RectangleRegion r) r.LockAspectRatio = isLocked;
                if (region is EllipseRegion el) el.LockAspectRatio = isLocked;
            }
            if (controller.DrawingRegion is RectangleRegion dr) dr.LockAspectRatio = isLocked;
            if (controller.DrawingRegion is EllipseRegion del) del.LockAspectRatio = isLocked;
            _editor.InvalidateVisual();
        };
        controlsPanel.Children.Add(_aspectRatioLockCheckbox);

        // Wire up new region creations to apply current aspect ratio lock
        controller.RegionsChanged += (s, e) =>
        {
            var isLocked = _aspectRatioLockCheckbox.IsChecked == true;
            foreach (var region in controller.Regions)
            {
                if (region is RectangleRegion r) r.LockAspectRatio = isLocked;
                if (region is EllipseRegion el) el.LockAspectRatio = isLocked;
            }
        };

        var clearButton = new Button
        {
            Content = "Clear All Regions",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 16, 0, 0),
        };
        clearButton.Click += (s, e) =>
        {
            controller.SetRegions(Array.Empty<IEditableRegion>());
            controller.CancelActiveOperation();
        };
        controlsPanel.Children.Add(clearButton);

        var hintTextBlock = new TextBlock
        {
            Text = "Instructions:\n1. Click and drag on the canvas to draw new shapes when in a Draw Mode.\n2. In Select Mode, click a shape to select it, then drag the body to translate or drag the handles to resize/reshape.\n3. Press Escape to cancel drawing or dragging.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            FontSize = 12,
            Margin = new Thickness(0, 16, 0, 0),
        };
        controlsPanel.Children.Add(hintTextBlock);

        // Right canvas area with background frame
        var canvasContainer = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1A1A1A")),
            BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(16),
            Child = _editor,
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
    }
}
