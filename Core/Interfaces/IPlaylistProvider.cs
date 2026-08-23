namespace Core.Interfaces;

using Models;

public interface IPlaylistProvider
{
    public Task<Playlist?> GetPlaylist(string url, CancellationToken ct);
    public Task<Playlist?> RefreshPlaylist(Playlist playlist, CancellationToken ct);
}