namespace playlistPlayer.Core.Models;

public class Song(string id, string title, Author artist, string file, TimeSpan duration, DateTime lastPlayed)
{
    private readonly string id = id;
    private readonly string title = title;
    private readonly Author artist = artist;
    private readonly string file = file;
    private readonly TimeSpan duration = duration;
    private readonly DateTime lastPlayed = lastPlayed;

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

    public Author GetArtist()
    {
        return artist;
    }

    public TimeSpan GetDuration()
    {
        return duration;
    }

    public DateTime GetLastPlayed()
    {
        return lastPlayed;
    }
}