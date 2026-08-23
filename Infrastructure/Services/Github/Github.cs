using System.Text.Json;

namespace Infrastructure.Services.Github;

internal class Github
{
    public static async Task<string> DownloadLatestReleaseAsset(string owner, string repo, Func<GithubAsset, bool> findCallback, string outputPath, HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("playlisyPlayer/1.0");

        var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        var releaseJson = await httpClient.GetStringAsync(apiUrl);

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

        await using var stream = await httpClient.GetStreamAsync(asset.Browser_Download_Url);

        var filename = Path.Combine(outputPath, asset.Name);

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        await using var file = File.Create(filename);

        await stream.CopyToAsync(file);

        return filename;
    }

    public static async Task<string> GetLatestReleaseVersionAsync(string owner, string repo, HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("playlisyPlayer/1.0");

        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        using var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("tag_name")
            .GetString()
            ?? throw new Exception("tag_name not found in release.");
    }
}