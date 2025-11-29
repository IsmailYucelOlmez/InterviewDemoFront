using System.IO;
using CommunicationApp.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CommunicationApp.Helpers;

public static class ConfigHelper
{
    private static AppSettings? _settings;
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public static AppSettings GetSettings()
    {
        if (_settings != null)
            return _settings;

        if (!File.Exists(ConfigPath))
        {
            _settings = new AppSettings();
            SaveSettings(_settings);
            return _settings;
        }

        var json = File.ReadAllText(ConfigPath);
        _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        return _settings;
    }

    public static void SaveSettings(AppSettings settings)
    {
        _settings = settings;
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(ConfigPath, json);
    }

    public static void ReloadSettings()
    {
        _settings = null;
        GetSettings();
    }
}

