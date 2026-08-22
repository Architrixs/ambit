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
        FontFamily = new FontFamily("Inter, Segoe UI, sans-serif");
#if BROWSER
        Background = Brushes.Transparent;
#else
        Background = new SolidColorBrush(Color.Parse("#F8FAFC"));
#endif

        // Sidebar Panel
        var sidebar = new Grid();
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Border
        {
            Padding = new Thickness(16, 12),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new Image
            {
                Source = SharedAssets.AmbitLogo,
                Stretch = Stretch.Uniform,
                Height = 36,
                HorizontalAlignment = HorizontalAlignment.Left,
            }
        };
        Grid.SetRow(header, 0);
        sidebar.Children.Add(header);

        // Sidebar Navigation
        _navigationList = new ListBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(8, 12, 8, 12),
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
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
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
            Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = sidebar,
        };

        // Main Content Area
        _contentArea = new ContentControl
        {
            Padding = new Thickness(6),
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
