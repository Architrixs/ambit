using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

public sealed class SerializationRoundTripPage : UserControl
{
    private readonly RegionEditorControl _editor;
    private readonly RegionEditController _controller;
    private readonly IRegionTypeRegistry _typeRegistry;
    private readonly TextBox _jsonTextBox;

    public SerializationRoundTripPage()
    {
        // Setup type registry with built-ins + custom types (from extensibility proof)
        _typeRegistry = new RegionTypeRegistry().RegisterBuiltInTypes();
        _typeRegistry.Register(new CircleRegionFactory());
        _typeRegistry.Register(new CountBadgeDecorationFactory());

        _controller = new RegionEditController();
        _editor = new RegionEditorControl(_controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // Prepopulate with a mix of regions and decorations
        var defaultStyle = new RegionStyle { StrokeColorHex = "#3B82F6", FillColorHex = "#60A5FA", FillOpacity = 0.2 };
        var rect = new RectangleRegion(new NormalizedPoint(0.1, 0.1), new NormalizedPoint(0.5, 0.4), defaultStyle, label: "Zone Alpha");
        
        var circleDec = new CountBadgeDecoration(new NormalizedPoint(0.7, 0.7), 42);
        var circle = new CircleRegion(new NormalizedPoint(0.7, 0.7), 0.15, defaultStyle, decorations: new[] { circleDec }, label: "Circle Beta");
        
        _controller.SetRegions(new IEditableRegion[] { rect, circle });

        // Left controls panel
        var controlsPanel = new StackPanel
        {
            Width = 260,
            Spacing = 12,
            Margin = new Thickness(16),
            VerticalAlignment = VerticalAlignment.Top,
        };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "DTO Serialization",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var description = new TextBlock
        {
            Text = "Demonstrates saving/loading regions to and from plain DTOs:\n\n" +
                   "1. Modify or move the shapes on the right.\n\n" +
                   "2. Click 'Save to DTO & JSON' to serialize the current state.\n\n" +
                   "3. Modify them further or clear the canvas.\n\n" +
                   "4. Click 'Load from JSON & DTO' to reconstruct them.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.LightGray,
            FontSize = 13,
        };
        controlsPanel.Children.Add(description);

        var saveButton = new Button
        {
            Content = "Save to DTO & JSON",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        saveButton.Click += (s, e) => SaveRegions();
        controlsPanel.Children.Add(saveButton);

        var clearButton = new Button
        {
            Content = "Clear Canvas",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        clearButton.Click += (s, e) =>
        {
            _controller.SetRegions(Array.Empty<IEditableRegion>());
            _controller.CancelActiveOperation();
        };
        controlsPanel.Children.Add(clearButton);

        var loadButton = new Button
        {
            Content = "Load from JSON & DTO",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        loadButton.Click += (s, e) => LoadRegions();
        controlsPanel.Children.Add(loadButton);

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Serialized JSON State:",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 8, 0, 0),
        });

        _jsonTextBox = new TextBox
        {
            Height = 250,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            IsReadOnly = true,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
        };
        controlsPanel.Children.Add(_jsonTextBox);

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

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(290) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(controlsPanel, 0);
        Grid.SetColumn(canvasContainer, 1);

        grid.Children.Add(controlsPanel);
        grid.Children.Add(canvasContainer);

        Content = grid;

        // Perform initial save to populate JSON textbox
        SaveRegions();
    }

    private void SaveRegions()
    {
        try
        {
            // Map runtime regions to DTOs using type registry
            var dtos = _controller.Regions.Select(r => _typeRegistry.ToDto(r)).ToList();

            // Serialize to JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(dtos, options);
            _jsonTextBox.Text = json;
        }
        catch (Exception ex)
        {
            _jsonTextBox.Text = $"Error saving: {ex.Message}";
        }
    }

    private void LoadRegions()
    {
        try
        {
            var json = _jsonTextBox.Text;
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            // Deserialize to DTOs
            var dtos = JsonSerializer.Deserialize<List<RegionDto>>(json);
            if (dtos == null)
            {
                return;
            }

            // Map DTOs back to runtime regions using type registry
            var regions = dtos.Select(dto => _typeRegistry.CreateRegion(dto)).ToList();

            _controller.SetRegions(regions);
            _controller.CancelActiveOperation();
        }
        catch (Exception ex)
        {
            _jsonTextBox.Text = $"Error loading: {ex.Message}";
        }
    }
}
