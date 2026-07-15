using System;
using Ambit.Sample.Pages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ambit.Sample;

public sealed class MainWindow : Window
{
    private readonly ContentControl _contentArea;
    private readonly ListBox _navigationList;

    public MainWindow()
    {
        Title = "Ambit — Interactive Region & Annotation Gallery";
        Width = 1280;
        Height = 800;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#0F172A")); // Slate 900

        // Sidebar Panel
        var sidebar = new Grid();
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header Title
        var header = new Border
        {
            Padding = new Thickness(16, 16),
            BorderBrush = new SolidColorBrush(Color.Parse("#334155")), // Slate 700
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = "AMBIT",
                        FontSize = 20,
                        FontWeight = FontWeight.Black,
                        Foreground = new SolidColorBrush(Color.Parse("#F8FAFC")), // Slate 50
                        LetterSpacing = 2,
                    },
                    new TextBlock
                    {
                        Text = "Annotation Library",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#94A3B8")), // Slate 400
                        LetterSpacing = 1,
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
                Text = "v1.0.0 • .NET 10 • Avalonia 11",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#64748B")), // Slate 500
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
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
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
