using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using WAViewer.Models;
using WAViewer.Views;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WAViewer.Pages;

public sealed partial class StartupPage : Page
{
    private AppSettings _settings = null!;

    public StartupPage()
    {
        InitializeComponent();
    }

    public void Initialize(AppSettings settings)
    {
        _settings = settings;
        FilePathBox.Text = settings.AudioFilePath;
        VolumeSlider.Value = settings.Volume;
        VolumeLabel.Text = $"{settings.Volume:F0}%";

        if (settings.DisplayMode == "频谱")
            SpectrumRadio.IsChecked = true;
        else
            WaveformRadio.IsChecked = true;
    }

    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".wav");
        picker.FileTypeFilter.Add(".mp3");
        picker.FileTypeFilter.Add(".flac");
        picker.FileTypeFilter.Add(".aiff");
        picker.FileTypeFilter.Add(".m4a");
        picker.FileTypeFilter.Add(".wma");

        var hwnd = WindowNative.GetWindowHandle(App.CurrentWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            FilePathBox.Text = file.Path;
            _settings.AudioFilePath = file.Path;
            _settings.Save();
        }
    }

    private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_settings == null) return;
        VolumeLabel.Text = $"{e.NewValue:F0}%";
        _settings.Volume = e.NewValue;
        _settings.Save();
    }

    private void FilePathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        StartButton.IsEnabled = !string.IsNullOrWhiteSpace(FilePathBox.Text);
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.DisplayMode = WaveformRadio.IsChecked == true ? "波形" : "频谱";
        _settings.Save();

        var vizWindow = new VisualizationWindow(_settings);
        vizWindow.Activate();
    }
}
