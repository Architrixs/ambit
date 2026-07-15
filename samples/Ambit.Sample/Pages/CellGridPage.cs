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
        controller.CellGrid = new CellGrid(12, 16); // 12 rows, 16 columns
        controller.IsCellPaintMode = true;

        _editor = new RegionEditorControl(controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // Left controls panel
        var controlsPanel = new StackPanel
        {
            Spacing = 12,
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
            Text = "Demonstrates cell-grid drag painting:\n\n" +
                   "1. Click and drag your pointer over the cells.\n\n" +
                   "2. Starting a drag on an empty cell will SELECT cells as you paint.\n\n" +
                   "3. Starting a drag on an active cell will DESELECT cells as you paint.\n\n" +
                   "4. A single drag stroke is consistently select or deselect.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
            FontSize = 13,
        };
        controlsPanel.Children.Add(description);

        _statusText = new TextBlock
        {
            Text = "Selected cells: 0",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 16, 0, 0),
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
            Margin = new Thickness(0, 16, 0, 0),
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
            Background = new SolidColorBrush(Color.Parse("#1E293B")),
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(12),
            Padding = new Thickness(12),
            VerticalAlignment = VerticalAlignment.Top,
            Child = controlsPanel,
        };

        // Right canvas area with camera background frame
        var canvasContainer = SharedAssets.CreatePreviewContainer(_editor);
        canvasContainer.Margin = new Thickness(12);

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
