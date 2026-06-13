namespace playlistPlayer.Core;

public class Song(string id, string title, Author artist, string file, string cover, TimeSpan duration)
{
    private string id = id;
    private string title = title;
    private Author artist = artist;
    private string file = file;
    private string cover = cover;
    private TimeSpan duration = duration;

    public string GetId()
    {
        return id;
    }

    public string GetTitle()
    {
        return title;
    }

    public string GetFile()
    {
        return file;
    }

    public string GetCover()
    {
        return cover;
    }

    public Author GetArtist()
    {
        return artist;
    }

    public TimeSpan GetDuration()
    {
        return duration;
    }
}