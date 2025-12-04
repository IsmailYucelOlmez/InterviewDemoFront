using System;
using System.IO;
using CommunicationApp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommunicationApp.Helpers;

public static class ConfigHelper
{
    private static ServerSettings? _settings;
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public static ServerSettings GetSettings()
    {
        if (_settings != null)
            return _settings;

        if (!File.Exists(ConfigPath))
        {
            _settings = new ServerSettings();
            return _settings;
        }

        var json = File.ReadAllText(ConfigPath);
        var jObject = JObject.Parse(json);
        var serverSettingsToken = jObject["ServerSettings"];
        _settings = serverSettingsToken?.ToObject<ServerSettings>() ?? new ServerSettings();
        return _settings;
    }

    public static string GetBaseUrl()
    {
        var settings = GetSettings();
        var hubUrl = settings.HubUrl;

        if (Uri.TryCreate(hubUrl, UriKind.Absolute, out var uri))
        {
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }

        var scheme = hubUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
        return $"{scheme}://{settings.ServerIp}:{settings.ServerPort}";
    }
}

