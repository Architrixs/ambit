using System;
using Ambit.Sample.Pages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample;

/// <summary>
/// The main user control hosting the Ambit gallery layout, navigation, and branding.
/// Supports both desktop window hosting and browser/WASM single-view application lifetimes.
/// </summary>
public sealed class MainView : UserControl
{
    private readonly ContentControl _contentArea;
    private readonly ListBox _navigationList;

    public MainView()
    {
        Background = new SolidColorBrush(Color.Parse("#0F172A")); // Slate 900

        // Sidebar Panel
        var sidebar = new Grid();
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header Branding with Vector Logo & Pill Badge
        var logoPath = new global::Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse("M 4 8 L 4 4 L 8 4 M 16 4 L 20 4 L 20 8 M 20 16 L 20 20 L 16 20 M 8 20 L 4 20 L 4 16 M 12 8 A 4 4 0 1 0 12 16 A 4 4 0 1 0 12 8 Z"),
            Stroke = new SolidColorBrush(Color.Parse("#38BDF8")), // Vibrant sky blue
            StrokeThickness = 2,
            Width = 24,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var header = new Border
        {
            Padding = new Thickness(16, 16),
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")), // Slate 700
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new Border
                            {
                                Background = new SolidColorBrush(Color.Parse("#260EA5E9")),
                                CornerRadius = new CornerRadius(6),
                                Padding = new Thickness(6),
                                Child = logoPath
                            },
                            new StackPanel
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "AMBIT",
                                        FontSize = 18,
                                        FontWeight = FontWeight.Black,
                                        Foreground = new SolidColorBrush(Color.Parse("#F8FAFC")),
                                        LetterSpacing = 2,
                                    },
                                    new TextBlock
                                    {
                                        Text = "ANNOTATION ENGINE",
                                        FontSize = 9,
                                        FontWeight = FontWeight.Bold,
                                        Foreground = new SolidColorBrush(Color.Parse("#38BDF8")),
                                        LetterSpacing = 1.2,
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        Grid.SetRow(header, 0);
        sidebar.Children.Add(header);

        // Sidebar Navigation
        _navigationList = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(12, 16, 12, 16),
            ItemsSource = new[]
            {
                "Editor & Drawings",
                "Cell Grid Editor",
                "Video & Analytics",
                "Performance Benchmark"
            }
        };
        _navigationList.SelectionChanged += NavigationListOnSelectionChanged;
        Grid.SetRow(_navigationList, 1);
        sidebar.Children.Add(_navigationList);

        // Footer Metadata
        var footer = new Border
        {
            Padding = new Thickness(16, 16),
            BorderThickness = new Thickness(0, 1, 0, 0),
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")), // Slate 700
            Child = new TextBlock
            {
                Text = "v1.0.0 • Semi.Avalonia • .NET 10",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#64748B")),
                HorizontalAlignment = HorizontalAlignment.Center,
            }
        };
        Grid.SetRow(footer, 2);
        sidebar.Children.Add(footer);

        var sidebarBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E293B")), // Slate 800
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")), // Slate 700
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = sidebar,
        };

        // Main Content Area
        _contentArea = new ContentControl
        {
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        // Main Layout Grid
        var mainGrid = new Grid();
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(sidebarBorder, 0);
        Grid.SetColumn(_contentArea, 1);

        mainGrid.Children.Add(sidebarBorder);
        mainGrid.Children.Add(_contentArea);

        Content = mainGrid;

        // Select first page by default
        _navigationList.SelectedIndex = 0;
    }

    private void NavigationListOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_contentArea.Content is IDisposable disposable)
        {
            disposable.Dispose();
        }

        var selected = _navigationList.SelectedItem as string;
        _contentArea.Content = selected switch
        {
            "Editor & Drawings" => new RegionKindsPage(),
            "Cell Grid Editor" => new CellGridPage(),
            "Video & Analytics" => new VideoPlaybackPage(),
            "Performance Benchmark" => new PerformanceBenchmarkPage(),
            _ => new TextBlock { Text = "Select a demo from the sidebar." }
        };
    }
}
