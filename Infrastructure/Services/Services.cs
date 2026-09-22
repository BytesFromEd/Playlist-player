using Core.Models;
using Infrastructure.Services.Ytdlp;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Playlist = Core.Models.Playlist;

namespace Infrastructure.Services;

public class Services
{
    private YtdlpWrapper? ytdlpWrapper;

    public event EventHandler<ServiceEventArgs>? OnServiceMessage;
    public event EventHandler<EventArgs>? OnServiceDownloadProgress;

    public Services()
    {
        ytdlpWrapper?.OnYtdlpMessages += (sender, msg) => OnServiceMessage?.Invoke(sender, msg);

        ytdlpWrapper?.OnYtdlpDownloadProgress += (sender, args) => OnServiceDownloadProgress?.Invoke(sender, args);
    }

    public Task Initialize(CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            var client = new HttpClient();

            await using var context = new Database();
            await context.Database.MigrateAsync(ct);

            ytdlpWrapper = new YtdlpWrapper(client);
        }, ct);
    }

    public async Task<Playlist?> AddPlayist(string url, CancellationToken ct)
    {
        if (ytdlpWrapper == null)
        {
            return null;
        }

        var playlist = await ytdlpWrapper.GetPlaylist(url, ct);
        if (ct.IsCancellationRequested)
        {
            return null;
        }

        if (playlist == null)
            throw new Exception("Playlist not found in URL: " + url);

        await ytdlpWrapper.DownloadSongs(playlist.Id, ct, playlist.Songs);
        return ct.IsCancellationRequested ? null : playlist;
    }

    public async Task<Playlist?> RefreshPlaylist(string playlistId, CancellationToken ct)
    {
        if (ytdlpWrapper == null)
        {
            return null;
        }

        var playlist = await ytdlpWrapper.RefreshPlaylist(playlistId, ct);
        if (ct.IsCancellationRequested)
        {
            return null;
        }

        if (playlist == null)
            throw new Exception("Error fetching playlist");

        await ytdlpWrapper.DownloadSongs(playlist.Id, ct, playlist.Songs);
        return ct.IsCancellationRequested ? null : playlist;
    }

    public static List<Playlist> GetPlaylists()
    {
        using var db = new Database();
        return [.. db.Playlists];
    }

    public static Playlist GetPlaylist(string id)
    {
        using var db = new Database();
        var playlist = db.Playlists
            .Include(p => p.Songs)
            .FirstOrDefault(x => x.Id == id);

        return playlist ?? throw new Exception($"Playlist {id} not found");
    }

    public static bool PlaylistExists(string id)
    {
        using var db = new Database();
        return db.Playlists.Any(x => x.Id == id);
    }

    public static void RemovePlaylist(Playlist playlist, bool removeSongs)
    {
        using var db = new Database();
        db.Playlists.Remove(playlist);
    }

    public static void RemoveSongs(params List<Song> songs)
    {
        using var db = new Database();
        db.Songs.RemoveRange(songs);
    }

    public async Task UpdateTools()
    {
        if (ytdlpWrapper != null)
            await ytdlpWrapper.UpdateTools();
    }
}