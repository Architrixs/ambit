using Ambit.Sample.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ambit.Avalonia.Controls;
using Ambit.Avalonia.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample.Pages;

// --- Main Page Implementation ---
public sealed class RegionKindsPage : UserControl, IDisposable
{
    private readonly RegionEditorControl _editor;
    private readonly RegionEditController _controller;
    private readonly IRegionTypeRegistry _typeRegistry;
    private readonly HashSet<Guid> _knownRegionIds = new();

    // UI Elements
    private readonly CheckBox _aspectRatioLockCheckbox;
    private readonly Border _propertiesCard;
    private readonly TextBlock _noSelectionText;
    private readonly StackPanel _propertiesStack;
    private readonly TextBox _labelTextbox;
    private readonly ComboBox _placementCombobox;
    private readonly ComboBox _colorCombobox;
    private readonly ComboBox _thicknessCombobox;
    private readonly ComboBox _strokeStyleCombobox;
    private readonly Button _cycleArrowsButton;
    private readonly TextBox _jsonTextBox;

    private bool _isPopulatingUi;

    // Color definitions
    private static readonly (string Name, string Hex)[] ColorsList =
    {
        ("Blue", "#3B82F6"),
        ("Green", "#10B981"),
        ("Red", "#EF4444"),
        ("Yellow", "#F59E0B"),
        ("Pink", "#EC4899"),
        ("Purple", "#8B5CF6"),
        ("Slate", "#64748B"),
    };

    public RegionKindsPage()
    {
        // 1. Setup registries — sample-only decorations/shapes are registered here
        // and CountBadgeDecoration are NOT built into Ambit.Core; they are registered
        // here purely as sample-level extensions to prove the registry pattern.
        _typeRegistry = new RegionTypeRegistry().RegisterBuiltInTypes();
        var circleAspectRatio = (double)SharedAssets.SchoenbrunnSKBitmap.Width / SharedAssets.SchoenbrunnSKBitmap.Height;
        _typeRegistry.Register(new CircleRegionFactory(circleAspectRatio));
        _typeRegistry.Register(new CountBadgeDecorationFactory());
        _typeRegistry.Register(new DirectionIndicatorDecorationFactory());

        var renderRegistry = new RegionRenderRegistry().RegisterBuiltInRenderers();
        renderRegistry.Register(new CircleRegionRenderer());
        renderRegistry.Register(new CountBadgeDecorationRenderer());
        renderRegistry.Register(new DirectionIndicatorDecorationRenderer());
        var customRenderer = new RegionOverlayRenderer(renderRegistry);

        _controller = new RegionEditController
        {
            RegionTypeRegistry = _typeRegistry, // P4: registry-driven drawing for custom types (e.g. Circle)
            DefaultDrawStyle = new RegionStyle
            {
                StrokeColorHex = "#3B82F6",
                StrokeThickness = 2.0,
                FillColorHex = "#3B82F6",
                FillOpacity = 0.15,
                LabelStyle = new LabelStyle
                {
                    TextColorHex = "#FFFFFF",
                    BackgroundColorHex = "#1E293B",
                    FontSize = 11.0,
                    Placement = LabelPlacement.TopLeft
                }
            }
        };
        _editor = new RegionEditorControl(_controller, customRenderer)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BackgroundImage = SharedAssets.SchoenbrunnSKBitmap,
            IsPanZoomEnabled = true,
        };

        // 2. Prepopulate regions with labels and styles
        var defaultStyle = new RegionStyle { StrokeColorHex = "#3B82F6", FillColorHex = "#3B82F6", FillOpacity = 0.15 };
        var activeStyle = new RegionStyle
        {
            StrokeColorHex = "#10B981",
            FillColorHex = "#10B981",
            FillOpacity = 0.15,
            StrokeDashPattern = new double[] { 6.0, 4.0 },
            LabelStyle = new LabelStyle { TextColorHex = "#FFFFFF", BackgroundColorHex = "#064E3B", FontSize = 12d, Placement = LabelPlacement.TopRight }
        };
        var lineStyle = new RegionStyle { StrokeColorHex = "#F59E0B", StrokeThickness = 3.0 };

        var rect = new RectangleRegion(new NormalizedPoint(0.1, 0.1), new NormalizedPoint(0.4, 0.45), defaultStyle, label: "Zone Alpha");
        _knownRegionIds.Add(rect.Id);

