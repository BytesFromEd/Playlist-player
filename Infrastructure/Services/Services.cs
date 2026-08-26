using Infrastructure.Services.Storage;
using Infrastructure.Services.Ytdlp;
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
        return Task.Run(() =>
        {
            var client = new HttpClient();
            ytdlpWrapper = new YtdlpWrapper(client);
        }, ct);
    }

    public void CreateTable()
    {
        Database.CreateInitialTables();
        YtdlpDatabase.CreateTable();
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

        await ytdlpWrapper.DownloadSongs(playlist.GetId(), ct, playlist.GetSongs());
        return ct.IsCancellationRequested ? null : playlist;
    }

    public async Task<Playlist?> RefreshPlaylist(Playlist playlist, CancellationToken ct)
    {
        if (ytdlpWrapper == null)
        {
            return null;
        }
        
        var temp = await ytdlpWrapper.RefreshPlaylist(playlist, ct);
        if (ct.IsCancellationRequested)
        {
            return null;
        }

        if (temp == null)
            throw new Exception("Error fetching playlist");

        await ytdlpWrapper.DownloadSongs(temp.GetId(), ct, temp.GetSongs());
        return ct.IsCancellationRequested ? null : temp;
    }

    public List<Playlist> GetPlaylists()
    {
        return Database.GetPlaylists();
    }

    public Playlist GetPlaylist(string id)
    {
        var playlist = Database.GetPlaylist(id);

        return playlist ?? throw new Exception($"Playlist {id} not found");
    }

    public bool PlaylistExists(string id)
    {
        return Database.PlaylistExists(id);
    }
}