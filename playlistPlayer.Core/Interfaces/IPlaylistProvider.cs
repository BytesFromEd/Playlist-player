namespace playlistPlayer.Core.Interfaces;

using playlistPlayer.Core.Models;

public interface IPlaylistProvider
{
    public Task<Playlist?> GetPlaylist(string url);
}