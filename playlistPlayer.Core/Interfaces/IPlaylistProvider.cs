namespace playlistPlayer.Core;

interface IPlaylistProvider
{
    public Playlist GetPlaylist(string url);
}