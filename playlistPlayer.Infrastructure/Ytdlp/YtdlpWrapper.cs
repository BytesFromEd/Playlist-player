namespace playlistPlayer.Infrastructure.Ytdlp;

using playlistPlayer.Core.Interfaces;
using playlistPlayer.Core.Models;
using playlistPlayer.Infrastructure.GithubAsset;
using playlistPlayer.Infrastructure.Storage;

using ManuHub.Ytdlp.NET;
using System.Runtime.InteropServices;
using System.Text.Json;

public class YtdlpWrapper : IDownloadService, IPlaylistProvider
{
    private Ytdlp service;
    public event EventHandler<YtdlpMessageEventArgs>? OnYtdlpMessages;
    public event EventHandler<DownloadProgressEventArgs>? OnYtdlpDownloadProgress;

    public YtdlpWrapper()
    {
        //add deno download
        string version = Github.GetLatestReleaseVersionAsync("yt-dlp", "yt-dlp").GetAwaiter().GetResult();
        string filename;

        YtdlpDatabase.CreateTable();

        if (YtdlpDatabase.SetVersion(version))
        {

            filename = Github.DownloadLatestReleaseAsset("yt-dlp", "yt-dlp", (asset) =>
                {
                    if (OperatingSystem.IsWindows())
                    {
                        return asset.Name.EndsWith(".exe");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        return asset.Name.EndsWith("_linux");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        return asset.Name.EndsWith("_macos");
                    }
                    else
                    {
                        throw new Exception($"OS: {RuntimeInformation.OSDescription} arch: {RuntimeInformation.OSArchitecture} isn't supported yet!");
                    }
                }, "./tools")
                .GetAwaiter()
                .GetResult();
        }
        else
        {
            filename = YtdlpDatabase.GetFilename();
        }

        const string outputPath = "./songs";

        service = new Ytdlp(filename)
            .WithBestAudioOnly()
            .WithEmbedThumbnail()
            .WithOutputFolder(outputPath)
            .WithFlatPlaylist()
            .WithOutputTemplate("%(id)s.%(ext)s");

        if (Path.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        service.ProgressDownload += (s, e) =>
        {
            OnYtdlpDownloadProgress?.Invoke(this, e);
        };
        service.DownloadCompleted += (s, msg) =>
        {
            OnYtdlpMessages?.Invoke(this, new YtdlpMessageEventArgs("Song download finished: " + msg));
        };
        service.ErrorMessage += (s, msg) =>
        {
            OnYtdlpMessages?.Invoke(this, new YtdlpMessageEventArgs("yt-dlp error: " + msg));
        };
    }

    //todo confirm it is the file path
    public async Task<Song[]> DownloadSong(params Song[] songs)
    {
        CancellationTokenSource ct = new();

        var urls = songs.Select(x => x.GetId());

        var downloadedFiles = new List<string>();

        service.DownloadCompleted += (sender, message) =>
        {
            downloadedFiles.Add(message);
        };

        await service.DownloadBatchAsync(urls, 3, ct.Token);

        if (ct.IsCancellationRequested)
        {
            return [];
        }

        return [.. songs.Select(x => {
            x.SetFile(downloadedFiles.First(y => y.Contains(x.GetId())));
            return x;
        })];
    }

    //yt-dlp --remote-components ejs:github --dump-single-json --skip-download https://youtube.com/playlist?list=PLebv-XoARUkw6PruMaKKOFWhvEjB96BSp
    public async Task<Playlist?> GetPlaylist(string url)
    {
        if (url.Contains("music.")) url = url.Replace("music.", "");

        var result = await service.ExecuteRawAsync("--dump-single-json --skip-download " + url);

        System.Console.WriteLine(result.FullOutput);

        if (!result.IsSuccess || result.FullOutput == null)
        {
            return null;
        }

        Playlist? playlist = JsonSerializer.Deserialize<Playlist>(result.FullOutput, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return playlist;
    }
}
