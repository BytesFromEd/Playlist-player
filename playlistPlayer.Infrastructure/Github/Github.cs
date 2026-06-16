namespace playlistPlayer.Infrastructure.GithubAsset;

using System.Net.Http;
using System.Text.Json;

public class Github
{
    public static async Task<string> DownloadLatestReleaseAsset(string owner, string repo, Func<GithubAsset, bool> findCallback, string outputPath)
    {
        using var http = new HttpClient();

        http.DefaultRequestHeaders.UserAgent.ParseAdd("playlisyPlayer/1.0");

        string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        var releaseJson = await http.GetStringAsync(apiUrl);

        var release = JsonSerializer.Deserialize<GithubRelease>(releaseJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        if (release?.Assets == null || release.Assets.Count == 0)
            throw new Exception("No assets found in latest release.");

        var asset = release.Assets.First(findCallback);

        Console.WriteLine($"Downloading {asset.Name}...");

        await using var stream = await http.GetStreamAsync(asset.Browser_Download_Url);

        var path = Path.Combine(outputPath, asset.Name);

        if (Path.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        await using var file = File.Create(path);

        await stream.CopyToAsync(file);

        return path;
    }

    public static async Task<string> GetLatestReleaseVersionAsync(string owner, string repo)
    {
        using var http = new HttpClient();

        http.DefaultRequestHeaders.UserAgent.ParseAdd("playlisyPlayer/1.0");

        string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("tag_name")
            .GetString()
            ?? throw new Exception("tag_name not found in release.");
    }
}