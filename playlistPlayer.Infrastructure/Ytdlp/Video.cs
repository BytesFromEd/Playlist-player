namespace playlistPlayer.Infrastructure.Ytdlp;

public class Video
{
    public string id { get; set; }
    public string title { get; set; }
    public int? duration { get; set; }
    public string uploader { get; set; }
}