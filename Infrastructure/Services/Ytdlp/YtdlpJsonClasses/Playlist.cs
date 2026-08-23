namespace Infrastructure.Services.Ytdlp.YtdlpJsonClasses;

public class Playlist
{
    public string? Title { get; set; }
    public string? Uploader { get; set; }
    public List<Thumbnail>? Thumbnails { get; set; }
    public List<Video>? Entries { get; set; }
}