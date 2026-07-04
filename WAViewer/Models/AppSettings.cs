using System;
using System.IO;
using System.Text.Json;

namespace WAViewer.Models;

public class AppSettings
{
    public string AudioFilePath { get; set; } = string.Empty;
    public double Volume { get; set; } = 50;
    public string DisplayMode { get; set; } = "波形"; // 波形 or 频谱
    public bool IsFirstLaunch { get; set; } = true;

    // 设置页面的配置
    public double DefaultVolume { get; set; } = 50;
    public string DefaultDisplayMode { get; set; } = "波形";

    private static readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "Settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }

        // 文件不存在则创建默认配置并保存
        var settings = new AppSettings();
        settings.Save();
        return settings;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
