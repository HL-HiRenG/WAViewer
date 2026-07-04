using NAudio.Wave;
using System;
using System.Threading;

namespace WAViewer.Helpers;

// 包装 WaveStream，精确统计音频设备已读取的采样数
internal class CountingWaveProvider : IWaveProvider
{
    private readonly WaveStream _source;
    public long SamplesRead;
    private readonly int _bytesPerSample;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public CountingWaveProvider(WaveStream source)
    {
        _source = source;
        _bytesPerSample = source.WaveFormat.BitsPerSample / 8 * source.WaveFormat.Channels;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        Interlocked.Add(ref SamplesRead, read / _bytesPerSample);
        return read;
    }
}

public class AudioProcessor : IDisposable
{
    public float[] Samples { get; private set; } = Array.Empty<float>();
    public int SampleRate { get; private set; }
    public double Duration { get; private set; }

    // 从设备实际消费量反推当前位置，绝对同步
    public int Position
    {
        get
        {
            if (_provider == null) return 0;
            var pos = (int)Interlocked.Read(ref _provider.SamplesRead);
            return Math.Min(pos, Samples.Length);
        }
    }

    private string _filePath = string.Empty;
    private WaveOutEvent? _output;
    private AudioFileReader? _playbackReader;
    private CountingWaveProvider? _provider;
    private double _volume = 100;

    public bool Load(string filePath)
    {
        try
        {
            _filePath = filePath;

            using var reader = new AudioFileReader(filePath);
            SampleRate = reader.WaveFormat.SampleRate;
            var totalSamples = (int)(reader.Length / sizeof(float));
            Samples = new float[totalSamples];
            reader.Read(Samples, 0, totalSamples);
            Duration = (double)totalSamples / SampleRate;
            return true;
        }
        catch
        {
            Samples = Array.Empty<float>();
            return false;
        }
    }

    public void Play()
    {
        if (string.IsNullOrEmpty(_filePath)) return;

        _playbackReader = new AudioFileReader(_filePath)
        {
            Volume = (float)(_volume / 100.0)
        };

        _provider = new CountingWaveProvider(_playbackReader);
        _output = new WaveOutEvent();
        _output.Init(_provider);
        _output.Play();
    }

    public void SetVolume(double volume)
    {
        _volume = volume;
        if (_playbackReader != null)
            _playbackReader.Volume = (float)(volume / 100.0);
    }

    public void Stop()
    {
        _output?.Stop();
        _output?.Dispose();
        _output = null;

        _playbackReader?.Dispose();
        _playbackReader = null;
        _provider = null;
    }

    public void Dispose()
    {
        Stop();
    }

    // 波形采样 → 保留正负号
    public float[] GetWaveformSamples(int startSample, int windowSamples, int targetCount)
    {
        if (Samples.Length == 0 || targetCount <= 0) return Array.Empty<float>();

        var result = new float[targetCount];
        var step = Math.Max(1, windowSamples / targetCount);

        for (int i = 0; i < targetCount; i++)
        {
            var srcIdx = startSample + i * step;
            if (srcIdx >= Samples.Length) break;

            float peak = 0;
            for (int j = 0; j < step && srcIdx + j < Samples.Length; j++)
            {
                var val = Samples[srcIdx + j];
                if (Math.Abs(val) > Math.Abs(peak)) peak = val;
            }
            result[i] = peak;
        }

        return result;
    }

    // FFT 频谱
    public float[] GetSpectrumData(int startSample, int windowSamples, int targetCount)
    {
        if (Samples.Length == 0 || targetCount <= 0) return Array.Empty<float>();

        var fftSize = 1;
        while (fftSize < windowSamples) fftSize *= 2;
        if (fftSize > 4096) fftSize = 4096;

        var real = new float[fftSize];
        var imag = new float[fftSize];

        for (int i = 0; i < Math.Min(windowSamples, fftSize); i++)
        {
            var idx = startSample + i;
            if (idx < Samples.Length) real[i] = Samples[idx];
        }

        Fft(real, imag);

        var result = new float[targetCount];
        var binsPerBand = (fftSize / 2) / targetCount;

        for (int i = 0; i < targetCount; i++)
        {
            float mag = 0;
            for (int j = 0; j < binsPerBand; j++)
            {
                var bin = i * binsPerBand + j;
                if (bin < fftSize / 2)
                    mag += MathF.Sqrt(real[bin] * real[bin] + imag[bin] * imag[bin]);
            }
            result[i] = mag / binsPerBand;
        }

        var maxMag = 0f;
        foreach (var m in result) if (m > maxMag) maxMag = m;
        if (maxMag > 0)
            for (int i = 0; i < result.Length; i++)
                result[i] /= maxMag;

        return result;
    }

    private static void Fft(float[] real, float[] imag)
    {
        int n = real.Length;
        int bits = (int)Math.Log2(n);

        for (int i = 0; i < n; i++)
        {
            int j = 0;
            for (int b = 0; b < bits; b++)
                if ((i & (1 << b)) != 0)
                    j |= 1 << (bits - 1 - b);
            if (j > i)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
        }

        for (int len = 2; len <= n; len *= 2)
        {
            float angle = -2.0f * MathF.PI / len;
            var wReal = MathF.Cos(angle);
            var wImag = MathF.Sin(angle);

            for (int i = 0; i < n; i += len)
            {
                float curReal = 1, curImag = 0;
                for (int j = 0; j < len / 2; j++)
                {
                    int even = i + j;
                    int odd = i + j + len / 2;

                    float tReal = curReal * real[odd] - curImag * imag[odd];
                    float tImag = curReal * imag[odd] + curImag * real[odd];

                    real[odd] = real[even] - tReal;
                    imag[odd] = imag[even] - tImag;
                    real[even] += tReal;
                    imag[even] += tImag;

                    float nReal = curReal * wReal - curImag * wImag;
                    curImag = curReal * wImag + curImag * wReal;
                    curReal = nReal;
                }
            }
        }
    }
}
