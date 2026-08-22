using System;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

public sealed class CellGridPage : UserControl
{
    private readonly RegionEditorControl _editor;
    private readonly TextBlock _statusText;

    public CellGridPage()
    {
        var controller = new RegionEditController();
        controller.CellGrid = new CellGrid(9, 16); // 9 rows, 16 columns (16:9 ratio)
        controller.IsCellPaintMode = true;

        _editor = new RegionEditorControl(controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BackgroundImage = SharedAssets.CameraFeedSKBitmap,
        };

        // Left controls panel
        var controlsPanel = new StackPanel
        {
            Spacing = 8,
        };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Cell Grid",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var description = new TextBlock
        {
            Text = "Demonstrates cell-grid drag painting & hover:\n\n" +
                   "1. Hover over cells to see subtle live preview.\n" +
                   "2. Click and drag over cells to paint or erase.\n" +
                   "3. Adjust grid resolution below (defaults to 16:9 video aspect ratio).",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#64748B")),
            FontSize = 13,
        };
        controlsPanel.Children.Add(description);

        // Columns slider
        var colsLabel = new TextBlock
        {
            Text = "Columns per Length: 16",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 8, 0, 0)
        };
        controlsPanel.Children.Add(colsLabel);

        var colsSlider = new Slider
        {
            Minimum = 4,
            Maximum = 64,
            Value = 16,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
        };
        controlsPanel.Children.Add(colsSlider);

        // Rows slider
        var rowsLabel = new TextBlock
        {
            Text = "Rows per Height: 9",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 4, 0, 0)
        };
        controlsPanel.Children.Add(rowsLabel);

        var rowsSlider = new Slider
        {
            Minimum = 4,
            Maximum = 64,
            Value = 9,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
        };
        controlsPanel.Children.Add(rowsSlider);

        void UpdateCellGrid()
        {
            var cols = (int)colsSlider.Value;
            var rows = (int)rowsSlider.Value;
            colsLabel.Text = $"Columns per Length: {cols}";
            rowsLabel.Text = $"Rows per Height: {rows}";
            controller.SelectedCells.Clear();
            controller.CellGrid = new CellGrid(rows, cols);
            controller.CancelActiveOperation();
            _editor.InvalidateVisual();
        }

        colsSlider.PropertyChanged += (s, e) => { if (e.Property == Slider.ValueProperty) UpdateCellGrid(); };
        rowsSlider.PropertyChanged += (s, e) => { if (e.Property == Slider.ValueProperty) UpdateCellGrid(); };

        _statusText = new TextBlock
        {
            Text = "Selected cells: 0",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 12, 0, 0),
        };
        controlsPanel.Children.Add(_statusText);

        controller.CellsChanged += (s, e) =>
        {
            _statusText.Text = $"Selected cells: {controller.SelectedCells.Count}";
        };

        var clearButton = new Button
        {
            Content = "Clear Selection",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 12, 0, 0),
        };
        clearButton.Click += (s, e) =>
        {
            controller.SelectedCells.Clear();
            controller.CancelActiveOperation();
            _statusText.Text = "Selected cells: 0";
        };
        controlsPanel.Children.Add(clearButton);

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

        // Right canvas area with camera background frame
        var canvasContainer = SharedAssets.CreatePreviewContainer(_editor);
        canvasContainer.Margin = new Thickness(6);

        // Main Layout
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(card, 0);
        Grid.SetColumn(canvasContainer, 1);

        grid.Children.Add(card);
        grid.Children.Add(canvasContainer);

        Content = grid;
    }
}
