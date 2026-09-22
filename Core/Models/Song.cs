using Core.Models.Enums;

namespace Core.Models;

public class Song(
    string id,
    string title,
    string artist,
    string file,
    string cover,
    int duration,
    Provider provider)
{
    public string Id { get; set; } = id;
    public string Title { get; set; } = title;
    public string Artist { get; set; } = artist;
    public string Cover { get; set; } = cover;
    public Provider Provider { get; set; } = provider;
    public string File { get; set; } = file;
    public int Duration { get; set; } = duration;
}