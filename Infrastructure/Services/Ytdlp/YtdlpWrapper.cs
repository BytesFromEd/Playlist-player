using System.Data;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Interfaces;
using Core.Models;
using Core.Models.Enums;
using Infrastructure.Services.Github;
using Infrastructure.Services.Storage;
using ManuHub.Ytdlp.NET;
using ManuHub.Ytdlp.NET.Core;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Playlist = Infrastructure.Services.Ytdlp.YtdlpJsonClasses.Playlist;

namespace Infrastructure.Services.Ytdlp;

internal partial class YtdlpWrapper : IDownloadService, IPlaylistProvider
{
    private readonly HttpClient client;
    private ManuHub.Ytdlp.NET.Ytdlp ytdlp;
    public event EventHandler<ServiceEventArgs>? OnYtdlpMessages;
    public event EventHandler<DownloadProgressEventArgs>? OnYtdlpDownloadProgress;

    private readonly JsonSerializerOptions options;

    private readonly string toolsOutFolder = Path.Combine(AppSettings.GetInstance().AppFolder, "./tools");
    private readonly string tempOutFolder = Path.Combine(AppSettings.GetInstance().AppFolder, "./temp");
    private readonly string songsOutFolder = Path.Combine(AppSettings.GetInstance().AppFolder, "./songs");
    private readonly string thumbnailOutFolder = Path.Combine(AppSettings.GetInstance().AppFolder, "./thumbnails");

    private readonly Regex removeVideo;

    public YtdlpWrapper(HttpClient client)
    {
        this.client = client;

        YtdlpDatabase.CreateTable();

        if (AppSettings.GetInstance().Youtube.DownloadTools)
        {
            DownloadTools()
                .GetAwaiter()
                .GetResult();
        }

        var outputPath = Path.GetFullPath(songsOutFolder);

        var executable = 0 switch
        {
            _ when OperatingSystem.IsWindows() && !AppSettings.GetInstance().Youtube.DownloadTools => "yt-dlp.exe",
            _ when OperatingSystem.IsLinux() && !AppSettings.GetInstance().Youtube.DownloadTools => "yt-dlp",
            _ when AppSettings.GetInstance().Youtube.DownloadTools => Path.Combine(toolsOutFolder,
                YtdlpDatabase.GetFilenameYtdlp()),
            _ => throw new PlatformNotSupportedException()
        };

        ytdlp = GetTool(executable, AppSettings.GetInstance().AppFolder);

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

        removeVideo = RemoveVideoRegex();
    }

    private static ManuHub.Ytdlp.NET.Ytdlp GetTool(string executable, string root)
    {
        var temp = new ManuHub.Ytdlp.NET.Ytdlp(executable)
                .WithExtractAudio(AudioFormat.Mp3)
                .WithOutputFolder(Path.Combine(root, "songs"))
                .WithTempFolder(Path.Combine(root, "temp"))
                .WithThumbnails()
                .WithOutputTemplate("%(id)s.%(ext)s")
                .AddOption("--extractor-args", "youtube:player_client=default,web_embedded")
            ;

        if (AppSettings.GetInstance().Youtube.UseCookie)
        {
            temp = temp.WithCookiesFile(Path.Combine(AppSettings.GetInstance().AppFolder, "cookies.txt"));
        }

        if (AppSettings.GetInstance().Youtube.DownloadTools)
        {
            temp = temp.WithFFmpegLocation(Path.Combine(root, "tools"))
                .WithJsRuntime(Runtime.Deno, Path.Combine(root, "tools"));
        }

        return temp;
    }

    [GeneratedRegex(@"&?v=[a-zA-Z0-9_-]+&?")]
    private static partial Regex RemoveVideoRegex();

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

