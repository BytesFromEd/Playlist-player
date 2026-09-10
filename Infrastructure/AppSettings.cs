using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Infrastructure.AppSettingsSections;

namespace Infrastructure;

public class AppSettings : IDisposable
{
    public string AppFolder { get; private set; }
    public float Volume { get; set; }

    public YoutubeSettings Youtube { get; set; }

    private static AppSettings? instance;

    public AppSettings()
    {
        AppFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PlaylistPlayer");

        if (!Directory.Exists(AppFolder))
            Directory.CreateDirectory(AppFolder);

        if (File.Exists(Path.Combine(AppFolder, "settings.json")))
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(
                File.ReadAllText(Path.Combine(AppFolder, "settings.json"))
            )!;

            Volume = data.TryGetValue("Volume", out var value) ? value as float? ?? 1.0f : 1.0f;
            Youtube = new YoutubeSettings(
                data.TryGetValue("Youtube", out var youtube) ? youtube as Dictionary<string, object> : null
            );
        }
        else
        {
            Volume = 1.0f;
            Youtube = new YoutubeSettings(null);
        }
    }

    public static AppSettings GetInstance()
    {
        instance ??= new AppSettings();
        return instance;
    }

    [SuppressMessage("Performance", "CA1869:Cache and reuse \'JsonSerializerOptions\' instances")]
    private void Save()
    {
        var dic = new Dictionary<string, object>
        {
            { "Volume", Volume },
            { "Youtube", Youtube.GetSettings() }
        };

        File.WriteAllText(Path.Combine(AppFolder, "settings.json"),
            JsonSerializer.Serialize(dic, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Dispose()
    {
        Save();
    }
}