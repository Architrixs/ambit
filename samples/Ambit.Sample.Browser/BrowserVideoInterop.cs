using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Ambit.Sample;

/// <summary>
/// C# → JavaScript bridge for the native HTML &lt;video&gt; element used in the
/// Video Playback page when running as a WebAssembly app in the browser.
///
/// The module is loaded once at app startup (see BrowserAppSetup.cs).
/// All methods are no-ops when called before the module is initialised.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class BrowserVideoInterop
{
    /// <summary>Module name used by JSHost.ImportAsync and [JSImport].</summary>
    public const string ModuleName = "VideoInterop";

    /// <summary>Path to the JS module relative to _framework/dotnet.js.</summary>
    public const string ModulePath = "../js/video-interop.js";

    private static bool _initialised;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the JS module the first time it is needed.
    /// Safe to call multiple times; subsequent calls are instant no-ops.
    /// </summary>
    public static async Task EnsureInitialisedAsync()
    {
        if (_initialised) return;
        await JSHost.ImportAsync(ModuleName, ModulePath);
        _initialised = true;
    }

    // ── JS imports ────────────────────────────────────────────────────────

    /// <summary>Creates and inserts the native &lt;video&gt; element.</summary>
    /// <param name="src">URL of the video asset, relative to the WASM app root.</param>
    /// <param name="contentWidth">Native pixel width of the video.</param>
    /// <param name="contentHeight">Native pixel height of the video.</param>
    [JSImport("createVideo", ModuleName)]
    public static partial void CreateVideo(string src, double contentWidth, double contentHeight);

    /// <summary>
    /// Applies the pan/zoom CSS transform so the video tracks the Avalonia
    /// overlay canvas exactly.
    /// </summary>
    [JSImport("setTransform", ModuleName)]
    public static partial void SetTransform(
        double zoom,
        double panX,
        double panY,
        double containerWidth,
        double containerHeight,
        double containerX,
        double containerY);

    /// <summary>Pauses video playback.</summary>
    [JSImport("pauseVideo", ModuleName)]
    public static partial void PauseVideo();

    /// <summary>Resumes video playback.</summary>
    [JSImport("playVideo", ModuleName)]
    public static partial void PlayVideo();

    /// <summary>Removes the video element and releases media resources.</summary>
    [JSImport("destroyVideo", ModuleName)]
    public static partial void DestroyVideo();

    /// <summary>Returns true if the video element is present and has loaded.</summary>
    [JSImport("isVideoReady", ModuleName)]
    public static partial bool IsVideoReady();
}
