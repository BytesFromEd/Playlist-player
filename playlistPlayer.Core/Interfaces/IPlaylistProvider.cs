namespace playlistPlayer.Core.Interfaces;

using playlistPlayer.Core.Models;

public interface IPlaylistProvider
{
    public Playlist GetPlaylist(string url);
}