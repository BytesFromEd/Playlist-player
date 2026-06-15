namespace playlistPlayer.Core.Models;

public class Author(string name)
{
    private readonly string name = name;

    public string GetName()
    {
        return name;
    }
}