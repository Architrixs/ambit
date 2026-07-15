using System;
using System.IO;
using Ambit.Avalonia.Controls;
using Ambit.Avalonia.Heatmaps;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

#if DESKTOP
using Avalonia.FFmpegVideoPlayer;
#endif

namespace Ambit.Sample.Pages;

public sealed class VideoPlaybackPage : UserControl, IDisposable
{
    private const double VideoWidth  = 1920;
    private const double VideoHeight = 1080;
    private const int HeatmapRows = 8;
    private const int HeatmapCols = 8;

    private readonly RegionEditorControl _editor;
    private readonly RegionEditController _controller;
    private readonly Grid _canvasContainer;
    private readonly CellGrid _cellGrid = new(12, 16);

    private readonly Slider _intensitySlider;
    private readonly Slider _opacitySlider;
    private readonly CheckBox _cellPaintCheckbox;
    private readonly TextBlock _statusText;

#if DESKTOP
    private readonly VideoPlayerControl? _videoPlayer;
#endif

    public VideoPlaybackPage()
    {
        _controller = new RegionEditController();

        _editor = new RegionEditorControl(_controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
            IsPanZoomEnabled    = true,
            ContentSize         = new Size(VideoWidth, VideoHeight),
        };

        // 1. Initial annotation shapes
        var zoneStyle     = new RegionStyle { StrokeColorHex = "#10B981", FillColorHex = "#10B981", FillOpacity = 0.15 };
        var tripwireStyle = new RegionStyle { StrokeColorHex = "#F59E0B", StrokeThickness = 3.0 };

        _controller.SetRegions(new IEditableRegion[]
        {
            new RectangleRegion(new NormalizedPoint(0.10, 0.40), new NormalizedPoint(0.45, 0.90), zoneStyle,     label: "Lane 1 Target Zone"),
            new RectangleRegion(new NormalizedPoint(0.55, 0.40), new NormalizedPoint(0.90, 0.90), zoneStyle,     label: "Lane 2 Target Zone"),
            new LineRegion     (new NormalizedPoint(0.15, 0.65), new NormalizedPoint(0.85, 0.65), tripwireStyle, label: "ANPR Speed Limit Gate"),
        });

        // 2. Left controls panel
        var controlsPanel = new StackPanel { Spacing = 12 };

        controlsPanel.Children.Add(new TextBlock
        {
            Text       = "Video & Analytics",
            FontSize   = 20,
            FontWeight = FontWeight.Bold,
            Margin     = new Thickness(0, 0, 0, 8),
        });

        controlsPanel.Children.Add(new TextBlock
        {
            Text = "Live video feed overlay with annotations, cell painting, and heatmap analytics:\n\n" +
                   "• Scroll to zoom, right-drag background to pan.\n" +
                   "• Toggle cell painting and drag on canvas to paint cells.",
            TextWrapping = TextWrapping.Wrap,
            Foreground   = new SolidColorBrush(Color.Parse("#94A3B8")),
            FontSize     = 13,
        });

        // Draw Modes
        controlsPanel.Children.Add(new TextBlock
        {
            Text       = "Draw Mode:",
            FontSize   = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
            Margin     = new Thickness(0, 8, 0, 0),
        });

        var selectMode = new RadioButton { Content = "Select / Edit Mode", IsChecked = true };
        var rectMode   = new RadioButton { Content = "Draw Rectangle" };
        var lineMode   = new RadioButton { Content = "Draw Tripwire Line" };

        selectMode.IsCheckedChanged += (_, _) => { if (selectMode.IsChecked == true) _controller.ActiveDrawTypeId = null; };
        rectMode  .IsCheckedChanged += (_, _) => { if (rectMode  .IsChecked == true) _controller.ActiveDrawTypeId = RectangleRegion.RectangleTypeId; };
        lineMode  .IsCheckedChanged += (_, _) => { if (lineMode  .IsChecked == true) _controller.ActiveDrawTypeId = LineRegion.LineTypeId; };

        controlsPanel.Children.Add(selectMode);
        controlsPanel.Children.Add(rectMode);
        controlsPanel.Children.Add(lineMode);

        // Cell Grid Paint toggle
        _cellPaintCheckbox = new CheckBox
        {
            Content = "Enable Cell Painting",
            IsChecked = false,
            Margin = new Thickness(0, 8, 0, 0),
        };
        _cellPaintCheckbox.IsCheckedChanged += (s, e) =>
        {
            if (_cellPaintCheckbox.IsChecked == true)
            {
                _controller.CellGrid = _cellGrid;
                _controller.IsCellPaintMode = true;
                _controller.ActiveDrawTypeId = null;
                selectMode.IsChecked = true;
            }
            else
            {
                _controller.IsCellPaintMode = false;
            }
        };
        controlsPanel.Children.Add(_cellPaintCheckbox);

        _statusText = new TextBlock
        {
            Text = "Selected cells: 0",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
        };
        _controller.CellsChanged += (s, e) =>
        {
            _statusText.Text = $"Selected cells: {_controller.SelectedCells.Count}";
        };
        controlsPanel.Children.Add(_statusText);

        // Heatmap Controls
        controlsPanel.Children.Add(new Separator { Background = new SolidColorBrush(Color.Parse("#334155")), Margin = new Thickness(0, 4, 0, 4) });
        controlsPanel.Children.Add(new TextBlock
        {
            Text       = "Heatmap Analytics Overlay:",
            FontSize   = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
        });

        controlsPanel.Children.Add(new TextBlock { Text = "Center Intensity:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _intensitySlider = new Slider { Minimum = 0, Maximum = 255, Value = 180 };
        _intensitySlider.ValueChanged += (s, e) => UpdateHeatmap();
        controlsPanel.Children.Add(_intensitySlider);

        controlsPanel.Children.Add(new TextBlock { Text = "Overall Opacity:", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#64748B")) });
        _opacitySlider = new Slider { Minimum = 0, Maximum = 1, Value = 0.65 };
        _opacitySlider.ValueChanged += (s, e) => UpdateHeatmap();
        controlsPanel.Children.Add(_opacitySlider);

        // Fit & Clear Actions
        var fitButton = new Button
        {
            Content             = "Fit to Canvas",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin              = new Thickness(0, 12, 0, 0),
        };
        fitButton.Click += (_, _) => _editor.FitToCanvas();
        controlsPanel.Children.Add(fitButton);

        var clearButton = new Button
        {
            Content             = "Clear Zones & Cells",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        clearButton.Click += (_, _) =>
        {
            _controller.SetRegions(Array.Empty<IEditableRegion>());
            _controller.SelectedCells.Clear();
            _controller.CancelActiveOperation();
            _statusText.Text = "Selected cells: 0";
        };
        controlsPanel.Children.Add(clearButton);

        var card = new Border
        {
            Background      = new SolidColorBrush(Color.Parse("#1E293B")),
            BorderBrush     = new SolidColorBrush(Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(12),
            Margin          = new Thickness(12),
            Padding         = new Thickness(12),
            VerticalAlignment = VerticalAlignment.Top,
            Child           = controlsPanel,
        };

        // 3. Right canvas: video layer + editor overlay
        _canvasContainer = new Grid
        {
            Background  = new SolidColorBrush(Color.Parse("#0F172A")),
            ClipToBounds = true,
        };

        _editor.BackgroundImage = SharedAssets.CameraFeedSKBitmap;

#if DESKTOP
        var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ANPR.mp4");
        Console.WriteLine($"[VideoPlayback] Loading video from path: {videoPath}");
        Console.WriteLine($"[VideoPlayback] File exists: {File.Exists(videoPath)}");
        if (File.Exists(videoPath))
        {
            _videoPlayer = new VideoPlayerControl
            {
                Volume              = 0,
                ShowControls        = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch,
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            };
            _canvasContainer.Children.Insert(0, _videoPlayer);

            _editor.PanZoomChanged += SyncDesktopVideoTransform;
            _editor.LayoutUpdated  += OnDesktopLayoutUpdated;

            this.Loaded += (_, _) =>
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        Console.WriteLine($"[VideoPlayback] Attaching Source on Loaded UIThread: {videoPath}");
                        if (_videoPlayer != null)
                        {
                            _videoPlayer.Source = videoPath;
                            _videoPlayer.AutoPlay = true;
                            _videoPlayer.Play();
                            _editor.BackgroundImage = null; // Clear static background so live video shows through
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VideoPlayback] Play error: {ex.Message}");
                        _editor.BackgroundImage = SharedAssets.CameraFeedSKBitmap;
                    }
                }, global::Avalonia.Threading.DispatcherPriority.Loaded);
            };
        }
        else
        {
            AddStaticFallback($"'Assets/ANPR.mp4' not found.\n{videoPath}");
        }

#elif BROWSER
        this.Loaded += async (_, _) =>
        {
            try
            {
                await BrowserVideoInterop.EnsureInitialisedAsync();
                BrowserVideoInterop.CreateVideo("./Assets/ANPR.mp4", VideoWidth, VideoHeight);
                SyncBrowserVideoTransform();

                _editor.PanZoomChanged += OnBrowserPanZoomOrLayoutChanged;
                _editor.LayoutUpdated  += OnBrowserPanZoomOrLayoutChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VideoPlaybackPage] JS interop error: {ex.Message}");
                AddStaticFallback("Video interop unavailable.\nShowing static fallback.");
            }
        };
#else
        AddStaticFallback("Video playback is only available\non Desktop and Browser builds.");
#endif

        _canvasContainer.Children.Add(_editor);

        var rightBorder = new Border
        {
            BorderBrush     = new SolidColorBrush(Color.Parse("#334155")),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(12),
            Margin          = new Thickness(12),
            ClipToBounds    = true,
            Child           = _canvasContainer,
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(card,        0);
        Grid.SetColumn(rightBorder, 1);

        grid.Children.Add(card);
        grid.Children.Add(rightBorder);

        Content = grid;

        this.Unloaded += (_, _) => Dispose();

        // Apply initial heatmap
        UpdateHeatmap();
    }

    private void UpdateHeatmap()
    {
        var centerIntensity = (byte)_intensitySlider.Value;
        var opacity = (float)_opacitySlider.Value;

        var intensities = new byte[HeatmapRows * HeatmapCols];
        for (var r = 0; r < HeatmapRows; r++)
        {
            for (var c = 0; c < HeatmapCols; c++)
            {
                var dx = c - (HeatmapCols - 1) / 2d;
                var dy = r - (HeatmapRows - 1) / 2d;
                var dist = Math.Sqrt((dx * dx) + (dy * dy));
                var val = Math.Max(0, 1.0 - (dist / 4.0)) * centerIntensity;
                intensities[(r * HeatmapCols) + c] = (byte)val;
            }
        }

        var heatmap = new HeatmapLayer
        {
            Rows = HeatmapRows,
            Columns = HeatmapCols,
            Intensities = intensities,
            Opacity = opacity,
        };

        _editor.Heatmap = heatmap;
    }

    public void Dispose()
    {
#if DESKTOP
        if (_videoPlayer is not null)
        {
            _editor.PanZoomChanged -= SyncDesktopVideoTransform;
            _editor.LayoutUpdated  -= OnDesktopLayoutUpdated;
            _videoPlayer.Stop();
        }
#elif BROWSER
        _editor.PanZoomChanged -= OnBrowserPanZoomOrLayoutChanged;
        _editor.LayoutUpdated  -= OnBrowserPanZoomOrLayoutChanged;
        BrowserVideoInterop.DestroyVideo();
#endif
        _editor.Dispose();
    }

    private void AddStaticFallback(string message)
    {
        _editor.BackgroundImage = SharedAssets.CameraFeedSKBitmap;

        var fallbackImage = new Image
        {
            Source              = SharedAssets.BackgroundImage,
            Stretch             = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };
        _canvasContainer.Children.Add(fallbackImage);

        _canvasContainer.Children.Add(new Border
        {
            Background      = new SolidColorBrush(Color.Parse("#CC0B0F19")),
            CornerRadius    = new CornerRadius(6),
            Padding         = new Thickness(12, 6),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text            = message,
                Foreground      = new SolidColorBrush(Color.Parse("#10B981")),
                FontWeight      = FontWeight.Bold,
                FontSize        = 14,
                TextAlignment   = TextAlignment.Center,
                TextWrapping    = TextWrapping.Wrap,
            },
        });
    }

#if DESKTOP
    private void OnDesktopLayoutUpdated(object? sender, EventArgs e) => SyncDesktopVideoTransform(null, EventArgs.Empty);

    private void SyncDesktopVideoTransform(object? sender, EventArgs e)
    {
        if (_videoPlayer is null) return;

        _videoPlayer.RenderTransform = new TransformGroup
        {
            Children =
            [
                new ScaleTransform(_editor.Zoom, _editor.Zoom),
                new TranslateTransform(_editor.PanX, _editor.PanY),
            ],
        };
    }
#endif

#if BROWSER
    private void OnBrowserPanZoomOrLayoutChanged(object? sender, EventArgs e) => SyncBrowserVideoTransform();

    private void SyncBrowserVideoTransform()
    {
        var bounds = _canvasContainer.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        BrowserVideoInterop.SetTransform(
            _editor.Zoom,
            _editor.PanX,
            _editor.PanY,
            bounds.Width,
            bounds.Height);
    }
#endif
}
