using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace Infrastructure;

public class AppSettings : IDisposable
{
    public string AppFolder { get; private set; }
    public float Volume { get; set; }
    
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
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(AppFolder, "settings.json"))
            )!;

            Volume = float.Parse(data["Volume"], CultureInfo.InvariantCulture);
        }
        else
        {
            Volume = 1.0f;
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
        var dic = new Dictionary<string, string>
        {
            { "Volume", Volume.ToString(CultureInfo.InvariantCulture) }
        };

        File.WriteAllText(Path.Combine(AppFolder, "settings.json"),
            JsonSerializer.Serialize(dic, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Dispose()
    {
        Save();
    }
}