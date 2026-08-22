using System;
using Ambit.Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Ambit.Sample;

public static class SharedAssets
{
    private static string GetResourceUri(string assetPath)
    {
        var asmName = typeof(SharedAssets).Assembly.GetName().Name;
        return $"avares://{asmName}/{assetPath.TrimStart('/')}";
    }

    // ── Avalonia Bitmap (used only where an Avalonia Image control is needed) ──
    private static readonly Lazy<Bitmap> _lazySchoenbrunnBitmap = new(() =>
    {
        try
        {
            var uri = new Uri(GetResourceUri("Assets/schoenbrunn.jpg"));
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load schoenbrunn Bitmap: {ex.Message}");
            return new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);
        }
    });

    /// <summary>
    /// Schönbrunn palace photo as an Avalonia <see cref="Bitmap"/>.
    /// Used by the passive-playback tiles (which need a static <see cref="Image"/> control).
    /// </summary>
    public static Bitmap BackgroundImage => _lazySchoenbrunnBitmap.Value;

    private static readonly Lazy<Bitmap> _lazyAmbitIcon = new(() => LoadBitmap("Assets/ambit_icon.png"));
    private static readonly Lazy<Bitmap> _lazyAmbitLogo = new(() => LoadBitmap("Assets/ambit_logo.png"));
    private static readonly Lazy<Bitmap> _lazyAmbitText = new(() => LoadBitmap("Assets/ambit.png"));

    public static Bitmap AmbitIcon => _lazyAmbitIcon.Value;
    public static Bitmap AmbitLogo => _lazyAmbitLogo.Value;
    public static Bitmap AmbitText => _lazyAmbitText.Value;

    private static Bitmap LoadBitmap(string assetPath)
    {
        try
        {
            var uri = new Uri(GetResourceUri(assetPath));
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load {assetPath}: {ex.Message}");
            return new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);
        }
    }

    // ── SkiaSharp bitmaps (used by RegionEditorControl.BackgroundImage) ────────
    private static readonly Lazy<SkiaSharp.SKBitmap> _lazySchoenbrunn =
        new(() => LoadSKBitmap(GetResourceUri("Assets/schoenbrunn.jpg")));

    private static readonly Lazy<SkiaSharp.SKBitmap> _lazyCameraFeed =
        new(() => LoadSKBitmap(GetResourceUri("Assets/schoenbrunn.jpg")));

    public static SkiaSharp.SKBitmap SchoenbrunnSKBitmap => _lazySchoenbrunn.Value;
    public static SkiaSharp.SKBitmap CameraFeedSKBitmap  => _lazyCameraFeed.Value;

    private static SkiaSharp.SKBitmap LoadSKBitmap(string avaresUri)
    {
        try
        {
            var uri = new Uri(avaresUri);
            using var stream = AssetLoader.Open(uri);
            return SkiaSharp.SKBitmap.Decode(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load SKBitmap {avaresUri}: {ex.Message}");
            return new SkiaSharp.SKBitmap(1, 1);
        }
    }

    // ── Preview container factory ──────────────────────────────────────────────

    /// <summary>
    /// Wraps <paramref name="editorControl"/> in a 960×540 viewport that uses
    /// schoenbrunn.jpg as the background (rendered by the editor's own Skia pass
    /// so pan/zoom work everywhere) and scales uniformly to fill available space.
    /// </summary>
    public static RegionEditorControl CreateEditorWithBackground(RegionEditController controller)
    {
        return new RegionEditorControl(controller)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
            BackgroundImage     = SchoenbrunnSKBitmap,
            IsPanZoomEnabled    = true,
        };
    }

    /// <summary>
    /// Wraps any <paramref name="editorControl"/> in a styled 960×540 Viewbox
    /// border. Background is rendered by the editor itself (BackgroundImage must
    /// be set on the editor before calling this).
    /// For legacy callers that pass a pre-built control without a background,
    /// a static schoenbrunn <see cref="Image"/> is placed behind it.
    /// </summary>
    public static Control CreatePreviewContainer(Control editorControl)
    {
        // If the caller already configured a BackgroundImage on a RegionEditorControl
        // we don't need a separate Image layer — just wrap directly.
        var hasBackground = editorControl is RegionEditorControl rec && rec.BackgroundImage != null;

        var grid = new Grid { Width = 960, Height = 540 };

        if (!hasBackground)
        {
            // Fallback: static image layer for controls that don't own a background.
            grid.Children.Add(new Image
            {
                Source  = BackgroundImage,
                Stretch = Stretch.Fill,
            });
        }

        grid.Children.Add(editorControl);

        var viewbox = new Viewbox { Stretch = Stretch.Uniform, Child = grid };

        return new Border
        {
            Background      = new SolidColorBrush(Color.Parse("#FFFFFF")),
            BorderBrush     = new SolidColorBrush(Color.Parse("#E2E8F0")),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(8),
            Child           = viewbox,
        };
    }
}
