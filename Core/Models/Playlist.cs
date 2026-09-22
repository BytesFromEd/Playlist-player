using Core.Models.Enums;

namespace Core.Models;

public class Playlist(string name, string owner, string id, Provider provider, string? thumbnail)
{
    public string Name { get; set; } = name;
    public string Owner { get; set; } = owner;
    public string Id { get; set; } = id;
    public List<Song> Songs { get; set; } = [];
    public Provider Provider { get; set; } = provider;
    public string? Thumbnail { get; set; } = thumbnail;
}