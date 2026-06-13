namespace playlistPlayer.Core;

class PlaybackQueue
{
    private List<Song> queue = [];
    private readonly Playlist playlist;

    public PlaybackQueue(Playlist playlist)
    {
        this.playlist = playlist;
        Refresh();
    }

    public void Shuffle()
    {
        queue = [.. queue.Shuffle()];
    }

    public void Refresh()
    {
        queue = [.. playlist.GetSongs()];
    }
}