using Core.Interfaces;
using Core.Models;

namespace Infrastructure.MediaPlayer;

public class QueueManager(Playlist playlist) : IQueue
{
    private int index { get; set; } = 0;
    private List<Song> queue = playlist.GetSongs();

    public void Shuffle()
    {
        var song = queue[index];
        queue.RemoveAt(index);
        queue = [.. queue.OrderBy(x => Guid.NewGuid())];
        index = 0;
        queue.Insert(index, song);
    }

    public int Next()
    {
        index = (index + 1) % queue.Count;
        return index;
    }

    public int Previous()
    {
        index = index == 0 ? queue.Count - 1 : index - 1;
        return index;
    }

    public List<Song> GetSongs()
    {
        return queue;
    }

    public void SetIndex(Song song)
    {
        index = queue.FindIndex(x => x.GetId() == song.GetId());
        if (index < 0)
            throw new IndexOutOfRangeException();
    }
}