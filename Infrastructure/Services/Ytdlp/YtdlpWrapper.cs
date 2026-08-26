using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Interfaces;
using Core.Models;
using Core.Models.Enums;
using Infrastructure.Services.Storage;
using ManuHub.Ytdlp.NET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Playlist = Infrastructure.Services.Ytdlp.YtdlpJsonClasses.Playlist;

namespace Infrastructure.Services.Ytdlp;

internal class YtdlpWrapper : IDownloadService, IPlaylistProvider
{
    private readonly HttpClient client;
    private readonly ManuHub.Ytdlp.NET.Ytdlp ytdlp;
    public event EventHandler<ServiceEventArgs>? OnYtdlpMessages;
    public event EventHandler<DownloadProgressEventArgs>? OnYtdlpDownloadProgress;

    private readonly JsonSerializerOptions options;

    private readonly string songsOutFolder = Path.Combine(AppSettings.GetInstance().AppFolder, "./songs");
    private readonly string thumbnailOutFolder = Path.Combine(AppSettings.GetInstance().AppFolder, "./thumbnails");

    private readonly Regex removeVideo;

    public YtdlpWrapper(HttpClient client)
    {
        this.client = client;

        YtdlpDatabase.CreateTable();

        var outputPath = Path.GetFullPath(songsOutFolder);

        ytdlp = new ManuHub.Ytdlp.NET.Ytdlp()
                .WithExtractAudio(AudioFormat.Mp3)
                .WithOutputFolder(outputPath)
                .WithThumbnails()
                .WithOutputTemplate("%(id)s.%(ext)s")
                .WithCookiesFile(Path.Combine(AppSettings.GetInstance().AppFolder, "cookies.txt"))
                .AddOption("--extractor-args", "youtube:player_client=default,web_embedded")
            ;

        if (Path.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        ytdlp.ProgressDownload += (_, e) => { OnYtdlpDownloadProgress?.Invoke(this, e); };
        ytdlp.DownloadCompleted += (_, msg) =>
        {
            OnYtdlpMessages?.Invoke(this, new ServiceEventArgs("Song download finished: " + msg));
        };
        ytdlp.ErrorMessage += (_, msg) =>
        {
            OnYtdlpMessages?.Invoke(this, new ServiceEventArgs("yt-dlp error: " + msg));
        };

        options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        removeVideo = new Regex(@"&?v=[a-zA-Z0-9_-]+&?");
    }

    public async Task DownloadSongs(string playlist, CancellationToken ct, params List<Song> songs)
    {
        var outputPath = Path.GetFullPath(songsOutFolder);

        var filesDownloaded = Directory.Exists(outputPath) ? Directory.GetFiles(outputPath, "*.mp3") : [];

        var urls = songs.Where(x => !filesDownloaded.Any(y => y.Contains(x.GetId())))
            .Select(x => $"https://www.youtube.com/watch?v={x.GetId()}").ToList();

        if (urls.Count == 0) return;

        try
        {
            await ytdlp.DownloadBatchAsync(urls, 3, ct);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            filesDownloaded = [.. Directory.GetFiles(outputPath).Where(x => !x.EndsWith("mp3") && !x.EndsWith("webp"))];

            foreach (var file in filesDownloaded)
            {
                File.Delete(file);
                Console.WriteLine("Deleting file: " + file);
            }


            filesDownloaded = Directory.GetFiles(outputPath, "*.mp3");
            // ReSharper disable once AccessToModifiedClosure
            songs = [.. songs.Where(x => filesDownloaded.Any(y => y.Contains(x.GetId())))];

            filesDownloaded = Directory.GetFiles(outputPath, "*.webp");

            foreach (var file in filesDownloaded)
            {
                if (!File.Exists(file.Replace(".webp", ".mp3"))) continue;
                using var image = await Image.LoadAsync(file, ct);

                if (ct.IsCancellationRequested)
                {
                    return;
                }

                if (image.Width != image.Height)
                {
                    OnYtdlpMessages?.Invoke(this, new ServiceEventArgs("Cropping image: " + file));

                    var width = image.Width;
                    var height = image.Height;
                    var smaller = Math.Min(image.Width, image.Height);
                    image.Mutate(ctx =>
                        ctx.Crop(
                            new Rectangle(
                                width / 2 - smaller / 2,
                                height / 2 - smaller / 2,
                                smaller,
                                smaller
                            )
                        )
                    );
                }

                await image.SaveAsWebpAsync(file, ct);
                if (ct.IsCancellationRequested)
                {
                    return;
                }
            }

            Database.AddSongs(playlist, songs);
        }
        catch (Exception ex)
        {
            Console.WriteLine(">>>ERROR<<< " + ex.Message);
        }
    }

    private async Task<Core.Models.Playlist?> PlaylistSetter(string url, string id, CancellationToken ct)
    {
        if (url.Contains("music.")) url = url.Replace("music.", "");

        var result =
            await ytdlp.ExecuteRawAsync(string.Join(" ",
                ["--dump-single-json", "--skip-download", "--flat-playlist", url]), null, ct);
        if (ct.IsCancellationRequested)
        {
            return null;
        }

        if (!result.IsSuccess || result.FullOutput == null || result.FullOutput.StartsWith("null") ||
            result.ExitCode != 0)
        {
            throw new Exception("yt-dlp exit code: " + result.ExitCode + " please see logs to have more information");
        }

        var rawPlaylist = JsonSerializer.Deserialize<Playlist>(result.FullOutput, options);

        var thumbnailUrl = rawPlaylist?.Thumbnails?.MaxBy(x => x.Id)?.Url;

        if (thumbnailUrl != null)
        {
            if (!Directory.Exists(thumbnailOutFolder))
            {
                Directory.CreateDirectory(thumbnailOutFolder);
            }

            await using var s = await client.GetStreamAsync(thumbnailUrl, ct);
            if (ct.IsCancellationRequested)
            {
                return null;
            }

            using var image = await Image.LoadAsync(s, ct);
            if (ct.IsCancellationRequested)
            {
                return null;
            }

            if (image.Width != image.Height)
            {
                var width = image.Width;
                var height = image.Height;
                var smaller = Math.Min(image.Width, image.Height);
                image.Mutate(ctx =>
                    ctx.Crop(
                        new Rectangle(
                            width / 2 - smaller / 2,
                            height / 2 - smaller / 2,
                            smaller,
                            smaller
                        )
                    )
                );
            }

            await image.SaveAsJpegAsync(Path.Combine(thumbnailOutFolder, id + ".jpg"), ct);
            if (ct.IsCancellationRequested)
            {
                return null;
            }
        }

        var playlist = new Core.Models.Playlist(rawPlaylist?.Title ?? "ERROR",
            rawPlaylist?.Uploader ?? "ERROR",
            id,
            Provider.Youtube,
            thumbnailUrl != null ? Path.Combine(thumbnailOutFolder, id + ".jpg") : null,
            rawPlaylist?
                .Entries?
                .Select(x => new Song(x.Id ?? "ERROR ID",
                    x.Title ?? "ERROR TITLE",
                    x.Uploader ?? "ERROR UPLOADER",
                    x.Id != null ? (x.Id + ".mp3") : "ERROR ID",
                    x.Id != null ? (x.Id + ".webp") : "ERROR ID",
                    x.Duration ?? -1,
                    DateTime.Now,
                    Provider.Youtube))
            ?? []
        );
        return playlist;
    }


    public async Task<Core.Models.Playlist?> GetPlaylist(string url, CancellationToken ct)
    {
        string id;
        try
        {
            id = url[(url.IndexOf("list=", StringComparison.Ordinal) + 5)..];
            url = removeVideo.Replace(url, string.Empty);
        }
        catch (Exception)
        {
            throw new Exception("Id not found in URL: " + url);
        }

        if (Database.PlaylistExists(id))
        {
            throw new DuplicateNameException("Playlist already exists");
        }

        var playlist = await PlaylistSetter(url, id, ct);

        if (playlist == null)
            throw new Exception("Playlist not found");

        Database.AddPlaylist(playlist);

        return playlist;
    }

    public async Task<Core.Models.Playlist?> RefreshPlaylist(Core.Models.Playlist playlist, CancellationToken ct)
    {
        var temp = await PlaylistSetter($"https://youtube.com/watch?list={playlist.GetId()}", playlist.GetId(), ct);

        if (temp == null)
            throw new Exception("Playlist not found");

        Database.UpdatePlaylist(temp);

        return temp;
    }
}