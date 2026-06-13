namespace playlistPlayer.Core;

class Playlist
{
    private readonly string name;
    private readonly string owner;
    private readonly string id;
    private readonly List<Song> songs;

    public Playlist(string name, string owner, string id)
    {
        this.name = name;
        this.owner = owner;
        this.id = id;
        songs = [];
    }

    public Playlist(string name, string owner, string id, List<Song> songs)
    {
        this.name = name;
        this.owner = owner;
        this.id = id;
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
}