    private async Task<Core.Models.Playlist?> PlaylistGetter(string url, string id, CancellationToken ct)
    {
        if (url.Contains("music.")) url = url.Replace("music.", "");
        ProcessResult result;

        try
        {
            result =
                await ytdlp.ExecuteRawAsync(string.Join(" ",
                    ["--dump-single-json", "--skip-download", "--flat-playlist", url]), null, ct);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        if (ct.IsCancellationRequested)
        {
            return null;
        }

        if (!result.IsSuccess || result.FullOutput == null || result.FullOutput.StartsWith("null") ||
            result.ExitCode != 0)
        {
            throw new Exception("yt-dlp exit code: " + result.ExitCode + " please see logs to have more information");
        }

        Console.WriteLine("Playlist information fetched");

        Playlist rawPlaylist;
        try
        {
            rawPlaylist = JsonSerializer.Deserialize<Playlist>(result.FullOutput, options)!;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        Console.WriteLine("Playlist fetched information is right");

        var thumbnailUrl = rawPlaylist.Thumbnails?.MaxBy(x => x.Id)?.Url;

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

        Console.WriteLine("Playlist thumbnail cropped");

        var playlist = new Core.Models.Playlist(rawPlaylist.Title ?? "ERROR",
            rawPlaylist.Uploader ?? "ERROR",
            id,
            Provider.Youtube,
            thumbnailUrl != null ? id + ".jpg" : null,
            rawPlaylist
                .Entries?
                .Select(x => new Song(x.Id ?? "ERROR ID",
                    x.Title ?? "ERROR TITLE",
                    x.Uploader ?? "ERROR UPLOADER",
                    x.Id != null ? x.Id + ".mp3" : "ERROR ID",
                    x.Id != null ? x.Id + ".webp" : "ERROR ID",
                    (int)Math.Floor(x.Duration ?? -1),
                    DateTime.Now,
                    Provider.Youtube))
            ?? []
        );

        Console.WriteLine("Playlist information successfully downloaded");
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

        var playlist = await PlaylistGetter(url, id, ct);

        if (playlist == null)
            throw new Exception("Playlist not found");

        Database.AddPlaylist(playlist);

        return playlist;
    }

    public async Task<Core.Models.Playlist?> RefreshPlaylist(Core.Models.Playlist playlist, CancellationToken ct)
    {
        var temp = await PlaylistGetter($"https://youtube.com/watch?list={playlist.GetId()}", playlist.GetId(), ct);

        if (temp == null)
            throw new Exception("Playlist not found");

        Database.UpdatePlaylist(temp);

        return temp;
    }

    private async Task DownloadTools()
    {
        //yt-dlp
        if (YtdlpDatabase.GetVersionYtdlp() == "xx")
        {
            var ytdlpVersion = await Github.Github.GetLatestReleaseVersionAsync("yt-dlp", "yt-dlp", client);
            var ytdlpExe =
                await Github.Github.DownloadLatestReleaseAsset("yt-dlp", "yt-dlp", YtdlpFilter, tempOutFolder, client);

            YtdlpDatabase.SetVersionYtdlp(ytdlpVersion);
            YtdlpDatabase.SetFilenameYtdlp(ytdlpExe);
        }

        //ffmpeg
        if (YtdlpDatabase.GetVersionFfmpeg() == "xx")
        {
            var ffmpegVersion = await Github.Github.GetLatestReleaseVersionAsync("yt-dlp", "FFmpeg-Builds", client);
            var filename =
                await Github.Github.DownloadLatestReleaseAsset("yt-dlp", "FFmpeg-Builds", FfmpegFilter, tempOutFolder,
                    client);

            ExtractFile(Path.Combine(tempOutFolder, filename), toolsOutFolder,
                entry => entry is { IsDirectory: false, Key: not null } &&
                         (entry.Key.EndsWith("ffmpeg" + (OperatingSystem.IsWindows() ? ".exe" : "")) ||
                          entry.Key.EndsWith("ffprobe" + (OperatingSystem.IsWindows() ? ".exe" : "")))
            );
            YtdlpDatabase.SetVersionFfmpeg(ffmpegVersion);
        }

        //deno
        if (YtdlpDatabase.GetVersionDeno() == "xx")
        {
            var denoVersion = await Github.Github.GetLatestReleaseVersionAsync("denoland", "deno", client);
            var filename =
                await Github.Github.DownloadLatestReleaseAsset("denoland", "deno", DenoFilter, tempOutFolder, client);

            ExtractFile(Path.Combine(tempOutFolder, filename), toolsOutFolder,
                entry => entry is { IsDirectory: false, Key: not null }
            );
            YtdlpDatabase.SetVersionDeno(denoVersion);
        }

        ytdlp = GetTool(Path.GetFullPath(Path.Combine(toolsOutFolder, YtdlpDatabase.GetFilenameYtdlp())),
            AppSettings.GetInstance().AppFolder);
    }

    private static void ExtractFile(string filename, string output, Func<IArchiveEntry, bool> filter)
    {
        var progress = new Progress<ProgressReport>(report =>
        {
            Console.WriteLine($"Extracting {report.EntryPath}: {report.PercentComplete}%");
        });

        using var archive = ArchiveFactory.OpenArchive(filename, ReaderOptions.ForFilePath.WithProgress(progress));

        foreach (var archiveEntry in archive.Entries.Where(filter))
        {
            if (archiveEntry.IsDirectory ||
                archiveEntry.Key == null) continue;

            archiveEntry.WriteToFile(
                Path.Combine(output, archiveEntry.Key.Split(Path.AltDirectorySeparatorChar).Last()));
        }
    }

    public async Task UpdateTools()
    {
        if (YtdlpDatabase.GetVersionYtdlp() != "Manual")
            await ytdlp.UpdateAsync();

        var ffmpegVersion = await Github.Github.GetLatestReleaseVersionAsync("yt-dlp", "FFmpeg-Builds", client);
        //ffmpeg
        if (YtdlpDatabase.GetVersionFfmpeg() != "Manual" && YtdlpDatabase.GetVersionFfmpeg() != ffmpegVersion)
        {
            var filename =
                await Github.Github.DownloadLatestReleaseAsset("yt-dlp", "FFmpeg-Builds", FfmpegFilter, tempOutFolder,
                    client);

            ExtractFile(Path.Combine(tempOutFolder, filename), toolsOutFolder,
                entry => entry is { IsDirectory: false, Key: not null } &&
                         (entry.Key.EndsWith("ffmpeg" + (OperatingSystem.IsWindows() ? ".exe" : "")) ||
                          entry.Key.EndsWith("ffprobe" + (OperatingSystem.IsWindows() ? ".exe" : "")))
            );
            YtdlpDatabase.SetVersionFfmpeg(ffmpegVersion);
        }

        var denoVersion = await Github.Github.GetLatestReleaseVersionAsync("denoland", "deno", client);
        //deno
        if (YtdlpDatabase.GetVersionDeno() != "Manual" && YtdlpDatabase.GetVersionDeno() != denoVersion)
        {
            var filename =
                await Github.Github.DownloadLatestReleaseAsset("denoland", "deno", DenoFilter, tempOutFolder, client);

            ExtractFile(Path.Combine(tempOutFolder, filename), toolsOutFolder,
                entry => entry is { IsDirectory: false, Key: not null }
            );
            YtdlpDatabase.SetVersionDeno(denoVersion);
        }

        ytdlp = GetTool(Path.GetFullPath(Path.Combine(toolsOutFolder, YtdlpDatabase.GetFilenameYtdlp())),
            AppSettings.GetInstance().AppFolder);
    }

    private static bool YtdlpFilter(GithubAsset asset)
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => asset.Name == "yt-dlp_arm64.exe",
                Architecture.X64 => asset.Name == "yt-dlp.exe",
                Architecture.X86 => asset.Name == "yt-dlp_x86.exe",
                _ => throw new PlatformNotSupportedException("Executable for this platform not found")
            };
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            if (ExternalServices.IsInstalled("python"))
            {
                return asset.Name == "yt-dlp";
            }

            return asset.Name == "yt-dlp_linux";
        }

