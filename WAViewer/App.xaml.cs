using Microsoft.UI.Xaml;
using WAViewer.Models;

namespace WAViewer;

public partial class App : Application
{
    private Window? _window;

    public static Window CurrentWindow => ((App)Current)._window!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settings = AppSettings.Load();

        _window = new MainWindow(settings);
        _window.Activate();
    }
}
