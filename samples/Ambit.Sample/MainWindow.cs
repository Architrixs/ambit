using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ambit.Sample;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Ambit — Interactive Region & Annotation Gallery";
        Width = 1280;
        Height = 800;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#0F172A")); // Slate 900
        Content = new MainView();
    }
}
