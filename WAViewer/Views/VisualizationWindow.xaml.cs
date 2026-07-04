using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Windowing;
using System;
using WAViewer.Helpers;
using WAViewer.Models;
using Windows.Foundation;
using WinRT.Interop;

namespace WAViewer.Views;

public sealed partial class VisualizationWindow : Window
{
    private readonly AppSettings _settings;
    private readonly AudioProcessor _processor = new();
    private readonly DispatcherTimer _timer = new();

    private const int BarCount = 80;
    private readonly Polyline _waveformLine = new();
    private readonly Rectangle[] _spectrumBars = new Rectangle[BarCount];

    public VisualizationWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        if (!_processor.Load(settings.AudioFilePath))
        {
            Close();
            return;
        }

        for (int i = 0; i < BarCount; i++)
        {
            _spectrumBars[i] = new Rectangle
            {
                Fill = new SolidColorBrush(Microsoft.UI.Colors.White),
                Width = 4
            };
        }

        SetupFullscreen();
        Closed += (s, e) => Cleanup();
        RootGrid.Focus(FocusState.Programmatic);

        _processor.SetVolume(settings.Volume);
        _processor.Play();

        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void SetupFullscreen()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    private void Cleanup()
    {
        _timer.Stop();
        _processor.Dispose();
    }

    private void OnTick(object? sender, object e)
    {
        var width = VizCanvas.ActualWidth;
        var height = VizCanvas.ActualHeight;
        if (width <= 0 || height <= 0) return;

        var playhead = _processor.Position;
        var windowSamples = _processor.SampleRate / 30;

        // 播放完毕退出
        if (playhead >= _processor.Samples.Length)
        {
            Cleanup();
            Close();
            return;
        }

        if (_settings.DisplayMode == "波形")
            DrawWaveform(width, height, playhead, windowSamples);
        else
            DrawSpectrum(width, height, playhead, windowSamples);
    }

    private void DrawWaveform(double width, double height, int playhead, int windowSamples)
    {
        VizCanvas.Children.Clear();

        var targetPoints = (int)width;
        var samples = _processor.GetWaveformSamples(playhead, windowSamples, targetPoints);
        if (samples.Length < 2) return;

        var points = new PointCollection();
        var midY = height / 2;
        var amp = height * 0.45;

        for (int i = 0; i < samples.Length; i++)
        {
            var x = (i / (double)(samples.Length - 1)) * width;
            var y = midY - samples[i] * amp; // 正负号保留 → 自然上下延伸
            points.Add(new Point(x, y));
        }

        _waveformLine.Points = points;
        _waveformLine.Stroke = new SolidColorBrush(Microsoft.UI.Colors.White);
        _waveformLine.StrokeThickness = 1.5;
        VizCanvas.Children.Add(_waveformLine);
    }

    private void DrawSpectrum(double width, double height, int playhead, int windowSamples)
    {
        VizCanvas.Children.Clear();

        var spectrum = _processor.GetSpectrumData(playhead, windowSamples, BarCount);
        var barSpacing = width / BarCount;
        var barActualWidth = barSpacing * 0.7;
        var midY = height / 2;

        for (int i = 0; i < BarCount; i++)
        {
            var halfBar = spectrum[i] * height * 0.45;
            if (halfBar < 1) halfBar = 1;

            _spectrumBars[i].Width = barActualWidth;
            _spectrumBars[i].Height = halfBar * 2;
            _spectrumBars[i].SetValue(Canvas.LeftProperty, i * barSpacing + (barSpacing - barActualWidth) / 2);
            _spectrumBars[i].SetValue(Canvas.TopProperty, midY - halfBar); // 居中上下延伸
            VizCanvas.Children.Add(_spectrumBars[i]);
        }
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Cleanup();
            Close();
        }
    }
}
