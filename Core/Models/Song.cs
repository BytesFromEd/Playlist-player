using Core.Models.Enums;

namespace Core.Models;

public class Song(string id, string title, string artist, string file, int duration, DateTime lastPlayed, Provider provider)
{
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

    public string GetArtist()
    {
        return artist;
    }

    public int GetDuration()
    {
        return duration;
    }

    public DateTime GetLastPlayed()
    {
        return lastPlayed;
    }

    public Provider GetProvider()
    {
        return provider;
    }
}