        var lineStart = new NormalizedPoint(0.2, 0.7);
        var lineEnd = new NormalizedPoint(0.8, 0.7);
        var arrowDec = new DirectionIndicatorDecoration(
            new NormalizedPoint((lineStart.X + lineEnd.X) / 2, (lineStart.Y + lineEnd.Y) / 2),
            directionSign: 1);
        var tripwire = new LineRegion(
            lineStart,
            lineEnd,
            lineStyle,
            decorations: new IDecoration[] { arrowDec },
            label: "ANPR Tripwire");
        _knownRegionIds.Add(tripwire.Id);

        var activeZone = new RectangleRegion(
            new NormalizedPoint(0.5, 0.15),
            new NormalizedPoint(0.8, 0.45),
            activeStyle,
            label: "Detection Zone");
        _knownRegionIds.Add(activeZone.Id);

        _controller.SetRegions(new IEditableRegion[] { rect, tripwire, activeZone });

        // 3. Sidebar Controls (compact 220px layout)
        var controlsPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0) };

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Editor & Drawings",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 4),
        });
        controlsPanel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse("#ECFDF5")),
            BorderBrush = new SolidColorBrush(Color.Parse("#A7F3D0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 6),
            Margin = new Thickness(0, 0, 0, 6),
            Child = new TextBlock
            {
                Text = "Try it: Circle + DirectionArrow + CountBadge live in samples/Ambit.Sample/Extensibility/ — zero library edits.",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#065F46")),
                TextWrapping = TextWrapping.Wrap,
            }
        });
        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Shapes + arrows/label · styling · extensibility · JSON round-trip",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0,0,0,4),
        });

        // Draw Mode Selector
        controlsPanel.Children.Add(new TextBlock { Text = "Draw Mode:", FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        var selectMode = new RadioButton { Content = "Select / Edit Mode", IsChecked = true, FontSize = 12 };
        var rectMode = new RadioButton { Content = "Draw Rectangle", FontSize = 12 };
        var ellipseMode = new RadioButton { Content = "Draw Ellipse", FontSize = 12 };
        var lineMode = new RadioButton { Content = "Draw Line", FontSize = 12 };
        var polylineMode = new RadioButton { Content = "Draw Polyline", FontSize = 12 };
        var polygonMode = new RadioButton { Content = "Draw Polygon", FontSize = 12 };
        var circleMode = new RadioButton { Content = "Draw Custom Circle", FontSize = 12 };

        selectMode.IsCheckedChanged += (_, _) => { if (selectMode.IsChecked == true) _controller.ActiveDrawTypeId = null; };
        rectMode.IsCheckedChanged += (_, _) => { if (rectMode.IsChecked == true) _controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId; };
        ellipseMode.IsCheckedChanged += (_, _) => { if (ellipseMode.IsChecked == true) _controller.ActiveDrawTypeId = EllipseRegion.EllipseTypeId; };
        lineMode.IsCheckedChanged += (_, _) => { if (lineMode.IsChecked == true) _controller.ActiveDrawTypeId = LineRegion.LineTypeId; };
        polylineMode.IsCheckedChanged += (_, _) => { if (polylineMode.IsChecked == true) _controller.ActiveDrawTypeId = PolylineRegion.PolylineTypeId; };
        polygonMode.IsCheckedChanged += (_, _) => { if (polygonMode.IsChecked == true) _controller.ActiveDrawTypeId = PolygonRegion.PolygonTypeId; };
        circleMode.IsCheckedChanged += (_, _) => { if (circleMode.IsChecked == true) _controller.ActiveDrawTypeId = CircleRegion.CircleTypeId; };

        controlsPanel.Children.Add(selectMode);
        controlsPanel.Children.Add(rectMode);
        controlsPanel.Children.Add(ellipseMode);
        controlsPanel.Children.Add(lineMode);
        controlsPanel.Children.Add(polylineMode);
        controlsPanel.Children.Add(polygonMode);
        controlsPanel.Children.Add(circleMode);

        _aspectRatioLockCheckbox = new CheckBox { Content = "Lock Aspect Ratio", IsChecked = false, Margin = new Thickness(0, 2, 0, 0), FontSize = 12 };
        _aspectRatioLockCheckbox.IsCheckedChanged += (_, _) => ApplyAspectLock();
        controlsPanel.Children.Add(_aspectRatioLockCheckbox);

        var panZoomCheck = new CheckBox { Content = "Enable pan & zoom", IsChecked = true, FontSize = 11 };
        panZoomCheck.IsCheckedChanged += (_, _) => _editor.IsPanZoomEnabled = panZoomCheck.IsChecked == true;
        controlsPanel.Children.Add(panZoomCheck);

        controlsPanel.Children.Add(new TextBlock { Text = "Library: set AmbitViewer.IsPanZoomEnabled = false to disable.", FontSize = 10, Foreground = new SolidColorBrush(Color.Parse("#94A3B8")), TextWrapping = TextWrapping.Wrap });
        controlsPanel.Children.Add(new Separator { Background = new SolidColorBrush(Color.Parse("#E2E8F0")), Margin = new Thickness(0, 4, 0, 4) });

        // Properties Editor Panel
        _propertiesStack = new StackPanel { Spacing = 8 };

        _propertiesStack.Children.Add(new TextBlock { Text = "Label Text:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _labelTextbox = new TextBox { Watermark = "Label Text", FontSize = 12 };
        _labelTextbox.TextChanged += (s, e) => UpdateSelectedRegionLabel();
        _propertiesStack.Children.Add(_labelTextbox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Label Placement:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _placementCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Enum.GetNames<LabelPlacement>(),
            SelectedIndex = 0,
        };
        _placementCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionLabelPlacement();
        _propertiesStack.Children.Add(_placementCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Color Theme:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _colorCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = ColorsList.Select(c => c.Name).ToList(),
            SelectedIndex = 0,
        };
        _colorCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionStyle();
        _propertiesStack.Children.Add(_colorCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Stroke Width:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _thicknessCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "1 px", "2 px", "3 px", "5 px", "8 px" },
            SelectedIndex = 1,
        };
        _thicknessCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionStyle();
        _propertiesStack.Children.Add(_thicknessCombobox);

        _propertiesStack.Children.Add(new TextBlock { Text = "Stroke Style:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _strokeStyleCombobox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = new[] { "Solid", "Dashed" },
            SelectedIndex = 0,
        };
        _strokeStyleCombobox.SelectionChanged += (s, e) => UpdateSelectedRegionStyle();
        _propertiesStack.Children.Add(_strokeStyleCombobox);

        _cycleArrowsButton = new Button
        {
            Content = "Cycle Arrow Direction",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 0),
        };
        _cycleArrowsButton.Click += (s, e) => CycleSelectedLineArrows();
        _propertiesStack.Children.Add(_cycleArrowsButton);

        _noSelectionText = new TextBlock
        {
            Text = "Select a shape on the canvas to configure its style and label properties.",
            FontStyle = FontStyle.Italic,
            Foreground = new SolidColorBrush(Color.Parse("#64748B")),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            LineHeight = 16,
            Margin = new Thickness(0, 4, 0, 4),
        };

        _propertiesCard = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#F8FAFC")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6),
            Child = new Grid
            {
                Children = { _noSelectionText, _propertiesStack }
            }
        };
        controlsPanel.Children.Add(_propertiesCard);

        controlsPanel.Children.Add(new Separator { Background = new SolidColorBrush(Color.Parse("#E2E8F0")), Margin = new Thickness(0, 4, 0, 4) });

        // Actions
        var fitButton = new Button { Content = "Fit to Canvas", HorizontalAlignment = HorizontalAlignment.Stretch };
        fitButton.Click += (_, _) => _editor.FitToCanvas();
        controlsPanel.Children.Add(fitButton);

        var clearButton = new Button { Content = "Clear All Regions", HorizontalAlignment = HorizontalAlignment.Stretch };
        clearButton.Click += (_, _) =>
        {
            _controller.SetRegions(Array.Empty<IEditableRegion>());
            _controller.CancelActiveOperation();
        };
        controlsPanel.Children.Add(clearButton);

        // Quick demo: live JSON preview of current regions (read only)
        _jsonTextBox = new TextBox
        {
            Height = 110,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            FontSize = 9,
            Background = new SolidColorBrush(Color.Parse("#F1F5F9")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
        };

        var jsonExpander = new Expander
        {
            Header = "Live JSON",
            Content = _jsonTextBox,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 4, 0, 0),
        };
        controlsPanel.Children.Add(jsonExpander);

        var scrollViewer = new ScrollViewer
        {
            Content = controlsPanel,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };

        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(6),
            Padding = new Thickness(6),
            Child = scrollViewer,
        };

        // Right preview container
        var canvasContainer = SharedAssets.CreatePreviewContainer(_editor);
        canvasContainer.Margin = new Thickness(6);

        // Page layout grid (compact 220px control bar)
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(card, 0);
        Grid.SetColumn(canvasContainer, 1);

        grid.Children.Add(card);
        grid.Children.Add(canvasContainer);

        Content = grid;

        // Controller Event Subscriptions
        _controller.RegionsChanged += OnRegionsChanged;
        _controller.RenderStateChanged += OnRenderStateChanged;

        // Initial setup
        UpdateSelectionUi();
        SaveToJSON();
    }

    private void ApplyAspectLock()
    {
        var isLocked = _aspectRatioLockCheckbox.IsChecked == true;
        foreach (var region in _controller.Regions)
        {
            if (region is RectangleRegion r) r.LockAspectRatio = isLocked;
            if (region is EllipseRegion el) el.LockAspectRatio = isLocked;
        }
        if (_controller.DrawingRegion is RectangleRegion dr) dr.LockAspectRatio = isLocked;
        if (_controller.DrawingRegion is EllipseRegion del) del.LockAspectRatio = isLocked;
        _editor.InvalidateVisual();
    }

    private void OnRegionsChanged(object? sender, EventArgs e)
    {
        AutoConfigureNewRegions();
        SaveToJSON();
    }

    private void OnRenderStateChanged(object? sender, EventArgs e)
    {
        UpdateSelectionUi();
    }

    private void AutoConfigureNewRegions()
    {
        var regions = _controller.Regions.ToList();
        var changed = false;

        for (var i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            if (!_knownRegionIds.Contains(region.Id))
            {
                _knownRegionIds.Add(region.Id);

                // Auto-configure labels/decorations without throwing away the style
                // that was already applied at draw-start.
                var defaultLabel = $"Zone {_knownRegionIds.Count}";

                var dto = _typeRegistry.ToDto(region);
                var decorations = dto.Decorations;

                // For line regions, attach a direction indicator at the line midpoint
                if (region.TypeId == LineRegion.LineTypeId && dto.Vertices.Count >= 2)
                {
                    var mid = new NormalizedPoint(
                        (dto.Vertices[0].X + dto.Vertices[1].X) / 2,
                        (dto.Vertices[0].Y + dto.Vertices[1].Y) / 2);
                    decorations = new[]
                    {
                        new DecorationDto
                        {
                            TypeId = DirectionIndicatorDecoration.DirectionIndicatorTypeId,
                            Anchor = mid,
                            IsInteractive = true,
                            Properties = new Dictionary<string, string?> { ["directionSign"] = "1" }
                        }
                    };
                }

                var configuredDto = new RegionDto
                {
                    Id = dto.Id,
                    TypeId = dto.TypeId,
                    Vertices = dto.Vertices,
                    Decorations = decorations,
                    Style = dto.Style,
                    Label = string.IsNullOrWhiteSpace(dto.Label) ? defaultLabel : dto.Label,
                    Properties = dto.Properties
                };

                var configuredRegion = _typeRegistry.CreateRegion(configuredDto);
                regions[i] = configuredRegion;
                changed = true;
            }
        }

        if (changed)
        {
            _controller.RegionsChanged -= OnRegionsChanged;
            _controller.SetRegions(regions);
            _controller.RegionsChanged += OnRegionsChanged;
        }
    }

    private void UpdateSelectionUi()
    {
        var selectedId = _controller.SelectedRegionId;
        if (selectedId == null)
        {
            _noSelectionText.IsVisible = true;
            _propertiesStack.IsVisible = true;
            _isPopulatingUi = true;
            PopulateStyleInputs(_controller.DefaultDrawStyle);
            _labelTextbox.Text = string.Empty;
            _cycleArrowsButton.IsVisible = false;
            _isPopulatingUi = false;
            return;
        }

        var region = _controller.Regions.FirstOrDefault(r => r.Id == selectedId);
        if (region == null)
        {
            _noSelectionText.IsVisible = true;
            _propertiesStack.IsVisible = false;
            return;
        }

        _noSelectionText.IsVisible = false;
        _propertiesStack.IsVisible = true;

        _isPopulatingUi = true;

        // 1. Label
        _labelTextbox.Text = region.Label ?? "";

        PopulateStyleInputs(region.Style);

        // 6. Arrow visibility (Line only)
        _cycleArrowsButton.IsVisible = region.TypeId == LineRegion.LineTypeId;

        _isPopulatingUi = false;
    }

    private void PopulateStyleInputs(RegionStyle style)
    {
        var placement = style.LabelStyle?.Placement ?? LabelPlacement.TopLeft;
        _placementCombobox.SelectedIndex = (int)placement;

        var currentHex = style.StrokeColorHex.ToUpperInvariant();
        var colorIdx = Array.FindIndex(ColorsList, c => c.Hex.Equals(currentHex, StringComparison.OrdinalIgnoreCase));
        _colorCombobox.SelectedIndex = colorIdx >= 0 ? colorIdx : 0;

        _thicknessCombobox.SelectedIndex = style.StrokeThickness switch
        {
            1.0d => 0,
            2.0d => 1,
            3.0d => 2,
            5.0d => 3,
            8.0d => 4,
            _ => 1
        };

        _strokeStyleCombobox.SelectedIndex = style.StrokeDashPattern is not null ? 1 : 0;
    }

    private static RegionDto CloneWithLabel(RegionDto dto, string? label)
    {
        return new RegionDto
        {
            Id = dto.Id,
            TypeId = dto.TypeId,
            Vertices = dto.Vertices,
            Decorations = dto.Decorations,
            Style = dto.Style,
            Label = label,
            Properties = dto.Properties
        };
    }

    private static RegionDto CloneWithStyle(RegionDto dto, RegionStyle style)
    {
        return new RegionDto
        {
            Id = dto.Id,
            TypeId = dto.TypeId,
            Vertices = dto.Vertices,
            Decorations = dto.Decorations,
            Style = style,
            Label = dto.Label,
            Properties = dto.Properties
        };
    }

    private static RegionDto CloneWithDecorations(RegionDto dto, DecorationDto[] decorations)
    {
        return new RegionDto
        {
            Id = dto.Id,
            TypeId = dto.TypeId,
            Vertices = dto.Vertices,
            Decorations = decorations,
            Style = dto.Style,
            Label = dto.Label,
            Properties = dto.Properties
        };
    }

    private void UpdateSelectedRegion(Func<RegionDto, RegionDto> modifyFunc)
    {
        var selectedId = _controller.SelectedRegionId;
        if (selectedId == null) return;

        var regions = _controller.Regions.ToList();
        var index = regions.FindIndex(r => r.Id == selectedId);
        if (index < 0) return;

        var oldRegion = regions[index];
        var dto = _typeRegistry.ToDto(oldRegion);
        
        var newDto = modifyFunc(dto);

        var newRegion = _typeRegistry.CreateRegion(newDto);
        regions[index] = newRegion;

        _controller.RegionsChanged -= OnRegionsChanged;
        _controller.SetRegions(regions);
        _controller.RegionsChanged += OnRegionsChanged;

        _editor.InvalidateVisual();
        SaveToJSON();
    }

    private void UpdateSelectedRegionLabel()
    {
        if (_isPopulatingUi) return;
        UpdateSelectedRegion(dto => CloneWithLabel(dto, _labelTextbox.Text));
    }

    private void UpdateSelectedRegionLabelPlacement()
    {
        if (_isPopulatingUi) return;
        var selectedPlacement = (LabelPlacement)_placementCombobox.SelectedIndex;
        UpdateDefaultDrawStyle(current =>
        {
            var defaultLabelStyle = current.LabelStyle ?? new LabelStyle { TextColorHex = "#FFFFFF", BackgroundColorHex = "#1E293B" };
            return current.With(labelStyle: defaultLabelStyle.With(placement: selectedPlacement));
        });

        UpdateSelectedRegion(dto =>
        {
            var ls = dto.Style.LabelStyle ?? new LabelStyle { TextColorHex = "#FFFFFF", BackgroundColorHex = "#1E293B" };
            var newStyle = dto.Style.With(labelStyle: ls.With(placement: selectedPlacement));
            return CloneWithStyle(dto, newStyle);
        });
    }

    private void UpdateSelectedRegionStyle()
    {
        if (_isPopulatingUi) return;

        var chosenColor = ColorsList[Math.Max(0, _colorCombobox.SelectedIndex)];
        var thickness = _thicknessCombobox.SelectedIndex switch
        {
            0 => 1.0,
            1 => 2.0,
            2 => 3.0,
            3 => 5.0,
            4 => 8.0,
            _ => 2.0
        };
        var isDashed = _strokeStyleCombobox.SelectedIndex == 1;

        UpdateDefaultDrawStyle(dtoStyle =>
            dtoStyle.With(
                strokeColorHex: chosenColor.Hex,
                strokeThickness: thickness,
                strokeDashPattern: isDashed ? new double[] { 6.0, 4.0 } : null,
                clearStrokeDashPattern: !isDashed,
                fillColorHex: chosenColor.Hex));

        UpdateSelectedRegion(dto =>
        {
            var newStyle = new RegionStyle
            {
                StrokeColorHex = chosenColor.Hex,
                StrokeThickness = thickness,
                StrokeDashPattern = isDashed ? new double[] { 6.0, 4.0 } : null,
                FillColorHex = chosenColor.Hex,
                FillOpacity = dto.Style.FillOpacity,
                DefaultHandleStyle = dto.Style.DefaultHandleStyle,
                LabelStyle = dto.Style.LabelStyle
            };
            return CloneWithStyle(dto, newStyle);
        });
    }

    private void UpdateDefaultDrawStyle(Func<RegionStyle, RegionStyle> update)
    {
        _controller.DefaultDrawStyle = update(_controller.DefaultDrawStyle);
    }

    private void CycleSelectedLineArrows()
    {
        if (_isPopulatingUi) return;

        UpdateSelectedRegion(dto =>
        {
            var decs = dto.Decorations.ToList();
            var index = decs.FindIndex(d => d.TypeId == DirectionIndicatorDecoration.DirectionIndicatorTypeId);
            if (index >= 0)
            {
                var dec = decs[index];
                var sign = dec.Properties.TryGetValue("directionSign", out var s) && int.TryParse(s, out var parsed) ? parsed : 1;
                var nextSign = sign switch
                {
                    1 => -1,
                    -1 => 2,
                    2 => 0,
                    _ => 1
                };

                decs[index] = new DecorationDto
                {
                    TypeId = dec.TypeId,
                    Anchor = dec.Anchor,
                    IsInteractive = dec.IsInteractive,
                    Properties = new Dictionary<string, string?>(dec.Properties)
                    {
                        ["directionSign"] = nextSign.ToString()
                    }
                };
                return CloneWithDecorations(dto, decs.ToArray());
            }
            return dto;
        });
    }

    private void SaveToJSON()
    {
        try
        {
#pragma warning disable IL2026
            var dtos = _controller.Regions.Select(r => _typeRegistry.ToDto(r)).ToList();
            var options = new JsonSerializerOptions { WriteIndented = true };
            _jsonTextBox.Text = JsonSerializer.Serialize(dtos, options);
#pragma warning restore IL2026
        }
        catch (Exception ex)
        {
            _jsonTextBox.Text = $"Error serializing: {ex.Message}";
        }
    }

    private void LoadFromJSON()
    {
        try
        {
            var json = _jsonTextBox.Text;
            if (string.IsNullOrWhiteSpace(json)) return;

#pragma warning disable IL2026
            var dtos = JsonSerializer.Deserialize<List<RegionDto>>(json);
#pragma warning restore IL2026
            if (dtos == null) return;

            var regions = dtos.Select(dto => _typeRegistry.CreateRegion(dto)).ToList();
            
            _knownRegionIds.Clear();
            foreach (var r in regions)
            {
                _knownRegionIds.Add(r.Id);
            }

            _controller.RegionsChanged -= OnRegionsChanged;
            _controller.SetRegions(regions);
            _controller.RegionsChanged += OnRegionsChanged;
            
            _controller.CancelActiveOperation();
            UpdateSelectionUi();
        }
        catch (Exception ex)
        {
            _jsonTextBox.Text = $"Error deserializing: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _controller.RegionsChanged -= OnRegionsChanged;
        _controller.RenderStateChanged -= OnRenderStateChanged;
        _editor.Dispose();
    }
}
