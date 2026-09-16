using System.Text.Json;

namespace Infrastructure.AppSettingsSections;

public class YoutubeSettings
{
    public bool DownloadTools { get; set; }
    public bool UseCookie { get; set; }

    public YoutubeSettings(JsonElement? settings)
    {
        if (settings.HasValue)
        {
            DownloadTools = !settings.Value.TryGetProperty(nameof(DownloadTools), out var downloadvalue) ||
                            downloadvalue.GetBoolean();
            UseCookie = !settings.Value.TryGetProperty(nameof(UseCookie), out var cookievalue) ||
                        cookievalue.GetBoolean();
        }
        else
            SetDefaults();
    }

    private void SetDefaults()
    {
        DownloadTools = true;
        UseCookie = false;
    }

    public Dictionary<string, object> GetSettings()
    {
        return new Dictionary<string, object>()
        {
            { "DownloadTools", DownloadTools },
            { "UseCookie", UseCookie }
        };
    }
}