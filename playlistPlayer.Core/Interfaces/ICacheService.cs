namespace playlistPlayer.Core.Interfaces;

using playlistPlayer.Core.Models;

public interface ICacheService
{
    public void SaveSong(Song song);
    public Song GetSong(string id);
}