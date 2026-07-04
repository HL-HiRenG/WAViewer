using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WAViewer.Models;

namespace WAViewer.Pages;

public sealed partial class SettingsPage : Page
{
    private AppSettings _settings = null!;

    public SettingsPage()
    {
        InitializeComponent();
    }

    public void Initialize(AppSettings settings)
    {
        _settings = settings;
        DefaultVolumeSlider.Value = settings.DefaultVolume;
        DefaultVolumeLabel.Text = $"{settings.DefaultVolume:F0}%";

        if (settings.DefaultDisplayMode == "频谱")
            DefaultSpectrum.IsChecked = true;
        else
            DefaultWaveform.IsChecked = true;
    }

    private void DefaultVolume_Changed(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_settings == null) return;
        DefaultVolumeLabel.Text = $"{e.NewValue:F0}%";
        _settings.DefaultVolume = e.NewValue;
        _settings.Save();
    }

    private void DefaultDisplay_Changed(object sender, RoutedEventArgs e)
    {
        if (_settings == null) return;
        _settings.DefaultDisplayMode = DefaultWaveform.IsChecked == true ? "波形" : "频谱";
        _settings.Save();
    }

    private void AboutToggle_Click(object sender, RoutedEventArgs e)
    {
        if (AboutContent.Visibility == Visibility.Collapsed)
        {
            AboutContent.Visibility = Visibility.Visible;
            AboutChevron.Glyph = ""; // 上箭头
        }
        else
        {
            AboutContent.Visibility = Visibility.Collapsed;
            AboutChevron.Glyph = ""; // 下箭头
        }
    }
}
