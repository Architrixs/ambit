using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Ambit.Sample.Pages;

public sealed class PassiveModePage : UserControl, IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly List<RegionOverlayControl> _overlays = new();
    private readonly List<List<IRegion>> _tileRegions = new();
    private readonly TextBlock _allocationText;
    private double _animationTime;
    private long _lastAllocatedBytes;
    private int _frameCount;
    private long _totalDeltaBytes;

    public PassiveModePage()
    {
        _lastAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();

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
            Text = "Passive Playback",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var description = new TextBlock
        {
            Text = "Simulates playback mode over 6 video tiles with live annotations.\n\n" +
                   "A background timer updates region positions at 30 FPS. " +
                   "This measures the memory allocated on the UI thread to verify the zero-allocation steady-state rendering path.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.LightGray,
            FontSize = 13,
        };
        controlsPanel.Children.Add(description);

        _allocationText = new TextBlock
        {
            Text = "Monitoring allocations...",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.LightGreen,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 16, 0, 0),
        };
        controlsPanel.Children.Add(_allocationText);

        // Right grid of video tiles
        var tilesGrid = new Grid
        {
            Margin = new Thickness(16),
        };
        tilesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        tilesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        tilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var defaultStyle = new RegionStyle { StrokeColorHex = "#10B981", FillColorHex = "#10B981", FillOpacity = 0.15 };
        var lineStyle = new RegionStyle { StrokeColorHex = "#F59E0B", StrokeThickness = 3.0 };

        for (var i = 0; i < 6; i++)
        {
            var row = i / 3;
            var col = i % 3;

            // Simulated camera background
            var tileContainer = new Grid();
            var bgBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#111827")),
                BorderBrush = new SolidColorBrush(Color.Parse("#374151")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(4),
            };

            // Label for the camera channel
            var channelLabel = new TextBlock
            {
                Text = $"CH-{i + 1:D2}",
                Foreground = new SolidColorBrush(Color.Parse("#4B5563")),
                FontWeight = FontWeight.SemiBold,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var overlay = new RegionOverlayControl
            {
                Margin = new Thickness(4),
            };

            bgBorder.Child = channelLabel;
            tileContainer.Children.Add(bgBorder);
            tileContainer.Children.Add(overlay);

            Grid.SetRow(tileContainer, row);
            Grid.SetColumn(tileContainer, col);
            tilesGrid.Children.Add(tileContainer);

            _overlays.Add(overlay);

            // Generate initial shapes for this tile
            var regions = new List<IRegion>();
            if (i % 2 == 0)
            {
                regions.Add(new RectangleRegion(new NormalizedPoint(0.1, 0.1), new NormalizedPoint(0.4, 0.4), defaultStyle));
                regions.Add(new EllipseRegion(new NormalizedPoint(0.6, 0.6), new NormalizedPoint(0.9, 0.9), defaultStyle));
            }
            else
            {
                regions.Add(new LineRegion(new NormalizedPoint(0.2, 0.8), new NormalizedPoint(0.8, 0.2), lineStyle));
                regions.Add(new PolygonRegion(
                    new[] { new NormalizedPoint(0.4, 0.4), new NormalizedPoint(0.6, 0.4), new NormalizedPoint(0.5, 0.7) },
                    defaultStyle));
            }

            _tileRegions.Add(regions);
            overlay.UpdateRegions(regions);
        }

        // Setup animation timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33), // ~30 FPS
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(controlsPanel, 0);
        Grid.SetColumn(tilesGrid, 1);

        grid.Children.Add(controlsPanel);
        grid.Children.Add(tilesGrid);

        Content = grid;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _animationTime += 0.05;

        // Perform memory tracking before allocations from rendering state
        var currentAlloc = GC.GetAllocatedBytesForCurrentThread();
        var delta = currentAlloc - _lastAllocatedBytes;

        // Ignore startup/first-few-frames allocations due to lazy type loads or thread context setups
        _frameCount++;
        if (_frameCount > 30)
        {
            // Accumulate steady-state allocations only
            // Note: DispatcherTimer ticks themselves allocate minimal bytes inside the Avalonia framework loop,
            // but the steady-state Render path of Ambit remains allocation-free.
            _totalDeltaBytes += Math.Max(0, delta);
            _allocationText.Text = $"Frame: {_frameCount}\n" +
                                   $"Current Tick Alloc: {delta:N0} bytes\n" +
                                   $"Steady-State Avg: {(_totalDeltaBytes / (_frameCount - 30.0)):F0} bytes/frame\n\n" +
                                   $"Verify that after warming up, the render path produces 0 managed allocations.";
        }

        // Animate region points
        var offset = Math.Sin(_animationTime) * 0.05;
        for (var i = 0; i < 6; i++)
        {
            var regions = _tileRegions[i];
            var overlay = _overlays[i];
            var updatedRegions = new List<IRegion>();

            foreach (var region in regions)
            {
                if (region is RectangleRegion r)
                {
                    var moved = new RectangleRegion(
                        new NormalizedPoint(0.1 + offset, 0.1),
                        new NormalizedPoint(0.4 + offset, 0.4),
                        r.Style,
                        r.Id);
                    updatedRegions.Add(moved);
                }
                else if (region is EllipseRegion el)
                {
                    var moved = new EllipseRegion(
                        new NormalizedPoint(0.6, 0.6 + offset),
                        new NormalizedPoint(0.9, 0.9 + offset),
                        el.Style,
                        el.Id);
                    updatedRegions.Add(moved);
                }
                else if (region is LineRegion l)
                {
                    var moved = new LineRegion(
                        new NormalizedPoint(0.2, 0.8 + offset),
                        new NormalizedPoint(0.8, 0.2 + offset),
                        l.Style,
                        l.Id);
                    updatedRegions.Add(moved);
                }
                else if (region is PolygonRegion p)
                {
                    var moved = new PolygonRegion(
                        new[]
                        {
                            new NormalizedPoint(0.4 + offset, 0.4),
                            new NormalizedPoint(0.6 + offset, 0.4),
                            new NormalizedPoint(0.5 + offset, 0.7)
                        },
                        p.Style,
                        p.Id);
                    updatedRegions.Add(moved);
                }
            }

            _tileRegions[i] = updatedRegions;
            overlay.UpdateRegions(updatedRegions);
        }

        _lastAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
