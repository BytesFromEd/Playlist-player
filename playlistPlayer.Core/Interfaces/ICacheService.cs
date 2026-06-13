namespace playlistPlayer.Core;

interface ICacheService
{
    public void SaveSong(Song song);
    public Song GetSong(string id);
}