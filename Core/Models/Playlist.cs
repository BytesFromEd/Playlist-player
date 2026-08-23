using Core.Models.Enums;

namespace Core.Models;

public class Playlist
{
    private readonly string name;
    private readonly string owner;
    private readonly string id;
    private readonly List<Song> songs;
    private readonly Provider provider;
    private readonly string? thumbnail;

    public Playlist(string name, string owner, string id, Provider provider, string? thumbnail)
    {
        this.name = name;
        this.owner = owner;
        this.id = id;
        this.provider = provider;
        this.thumbnail = thumbnail;
        songs = [];
    }

    public Playlist(string name, string owner, string id, Provider provider, string? thumbnail, IEnumerable<Song> songs)
    {
        this.name = name;
        this.owner = owner;
        this.id = id;
        this.provider = provider;
        this.thumbnail = thumbnail;
        this.songs = [.. songs];
    }

    public List<Song> GetSongs()
    {
        return songs;
    }

    public void AddSong(Song song)
    {
        songs.Add(song);
    }

    public string GetName() { return name; }
    public string GetOwner() { return owner; }
    public string GetId() { return id; }
    public Provider GetProvider() { return provider; }
    public string? GetThumbanil() { return thumbnail; }
}