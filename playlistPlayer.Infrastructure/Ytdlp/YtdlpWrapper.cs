namespace playlistPlayer.Infrastructure.Ytdlp;

using playlistPlayer.Core.Interfaces;
using playlistPlayer.Core.Models;
using playlistPlayer.Infrastructure.GithubAsset;

using ManuHub.Ytdlp.NET;
using System.Runtime.InteropServices;

public class YtdlpWrapper : IDownloadService
{
    private Ytdlp service;
    public event EventHandler<YtdlpMessageEventArgs>? OnYtdlpMessages;
    public event EventHandler<DownloadProgressEventArgs>? OnYtdlpDownloadProgress;

    public YtdlpWrapper()
    {
        string version = Github.GetLatestReleaseVersionAsync("yt-dlp", "yt-dlp").GetAwaiter().GetResult();

        //check if there's a new release and download it if there is

        string filename = Github.DownloadLatestReleaseAsset("yt-dlp", "yt-dlp", (asset) =>
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


        service = new Ytdlp(filename)
            .WithBestAudioOnly()
            .WithEmbedThumbnail()
            .WithOutputFolder("./songs")
            .WithOutputTemplate("%(id)s.%(ext)s");

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
    public async Task<Song[]> DownloadSong(Func<string, string> selector, params string[] ids)
    {
        CancellationTokenSource ct = new();

        var urls = ids.Select(selector);

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

        var songsMetadata = await GetSongMetadata(selector, ct.Token, ids);

        if (ct.IsCancellationRequested)
        {
            return [];
        }

        List<Song> songs = [];

        foreach (var info in songsMetadata)
        {
            if (info == null) continue;

            var file = downloadedFiles.FirstOrDefault(x => x.Contains(info.Id!, StringComparison.CurrentCultureIgnoreCase));

            if (file == null) continue;

            songs.Add(new Song(
                info.Id ?? "ID_ERROR",
                info.Title ?? "TITLE_ERROR",
                new Author(info.Uploader ?? "UPLOADER_ERROR"),
                file,
                new TimeSpan((long)(info.Duration ?? 0)),
                DateTime.Now
            ));
        }

        return [.. songs];
    }

    private async Task<Metadata[]> GetSongMetadata(Func<string, string> selector, CancellationToken ct, params string[] ids)
    {
        List<Metadata> songsMetadata = [];

        foreach (var url in ids.Select(selector))
        {
            var metadata = await service.GetMetadataAsync(url, ct);

            if (metadata != null)
                songsMetadata.Add(metadata);
        }

        return [.. songsMetadata];
    }
}
