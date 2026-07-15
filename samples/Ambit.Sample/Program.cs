using System;
using Avalonia;
using FFmpegVideoPlayer.Core;

namespace Ambit.Sample;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        FFmpegInitializer.StatusChanged += (msg) => 
        {
            Console.WriteLine($"[FFmpeg Status] {msg}");
            System.Diagnostics.Debug.WriteLine($"[FFmpeg Status] {msg}");
        };

        try
        {
            Console.WriteLine("[FFmpeg] Initializing...");
            FFmpegInitializer.Initialize();
            Console.WriteLine("[FFmpeg] Initialization completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FFmpeg Error] Initialization failed: {ex}");
            System.Diagnostics.Debug.WriteLine($"[FFmpeg Error] Initialization failed: {ex}");
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
