namespace playlistPlayer.Core;

public class PlaylistManager
{
    private List<Song> songs;

    public void AddSong(Song song)
    {
        songs.Add(song);
    }
}
