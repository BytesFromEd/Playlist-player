using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Infrastructure.AppSettingsSections;

namespace Infrastructure;

public class AppSettings : IDisposable
{
    public string AppFolder { get; private set; }
    public float Volume { get; set; }
    public bool FadeSong { get; set; }
    public long FadeDuration { get; set; }

    public YoutubeSettings Youtube { get; set; }
    private static AppSettings? _instance;

    public AppSettings()
    {
        AppFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PlaylistPlayer");

        if (!Directory.Exists(AppFolder))
            Directory.CreateDirectory(AppFolder);

        if (File.Exists(Path.Combine(AppFolder, "settings.json")))
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                File.ReadAllText(Path.Combine(AppFolder, "settings.json"))
            );

            if (data == null)
                throw new Exception("Settings file could not be read.");

            Volume = data.TryGetValue("Volume", out var volume) ? (float)volume.GetDouble() : 1.0f;
            FadeSong = data.TryGetValue("Fade", out var fade) && fade.GetBoolean();
            FadeDuration = data.TryGetValue("FadeDuration", out var duration) ? duration.GetInt64() : 0;
            Youtube = new YoutubeSettings(
                data.TryGetValue("Youtube", out var youtube) ? youtube: null
            );
        }
        else
        {
            Volume = 1.0f;
            FadeSong = false;
            FadeDuration = 0;
            Youtube = new YoutubeSettings(null);
        }

        _instance = this;
    }

    public static AppSettings GetInstance()
    {
        _instance ??= new AppSettings();
        return _instance;
    }

    [SuppressMessage("Performance", "CA1869:Cache and reuse \'JsonSerializerOptions\' instances")]
    private void Save()
    {
        var dic = new Dictionary<string, object>
        {
            { "Volume", Volume },
            { "Youtube", Youtube.GetSettings() },
            { "Fade", FadeSong },
            { "FadeDuration", FadeDuration },
        };

        File.WriteAllText(Path.Combine(AppFolder, "settings.json"),
            JsonSerializer.Serialize(dic, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Dispose()
    {
        Save();
    }
}