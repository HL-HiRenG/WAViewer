using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WAViewer.Models;
using WAViewer.Pages;

namespace WAViewer;

public sealed partial class MainWindow : Window
{
    private readonly AppSettings _settings;

    private StartupPage? _startupPage;
    private SettingsPage? _settingsPage;

    public MainWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        ExtendsContentIntoTitleBar = true;

        // 导航到启动页
        NavView.SelectedItem = NavView.MenuItems[0];
        NavigateTo("Startup");
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        switch (tag)
        {
            case "Startup":
                _startupPage ??= new StartupPage();
                _startupPage.Initialize(_settings);
                ContentFrame.Content = _startupPage;
                break;

            case "Settings":
                _settingsPage ??= new SettingsPage();
                _settingsPage.Initialize(_settings);
                ContentFrame.Content = _settingsPage;
                break;
        }
    }
}
