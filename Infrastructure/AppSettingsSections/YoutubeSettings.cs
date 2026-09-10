namespace Infrastructure.AppSettingsSections;

public class YoutubeSettings
{
    public bool DownloadTools { get; set; }
    public bool UseCookie { get; set; }

    public YoutubeSettings(Dictionary<string, object>? settings)
    {
        if (settings != null)
        {
            DownloadTools = !settings.TryGetValue("DownloadTools", out var downloadvalue) ||
                            (downloadvalue as bool? ?? true);
            UseCookie = !settings.TryGetValue("UseCookie", out var cookievalue) ||
                        (cookievalue as bool? ?? true);
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