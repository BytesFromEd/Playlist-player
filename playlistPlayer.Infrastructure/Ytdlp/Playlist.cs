namespace playlistPlayer.Infrastructure.Ytdlp;

public class Playlist
{
    public string title { get; set; }
    public string uploader { get; set; }
    public List<Video> entries { get; set; }
}