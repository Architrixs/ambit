using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;

namespace Ambit.Sample;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Ambit — Interactive Region & Annotation Gallery";
        Width = 1280;
        Height = 800;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#F8FAFC"));
        try
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Ambit.Sample/Assets/ambit_icon.png")));
        }
        catch { }
        Content = new MainView();
    }
}