        if (OperatingSystem.IsMacOS())
        {
            return asset.Name == "yt-dlp_macos";
        }

        throw new PlatformNotSupportedException("Executable for this platform not found");
    }

    private static bool DenoFilter(GithubAsset asset)
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 or Architecture.X86 => asset.Name == "deno-x86_64-pc-windows-msvc.zip",
                _ => throw new PlatformNotSupportedException("Executable for this platform not found")
            };
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => asset.Name == "deno-x86_64-unknown-linux-gnu.zip",
                _ => throw new PlatformNotSupportedException("Executable for this platform not found")
            };
        }

        throw new PlatformNotSupportedException("Executable for this platform not found");
    }

    private static bool FfmpegFilter(GithubAsset asset)
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => asset.Name == "ffmpeg-master-latest-win64-gpl.zip",
                Architecture.X86 => asset.Name == "ffmpeg-master-latest-win32-gpl.zip",
                _ => throw new PlatformNotSupportedException("Executable for this platform not found")
            };
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => asset.Name == "ffmpeg-master-latest-linux64-gpl.zip",
                Architecture.Arm64 => asset.Name == "ffmpeg-master-latest-linuxarm64-gpl.zip",
                _ => throw new PlatformNotSupportedException("Executable for this platform not found")
            };
        }

        throw new PlatformNotSupportedException("Executable for this platform not found");
    }
}