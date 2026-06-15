namespace playlistPlayer.Core.Services;

using playlistPlayer.Core.Models;

class PlaybackQueue
{
    private List<Song> queue = [];
    private readonly Playlist playlist;
    private int curr = -1;

    public PlaybackQueue(Playlist playlist)
    {
        this.playlist = playlist;
        Refresh();
    }

    public void ShuffleQueue()
    {
        Song? song = null;

        if (curr >= 0)
        {
            song = queue[curr];
            queue.RemoveAt(curr);
        }

        queue = [.. Shuffle()];

        if (curr >= 0 && song != null)
            queue.Insert(0, song);
    }

    private IEnumerable<Song> Shuffle()
    {
        return queue.Shuffle();
    }

    public void Refresh()
    {
        queue = [.. playlist.GetSongs()];
    }

    public Song[] GetSongs()
    {
        Song[] songs = [];
        queue.CopyTo(songs);
        return songs;
    }

    public int GetCurrentIndex()
    {
        return curr;
    }

    public Song GetNext(int? index = null)
    {
        if (index < 0)
            throw new IndexOutOfRangeException("PlaylistQueue.GetNext() got a negative number!");
            
        curr = index ?? (curr + 1) % queue.Count;
        return queue[curr];
    }

    public Song GetPrev()
    {
        curr--;
        if (curr == -1)
        {
            curr = queue.Count - 1;
        }
        return queue[curr];
    }
}