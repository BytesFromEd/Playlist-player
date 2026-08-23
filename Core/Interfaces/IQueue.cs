using Core.Models;

namespace Core.Interfaces;

public interface IQueue
{
    void Shuffle();
    int Next();
    int Previous();
    List<Song> GetSongs();
    void SetIndex(Song song);
}